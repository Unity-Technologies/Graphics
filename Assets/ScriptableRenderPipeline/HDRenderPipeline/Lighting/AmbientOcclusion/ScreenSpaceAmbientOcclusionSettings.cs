using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceAmbientOcclusionSettings : ScriptableObject
    {
        [Serializable]
        public struct Settings
        {
            [SerializeField]
            bool m_Enable;

            [SerializeField, Range(0, 2)]
            float m_Intensity;
            [SerializeField]
            float m_Radius;

            [SerializeField, Range(1, 32)]
            int m_SampleCount;
            [SerializeField]
            bool m_Downsampling;

            public bool enable { set { m_Enable = value; } get { return m_Enable; } }
            public float intensity { set { m_Intensity = value; OnValidate(); } get { return m_Intensity; } }
            public float radius { set { m_Radius = value; OnValidate(); } get { return m_Radius; } }
            public int sampleCount { set { m_SampleCount = value; OnValidate(); } get { return m_SampleCount; } }
            public bool downsampling { set { m_Downsampling = value; } get { return m_Downsampling; } }

            public void OnValidate()
            {
                m_Intensity = Mathf.Clamp(m_Intensity, 0, 2);
                m_Radius = Mathf.Max(0, m_Radius);
                m_SampleCount = Mathf.Clamp(m_SampleCount, 1, 32);
            }

            public static readonly Settings s_Defaultsettings = new Settings
            {
                m_Enable = false,
                m_Intensity = 1.0f,
                m_Radius = 0.5f,
                m_SampleCount = 8,
                m_Downsampling = true
            };
        }

        [SerializeField]
        Settings m_Settings = Settings.s_Defaultsettings;

        public Settings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }

        public void OnValidate()
        {
            m_Settings.OnValidate();
        }
    }
}
