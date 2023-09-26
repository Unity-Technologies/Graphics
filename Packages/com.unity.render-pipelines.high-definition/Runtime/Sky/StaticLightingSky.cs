using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Class controlling which sky is used for static and baked lighting.
    /// </summary>
    [HDRPHelpURLAttribute("Static-Lighting-Sky")]
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide this object from the Add Component menu
    public class StaticLightingSky : MonoBehaviour
    {
        [SerializeField]
        VolumeProfile m_Profile;
        bool m_NeedUpdateStaticLightingSky;

        [SerializeField, FormerlySerializedAs("m_BakingSkyUniqueID")]
        int m_StaticLightingSkyUniqueID = 0;
        int m_LastComputedHash;

        [SerializeField]
        int m_StaticLightingCloudsUniqueID = 0;
        int m_LastComputedCloudHash;

        SkySettings m_SkySettings; // This one contain only property values from overridden properties in the original profile component
        SkySettings m_SkySettingsFromProfile;

        CloudSettings m_CloudSettings; // This one contain only property values from overridden properties in the original profile component
        CloudSettings m_CloudSettingsFromProfile;

        // Volumetric Clouds
        [SerializeField]
        bool m_StaticLightingVolumetricClouds = false;
        int m_LastComputedVolumetricCloudHash;
        VolumetricClouds m_VolumetricClouds;
        VolumetricClouds m_VolumetricCloudSettingsFromProfile;

        internal SkySettings skySettings
        {
            get
            {
                GetSkyFromIDAndVolume(m_StaticLightingSkyUniqueID, m_Profile, out var skyFromProfile, out var skyType);
                if (skyFromProfile != null)
                {
                    int newHash = skyFromProfile.GetHashCode();
                    if (m_LastComputedHash != newHash)
                        UpdateCurrentStaticLightingSky();
                }
                else
                {
                    ResetSky();
                }
                return m_SkySettings;
            }
        }

        internal CloudSettings cloudSettings
        {
            get
            {
                GetCloudFromIDAndVolume(m_StaticLightingCloudsUniqueID, m_Profile, out var cloudFromProfile, out var cloudType);
                if (cloudFromProfile != null)
                {
                    int newHash = cloudFromProfile.GetHashCode();
                    if (m_LastComputedCloudHash != newHash)
                        UpdateCurrentStaticLightingClouds();
                }
                else
                {
                    ResetCloud();
                }
                return m_CloudSettings;
            }
        }

        internal VolumetricClouds volumetricClouds
        {
            get
            {
                return m_StaticLightingVolumetricClouds ? m_VolumetricClouds : null;
            }
        }

        List<SkySettings> m_VolumeSkyList = new List<SkySettings>();
        List<CloudSettings> m_VolumeCloudsList = new List<CloudSettings>();


        /// <summary>
        /// Volume profile where the sky settings used for static lighting will be fetched.
        /// </summary>
        public VolumeProfile profile
        {
            get
            {
                return m_Profile;
            }
            set
            {
                if (value != m_Profile)
                {
                    // Changing the volume is considered a destructive operation => reset the static lighting sky.
                    m_StaticLightingSkyUniqueID = 0;

                    //Registration is also done when we go from null to not null
                    if (m_Profile == null)
                        SkyManager.RegisterStaticLightingSky(this);

                    //Unregistration is also done when we go from not null to null
                    if (value == null)
                        SkyManager.UnRegisterStaticLightingSky(this);
                }

                m_Profile = value;
            }
        }

        /// <summary>
        /// Unique ID of the sky used for static lighting.
        /// The unique ID should be for a sky that is present in the profile. See SkySettings.GetUniqueID to get the ID per sky type.
        /// </summary>
        public int staticLightingSkyUniqueID
        {
            get
            {
                return m_StaticLightingSkyUniqueID;
            }
            set
            {
                m_StaticLightingSkyUniqueID = value;
                UpdateCurrentStaticLightingSky();
            }
        }

        /// <summary>
        /// Unique ID of the clouds used for static lighting.
        /// The unique ID should be for a cloud type that is present in the profile. See CloudSettings.GetUniqueID to get the ID per cloud type.
        /// </summary>
        public int staticLightingCloudsUniqueID
        {
            get
            {
                return m_StaticLightingCloudsUniqueID;
            }
            set
            {
                m_StaticLightingCloudsUniqueID = value;
                UpdateCurrentStaticLightingClouds();
            }
        }

        void GetSkyFromIDAndVolume(int skyUniqueID, VolumeProfile profile, out SkySettings skySetting, out System.Type skyType)
        {
            skySetting = null;
            skyType = typeof(SkySettings);
            if (profile != null && skyUniqueID != 0)
            {
                m_VolumeSkyList.Clear();
                if (profile.TryGetAllSubclassOf<SkySettings>(typeof(SkySettings), m_VolumeSkyList))
                {
                    foreach (var sky in m_VolumeSkyList)
                    {
                        if (skyUniqueID == SkySettings.GetUniqueID(sky.GetType()) && sky.active)
                        {
                            skyType = sky.GetType();
                            skySetting = sky;
                        }
                    }
                }
            }
        }

        void GetCloudFromIDAndVolume(int cloudUniqueID, VolumeProfile profile, out CloudSettings cloudSetting, out System.Type cloudType)
        {
            cloudSetting = null;
            cloudType = typeof(CloudSettings);
            if (profile != null && cloudUniqueID != 0)
            {
                m_VolumeCloudsList.Clear();
                if (profile.TryGetAllSubclassOf<CloudSettings>(typeof(CloudSettings), m_VolumeCloudsList))
                {
                    foreach (var cloud in m_VolumeCloudsList)
                    {
                        if (cloudUniqueID == CloudSettings.GetUniqueID(cloud.GetType()) && cloud.active)
                        {
                            cloudType = cloud.GetType();
                            cloudSetting = cloud;
                        }
                    }
                }
            }
        }

        void GetVolumetricCloudVolume(VolumeProfile profile, out VolumetricClouds volumetricClouds)
        {
            volumetricClouds = null;
            if (profile != null)
                profile.TryGet<VolumetricClouds>(out volumetricClouds);
        }

        private int InitComponentFromProfile<T>(T component, T componentFromProfile, Type type)
            where T : VolumeComponent
        {
            // The static lighting sky is a Volume Component that lives outside of the volume system (we just grab a component from a profile)
            // As such, it may contain values that are not actually overridden
            // For example, user overrides a value, change it, and disable overrides. In this case the volume still contains the old overridden value
            // In this case, we want to use values only if they are still overridden, so we create a volume component with default values and then copy the overridden values from the profile.
            // Also, a default profile might be set in the HDRP project settings, this volume is applied by default to all the scene so it should also be taken into account here.

            var newParameters = component.parameters;
            var profileParameters = componentFromProfile.parameters;

            var defaultVolume = HDRenderPipelineGlobalSettings.instance.GetOrCreateDefaultVolume();
            T defaultComponent = null;
            if (defaultVolume.sharedProfile != null)     // This can happen with old projects.
                defaultVolume.sharedProfile.TryGet(type, out defaultComponent);
            var defaultParameters = defaultComponent != null ? defaultComponent.parameters : null;     // Can be null if the profile does not contain the component.

            // Seems to inexplicably happen sometimes on domain reload.
            if (profileParameters == null)
                return 0;

            int parameterCount = newParameters.Count;
            // Copy overridden parameters.
            for (int i = 0; i < parameterCount; ++i)
            {
                if (profileParameters[i].overrideState == true)
                {
                    newParameters[i].SetValue(profileParameters[i]);
                }
                // Fallback to the default profile if values are overridden in there.
                else if (defaultParameters != null && defaultParameters[i].overrideState == true)
                {
                    newParameters[i].SetValue(defaultParameters[i]);
                }
            }

            return componentFromProfile.GetHashCode();
        }

        void UpdateCurrentStaticLightingSky()
        {
            if ((RenderPipelineManager.currentPipeline is HDRenderPipeline) == false)
                return;

            // First, grab the sky settings of the right type in the profile.
            CoreUtils.Destroy(m_SkySettings);
            m_SkySettings = null;
            m_LastComputedHash = 0;
            GetSkyFromIDAndVolume(m_StaticLightingSkyUniqueID, m_Profile, out m_SkySettingsFromProfile, out var skyType);

            if (m_SkySettingsFromProfile != null)
            {
                m_SkySettings = (SkySettings)ScriptableObject.CreateInstance(skyType);
                m_LastComputedHash = InitComponentFromProfile(m_SkySettings, m_SkySettingsFromProfile, skyType);
            }
        }

        void UpdateCurrentStaticLightingClouds()
        {
            // First, grab the cloud settings of the right type in the profile.
            CoreUtils.Destroy(m_CloudSettings);
            m_CloudSettings = null;
            m_LastComputedCloudHash = 0;
            GetCloudFromIDAndVolume(m_StaticLightingCloudsUniqueID, m_Profile, out m_CloudSettingsFromProfile, out var cloudType);

            if (m_CloudSettingsFromProfile != null)
            {
                m_CloudSettings = (CloudSettings)ScriptableObject.CreateInstance(cloudType);
                m_LastComputedCloudHash = InitComponentFromProfile(m_CloudSettings, m_CloudSettingsFromProfile, cloudType);
            }
        }

        void UpdateCurrentStaticLightingVolumetricClouds()
        {
            // First, grab the cloud settings of the right type in the profile.
            CoreUtils.Destroy(m_VolumetricClouds);
            m_VolumetricClouds = null;
            m_LastComputedVolumetricCloudHash = 0;
            GetVolumetricCloudVolume(m_Profile, out m_VolumetricCloudSettingsFromProfile);

            if (m_VolumetricCloudSettingsFromProfile != null)
            {
                m_VolumetricClouds = (VolumetricClouds)ScriptableObject.CreateInstance(typeof(VolumetricClouds));
                m_LastComputedVolumetricCloudHash = InitComponentFromProfile(m_VolumetricClouds, m_VolumetricCloudSettingsFromProfile, typeof(VolumetricClouds));
            }
        }

        // All actions done in this method are because Editor won't go through setters so we need to manually check consistency of our data.
        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            // If we detect that the profile has been removed we need to reset the static lighting sky.
            if (m_Profile == null)
            {
                m_StaticLightingSkyUniqueID = 0;
                m_StaticLightingCloudsUniqueID = 0;
                m_StaticLightingVolumetricClouds = false;
            }

            // If we detect that the profile has changed, we need to reset the static lighting sky.
            // We have to do that manually because PropertyField won't go through setters.
            if (profile != null)
            {
                if (m_SkySettingsFromProfile != null && !profile.components.Find(x => x == m_SkySettingsFromProfile))
                    m_StaticLightingSkyUniqueID = 0;
                if (m_CloudSettingsFromProfile != null && !profile.components.Find(x => x == m_CloudSettingsFromProfile))
                    m_StaticLightingCloudsUniqueID = 0;
            }

            // We can't call UpdateCurrentStaticLightingSky in OnValidate because we may destroy an object there and it's forbidden.
            // So we delay the update.
            m_NeedUpdateStaticLightingSky = true;
        }

        // Fix UUM-45262: There is a race condition between StaticLightingSky.OnEnable() and VolumeComponent.OnEnable().
        // StaticLightingSky wants to use the VolumeComponents assuming that OnEnable() has been executed (and therefore
        // VolumeComponent.parameters has been populated), but nothing guarantees this. In this case we need to defer
        // StaticLightingSky update. This issue is fixed in 2023.2.
        bool VerifyProfileComponentsInitialized()
        {
            if (m_Profile != null)
            {
                foreach (var c in m_Profile.components)
                {
                    if (c.parameters == null || c.parameters.Count == 0)
                        return false;
                }
            }
            return true;
        }

        void OnEnable()
        {
            if (VerifyProfileComponentsInitialized())
            {
                UpdateCurrentStaticLightingSky();
                UpdateCurrentStaticLightingClouds();
                UpdateCurrentStaticLightingVolumetricClouds();
            }
            else
            {
                m_NeedUpdateStaticLightingSky = true;
            }

            if (m_Profile != null)
                SkyManager.RegisterStaticLightingSky(this);
        }

        void OnDisable()
        {
            if (m_Profile != null)
                SkyManager.UnRegisterStaticLightingSky(this);

            ResetSky();
            ResetCloud();
            ResetVolumetricCloud();
        }

        void Update()
        {
            if (m_NeedUpdateStaticLightingSky)
            {
                UpdateCurrentStaticLightingSky();
                UpdateCurrentStaticLightingClouds();
                UpdateCurrentStaticLightingVolumetricClouds();
                m_NeedUpdateStaticLightingSky = false;
            }
        }

        void ResetSky()
        {
            CoreUtils.Destroy(m_SkySettings);
            m_SkySettings = null;
            m_SkySettingsFromProfile = null;
            m_LastComputedHash = 0;
        }

        void ResetCloud()
        {
            CoreUtils.Destroy(m_CloudSettings);
            m_CloudSettings = null;
            m_CloudSettingsFromProfile = null;
            m_LastComputedCloudHash = 0;
        }

        void ResetVolumetricCloud()
        {
            CoreUtils.Destroy(m_VolumetricClouds);
            m_VolumetricClouds = null;
            m_CloudSettingsFromProfile = null;
            m_LastComputedVolumetricCloudHash = 0;
        }
    }
}
