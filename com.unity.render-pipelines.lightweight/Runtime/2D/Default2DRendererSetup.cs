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

#if UNITY_EDITOR
        private GizmoRenderingPass m_LitGizmoRenderingPass;
        private GizmoRenderingPass m_UnlitGizmoRenderingPass;
#endif

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

#if UNITY_EDITOR
            m_LitGizmoRenderingPass = new GizmoRenderingPass();
            m_UnlitGizmoRenderingPass = new GizmoRenderingPass();
#endif

            m_Initialized = true;
        }

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            m_Render2DLightingPass.Setup(
                m_RendererData.m_PointLightNormalRenderTextureInfo,
                m_RendererData.m_PointLightColorRenderTextureInfo,
                m_RendererData.LightIntensityScale,
                m_RendererData.shapeLightTypes,
                renderingData.cameraData.camera
            );

            renderer.EnqueuePass(m_Render2DLightingPass);

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_LitGizmoRenderingPass.Setup(true);
                renderer.EnqueuePass(m_LitGizmoRenderingPass);

                m_UnlitGizmoRenderingPass.Setup(false);
                renderer.EnqueuePass(m_UnlitGizmoRenderingPass);
            }   
#endif
        }
    }
}
