using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices;  // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class PostProcess : IDisposable
    {
        // Passes
        StopNanPostProcessPass m_StopNanPostProcessPass;
        SmaaPostProcessPass m_SmaaPostProcessPass;
        DepthOfFieldGaussianPostProcessPass m_DepthOfFieldGaussianPass;
        DepthOfFieldBokehPostProcessPass m_DepthOfFieldBokehPass;
        UpscalerPostProcessPass m_UpscalerPostProcessPass;
        StpPostProcessPass m_StpPostProcessPass;
        TemporalAntiAliasingPostProcessPass m_TemporalAntiAliasingPass;
        MotionBlurPostProcessPass m_MotionBlurPass;
        PaniniProjectionPostProcessPass m_PaniniProjectionPass;
        BloomPostProcessPass m_BloomPass;
        LensFlareScreenSpacePostProcessPass m_LensFlareScreenSpacePass;
        LensFlareDataDrivenPostProcessPass m_LensFlareDataDrivenPass;
        UberPostProcessPass m_UberPass;

        // Final Passes
        ScalingSetupPostProcessPass m_ScalingSetupFinalPostProcessPass;
        Fsr1UpscalePostProcessPass m_Fsr1UpscaleFinalPostProcessPass;
        FinalPostProcessPass m_FinalPostProcessPass;

        PostProcessData m_Resources;

        RTHandle m_UserLut;
        RTHandle m_InternalLut;

        int m_DitheringTextureIndex;    // 8-bit dithering

        /// <summary>
        /// Creates a new <c>PostProcessPass</c> instance.
        /// </summary>
        /// <param name="postProcessResourceAssetData">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="requestPostProColorFormat">Requested <c>GraphicsFormat</c> for postprocess rendering.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="PostProcessData"/>
        /// <seealso cref="PostProcessParams"/>
        /// <seealso cref="GraphicsFormat"/>
        public PostProcess(PostProcessData postProcessResourceAssetData)
        {
            Assertions.Assert.IsNotNull(postProcessResourceAssetData, "PostProcessData and resources cannot be null.");
            m_Resources = postProcessResourceAssetData;

            m_StopNanPostProcessPass   = new StopNanPostProcessPass(m_Resources.shaders.stopNanPS);
            m_SmaaPostProcessPass      = new SmaaPostProcessPass(m_Resources.shaders.subpixelMorphologicalAntialiasingPS, m_Resources.textures.smaaAreaTex, m_Resources.textures.smaaSearchTex);
            m_DepthOfFieldGaussianPass = new DepthOfFieldGaussianPostProcessPass(m_Resources.shaders.gaussianDepthOfFieldPS);
            m_DepthOfFieldBokehPass    = new DepthOfFieldBokehPostProcessPass(m_Resources.shaders.bokehDepthOfFieldPS);
            m_UpscalerPostProcessPass  = new UpscalerPostProcessPass(m_Resources.textures.blueNoise16LTex);
            m_StpPostProcessPass       = new StpPostProcessPass(m_Resources.textures.blueNoise16LTex);
            m_TemporalAntiAliasingPass = new TemporalAntiAliasingPostProcessPass(m_Resources.shaders.temporalAntialiasingPS);
            m_MotionBlurPass           = new MotionBlurPostProcessPass(m_Resources.shaders.cameraMotionBlurPS);
            m_PaniniProjectionPass     = new PaniniProjectionPostProcessPass(m_Resources.shaders.paniniProjectionPS);
            m_BloomPass                = new BloomPostProcessPass(m_Resources.shaders.bloomPS);
            m_LensFlareScreenSpacePass = new LensFlareScreenSpacePostProcessPass(m_Resources.shaders.LensFlareScreenSpacePS);
            m_LensFlareDataDrivenPass  = new LensFlareDataDrivenPostProcessPass(m_Resources.shaders.LensFlareDataDrivenPS);
            m_UberPass                 = new UberPostProcessPass(m_Resources.shaders.uberPostPS, m_Resources.textures.filmGrainTex);

            // Final post processing.
            m_ScalingSetupFinalPostProcessPass = new ScalingSetupPostProcessPass(m_Resources.shaders.scalingSetupPS);
            m_Fsr1UpscaleFinalPostProcessPass  = new Fsr1UpscalePostProcessPass(m_Resources.shaders.easuPS);
            m_FinalPostProcessPass             = new FinalPostProcessPass(m_Resources.shaders.finalPostPassPS, m_Resources.textures.filmGrainTex);
        }

        /// <summary>
        /// Disposes used resources.
        /// </summary>
        public void Dispose()
        {
            m_FinalPostProcessPass?.Dispose();
            m_Fsr1UpscaleFinalPostProcessPass?.Dispose();
            m_ScalingSetupFinalPostProcessPass?.Dispose();

            m_UberPass?.Dispose();
            m_LensFlareDataDrivenPass?.Dispose();
            m_LensFlareScreenSpacePass?.Dispose();
            m_BloomPass?.Dispose();
            m_PaniniProjectionPass?.Dispose();
            m_MotionBlurPass?.Dispose();
            m_TemporalAntiAliasingPass?.Dispose();
            m_StpPostProcessPass?.Dispose();
            m_DepthOfFieldBokehPass?.Dispose();
            m_DepthOfFieldGaussianPass?.Dispose();
            m_SmaaPostProcessPass?.Dispose();
            m_StopNanPostProcessPass?.Dispose();

            m_UserLut?.Release();
        }

        // Some Android devices do not support sRGB backbuffer
        // We need to do the conversion manually on those
        // Also if HDR output is active
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool RequireSRGBConversionBlitToBackBuffer(UniversalCameraData cameraData, bool enableColorEncodingIfNeeded)
        {
            return cameraData.requireSrgbConversion && enableColorEncodingIfNeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetNextDitherIndex()
        {
#if LWRP_DEBUG_STATIC_POSTFX // Used by QA for automated testing
            return 0;
#else
            if (++m_DitheringTextureIndex >= m_Resources.textures.blueNoise16LTex.Length)
                m_DitheringTextureIndex = 0;
            return m_DitheringTextureIndex;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Texture2D GetNextDitherTexture()
        {
            return m_Resources.textures.blueNoise16LTex[GetNextDitherIndex()];
        }

        TextureHandle TryGetCachedUserLutTextureHandle(RenderGraph renderGraph, ColorLookup colorLookup)
        {
            if (colorLookup.texture.value == null)
            {
                if (m_UserLut != null)
                {
                    m_UserLut.Release();
                    m_UserLut = null;
                }
            }
            else
            {
                if (m_UserLut == null || m_UserLut.externalTexture != colorLookup.texture.value)
                {
                    m_UserLut?.Release();
                    m_UserLut = RTHandles.Alloc(colorLookup.texture.value);
                }
            }
            return m_UserLut != null ? renderGraph.ImportTexture(m_UserLut) : TextureHandle.nullHandle;
        }

        static void UpdateGlobalDebugHandlerPass(RenderGraph renderGraph, UniversalCameraData cameraData, bool isFinalPass)
        {
            // NOTE: Debug handling injects a global state render pass.
            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, isFinalPass && !resolveToDebugScreen);
        }

        const string _CameraColorUpscaled = "_CameraColorUpscaled";
        const string _CameraColorAfterPostProcessingName = "_CameraColorAfterPostProcessing";

        // If postProcessingTarget is not valid then this function will create an RG managed texture. Only pass postProcessingTarget if the output needs to be written to a certain persistent texture.
        // If hasFinalPass == true, Film Grain and Dithering are setup in the final pass, otherwise they are setup in this pass.
        public TextureHandle RenderPostProcessing(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle activeCameraColorTexture, in TextureHandle internalColorLutTexture, in TextureHandle overlayUITexture, in TextureHandle persistentTarget, bool hasFinalPass, bool enableColorEncodingIfNeeded)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            var stack = VolumeManager.instance.stack;
            var depthOfField = stack.GetComponent<DepthOfField>();
            var motionBlur = stack.GetComponent<MotionBlur>();
            var paniniProjection = stack.GetComponent<PaniniProjection>();
            var bloom = stack.GetComponent<Bloom>();
            var lensFlareScreenSpace = stack.GetComponent<ScreenSpaceLensFlare>();
            var lensDistortion = stack.GetComponent<LensDistortion>();
            var chromaticAberration = stack.GetComponent<ChromaticAberration>();
            var vignette = stack.GetComponent<Vignette>();
            var colorLookup = stack.GetComponent<ColorLookup>();
            var colorAdjustments = stack.GetComponent<ColorAdjustments>();
            var tonemapping = stack.GetComponent<Tonemapping>();
            var filmGrain = stack.GetComponent<FilmGrain>();

            bool useFastSRGBLinearConversion = postProcessingData.useFastSRGBLinearConversion;
            bool supportDataDrivenLensFlare = postProcessingData.supportDataDrivenLensFlare;
            bool supportScreenSpaceLensFlare = postProcessingData.supportScreenSpaceLensFlare;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            //We blit back and forth without msaa untill the last blit.
            bool useStopNan = cameraData.isStopNaNEnabled && m_StopNanPostProcessPass.IsValid();
            bool useSubPixelMorpAA = (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing) && m_SmaaPostProcessPass.IsValid();
            bool useDepthOfField = depthOfField.IsActive() && !isSceneViewCamera && (m_DepthOfFieldGaussianPass.IsValid() || m_DepthOfFieldBokehPass.IsValid());
            bool useLensFlare = !LensFlareCommonSRP.Instance.IsEmpty() && supportDataDrivenLensFlare;
            bool useLensFlareScreenSpace = lensFlareScreenSpace.IsActive() && supportScreenSpaceLensFlare;
            bool useMotionBlur = motionBlur.IsActive() && !isSceneViewCamera && m_MotionBlurPass.IsValid();
            bool usePaniniProjection = paniniProjection.IsActive() && !isSceneViewCamera && m_PaniniProjectionPass.IsValid();

            // Disable MotionBlur in EditMode, so that editing remains clear and readable.
            // NOTE: HDRP does the same via CoreUtils::AreAnimatedMaterialsEnabled().
            // Disable MotionBlurMode.CameraAndObjects on renderers that do not support motion vectors
            useMotionBlur = useMotionBlur && Application.isPlaying;
            if (useMotionBlur && motionBlur.mode.value == MotionBlurMode.CameraAndObjects)
            {
                ScriptableRenderer renderer = cameraData.renderer;
                useMotionBlur &= renderer.SupportsMotionVectors();
                if (!useMotionBlur)
                {
                    var warning = "Disabling Motion Blur for Camera And Objects because the renderer does not implement motion vectors.";
                    const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
                    if (Time.frameCount % warningThrottleFrames == 0)
                        Debug.LogWarning(warning);
                }
            }

            // Note that enabling jitters uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides (like
            // disable useTemporalAA if another feature is disabled) then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking the value here.
            bool useTemporalAA = cameraData.IsTemporalAAEnabled() && m_TemporalAntiAliasingPass.IsValid();

            // STP is only enabled when TAA is enabled and all of its runtime requirements are met.
            // Using IsSTPRequested() vs IsSTPEnabled() for perf reason here, as we already know TAA status
            bool isSTPRequested = cameraData.IsSTPRequested();
            bool useSTP = useTemporalAA && isSTPRequested;

            // Warn users if TAA and STP are disabled despite being requested
            if (!useTemporalAA && cameraData.IsTemporalAARequested())
                TemporalAA.ValidateAndWarn(cameraData, isSTPRequested);

            // NOTE: Debug handling injects a global state render pass.
            UpdateGlobalDebugHandlerPass(renderGraph, cameraData, !hasFinalPass);

            TextureHandle currentSource = activeCameraColorTexture;

            // Optional NaN killer before post-processing kicks in
            // stopNaN may be null on Adreno 3xx. It doesn't support full shader level 3.5, but SystemInfo.graphicsShaderLevel is 35.
            if (useStopNan)
            {
                var stopNanTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, currentSource, StopNanPostProcessPass.k_TargetName, true, FilterMode.Bilinear);
                m_StopNanPostProcessPass.sourceTexture = currentSource;
                m_StopNanPostProcessPass.destinationTexture = stopNanTarget;
                m_StopNanPostProcessPass.RecordRenderGraph(renderGraph, frameData);
                currentSource = m_StopNanPostProcessPass.destinationTexture;
            }

            if(useSubPixelMorpAA)
            {
                var targetDesc = renderGraph.GetTextureDesc(currentSource);
                var smaaTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, targetDesc, SmaaPostProcessPass.k_TargetName, true, FilterMode.Bilinear);
                m_SmaaPostProcessPass.antialiasingQuality = cameraData.antialiasingQuality;
                m_SmaaPostProcessPass.sourceTexture = currentSource;
                m_SmaaPostProcessPass.destinationTexture = smaaTarget;
                m_SmaaPostProcessPass.RecordRenderGraph(renderGraph, frameData);
                currentSource = m_SmaaPostProcessPass.destinationTexture;
            }

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            if (useDepthOfField)
            {
                    var doFTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, currentSource, DepthOfFieldGaussianPostProcessPass.k_TargetName, true, FilterMode.Bilinear);
                    if(depthOfField.mode.value == DepthOfFieldMode.Gaussian)
                    {
                        m_DepthOfFieldGaussianPass.depthOfField = depthOfField;
                        m_DepthOfFieldGaussianPass.sourceTexture = currentSource;
                        m_DepthOfFieldGaussianPass.destinationTexture = doFTarget;
                        m_DepthOfFieldGaussianPass.RecordRenderGraph(renderGraph, frameData);
                    }
                    else
                    {
                        m_DepthOfFieldBokehPass.depthOfField = depthOfField;
                        m_DepthOfFieldBokehPass.useFastSRGBLinearConversion = useFastSRGBLinearConversion;
                        m_DepthOfFieldBokehPass.sourceTexture = currentSource;
                        m_DepthOfFieldBokehPass.destinationTexture = doFTarget;
                        m_DepthOfFieldBokehPass.RecordRenderGraph(renderGraph, frameData);
                    }
                    currentSource = doFTarget;
            }

            // Temporal Anti Aliasing / Upscaling

            if (useTemporalAA)
            {
#if ENABLE_UPSCALER_FRAMEWORK
                if (postProcessingData.activeUpscaler != null)
                {
                    // TODO: The caller of the pass should create the upscaled target. Caller should have the control of the placement of the result.
                    m_UpscalerPostProcessPass.sourceTexture = currentSource;
                    m_UpscalerPostProcessPass.RecordRenderGraph(renderGraph, frameData);
                    currentSource = m_UpscalerPostProcessPass.destinationTexture;
                }
                else
#endif
                if (useSTP)
                {
                    var srcDesc = renderGraph.GetTextureDesc(currentSource);
                    var dstDesc = StpPostProcessPass.GetStpTargetDesc(srcDesc, cameraData);
                    var stpTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, dstDesc, StpPostProcessPass.k_UpscaledColorTargetName, false, FilterMode.Bilinear);

                    m_StpPostProcessPass.sourceTexture = currentSource;
                    m_StpPostProcessPass.destinationTexture = stpTarget;
                    m_StpPostProcessPass.RecordRenderGraph(renderGraph, frameData);
                    currentSource = m_StpPostProcessPass.destinationTexture;
                }
                else
                {
                    var taaTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, currentSource, TemporalAntiAliasingPostProcessPass.k_TargetName, false, FilterMode.Bilinear);

                    m_TemporalAntiAliasingPass.sourceTexture = currentSource;
                    m_TemporalAntiAliasingPass.destinationTexture = taaTarget;
                    m_TemporalAntiAliasingPass.RecordRenderGraph(renderGraph, frameData);
                    currentSource = m_TemporalAntiAliasingPass.destinationTexture;
                }
            }

            if(useMotionBlur)
            {
                var motionTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, currentSource, MotionBlurPostProcessPass.k_TargetName, true, FilterMode.Bilinear);

                m_MotionBlurPass.motionBlur = motionBlur;
                m_MotionBlurPass.sourceTexture = currentSource;
                m_MotionBlurPass.destinationTexture = motionTarget;
                m_MotionBlurPass.RecordRenderGraph(renderGraph, frameData);
                currentSource = m_MotionBlurPass.destinationTexture;
            }

            if(usePaniniProjection)
            {
                var paniniTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, currentSource, PaniniProjectionPostProcessPass.k_TargetName, true, FilterMode.Bilinear);

                m_PaniniProjectionPass.paniniProjection = paniniProjection;
                m_PaniniProjectionPass.sourceTexture = currentSource;
                m_PaniniProjectionPass.destinationTexture = paniniTarget;
                m_PaniniProjectionPass.RecordRenderGraph(renderGraph, frameData);
                currentSource = m_PaniniProjectionPass.destinationTexture;
            }

            // Uberpost
            {
                var colorSrcDesc = currentSource.GetDescriptor(renderGraph);

                // Bloom goes first
                TextureHandle bloomTexture = TextureHandle.nullHandle;
                bool bloomActive = bloom.IsActive() || useLensFlareScreenSpace;
                //Even if bloom is not active we need the texture if the lensFlareScreenSpace pass is active.
                if (bloomActive)
                {
                    // NOTE: bloom destination texture is some texture in the bloom mip pyramid. It's not explicitly set beforehand.
                    m_BloomPass.bloom = bloom;
                    m_BloomPass.sourceTexture = currentSource;
                    m_BloomPass.RecordRenderGraph(renderGraph, frameData);
                    bloomTexture = m_BloomPass.destinationTexture;

                    if (useLensFlareScreenSpace)
                    {
                        var mipPyramid = m_BloomPass.mipPyramid;

                        // We need to take into account how many valid mips the bloom pass produced.
                        int bloomMipCount = mipPyramid.mipCount;
                        int maxBloomMip = Mathf.Clamp(bloomMipCount - 1, 0, bloom.maxIterations.value / 2);
                        int useBloomMip = Mathf.Clamp(lensFlareScreenSpace.bloomMip.value, 0, maxBloomMip);

                        TextureHandle bloomMipFlareSource = mipPyramid.GetResultMip(useBloomMip);
                        // Flare source and Flare target is the same texture. BloomMip[0]
                        bool sameBloomSrcDestTex = useBloomMip == 0;

                        // Kawase blur does not use the mip pyramid.
                        // It is safe to pass the same texture to both input/output.
                        if (bloom.filter.value == BloomFilterMode.Kawase)
                        {
                            bloomMipFlareSource = bloomTexture;
                            sameBloomSrcDestTex = true;
                        }

                        m_LensFlareScreenSpacePass.lensFlareScreenSpace = lensFlareScreenSpace;
                        m_LensFlareScreenSpacePass.sameSourceDestinationTexture = sameBloomSrcDestTex;
                        m_LensFlareScreenSpacePass.colorBufferTextureDesc = colorSrcDesc;
                        m_LensFlareScreenSpacePass.sourceTexture = bloomMipFlareSource;
                        m_LensFlareScreenSpacePass.destinationTexture = bloomTexture;
                        m_LensFlareScreenSpacePass.RecordRenderGraph(renderGraph, frameData);
                    }
                }

                if (useLensFlare)
                {
                    // Lens Flares are procedurally generated and blended to the destination texture.
                    m_LensFlareDataDrivenPass.paniniProjection = paniniProjection;
                    m_LensFlareDataDrivenPass.destinationTexture = currentSource;
                    m_LensFlareDataDrivenPass.RecordRenderGraph(renderGraph, frameData);
                }

                // Settings
                m_UberPass.colorLookup = colorLookup;
                m_UberPass.colorAdjustments = colorAdjustments;
                m_UberPass.tonemapping = tonemapping;
                m_UberPass.bloom = bloom;
                m_UberPass.lensDistortion = lensDistortion;
                m_UberPass.chromaticAberration = chromaticAberration;
                m_UberPass.vignette = vignette;
                m_UberPass.filmGrain = filmGrain;

                m_UberPass.isFinalPass = !hasFinalPass;
                m_UberPass.requireSRGBConversionBlit = RequireSRGBConversionBlitToBackBuffer(cameraData, enableColorEncodingIfNeeded);
                m_UberPass.useFastSRGBLinearConversion = useFastSRGBLinearConversion;

                TextureHandle activeOverlayUITexture = TextureHandle.nullHandle;
                bool requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
                if (requireHDROutput)
                {
                    // Color space conversion is already applied through color grading, do encoding if uber post is the last pass
                    // Otherwise encoding will happen in the final post process pass or the final blit pass
                    m_UberPass.hdrOperations = !hasFinalPass && enableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;

                    if(enableColorEncodingIfNeeded && overlayUITexture.IsValid())
                        activeOverlayUITexture = overlayUITexture;
                }

                // Input
                m_UberPass.sourceTexture = currentSource;
                m_UberPass.internalLutTexture = internalColorLutTexture;
                m_UberPass.userLutTexture = TryGetCachedUserLutTextureHandle(renderGraph, colorLookup);
                m_UberPass.bloomTexture = bloomTexture;
                m_UberPass.overlayUITexture = activeOverlayUITexture;
                m_UberPass.ditherTexture = cameraData.isDitheringEnabled ? GetNextDitherTexture() : null;

                if (persistentTarget.IsValid())
                {
                    m_UberPass.destinationTexture = persistentTarget;
                }else
                {
                    m_UberPass.destinationTexture = renderGraph.CreateTexture(m_UberPass.sourceTexture, _CameraColorAfterPostProcessingName);                   
                }

                m_UberPass.RecordRenderGraph(renderGraph, frameData);

                return m_UberPass.destinationTexture;
            }
        }

#region FinalPass
        public void RenderFinalPostProcessing(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle source, in TextureHandle overlayUITexture, in TextureHandle postProcessingTarget, bool enableColorEncodingIfNeeded)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var stack = VolumeManager.instance.stack;
            var tonemapping = stack.GetComponent<Tonemapping>();
            var filmGrain = stack.GetComponent<FilmGrain>();

            // NOTE: Debug handling injects a global state render pass.
            UpdateGlobalDebugHandlerPass(renderGraph, cameraData, true);

            var srcDesc = renderGraph.GetTextureDesc(source);

            HDROutputUtils.Operation hdrOperations = HDROutputUtils.Operation.None;
            bool requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
            if (requireHDROutput)
            {
                // Final post process pass always does color encoding
                hdrOperations = enableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
                // If the color space conversion wasn't applied by the uber pass, do it here
                if (!cameraData.postProcessEnabled)
                    hdrOperations |= HDROutputUtils.Operation.ColorConversion;
            }

            FinalPostProcessPass.SamplingOperation samplingOperation = FinalPostProcessPass.SamplingOperation.Linear;

            // Reuse RCAS pass as an optional standalone post sharpening pass for TAA.
            // This avoids the cost of EASU and is available for other upscaling options.
            // If FSR is enabled then FSR settings override the TAA settings and we perform RCAS only once.
            // If STP is enabled, then TAA sharpening has already been performed inside STP.
            if(PostProcessUtils.IsTaaSharpeningEnabled(cameraData))
                samplingOperation = FinalPostProcessPass.SamplingOperation.TaaSharpening;

            bool applyFxaa = PostProcessUtils.IsFxaaEnabled(cameraData);

            var currentSource = source;
            if (cameraData.imageScalingMode != ImageScalingMode.None)
            {
                // When FXAA is enabled in scaled renders, we execute it in a separate blit since it's not designed to be used in
                // situations where the input and output resolutions do not match.
                // When FSR is active, we always need an additional pass since it has a very particular color encoding requirement.

                // NOTE: An ideal implementation could inline this color conversion logic into the UberPost pass, but the current code structure would make
                //       this process very complex. Specifically, we'd need to guarantee that the uber post output is always written to a UNORM format render
                //       target in order to preserve the precision of specially encoded color data.
                bool isSetupRequired = (applyFxaa || PostProcessUtils.IsFsrEnabled(cameraData));

                // When FXAA is needed while scaling is active, we must perform it before the scaling takes place.
                if (isSetupRequired)
                {
                    var scalingSetupDesc = srcDesc;
                    if (!requireHDROutput)
                    {
                        // Select a UNORM format since we've already performed tonemapping. (Values are in 0-1 range)
                        // This improves precision and is required if we want to avoid excessive banding when FSR is in use.
                        scalingSetupDesc.format = UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat();
                    }

                    var scalingSetupTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, scalingSetupDesc, ScalingSetupPostProcessPass.k_TargetName, true, FilterMode.Point);

                    m_ScalingSetupFinalPostProcessPass.tonemapping = tonemapping;
                    m_ScalingSetupFinalPostProcessPass.hdrOperations = hdrOperations;

                    m_ScalingSetupFinalPostProcessPass.sourceTexture = currentSource;
                    m_ScalingSetupFinalPostProcessPass.destinationTexture = scalingSetupTarget;
                    m_ScalingSetupFinalPostProcessPass.RecordRenderGraph(renderGraph,frameData);

                    currentSource = m_ScalingSetupFinalPostProcessPass.destinationTexture;

                    // Indicate that we no longer need to perform FXAA in the final pass since it was already perfomed here.
                    applyFxaa = false;
                }

                // Upscaling (and downscaling)

                var upscaledDesc = srcDesc;
                upscaledDesc.width = cameraData.pixelWidth;
                upscaledDesc.height = cameraData.pixelHeight;
                var upScaleTarget = PostProcessUtils.CreateCompatibleTexture(renderGraph, upscaledDesc, "_UpscaledTexture", true, FilterMode.Point);

                switch (cameraData.imageScalingMode)
                {
                    case ImageScalingMode.Upscaling:
                    {
                        switch (cameraData.upscalingFilter)
                        {
                            case ImageUpscalingFilter.Point:
                            {
                                // TAA post sharpening is an RCAS pass, avoid overriding it with point sampling.
                                if (samplingOperation != FinalPostProcessPass.SamplingOperation.TaaSharpening)
                                    samplingOperation = FinalPostProcessPass.SamplingOperation.Point;
                                break;
                            }
                            case ImageUpscalingFilter.Linear:
                            {
                                break;
                            }
                            case ImageUpscalingFilter.FSR:
                            {
                                m_Fsr1UpscaleFinalPostProcessPass.sourceTexture = currentSource;
                                m_Fsr1UpscaleFinalPostProcessPass.destinationTexture = upScaleTarget;
                                m_Fsr1UpscaleFinalPostProcessPass.RecordRenderGraph(renderGraph,frameData);
                                currentSource = m_Fsr1UpscaleFinalPostProcessPass.destinationTexture;
                                samplingOperation = FinalPostProcessPass.SamplingOperation.FsrSharpening;
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
                        samplingOperation = FinalPostProcessPass.SamplingOperation.Linear;
                        break;
                    }
                }
            }

            bool renderOverlayUI = requireHDROutput && enableColorEncodingIfNeeded && cameraData.rendersOverlayUI;

            m_FinalPostProcessPass.tonemapping = tonemapping;
            m_FinalPostProcessPass.filmGrain = filmGrain;

            m_FinalPostProcessPass.samplingOperation = samplingOperation;
            m_FinalPostProcessPass.applyFxaa = applyFxaa;
            m_FinalPostProcessPass.applySrgbEncoding = RequireSRGBConversionBlitToBackBuffer(cameraData, enableColorEncodingIfNeeded);
            m_FinalPostProcessPass.hdrOperations = hdrOperations;

            m_FinalPostProcessPass.sourceTexture = currentSource;
            m_FinalPostProcessPass.overlayUITexture = renderOverlayUI ? overlayUITexture : TextureHandle.nullHandle;
            m_FinalPostProcessPass.ditherTexture = cameraData.isDitheringEnabled ? GetNextDitherTexture() : null;

            m_FinalPostProcessPass.destinationTexture = postProcessingTarget;
            m_FinalPostProcessPass.RecordRenderGraph(renderGraph,frameData);
            currentSource = TextureHandle.nullHandle;
        }
#endregion
    }
}
