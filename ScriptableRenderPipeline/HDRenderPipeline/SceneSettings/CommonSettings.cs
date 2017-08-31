using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class CommonSettings : ScriptableObject
    {
        [Serializable]
        public struct Settings
        {
            // Shadows
            [SerializeField]
            float m_ShadowMaxDistance;
            [SerializeField]
            float m_ShadowNearPlaneOffset;

            public float shadowMaxDistance      { set { m_ShadowMaxDistance   = value; OnValidate(); } get { return m_ShadowMaxDistance; } }
            public float shadowNearPlaneOffset  { set { m_ShadowNearPlaneOffset = value; OnValidate(); } get { return m_ShadowNearPlaneOffset; } }

            void OnValidate()
            {
                m_ShadowMaxDistance     = Mathf.Max(0.0f, m_ShadowMaxDistance);
                m_ShadowNearPlaneOffset = Mathf.Max(0, m_ShadowNearPlaneOffset);
            }

            public static readonly Settings s_Defaultsettings = new Settings
            {
                m_ShadowMaxDistance     = ShadowSettings.kDefaultMaxShadowDistance,
                m_ShadowNearPlaneOffset = ShadowSettings.kDefaultDirectionalNearPlaneOffset,
            };
        }

        [SerializeField]
        private Settings m_Settings = Settings.s_Defaultsettings;

        public Settings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }
    }
}
