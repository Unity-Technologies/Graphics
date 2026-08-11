using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class ShadowPassGroup
    {
        internal bool isRenderingShadows => isMainLightShadowCasting || isAdditionalLightShadowCasting;

        internal bool isMainLightShadowCasting { get; private set; }
        internal TextureHandle mainLightShadowmapTexture { get; private set; }
        internal MainLightShadowCasterPass mainLightShadowmapPass { get; }

        internal bool isAdditionalLightShadowCasting { get; private set; }
        internal TextureHandle additionalLightShadowmapTexture { get; private set; }
        internal AdditionalLightsShadowCasterPass additionalLightShadowmapPass { get; }

        internal ShadowPassGroup()
        {
            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            mainLightShadowmapPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            additionalLightShadowmapPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        internal void RenderMainLightShadows(RenderGraph renderGraph, ContextContainer frameData, bool allowCachedShadowMap = true, bool shadowmapStencil = false)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            mainLightShadowmapTexture = TextureHandle.nullHandle;
            isMainLightShadowCasting = mainLightShadowmapPass.Setup(renderingData, cameraData, lightData, shadowData, shadowmapStencil);

            if (isMainLightShadowCasting)
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (allowCachedShadowMap && shadowData.useCachedShadowMap)
                    mainLightShadowmapTexture = mainLightShadowmapPass.FetchShadowMapAndSetParameters(renderGraph, frameData);
                else
#endif
                    mainLightShadowmapTexture = mainLightShadowmapPass.Render(renderGraph, frameData);
            }
        }

        internal void RenderAdditionalLightShadows(RenderGraph renderGraph, ContextContainer frameData, bool allowCachedShadowMap = true, bool shadowmapStencil = false)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            additionalLightShadowmapTexture = TextureHandle.nullHandle;
            isAdditionalLightShadowCasting = additionalLightShadowmapPass.Setup(renderingData, cameraData, lightData, shadowData, shadowmapStencil);

            if (isAdditionalLightShadowCasting)
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                if (allowCachedShadowMap && shadowData.useCachedShadowMap)
                    additionalLightShadowmapTexture = additionalLightShadowmapPass.FetchShadowMapAndSetParameters(renderGraph, frameData);
                else
#endif
                    additionalLightShadowmapTexture = additionalLightShadowmapPass.Render(renderGraph, frameData);
            }
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
