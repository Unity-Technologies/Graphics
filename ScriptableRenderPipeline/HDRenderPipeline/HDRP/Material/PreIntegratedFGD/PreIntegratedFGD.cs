using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Currently PreIntegratedFGD only have GGX, if we add another case convert it to a textureArray (like LTCArea)
    public partial class PreIntegratedFGD
    {
        static PreIntegratedFGD s_Instance;

        public static PreIntegratedFGD instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PreIntegratedFGD();

                return s_Instance;
            }
        }

        bool m_isInit;
        int m_refCounting;

        // For image based lighting
        Material m_PreIntegratedFGDMaterial;
        RenderTexture m_PreIntegratedFGD;

        PreIntegratedFGD()
        {
            m_isInit = false;
            m_refCounting = 0;
        }

        public void Build()
        {
            Debug.Assert(m_refCounting >= 0);

            if (m_refCounting == 0)
            {
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                m_PreIntegratedFGDMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.preIntegratedFGD);

                m_PreIntegratedFGD = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
                m_PreIntegratedFGD.hideFlags = HideFlags.HideAndDontSave;
                m_PreIntegratedFGD.filterMode = FilterMode.Bilinear;
                m_PreIntegratedFGD.wrapMode = TextureWrapMode.Clamp;
                m_PreIntegratedFGD.hideFlags = HideFlags.DontSave;
                m_PreIntegratedFGD.name = CoreUtils.GetRenderTargetAutoName(128, 128, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD");
                m_PreIntegratedFGD.Create();

                m_isInit = false;
            }

            m_refCounting++;
        }

        public void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            using (new ProfilingSample(cmd, "PreIntegratedFGD Material Generation"))
            {
                CoreUtils.DrawFullScreen(cmd, m_PreIntegratedFGDMaterial, new RenderTargetIdentifier(m_PreIntegratedFGD));
            }

            m_isInit = true;
        }

        public void Cleanup()
        {
            m_refCounting--;

            if (m_refCounting == 0)
            {
                CoreUtils.Destroy(m_PreIntegratedFGDMaterial);
                CoreUtils.Destroy(m_PreIntegratedFGD);

                m_isInit = false;
            }

            Debug.Assert(m_refCounting >= 0);
        }

        public void Bind()
        {
            Shader.SetGlobalTexture("_PreIntegratedFGD", m_PreIntegratedFGD);
        }
    }
}
