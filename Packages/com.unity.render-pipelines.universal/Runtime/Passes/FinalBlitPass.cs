using System;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    public class FinalBlitPass : ScriptableRenderPass
    {
        RTHandle m_Source;
        private PassData m_PassData;

        static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");

        // Use specialed URP fragment shader pass for debug draw support and color space conversion/encoding support.
        // See CoreBlit.shader and BlitHDROverlay.shader
        static class BlitPassNames
        {
            public const string NearestSampler = "NearestDebugDraw";
            public const string BilinearSampler = "BilinearDebugDraw";
        }

        enum BlitType
        {
            Core = 0, // Core blit
            HDR = 1, // Blit with HDR encoding and overlay UI compositing
            Count = 2
        }

        struct BlitMaterialData
        {
            public Material material;
            public int nearestSamplerPass;
            public int bilinearSamplerPass;
        }

        BlitMaterialData[] m_BlitMaterialData;

        /// <summary>
        /// Creates a new <c>FinalBlitPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="blitMaterial">The <c>Material</c> to use for copying the executing the final blit.</param>
        /// <param name="blitHDRMaterial">The <c>Material</c> to use for copying the executing the final blit when HDR output is active.</param>
        /// <seealso cref="RenderPassEvent"/>
        public FinalBlitPass(RenderPassEvent evt, Material blitMaterial, Material blitHDRMaterial)
        {
            profilingSampler = ProfilingSampler.Get(URPProfileId.BlitFinalToBackBuffer);
            base.useNativeRenderPass = false;
            m_PassData = new PassData();
            renderPassEvent = evt;

            // Find sampler passes by name
            const int blitTypeCount = (int)BlitType.Count;
            m_BlitMaterialData = new BlitMaterialData[blitTypeCount];
            for (int i = 0; i < blitTypeCount; ++i)
            {
                m_BlitMaterialData[i].material = i == (int)BlitType.Core ? blitMaterial : blitHDRMaterial;
                m_BlitMaterialData[i].nearestSamplerPass = m_BlitMaterialData[i].material?.FindPass(BlitPassNames.NearestSampler) ?? -1;
                m_BlitMaterialData[i].bilinearSamplerPass = m_BlitMaterialData[i].material?.FindPass(BlitPassNames.BilinearSampler) ?? -1;
            }
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        [Obsolete("Use RTHandles for colorHandle", true)]
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
        {
            throw new NotSupportedException("Setup with RenderTargetHandle has been deprecated. Use it with RTHandles instead.");
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle colorHandle)
        {
            m_Source = colorHandle;
        }

        static void SetupHDROutput(ColorGamut hdrDisplayColorGamut, Material material, HDROutputUtils.Operation hdrOperation, Vector4 hdrOutputParameters, bool rendersOverlayUI)
        {
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputParameters);
            HDROutputUtils.ConfigureHDROutput(material, hdrDisplayColorGamut, hdrOperation);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HDROverlay, rendersOverlayUI);
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);

            if (resolveToDebugScreen)
            {
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                ConfigureTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                #pragma warning restore CS0618
            }
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            bool outputsToHDR = renderingData.cameraData.isHDROutputActive;
            bool outputsAlpha = false;
            InitPassData(cameraData, ref m_PassData, outputsToHDR ? BlitType.HDR : BlitType.Core, outputsAlpha);

            if (m_PassData.blitMaterialData.material == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_PassData.blitMaterialData, GetType().Name);
                return;
            }

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);
            DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);

            // Get RTHandle alias to use RTHandle apis
            RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
            var cameraTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

            var cmd = renderingData.commandBuffer;

            if (m_Source == cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, profilingSampler))
            {
                m_PassData.blitMaterialData.material.enabledKeywords = null;

                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, cameraData, !resolveToDebugScreen);

                cmd.SetKeyword(ShaderGlobalKeywords.LinearToSRGBConversion,
                    cameraData.requireSrgbConversion);

                if (outputsToHDR)
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();

                    Vector4 hdrOutputLuminanceParams;
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, tonemapping, out hdrOutputLuminanceParams);

                    HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.None;
                    // If the HDRDebugView is on, we don't want the encoding
                    if (debugHandler == null || !debugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget))
                        hdrOperation |= HDROutputUtils.Operation.ColorEncoding;
                    // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                    if (!cameraData.postProcessEnabled)
                        hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                    SetupHDROutput(cameraData.hdrDisplayColorGamut, m_PassData.blitMaterialData.material, hdrOperation, hdrOutputLuminanceParams, cameraData.rendersOverlayUI);
                }

                if (resolveToDebugScreen)
                {
                    // Blit to the debugger texture instead of the camera target
                    int shaderPassIndex = m_Source.rt?.filterMode == FilterMode.Bilinear ? m_PassData.blitMaterialData.bilinearSamplerPass : m_PassData.blitMaterialData.nearestSamplerPass;
                    Vector2 viewportScale = m_Source.useScaling ? new Vector2(m_Source.rtHandleProperties.rtHandleScale.x, m_Source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, m_Source, viewportScale, m_PassData.blitMaterialData.material, shaderPassIndex);

                    cameraData.renderer.ConfigureCameraTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                }
                // TODO RENDERGRAPH: See https://jira.unity3d.com/projects/URP/issues/URP-1737
                // This branch of the if statement must be removed for render graph and the new command list with a novel way of using Blitter with fill mode
                else if (GL.wireframe && cameraData.isSceneViewCamera)
                {
                    // This set render target is necessary so we change the LOAD state to DontCare.
                    cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(m_Source.nameID, cameraTargetHandle.nameID);
                }
                else
                {
                    // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
                    // We need to keep in the pipeline of first render pass to each render target to properly set load/store actions.
                    // meanwhile we set to load so split screen case works.
                    var loadAction = RenderBufferLoadAction.DontCare;
                    if (!cameraData.isSceneViewCamera && !cameraData.isDefaultViewport)
                        loadAction = RenderBufferLoadAction.Load;
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (cameraData.xr.enabled)
                        loadAction = RenderBufferLoadAction.Load;
#endif

                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, cameraTargetHandle, loadAction, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
                    ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, m_Source, cameraTargetHandle, cameraData);
                    cameraData.renderer.ConfigureCameraTarget(cameraTargetHandle, cameraTargetHandle);
                }
            }
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData data, RTHandle source, RTHandle destination, UniversalCameraData cameraData)
        {
            bool isRenderToBackBufferTarget = !cameraData.isSceneViewCamera;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                isRenderToBackBufferTarget = new RenderTargetIdentifier(destination.nameID, 0, CubemapFace.Unknown, -1) == new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, -1);
#endif
            Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(source, destination, cameraData);
            if (isRenderToBackBufferTarget)
                cmd.SetViewport(cameraData.pixelRect);

            // turn off any global wireframe & "scene view wireframe shader hijack" settings for doing blits:
            // we never want them to show up as wireframe
            cmd.SetWireframe(false);

            CoreUtils.SetKeyword(data.blitMaterialData.material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

            int shaderPassIndex = source.rt?.filterMode == FilterMode.Bilinear ? data.blitMaterialData.bilinearSamplerPass : data.blitMaterialData.nearestSamplerPass;
            Blitter.BlitTexture(cmd, source, scaleBias, data.blitMaterialData.material, shaderPassIndex);
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal int sourceID;
            internal Vector4 hdrOutputLuminanceParams;
            internal bool requireSrgbConversion;
            internal bool enableAlphaOutput;
            internal BlitMaterialData blitMaterialData;
            internal UniversalCameraData cameraData;
        }

        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        private void InitPassData(UniversalCameraData cameraData, ref PassData passData, BlitType blitType, bool enableAlphaOutput)
        {
            passData.cameraData = cameraData;
            passData.requireSrgbConversion = cameraData.requireSrgbConversion;
            passData.enableAlphaOutput = enableAlphaOutput;

            passData.blitMaterialData = m_BlitMaterialData[(int)blitType];
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData, in TextureHandle src, in TextureHandle dest, TextureHandle overlayUITexture)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Only the UniversalRenderer guarantees that global textures will be available at this point
                bool isUniversalRenderer = (cameraData.renderer as UniversalRenderer) != null;

                if (cameraData.requiresDepthTexture && isUniversalRenderer)
                    builder.UseGlobalTexture(s_CameraDepthTextureID);

                bool outputsToHDR = cameraData.isHDROutputActive;
                bool outputsAlpha = cameraData.isAlphaOutputEnabled;
                InitPassData(cameraData, ref passData, outputsToHDR ? BlitType.HDR : BlitType.Core, outputsAlpha);

                passData.sourceID = ShaderPropertyId.sourceTex;
                passData.source = src;
                builder.UseTexture(src, AccessFlags.Read);
                passData.destination = dest;
                builder.SetRenderAttachment(dest, 0, AccessFlags.Write);

#if ENABLE_VR && ENABLE_XR_MODULE
                // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                bool passSupportsFoveation = !XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
#endif

                if (outputsToHDR && overlayUITexture.IsValid())
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    Tonemapping tonemapping = stack.GetComponent<Tonemapping>();
                    UniversalRenderPipeline.GetHDROutputLuminanceParameters(passData.cameraData.hdrDisplayInformation, passData.cameraData.hdrDisplayColorGamut, tonemapping, out passData.hdrOutputLuminanceParams);

                    builder.UseTexture(overlayUITexture, AccessFlags.Read);
                }
                else
                {
                    passData.hdrOutputLuminanceParams = new Vector4(-1.0f, -1.0f, -1.0f, -1.0f);
                }

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.blitMaterialData.material.enabledKeywords = null;

                    context.cmd.SetKeyword(ShaderGlobalKeywords.LinearToSRGBConversion, data.requireSrgbConversion);
                    data.blitMaterialData.material.SetTexture(data.sourceID, data.source);

                    DebugHandler debugHandler = GetActiveDebugHandler(data.cameraData);
                    bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(data.cameraData.resolveFinalTarget);

                    // TODO RENDERGRAPH: this should ideally be shared in ExecutePass to avoid code duplication
                    if (data.hdrOutputLuminanceParams.w >= 0)
                    {
                        HDROutputUtils.Operation hdrOperation = HDROutputUtils.Operation.None;
                        // If the HDRDebugView is on, we don't want the encoding
                        if (debugHandler == null || !debugHandler.HDRDebugViewIsActive(data.cameraData.resolveFinalTarget))
                            hdrOperation |= HDROutputUtils.Operation.ColorEncoding;

                        // Color conversion may have happened in the Uber post process through color grading, so we don't want to reapply it
                        if (!data.cameraData.postProcessEnabled)
                            hdrOperation |= HDROutputUtils.Operation.ColorConversion;

                        SetupHDROutput(data.cameraData.hdrDisplayColorGamut, data.blitMaterialData.material, hdrOperation, data.hdrOutputLuminanceParams, data.cameraData.rendersOverlayUI);
                    }

                    if (resolveToDebugScreen)
                    {
                        RTHandle sourceTex = data.source;
                        Vector2 viewportScale = sourceTex.useScaling ? new Vector2(sourceTex.rtHandleProperties.rtHandleScale.x, sourceTex.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                        int shaderPassIndex = sourceTex.rt?.filterMode == FilterMode.Bilinear ? data.blitMaterialData.bilinearSamplerPass : data.blitMaterialData.nearestSamplerPass;
                        Blitter.BlitTexture(context.cmd, sourceTex, viewportScale, data.blitMaterialData.material, shaderPassIndex);
                    }
                    else
                        ExecutePass(context.cmd, data, data.source, data.destination, data.cameraData);
                });
            }
        }
    }
}
