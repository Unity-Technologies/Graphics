using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Class controlling which sky is used for static and baked lighting.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Static-Lighting-Sky" + Documentation.endURL)]
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide this object from the Add Component menu
    public class StaticLightingSky : MonoBehaviour
    {
        [SerializeField]
        VolumeProfile m_Profile;
        [SerializeField, FormerlySerializedAs("m_BakingSkyUniqueID")]
        int m_StaticLightingSkyUniqueID = 0;
        int m_LastComputedHash;
        bool m_NeedUpdateStaticLightingSky;

        SkySettings m_SkySettings; // This one contain only property values from overridden properties in the original profile component
        SkySettings m_SkySettingsFromProfile;

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
                    Reset();
                }
                return m_SkySettings;
            }
        }

        List<SkySettings> m_VolumeSkyList = new List<SkySettings>();

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
                        if (skyUniqueID == SkySettings.GetUniqueID(sky.GetType()))
                        {
                            skyType = sky.GetType();
                            skySetting = sky;
                        }
                    }
                }
            }
        }

        void UpdateCurrentStaticLightingSky()
        {
            // First, grab the sky settings of the right type in the profile.
            CoreUtils.Destroy(m_SkySettings);
            m_SkySettings = null;
            m_LastComputedHash = 0;
            GetSkyFromIDAndVolume(m_StaticLightingSkyUniqueID, m_Profile, out m_SkySettingsFromProfile, out var skyType);

            if (m_SkySettingsFromProfile != null)
            {
                // The static lighting sky is a Volume Component that lives outside of the volume system (we just grab a component from a profile)
                // As such, it may contain values that are not actually overridden
                // For example, user overrides a value, change it, and disable overrides. In this case the volume still contains the old overridden value
                // In this case, we want to use values only if they are still overridden, so we create a volume component with default values and then copy the overridden values from the profile.
                // Also, a default profile might be set in the HDRP project settings, this volume is applied by default to all the scene so it should also be taken into account here.

                // Create an instance with default values
                m_SkySettings = (SkySettings)ScriptableObject.CreateInstance(skyType);
                var newSkyParameters = m_SkySettings.parameters;
                var profileSkyParameters = m_SkySettingsFromProfile.parameters;

                var defaultVolume = HDRenderPipeline.GetOrCreateDefaultVolume();
                SkySettings defaultSky = null;
                if (defaultVolume.sharedProfile != null) // This can happen with old projects.
                    defaultVolume.sharedProfile.TryGet(skyType, out defaultSky);
                var defaultSkyParameters = defaultSky != null ? defaultSky.parameters : null; // Can be null if the profile does not contain the component.

                // Seems to inexplicably happen sometimes on domain reload.
                if (profileSkyParameters == null)
                {
                    return;
                }

                int parameterCount = m_SkySettings.parameters.Count;
                // Copy overridden parameters.
                for (int i  = 0; i < parameterCount; ++i)
                {
                    if (profileSkyParameters[i].overrideState == true)
                    {
                        newSkyParameters[i].SetValue(profileSkyParameters[i]);
                    }
                    // Fallback to the default profile if values are overridden in there.
                    else if (defaultSkyParameters != null && defaultSkyParameters[i].overrideState == true)
                    {
                        newSkyParameters[i].SetValue(defaultSkyParameters[i]);
                    }
                }

                m_LastComputedHash = m_SkySettingsFromProfile.GetHashCode();
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
            }

            // If we detect that the profile has changed, we need to reset the static lighting sky.
            // We have to do that manually because PropertyField won't go through setters.
            if (profile != null && m_SkySettingsFromProfile != null)
            {
                if (!profile.components.Find(x => x == m_SkySettingsFromProfile))
                {
                    m_StaticLightingSkyUniqueID = 0;
                }
            }

            // We can't call UpdateCurrentStaticLightingSky in OnValidate because we may destroy an object there and it's forbidden.
            // So we delay the update.
            m_NeedUpdateStaticLightingSky = true;
        }

        void OnEnable()
        {
            UpdateCurrentStaticLightingSky();
            if (m_Profile != null)
                SkyManager.RegisterStaticLightingSky(this);
        }

        void OnDisable()
        {
            if (m_Profile != null)
                SkyManager.UnRegisterStaticLightingSky(this);

            Reset();
        }

        void Update()
        {
            if (m_NeedUpdateStaticLightingSky)
            {
                UpdateCurrentStaticLightingSky();
                m_NeedUpdateStaticLightingSky = false;
            }
        }

        void Reset()
        {
            CoreUtils.Destroy(m_SkySettings);
            m_SkySettings = null;
            m_SkySettingsFromProfile = null;
            m_LastComputedHash = 0;
        }
    }
}
