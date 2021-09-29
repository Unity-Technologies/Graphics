using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class CustomRenderer : ScriptableRenderer
    {
        DrawObjectsPass m_RenderOpaqueForwardPass;
        ForwardLights m_ForwardLights;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;

        public CustomRenderer(CustomRenderGraphData data) : base(data)
        {
            stripShadowsOffVariants = true;
            stripAdditionalLightOffVariants = true;

            ForwardLights.InitParams forwardInitParams = ForwardLights.InitParams.GetDefault();
            forwardInitParams.additionalLightsAlwaysEnabled = stripAdditionalLightOffVariants;
            m_ForwardLights = new ForwardLights(forwardInitParams);

            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

            foreach (var feature in rendererFeatures)
                feature.AddRenderPasses(this, ref renderingData);
            EnqueuePass(m_RenderOpaqueForwardPass);

            m_MainLightShadowCasterPass.SetupForEmptyRendering();
            m_AdditionalLightsShadowCasterPass.SetupForEmptyRendering();

            EnqueuePass(m_MainLightShadowCasterPass);
            EnqueuePass(m_AdditionalLightsShadowCasterPass);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);
        }
    }
}
