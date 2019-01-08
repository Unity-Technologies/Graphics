using System;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class Default2DRendererSetup : IRendererSetup
    {
        private DrawSkyboxPass m_DrawSkyboxPass;
        private Render2DLightingPass m_Render2DLightingPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;
        //Render2DFallbackPass   m_Render2DFallbackPass;

        private RenderTargetHandle m_DepthTexture;

        [NonSerialized]
        private bool m_Initialized = false;

        private Default2DRendererData m_RendererData;

        public Default2DRendererSetup(Default2DRendererData data)
        {
            m_RendererData = data;
        }

        private void Init()
        {
            if (m_Initialized)
                return;

            m_DepthTexture.Init("_CameraDepthTexture");
            m_Render2DLightingPass = new Render2DLightingPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();

            m_DrawSkyboxPass = new DrawSkyboxPass();

            m_Initialized = true;
        }

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            m_Render2DLightingPass.Setup(
                RenderSettings.ambientLight,
                m_RendererData.m_AmbientRenderTextureInfo,
                m_RendererData.m_SpecularRenderTextureInfo,
                m_RendererData.m_RimRenderTextureInfo,
                m_RendererData.m_PointLightNormalRenderTextureInfo,
                m_RendererData.m_PointLightColorRenderTextureInfo,
                m_RendererData.LightIntensityScale
            );

            renderer.EnqueuePass(m_Render2DLightingPass);
        }
    }
}
