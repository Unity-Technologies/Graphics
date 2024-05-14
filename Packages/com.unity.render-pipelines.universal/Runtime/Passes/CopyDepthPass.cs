using System;
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
    public class CopyDepthPass : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }
        internal int MssaSamples { get; set; }
        // In some cases (Scene view, XR and etc.) we actually want to output to depth buffer
        // So this variable needs to be set to true to enable the correct copy shader semantic
        internal bool CopyToDepth { get; set; }
        Material m_CopyDepthMaterial;

        internal bool m_CopyResolvedDepth;
        internal bool m_ShouldClear;
        private PassData m_PassData;

        /// <summary>
        /// Creates a new <c>CopyDepthPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="copyDepthShader">The <c>Shader</c> to use for copying the depth.</param>
        /// <param name="shouldClear">Controls whether it should do a clear before copying the depth.</param>
        /// <param name="copyToDepth">Controls whether it should do a copy to a depth format target.</param>
        /// <param name="copyResolvedDepth">Set to true if the source depth is MSAA resolved.</param>
        /// <param name="customPassName">An optional custom profiling name to disambiguate multiple copy passes.</param>
        /// <seealso cref="RenderPassEvent"/>
        public CopyDepthPass(RenderPassEvent evt, Shader copyDepthShader, bool shouldClear = false, bool copyToDepth = false, bool copyResolvedDepth = false, string customPassName = null)
        {
            base.profilingSampler = customPassName != null ? new ProfilingSampler(customPassName) : new ProfilingSampler(nameof(CopyDepthPass));
            m_PassData = new PassData();
            CopyToDepth = copyToDepth;
            m_CopyDepthMaterial = copyDepthShader != null ? CoreUtils.CreateEngineMaterial(copyDepthShader) : null;
            renderPassEvent = evt;
            m_CopyResolvedDepth = copyResolvedDepth;
            m_ShouldClear = shouldClear;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
            this.MssaSamples = -1;
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            CoreUtils.Destroy(m_CopyDepthMaterial);
        }

        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
#if UNITY_EDITOR
            // This is a temporary workaround for Editor as not setting any depth here
            // would lead to overwriting depth in certain scenarios (reproducable while running DX11 tests)
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
                ConfigureTarget(destination, destination);
            else
#endif
            ConfigureTarget(destination);

            if (m_ShouldClear)
                ConfigureClear(ClearFlag.All, Color.black);

            #pragma warning restore CS0618
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal UniversalCameraData cameraData;
            internal Material copyDepthMaterial;
            internal int msaaSamples;
            internal bool copyResolvedDepth;
            internal bool copyToDepth;
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.frameData.Get<UniversalCameraData>();

            m_PassData.copyDepthMaterial = m_CopyDepthMaterial;
            m_PassData.msaaSamples = MssaSamples;
            m_PassData.copyResolvedDepth = m_CopyResolvedDepth;
            m_PassData.copyToDepth = CopyToDepth;
            m_PassData.cameraData = cameraData;
            var cmd = renderingData.commandBuffer;
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.nameID);
#if ENABLE_VR && ENABLE_XR_MODULE
            if (m_PassData.cameraData.xr.enabled)
            {
                if (m_PassData.cameraData.xr.supportsFoveatedRendering)
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
            }
#endif
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), m_PassData, this.source, this.destination);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RTHandle source, RTHandle destination)
        {
            var copyDepthMaterial = passData.copyDepthMaterial;
            var msaaSamples = passData.msaaSamples;
            var copyResolvedDepth = passData.copyResolvedDepth;
            var copyToDepth = passData.copyToDepth;

            if (copyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. Copy Depth render pass will not execute. Check for missing reference in the renderer resources.", copyDepthMaterial);
                return;
            }
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyDepth)))
            {
                int cameraSamples = 0;
                if (msaaSamples == -1)
                {
                    RTHandle sourceTex = source;
                    cameraSamples = sourceTex.rt.antiAliasing;
                }
                else
                    cameraSamples = msaaSamples;

                // When depth resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0 || copyResolvedDepth)
                    cameraSamples = 1;

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

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa2, false);
                        cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa4, false);
                        cmd.SetKeyword(ShaderGlobalKeywords.DepthMsaa8, false);
                        break;
                }

                if (copyToDepth || destination.rt.graphicsFormat == GraphicsFormat.None)
                    cmd.SetKeyword(ShaderGlobalKeywords._OUTPUT_DEPTH, true);
                else
                    cmd.SetKeyword(ShaderGlobalKeywords._OUTPUT_DEPTH, false);

                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                // We y-flip if
                // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
                // 2) renderTexture starts UV at top
                bool isGameViewFinalTarget = passData.cameraData.cameraType == CameraType.Game && destination.nameID == BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (passData.cameraData.xr.enabled)
                {
                    isGameViewFinalTarget |= new RenderTargetIdentifier(destination.nameID, 0, CubemapFace.Unknown, 0) == new RenderTargetIdentifier(passData.cameraData.xr.renderTarget, 0, CubemapFace.Unknown, 0);
                }
#endif
                bool yflip = passData.cameraData.IsHandleYFlipped(source) != passData.cameraData.IsHandleYFlipped(destination);
                Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);
                if (isGameViewFinalTarget)
                    cmd.SetViewport(passData.cameraData.pixelRect);

                copyDepthMaterial.SetTexture(Shader.PropertyToID("_CameraDepthAttachment"), source);
                Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, 0);
            }
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            destination = k_CameraTarget;
            #pragma warning restore CS0618
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
            Render(renderGraph, destination, source, resourceData, cameraData, bindAsCameraDepth);
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
            // TODO RENDERGRAPH: should call the equivalent of Setup() to initialise everything correctly
            MssaSamples = -1;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, base.profilingSampler))
            {
                passData.copyDepthMaterial = m_CopyDepthMaterial;
                passData.msaaSamples = MssaSamples;
                passData.cameraData = cameraData;
                passData.copyResolvedDepth = m_CopyResolvedDepth;
                passData.copyToDepth = CopyToDepth;

                if (CopyToDepth)
                {
                    // Writes depth using custom depth output
                    passData.destination = destination;
                    builder.SetRenderAttachmentDepth(destination, AccessFlags.Write);

#if UNITY_EDITOR
                    // binding a dummy color target as a workaround to an OSX issue in Editor scene view (UUM-47698).
                    // Also required for preview camera rendering for grid drawn with builtin RP (UUM-55171).
                    if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera)
                        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
#endif
                }
                else
                {
                    // Writes depth as "grayscale color" output
                    passData.destination = destination;
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                }

                passData.source = source;
                builder.UseTexture(source, AccessFlags.Read);

                if (bindAsCameraDepth && destination.IsValid())
                    builder.SetGlobalTextureAfterPass(destination, Shader.PropertyToID("_CameraDepthTexture"));

                // TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.source, data.destination);
                });
            }
        }
    }
}
