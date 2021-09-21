using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class CustomRenderer : ScriptableRenderer
    {
        private DrawObjectsPass m_RenderOpaqueForwardPass;

        ForwardLights m_ForwardLights;
        FakeMainLightShadowCasterPass m_FakeMainLightShadowCasterPass;
        FakeAdditionalLightsShadowCasterPass m_FakeAdditionalLightsShadowCasterPass;

        public CustomRenderer(CustomRenderGraphData data) : base(data)
        {
            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_ForwardLights = new ForwardLights();
            m_FakeMainLightShadowCasterPass = new FakeMainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_FakeAdditionalLightsShadowCasterPass = new FakeAdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            stripShadowsOffVariants = true;
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

            foreach (var feature in rendererFeatures)
                feature.AddRenderPasses(this, ref renderingData);
            EnqueuePass(m_RenderOpaqueForwardPass);
            EnqueuePass(m_FakeMainLightShadowCasterPass);
            EnqueuePass(m_FakeAdditionalLightsShadowCasterPass);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);
        }
    }
}
