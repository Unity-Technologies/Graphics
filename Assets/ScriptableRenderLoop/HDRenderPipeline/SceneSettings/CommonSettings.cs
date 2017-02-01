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
            int   m_ShadowCascadeCount;
            [SerializeField]
            float m_ShadowCascadeSplit0;
            [SerializeField]
            float m_ShadowCascadeSplit1;
            [SerializeField]
            float m_ShadowCascadeSplit2;

            public float shadowMaxDistance   { set { m_ShadowMaxDistance   = value; OnValidate(); } get { return m_ShadowMaxDistance; } }
            public int   shadowCascadeCount  { set { m_ShadowCascadeCount  = value; OnValidate(); } get { return m_ShadowCascadeCount; } }
            public float shadowCascadeSplit0 { set { m_ShadowCascadeSplit0 = value; OnValidate(); } get { return m_ShadowCascadeSplit0; } }
            public float shadowCascadeSplit1 { set { m_ShadowCascadeSplit1 = value; OnValidate(); } get { return m_ShadowCascadeSplit1; } }
            public float shadowCascadeSplit2 { set { m_ShadowCascadeSplit2 = value; OnValidate(); } get { return m_ShadowCascadeSplit2; } }

            // Subsurface scattering
            [SerializeField] [ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
            Color m_SssProfileStdDev1;
            [SerializeField] [ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
            Color m_SssProfileStdDev2;
            [SerializeField]
            float m_SssProfileLerpWeight;
            [SerializeField]
            float m_SssBilateralScale;

            public Color sssProfileStdDev1    { set { m_SssProfileStdDev1    = value; OnValidate(); } get { return m_SssProfileStdDev1; } }
            public Color sssProfileStdDev2    { set { m_SssProfileStdDev2    = value; OnValidate(); } get { return m_SssProfileStdDev2; } }
            public float sssProfileLerpWeight { set { m_SssProfileLerpWeight = value; OnValidate(); } get { return m_SssProfileLerpWeight; } }
            public float sssBilateralScale    { set { m_SssBilateralScale    = value; OnValidate(); } get { return m_SssBilateralScale; } }

            void OnValidate()
            {
                m_ShadowMaxDistance    = Mathf.Max(0.0f, m_ShadowMaxDistance);
                m_ShadowCascadeCount   = Math.Min(4, Math.Max(1, m_ShadowCascadeCount));
                m_ShadowCascadeSplit0  = Mathf.Clamp01(m_ShadowCascadeSplit0);
                m_ShadowCascadeSplit1  = Mathf.Clamp01(m_ShadowCascadeSplit1);
                m_ShadowCascadeSplit2  = Mathf.Clamp01(m_ShadowCascadeSplit2);

                m_SssProfileStdDev1.r  = Mathf.Max(0.05f, m_SssProfileStdDev1.r);
                m_SssProfileStdDev1.g  = Mathf.Max(0.05f, m_SssProfileStdDev1.g);
                m_SssProfileStdDev1.b  = Mathf.Max(0.05f, m_SssProfileStdDev1.b);
                m_SssProfileStdDev1.a  = 0.0f;
                m_SssProfileStdDev2.r  = Mathf.Max(0.05f, m_SssProfileStdDev2.r);
                m_SssProfileStdDev2.g  = Mathf.Max(0.05f, m_SssProfileStdDev2.g);
                m_SssProfileStdDev2.b  = Mathf.Max(0.05f, m_SssProfileStdDev2.b);
                m_SssProfileStdDev2.a  = 0.0f;
                m_SssProfileLerpWeight = Mathf.Clamp01(m_SssProfileLerpWeight);
                m_SssBilateralScale    = Mathf.Clamp01(m_SssBilateralScale);
            }

            public static readonly Settings s_Defaultsettings = new Settings
            {
                m_ShadowMaxDistance    = ShadowSettings.Default.maxShadowDistance,
                m_ShadowCascadeCount   = ShadowSettings.Default.directionalLightCascadeCount,
                m_ShadowCascadeSplit0  = ShadowSettings.Default.directionalLightCascades.x,
                m_ShadowCascadeSplit1  = ShadowSettings.Default.directionalLightCascades.y,
                m_ShadowCascadeSplit2  = ShadowSettings.Default.directionalLightCascades.z,

                m_SssProfileStdDev1    = SubsurfaceScatteringProfile.Default.stdDev1,
                m_SssProfileStdDev2    = SubsurfaceScatteringProfile.Default.stdDev2,
                m_SssProfileLerpWeight = SubsurfaceScatteringProfile.Default.lerpWeight,
                m_SssBilateralScale    = SubsurfaceScatteringParameters.Default.bilateralScale
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
