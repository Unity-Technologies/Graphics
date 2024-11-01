using System;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Renders the post-processing effect stack.
    /// </summary>
    internal partial class PostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RTHandle m_Source;
        RTHandle m_Destination;
        RTHandle m_Depth;
        RTHandle m_InternalLut;
        RTHandle m_MotionVectors;
        RTHandle m_FullCoCTexture;
        RTHandle m_HalfCoCTexture;
        RTHandle m_PingTexture;
        RTHandle m_PongTexture;
        RTHandle[] m_BloomMipDown;
        RTHandle[] m_BloomMipUp;
        TextureHandle[] _BloomMipUp;
        TextureHandle[] _BloomMipDown;
        RTHandle m_BlendTexture;
        RTHandle m_EdgeColorTexture;
        RTHandle m_EdgeStencilTexture;
        RTHandle m_TempTarget;
        RTHandle m_TempTarget2;
        RTHandle m_StreakTmpTexture;
        RTHandle m_StreakTmpTexture2;
        RTHandle m_ScreenSpaceLensFlareResult;
        RTHandle m_UserLut;

        const string k_RenderPostProcessingTag = "Blit PostProcessing Effects";
        const string k_RenderFinalPostProcessingTag = "Blit Final PostProcessing";
        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);
        private static readonly ProfilingSampler m_ProfilingRenderFinalPostProcessing = new ProfilingSampler(k_RenderFinalPostProcessingTag);

        MaterialLibrary m_Materials;
        PostProcessData m_Data;

        // Builtin effects settings
        DepthOfField m_DepthOfField;
        MotionBlur m_MotionBlur;
        ScreenSpaceLensFlare m_LensFlareScreenSpace;
        PaniniProjection m_PaniniProjection;
        Bloom m_Bloom;
        LensDistortion m_LensDistortion;
        ChromaticAberration m_ChromaticAberration;
        Vignette m_Vignette;
        ColorLookup m_ColorLookup;
        ColorAdjustments m_ColorAdjustments;
        Tonemapping m_Tonemapping;
        FilmGrain m_FilmGrain;

        // Depth Of Field shader passes
        const int k_GaussianDoFPassComputeCoc = 0;
        const int k_GaussianDoFPassDownscalePrefilter = 1;
        const int k_GaussianDoFPassBlurH = 2;
        const int k_GaussianDoFPassBlurV = 3;
        const int k_GaussianDoFPassComposite = 4;

        const int k_BokehDoFPassComputeCoc = 0;
        const int k_BokehDoFPassDownscalePrefilter = 1;
        const int k_BokehDoFPassBlur = 2;
        const int k_BokehDoFPassPostFilter = 3;
        const int k_BokehDoFPassComposite = 4;


        // Misc
        const int k_MaxPyramidSize = 16;
        readonly GraphicsFormat m_DefaultColorFormat;   // The default format for post-processing, follows back-buffer format in URP.
        bool m_DefaultColorFormatIsAlpha;
        readonly GraphicsFormat m_SMAAEdgeFormat;
        readonly GraphicsFormat m_GaussianCoCFormat;

        int m_DitheringTextureIndex;
        RenderTargetIdentifier[] m_MRT2;
        Vector4[] m_BokehKernel;
        int m_BokehHash;
        // Needed if the device changes its render target width/height (ex, Mobile platform allows change of orientation)
        float m_BokehMaxRadius;
        float m_BokehRCPAspect;

        // True when this is the very last pass in the pipeline
        bool m_IsFinalPass;

        // If there's a final post process pass after this pass.
        // If yes, Film Grain and Dithering are setup in the final pass, otherwise they are setup in this pass.
        bool m_HasFinalPass;

        // Some Android devices do not support sRGB backbuffer
        // We need to do the conversion manually on those
        // Also if HDR output is active
        bool m_EnableColorEncodingIfNeeded;

        // Use Fast conversions between SRGB and Linear
        bool m_UseFastSRGBLinearConversion;

        // Support Screen Space Lens Flare post process effect
        bool m_SupportScreenSpaceLensFlare;

        // Support Data Driven Lens Flare post process effect
        bool m_SupportDataDrivenLensFlare;

        // Blit to screen or color frontbuffer at the end
        bool m_ResolveToScreen;

        // Renderer is using swapbuffer system
        bool m_UseSwapBuffer;

        // RTHandle used as a temporary target when operations need to be performed before image scaling
        RTHandle m_ScalingSetupTarget;

        // RTHandle used as a temporary target when operations need to be performed after upscaling
        RTHandle m_UpscaledTarget;

        Material m_BlitMaterial;

        // Cached bloom params from previous frame to avoid unnecessary material updates
        BloomMaterialParams m_BloomParamsPrev;

        /// <summary>
        /// Creates a new <c>PostProcessPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="postProcessParams">The <c>PostProcessParams</c> run-time params to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="PostProcessData"/>
        /// <seealso cref="PostProcessParams"/>
        public PostProcessPass(RenderPassEvent evt, PostProcessData data, ref PostProcessParams postProcessParams)
        {
            profilingSampler = new ProfilingSampler(nameof(PostProcessPass));
            renderPassEvent = evt;
            m_Data = data;
            m_Materials = new MaterialLibrary(data);

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];
            m_BloomMipUp = new RTHandle[k_MaxPyramidSize];
            m_BloomMipDown = new RTHandle[k_MaxPyramidSize];
            // Bloom pyramid TextureHandles
            _BloomMipUp = new TextureHandle[k_MaxPyramidSize];
            _BloomMipDown = new TextureHandle[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
                // Get name, will get Allocated with descriptor later
                m_BloomMipUp[i] = RTHandles.Alloc(ShaderConstants._BloomMipUp[i], name: "_BloomMipUp" + i);
                m_BloomMipDown[i] = RTHandles.Alloc(ShaderConstants._BloomMipDown[i], name: "_BloomMipDown" + i);
            }

            m_MRT2 = new RenderTargetIdentifier[2];
            base.useNativeRenderPass = false;

            m_BlitMaterial = postProcessParams.blitMaterial;

            // NOTE: Request color format is the back-buffer color format. It can be HDR or SDR (when HDR disabled).
            // Request color might have alpha or might not have alpha.
            // The actual post-process target can be different. A RenderTexture with a custom format. Not necessarily a back-buffer.
            // A RenderTexture with a custom format can have an alpha channel, regardless of the back-buffer setting,
            // so the post-processing should just use the current target format/alpha to toggle alpha output.
            //
            // However, we want to filter out the alpha shader variants when not used (common case).
            // The rule is that URP post-processing format follows the back-buffer format setting.

            bool requestHDR = IsHDRFormat(postProcessParams.requestColorFormat);
            bool requestAlpha = IsAlphaFormat(postProcessParams.requestColorFormat);

            // Texture format pre-lookup
            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (requestHDR)
            {
                m_DefaultColorFormatIsAlpha = requestAlpha;

                const GraphicsFormatUsage usage = GraphicsFormatUsage.Blend;
                if (SystemInfo.IsFormatSupported(postProcessParams.requestColorFormat, usage))    // Typically, RGBA16Float.
                {
                    m_DefaultColorFormat = postProcessParams.requestColorFormat;
                }
                else if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)) // HDR fallback
                {
                    // NOTE: Technically request format can be with alpha, however if it's not supported and we fall back here
                    // , we assume no alpha. Post-process default format follows the back buffer format.
                    // If support failed, it must have failed for back buffer too.
                    m_DefaultColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                    m_DefaultColorFormatIsAlpha = false;
                }
                else
                {
                    m_DefaultColorFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? GraphicsFormat.R8G8B8A8_SRGB
                        : GraphicsFormat.R8G8B8A8_UNorm;
                }
            }
            else // SDR
            {
                m_DefaultColorFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;

                m_DefaultColorFormatIsAlpha = true;
            }

            // Only two components are needed for edge render texture, but on some vendors four components may be faster.
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8_UNorm, GraphicsFormatUsage.Render) && SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("arm"))
                m_SMAAEdgeFormat = GraphicsFormat.R8G8_UNorm;
            else
                m_SMAAEdgeFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, GraphicsFormatUsage.Blend))
                m_GaussianCoCFormat = GraphicsFormat.R16_UNorm;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Blend))
                m_GaussianCoCFormat = GraphicsFormat.R16_SFloat;
            else // Expect CoC banding
                m_GaussianCoCFormat = GraphicsFormat.R8_UNorm;
        }

        /// <summary>
        /// Cleans up the Material Library used in the passes.
        /// </summary>
        public void Cleanup()
        {
            m_Materials.Cleanup();
            Dispose();
        }

        /// <summary>
        /// Disposes used resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var handle in m_BloomMipDown)
                handle?.Release();
            foreach (var handle in m_BloomMipUp)
                handle?.Release();
            m_ScalingSetupTarget?.Release();
            m_UpscaledTarget?.Release();
            m_FullCoCTexture?.Release();
            m_HalfCoCTexture?.Release();
            m_PingTexture?.Release();
            m_PongTexture?.Release();
            m_BlendTexture?.Release();
            m_EdgeColorTexture?.Release();
            m_EdgeStencilTexture?.Release();
            m_TempTarget?.Release();
            m_TempTarget2?.Release();
            m_StreakTmpTexture?.Release();
            m_StreakTmpTexture2?.Release();
            m_ScreenSpaceLensFlareResult?.Release();
            m_UserLut?.Release();
        }

        /// <summary>
        /// Configures the pass.
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="source"></param>
        /// <param name="resolveToScreen"></param>
        /// <param name="depth"></param>
        /// <param name="internalLut"></param>
        /// <param name="hasFinalPass"></param>
        /// <param name="enableColorEncoding"></param>
        public void Setup(in RenderTextureDescriptor baseDescriptor, in RTHandle source, bool resolveToScreen, in RTHandle depth, in RTHandle internalLut, in RTHandle motionVectors, bool hasFinalPass, bool enableColorEncoding)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Source = source;
            m_Depth = depth;
            m_InternalLut = internalLut;
            m_MotionVectors = motionVectors;
            m_IsFinalPass = false;
            m_HasFinalPass = hasFinalPass;
            m_EnableColorEncodingIfNeeded = enableColorEncoding;
            m_ResolveToScreen = resolveToScreen;
            m_UseSwapBuffer = true;

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            m_Destination = k_CameraTarget;
            #pragma warning restore CS0618
        }

        /// <summary>
        /// Configures the pass.
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="depth"></param>
        /// <param name="internalLut"></param>
        /// <param name="hasFinalPass"></param>
        /// <param name="enableColorEncoding"></param>
        public void Setup(in RenderTextureDescriptor baseDescriptor, in RTHandle source, RTHandle destination, in RTHandle depth, in RTHandle internalLut, bool hasFinalPass, bool enableColorEncoding)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Source = source;
            m_Destination = destination;
            m_Depth = depth;
            m_InternalLut = internalLut;
            m_IsFinalPass = false;
            m_HasFinalPass = hasFinalPass;
            m_EnableColorEncodingIfNeeded = enableColorEncoding;
            m_UseSwapBuffer = true;
        }

        /// <summary>
        /// Configures the Final pass.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="useSwapBuffer"></param>
        /// <param name="enableColorEncoding"></param>
        public void SetupFinalPass(in RTHandle source, bool useSwapBuffer = false, bool enableColorEncoding = true)
        {
            m_Source = source;
            m_IsFinalPass = true;
            m_HasFinalPass = false;
            m_EnableColorEncodingIfNeeded = enableColorEncoding;
            m_UseSwapBuffer = useSwapBuffer;

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            m_Destination = k_CameraTarget;
            #pragma warning restore CS0618
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            overrideCameraTarget = true;
        }

        public bool CanRunOnTile()
        {
            // Check builtin & user effects here
            return false;
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Start by pre-fetching all builtin effect settings we need
            // Some of the color-grading settings are only used in the color grading lut pass
            var stack = VolumeManager.instance.stack;
            m_DepthOfField = stack.GetComponent<DepthOfField>();
            m_MotionBlur = stack.GetComponent<MotionBlur>();
            m_LensFlareScreenSpace = stack.GetComponent<ScreenSpaceLensFlare>();
            m_PaniniProjection = stack.GetComponent<PaniniProjection>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_LensDistortion = stack.GetComponent<LensDistortion>();
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_ColorLookup = stack.GetComponent<ColorLookup>();
            m_ColorAdjustments = stack.GetComponent<ColorAdjustments>();
            m_Tonemapping = stack.GetComponent<Tonemapping>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            m_UseFastSRGBLinearConversion = renderingData.postProcessingData.useFastSRGBLinearConversion;
            m_SupportScreenSpaceLensFlare = renderingData.postProcessingData.supportScreenSpaceLensFlare;
            m_SupportDataDrivenLensFlare = renderingData.postProcessingData.supportDataDrivenLensFlare;

            var cmd = renderingData.commandBuffer;
            if (m_IsFinalPass)
            {
                using (new ProfilingScope(cmd, m_ProfilingRenderFinalPostProcessing))
                {
                    RenderFinalPass(cmd, ref renderingData);
                }
            }
            else if (CanRunOnTile())
            {
                // TODO: Add a fast render path if only on-tile compatible effects are used and we're actually running on a platform that supports it
                // Note: we can still work on-tile if FXAA is enabled, it'd be part of the final pass
            }
            else
            {
                // Regular render path (not on-tile) - we do everything in a single command buffer as it
                // makes it easier to manage temporary targets' lifetime
                using (new ProfilingScope(cmd, m_ProfilingRenderPostProcessing))
                {
                    Render(cmd, ref renderingData);
                }
            }
        }

        bool IsHDRFormat(GraphicsFormat format)
        {
            return format == GraphicsFormat.B10G11R11_UFloatPack32 ||
                   GraphicsFormatUtility.IsHalfFormat(format) ||
                   GraphicsFormatUtility.IsFloatFormat(format);
        }

        bool IsAlphaFormat(GraphicsFormat format)
        {
            return GraphicsFormatUtility.HasAlphaChannel(format);
        }

        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, GraphicsFormat depthStencilFormat = GraphicsFormat.None)
            => GetCompatibleDescriptor(m_Descriptor, width, height, format, depthStencilFormat);

        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, GraphicsFormat depthStencilFormat = GraphicsFormat.None)
        {
            desc.depthStencilFormat = depthStencilFormat;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        bool RequireSRGBConversionBlitToBackBuffer(bool requireSrgbConversion)
        {
            return requireSrgbConversion && m_EnableColorEncodingIfNeeded;
        }

        bool RequireHDROutput(UniversalCameraData cameraData)
        {
            // If capturing, don't convert to HDR.
            // If not last in the stack, don't convert to HDR.
            return cameraData.isHDROutputActive && cameraData.captureActions == null;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            ref ScriptableRenderer renderer = ref cameraData.renderer;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            //Check amount of swaps we have to do
            //We blit back and forth without msaa until the last blit.
            bool useStopNan = cameraData.isStopNaNEnabled && m_Materials.stopNaN != null;
            bool useSubPixeMorpAA = cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var dofMaterial = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian ? m_Materials.gaussianDepthOfField : m_Materials.bokehDepthOfField;
            bool useDepthOfField = m_DepthOfField.IsActive() && !isSceneViewCamera && dofMaterial != null;
            bool useLensFlare = !LensFlareCommonSRP.Instance.IsEmpty() && m_SupportDataDrivenLensFlare;
            bool useLensFlareScreenSpace = m_LensFlareScreenSpace.IsActive() && m_SupportScreenSpaceLensFlare;
            bool useMotionBlur = m_MotionBlur.IsActive() && !isSceneViewCamera;
            bool usePaniniProjection = m_PaniniProjection.IsActive() && !isSceneViewCamera;

            // Disable MotionBlur in EditMode, so that editing remains clear and readable.
            // NOTE: HDRP does the same via CoreUtils::AreAnimatedMaterialsEnabled().
            useMotionBlur = useMotionBlur && Application.isPlaying;

            // Note that enabling jitters uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides (like
            // disable useTemporalAA if another feature is disabled) then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking the value here.
            bool useTemporalAA = cameraData.IsTemporalAAEnabled();
            if (cameraData.antialiasing == AntialiasingMode.TemporalAntiAliasing && !useTemporalAA)
                TemporalAA.ValidateAndWarn(cameraData);

            int amountOfPassesRemaining = (useStopNan ? 1 : 0) + (useSubPixeMorpAA ? 1 : 0) + (useDepthOfField ? 1 : 0) + (useLensFlare ? 1 : 0) + (useTemporalAA ? 1 : 0) + (useMotionBlur ? 1 : 0) + (usePaniniProjection ? 1 : 0);

            if (m_UseSwapBuffer && amountOfPassesRemaining > 0)
            {
                renderer.EnableSwapBufferMSAA(false);
            }

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            // Don't use these directly unless you have a good reason to, use GetSource() and
            // GetDestination() instead
            RTHandle source = m_UseSwapBuffer ? renderer.cameraColorTargetHandle : m_Source;
            RTHandle destination = m_UseSwapBuffer ? renderer.GetCameraColorFrontBuffer(cmd) : null;
            #pragma warning restore CS0618

            RTHandle GetSource() => source;

            RTHandle GetDestination()
            {
                if (destination == null)
                {
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempTarget, GetCompatibleDescriptor(), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTarget");
                    destination = m_TempTarget;
                }
                else if (destination == m_Source && m_Descriptor.msaaSamples > 1)
                {
                    // Avoid using m_Source.id as new destination, it may come with a depth buffer that we don't want, may have MSAA that we don't want etc
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempTarget2, GetCompatibleDescriptor(), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTarget2");
                    destination = m_TempTarget2;
                }
                return destination;
            }

            void Swap(ref ScriptableRenderer r)
            {
                --amountOfPassesRemaining;
                if (m_UseSwapBuffer)
                {
                    r.SwapColorBuffer(cmd);

                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    source = r.cameraColorTargetHandle;
                    #pragma warning restore CS0618

                    //we want the last blit to be to MSAA
                    if (amountOfPassesRemaining == 0 && !m_HasFinalPass)
                        r.EnableSwapBufferMSAA(true);

                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    destination = r.GetCameraColorFrontBuffer(cmd);
                    #pragma warning restore CS0618
                }
                else
                {
                    CoreUtils.Swap(ref source, ref destination);
                }
            }

            // Setup projection matrix for cmd.DrawMesh()
            cmd.SetGlobalMatrix(ShaderConstants._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));

            // Optional NaN killer before post-processing kicks in
            // stopNaN may be null on Adreno 3xx. It doesn't support full shader level 3.5, but SystemInfo.graphicsShaderLevel is 35.
            if (useStopNan)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.StopNaNs)))
                {
                    Blitter.BlitCameraTexture(cmd, GetSource(), GetDestination(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Materials.stopNaN, 0);
                    Swap(ref renderer);
                }
            }

            // Anti-aliasing
            if (useSubPixeMorpAA)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.SMAA)))
                {
                    DoSubpixelMorphologicalAntialiasing(ref renderingData.cameraData, cmd, GetSource(), GetDestination());
                    Swap(ref renderer);
                }
            }

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            if (useDepthOfField)
            {
                var markerName = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian
                    ? URPProfileId.GaussianDepthOfField
                    : URPProfileId.BokehDepthOfField;

                using (new ProfilingScope(cmd, ProfilingSampler.Get(markerName)))
                {
                    DoDepthOfField(ref renderingData.cameraData, cmd, GetSource(), GetDestination(), cameraData.pixelRect);
                    Swap(ref renderer);
                }
            }

            // Temporal Anti Aliasing
            if (useTemporalAA)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.TemporalAA)))
                {
                    Debug.Assert(m_MotionVectors != null, "MotionVectors are invalid. TAA requires a motion vector texture.");

                    TemporalAA.ExecutePass(cmd, m_Materials.temporalAntialiasing, ref renderingData.cameraData, source, destination, m_MotionVectors?.rt);
                    Swap(ref renderer);
                }
            }


            // Motion blur
            if (useMotionBlur)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MotionBlur)))
                {
                    DoMotionBlur(cmd, GetSource(), GetDestination(), m_MotionVectors, ref renderingData.cameraData);
                    Swap(ref renderer);
                }
            }

            // Panini projection is done as a fullscreen pass after all depth-based effects are done
            // and before bloom kicks in
            if (usePaniniProjection)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.PaniniProjection)))
                {
                    DoPaniniProjection(cameraData.camera, cmd, GetSource(), GetDestination());
                    Swap(ref renderer);
                }
            }

            // Combined post-processing stack
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.UberPostProcess)))
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive();
                bool lensFlareScreenSpaceActive = m_LensFlareScreenSpace.IsActive();

                // We need to still do the bloom pass if lens flare screen space is active because it uses _Bloom_Texture.
                if (bloomActive || lensFlareScreenSpaceActive)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.Bloom)))
                        SetupBloom(cmd, GetSource(), m_Materials.uber, cameraData.isAlphaOutputEnabled);
                }

                // Lens Flare Screen Space
                if (useLensFlareScreenSpace)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.LensFlareScreenSpace)))
                    {
                        // We clamp the bloomMip value to avoid picking a mip that doesn't exist, since in URP you can set the number of maxIteration of the bloomPass.
                        int maxBloomMip = Mathf.Clamp(m_LensFlareScreenSpace.bloomMip.value, 0, m_Bloom.maxIterations.value/2);
                        DoLensFlareScreenSpace(cameraData.camera, cmd, GetSource(), m_BloomMipUp[0], m_BloomMipUp[maxBloomMip]);
                    }
                }

                // Lens Flare
                if (useLensFlare)
                {
                    bool usePanini;
                    float paniniDistance;
                    float paniniCropToFit;
                    if (m_PaniniProjection.IsActive())
                    {
                        usePanini = true;
                        paniniDistance = m_PaniniProjection.distance.value;
                        paniniCropToFit = m_PaniniProjection.cropToFit.value;
                    }
                    else
                    {
                        usePanini = false;
                        paniniDistance = 1.0f;
                        paniniCropToFit = 1.0f;
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.LensFlareDataDrivenComputeOcclusion)))
                    {
                        LensFlareDataDrivenComputeOcclusion(ref cameraData, cmd, GetSource(), usePanini, paniniDistance, paniniCropToFit);
                    }
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.LensFlareDataDriven)))
                    {
                        LensFlareDataDriven(ref cameraData, cmd, GetSource(), usePanini, paniniDistance, paniniCropToFit);
                    }
                }

                // Setup other effects constants
                SetupLensDistortion(m_Materials.uber, isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(m_Materials.uber, cameraData.xr);
                SetupColorGrading(cmd, ref renderingData, m_Materials.uber);

                // Only apply dithering & grain if there isn't a final pass.
                SetupGrain(cameraData, m_Materials.uber);
                SetupDithering(cameraData, m_Materials.uber);

                if (RequireSRGBConversionBlitToBackBuffer(cameraData.requireSrgbConversion))
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

                bool requireHDROutput = RequireHDROutput(cameraData);
                if (requireHDROutput)
                {
                    // Color space conversion is already applied through color grading, do encoding if uber post is the last pass
                    // Otherwise encoding will happen in the final post process pass or the final blit pass
                    HDROutputUtils.Operation hdrOperation = !m_HasFinalPass && m_EnableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
                    SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, m_Materials.uber, hdrOperation, cameraData.rendersOverlayUI);
                }

                if (m_UseFastSRGBLinearConversion)
                {
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.UseFastSRGBLinearConversion);
                }

                CoreUtils.SetKeyword(m_Materials.uber, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

                DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
                bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, cameraData, !m_HasFinalPass && !resolveToDebugScreen);

                // Done with Uber, blit it
                var colorLoadAction = RenderBufferLoadAction.DontCare;

                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                if (m_Destination == k_CameraTarget && !cameraData.isDefaultViewport)
                    colorLoadAction = RenderBufferLoadAction.Load;
                #pragma warning restore CS0618

                // Note: We rendering to "camera target" we need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
                // Overlay cameras need to output to the target described in the base camera while doing camera stack.
                RenderTargetIdentifier cameraTargetID = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                    cameraTargetID = cameraData.xr.renderTarget;
#endif

                if (!m_UseSwapBuffer)
                    m_ResolveToScreen = cameraData.resolveFinalTarget || m_Destination.nameID == cameraTargetID || m_HasFinalPass == true;

                // With camera stacking we not always resolve post to final screen as we might run post-processing in the middle of the stack.
                if (m_UseSwapBuffer && !m_ResolveToScreen)
                {
                    if (!m_HasFinalPass)
                    {
                        // We need to reenable this to be able to blit to the correct AA target
                        renderer.EnableSwapBufferMSAA(true);
                        // Disable obsolete warning for internal usage
                        #pragma warning disable CS0618
                        destination = renderer.GetCameraColorFrontBuffer(cmd);
                        #pragma warning restore CS0618
                    }

                    Blitter.BlitCameraTexture(cmd, GetSource(), destination, colorLoadAction, RenderBufferStoreAction.Store, m_Materials.uber, 0);
                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    renderer.ConfigureCameraColorTarget(destination);
                    #pragma warning restore CS0618
                    Swap(ref renderer);
                }
                // TODO: Implement swapbuffer in 2DRenderer so we can remove this
                // For now, when render post-processing in the middle of the camera stack (not resolving to screen)
                // we do an extra blit to ping pong results back to color texture. In future we should allow a Swap of the current active color texture
                // in the pipeline to avoid this extra blit.
                else if (!m_UseSwapBuffer)
                {
                    var firstSource = GetSource();
                    Blitter.BlitCameraTexture(cmd, firstSource, GetDestination(), colorLoadAction, RenderBufferStoreAction.Store, m_Materials.uber, 0);
                    Blitter.BlitCameraTexture(cmd, GetDestination(), m_Destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_BlitMaterial, m_Destination.rt?.filterMode == FilterMode.Bilinear ? 1 : 0);
                }
                else if (m_ResolveToScreen)
                {
                    if (resolveToDebugScreen)
                    {
                        // Blit to the debugger texture instead of the camera target
                        Blitter.BlitCameraTexture(cmd, GetSource(), debugHandler.DebugScreenColorHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, m_Materials.uber, 0);
                        // Disable obsolete warning for internal usage
                        #pragma warning disable CS0618
                        renderer.ConfigureCameraTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                        #pragma warning restore CS0618
                    }
                    else
                    {
                        // Get RTHandle alias to use RTHandle apis
                        RenderTargetIdentifier cameraTarget = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : cameraTargetID;
                        RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
                        var cameraTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

                        RenderingUtils.FinalBlit(cmd, cameraData, GetSource(), cameraTargetHandle, colorLoadAction, RenderBufferStoreAction.Store, m_Materials.uber, 0);
                        // Disable obsolete warning for internal usage
                        #pragma warning disable CS0618
                        renderer.ConfigureCameraColorTarget(cameraTargetHandle);
                        #pragma warning restore CS0618
                    }
                }
            }
        }
        #region Sub-pixel Morphological Anti-aliasing

        void DoSubpixelMorphologicalAntialiasing(ref CameraData cameraData, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            var pixelRect = new Rect(Vector2.zero, new Vector2(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height));
            var material = m_Materials.subpixelMorphologicalAntialiasing;
            const int kStencilBit = 64;

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_EdgeStencilTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.None, GraphicsFormatUtility.GetDepthStencilFormat(24)), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_EdgeStencilTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_EdgeColorTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_SMAAEdgeFormat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_EdgeColorTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_BlendTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8G8B8A8_UNorm), FilterMode.Point, TextureWrapMode.Clamp, name: "_BlendTexture");

            // Globals
            var targetSize = m_EdgeColorTexture.useScaling ? m_EdgeColorTexture.rtHandleProperties.currentRenderTargetSize : new Vector2Int(m_EdgeColorTexture.rt.width, m_EdgeColorTexture.rt.height);
            material.SetVector(ShaderConstants._Metrics, new Vector4(1f / targetSize.x, 1f / targetSize.y, targetSize.x, targetSize.y));
            material.SetTexture(ShaderConstants._AreaTexture, m_Data.textures.smaaAreaTex);
            material.SetTexture(ShaderConstants._SearchTexture, m_Data.textures.smaaSearchTex);
            material.SetFloat(ShaderConstants._StencilRef, (float)kStencilBit);
            material.SetFloat(ShaderConstants._StencilMask, (float)kStencilBit);

            // Quality presets
            material.shaderKeywords = null;

            switch (cameraData.antialiasingQuality)
            {
                case AntialiasingQuality.Low:
                    material.EnableKeyword(ShaderKeywordStrings.SmaaLow);
                    break;
                case AntialiasingQuality.Medium:
                    material.EnableKeyword(ShaderKeywordStrings.SmaaMedium);
                    break;
                case AntialiasingQuality.High:
                    material.EnableKeyword(ShaderKeywordStrings.SmaaHigh);
                    break;
            }

            // Pass 1: Edge detection
            RenderingUtils.Blit(cmd, source, pixelRect,
                m_EdgeColorTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                m_EdgeStencilTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                ClearFlag.ColorStencil, Color.clear,  // implicit depth=1.0f stencil=0x0
                material, 0);

            // Pass 2: Blend weights
            RenderingUtils.Blit(cmd, m_EdgeColorTexture, pixelRect,
                m_BlendTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                m_EdgeStencilTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare,
                ClearFlag.Color, Color.clear, material, 1);

            // Pass 3: Neighborhood blending
            cmd.SetGlobalTexture(ShaderConstants._BlendTexture, m_BlendTexture.nameID);
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 2);
        }

        #endregion

        #region Depth Of Field

        // TODO: CoC reprojection once TAA gets in LW
        // TODO: Proper LDR/gamma support
        void DoDepthOfField(ref CameraData cameraData, CommandBuffer cmd, RTHandle source, RTHandle destination, Rect pixelRect)
        {
            if (m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian)
                DoGaussianDepthOfField(cmd, source, destination, pixelRect, cameraData.isAlphaOutputEnabled);
            else if (m_DepthOfField.mode.value == DepthOfFieldMode.Bokeh)
                DoBokehDepthOfField(cmd, source, destination, pixelRect, cameraData.isAlphaOutputEnabled);
        }

        void DoGaussianDepthOfField(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect pixelRect, bool enableAlphaOutput)
        {
            int downSample = 2;
            var material = m_Materials.gaussianDepthOfField;
            int wh = m_Descriptor.width / downSample;
            int hh = m_Descriptor.height / downSample;
            float farStart = m_DepthOfField.gaussianStart.value;
            float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianEnd.value);

            // Assumes a radius of 1 is 1 at 1080p
            // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
            // very high resolutions (4K+).
            float maxRadius = m_DepthOfField.gaussianMaxRadius.value * (wh / 1080f);
            maxRadius = Mathf.Min(maxRadius, 2f);

            CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, enableAlphaOutput);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HighQualitySampling, m_DepthOfField.highQualitySampling.value);
            material.SetVector(ShaderConstants._CoCParams, new Vector3(farStart, farEnd, maxRadius));

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_FullCoCTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_GaussianCoCFormat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_FullCoCTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_HalfCoCTexture, GetCompatibleDescriptor(wh, hh, m_GaussianCoCFormat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_HalfCoCTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_PingTexture, GetCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PingTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_PongTexture, GetCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PongTexture");

            PostProcessUtils.SetSourceSize(cmd, m_FullCoCTexture);
            cmd.SetGlobalVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / downSample, 1.0f / downSample, downSample, downSample));

            // Compute CoC
            Blitter.BlitCameraTexture(cmd, source, m_FullCoCTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_GaussianDoFPassComputeCoc);

            // Downscale & prefilter color + coc
            m_MRT2[0] = m_HalfCoCTexture.nameID;
            m_MRT2[1] = m_PingTexture.nameID;

            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, m_FullCoCTexture.nameID);
            CoreUtils.SetRenderTarget(cmd, m_MRT2, m_HalfCoCTexture);
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            Blitter.BlitTexture(cmd, source, viewportScale, material, k_GaussianDoFPassDownscalePrefilter);

            // Blur
            cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, m_HalfCoCTexture.nameID);
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, source);
            Blitter.BlitCameraTexture(cmd, m_PingTexture, m_PongTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_GaussianDoFPassBlurH);
            Blitter.BlitCameraTexture(cmd, m_PongTexture, m_PingTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_GaussianDoFPassBlurV);

            // Composite
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, m_PingTexture.nameID);
            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, m_FullCoCTexture.nameID);
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_GaussianDoFPassComposite);
        }

        void PrepareBokehKernel(float maxRadius, float rcpAspect)
        {
            const int kRings = 4;
            const int kPointsPerRing = 7;

            // Check the existing array
            if (m_BokehKernel == null)
                m_BokehKernel = new Vector4[42];

            // Fill in sample points (concentric circles transformed to rotated N-Gon)
            int idx = 0;
            float bladeCount = m_DepthOfField.bladeCount.value;
            float curvature = 1f - m_DepthOfField.bladeCurvature.value;
            float rotation = m_DepthOfField.bladeRotation.value * Mathf.Deg2Rad;
            const float PI = Mathf.PI;
            const float TWO_PI = Mathf.PI * 2f;

            for (int ring = 1; ring < kRings; ring++)
            {
                float bias = 1f / kPointsPerRing;
                float radius = (ring + bias) / (kRings - 1f + bias);
                int points = ring * kPointsPerRing;

                for (int point = 0; point < points; point++)
                {
                    // Angle on ring
                    float phi = 2f * PI * point / points;

                    // Transform to rotated N-Gon
                    // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                    float nt = Mathf.Cos(PI / bladeCount);
                    float dt = Mathf.Cos(phi - (TWO_PI / bladeCount) * Mathf.Floor((bladeCount * phi + Mathf.PI) / TWO_PI));
                    float r = radius * Mathf.Pow(nt / dt, curvature);
                    float u = r * Mathf.Cos(phi - rotation);
                    float v = r * Mathf.Sin(phi - rotation);

                    float uRadius = u * maxRadius;
                    float vRadius = v * maxRadius;
                    float uRadiusPowTwo = uRadius * uRadius;
                    float vRadiusPowTwo = vRadius * vRadius;
                    float kernelLength = Mathf.Sqrt((uRadiusPowTwo + vRadiusPowTwo));
                    float uRCP = uRadius * rcpAspect;

                    m_BokehKernel[idx] = new Vector4(uRadius, vRadius, kernelLength, uRCP);
                    idx++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetMaxBokehRadiusInPixels(float viewportHeight)
        {
            // Estimate the maximum radius of bokeh (empirically derived from the ring count)
            const float kRadiusInPixels = 14f;
            return Mathf.Min(0.05f, kRadiusInPixels / viewportHeight);
        }

        void DoBokehDepthOfField(CommandBuffer cmd, RTHandle source, RTHandle destination, Rect pixelRect, bool enableAlphaOutput)
        {
            int downSample = 2;
            var material = m_Materials.bokehDepthOfField;
            int wh = m_Descriptor.width / downSample;
            int hh = m_Descriptor.height / downSample;

            // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
            float F = m_DepthOfField.focalLength.value / 1000f;
            float A = m_DepthOfField.focalLength.value / m_DepthOfField.aperture.value;
            float P = m_DepthOfField.focusDistance.value;
            float maxCoC = (A * F) / (P - F);
            float maxRadius = GetMaxBokehRadiusInPixels(m_Descriptor.height);
            float rcpAspect = 1f / (wh / (float)hh);

            CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, enableAlphaOutput);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.UseFastSRGBLinearConversion, m_UseFastSRGBLinearConversion);
            cmd.SetGlobalVector(ShaderConstants._CoCParams, new Vector4(P, maxCoC, maxRadius, rcpAspect));

            // Prepare the bokeh kernel constant buffer
            int hash = m_DepthOfField.GetHashCode();
            if (hash != m_BokehHash || maxRadius != m_BokehMaxRadius || rcpAspect != m_BokehRCPAspect)
            {
                m_BokehHash = hash;
                m_BokehMaxRadius = maxRadius;
                m_BokehRCPAspect = rcpAspect;
                PrepareBokehKernel(maxRadius, rcpAspect);
            }

            cmd.SetGlobalVectorArray(ShaderConstants._BokehKernel, m_BokehKernel);

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_FullCoCTexture, GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8_UNorm), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_FullCoCTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_PingTexture, GetCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PingTexture");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_PongTexture, GetCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PongTexture");

            PostProcessUtils.SetSourceSize(cmd, m_FullCoCTexture);
            cmd.SetGlobalVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / downSample, 1.0f / downSample, downSample, downSample));
            float uvMargin = (1.0f / m_Descriptor.height) * downSample;
            cmd.SetGlobalVector(ShaderConstants._BokehConstants, new Vector4(uvMargin, uvMargin * 2.0f));

            // Compute CoC
            Blitter.BlitCameraTexture(cmd, source, m_FullCoCTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_BokehDoFPassComputeCoc);
            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, m_FullCoCTexture.nameID);

            // Downscale & prefilter color + coc
            Blitter.BlitCameraTexture(cmd, source, m_PingTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_BokehDoFPassDownscalePrefilter);

            // Bokeh blur
            Blitter.BlitCameraTexture(cmd, m_PingTexture, m_PongTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_BokehDoFPassBlur);

            // Post-filtering
            Blitter.BlitCameraTexture(cmd, m_PongTexture, m_PingTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_BokehDoFPassPostFilter);

            // Composite
            cmd.SetGlobalTexture(ShaderConstants._DofTexture, m_PingTexture.nameID);
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, k_BokehDoFPassComposite);
        }

        #endregion

        #region LensFlareDataDriven

        static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        {
            // Must always be true
            if (light != null)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
                    case LightType.Point:
                        return LensFlareCommonSRP.ShapeAttenuationPointLight();
                    case LightType.Spot:
                        return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
                    default:
                        return 1.0f;
                }
            }

            return 1.0f;
        }

        void LensFlareDataDrivenComputeOcclusion(ref UniversalCameraData cameraData, CommandBuffer cmd, RenderTargetIdentifier source, bool usePanini, float paniniDistance, float paniniCropToFit)
        {
            if (!LensFlareCommonSRP.IsOcclusionRTCompatible())
                return;

            Camera camera = cameraData.camera;

            Matrix4x4 nonJitteredViewProjMatrix0;
            int xrId0;
#if ENABLE_VR && ENABLE_XR_MODULE
            // Not VR or Multi-Pass
            if (cameraData.xr.enabled)
            {
                if (cameraData.xr.singlePassEnabled)
                {
                    nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrixNoJitter(0), true) * cameraData.GetViewMatrix(0);
                    xrId0 = 0;
                }
                else
                {
                    var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                    nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
                    xrId0 = cameraData.xr.multipassId;
                }
            }
            else
            {
                nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrixNoJitter(0), true) * cameraData.GetViewMatrix(0);
                xrId0 = 0;
            }
#else
            var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
            xrId0 = cameraData.xr.multipassId;
#endif

            cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);

            LensFlareCommonSRP.ComputeOcclusion(
                m_Materials.lensFlareDataDriven, camera, cameraData.xr, cameraData.xr.multipassId,
                (float)m_Descriptor.width, (float)m_Descriptor.height,
                usePanini, paniniDistance, paniniCropToFit, true,
                camera.transform.position,
                nonJitteredViewProjMatrix0,
                cmd,
                false, false, null, null);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
            {
                for (int xrIdx = 1; xrIdx < cameraData.xr.viewCount; ++xrIdx)
                {
                    Matrix4x4 gpuVPXR = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * cameraData.GetViewMatrix(xrIdx);

                    cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);

                    // Bypass single pass version
                    LensFlareCommonSRP.ComputeOcclusion(
                        m_Materials.lensFlareDataDriven, camera, cameraData.xr, xrIdx,
                        (float)m_Descriptor.width, (int)m_Descriptor.height,
                        usePanini, paniniDistance, paniniCropToFit, true,
                        camera.transform.position,
                        gpuVPXR,
                        cmd,
                        false, false, null, null);
                }
            }
#endif
        }

        void LensFlareDataDriven(ref UniversalCameraData cameraData, CommandBuffer cmd, RenderTargetIdentifier source, bool usePanini, float paniniDistance, float paniniCropToFit)
        {
            Camera camera = cameraData.camera;
            var pixelRect = new Rect(Vector2.zero, new Vector2(m_Descriptor.width, m_Descriptor.height));

#if ENABLE_VR && ENABLE_XR_MODULE
            // Not VR or Multi-Pass
            if (!cameraData.xr.enabled ||
                (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled))
            {
#endif
                var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                var gpuVP = gpuNonJitteredProj * camera.worldToCameraMatrix;

                LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                    m_Materials.lensFlareDataDriven, camera, pixelRect, cameraData.xr, cameraData.xr.multipassId,
                    (float)m_Descriptor.width, (float)m_Descriptor.height,
                    usePanini, paniniDistance, paniniCropToFit, true,
                    camera.transform.position,
                    gpuVP,
                    cmd,
                    false, false, null, null,
                    source,
                    (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                    false);
#if ENABLE_VR && ENABLE_XR_MODULE
            }
            else // data.hdCamera.xr.enabled && data.hdCamera.xr.singlePassEnabled
            {
                // Bypass single pass version
                for (int xrIdx = 0; xrIdx < cameraData.xr.viewCount; ++xrIdx)
                {
                    Matrix4x4 gpuVPXR = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * cameraData.GetViewMatrix(xrIdx);

                    LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                    m_Materials.lensFlareDataDriven, camera, pixelRect, cameraData.xr, cameraData.xr.multipassId,
                    (float)m_Descriptor.width, (float)m_Descriptor.height,
                    usePanini, paniniDistance, paniniCropToFit, true,
                    camera.transform.position,
                    gpuVPXR,
                    cmd,
                    false, false, null, null,
                    source,
                    (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                    false);
                }
            }
#endif
        }

        #endregion

        #region LensFlareScreenSpace

        void DoLensFlareScreenSpace(Camera camera, CommandBuffer cmd, RenderTargetIdentifier source, RTHandle originalBloomTexture, RTHandle screenSpaceLensFlareBloomMipTexture)
        {
            int ratio = (int)m_LensFlareScreenSpace.resolution.value;

            int width = Mathf.Max(1, (int)m_Descriptor.width / ratio);
            int height = Mathf.Max(1, (int)m_Descriptor.height / ratio);
            var desc = GetCompatibleDescriptor(width, height, m_DefaultColorFormat);

            if (m_LensFlareScreenSpace.IsStreaksActive())
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_StreakTmpTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_StreakTmpTexture");
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_StreakTmpTexture2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_StreakTmpTexture2");
            }

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_ScreenSpaceLensFlareResult, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ScreenSpaceLensFlareResult");

            LensFlareCommonSRP.DoLensFlareScreenSpaceCommon(
                m_Materials.lensFlareScreenSpace,
                camera,
                (float)m_Descriptor.width,
                (float)m_Descriptor.height,
                m_LensFlareScreenSpace.tintColor.value,
                originalBloomTexture,
                screenSpaceLensFlareBloomMipTexture,
                null, // We don't have any spectral LUT in URP
                m_StreakTmpTexture,
                m_StreakTmpTexture2,
                new Vector4(
                    m_LensFlareScreenSpace.intensity.value,
                    m_LensFlareScreenSpace.firstFlareIntensity.value,
                    m_LensFlareScreenSpace.secondaryFlareIntensity.value,
                    m_LensFlareScreenSpace.warpedFlareIntensity.value),
                new Vector4(
                    m_LensFlareScreenSpace.vignetteEffect.value,
                    m_LensFlareScreenSpace.startingPosition.value,
                    m_LensFlareScreenSpace.scale.value,
                    0), // Free slot, not used
                new Vector4(
                    m_LensFlareScreenSpace.samples.value,
                    m_LensFlareScreenSpace.sampleDimmer.value,
                    m_LensFlareScreenSpace.chromaticAbberationIntensity.value,
                    0), // No need to pass a chromatic aberration sample count, hardcoded at 3 in shader
                new Vector4(
                    m_LensFlareScreenSpace.streaksIntensity.value,
                    m_LensFlareScreenSpace.streaksLength.value,
                    m_LensFlareScreenSpace.streaksOrientation.value,
                    m_LensFlareScreenSpace.streaksThreshold.value),
                new Vector4(
                    ratio,
                    m_LensFlareScreenSpace.warpedFlareScale.value.x,
                    m_LensFlareScreenSpace.warpedFlareScale.value.y,
                    0), // Free slot, not used
                cmd,
                m_ScreenSpaceLensFlareResult,
                false);

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, originalBloomTexture);
        }

        #endregion

        #region Motion Blur

        internal static readonly int k_ShaderPropertyId_ViewProjM = Shader.PropertyToID("_ViewProjM");
        internal static readonly int k_ShaderPropertyId_PrevViewProjM = Shader.PropertyToID("_PrevViewProjM");
        internal static readonly int k_ShaderPropertyId_ViewProjMStereo = Shader.PropertyToID("_ViewProjMStereo");
        internal static readonly int k_ShaderPropertyId_PrevViewProjMStereo = Shader.PropertyToID("_PrevViewProjMStereo");

        internal static void UpdateMotionBlurMatrices(ref Material material, Camera camera, XRPass xr)
        {
            MotionVectorsPersistentData motionData = null;

            if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                motionData = additionalCameraData.motionVectorsPersistentData;

            if (motionData == null)
                return;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                material.SetMatrixArray(k_ShaderPropertyId_PrevViewProjMStereo, motionData.previousViewProjectionStereo);
                material.SetMatrixArray(k_ShaderPropertyId_ViewProjMStereo, motionData.viewProjectionStereo);
            }
            else
#endif
            {
                int viewProjMIdx = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled)
                    viewProjMIdx = xr.multipassId;
#endif

                // TODO: These should be part of URP main matrix set. For now, we set them here for motion vector rendering.
                material.SetMatrix(k_ShaderPropertyId_PrevViewProjM, motionData.previousViewProjectionStereo[viewProjMIdx]);
                material.SetMatrix(k_ShaderPropertyId_ViewProjM, motionData.viewProjectionStereo[viewProjMIdx]);
            }
        }


        void DoMotionBlur(CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle motionVectors, ref CameraData cameraData)
        {
            var material = m_Materials.cameraMotionBlur;

            UpdateMotionBlurMatrices(ref material, cameraData.camera, cameraData.xr);

            material.SetFloat("_Intensity", m_MotionBlur.intensity.value);
            material.SetFloat("_Clamp", m_MotionBlur.clamp.value);

            int pass = (int)m_MotionBlur.quality.value;
            var mode = m_MotionBlur.mode.value;
            if (mode == MotionBlurMode.CameraAndObjects)
            {
                Debug.Assert(motionVectors != null, "Motion vectors are invalid. Per-object motion blur requires a motion vector texture.");
                pass += 3;
                material.SetTexture(MotionVectorRenderPass.k_MotionVectorTextureName, motionVectors);
            }

            PostProcessUtils.SetSourceSize(cmd, source);

            CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, pass);
        }

#endregion

#region Panini Projection

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse!
        void DoPaniniProjection(Camera camera, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            var material = m_Materials.paniniProjection;
            material.SetVector(ShaderConstants._Params, new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS));
            material.EnableKeyword(
                1f - Mathf.Abs(paniniD) > float.Epsilon
                ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance
            );
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 0);
        }

        Vector2 CalcViewExtents(Camera camera)
        {
            float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = m_Descriptor.width / (float)m_Descriptor.height;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        Vector2 CalcCropExtents(Camera camera, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;

            var projPos = CalcViewExtents(camera);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

#endregion

#region Bloom

        void SetupBloom(CommandBuffer cmd, RTHandle source, Material uberMaterial, bool enableAlphaOutput)
        {
            // Start at half-res
            int downres = 1;
            switch (m_Bloom.downscale.value)
            {
                case BloomDownscaleMode.Half:
                    downres = 1;
                    break;
                case BloomDownscaleMode.Quarter:
                    downres = 2;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
            int tw = m_Descriptor.width >> downres;
            int th = m_Descriptor.height >> downres;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value);

            // Pre-filtering parameters
            float clamp = m_Bloom.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            var bloomMaterial = m_Materials.bloom;
            bloomMaterial.SetVector(ShaderConstants._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.BloomHQ, m_Bloom.highQualityFiltering.value);
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, enableAlphaOutput);

            // Prefilter
            var desc = GetCompatibleDescriptor(tw, th, m_DefaultColorFormat);
            for (int i = 0; i < mipCount; i++)
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_BloomMipUp[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipUp[i].name);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_BloomMipDown[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipDown[i].name);
                desc.width = Mathf.Max(1, desc.width >> 1);
                desc.height = Mathf.Max(1, desc.height >> 1);
            }

            Blitter.BlitCameraTexture(cmd, source, m_BloomMipDown[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 0);

            // Downsample - gaussian pyramid
            var lastDown = m_BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                Blitter.BlitCameraTexture(cmd, lastDown, m_BloomMipUp[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 1);
                Blitter.BlitCameraTexture(cmd, m_BloomMipUp[i], m_BloomMipDown[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 2);

                lastDown = m_BloomMipDown[i];
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                var lowMip = (i == mipCount - 2) ? m_BloomMipDown[i + 1] : m_BloomMipUp[i + 1];
                var highMip = m_BloomMipDown[i];
                var dst = m_BloomMipUp[i];

                cmd.SetGlobalTexture(ShaderConstants._SourceTexLowMip, lowMip);
                Blitter.BlitCameraTexture(cmd, highMip, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 3);
            }

            // Setup bloom on uber
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);
            uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, m_BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
            var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value;

            if (dirtRatio > screenRatio)
            {
                dirtScaleOffset.x = screenRatio / dirtRatio;
                dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtScaleOffset.y = dirtRatio / screenRatio;
                dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
            }

            uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
            uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
            uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

            // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
            if (m_Bloom.highQualityFiltering.value)
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
        }

#endregion

#region Lens Distortion

        void SetupLensDistortion(Material material, bool isSceneView)
        {
            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            var p1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            var p2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );

            material.SetVector(ShaderConstants._Distortion_Params1, p1);
            material.SetVector(ShaderConstants._Distortion_Params2, p2);

            if (m_LensDistortion.IsActive() && !isSceneView)
                material.EnableKeyword(ShaderKeywordStrings.Distortion);
        }

#endregion

#region Chromatic Aberration

        void SetupChromaticAberration(Material material)
        {
            material.SetFloat(ShaderConstants._Chroma_Params, m_ChromaticAberration.intensity.value * 0.05f);

            if (m_ChromaticAberration.IsActive())
                material.EnableKeyword(ShaderKeywordStrings.ChromaticAberration);
        }

#endregion

#region Vignette

        void SetupVignette(Material material, XRPass xrPass)
        {
            var color = m_Vignette.color.value;
            var center = m_Vignette.center.value;
            var aspectRatio = m_Descriptor.width / (float)m_Descriptor.height;


#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass != null && xrPass.enabled)
            {
                if (xrPass.singlePassEnabled)
                    material.SetVector(ShaderConstants._Vignette_ParamsXR, xrPass.ApplyXRViewCenterOffset(center));
                else
                    // In multi-pass mode we need to modify the eye center with the values from .xy of the corrected
                    // center since the version of the shader that is not single-pass will use the value in _Vignette_Params2
                    center = xrPass.ApplyXRViewCenterOffset(center);
            }
#endif

            var v1 = new Vector4(
                color.r, color.g, color.b,
                m_Vignette.rounded.value ? aspectRatio : 1f
            );
            var v2 = new Vector4(
                center.x, center.y,
                m_Vignette.intensity.value * 3f,
                m_Vignette.smoothness.value * 5f
            );

            material.SetVector(ShaderConstants._Vignette_Params1, v1);
            material.SetVector(ShaderConstants._Vignette_Params2, v2);
        }

#endregion

#region Color Grading

        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material material)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            material.SetTexture(ShaderConstants._InternalLut, m_InternalLut);
            material.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            material.SetTexture(ShaderConstants._UserLut, m_ColorLookup.texture.value);
            material.SetVector(ShaderConstants._UserLut_Params, !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                    1f / m_ColorLookup.texture.value.height,
                    m_ColorLookup.texture.value.height - 1f,
                    m_ColorLookup.contribution.value)
            );

            if (hdr)
            {
                material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                    case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                    default: break; // None
                }
            }
        }

#endregion

#region Film Grain

        void SetupGrain(UniversalCameraData cameraData, Material material)
        {
            if (!m_HasFinalPass && m_FilmGrain.IsActive())
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Data,
                    m_FilmGrain,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }
        }

#endregion

#region 8-bit Dithering

        void SetupDithering(UniversalCameraData cameraData, Material material)
        {
            if (!m_HasFinalPass && cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }
        }

        #endregion

#region HDR Output
        void SetupHDROutput(HDROutputUtils.HDRDisplayInformation hdrDisplayInformation, ColorGamut hdrDisplayColorGamut, Material material, HDROutputUtils.Operation hdrOperations, bool rendersOverlayUI)
        {
            Vector4 hdrOutputLuminanceParams;
            UniversalRenderPipeline.GetHDROutputLuminanceParameters(hdrDisplayInformation, hdrDisplayColorGamut, m_Tonemapping, out hdrOutputLuminanceParams);
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputLuminanceParams);

            HDROutputUtils.ConfigureHDROutput(material, hdrDisplayColorGamut, hdrOperations);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HDROverlay, rendersOverlayUI);
        }
#endregion

#region Final pass

        void RenderFinalPass(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            var material = m_Materials.finalPass;
            material.shaderKeywords = null;

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            PostProcessUtils.SetSourceSize(cmd, cameraData.renderer.cameraColorTargetHandle);
            #pragma warning restore CS0618

            SetupGrain(renderingData.cameraData.universalCameraData, material);
            SetupDithering(renderingData.cameraData.universalCameraData, material);

            if (RequireSRGBConversionBlitToBackBuffer(renderingData.cameraData.requireSrgbConversion))
                material.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            HDROutputUtils.Operation hdrOperations = HDROutputUtils.Operation.None;
            bool requireHDROutput = RequireHDROutput(renderingData.cameraData.universalCameraData);
            if (requireHDROutput)
            {
                // If there is a final post process pass, it's always the final pass so do color encoding
                hdrOperations = m_EnableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
                // If the color space conversion wasn't applied by the uber pass, do it here
                if (!cameraData.postProcessEnabled)
                    hdrOperations |= HDROutputUtils.Operation.ColorConversion;

                SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, material, hdrOperations, cameraData.rendersOverlayUI);
            }

            CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

            DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, cameraData, m_IsFinalPass && !resolveToDebugScreen);

            if (m_UseSwapBuffer)
            {
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                m_Source = cameraData.renderer.GetCameraColorBackBuffer(cmd);
                #pragma warning restore CS0618
            }

            RTHandle sourceTex = m_Source;

            var colorLoadAction = cameraData.isDefaultViewport ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            bool isFxaaEnabled = (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing);

            // FSR is only considered "enabled" when we're performing upscaling. (downscaling uses a linear filter unconditionally)
            bool isFsrEnabled = ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter == ImageUpscalingFilter.FSR));

            // Reuse RCAS pass as an optional standalone post sharpening pass for TAA.
            // This avoids the cost of EASU and is available for other upscaling options.
            // If FSR is enabled then FSR settings override the TAA settings and we perform RCAS only once.
            bool isTaaSharpeningEnabled = (cameraData.IsTemporalAAEnabled() && cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f) && !isFsrEnabled;

            // If target format has alpha and post-process needs to process/output alpha.
            bool isAlphaOutputEnabled = cameraData.isAlphaOutputEnabled;

            if (cameraData.imageScalingMode != ImageScalingMode.None)
            {
                // When FXAA is enabled in scaled renders, we execute it in a separate blit since it's not designed to be used in
                // situations where the input and output resolutions do not match.
                // When FSR is active, we always need an additional pass since it has a very particular color encoding requirement.

                // NOTE: An ideal implementation could inline this color conversion logic into the UberPost pass, but the current code structure would make
                //       this process very complex. Specifically, we'd need to guarantee that the uber post output is always written to a UNORM format render
                //       target in order to preserve the precision of specially encoded color data.
                bool isSetupRequired = (isFxaaEnabled || isFsrEnabled);

                // Make sure to remove any MSAA and attached depth buffers from the temporary render targets
                var tempRtDesc = cameraData.cameraTargetDescriptor;
                tempRtDesc.msaaSamples = 1;
                tempRtDesc.depthStencilFormat = GraphicsFormat.None;

                // Select a UNORM format since we've already performed tonemapping. (Values are in 0-1 range)
                // This improves precision and is required if we want to avoid excessive banding when FSR is in use.
                if (!requireHDROutput)
                    tempRtDesc.graphicsFormat = UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat();

                m_Materials.scalingSetup.shaderKeywords = null;

                if (isSetupRequired)
                {
                    if (requireHDROutput)
                    {
                        SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, m_Materials.scalingSetup, hdrOperations, cameraData.rendersOverlayUI);
                    }

                    if (isFxaaEnabled)
                    {
                        m_Materials.scalingSetup.EnableKeyword(ShaderKeywordStrings.Fxaa);
                    }

                    if (isFsrEnabled)
                    {
                        m_Materials.scalingSetup.EnableKeyword(hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding) ? ShaderKeywordStrings.Gamma20AndHDRInput : ShaderKeywordStrings.Gamma20);
                    }

                    if (isAlphaOutputEnabled)
                    {
                        m_Materials.scalingSetup.EnableKeyword(ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT);
                    }

                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_ScalingSetupTarget, tempRtDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_ScalingSetupTexture");
                    Blitter.BlitCameraTexture(cmd, m_Source, m_ScalingSetupTarget, colorLoadAction, RenderBufferStoreAction.Store, m_Materials.scalingSetup, 0);

                    sourceTex = m_ScalingSetupTarget;
                }

                switch (cameraData.imageScalingMode)
                {
                    case ImageScalingMode.Upscaling:
                    {
                        // In the upscaling case, set material keywords based on the selected upscaling filter
                        // Note: If FSR is enabled, we go down this path regardless of the current render scale. We do this because
                        //       FSR still provides visual benefits at 100% scale. This will also make the transition between 99% and 100%
                        //       scale less obvious for cases where FSR is used with dynamic resolution scaling.
                        switch (cameraData.upscalingFilter)
                        {
                            case ImageUpscalingFilter.Point:
                            {
                                // TAA post sharpening is an RCAS pass, avoid overriding it with point sampling.
                                if(!isTaaSharpeningEnabled)
                                    material.EnableKeyword(ShaderKeywordStrings.PointSampling);
                                break;
                            }

                            case ImageUpscalingFilter.Linear:
                            {
                                // Do nothing as linear is the default filter in the shader
                                break;
                            }

                            case ImageUpscalingFilter.FSR:
                            {
                                m_Materials.easu.shaderKeywords = null;

                                var upscaleRtDesc = cameraData.cameraTargetDescriptor;
                                upscaleRtDesc.msaaSamples = 1;
                                upscaleRtDesc.depthStencilFormat = GraphicsFormat.None;
                                upscaleRtDesc.width = cameraData.pixelWidth;
                                upscaleRtDesc.height = cameraData.pixelHeight;

                                // EASU
                                RenderingUtils.ReAllocateHandleIfNeeded(ref m_UpscaledTarget, upscaleRtDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_UpscaledTexture");
                                var fsrInputSize = new Vector2(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
                                var fsrOutputSize = new Vector2(cameraData.pixelWidth, cameraData.pixelHeight);
                                FSRUtils.SetEasuConstants(cmd, fsrInputSize, fsrInputSize, fsrOutputSize);

                                Blitter.BlitCameraTexture(cmd, sourceTex, m_UpscaledTarget, colorLoadAction, RenderBufferStoreAction.Store, m_Materials.easu, 0);

                                // RCAS
                                // Use the override value if it's available, otherwise use the default.
                                float sharpness = cameraData.fsrOverrideSharpness ? cameraData.fsrSharpness : FSRUtils.kDefaultSharpnessLinear;

                                // Set up the parameters for the RCAS pass unless the sharpness value indicates that it wont have any effect.
                                if (cameraData.fsrSharpness > 0.0f)
                                {
                                    // RCAS is performed during the final post blit, but we set up the parameters here for better logical grouping.
                                    material.EnableKeyword(requireHDROutput ? ShaderKeywordStrings.EasuRcasAndHDRInput : ShaderKeywordStrings.Rcas);
                                    FSRUtils.SetRcasConstantsLinear(cmd, sharpness);
                                }

                                // Update the source texture for the next operation
                                sourceTex = m_UpscaledTarget;
                                PostProcessUtils.SetSourceSize(cmd, m_UpscaledTarget);

                                break;
                            }
                        }

                        break;
                    }

                    case ImageScalingMode.Downscaling:
                    {
                        // In the downscaling case, we don't perform any sort of filter override logic since we always want linear filtering
                        // and it's already the default option in the shader.

                        // Also disable TAA post sharpening pass when downscaling.
                        isTaaSharpeningEnabled = false;

                        break;
                    }
                }
            }
            else if (isFxaaEnabled)
            {
                // In unscaled renders, FXAA can be safely performed in the FinalPost shader
                material.EnableKeyword(ShaderKeywordStrings.Fxaa);
            }

            // Reuse RCAS as a standalone sharpening filter for TAA.
            // If FSR is enabled then it overrides the TAA setting and we skip it.
            if(isTaaSharpeningEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Rcas);
                FSRUtils.SetRcasConstantsLinear(cmd, cameraData.taaSettings.contrastAdaptiveSharpening);
            }

            var cameraTarget = RenderingUtils.GetCameraTargetIdentifier(ref renderingData);

            if (resolveToDebugScreen)
            {
                // Blit to the debugger texture instead of the camera target
                Blitter.BlitCameraTexture(cmd, sourceTex, debugHandler.DebugScreenColorHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, material, 0);
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                cameraData.renderer.ConfigureCameraTarget(debugHandler.DebugScreenColorHandle, debugHandler.DebugScreenDepthHandle);
                #pragma warning restore CS0618
            }
            else
            {
                // Get RTHandle alias to use RTHandle apis
                RTHandleStaticHelpers.SetRTHandleStaticWrapper(cameraTarget);
                var cameraTargetHandle = RTHandleStaticHelpers.s_RTHandleWrapper;
                RenderingUtils.FinalBlit(cmd, cameraData, sourceTex, cameraTargetHandle, colorLoadAction, RenderBufferStoreAction.Store, material, 0);
            }
        }

#endregion

#region Internal utilities

        class MaterialLibrary
        {
            public readonly Material stopNaN;
            public readonly Material subpixelMorphologicalAntialiasing;
            public readonly Material gaussianDepthOfField;
            public readonly Material gaussianDepthOfFieldCoC;
            public readonly Material bokehDepthOfField;
            public readonly Material bokehDepthOfFieldCoC;
            public readonly Material cameraMotionBlur;
            public readonly Material paniniProjection;
            public readonly Material bloom;
            public readonly Material[] bloomUpsample;
            public readonly Material temporalAntialiasing;
            public readonly Material scalingSetup;
            public readonly Material easu;
            public readonly Material uber;
            public readonly Material finalPass;
            public readonly Material lensFlareDataDriven;
            public readonly Material lensFlareScreenSpace;

            public MaterialLibrary(PostProcessData data)
            {
                // NOTE NOTE NOTE NOTE NOTE NOTE
                // If you create something here you must also destroy it in Cleanup()
                // or it will leak during enter/leave play mode cycles
                // NOTE NOTE NOTE NOTE NOTE NOTE
                stopNaN = Load(data.shaders.stopNanPS);
                subpixelMorphologicalAntialiasing = Load(data.shaders.subpixelMorphologicalAntialiasingPS);
                gaussianDepthOfField = Load(data.shaders.gaussianDepthOfFieldPS);
                gaussianDepthOfFieldCoC = Load(data.shaders.gaussianDepthOfFieldPS);
                bokehDepthOfField = Load(data.shaders.bokehDepthOfFieldPS);
                bokehDepthOfFieldCoC = Load(data.shaders.bokehDepthOfFieldPS);
                cameraMotionBlur = Load(data.shaders.cameraMotionBlurPS);
                paniniProjection = Load(data.shaders.paniniProjectionPS);
                bloom = Load(data.shaders.bloomPS);
                temporalAntialiasing = Load(data.shaders.temporalAntialiasingPS);
                scalingSetup = Load(data.shaders.scalingSetupPS);
                easu = Load(data.shaders.easuPS);
                uber = Load(data.shaders.uberPostPS);
                finalPass = Load(data.shaders.finalPostPassPS);
                lensFlareDataDriven = Load(data.shaders.LensFlareDataDrivenPS);
                lensFlareScreenSpace = Load(data.shaders.LensFlareScreenSpacePS);

                bloomUpsample = new Material[k_MaxPyramidSize];
                for (uint i = 0; i < k_MaxPyramidSize; ++i)
                    bloomUpsample[i] = Load(data.shaders.bloomPS);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. PostProcessing render passes will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(stopNaN);
                CoreUtils.Destroy(subpixelMorphologicalAntialiasing);
                CoreUtils.Destroy(gaussianDepthOfField);
                CoreUtils.Destroy(gaussianDepthOfFieldCoC);
                CoreUtils.Destroy(bokehDepthOfField);
                CoreUtils.Destroy(bokehDepthOfFieldCoC);
                CoreUtils.Destroy(cameraMotionBlur);
                CoreUtils.Destroy(paniniProjection);
                CoreUtils.Destroy(bloom);
                CoreUtils.Destroy(temporalAntialiasing);
                CoreUtils.Destroy(scalingSetup);
                CoreUtils.Destroy(easu);
                CoreUtils.Destroy(uber);
                CoreUtils.Destroy(finalPass);
                CoreUtils.Destroy(lensFlareDataDriven);
                CoreUtils.Destroy(lensFlareScreenSpace);

                for (uint i = 0; i < k_MaxPyramidSize; ++i)
                    CoreUtils.Destroy(bloomUpsample[i]);
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
            public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int _PongTexture = Shader.PropertyToID("_PongTexture");
            public static readonly int _PingTexture = Shader.PropertyToID("_PingTexture");

            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");

            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Vignette_ParamsXR = Shader.PropertyToID("_Vignette_ParamsXR");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int _FlareOcclusionRemapTex = Shader.PropertyToID("_FlareOcclusionRemapTex");
            public static readonly int _FlareOcclusionTex = Shader.PropertyToID("_FlareOcclusionTex");
            public static readonly int _FlareOcclusionIndex = Shader.PropertyToID("_FlareOcclusionIndex");
            public static readonly int _FlareTex = Shader.PropertyToID("_FlareTex");
            public static readonly int _FlareColorValue = Shader.PropertyToID("_FlareColorValue");
            public static readonly int _FlareData0 = Shader.PropertyToID("_FlareData0");
            public static readonly int _FlareData1 = Shader.PropertyToID("_FlareData1");
            public static readonly int _FlareData2 = Shader.PropertyToID("_FlareData2");
            public static readonly int _FlareData3 = Shader.PropertyToID("_FlareData3");
            public static readonly int _FlareData4 = Shader.PropertyToID("_FlareData4");
            public static readonly int _FlareData5 = Shader.PropertyToID("_FlareData5");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }

#endregion
    }
}
