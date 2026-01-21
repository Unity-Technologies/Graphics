using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given depth buffer into the given destination depth buffer.
    ///
    /// You can use this pass to copy a depth buffer to a destination,
    /// so you can use it later in rendering. If the source texture has MSAA
    /// enabled, the pass uses a custom MSAA resolve. If the source texture
    /// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
    /// operation, depending on what the current platform supports.
    /// </summary>
    public partial class CopyDepthPass : ScriptableRenderPass
    {
        // In XR CopyDepth, we need a special workaround to handle dummy color issue in RenderGraph.
        internal bool CopyToDepthXR { get; set; }
        Material m_CopyDepthMaterial;

        /// <summary>
        /// Shader resource ids used to communicate with the shader implementation
        /// </summary>
        static class ShaderConstants
        {
            public static readonly int _CameraDepthAttachment = Shader.PropertyToID("_CameraDepthAttachment");
            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _ZWriteShaderHandle = Shader.PropertyToID("_ZWrite");
        }

        /// <summary>
        /// Creates a new <c>CopyDepthPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="copyDepthShader">The <c>Shader</c> to use for copying the depth.</param>
        /// <param name="shouldClear">Controls whether it should do a clear before copying the depth.</param>
        /// <param name="copyToDepth">Deprecated, the parameter is ignored. This is now automatically derived from the source and destination TextureHandle.</param>
        /// <param name="copyResolvedDepth">Deprecated, the parameter is ignored. This is now automatically derived from the source and destination TextureHandle.</param>
        /// <param name="customPassName">An optional custom profiling name to disambiguate multiple copy passes.</param>
        /// <seealso cref="RenderPassEvent"/>
        public CopyDepthPass(RenderPassEvent evt, Shader copyDepthShader, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false, string customPassName = null)
        {
            profilingSampler = customPassName != null ? new ProfilingSampler(customPassName) : ProfilingSampler.Get(URPProfileId.CopyDepth);
            m_CopyDepthMaterial = copyDepthShader != null ? CoreUtils.CreateEngineMaterial(copyDepthShader) : null;
            renderPassEvent = evt;
            CopyToDepthXR = false;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination)
        {

        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            CoreUtils.Destroy(m_CopyDepthMaterial);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal UniversalCameraData cameraData;
            internal Material copyDepthMaterial;
            internal bool copyResolvedDepth;
            internal bool copyToDepth;
            internal bool setViewport;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RTHandle source, Vector4 scaleBias)
        {
            var copyDepthMaterial = passData.copyDepthMaterial;

            if (copyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. Copy Depth render pass will not execute. Check for missing reference in the renderer resources.", copyDepthMaterial);
                return;
            }

            int cameraSamples;

            if (passData.copyResolvedDepth)
            {
                cameraSamples = 1;
            }
            else
            {
                cameraSamples = source.rt.antiAliasing;
            }

            switch (cameraSamples)
            {
                case 8:
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa2, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa4, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa8, true);
                    break;

                case 4:
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa2, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa4, true);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa8, false);
                    break;

                case 2:
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa2, true);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa4, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa8, false);
                    break;

                // MSAA disabled, auto resolve supported, resolve texture requested, or ms textures not supported
                default:
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa2, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa4, false);
                    cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa8, false);
                    break;
            }

            cmd.SetKeyword(ShaderGlobalKeywords._OUTPUT_DEPTH, passData.copyToDepth);

            // When we render to the backbuffer, we update the viewport to cover the entire screen just in case it hasn't been updated already.
            if (passData.setViewport)
                cmd.SetViewport(passData.cameraData.pixelRect);

            copyDepthMaterial.SetTexture(ShaderConstants._CameraDepthAttachment, source);
            copyDepthMaterial.SetFloat(ShaderConstants._ZWriteShaderHandle, passData.copyToDepth ? 1.0f : 0.0f);
            Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, 0);
        }

        /// <summary>
        /// Sets up the Copy Depth pass for RenderGraph execution
        /// </summary>
        /// <param name="renderGraph">The current RenderGraph used for recording and execution of a frame.</param>
        /// <param name="frameData">The renderer settings containing rendering data of the current frame.</param>
        /// <param name="destination"><c>TextureHandle</c> of the destination it will copy to.</param>
        /// <param name="source"><c>TextureHandle</c> of the source it will copy from.</param>
        /// <param name="bindAsCameraDepth">If this is true, the destination texture is bound as _CameraDepthTexture after the copy pass</param>
        /// <param name="passName">The pass name used for debug and identifying the pass.</param>
        public void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle destination, TextureHandle source, bool bindAsCameraDepth = false, string passName = "Copy Depth")
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            Render(renderGraph, destination, source, resourceData, cameraData, bindAsCameraDepth, passName);
        }

        /// <summary>
        /// Sets up the Copy Depth pass for RenderGraph execution
        /// </summary>
        /// <param name="renderGraph">The current RenderGraph used for recording and execution of a frame.</param>
        /// <param name="destination"><c>TextureHandle</c> of the destination it will copy to.</param>
        /// <param name="source"><c>TextureHandle</c> of the source it will copy from.</param>
        /// <param name="resourceData">URP texture handles for the current frame.</param>
        /// <param name="cameraData">Camera settings for the current frame.</param>
        /// <param name="bindAsCameraDepth">If this is true, the destination texture is bound as _CameraDepthTexture after the copy pass</param>
        /// <param name="passName">The pass name used for debug and identifying the pass.</param>
        public void Render(RenderGraph renderGraph, TextureHandle destination, TextureHandle source, UniversalResourceData resourceData, UniversalCameraData cameraData, bool bindAsCameraDepth = false, string passName = "Copy Depth")
        {
            Debug.Assert(source.IsValid(), "CopyDepthPass source is not a valid texture.");
            Debug.Assert(destination.IsValid(), "CopyDepthPass destination is not a valid texture.");

            var sourceDesc = renderGraph.GetTextureDesc(source);
            var destinationDesc = renderGraph.GetRenderTargetInfo(destination);

            bool dstHasDepthFormat = GraphicsFormatUtility.IsDepthFormat(destinationDesc.format);

            bool hasMSAA = sourceDesc.msaaSamples != MSAASamples.None;
            var canUseResolvedDepth = !sourceDesc.bindTextureMS && RenderingUtils.MultisampleDepthResolveSupported();
            var canSampleMSAADepth = sourceDesc.bindTextureMS && SystemInfo.supportsMultisampledTextures != 0;

            Debug.Assert(!hasMSAA || canUseResolvedDepth || canSampleMSAADepth || !dstHasDepthFormat
                , "Can't copy depth to destination with depth format due to MSAA and platform/API limitations: no resolved depth resource (bindMS), depth resolve unsupported, and MSAA depth sampling unsupported.");

            // Having a different pass name than profilingSampler.name is bad practice but this method was public before we cleaned up this naming
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.copyDepthMaterial = m_CopyDepthMaterial;
                passData.cameraData = cameraData;
                //When we can't resolve depth and can't sample MSAA depth, we should have set the target to color. This works on GLES for example.
                //Perhaps we need to check for !dstHasDepthFormat but we keep to original check to avoid any issues for now.
                passData.copyResolvedDepth = canUseResolvedDepth || !canSampleMSAADepth;
                passData.copyToDepth = dstHasDepthFormat;
                passData.setViewport = CopyToDepthXR;

                if (cameraData.xr.enabled)
                {
                    // Apply MultiviewRenderRegionsCompatible flag only to the peripheral view in Quad Views
                    if (cameraData.xr.multipassId == 0)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
                }

                if (CopyToDepthXR)
                {
                    // Writes depth using custom depth output
                    builder.SetRenderAttachmentDepth(destination, AccessFlags.WriteAll);

#if ENABLE_VR && ENABLE_XR_MODULE
                    // binding a dummy color target as a workaround to NRP depth only rendering limitation:
                    // "Attempting to render to a depth only surface with no dummy color attachment"
                    if (cameraData.xr.enabled && cameraData.xr.copyDepth)
                    {
                        RenderTargetInfo backBufferDesc = renderGraph.GetRenderTargetInfo(resourceData.backBufferColor);
                        // In the case where MSAA is enabled, we have to bind a different dummy texture
                        // This is to ensure that we don't render black in the resolve result of the color backbuffer
                        // This also makes this pass unmergeable in this case, potentially impacting performance
                        if (backBufferDesc.msaaSamples > 1)
                        {
                            TextureHandle dummyXRRenderTarget = renderGraph.CreateTexture(new TextureDesc(backBufferDesc.width, backBufferDesc.height, false, true)
                            {
                                name = "XR Copy Depth Dummy Render Target",
                                slices = backBufferDesc.volumeDepth,
                                format = backBufferDesc.format,
                                msaaSamples = (MSAASamples)backBufferDesc.msaaSamples,
                                clearBuffer = false,
                                bindTextureMS = backBufferDesc.bindMS
                            });
                            builder.SetRenderAttachment(dummyXRRenderTarget, 0);
                        }
                        else
                            builder.SetRenderAttachment(resourceData.backBufferColor, 0);
                    }
#endif
                }
                else if (passData.copyToDepth)
                {
                    // Writes depth using custom depth output
                    builder.SetRenderAttachmentDepth(destination, AccessFlags.WriteAll);
#if UNITY_EDITOR
                    // binding a dummy color target as a workaround to an OSX issue in Editor scene view (UUM-47698).
                    // Also required for preview camera rendering for grid drawn with builtin RP (UUM-55171).
                    // Also required for render gizmos (UUM-91335).
                    // When MSAA is enabled with Dbuffer can cause sample count mismatches between active color and depth target; create a dummy color target to resolve. (UUM-131330)
                    if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera || UnityEditor.Handles.ShouldRenderGizmos())
                    {
                        // get info for the active color
                        var activeColorInfo = renderGraph.GetRenderTargetInfo(resourceData.activeColorTexture);

                        // destination depth info (the texture we created earlier)
                        var destInfo = renderGraph.GetRenderTargetInfo(destination);

                        // if samples mismatch, create a dummy color RT with dest's samples
                        if (activeColorInfo.msaaSamples != destInfo.msaaSamples)
                        {
                            TextureHandle dummyColor = renderGraph.CreateTexture(new TextureDesc(activeColorInfo.width, activeColorInfo.height, false, true)
                            {
                                name = "Copy Depth Editor Dummy Color",
                                slices = activeColorInfo.volumeDepth,
                                format = activeColorInfo.format,
                                msaaSamples = (MSAASamples)destInfo.msaaSamples, // match the depth target
                                clearBuffer = false,
                                bindTextureMS = activeColorInfo.bindMS
                            });
                            builder.SetRenderAttachment(dummyColor, 0);
                        }
                        else
                        {
                            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                        }
                    }
#endif
                }
                else
                {
                    // Writes depth as "grayscale color" output
                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                }

                passData.source = source;
                passData.destination = destination;
                builder.UseTexture(source, AccessFlags.Read);

                if (bindAsCameraDepth)
                    builder.SetGlobalTextureAfterPass(destination, ShaderConstants._CameraDepthTexture);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(context, data.source, data.destination);
                    ExecutePass(context.cmd, data, data.source, scaleBias);
                });
            }
        }
    }
}
