using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        bool DebugHandlerRequireDepthPass(UniversalCameraData cameraData)
        {
            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode))
                    return true;
            }

            return false;
        }

        void CreateDebugTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var debugTexDescriptor = descriptor;
            debugTexDescriptor.useMipMap = false;
            debugTexDescriptor.autoGenerateMips = false;
            debugTexDescriptor.bindMS = false;
            debugTexDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphDebugTextureHandle, debugTexDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_RenderingDebuggerTexture");
        }

        private void SetupRenderGraphFinalPassDebug(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
            {
                if (DebugHandler.TryGetFullscreenDebugMode(out DebugFullScreenMode fullScreenDebugMode, out int textureHeightPercent) &&
                    (fullScreenDebugMode != DebugFullScreenMode.ReflectionProbeAtlas || m_Clustering))
                {
                    Camera camera = cameraData.camera;
                    float screenWidth = camera.pixelWidth;
                    float screenHeight = camera.pixelHeight;

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

                        m_RenderGraphDebugTextureHandle = RTHandles.Alloc(m_ForwardLights.reflectionProbeManager.atlasRT);
                    }
                    else // visualize RG internal resources
                    {
                        // if we want to visualize RG internal resources, we need to create an RTHandle external to RG and copy to it the textures to visualize
                        // this is required because the lifetime of these resources is limited to the RenderGraph execution, and we cannot access the actual resources here

                        CreateDebugTexture(renderGraph, cameraData.cameraTargetDescriptor);

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

                    float normalizedSizeX = width / screenWidth;
                    float normalizedSizeY = height / screenHeight;

                    Rect normalizedRect = new Rect(1 - normalizedSizeX, 1 - normalizedSizeY, normalizedSizeX, normalizedSizeY);

                    DebugHandler.SetDebugRenderTarget(m_RenderGraphDebugTextureHandle.nameID, normalizedRect, supportsStereo);
                }
                else
                {
                    DebugHandler.ResetDebugRenderTarget();
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
                passData.src = source.IsValid() ? builder.UseTexture(source) : renderGraph.defaultResources.blackTexture;

                passData.dest = builder.UseTextureFragment(destination, 0);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyToDebugTexturePassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1,1,0,0), 0, false);
                });
            }
        }

    }
}
