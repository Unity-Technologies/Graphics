using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        bool DebugHandlerRequireDepthPass(UniversalCameraData cameraData)
        {
            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out _))
                    return true;
            }

            return false;
        }

        void CreateDebugTexture(RenderTextureDescriptor descriptor)
        {
            var debugTexDescriptor = descriptor;
            debugTexDescriptor.useMipMap = false;
            debugTexDescriptor.autoGenerateMips = false;
            debugTexDescriptor.bindMS = false;
            debugTexDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_RenderGraphDebugTextureHandle, debugTexDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_RenderingDebuggerTexture");
        }

        private Rect CalculateUVRect(UniversalCameraData cameraData, float width, float height)
        {
            float normalizedSizeX = width / cameraData.pixelWidth;
            float normalizedSizeY = height / cameraData.pixelHeight;

            return new Rect(1 - normalizedSizeX, 1 - normalizedSizeY, normalizedSizeX, normalizedSizeY);
        }

        private Rect CalculateUVRect(UniversalCameraData cameraData, int textureHeightPercent)
        {
            var relativeSize = Mathf.Clamp01(textureHeightPercent / 100f);
            var width = relativeSize * cameraData.pixelWidth;
            var height = relativeSize * cameraData.pixelHeight;

            return CalculateUVRect(cameraData, width, height);
        }

        private void SetupRenderGraphFinalPassDebug(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode, out int textureHeightPercent) &&
                    (fullScreenDebugMode != DebugFullScreenMode.ReflectionProbeAtlas || m_Clustering) &&
                    (fullScreenDebugMode != DebugFullScreenMode.STP))
                {
                    float screenWidth = cameraData.pixelWidth;
                    float screenHeight = cameraData.pixelHeight;

                    var relativeSize = Mathf.Clamp01(textureHeightPercent / 100f);
                    var height = relativeSize * screenHeight;
                    var width = relativeSize * screenWidth;

                    bool supportsStereo = false;

                    if (fullScreenDebugMode == DebugFullScreenMode.ReflectionProbeAtlas)
                    {
                        // Ensure that atlas is not stretched, but doesn't take up more than the percentage in any dimension.
                        var texture = m_ForwardLights.reflectionProbeManager.atlasRT;
                        var targetWidth = height * texture.width / texture.height;
                        if (targetWidth > width)
                        {
                            height = width * texture.height / texture.width;
                        }
                        else
                        {
                            width = targetWidth;
                        }

                        m_RenderGraphDebugTextureHandle = RTHandles.Alloc(m_ForwardLights.reflectionProbeManager.atlasRT, transferOwnership: true);
                    }
                    else // visualize RG internal resources
                    {
                        // if we want to visualize RG internal resources, we need to create an RTHandle external to RG and copy to it the textures to visualize
                        // this is required because the lifetime of these resources is limited to the RenderGraph execution, and we cannot access the actual resources here

                        CreateDebugTexture(cameraData.cameraTargetDescriptor);

                        ImportResourceParams importParams = new ImportResourceParams();
                        importParams.clearOnFirstUse = false;
                        importParams.discardOnLastUse = false;
                        TextureHandle debugDepthTexture = renderGraph.ImportTexture(m_RenderGraphDebugTextureHandle, importParams);

                        switch (fullScreenDebugMode)
                        {
                            case DebugFullScreenMode.Depth:
                            {
                                CopyToDebugTexture(renderGraph, resourceData.cameraDepthTexture, debugDepthTexture);
                                supportsStereo = true;

                                break;
                            }
                            case DebugFullScreenMode.AdditionalLightsShadowMap:
                            {
                                CopyToDebugTexture(renderGraph, resourceData.additionalShadowsTexture, debugDepthTexture);

                                break;
                            }
                            case DebugFullScreenMode.MainLightShadowMap:
                            {
                                CopyToDebugTexture(renderGraph, resourceData.mainShadowsTexture, debugDepthTexture);

                                break;
                            }
                            default:
                            {
                                break;
                            }
                        }
                    }

                    Rect uvRect = CalculateUVRect(cameraData, width, height);
                    DebugHandler.SetDebugRenderTarget(m_RenderGraphDebugTextureHandle, uvRect, supportsStereo);
                }
                else
                {
                    DebugHandler.ResetDebugRenderTarget();
                }
            }

            if (DebugHandler != null)
            {
                if (!DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode, out int textureHeightPercent))
                {
                    var debugSettings = DebugHandler.DebugDisplaySettings.gpuResidentDrawerSettings;

                    GPUResidentDrawer.RenderDebugOcclusionTestOverlay(renderGraph, debugSettings, cameraData.camera.GetInstanceID(), resourceData.activeColorTexture);

                    float screenWidth = (int)(cameraData.pixelHeight * cameraData.renderScale);
                    float screenHeight = (int)(cameraData.pixelHeight * cameraData.renderScale);
                    float maxHeight = screenHeight * textureHeightPercent / 100.0f;
                    GPUResidentDrawer.RenderDebugOccluderOverlay(renderGraph, debugSettings, new Vector2(0.25f * screenWidth, screenHeight - 1.5f * maxHeight), maxHeight, resourceData.activeColorTexture);
                }
            }
        }

        private void SetupAfterPostRenderGraphFinalPassDebug(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out var debugFullscreenMode, out int textureHeightPercent) &&
                    (debugFullscreenMode == DebugFullScreenMode.STP))
                {
                    CreateDebugTexture(cameraData.cameraTargetDescriptor);

                    ImportResourceParams importParams = new ImportResourceParams();
                    importParams.clearOnFirstUse = false;
                    importParams.discardOnLastUse = false;
                    TextureHandle debugTexture = renderGraph.ImportTexture(m_RenderGraphDebugTextureHandle, importParams);

                    CopyToDebugTexture(renderGraph, resourceData.stpDebugView, debugTexture);

                    Rect uvRect = CalculateUVRect(cameraData, textureHeightPercent);

                    DebugHandler.SetDebugRenderTarget(m_RenderGraphDebugTextureHandle, uvRect, true);
                }
            }
        }

        class CopyToDebugTexturePassData
        {
            internal TextureHandle src;
            internal TextureHandle dest;
        }


        private void CopyToDebugTexture(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, string passName = "Copy To Debug Texture")
        {
            using (var builder = renderGraph.AddRasterRenderPass<CopyToDebugTexturePassData>(passName, out var passData))
            {
                if (source.IsValid())
                {
                    passData.src = source;
                    builder.UseTexture(source);
                }
                else
                {
                    passData.src = renderGraph.defaultResources.blackTexture;
                }

                passData.dest = destination;
                builder.SetRenderAttachment(destination, 0);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyToDebugTexturePassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1,1,0,0), 0, false);
                });
            }
        }

    }
}
