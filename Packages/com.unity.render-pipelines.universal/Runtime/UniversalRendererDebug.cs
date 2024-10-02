using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        Material m_DebugBlitMaterial = Blitter.GetBlitMaterial(TextureXR.dimension);

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
            debugTexDescriptor.depthStencilFormat = GraphicsFormat.None;

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

        private void CorrectForTextureAspectRatio(ref float width, ref float height, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth != 0 && sourceHeight != 0)
            {
                // Ensure that atlas is not stretched, but doesn't take up more than the percentage in any dimension.
                var targetWidth = height * sourceWidth / sourceHeight;
                if (targetWidth > width)
                {
                    height = width * sourceHeight / sourceWidth;
                }
                else
                {
                    width = targetWidth;
                }
            }
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
                    Vector4 dataRangeRemap = Vector4.zero; // zero = off, .x = old min, .y = old max, .z = new min, .w = new max

                    // visualize RG internal resources
                    {
                        // if we want to visualize RG internal resources, we need to create an RTHandle external to RG and copy to it the textures to visualize
                        // this is required because the lifetime of these resources is limited to the RenderGraph execution, and we cannot access the actual resources here

                        // we also copy external resources to make them "read only". CreateDebugTexture() can lead to (external) texture reallocation.

                        var debugDescriptor = cameraData.cameraTargetDescriptor;
                        // Ensure target can hold all source values. Source can be signed for example.
                        if(SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Linear | GraphicsFormatUsage.Render))
                            debugDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;

                        CreateDebugTexture(debugDescriptor);

                        ImportResourceParams importParams = new ImportResourceParams();
                        importParams.clearOnFirstUse = false;
                        importParams.discardOnLastUse = false;
                        TextureHandle debugTexture = renderGraph.ImportTexture(m_RenderGraphDebugTextureHandle, importParams);


                        switch (fullScreenDebugMode)
                        {
                            case DebugFullScreenMode.Depth:
                            {
                                BlitToDebugTexture(renderGraph, resourceData.cameraDepthTexture, debugTexture);
                                supportsStereo = true;
                                break;
                            }
                            case DebugFullScreenMode.MotionVector:
                            {
                                BlitToDebugTexture(renderGraph, resourceData.motionVectorColor, debugTexture, isSourceTextureColor: true);
                                supportsStereo = true;
                                // Motion vectors are in signed UV space, zoom in and normalize for visualization. (note: maybe add an option to use (angle, mag) visualization)
                                const float zoom = 0.01f;
                                dataRangeRemap.x = -zoom;
                                dataRangeRemap.y = zoom;
                                dataRangeRemap.z = 0;
                                dataRangeRemap.w = 1.0f;
                                break;
                            }
                            case DebugFullScreenMode.AdditionalLightsShadowMap:
                            {
                                BlitToDebugTexture(renderGraph, resourceData.additionalShadowsTexture, debugTexture);
                                break;
                            }
                            case DebugFullScreenMode.MainLightShadowMap:
                            {
                                BlitToDebugTexture(renderGraph, resourceData.mainShadowsTexture, debugTexture);
                                break;
                            }
                            case DebugFullScreenMode.AdditionalLightsCookieAtlas:
                            {
                                // Copy atlas texture to make it "readonly". Direct reference (debug=atlas) can lead to handle->texture reallocation.
                                var textureHandle =
                                    m_LightCookieManager is { AdditionalLightsCookieAtlasTexture: not null }
                                        ? renderGraph.ImportTexture(m_LightCookieManager
                                            .AdditionalLightsCookieAtlasTexture)
                                        : TextureHandle.nullHandle;

                                BlitToDebugTexture(renderGraph, textureHandle, debugTexture);
                                break;
                            }

                            case DebugFullScreenMode.ReflectionProbeAtlas:
                            {
                                // Copy atlas texture to make it "readonly". Direct reference (debug=atlas) can lead to handle->texture reallocation.
                                var textureHandle =
                                    m_ForwardLights.reflectionProbeManager.atlasRT != null
                                        ? renderGraph.ImportTexture(RTHandles.Alloc(
                                            m_ForwardLights.reflectionProbeManager.atlasRT, transferOwnership: true))
                                        : TextureHandle.nullHandle;

                                BlitToDebugTexture(renderGraph, textureHandle, debugTexture);
                                break;
                            }
                            default:
                            {
                                break;
                            }
                        }
                    }

                    // Textures that are not in screen aspect ratio need to be corrected
                    {
                        RenderTexture source = null;
                        switch (fullScreenDebugMode)
                        {
                            case DebugFullScreenMode.AdditionalLightsShadowMap: source = m_AdditionalLightsShadowCasterPass?.m_AdditionalLightsShadowmapHandle?.rt; break;
                            case DebugFullScreenMode.MainLightShadowMap: source = m_MainLightShadowCasterPass?.m_MainLightShadowmapTexture?.rt; break;
                            case DebugFullScreenMode.AdditionalLightsCookieAtlas: source = m_LightCookieManager?.AdditionalLightsCookieAtlasTexture?.rt; break;
                            case DebugFullScreenMode.ReflectionProbeAtlas: source = m_ForwardLights?.reflectionProbeManager.atlasRT; break;
                            default:
                                break;
                        }

                        // Ensure that atlas is not stretched, but doesn't take up more than the percentage in any dimension.
                        if (source != null)
                            CorrectForTextureAspectRatio(ref width, ref height, source.width, source.height);
                    }


                    Rect uvRect = CalculateUVRect(cameraData, width, height);
                    DebugHandler.SetDebugRenderTarget(m_RenderGraphDebugTextureHandle, uvRect, supportsStereo, dataRangeRemap);
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
                    BlitToDebugTexture(renderGraph, resourceData.stpDebugView, debugTexture);

                    Rect uvRect = CalculateUVRect(cameraData, textureHeightPercent);
                    Vector4 rangeRemap = Vector4.zero; // Off
                    DebugHandler.SetDebugRenderTarget(m_RenderGraphDebugTextureHandle, uvRect, true, rangeRemap);
                }
            }
        }

        class CopyToDebugTexturePassData
        {
            internal TextureHandle src;
            internal TextureHandle dest;
        }

        private void BlitToDebugTexture(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, bool isSourceTextureColor = false)
        {
            if (source.IsValid())
            {
                if (isSourceTextureColor) // Use AddCopyPass (RasterRenderPass) when we can
                {
                    renderGraph.AddCopyPass(source, destination);
                }
                else // Use direct blit (UnsafePass)
                {
                    var blitMaterialParameters =
                        new RenderGraphModule.Util.RenderGraphUtils.BlitMaterialParameters(
                            source, destination,
                            m_DebugBlitMaterial, 0);

                    renderGraph.AddBlitPass(blitMaterialParameters);
                }
            }
            else // Texture is invalid, just show a black view
            {
                BlitEmptyTexture(renderGraph, destination);
            }
        }

        private void BlitEmptyTexture(RenderGraph renderGraph, TextureHandle destination, string passName = "Copy To Debug Texture")
        {
            using (var builder = renderGraph.AddRasterRenderPass<CopyToDebugTexturePassData>(passName, out var passData))
            {
                passData.src = renderGraph.defaultResources.blackTexture;
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
