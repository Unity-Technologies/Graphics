using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Wires the core AlphaUpscaler into URP: it runs right after a color-only upscaler
    /// and rebuilds the output alpha from the pre-upscale alpha, so the
    /// alpha channel survives upscalers that only reconstruct the color channels.
    /// </summary>
    internal static class AlphaUpscaleUtils
    {
        internal static void Execute(RenderGraph renderGraph, UniversalCameraData cameraData, UniversalResourceData resourceData,
            TextureHandle preUpscaleColor, bool isTemporalUpscaler, Vector2Int preUpscaleSize, Vector2Int postUpscaleSize, Vector2 jitter,
            TextureHandle reactiveMask = default, bool reactiveMaskIsStencil = false)
        {
            if (!cameraData.isAlphaOutputEnabled)
                return;

            // The upscaler may have skipped this frame (e.g. unmet runtime requirements).
            var upscaledColor = resourceData.cameraColor;
            if (!upscaledColor.IsValid() || upscaledColor == preUpscaleColor)
                return;

            AlphaUpscaler.Config config = default;
            config.reactiveMask = reactiveMask;
            config.stencilExcludeBit = reactiveMaskIsStencil ? 0x1 : 0;
            config.preUpscaleAlpha = preUpscaleColor;
            config.upscaledColor = upscaledColor;
            config.preUpscaleSize = preUpscaleSize;
            // URP allocates the camera color at exactly the rendered size (render scale included, and
            // hardware DRS scales the allocation itself), so preUpscalePhysicalSize keeps its default.
            config.postUpscaleSize = postUpscaleSize;
            config.jitter = jitter;
            config.enableTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
            config.viewCount = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;

            // Setup temporal inputs.
            var history = cameraData.alphaUpscaleHistory;
            if (isTemporalUpscaler && history != null &&
                resourceData.motionVectorColor.IsValid() && resourceData.cameraDepthTexture.IsValid())
            {
                int eyeIndex = (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled) ? cameraData.xr.multipassId : 0;
                RTHandle prevAlphaHistory = history.GetPreviousTexture(eyeIndex);
                RTHandle nextAlphaHistory = history.GetCurrentTexture(eyeIndex);

                if (prevAlphaHistory != null && nextAlphaHistory != null)
                {
                    config.motionVectors = resourceData.motionVectorColor;
                    config.cameraDepth = resourceData.cameraDepthTexture;
                    config.prevAlphaHistory = renderGraph.ImportTexture(prevAlphaHistory);
                    config.nextAlphaHistory = renderGraph.ImportTexture(nextAlphaHistory);
                    config.historyBlendFactor = (history.validHistory && !cameraData.resetHistory)
                        ? AlphaUpscaler.defaultHistoryBlendFactor : 0.0f;
                }
            }

            resourceData.cameraColor = AlphaUpscaler.Execute(renderGraph, ref config);
        }
    }
}
