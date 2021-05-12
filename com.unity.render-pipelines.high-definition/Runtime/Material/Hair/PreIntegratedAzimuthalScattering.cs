using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class PreIntegratedAzimuthalScattering
    {
        [GenerateHLSL]
        public enum AzimuthalScatteringTexture
        {
            Resolution = 256
        }

        private static PreIntegratedAzimuthalScattering s_Instance;

        public static PreIntegratedAzimuthalScattering instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PreIntegratedAzimuthalScattering();

                return s_Instance;
            }
        }

        private bool m_IsInit = false;

        Material m_PreIntegratedAzimuthalScatteringMaterial = null;
        RenderTexture m_PreIntegratedAzimuthalScatteringRT  = null;

        PreIntegratedAzimuthalScattering() => m_IsInit = false;

        public void Build()
        {
            var res = (int)AzimuthalScatteringTexture.Resolution;
            var format = GraphicsFormat.A2B10G10R10_UNormPack32;

            m_PreIntegratedAzimuthalScatteringMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.preIntegratedAzimuthalScatteringPS);
            m_PreIntegratedAzimuthalScatteringRT = new RenderTexture(res, res, 0, format);
            m_PreIntegratedAzimuthalScatteringRT.hideFlags = HideFlags.HideAndDontSave;
            m_PreIntegratedAzimuthalScatteringRT.filterMode = FilterMode.Bilinear;
            m_PreIntegratedAzimuthalScatteringRT.wrapMode = TextureWrapMode.Clamp;
            m_PreIntegratedAzimuthalScatteringRT.name = CoreUtils.GetRenderTargetAutoName(res, res, 1, format, "PreIntegratedAzimuthalScattering");
            m_PreIntegratedAzimuthalScatteringRT.Create();

            m_IsInit = false;
        }

        public void RenderInit(CommandBuffer cmd)
        {
            if (m_IsInit && m_PreIntegratedAzimuthalScatteringRT.IsCreated())
                return;

            // Execute the pre-integration.
            CoreUtils.DrawFullScreen(cmd, m_PreIntegratedAzimuthalScatteringMaterial, new RenderTargetIdentifier(m_PreIntegratedAzimuthalScatteringRT));

            m_IsInit = true;
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_PreIntegratedAzimuthalScatteringMaterial);
            CoreUtils.Destroy(m_PreIntegratedAzimuthalScatteringRT);
        }

        public void Bind(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._PreIntegratedAzimuthalScattering, m_PreIntegratedAzimuthalScatteringRT);
        }
    }
}
