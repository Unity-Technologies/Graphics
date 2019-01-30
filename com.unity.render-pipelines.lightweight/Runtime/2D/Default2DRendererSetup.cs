using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class Default2DRendererSetup : IRendererSetup
    {
        Default2DRendererData m_RendererData;

        SetupForwardRenderingPass m_SetupForwardRenderingPass;
        Render2DLightingPass m_Render2DLightingPass;

#if UNITY_EDITOR
        GizmoRenderingPass m_LitGizmoRenderingPass;
        GizmoRenderingPass m_UnlitGizmoRenderingPass;
#endif

        // TODO: Create intermediate color texture as needed.

        public Default2DRendererSetup(Default2DRendererData data)
        {
            m_RendererData = data;

            m_Render2DLightingPass = new Render2DLightingPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();

#if UNITY_EDITOR
            m_LitGizmoRenderingPass = new GizmoRenderingPass();
            m_UnlitGizmoRenderingPass = new GizmoRenderingPass();
#endif
        }

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            m_Render2DLightingPass.Setup(m_RendererData.lightIntensityScale, m_RendererData.lightOperations, renderingData.cameraData.camera);
            renderer.EnqueuePass(m_Render2DLightingPass);

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_LitGizmoRenderingPass.Setup(true);
                renderer.EnqueuePass(m_LitGizmoRenderingPass);

                // TODO: Move this after post processing passes.
                m_UnlitGizmoRenderingPass.Setup(false);
                renderer.EnqueuePass(m_UnlitGizmoRenderingPass);
            }   
#endif
        }
    }
}
