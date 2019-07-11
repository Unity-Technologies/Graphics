using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [ExecuteAlways]
    public class StaticLightingSky : MonoBehaviour
    {
        [SerializeField]
        VolumeProfile m_Profile;
        [SerializeField, FormerlySerializedAs("m_BakingSkyUniqueID")]
        int m_StaticLightingSkyUniqueID = 0;

        public SkySettings skySettings { get; private set; }

        List<SkySettings> m_VolumeSkyList = new List<SkySettings>();


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

        void UpdateCurrentStaticLightingSky()
        {
            skySettings = GetSkyFromIDAndVolume(m_StaticLightingSkyUniqueID, m_Profile);
        }

        SkySettings GetSkyFromIDAndVolume(int skyUniqueID, VolumeProfile profile)
        {
            if (profile != null && skyUniqueID != 0)
            {
                m_VolumeSkyList.Clear();
                if (m_Profile.TryGetAllSubclassOf<SkySettings>(typeof(SkySettings), m_VolumeSkyList))
                {
                    foreach (var sky in m_VolumeSkyList)
                    {
                        if (skyUniqueID == SkySettings.GetUniqueID(sky.GetType()))
                        {
                            return sky;
                        }
                    }
                }
            }

            return null;
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
            if (profile != null && skySettings != null)
            {
                if (!profile.components.Find(x => x == skySettings))
                {
                    m_StaticLightingSkyUniqueID = 0;
                }
            }

            UpdateCurrentStaticLightingSky();
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
            skySettings = null;
        }
    }
}
