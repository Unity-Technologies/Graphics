using UnityEngine.Experimental.Rendering.RenderGraphModule;
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

            m_ForwardLights = new ForwardLights();

            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            base.Dispose(disposing);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ConfigureCameraTarget(k_CameraTarget, k_CameraTarget);

            foreach (var feature in rendererFeatures)
            {
                feature.AddRenderPasses(this, ref renderingData);
                feature.SetupRenderPasses(this, in renderingData);
            }
            EnqueuePass(m_RenderOpaqueForwardPass);

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);
            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);
        }


        static ProfilingSampler s_SetupLights = new ProfilingSampler("Setup URP lights.");
        private class SetupLightPassData
        {
            internal RenderingData renderingData;
            internal ForwardLights forwardLights;
        };
        private void SetupRenderGraphLights(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            m_ForwardLights.SetupRenderGraphLights(renderGraph, ref renderingData);
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.PreSetup(ref renderingData);
            SetupRenderGraphLights(renderGraph, ref renderingData);

            TextureHandle mainShadowsTexture = TextureHandle.nullHandle;
            TextureHandle additionalShadowsTexture = TextureHandle.nullHandle;

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
                mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, ref renderingData);
            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
                additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, ref renderingData);

            SetupRenderGraphCameraProperties(renderGraph, ref renderingData, true);

            var targetHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
            var depthHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);

            ClearTargetsPass.Render(renderGraph, targetHandle, depthHandle, renderingData.cameraData);

            m_RenderOpaqueForwardPass.Render(renderGraph, targetHandle, depthHandle, mainShadowsTexture, additionalShadowsTexture, ref renderingData);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.SetupLights(renderingData.commandBuffer, ref renderingData);
        }
    }
}
