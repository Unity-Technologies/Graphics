using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class ShadowPassGroup
    {
        internal MainLightShadowCasterPass mainLightShadowmapPass { get; }
        internal AdditionalLightsShadowCasterPass additionalLightShadowmapPass { get; }

        internal ShadowPassGroup()
        {
            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            mainLightShadowmapPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            additionalLightShadowmapPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        // The passes publish their shadowmap through UniversalResourceData themselves, and record a render pass only
        // when they render or reuse a shadowmap - the empty case is handled by the renderer's frame init pass.
        // The returned flag only tells whether real shadow geometry gets rendered, which invalidates camera properties.
        internal bool RenderMainLightShadows(RenderGraph renderGraph, ContextContainer frameData, bool shadowmapStencil = false, bool shadowsEnabledByCamera = true)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            bool willRenderShadowGeometry = mainLightShadowmapPass.Setup(renderingData, cameraData, lightData, shadowData, shadowmapStencil, shadowsEnabledByCamera);
            mainLightShadowmapPass.RecordRenderGraph(renderGraph, frameData);

            return willRenderShadowGeometry;
        }

        internal bool RenderAdditionalLightShadows(RenderGraph renderGraph, ContextContainer frameData, bool shadowmapStencil = false, bool shadowsEnabledByCamera = true)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            bool willRenderShadowGeometry = additionalLightShadowmapPass.Setup(renderingData, cameraData, lightData, shadowData, shadowmapStencil, shadowsEnabledByCamera);
            additionalLightShadowmapPass.RecordRenderGraph(renderGraph, frameData);

            return willRenderShadowGeometry;
        }

        internal void ReleaseRenderTargets()
        {
            mainLightShadowmapPass.Dispose();
            additionalLightShadowmapPass.ReleaseRenderTargets();
        }

        internal void Dispose()
        {
            mainLightShadowmapPass.Dispose();
            additionalLightShadowmapPass.Dispose();
        }
    }
}
