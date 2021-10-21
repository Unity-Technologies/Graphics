using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class CustomRenderer : ScriptableRenderer
    {
        private DrawObjectsPass m_RenderOpaqueForwardPass;

        ForwardLights m_ForwardLights;

        public CustomRenderer(CustomRenderGraphData data) : base(data)
        {
            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_ForwardLights = new ForwardLights();
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

            foreach (var feature in rendererFeatures)
            {
                feature.AddRenderPasses(this, ref renderingData);
                feature.SetupRenderPasses(this, in renderingData);
            }
            EnqueuePass(m_RenderOpaqueForwardPass);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);
        }
    }
}
