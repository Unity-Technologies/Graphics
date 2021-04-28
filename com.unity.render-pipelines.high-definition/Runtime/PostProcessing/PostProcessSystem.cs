using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    // Main class for all post-processing related features - only includes camera effects, no
    // lighting/surface effect like SSR/AO
    sealed partial class PostProcessSystem
    {
        private enum SMAAStage
        {
            EdgeDetection = 0,
            BlendWeights = 1,
            NeighborhoodBlending = 2
        }

        GraphicsFormat m_ColorFormat            = GraphicsFormat.B10G11R11_UFloatPack32;
        const GraphicsFormat k_CoCFormat        = GraphicsFormat.R16_SFloat;
        internal const GraphicsFormat k_ExposureFormat   = GraphicsFormat.R32G32_SFloat;

        readonly RenderPipelineResources m_Resources;
        Material m_FinalPassMaterial;
        Material m_ClearBlackMaterial;
        Material m_SMAAMaterial;
        Material m_TemporalAAMaterial;

        MaterialPropertyBlock m_TAAHistoryBlitPropertyBlock = new MaterialPropertyBlock();
        MaterialPropertyBlock m_TAAPropertyBlock = new MaterialPropertyBlock();

        // Exposure data
        const int k_ExposureCurvePrecision = 128;
        const int k_HistogramBins          = 128;   // Important! If this changes, need to change HistogramExposure.compute
        const int k_DebugImageHistogramBins = 256;   // Important! If this changes, need to change HistogramExposure.compute
        readonly Color[] m_ExposureCurveColorArray = new Color[k_ExposureCurvePrecision];
        readonly int[] m_ExposureVariants = new int[4];

        Texture2D m_ExposureCurveTexture;
        RTHandle m_EmptyExposureTexture; // RGHalf
        RTHandle m_DebugExposureData;
        ComputeBuffer m_HistogramBuffer;
        ComputeBuffer m_DebugImageHistogramBuffer;
        readonly int[] m_EmptyHistogram = new int[k_HistogramBins];
        readonly int[] m_EmptyDebugImageHistogram = new int[k_DebugImageHistogramBins * 4];

        // Depth of field data
        ComputeBuffer m_BokehNearKernel;
        ComputeBuffer m_BokehFarKernel;
        ComputeBuffer m_BokehIndirectCmd;
        ComputeBuffer m_NearBokehTileList;
        ComputeBuffer m_FarBokehTileList;

        //  AMD-CAS data
        ComputeBuffer m_ContrastAdaptiveSharpen;

        // Bloom data
        const int k_MaxBloomMipCount = 16;
        readonly Vector4[] m_BloomMipsInfo = new Vector4[k_MaxBloomMipCount + 1];   // xy: size, zw: scale
        int m_BloomMipCount = k_MaxBloomMipCount;

        // Chromatic aberration data
        Texture2D m_InternalSpectralLut;

        // Color grading data
        readonly int m_LutSize;
        readonly GraphicsFormat m_LutFormat;
        readonly HableCurve m_HableCurve;

        // Prefetched components (updated on every frame)
        Exposure m_Exposure;
        DepthOfField m_DepthOfField;
        MotionBlur m_MotionBlur;
        PaniniProjection m_PaniniProjection;
        Bloom m_Bloom;
        ChromaticAberration m_ChromaticAberration;
        LensDistortion m_LensDistortion;
        Vignette m_Vignette;
        Tonemapping m_Tonemapping;
        WhiteBalance m_WhiteBalance;
        ColorAdjustments m_ColorAdjustments;
        ChannelMixer m_ChannelMixer;
        SplitToning m_SplitToning;
        LiftGammaGain m_LiftGammaGain;
        ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;
        ColorCurves m_Curves;
        FilmGrain m_FilmGrain;
        PathTracing m_PathTracing;

        // Prefetched frame settings (updated on every frame)
        bool m_StopNaNFS;
        bool m_DepthOfFieldFS;
        bool m_MotionBlurFS;
        bool m_PaniniProjectionFS;
        bool m_BloomFS;
        bool m_ChromaticAberrationFS;
        bool m_LensDistortionFS;
        bool m_VignetteFS;
        bool m_ColorGradingFS;
        bool m_TonemappingFS;
        bool m_FilmGrainFS;
        bool m_DitheringFS;
        bool m_AntialiasingFS;

        // Debug Exposure compensation (Drive by debug menu) to add to all exposure processed value
        float m_DebugExposureCompensation;

        // Physical camera ref
        HDPhysicalCamera m_PhysicalCamera;
        static readonly HDPhysicalCamera m_DefaultPhysicalCamera = new HDPhysicalCamera();

        // HDRP has the following behavior regarding alpha:
        // - If post processing is disabled, the alpha channel of the rendering passes (if any) will be passed to the frame buffer by the final pass
        // - If post processing is enabled, then post processing passes will either copy (exposure, color grading, etc) or process (DoF, TAA, etc) the alpha channel, if one exists.
        // If the user explicitly requests a color buffer without alpha for post-processing (for performance reasons) but the rendering passes have alpha, then the alpha will be copied.
        readonly bool m_EnableAlpha;
        readonly bool m_KeepAlpha;

        readonly bool m_UseSafePath;
        bool m_PostProcessEnabled;
        bool m_AnimatedMaterialsEnabled;

        bool m_MotionBlurSupportsScattering;

        bool m_NonRenderGraphResourcesAvailable;

        // Max guard band size is assumed to be 8 pixels
        const int k_RTGuardBandSize = 4;

        readonly System.Random m_Random;

        HDRenderPipeline m_HDInstance;

        internal static void SetExposureTextureToEmpty(RTHandle exposureTexture)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGHalf, false, true);
            tex.SetPixel(0, 0, new Color(1f, ColorUtils.ConvertExposureToEV100(1f), 0f, 0f));
            tex.Apply();
            Graphics.Blit(tex, exposureTexture);
            CoreUtils.Destroy(tex);
        }

        public PostProcessSystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_Resources = defaultResources;
            m_FinalPassMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.finalPassPS);
            m_ClearBlackMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.clearBlackPS);
            m_SMAAMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.SMAAPS);
            m_TemporalAAMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.temporalAntialiasingPS);

            // Some compute shaders fail on specific hardware or vendors so we'll have to use a
            // safer but slower code path for them
            m_UseSafePath = SystemInfo.graphicsDeviceVendor
                .ToLowerInvariant().Contains("intel");

            // Project-wide LUT size for all grading operations - meaning that internal LUTs and
            // user-provided LUTs will have to be this size
            var postProcessSettings = hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings;
            m_LutSize = postProcessSettings.lutSize;
            m_LutFormat = (GraphicsFormat)postProcessSettings.lutFormat;

            // Grading specific
            m_HableCurve = new HableCurve();

            m_MotionBlurSupportsScattering = SystemInfo.IsFormatSupported(GraphicsFormat.R32_UInt, FormatUsage.LoadStore) && SystemInfo.IsFormatSupported(GraphicsFormat.R16_UInt, FormatUsage.LoadStore);
            // TODO: Remove this line when atomic bug in HLSLcc is fixed.
            m_MotionBlurSupportsScattering = m_MotionBlurSupportsScattering && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan);
            // TODO: Write a version that uses structured buffer instead of texture to do atomic as Metal doesn't support atomics on textures.
            m_MotionBlurSupportsScattering = m_MotionBlurSupportsScattering && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal);

            // Use a custom RNG, we don't want to mess with the Unity one that the users might be
            // relying on (breaks determinism in their code)
            m_Random = new System.Random();

            m_ColorFormat = (GraphicsFormat)postProcessSettings.bufferFormat;
            m_KeepAlpha = false;

            // if both rendering and post-processing support an alpha channel, then post-processing will process (or copy) the alpha
            m_EnableAlpha = hdAsset.currentPlatformRenderPipelineSettings.supportsAlpha && postProcessSettings.supportsAlpha;

            if (m_EnableAlpha == false)
            {
                // if only rendering has an alpha channel (and not post-processing), then we just copy the alpha to the output (but we don't process it).
                m_KeepAlpha = hdAsset.currentPlatformRenderPipelineSettings.supportsAlpha;
            }

            // Setup a default exposure textures and clear it to neutral values so that the exposure
            // multiplier is 1 and thus has no effect
            // Beware that 0 in EV100 maps to a multiplier of 0.833 so the EV100 value in this
            // neutral exposure texture isn't 0
            m_EmptyExposureTexture = RTHandles.Alloc(1, 1, colorFormat: k_ExposureFormat,
                enableRandomWrite: true, name: "Empty EV100 Exposure");

            m_DebugExposureData = RTHandles.Alloc(1, 1, colorFormat: k_ExposureFormat,
                enableRandomWrite: true, name: "Debug Exposure Info");

            m_ExposureCurveTexture = new Texture2D(k_ExposureCurvePrecision, 1, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None)
            {
                name = "Exposure Curve",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            m_ExposureCurveTexture.hideFlags = HideFlags.HideAndDontSave;

            SetExposureTextureToEmpty(m_EmptyExposureTexture);
        }

        public void Cleanup()
        {
            RTHandles.Release(m_EmptyExposureTexture);
            m_EmptyExposureTexture = null;

            CoreUtils.Destroy(m_ExposureCurveTexture);
            CoreUtils.Destroy(m_InternalSpectralLut);
            CoreUtils.Destroy(m_FinalPassMaterial);
            CoreUtils.Destroy(m_ClearBlackMaterial);
            CoreUtils.Destroy(m_SMAAMaterial);
            CoreUtils.Destroy(m_TemporalAAMaterial);
            CoreUtils.SafeRelease(m_HistogramBuffer);
            CoreUtils.SafeRelease(m_DebugImageHistogramBuffer);
            RTHandles.Release(m_DebugExposureData);

            m_ExposureCurveTexture      = null;
            m_InternalSpectralLut       = null;
            m_FinalPassMaterial         = null;
            m_ClearBlackMaterial        = null;
            m_SMAAMaterial              = null;
            m_TemporalAAMaterial        = null;
            m_HistogramBuffer           = null;
            m_DebugImageHistogramBuffer = null;
            m_DebugExposureData = null;
        }

        // In some cases, the internal buffer of render textures might be invalid.
        // Usually when using these textures with API such as SetRenderTarget, they are recreated internally.
        // This is not the case when these textures are used exclusively with Compute Shaders. So to make sure they work in this case, we recreate them here.
        void CheckRenderTexturesValidity()
        {
            if (!m_EmptyExposureTexture.rt.IsCreated())
                SetExposureTextureToEmpty(m_EmptyExposureTexture);

            HDUtils.CheckRTCreated(m_DebugExposureData.rt);
        }

        public void BeginFrame(CommandBuffer cmd, HDCamera camera, HDRenderPipeline hdInstance)
        {
            m_HDInstance = hdInstance;
            m_PostProcessEnabled = camera.frameSettings.IsEnabled(FrameSettingsField.Postprocess) && CoreUtils.ArePostProcessesEnabled(camera.camera);
            m_AnimatedMaterialsEnabled = camera.animateMaterials;

            // Grab physical camera settings or a default instance if it's null (should only happen
            // in rare occasions due to how HDAdditionalCameraData is added to the camera)
            m_PhysicalCamera = camera.physicalParameters ?? m_DefaultPhysicalCamera;

            // Prefetch all the volume components we need to save some cycles as most of these will
            // be needed in multiple places
            var stack = camera.volumeStack;
            m_Exposure                  = stack.GetComponent<Exposure>();
            m_DepthOfField              = stack.GetComponent<DepthOfField>();
            m_MotionBlur                = stack.GetComponent<MotionBlur>();
            m_PaniniProjection          = stack.GetComponent<PaniniProjection>();
            m_Bloom                     = stack.GetComponent<Bloom>();
            m_ChromaticAberration       = stack.GetComponent<ChromaticAberration>();
            m_LensDistortion            = stack.GetComponent<LensDistortion>();
            m_Vignette                  = stack.GetComponent<Vignette>();
            m_Tonemapping               = stack.GetComponent<Tonemapping>();
            m_WhiteBalance              = stack.GetComponent<WhiteBalance>();
            m_ColorAdjustments          = stack.GetComponent<ColorAdjustments>();
            m_ChannelMixer              = stack.GetComponent<ChannelMixer>();
            m_SplitToning               = stack.GetComponent<SplitToning>();
            m_LiftGammaGain             = stack.GetComponent<LiftGammaGain>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_Curves                    = stack.GetComponent<ColorCurves>();
            m_FilmGrain                 = stack.GetComponent<FilmGrain>();
            m_PathTracing               = stack.GetComponent<PathTracing>();

            // Prefetch frame settings - these aren't free to pull so we want to do it only once
            // per frame
            var frameSettings = camera.frameSettings;
            m_StopNaNFS             = frameSettings.IsEnabled(FrameSettingsField.StopNaN);
            m_DepthOfFieldFS        = frameSettings.IsEnabled(FrameSettingsField.DepthOfField);
            m_MotionBlurFS          = frameSettings.IsEnabled(FrameSettingsField.MotionBlur);
            m_PaniniProjectionFS    = frameSettings.IsEnabled(FrameSettingsField.PaniniProjection);
            m_BloomFS               = frameSettings.IsEnabled(FrameSettingsField.Bloom);
            m_ChromaticAberrationFS = frameSettings.IsEnabled(FrameSettingsField.ChromaticAberration);
            m_LensDistortionFS      = frameSettings.IsEnabled(FrameSettingsField.LensDistortion);
            m_VignetteFS            = frameSettings.IsEnabled(FrameSettingsField.Vignette);
            m_ColorGradingFS        = frameSettings.IsEnabled(FrameSettingsField.ColorGrading);
            m_TonemappingFS         = frameSettings.IsEnabled(FrameSettingsField.Tonemapping);
            m_FilmGrainFS           = frameSettings.IsEnabled(FrameSettingsField.FilmGrain);
            m_DitheringFS           = frameSettings.IsEnabled(FrameSettingsField.Dithering);
            m_AntialiasingFS        = frameSettings.IsEnabled(FrameSettingsField.Antialiasing);

            // Override full screen anti-aliasing when doing path tracing (which is naturally anti-aliased already)
            m_AntialiasingFS        &= !m_PathTracing.enable.value;

            m_DebugExposureCompensation = m_HDInstance.m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugExposure;

            CheckRenderTexturesValidity();

            // Handle fixed exposure & disabled pre-exposure by forcing an exposure multiplier of 1
            {
                // Fix exposure is store in Exposure Textures at the beginning of the frame as there is no need for color buffer
                // Dynamic exposure (Auto, curve) is store in Exposure Textures at the end of the frame (as it rely on color buffer)
                // Texture current and previous are swapped at the beginning of the frame.
                if (CanRunFixedExposurePass(camera))
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FixedExposure)))
                    {
                        DoFixedExposure(PrepareExposureParameters(camera), cmd, camera.currentExposureTextures.current);
                    }
                }

                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(camera));
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, GetPreviousExposureTexture(camera));
            }
        }

        struct UberPostParameters
        {
            public ComputeShader    uberPostCS;
            public int              uberPostKernel;
            public bool             outputColorLog;
            public int              width;
            public int              height;
            public int              viewCount;

            public Vector4          logLutSettings;

            public Vector4          lensDistortionParams1;
            public Vector4          lensDistortionParams2;

            public Texture          spectralLut;
            public Vector4          chromaticAberrationParameters;

            public Vector4          vignetteParams1;
            public Vector4          vignetteParams2;
            public Vector4          vignetteColor;
            public Texture          vignetteMask;

            public Texture          bloomDirtTexture;
            public Vector4          bloomParams;
            public Vector4          bloomTint;
            public Vector4          bloomBicubicParams;
            public Vector4          bloomDirtTileOffset;
            public Vector4          bloomThreshold;

            public Vector4          alphaScaleBias;
        }

        UberPostParameters PrepareUberPostParameters(HDCamera hdCamera, bool isSceneView)
        {
            var parameters = new UberPostParameters();

            // Feature flags are passed to all effects and it's their responsibility to check
            // if they are used or not so they can set default values if needed
            parameters.uberPostCS = m_Resources.shaders.uberPostCS;
            parameters.uberPostCS.shaderKeywords = null;
            var featureFlags = GetUberFeatureFlags(isSceneView);
            parameters.uberPostKernel = parameters.uberPostCS.FindKernel("Uber");
            if (m_EnableAlpha)
            {
                parameters.uberPostCS.EnableKeyword("ENABLE_ALPHA");
            }

            parameters.outputColorLog = m_HDInstance.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ColorLog;
            parameters.width = hdCamera.actualWidth;
            parameters.height = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Color grading
            // This should be EV100 instead of EV but given that EV100(0) isn't equal to 1, it means
            // we can't use 0 as the default neutral value which would be confusing to users
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            parameters.logLutSettings = new Vector4(1f / m_LutSize, m_LutSize - 1f, postExposureLinear, 0f);

            // Setup the rest of the effects
            PrepareLensDistortionParameters(ref parameters, featureFlags);
            PrepareChromaticAberrationParameters(ref parameters, featureFlags);
            PrepareVignetteParameters(ref parameters, featureFlags);
            PrepareUberBloomParameters(ref parameters, hdCamera);
            PrepareAlphaScaleParameters(ref parameters, hdCamera);

            return parameters;
        }

        void PrepareAlphaScaleParameters(ref UberPostParameters parameters, HDCamera camera)
        {
            if (m_EnableAlpha)
            {
                parameters.alphaScaleBias = Compositor.CompositionManager.GetAlphaScaleAndBiasForCamera(camera);
            }
            else
            {
                parameters.alphaScaleBias = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
            }
        }

        static void DoUberPostProcess(in UberPostParameters parameters,
            RTHandle source,
            RTHandle destination,
            RTHandle logLut,
            RTHandle bloomTexture,
            CommandBuffer cmd)
        {
            // Color grading
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._LogLut3D, logLut);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._LogLut3D_Params, parameters.logLutSettings);

            // Lens distortion
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._DistortionParams1, parameters.lensDistortionParams1);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._DistortionParams2, parameters.lensDistortionParams2);

            // Chromatic aberration
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._ChromaSpectralLut, parameters.spectralLut);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._ChromaParams, parameters.chromaticAberrationParameters);

            // Vignette
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._VignetteParams1, parameters.vignetteParams1);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._VignetteParams2, parameters.vignetteParams2);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._VignetteColor, parameters.vignetteColor);
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._VignetteMask, parameters.vignetteMask);

            // Bloom
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._BloomTexture, bloomTexture);
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._BloomDirtTexture, parameters.bloomDirtTexture);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._BloomParams, parameters.bloomParams);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._BloomTint, parameters.bloomTint);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._BloomBicubicParams, parameters.bloomBicubicParams);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._BloomDirtScaleOffset, parameters.bloomDirtTileOffset);
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._BloomThreshold, parameters.bloomThreshold);

            // Alpha scale and bias (only used when alpha is enabled)
            cmd.SetComputeVectorParam(parameters.uberPostCS, HDShaderIDs._AlphaScaleBias, parameters.alphaScaleBias);

            // Dispatch uber post
            cmd.SetComputeVectorParam(parameters.uberPostCS, "_DebugFlags", new Vector4(parameters.outputColorLog ? 1 : 0, 0, 0, 0));
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(parameters.uberPostCS, parameters.uberPostKernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(parameters.uberPostCS, parameters.uberPostKernel, (parameters.width + 7) / 8, (parameters.height + 7) / 8, parameters.viewCount);
        }

        // Grabs all active feature flags
        UberPostFeatureFlags GetUberFeatureFlags(bool isSceneView)
        {
            var flags = UberPostFeatureFlags.None;

            if (m_ChromaticAberration.IsActive() && m_ChromaticAberrationFS)
                flags |= UberPostFeatureFlags.ChromaticAberration;

            if (m_Vignette.IsActive() && m_VignetteFS)
                flags |= UberPostFeatureFlags.Vignette;

            if (m_LensDistortion.IsActive() && !isSceneView && m_LensDistortionFS)
                flags |= UberPostFeatureFlags.LensDistortion;

            if (m_EnableAlpha)
            {
                flags |= UberPostFeatureFlags.EnableAlpha;
            }

            return flags;
        }

        static void ValidateComputeBuffer(ref ComputeBuffer cb, int size, int stride, ComputeBufferType type = ComputeBufferType.Default)
        {
            if (cb == null || cb.count < size)
            {
                CoreUtils.SafeRelease(cb);
                cb = new ComputeBuffer(size, stride, type);
            }
        }

        #region NaN Killer

        struct StopNaNParameters
        {
            public ComputeShader nanKillerCS;
            public int nanKillerKernel;

            public int width;
            public int height;
            public int viewCount;
        }

        StopNaNParameters PrepareStopNaNParameters(HDCamera camera)
        {
            StopNaNParameters stopNanParams = new StopNaNParameters();
            stopNanParams.nanKillerCS = m_Resources.shaders.nanKillerCS;
            stopNanParams.nanKillerKernel = stopNanParams.nanKillerCS.FindKernel("KMain");
            stopNanParams.width = camera.actualWidth;
            stopNanParams.height = camera.actualHeight;
            stopNanParams.viewCount = camera.viewCount;

            stopNanParams.nanKillerCS.shaderKeywords = null;
            if (m_EnableAlpha)
            {
                stopNanParams.nanKillerCS.EnableKeyword("ENABLE_ALPHA");
            }

            return stopNanParams;
        }

        static void DoStopNaNs(in StopNaNParameters stopNanParameters, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            var cs = stopNanParameters.nanKillerCS;
            int kernel = stopNanParameters.nanKillerKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (stopNanParameters.width + 7) / 8, (stopNanParameters.height + 7) / 8, stopNanParameters.viewCount);
        }

        #endregion

        #region Copy Alpha

        DoCopyAlphaParameters PrepareCopyAlphaParameters(HDCamera hdCamera)
        {
            var parameters = new DoCopyAlphaParameters();
            parameters.hdCamera = hdCamera;
            parameters.copyAlphaCS = m_Resources.shaders.copyAlphaCS;
            parameters.copyAlphaKernel = parameters.copyAlphaCS.FindKernel("KMain");

            return parameters;
        }

        struct DoCopyAlphaParameters
        {
            public ComputeShader copyAlphaCS;
            public int copyAlphaKernel;
            public HDCamera hdCamera;
        }

        static void DoCopyAlpha(in DoCopyAlphaParameters parameters, RTHandle source, RTHandle outputAlphaTexture, CommandBuffer cmd)
        {
            cmd.SetComputeTextureParam(parameters.copyAlphaCS, parameters.copyAlphaKernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(parameters.copyAlphaCS, parameters.copyAlphaKernel, HDShaderIDs._OutputTexture, outputAlphaTexture);
            cmd.DispatchCompute(parameters.copyAlphaCS, parameters.copyAlphaKernel, (parameters.hdCamera.actualWidth + 7) / 8, (parameters.hdCamera.actualHeight + 7) / 8, parameters.hdCamera.viewCount);
        }

        #endregion

        #region Exposure

        struct ApplyExposureParameters
        {
            public ComputeShader applyExposureCS;
            public int applyExposureKernel;

            public int width;
            public int height;
            public int viewCount;
        }

        ApplyExposureParameters PrepareApplyExposureParameters(HDCamera camera)
        {
            ApplyExposureParameters parameters = new ApplyExposureParameters();
            parameters.applyExposureCS = m_Resources.shaders.applyExposureCS;
            parameters.applyExposureKernel = parameters.applyExposureCS.FindKernel("KMain");

            parameters.width = camera.actualWidth;
            parameters.height = camera.actualHeight;
            parameters.viewCount = camera.viewCount;

            return parameters;
        }

        static void ApplyExposure(in ApplyExposureParameters parameters, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle prevExposure)
        {
            var cs = parameters.applyExposureCS;
            int kernel = parameters.applyExposureKernel;

            // Note: we use previous instead of current because the textures
            // are swapped internally as the system expects the texture will be used
            // on the next frame. So the actual "current" for this frame is in
            // "previous".
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (parameters.width + 7) / 8, (parameters.height + 7) / 8, parameters.viewCount);
        }

        struct ExposureParameters
        {
            public ComputeShader exposureCS;
            public ComputeShader histogramExposureCS;
            public int exposurePreparationKernel;
            public int exposureReductionKernel;

            public Texture textureMeteringMask;
            public Texture exposureCurve;

            public HDCamera camera;

            public ComputeBuffer histogramBuffer;

            public ExposureMode exposureMode;
            public bool histogramUsesCurve;
            public bool histogramOutputDebugData;

            public int[] exposureVariants;
            public Vector4 exposureParams;
            public Vector4 exposureParams2;
            public Vector4 proceduralMaskParams;
            public Vector4 proceduralMaskParams2;
            public Vector4 histogramExposureParams;
            public Vector4 adaptationParams;
        }

        ExposureParameters PrepareExposureParameters(HDCamera hdCamera)
        {
            var parameters = new ExposureParameters();
            parameters.exposureCS = m_Resources.shaders.exposureCS;
            parameters.histogramExposureCS = m_Resources.shaders.histogramExposureCS;
            parameters.histogramExposureCS.shaderKeywords = null;

            parameters.camera = hdCamera;

            bool isFixed = IsExposureFixed(hdCamera);
            if (isFixed)
            {
                parameters.exposureParams2 = new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);
                if (m_Exposure.mode.value == ExposureMode.Fixed
#if UNITY_EDITOR
                    || HDAdditionalSceneViewSettings.sceneExposureOverriden && hdCamera.camera.cameraType == CameraType.SceneView
#endif
                )
                {
                    parameters.exposureReductionKernel = parameters.exposureCS.FindKernel("KFixedExposure");
                    parameters.exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, m_Exposure.fixedExposure.value, 0f, 0f);
#if UNITY_EDITOR
                    if (HDAdditionalSceneViewSettings.sceneExposureOverriden && hdCamera.camera.cameraType == CameraType.SceneView)
                    {
                        parameters.exposureParams = new Vector4(0.0f, HDAdditionalSceneViewSettings.sceneExposure, 0f, 0f);
                    }
#endif
                }
                else if (m_Exposure.mode == ExposureMode.UsePhysicalCamera)
                {
                    parameters.exposureReductionKernel = parameters.exposureCS.FindKernel("KManualCameraExposure");
                    parameters.exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, m_PhysicalCamera.aperture, m_PhysicalCamera.shutterSpeed, m_PhysicalCamera.iso);
                }
            }
            else
            {
                // Setup variants
                var adaptationMode = m_Exposure.adaptationMode.value;

                if (!Application.isPlaying || hdCamera.resetPostProcessingHistory)
                    adaptationMode = AdaptationMode.Fixed;

                parameters.exposureVariants = m_ExposureVariants;
                parameters.exposureVariants[0] = 1; // (int)exposureSettings.luminanceSource.value;
                parameters.exposureVariants[1] = (int)m_Exposure.meteringMode.value;
                parameters.exposureVariants[2] = (int)adaptationMode;
                parameters.exposureVariants[3] = 0;

                bool useTextureMask = m_Exposure.meteringMode.value == MeteringMode.MaskWeighted && m_Exposure.weightTextureMask.value != null;
                parameters.textureMeteringMask = useTextureMask ? m_Exposure.weightTextureMask.value : Texture2D.whiteTexture;

                ComputeProceduralMeteringParams(hdCamera, out parameters.proceduralMaskParams, out parameters.proceduralMaskParams2);

                bool isHistogramBased = m_Exposure.mode.value == ExposureMode.AutomaticHistogram;
                bool needsCurve = (isHistogramBased && m_Exposure.histogramUseCurveRemapping.value) || m_Exposure.mode.value == ExposureMode.CurveMapping;

                parameters.histogramUsesCurve = m_Exposure.histogramUseCurveRemapping.value;
                parameters.adaptationParams = new Vector4(m_Exposure.adaptationSpeedLightToDark.value, m_Exposure.adaptationSpeedDarkToLight.value, 0.0f, 0.0f);

                parameters.exposureMode = m_Exposure.mode.value;

                float limitMax = m_Exposure.limitMax.value;
                float limitMin = m_Exposure.limitMin.value;

                float curveMin = 0.0f;
                float curveMax = 0.0f;
                if (needsCurve)
                {
                    PrepareExposureCurveData(out curveMin, out curveMax);
                    limitMin = curveMin;
                    limitMax = curveMax;
                }

                parameters.exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, limitMin, limitMax, 0f);
                parameters.exposureParams2 = new Vector4(curveMin, curveMax, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);

                parameters.exposureCurve = m_ExposureCurveTexture;


                if (isHistogramBased)
                {
                    ValidateComputeBuffer(ref m_HistogramBuffer, k_HistogramBins, sizeof(uint));
                    m_HistogramBuffer.SetData(m_EmptyHistogram);    // Clear the histogram

                    Vector2 histogramFraction = m_Exposure.histogramPercentages.value / 100.0f;
                    float evRange = limitMax - limitMin;
                    float histScale = 1.0f / Mathf.Max(1e-5f, evRange);
                    float histBias = -limitMin * histScale;
                    parameters.histogramExposureParams = new Vector4(histScale, histBias, histogramFraction.x, histogramFraction.y);

                    parameters.histogramBuffer = m_HistogramBuffer;
                    parameters.histogramOutputDebugData = m_HDInstance.m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView;
                    if (parameters.histogramOutputDebugData)
                    {
                        parameters.histogramExposureCS.EnableKeyword("OUTPUT_DEBUG_DATA");
                    }

                    parameters.exposurePreparationKernel = parameters.histogramExposureCS.FindKernel("KHistogramGen");
                    parameters.exposureReductionKernel = parameters.histogramExposureCS.FindKernel("KHistogramReduce");
                }
                else
                {
                    parameters.exposurePreparationKernel = parameters.exposureCS.FindKernel("KPrePass");
                    parameters.exposureReductionKernel = parameters.exposureCS.FindKernel("KReduction");
                }
            }


            return parameters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsExposureFixed(HDCamera camera) => m_Exposure.mode.value == ExposureMode.Fixed || m_Exposure.mode.value == ExposureMode.UsePhysicalCamera
            #if UNITY_EDITOR
        || (camera.camera.cameraType == CameraType.SceneView && HDAdditionalSceneViewSettings.sceneExposureOverriden)
            #endif
        ;

        //if exposure comes from the parent camera, it means we dont have to calculate / force it.
        //Its already been done in the parent camera.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool CanRunFixedExposurePass(HDCamera camera) => IsExposureFixed(camera)
        && camera.exposureControlFS && camera.currentExposureTextures.useCurrentCamera
        && camera.currentExposureTextures.current != null;

        public RTHandle GetExposureTexture(HDCamera camera)
        {
            // Note: GetExposureTexture(camera) must be call AFTER the call of DoFixedExposure to be correctly taken into account
            // When we use Dynamic Exposure and we reset history we can't use pre-exposure (as there is no information)
            // For this reasons we put neutral value at the beginning of the frame in Exposure textures and
            // apply processed exposure from color buffer at the end of the Frame, only for a single frame.
            // After that we re-use the pre-exposure system
            if (m_Exposure != null && (camera.resetPostProcessingHistory && camera.currentExposureTextures.useCurrentCamera) && !IsExposureFixed(camera))
                return m_EmptyExposureTexture;

            // 1x1 pixel, holds the current exposure multiplied in the red channel and EV100 value
            // in the green channel
            // One frame delay + history RTs being flipped at the beginning of the frame means we
            // have to grab the exposure marked as "previous"
            return GetExposureTextureHandle(camera.currentExposureTextures.current);
        }

        public RTHandle GetExposureTextureHandle(RTHandle rt)
        {
            return rt ?? m_EmptyExposureTexture;
        }

        public RTHandle GetPreviousExposureTexture(HDCamera camera)
        {
            // See GetExposureTexture
            return GetExposureTextureHandle(camera.currentExposureTextures.previous);
        }

        internal RTHandle GetExposureDebugData()
        {
            return m_DebugExposureData;
        }

        internal HableCurve GetCustomToneMapCurve()
        {
            return m_HableCurve;
        }

        internal int GetLutSize()
        {
            return m_LutSize;
        }

        internal ComputeBuffer GetHistogramBuffer()
        {
            return m_HistogramBuffer;
        }

        internal void ComputeProceduralMeteringParams(HDCamera camera, out Vector4 proceduralParams1, out Vector4 proceduralParams2)
        {
            Vector2 proceduralCenter = m_Exposure.proceduralCenter.value;
            if (camera.exposureTarget != null && m_Exposure.centerAroundExposureTarget.value)
            {
                var transform = camera.exposureTarget.transform;
                // Transform in screen space
                Vector3 targetLocation = transform.position;
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    targetLocation -= camera.camera.transform.position;
                }
                var ndcLoc = camera.mainViewConstants.viewProjMatrix * (targetLocation);
                ndcLoc.x /= ndcLoc.w;
                ndcLoc.y /= ndcLoc.w;

                Vector2 targetUV = new Vector2(ndcLoc.x, ndcLoc.y) * 0.5f + new Vector2(0.5f, 0.5f);
                targetUV.y = 1.0f - targetUV.y;

                proceduralCenter += targetUV;
            }

            proceduralCenter.x = Mathf.Clamp01(proceduralCenter.x);
            proceduralCenter.y = Mathf.Clamp01(proceduralCenter.y);

            proceduralCenter.x *= camera.actualWidth;
            proceduralCenter.y *= camera.actualHeight;

            float screenDiagonal = 0.5f * (camera.actualHeight + camera.actualWidth);

            proceduralParams1 = new Vector4(proceduralCenter.x, proceduralCenter.y,
                m_Exposure.proceduralRadii.value.x * camera.actualWidth,
                m_Exposure.proceduralRadii.value.y * camera.actualHeight);

            proceduralParams2 = new Vector4(1.0f / m_Exposure.proceduralSoftness.value, LightUtils.ConvertEvToLuminance(m_Exposure.maskMinIntensity.value), LightUtils.ConvertEvToLuminance(m_Exposure.maskMaxIntensity.value), 0.0f);
        }

        internal ComputeBuffer GetDebugImageHistogramBuffer()
        {
            return m_DebugImageHistogramBuffer;
        }

        void DoFixedExposure(in ExposureParameters parameters, CommandBuffer cmd, RTHandle prevExposure)
        {
            var cs = parameters.exposureCS;
            int kernel = parameters.exposureReductionKernel;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, parameters.exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, parameters.exposureParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, prevExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        void PrepareExposureCurveData(out float min, out float max)
        {
            var curve = m_Exposure.curveMap.value;
            var minCurve = m_Exposure.limitMinCurveMap.value;
            var maxCurve = m_Exposure.limitMaxCurveMap.value;

            if (m_ExposureCurveTexture == null)
            {
                m_ExposureCurveTexture = new Texture2D(k_ExposureCurvePrecision, 1, TextureFormat.RGBAHalf, false, true)
                {
                    name = "Exposure Curve",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                m_ExposureCurveTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            bool minCurveHasPoints = minCurve.length > 0;
            bool maxCurveHasPoints = maxCurve.length > 0;
            float defaultMin = -100.0f;
            float defaultMax = 100.0f;

            var pixels = m_ExposureCurveColorArray;

            // Fail safe in case the curve is deleted / has 0 point
            if (curve == null || curve.length == 0)
            {
                min = 0f;
                max = 0f;

                for (int i = 0; i < k_ExposureCurvePrecision; i++)
                    pixels[i] = Color.clear;
            }
            else
            {
                min = curve[0].time;
                max = curve[curve.length - 1].time;
                float step = (max - min) / (k_ExposureCurvePrecision - 1f);

                for (int i = 0; i < k_ExposureCurvePrecision; i++)
                {
                    float currTime = min + step * i;
                    pixels[i] = new Color(curve.Evaluate(currTime),
                        minCurveHasPoints ? minCurve.Evaluate(currTime) : defaultMin,
                        maxCurveHasPoints ? maxCurve.Evaluate(currTime) : defaultMax,
                        0f);
                }
            }

            m_ExposureCurveTexture.SetPixels(pixels);
            m_ExposureCurveTexture.Apply();
        }

        void GrabExposureRequiredTextures(HDCamera camera, out RTHandle prevExposure, out RTHandle nextExposure)
        {
            prevExposure = camera.currentExposureTextures.current;
            nextExposure = camera.currentExposureTextures.previous;
            if (camera.resetPostProcessingHistory)
            {
                // For Dynamic Exposure, we need to undo the pre-exposure from the color buffer to calculate the correct one
                // When we reset history we must setup neutral value
                prevExposure = m_EmptyExposureTexture; // Use neutral texture
            }
        }

        static void DoDynamicExposure(in ExposureParameters exposureParameters, CommandBuffer cmd, RTHandle colorBuffer, RTHandle prevExposure, RTHandle nextExposure, RTHandle tmpRenderTarget1024, RTHandle tmpRenderTarget32)
        {
            var cs = exposureParameters.exposureCS;
            int kernel;

            var sourceTex = colorBuffer;

            kernel = exposureParameters.exposurePreparationKernel;
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants,  exposureParameters.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, sourceTex);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, exposureParameters.exposureParams2);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, exposureParameters.textureMeteringMask);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, exposureParameters.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, exposureParameters.proceduralMaskParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, tmpRenderTarget1024);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = exposureParameters.exposureReductionKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, tmpRenderTarget1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, tmpRenderTarget32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, exposureParameters.exposureParams);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (exposureParameters.exposureMode == ExposureMode.Automatic)
            {
                exposureParameters.exposureVariants[3] = 1;
            }
            else if (exposureParameters.exposureMode == ExposureMode.CurveMapping)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, exposureParameters.exposureCurve);
                exposureParameters.exposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, exposureParameters.adaptationParams);
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, exposureParameters.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, tmpRenderTarget32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        static void DoHistogramBasedExposure(in ExposureParameters exposureParameters, CommandBuffer cmd, RTHandle sourceTexture, RTHandle prevExposure, RTHandle nextExposure, RTHandle debugData)
        {
            var cs = exposureParameters.histogramExposureCS;
            int kernel;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, exposureParameters.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, exposureParameters.proceduralMaskParams2);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._HistogramExposureParams, exposureParameters.histogramExposureParams);

            // Generate histogram.
            kernel = exposureParameters.exposurePreparationKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, sourceTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, exposureParameters.textureMeteringMask);

            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, exposureParameters.exposureVariants);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, exposureParameters.histogramBuffer);

            int threadGroupSizeX = 16;
            int threadGroupSizeY = 8;
            int dispatchSizeX = HDUtils.DivRoundUp(exposureParameters.camera.actualWidth / 2, threadGroupSizeX);
            int dispatchSizeY = HDUtils.DivRoundUp(exposureParameters.camera.actualHeight / 2, threadGroupSizeY);
            cmd.DispatchCompute(cs, kernel, dispatchSizeX, dispatchSizeY, 1);

            // Now read the histogram
            kernel = exposureParameters.exposureReductionKernel;
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, exposureParameters.exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, exposureParameters.exposureParams2);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, exposureParameters.adaptationParams);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, exposureParameters.histogramBuffer);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, nextExposure);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, exposureParameters.exposureCurve);
            exposureParameters.exposureVariants[3] = 0;
            if (exposureParameters.histogramUsesCurve)
            {
                exposureParameters.exposureVariants[3] = 2;
            }
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, exposureParameters.exposureVariants);

            if (exposureParameters.histogramOutputDebugData)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureDebugTexture, debugData);
            }

            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        internal struct DebugImageHistogramParameters
        {
            public ComputeShader debugImageHistogramCS;
            public ComputeBuffer imageHistogram;

            public int debugImageHistogramKernel;
            public int cameraWidth;
            public int cameraHeight;
        }

        internal DebugImageHistogramParameters PrepareDebugImageHistogramParameters(HDCamera camera)
        {
            DebugImageHistogramParameters parameters = new DebugImageHistogramParameters();

            parameters.debugImageHistogramCS = m_Resources.shaders.debugImageHistogramCS;
            parameters.debugImageHistogramKernel = parameters.debugImageHistogramCS.FindKernel("KHistogramGen");

            ValidateComputeBuffer(ref m_DebugImageHistogramBuffer, k_DebugImageHistogramBins * 4, sizeof(uint));
            m_DebugImageHistogramBuffer.SetData(m_EmptyDebugImageHistogram);    // Clear the histogram

            parameters.imageHistogram = m_DebugImageHistogramBuffer;

            parameters.cameraWidth = camera.actualWidth;
            parameters.cameraHeight = camera.actualHeight;

            return parameters;
        }

        static internal void GenerateDebugImageHistogram(in DebugImageHistogramParameters parameters, CommandBuffer cmd, RTHandle sourceTexture)
        {
            var cs = parameters.debugImageHistogramCS;
            int kernel = parameters.debugImageHistogramKernel;

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, sourceTexture);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, parameters.imageHistogram);

            int threadGroupSizeX = 16;
            int threadGroupSizeY = 16;
            int dispatchSizeX = HDUtils.DivRoundUp(parameters.cameraWidth / 2, threadGroupSizeX);
            int dispatchSizeY = HDUtils.DivRoundUp(parameters.cameraHeight / 2, threadGroupSizeY);
            int totalPixels = parameters.cameraWidth * parameters.cameraHeight;
            cmd.DispatchCompute(cs, kernel, dispatchSizeX, dispatchSizeY, 1);
        }

        #endregion

        #region Temporal Anti-aliasing

        struct TemporalAntiAliasingParameters
        {
            public Material temporalAAMaterial;
            public MaterialPropertyBlock taaHistoryPropertyBlock;
            public MaterialPropertyBlock taaPropertyBlock;
            public bool resetPostProcessingHistory;

            public Vector4 previousScreenSize;
            public Vector4 taaParameters;
            public Vector4 taaFilterWeights;
            public bool motionVectorRejection;
        }

        TemporalAntiAliasingParameters PrepareTAAParameters(HDCamera camera, bool PostDOF = false)
        {
            TemporalAntiAliasingParameters parameters = new TemporalAntiAliasingParameters();

            parameters.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            float minAntiflicker = 0.0f;
            float maxAntiflicker = 3.5f;
            float motionRejectionMultiplier = Mathf.Lerp(0.0f, 250.0f, camera.taaMotionVectorRejection * camera.taaMotionVectorRejection * camera.taaMotionVectorRejection);

            // The anti flicker becomes much more aggressive on higher values
            float temporalContrastForMaxAntiFlicker = 0.7f - Mathf.Lerp(0.0f, 0.3f, Mathf.SmoothStep(0.5f, 1.0f, camera.taaAntiFlicker));

            parameters.taaParameters = new Vector4(camera.taaHistorySharpening, PostDOF ? maxAntiflicker : Mathf.Lerp(minAntiflicker, maxAntiflicker, camera.taaAntiFlicker), motionRejectionMultiplier, temporalContrastForMaxAntiFlicker);

            // Precompute weights used for the Blackman-Harris filter. TODO: Note that these are slightly wrong as they don't take into account the jitter size. This needs to be fixed at some point.
            float crossWeights = Mathf.Exp(-2.29f * 2);
            float plusWeights = Mathf.Exp(-2.29f);
            float centerWeight = 1;

            float totalWeight = centerWeight + (4 * plusWeights);
            if (camera.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.High)
            {
                totalWeight += crossWeights * 4;
            }

            // Weights will be x: central, y: plus neighbours, z: cross neighbours, w: total
            parameters.taaFilterWeights = new Vector4(centerWeight / totalWeight, plusWeights / totalWeight, crossWeights / totalWeight, totalWeight);

            parameters.temporalAAMaterial = m_TemporalAAMaterial;
            parameters.temporalAAMaterial.shaderKeywords = null;

            if (m_EnableAlpha)
            {
                parameters.temporalAAMaterial.EnableKeyword("ENABLE_ALPHA");
            }

            if (camera.taaHistorySharpening == 0)
            {
                parameters.temporalAAMaterial.EnableKeyword("FORCE_BILINEAR_HISTORY");
            }

            if (camera.taaHistorySharpening != 0 && camera.taaAntiRinging && camera.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.High)
            {
                parameters.temporalAAMaterial.EnableKeyword("ANTI_RINGING");
            }

            parameters.motionVectorRejection = camera.taaMotionVectorRejection > 0;
            if (parameters.motionVectorRejection)
            {
                parameters.temporalAAMaterial.EnableKeyword("ENABLE_MV_REJECTION");
            }

            if (PostDOF)
            {
                parameters.temporalAAMaterial.EnableKeyword("POST_DOF");
            }
            else
            {
                switch (camera.TAAQuality)
                {
                    case HDAdditionalCameraData.TAAQualityLevel.Low:
                        parameters.temporalAAMaterial.EnableKeyword("LOW_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.Medium:
                        parameters.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.High:
                        parameters.temporalAAMaterial.EnableKeyword("HIGH_QUALITY");
                        break;
                    default:
                        parameters.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                }
            }

            parameters.taaHistoryPropertyBlock = m_TAAHistoryBlitPropertyBlock;
            parameters.taaPropertyBlock = m_TAAPropertyBlock;
            Vector2Int prevViewPort = camera.historyRTHandleProperties.previousViewportSize;
            parameters.previousScreenSize = new Vector4(prevViewPort.x, prevViewPort.y, 1.0f / prevViewPort.x, 1.0f / prevViewPort.y);

            return parameters;
        }

        static void DoTemporalAntialiasing(in TemporalAntiAliasingParameters taaParams,
            CommandBuffer cmd,
            RTHandle source,
            RTHandle destination,
            RTHandle motionVecTexture,
            RTHandle depthBuffer,
            RTHandle depthMipChain,
            RTHandle prevHistory,
            RTHandle nextHistory,
            RTHandle prevMVLen,
            RTHandle nextMVLen)
        {
            if (taaParams.resetPostProcessingHistory)
            {
                taaParams.taaHistoryPropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
                var rtScaleSource = source.rtHandleProperties.rtHandleScale;
                taaParams.taaHistoryPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(rtScaleSource.x, rtScaleSource.y, 0.0f, 0.0f));
                taaParams.taaHistoryPropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                HDUtils.DrawFullScreen(cmd, HDUtils.GetBlitMaterial(source.rt.dimension), prevHistory, taaParams.taaHistoryPropertyBlock, 0);
                HDUtils.DrawFullScreen(cmd, HDUtils.GetBlitMaterial(source.rt.dimension), nextHistory, taaParams.taaHistoryPropertyBlock, 0);
            }

            taaParams.taaPropertyBlock.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ExcludeFromTAA);
            taaParams.taaPropertyBlock.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ExcludeFromTAA);
            taaParams.taaPropertyBlock.SetTexture(HDShaderIDs._CameraMotionVectorsTexture, motionVecTexture);
            taaParams.taaPropertyBlock.SetTexture(HDShaderIDs._InputTexture, source);
            taaParams.taaPropertyBlock.SetTexture(HDShaderIDs._InputHistoryTexture, prevHistory);
            if (prevMVLen != null && taaParams.motionVectorRejection)
            {
                taaParams.taaPropertyBlock.SetTexture(HDShaderIDs._InputVelocityMagnitudeHistory, prevMVLen);
            }

            taaParams.taaPropertyBlock.SetTexture(HDShaderIDs._DepthTexture, depthMipChain);

            var taaHistorySize = taaParams.previousScreenSize;

            taaParams.taaPropertyBlock.SetVector(HDShaderIDs._TaaPostParameters, taaParams.taaParameters);
            taaParams.taaPropertyBlock.SetVector(HDShaderIDs._TaaHistorySize, taaHistorySize);
            taaParams.taaPropertyBlock.SetVector(HDShaderIDs._TaaFilterWeights, taaParams.taaFilterWeights);

            CoreUtils.SetRenderTarget(cmd, destination, depthBuffer);
            cmd.SetRandomWriteTarget(1, nextHistory);
            if (nextMVLen != null && taaParams.motionVectorRejection)
            {
                cmd.SetRandomWriteTarget(2, nextMVLen);
            }

            cmd.DrawProcedural(Matrix4x4.identity, taaParams.temporalAAMaterial, 0, MeshTopology.Triangles, 3, 1, taaParams.taaPropertyBlock);
            cmd.DrawProcedural(Matrix4x4.identity, taaParams.temporalAAMaterial, 1, MeshTopology.Triangles, 3, 1, taaParams.taaPropertyBlock);
            cmd.ClearRandomWriteTargets();
        }

        void GrabTemporalAntialiasingHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next, bool postDoF = false)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one, TextureXR.slices, DepthBits.None, dimension: TextureXR.dimension,
                    filterMode: FilterMode.Bilinear, colorFormat: m_ColorFormat,
                    enableRandomWrite: true, useDynamicScale: true, name: $"{id} TAA History"
                );
            }

            int historyType = (int)(postDoF ?
                HDCameraFrameHistoryType.TemporalAntialiasingPostDoF : HDCameraFrameHistoryType.TemporalAntialiasing);

            next = camera.GetCurrentFrameRT(historyType)
                ?? camera.AllocHistoryFrameRT(historyType, Allocator, 2);
            previous = camera.GetPreviousFrameRT(historyType);
        }

        void GrabVelocityMagnitudeHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one, TextureXR.slices, DepthBits.None, dimension: TextureXR.dimension,
                    filterMode: FilterMode.Bilinear, colorFormat: GraphicsFormat.R16_SFloat,
                    enableRandomWrite: true, useDynamicScale: true, name: $"{id} Velocity magnitude"
                );
            }

            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.TAAMotionVectorMagnitude)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.TAAMotionVectorMagnitude, Allocator, 2);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.TAAMotionVectorMagnitude);
        }

        void ReleasePostDoFTAAHistoryTextures(HDCamera camera)
        {
            var rt = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasingPostDoF);
            if (rt != null)
            {
                camera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasingPostDoF);
            }
        }

        #endregion

        #region Depth Of Field

        struct DepthOfFieldParameters
        {
            public ComputeShader dofKernelCS;
            public int dofKernelKernel;
            public ComputeShader dofCoCCS;
            public int dofCoCKernel;
            public ComputeShader dofCoCReprojectCS;
            public int dofCoCReprojectKernel;
            public ComputeShader dofDilateCS;
            public int dofDilateKernel;
            public ComputeShader dofMipCS;
            public int dofMipColorKernel;
            public int dofMipCoCKernel;
            public ComputeShader dofMipSafeCS;
            public int dofMipSafeKernel;
            public ComputeShader dofPrefilterCS;
            public int dofPrefilterKernel;
            public ComputeShader dofTileMaxCS;
            public int dofTileMaxKernel;
            public ComputeShader dofGatherCS;
            public int dofGatherNearKernel;
            public int dofGatherFarKernel;
            public ComputeShader dofCombineCS;
            public int dofCombineKernel;
            public ComputeShader dofPrecombineFarCS;
            public int dofPrecombineFarKernel;
            public ComputeShader dofClearIndirectArgsCS;
            public int dofClearIndirectArgsKernel;

            // PB DoF shaders
            public ComputeShader dofCircleOfConfusionCS;
            public int dofCircleOfConfusionKernel;
            public ComputeShader pbDoFCoCMinMaxCS;
            public int pbDoFMinMaxKernel;
            public ComputeShader pbDoFGatherCS;
            public int pbDoFGatherKernel;
            public ComputeShader pbDoFDilateCS;
            public int pbDoFDilateKernel;
            public int minMaxCoCTileSize;

            public BlueNoise.DitheredTextureSet ditheredTextureSet;

            public HDCamera camera;

            public bool nearLayerActive;
            public bool farLayerActive;
            public bool highQualityFiltering;
            public bool useTiles;
            public bool resetPostProcessingHistory;

            public DepthOfFieldResolution resolution;
            public DepthOfFieldMode focusMode;

            public Vector2 physicalCameraCurvature;
            public float physicalCameraAperture;
            public float physicalCameraAnamorphism;
            public float physicalCameraBarrelClipping;
            public int physicalCameraBladeCount;

            public int farSampleCount;
            public int nearSampleCount;
            public float farMaxBlur;
            public float nearMaxBlur;

            public float nearFocusStart;
            public float nearFocusEnd;
            public float farFocusStart;
            public float farFocusEnd;
            public float focusDistance;

            public Vector2Int threadGroup8;

            public bool useMipSafePath;
        }

        DepthOfFieldParameters PrepareDoFParameters(HDCamera camera)
        {
            DepthOfFieldParameters parameters = new DepthOfFieldParameters();

            parameters.dofKernelCS = m_Resources.shaders.depthOfFieldKernelCS;
            parameters.dofKernelKernel = parameters.dofKernelCS.FindKernel("KParametricBlurKernel");
            parameters.dofCoCCS = m_Resources.shaders.depthOfFieldCoCCS;
            parameters.dofCoCReprojectCS = m_Resources.shaders.depthOfFieldCoCReprojectCS;
            parameters.dofCoCReprojectKernel = parameters.dofCoCReprojectCS.FindKernel("KMain");
            parameters.dofDilateCS = m_Resources.shaders.depthOfFieldDilateCS;
            parameters.dofDilateKernel = parameters.dofDilateCS.FindKernel("KMain");
            parameters.dofMipCS = m_Resources.shaders.depthOfFieldMipCS;
            if (!m_DepthOfField.physicallyBased)
            {
                parameters.dofMipColorKernel = parameters.dofMipCS.FindKernel(m_EnableAlpha ? "KMainColorAlpha" : "KMainColor");
            }
            else
            {
                parameters.dofMipColorKernel = parameters.dofMipCS.FindKernel(m_EnableAlpha ? "KMainColorCopyAlpha" : "KMainColorCopy");
            }
            parameters.dofMipCoCKernel = parameters.dofMipCS.FindKernel("KMainCoC");
            parameters.dofMipSafeCS = m_Resources.shaders.depthOfFieldMipSafeCS;
            parameters.dofPrefilterCS = m_Resources.shaders.depthOfFieldPrefilterCS;
            parameters.dofTileMaxCS = m_Resources.shaders.depthOfFieldTileMaxCS;
            parameters.dofTileMaxKernel = parameters.dofTileMaxCS.FindKernel("KMain");
            parameters.dofGatherCS = m_Resources.shaders.depthOfFieldGatherCS;
            parameters.dofGatherNearKernel = parameters.dofGatherCS.FindKernel("KMainNear");
            parameters.dofGatherFarKernel = parameters.dofGatherCS.FindKernel("KMainFar");
            parameters.dofCombineCS = m_Resources.shaders.depthOfFieldCombineCS;
            parameters.dofCombineKernel = parameters.dofCombineCS.FindKernel("KMain");
            parameters.dofPrecombineFarCS = m_Resources.shaders.depthOfFieldPreCombineFarCS;
            parameters.dofPrecombineFarKernel = parameters.dofPrecombineFarCS.FindKernel("KMainPreCombineFar");
            parameters.dofClearIndirectArgsCS = m_Resources.shaders.depthOfFieldClearIndirectArgsCS;
            parameters.dofClearIndirectArgsKernel = parameters.dofClearIndirectArgsCS.FindKernel("KClear");

            parameters.dofCircleOfConfusionCS = m_Resources.shaders.dofCircleOfConfusion;
            parameters.pbDoFCoCMinMaxCS = m_Resources.shaders.dofCoCMinMaxCS;
            parameters.pbDoFMinMaxKernel = parameters.pbDoFCoCMinMaxCS.FindKernel("KMainCoCMinMax");
            parameters.pbDoFDilateCS = m_Resources.shaders.dofMinMaxDilateCS;
            parameters.pbDoFDilateKernel = parameters.pbDoFDilateCS.FindKernel("KMain");
            parameters.pbDoFGatherCS = m_Resources.shaders.dofGatherCS;
            parameters.pbDoFGatherKernel = parameters.pbDoFGatherCS.FindKernel("KMain");
            parameters.minMaxCoCTileSize = 8;

            parameters.camera = camera;
            parameters.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            parameters.nearLayerActive = m_DepthOfField.IsNearLayerActive();
            parameters.farLayerActive = m_DepthOfField.IsFarLayerActive();
            parameters.highQualityFiltering = m_DepthOfField.highQualityFiltering;
            parameters.useTiles = !camera.xr.singlePassEnabled;

            parameters.resolution = m_DepthOfField.resolution;

            float scale = m_DepthOfField.physicallyBased ? 1f : 1f / (float)parameters.resolution; // Note: physical dof always runs at full resolution
            float resolutionScale = (camera.actualHeight / 1080f) * (scale * 2f);

            int farSamples = Mathf.CeilToInt(m_DepthOfField.farSampleCount * resolutionScale);
            int nearSamples = Mathf.CeilToInt(m_DepthOfField.nearSampleCount * resolutionScale);
            // We want at least 3 samples for both far and near
            parameters.farSampleCount = Mathf.Max(3, farSamples);
            parameters.nearSampleCount = Mathf.Max(3, nearSamples);

            parameters.farMaxBlur = m_DepthOfField.farMaxBlur;
            parameters.nearMaxBlur = m_DepthOfField.nearMaxBlur;

            int targetWidth = Mathf.RoundToInt(camera.actualWidth * scale);
            int targetHeight = Mathf.RoundToInt(camera.actualHeight * scale);
            int threadGroup8X = (targetWidth + 7) / 8;
            int threadGroup8Y = (targetHeight + 7) / 8;

            parameters.threadGroup8 = new Vector2Int(threadGroup8X, threadGroup8Y);

            parameters.physicalCameraCurvature = m_PhysicalCamera.curvature;
            parameters.physicalCameraAnamorphism = m_PhysicalCamera.anamorphism;
            parameters.physicalCameraAperture = m_PhysicalCamera.aperture;
            parameters.physicalCameraBarrelClipping = m_PhysicalCamera.barrelClipping;
            parameters.physicalCameraBladeCount = m_PhysicalCamera.bladeCount;

            parameters.nearFocusStart = m_DepthOfField.nearFocusStart.value;
            parameters.nearFocusEnd = m_DepthOfField.nearFocusEnd.value;
            parameters.farFocusStart = m_DepthOfField.farFocusStart.value;
            parameters.farFocusEnd = m_DepthOfField.farFocusEnd.value;
            parameters.focusDistance = m_DepthOfField.focusDistance.value;

            parameters.focusMode = m_DepthOfField.focusMode.value;

            if (parameters.focusMode == DepthOfFieldMode.UsePhysicalCamera)
            {
                parameters.dofCoCKernel = parameters.dofCoCCS.FindKernel("KMainPhysical");
                parameters.dofCircleOfConfusionKernel = parameters.dofCircleOfConfusionCS.FindKernel("KMainCoCPhysical");
            }
            else
            {
                parameters.dofCoCKernel = parameters.dofCoCCS.FindKernel("KMainManual");
                parameters.dofCircleOfConfusionKernel = parameters.dofCircleOfConfusionCS.FindKernel("KMainCoCManual");
            }

            parameters.dofPrefilterCS.shaderKeywords = null;
            parameters.dofPrefilterKernel = parameters.dofPrefilterCS.FindKernel("KMain");
            parameters.dofMipSafeCS.shaderKeywords = null;
            parameters.dofMipSafeKernel = parameters.dofMipSafeCS.FindKernel("KMain");
            parameters.dofTileMaxCS.shaderKeywords = null;
            parameters.dofGatherCS.shaderKeywords = null;
            parameters.dofCombineCS.shaderKeywords = null;
            parameters.dofPrecombineFarCS.shaderKeywords = null;
            parameters.dofCombineCS.shaderKeywords = null;
            parameters.pbDoFGatherCS.shaderKeywords = null;
            parameters.dofCoCReprojectCS.shaderKeywords = null;

            bool nearLayerActive = parameters.nearLayerActive;
            bool farLayerActive = parameters.farLayerActive;
            bool bothLayersActive = nearLayerActive && farLayerActive;

            if (m_EnableAlpha)
            {
                parameters.dofPrefilterCS.EnableKeyword("ENABLE_ALPHA");
                parameters.dofMipSafeCS.EnableKeyword("ENABLE_ALPHA");
                parameters.dofGatherCS.EnableKeyword("ENABLE_ALPHA");
                parameters.dofCombineCS.EnableKeyword("ENABLE_ALPHA");
                parameters.dofPrecombineFarCS.EnableKeyword("ENABLE_ALPHA");
                parameters.pbDoFGatherCS.EnableKeyword("ENABLE_ALPHA");
            }

            if (parameters.resolution == DepthOfFieldResolution.Full)
            {
                parameters.dofPrefilterCS.EnableKeyword("FULL_RES");
                parameters.dofCombineCS.EnableKeyword("FULL_RES");
            }
            else if (parameters.highQualityFiltering)
            {
                parameters.dofCombineCS.EnableKeyword("HIGH_QUALITY");
            }
            else
            {
                parameters.dofCombineCS.EnableKeyword("LOW_QUALITY");
            }

            if (bothLayersActive || nearLayerActive)
            {
                parameters.dofPrefilterCS.EnableKeyword("NEAR");
                parameters.dofTileMaxCS.EnableKeyword("NEAR");
                parameters.dofCombineCS.EnableKeyword("NEAR");
            }

            if (bothLayersActive || !nearLayerActive)
            {
                parameters.dofPrefilterCS.EnableKeyword("FAR");
                parameters.dofTileMaxCS.EnableKeyword("FAR");
                parameters.dofCombineCS.EnableKeyword("FAR");
            }

            if (parameters.useTiles)
            {
                parameters.dofGatherCS.EnableKeyword("USE_TILES");
            }

            if (m_DepthOfField.physicallyBased)
            {
                parameters.dofCoCReprojectCS.EnableKeyword("ENABLE_MAX_BLENDING");
                BlueNoise blueNoise = m_HDInstance.GetBlueNoiseManager();
                parameters.ditheredTextureSet = blueNoise.DitheredTextureSet256SPP();
            }

            parameters.useMipSafePath = m_UseSafePath;

            return parameters;
        }

        static void GetDoFResolutionScale(in DepthOfFieldParameters dofParameters, out float scale, out float resolutionScale)
        {
            scale = 1f / (float)dofParameters.resolution;
            resolutionScale = (dofParameters.camera.actualHeight / 1080f) * (scale * 2f);
        }

        //
        // Reference used:
        //   "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
        //   "A Low Distortion Map Between Disk and Square" [Shirley97] [Chiu97]
        //   "High Quality Antialiasing" [Lorach07]
        //   "CryEngine 3 Graphics Gems" [Sousa13]
        //   "Next Generation Post Processing in Call of Duty: Advanced Warfare" [Jimenez14]
        //
        // Note: do not merge if/else clauses for each layer, pass schedule order is important to
        // reduce sync points on the GPU
        //
        // TODO: can be further optimized
        // TODO: debug panel entries (coc, tiles, etc)
        //
        static void DoDepthOfField(in DepthOfFieldParameters dofParameters, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle depthBuffer,
            RTHandle pingNearRGB, RTHandle pongNearRGB, RTHandle nearCoC, RTHandle nearAlpha, RTHandle dilatedNearCoC,
            RTHandle pingFarRGB, RTHandle pongFarRGB, RTHandle farCoC, RTHandle fullresCoC, RTHandle[] mips, RTHandle dilationPingPong,
            RTHandle prevCoCHistory, RTHandle nextCoCHistory, RTHandle motionVecTexture,
            ComputeBuffer bokehNearKernel, ComputeBuffer bokehFarKernel, ComputeBuffer bokehIndirectCmd, ComputeBuffer nearBokehTileList, ComputeBuffer farBokehTileList,
            bool taaEnabled)
        {
            bool nearLayerActive = dofParameters.nearLayerActive;
            bool farLayerActive = dofParameters.farLayerActive;

            Assert.IsTrue(nearLayerActive || farLayerActive);

            bool bothLayersActive = nearLayerActive && farLayerActive;
            bool useTiles = dofParameters.useTiles;
            bool hqFiltering = dofParameters.highQualityFiltering;

            const uint kIndirectNearOffset = 0u * sizeof(uint);
            const uint kIndirectFarOffset = 3u * sizeof(uint);

            // -----------------------------------------------------------------------------
            // Data prep
            // The number of samples & max blur sizes are scaled according to the resolution, with a
            // base scale of 1.0 for 1080p output

            int bladeCount = dofParameters.physicalCameraBladeCount;

            float rotation = (dofParameters.physicalCameraAperture - HDPhysicalCamera.kMinAperture) / (HDPhysicalCamera.kMaxAperture - HDPhysicalCamera.kMinAperture);
            rotation *= (360f / bladeCount) * Mathf.Deg2Rad; // TODO: Crude approximation, make it correct

            float ngonFactor = 1f;
            if (dofParameters.physicalCameraCurvature.y - dofParameters.physicalCameraCurvature.x > 0f)
                ngonFactor = (dofParameters.physicalCameraAperture - dofParameters.physicalCameraCurvature.x) / (dofParameters.physicalCameraCurvature.y - dofParameters.physicalCameraCurvature.x);

            ngonFactor = Mathf.Clamp01(ngonFactor);
            ngonFactor = Mathf.Lerp(ngonFactor, 0f, Mathf.Abs(dofParameters.physicalCameraAnamorphism));

            float anamorphism = dofParameters.physicalCameraAnamorphism / 4f;
            float barrelClipping = dofParameters.physicalCameraBarrelClipping / 3f;

            GetDoFResolutionScale(dofParameters, out float scale, out float resolutionScale);
            var screenScale = new Vector2(scale, scale);
            int targetWidth = Mathf.RoundToInt(dofParameters.camera.actualWidth * scale);
            int targetHeight = Mathf.RoundToInt(dofParameters.camera.actualHeight * scale);

            cmd.SetGlobalVector(HDShaderIDs._TargetScale, new Vector4((float)dofParameters.resolution, scale, 0f, 0f));

            int farSamples = dofParameters.farSampleCount;
            int nearSamples = dofParameters.nearSampleCount;

            float farMaxBlur = dofParameters.farMaxBlur * resolutionScale;
            float nearMaxBlur = dofParameters.nearMaxBlur * resolutionScale;

            // If TAA is enabled we use the camera history system to grab CoC history textures, but
            // because these don't use the same RTHandleScale as the global one, we need to use
            // the RTHandleScale of the history RTHandles
            var cocHistoryScale = taaEnabled ? dofParameters.camera.historyRTHandleProperties.rtHandleScale : RTHandles.rtHandleProperties.rtHandleScale;

            ComputeShader cs;
            int kernel;

            // -----------------------------------------------------------------------------
            // Render logic

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldKernel)))
            {
                // -----------------------------------------------------------------------------
                // Pass: generate bokeh kernels
                // Given that we allow full customization of near & far planes we'll need a separate
                // kernel for each layer

                cs = dofParameters.dofKernelCS;
                kernel = dofParameters.dofKernelKernel;

                // Near samples
                if (nearLayerActive)
                {
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(nearSamples, ngonFactor, bladeCount, rotation));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(anamorphism, 0f, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, bokehNearKernel);
                    cmd.DispatchCompute(cs, kernel, Mathf.CeilToInt((nearSamples * nearSamples) / 64f), 1, 1);
                }

                // Far samples
                if (farLayerActive)
                {
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(farSamples, ngonFactor, bladeCount, rotation));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(anamorphism, 0f, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, bokehFarKernel);
                    cmd.DispatchCompute(cs, kernel, Mathf.CeilToInt((farSamples * farSamples) / 64f), 1, 1);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCoC)))
            {
                // -----------------------------------------------------------------------------
                // Pass: compute CoC in full-screen (needed for temporal re-projection & combine)
                // CoC is initially stored in a RHalf texture in range [-1,1] as it makes RT
                // management easier and temporal re-projection cheaper; later transformed into
                // individual targets for near & far layers

                cs = dofParameters.dofCoCCS;
                kernel = dofParameters.dofCoCKernel;

                if (dofParameters.focusMode == DepthOfFieldMode.UsePhysicalCamera)
                {
                    // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                    float F = dofParameters.camera.camera.focalLength / 1000f;
                    float A = dofParameters.camera.camera.focalLength / dofParameters.physicalCameraAperture;
                    float P = dofParameters.focusDistance;
                    float maxCoC = (A * F) / Mathf.Max((P - F), 1e-6f);

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(P, maxCoC, 0f, 0f));
                }
                else // DepthOfFieldMode.Manual
                {
                    float nearEnd = dofParameters.nearFocusEnd;
                    float nearStart = Mathf.Min(dofParameters.nearFocusStart, nearEnd - 1e-5f);
                    float farStart = Mathf.Max(dofParameters.farFocusStart, nearEnd);
                    float farEnd = Mathf.Max(dofParameters.farFocusEnd, farStart + 1e-5f);

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(nearStart, nearEnd, farStart, farEnd));
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, depthBuffer);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, fullresCoC);
                cmd.DispatchCompute(cs, kernel, (dofParameters.camera.actualWidth + 7) / 8, (dofParameters.camera.actualHeight + 7) / 8, dofParameters.camera.viewCount);

                // -----------------------------------------------------------------------------
                // Pass: re-project CoC if TAA is enabled

                if (taaEnabled)
                {
                    ReprojectCoCHistory(dofParameters, cmd, dofParameters.camera, prevCoCHistory, nextCoCHistory, motionVecTexture, ref fullresCoC);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldPrefilter)))
            {
                // -----------------------------------------------------------------------------
                // Pass: downsample & prefilter CoC and layers
                // We only need to pre-multiply the CoC for the far layer; if only near is being
                // rendered we can use the downsampled color target as-is
                // TODO: We may want to add an anti-flicker here

                cs = dofParameters.dofPrefilterCS;
                kernel = dofParameters.dofPrefilterKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._CoCTargetScale, cocHistoryScale);

                if (nearLayerActive)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputNearCoCTexture, nearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputNearTexture, pingNearRGB);
                }

                if (farLayerActive)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputFarCoCTexture, farCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputFarTexture, pingFarRGB);
                }

                cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);
            }

            if (farLayerActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldPyramid)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: mip generation
                    // We only do this for the far layer because the near layer can't really use
                    // very wide radii due to reconstruction artifacts

                    int tx = ((targetWidth >> 1) + 7) / 8;
                    int ty = ((targetHeight >> 1) + 7) / 8;

                    if (dofParameters.useMipSafePath)
                    {
                        // The other compute fails hard on Intel because of texture format issues
                        cs = dofParameters.dofMipSafeCS;
                        kernel = dofParameters.dofMipSafeKernel;

                        var mipScale = scale;

                        for (int i = 0; i < 4; i++)
                        {
                            mipScale *= 0.5f;
                            var size = new Vector2Int(Mathf.RoundToInt(dofParameters.camera.actualWidth * mipScale), Mathf.RoundToInt(dofParameters.camera.actualHeight * mipScale));
                            var mip = mips[i];

                            cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                            int gx = (size.x + 7) / 8;
                            int gy = (size.y + 7) / 8;

                            // Downsample
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, mip);
                            cmd.DispatchCompute(cs, kernel, gx, gy, dofParameters.camera.viewCount);

                            // Copy to mip
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, mip);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pingFarRGB, i + 1);
                            cmd.DispatchCompute(cs, kernel, gx, gy, dofParameters.camera.viewCount);
                        }
                    }
                    else
                    {
                        cs = dofParameters.dofMipCS;
                        kernel = dofParameters.dofMipColorKernel;
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB, 0);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip1, pingFarRGB, 1);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip2, pingFarRGB, 2);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip3, pingFarRGB, 3);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip4, pingFarRGB, 4);
                        cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                    }

                    cs = dofParameters.dofMipCS;
                    kernel = dofParameters.dofMipCoCKernel;
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, farCoC, 0);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip1, farCoC, 1);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip2, farCoC, 2);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip3, farCoC, 3);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip4, farCoC, 4);
                    cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                }
            }

            if (nearLayerActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldDilate)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: dilate the near CoC

                    cs = dofParameters.dofDilateCS;
                    kernel = dofParameters.dofDilateKernel;
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(targetWidth - 1, targetHeight - 1, 0f, 0f));

                    int passCount = Mathf.CeilToInt((nearMaxBlur + 2f) / 4f);

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, nearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, dilatedNearCoC);
                    cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);

                    if (passCount > 1)
                    {
                        // Ping-pong
                        var src = dilatedNearCoC;
                        var dst = dilationPingPong;

                        for (int i = 1; i < passCount; i++)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, src);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, dst);
                            cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);
                            CoreUtils.Swap(ref src, ref dst);
                        }

                        dilatedNearCoC = src;
                    }
                }
            }

            if (useTiles)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldTileMax)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: tile-max classification

                    // Clear the indirect command buffer
                    cs = dofParameters.dofClearIndirectArgsCS;
                    kernel = dofParameters.dofClearIndirectArgsKernel;
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, bokehIndirectCmd);
                    cmd.DispatchCompute(cs, kernel, 1, 1, 1);

                    // Build the tile list & indirect command buffer
                    cs = dofParameters.dofTileMaxCS;

                    kernel = dofParameters.dofTileMaxKernel;
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(targetWidth - 1, targetHeight - 1, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, bokehIndirectCmd);

                    if (nearLayerActive)
                    {
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputNearCoCTexture, dilatedNearCoC);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._NearTileList, nearBokehTileList);
                    }

                    if (farLayerActive)
                    {
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputFarCoCTexture, farCoC);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._FarTileList, farBokehTileList);
                    }

                    cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, 1);
                }
            }

            if (farLayerActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldGatherFar)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: bokeh blur the far layer

                    if (useTiles)
                    {
                        // Need to clear dest as we recycle render targets and tiles won't write to
                        // all pixels thus leaving previous-frame info
                        cmd.SetRenderTarget(pongFarRGB);
                        cmd.ClearRenderTarget(false, true, Color.clear);
                    }

                    cs = dofParameters.dofGatherCS;
                    kernel = dofParameters.dofGatherFarKernel;

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(farSamples, farSamples * farSamples, barrelClipping, farMaxBlur));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(targetWidth, targetHeight, 1f / targetWidth, 1f / targetHeight));
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, farCoC);

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongFarRGB);
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, bokehFarKernel);

                    if (useTiles)
                    {
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._TileList, farBokehTileList);
                        cmd.DispatchCompute(cs, kernel, bokehIndirectCmd, kIndirectFarOffset);
                    }
                    else
                    {
                        cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);
                    }
                }
            }

            if (nearLayerActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldPreCombine)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: if the far layer was active, use it as a source for the near blur to
                    // avoid out-of-focus artifacts (e.g. near blur in front of far blur)

                    if (farLayerActive)
                    {
                        cs = dofParameters.dofPrecombineFarCS;
                        kernel = dofParameters.dofPrecombineFarKernel;
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingNearRGB);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputFarTexture, pongFarRGB);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, farCoC);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongNearRGB);
                        cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);

                        CoreUtils.Swap(ref pingNearRGB, ref pongNearRGB);
                    }
                }

                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldGatherNear)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: bokeh blur the near layer

                    if (useTiles)
                    {
                        // Same as the far layer, clear to discard garbage data
                        if (!farLayerActive)
                        {
                            cmd.SetRenderTarget(pongNearRGB);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                        }

                        cmd.SetRenderTarget(nearAlpha);
                        cmd.ClearRenderTarget(false, true, Color.clear);
                    }

                    cs = dofParameters.dofGatherCS;
                    kernel = dofParameters.dofGatherNearKernel;

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(nearSamples, nearSamples * nearSamples, barrelClipping, nearMaxBlur));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(targetWidth, targetHeight, 1f / targetWidth, 1f / targetHeight));
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingNearRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, nearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputDilatedCoCTexture, dilatedNearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongNearRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputAlphaTexture, nearAlpha);
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, bokehNearKernel);

                    if (useTiles)
                    {
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._TileList, nearBokehTileList);
                        cmd.DispatchCompute(cs, kernel, bokehIndirectCmd, kIndirectNearOffset);
                    }
                    else
                    {
                        cmd.DispatchCompute(cs, kernel, dofParameters.threadGroup8.x, dofParameters.threadGroup8.y, dofParameters.camera.viewCount);
                    }
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCombine)))
            {
                // -----------------------------------------------------------------------------
                // Pass: combine blurred layers with source color

                cs = dofParameters.dofCombineCS;
                kernel = dofParameters.dofCombineKernel;

                if (nearLayerActive)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputNearTexture, pongNearRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputNearAlphaTexture, nearAlpha);
                }

                if (farLayerActive)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputFarTexture, pongFarRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                cmd.DispatchCompute(cs, kernel, (dofParameters.camera.actualWidth + 7) / 8, (dofParameters.camera.actualHeight + 7) / 8, dofParameters.camera.viewCount);
            }
        }

        static RTHandle CoCAllocatorMipsTrue(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R16_SFloat,
                dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: true, useDynamicScale: true, name: $"{id} CoC History"
            );
        }

        static RTHandle CoCAllocatorMipsFalse(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R16_SFloat,
                dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: false, useDynamicScale: true, name: $"{id} CoC History"
            );
        }

        static void GrabCoCHistory(HDCamera camera, out RTHandle previous, out RTHandle next, bool useMips = false)
        {
            // WARNING WORKAROUND
            // For some reason, the Allocator as it was declared before would capture the useMips parameter but only when both render graph and XR are enabled.
            // To work around this we have two hard coded allocators and use one or the other depending on the parameters.
            // Also don't try to put the right allocator in a temporary variable as it will also generate allocations hence the horrendous copy paste bellow.
            if (useMips)
            {
                next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC)
                    ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC, CoCAllocatorMipsTrue, 2);
            }
            else
            {
                next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC)
                    ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC, CoCAllocatorMipsFalse, 2);
            }

            if (useMips == true && next.rt.mipmapCount == 1)
            {
                camera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC);
                next = camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC, CoCAllocatorMipsTrue, 2);
            }

            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC);
        }

        static void ReprojectCoCHistory(in DepthOfFieldParameters parameters, CommandBuffer cmd, HDCamera camera, RTHandle prevCoC, RTHandle nextCoC, RTHandle motionVecTexture, ref RTHandle fullresCoC)
        {
            var cocHistoryScale = new Vector2(camera.historyRTHandleProperties.rtHandleScale.z, camera.historyRTHandleProperties.rtHandleScale.w);

            //Note: this reprojection creates some ghosting, we should replace it with something based on the new TAA
            ComputeShader cs = parameters.dofCoCReprojectCS;
            int kernel = parameters.dofCoCReprojectKernel;
            cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(parameters.resetPostProcessingHistory ? 0f : 0.91f, cocHistoryScale.x, cocHistoryScale.y, 0f));
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHistoryCoCTexture, prevCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, nextCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraMotionVectorsTexture, motionVecTexture);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

            fullresCoC = nextCoC;
        }

        static void DoPhysicallyBasedDepthOfField(in DepthOfFieldParameters dofParameters, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle fullresCoC, RTHandle prevCoCHistory, RTHandle nextCoCHistory, RTHandle motionVecTexture, RTHandle sourcePyramid, RTHandle depthBuffer, RTHandle minMaxCoCPing, RTHandle minMaxCoCPong, bool taaEnabled)
        {
            float scale = 1f / (float)dofParameters.resolution;
            int targetWidth = Mathf.RoundToInt(dofParameters.camera.actualWidth * scale);
            int targetHeight = Mathf.RoundToInt(dofParameters.camera.actualHeight * scale);

            // Map the old "max radius" parameters to a bigger range, so we can work on more challenging scenes, [0, 16] --> [0, 64]
            Vector2 cocLimit = new Vector2(
                Mathf.Max(4 * dofParameters.farMaxBlur, 0.01f),
                Mathf.Max(4 * dofParameters.nearMaxBlur, 0.01f));
            float maxCoc = Mathf.Max(cocLimit.x, cocLimit.y);

            ComputeShader cs;
            int kernel;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCoC)))
            {
                cs = dofParameters.dofCircleOfConfusionCS;
                kernel = dofParameters.dofCircleOfConfusionKernel;

                if (dofParameters.focusMode == DepthOfFieldMode.UsePhysicalCamera)
                {
                    // The sensor scale is used to convert the CoC size from mm to screen pixels
                    float sensorScale;
                    if (dofParameters.camera.camera.gateFit == Camera.GateFitMode.Horizontal)
                        sensorScale = (0.5f / dofParameters.camera.camera.sensorSize.x) * dofParameters.camera.camera.pixelWidth;
                    else
                        sensorScale = (0.5f / dofParameters.camera.camera.sensorSize.y) * dofParameters.camera.camera.pixelHeight;

                    // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                    // Note: Focus distance is in meters, but focalLength and sensor size are in mm.
                    // We don't convert them to meters because the multiplication factors cancel-out
                    float F = dofParameters.camera.camera.focalLength / 1000f;
                    float A = dofParameters.camera.camera.focalLength / dofParameters.physicalCameraAperture;
                    float P = dofParameters.focusDistance;
                    float maxFarCoC = sensorScale * (A * F) / Mathf.Max((P - F), 1e-6f);

                    // Scale and Bias factors for directly computing CoC size from post-rasterization depth with a single mad
                    float cocBias = maxFarCoC * (1f - P / dofParameters.camera.camera.farClipPlane);
                    float cocScale = maxFarCoC * P * (dofParameters.camera.camera.farClipPlane - dofParameters.camera.camera.nearClipPlane) / (dofParameters.camera.camera.farClipPlane * dofParameters.camera.camera.nearClipPlane);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(cocLimit.x, cocLimit.y, cocScale, cocBias));
                }
                else
                {
                    float nearEnd = dofParameters.nearFocusEnd;
                    float nearStart = Mathf.Min(dofParameters.nearFocusStart, nearEnd - 1e-5f);
                    float farStart = Mathf.Max(dofParameters.farFocusStart, nearEnd);
                    float farEnd = Mathf.Max(dofParameters.farFocusEnd, farStart + 1e-5f);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(farStart, nearEnd, 1.0f / (farEnd - farStart), 1.0f / (nearStart - nearEnd)));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(cocLimit.y, cocLimit.x, 0, 0));
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, depthBuffer);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, fullresCoC);
                cmd.DispatchCompute(cs, kernel, (dofParameters.camera.actualWidth + 7) / 8, (dofParameters.camera.actualHeight + 7) / 8, dofParameters.camera.viewCount);

                if (taaEnabled)
                {
                    ReprojectCoCHistory(dofParameters, cmd, dofParameters.camera, prevCoCHistory, nextCoCHistory, motionVecTexture, ref fullresCoC);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldPyramid)))
            {
                // DoF color pyramid
                if (sourcePyramid != null)
                {
                    cs = dofParameters.dofMipCS;
                    kernel = dofParameters.dofMipColorKernel;

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source, 0);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, sourcePyramid, 0);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip1, sourcePyramid, 1);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip2, sourcePyramid, 2);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip3, sourcePyramid, 3);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip4, sourcePyramid, 4);

                    int tx = ((dofParameters.camera.actualWidth >> 1) + 7) / 8;
                    int ty = ((dofParameters.camera.actualHeight >> 1) + 7) / 8;
                    cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldDilate)))
            {
                int tileSize = dofParameters.minMaxCoCTileSize;
                int tx = ((dofParameters.camera.actualWidth / tileSize) + 7) / 8;
                int ty = ((dofParameters.camera.actualHeight / tileSize) + 7) / 8;

                // Min Max CoC tiles
                {
                    cs = dofParameters.pbDoFCoCMinMaxCS;
                    kernel = dofParameters.pbDoFMinMaxKernel;

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, fullresCoC, 0);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, minMaxCoCPing, 0);
                    cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                }

                //  Min Max CoC tile dilation
                {
                    cs = dofParameters.pbDoFDilateCS;
                    kernel = dofParameters.pbDoFDilateKernel;

                    int iterations = (int)Mathf.Max(Mathf.Ceil(cocLimit.y / dofParameters.minMaxCoCTileSize), 1.0f);
                    for (int pass = 0; pass < iterations; ++pass)
                    {
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, minMaxCoCPing, 0);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, minMaxCoCPong, 0);
                        cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                        CoreUtils.Swap(ref minMaxCoCPing, ref minMaxCoCPong);
                    }
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCombine)))
            {
                cs = dofParameters.pbDoFGatherCS;
                kernel = dofParameters.pbDoFGatherKernel;
                float sampleCount = Mathf.Max(dofParameters.nearSampleCount, dofParameters.farSampleCount);
                float anamorphism = dofParameters.physicalCameraAnamorphism / 4f;

                float mipLevel = 1 + Mathf.Ceil(Mathf.Log(maxCoc, 2));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(sampleCount, maxCoc, anamorphism, 0.0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(mipLevel, 0, 0, (float)dofParameters.resolution));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourcePyramid != null ? sourcePyramid : source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileList, minMaxCoCPing, 0);
                BlueNoise.BindDitheredTextureSet(cmd, dofParameters.ditheredTextureSet);
                cmd.DispatchCompute(cs, kernel, (dofParameters.camera.actualWidth + 7) / 8, (dofParameters.camera.actualHeight + 7) / 8, dofParameters.camera.viewCount);
            }
        }

        #endregion

        #region Motion Blur

        struct MotionBlurParameters
        {
            public ComputeShader motionVecPrepCS;
            public ComputeShader tileGenCS;
            public ComputeShader tileNeighbourhoodCS;
            public ComputeShader tileMergeCS;
            public ComputeShader motionBlurCS;

            public int motionVecPrepKernel;
            public int tileGenKernel;
            public int tileNeighbourhoodKernel;
            public int tileMergeKernel;
            public int motionBlurKernel;

            public HDCamera camera;

            public Vector4 tileTargetSize;
            public Vector4 motionBlurParams0;
            public Vector4 motionBlurParams1;
            public Vector4 motionBlurParams2;
            public Vector4 motionBlurParams3;

            public bool motionblurSupportScattering;
        }

        MotionBlurParameters PrepareMotionBlurParameters(HDCamera camera)
        {
            MotionBlurParameters parameters = new MotionBlurParameters();

            parameters.camera = camera;

            int tileSize = 32;

            if (m_MotionBlurSupportsScattering)
            {
                tileSize = 16;
            }

            int tileTexWidth = Mathf.CeilToInt(camera.actualWidth / tileSize);
            int tileTexHeight = Mathf.CeilToInt(camera.actualHeight / tileSize);
            parameters.tileTargetSize = new Vector4(tileTexWidth, tileTexHeight, 1.0f / tileTexWidth, 1.0f / tileTexHeight);

            float screenMagnitude = (new Vector2(camera.actualWidth, camera.actualHeight).magnitude);
            parameters.motionBlurParams0 = new Vector4(
                screenMagnitude,
                screenMagnitude * screenMagnitude,
                m_MotionBlur.minimumVelocity.value,
                m_MotionBlur.minimumVelocity.value * m_MotionBlur.minimumVelocity.value
            );

            parameters.motionBlurParams1 = new Vector4(
                m_MotionBlur.intensity.value,
                m_MotionBlur.maximumVelocity.value / screenMagnitude,
                0.25f, // min/max velocity ratio for high quality.
                m_MotionBlur.cameraRotationVelocityClamp.value
            );

            uint sampleCount = (uint)m_MotionBlur.sampleCount;
            parameters.motionBlurParams2 = new Vector4(
                m_MotionBlurSupportsScattering ? (sampleCount + (sampleCount & 1)) : sampleCount,
                tileSize,
                m_MotionBlur.depthComparisonExtent.value,
                m_MotionBlur.cameraMotionBlur.value ? 0.0f : 1.0f
            );

            parameters.motionVecPrepCS = m_Resources.shaders.motionBlurMotionVecPrepCS;
            parameters.motionVecPrepKernel = parameters.motionVecPrepCS.FindKernel("MotionVecPreppingCS");
            parameters.motionVecPrepCS.shaderKeywords = null;

            if (!m_MotionBlur.cameraMotionBlur.value)
            {
                parameters.motionVecPrepCS.EnableKeyword("CAMERA_DISABLE_CAMERA");
            }
            else
            {
                var clampMode = m_MotionBlur.specialCameraClampMode.value;
                if (clampMode == CameraClampMode.None)
                    parameters.motionVecPrepCS.EnableKeyword("NO_SPECIAL_CLAMP");
                else if (clampMode == CameraClampMode.Rotation)
                    parameters.motionVecPrepCS.EnableKeyword("CAMERA_ROT_CLAMP");
                else if (clampMode == CameraClampMode.Translation)
                    parameters.motionVecPrepCS.EnableKeyword("CAMERA_TRANS_CLAMP");
                else if (clampMode == CameraClampMode.SeparateTranslationAndRotation)
                    parameters.motionVecPrepCS.EnableKeyword("CAMERA_SEPARATE_CLAMP");
                else if (clampMode == CameraClampMode.FullCameraMotionVector)
                    parameters.motionVecPrepCS.EnableKeyword("CAMERA_FULL_CLAMP");
            }

            parameters.motionBlurParams3 = new Vector4(
                m_MotionBlur.cameraTranslationVelocityClamp.value,
                m_MotionBlur.cameraVelocityClamp.value,
                0, 0);


            parameters.tileGenCS = m_Resources.shaders.motionBlurGenTileCS;
            parameters.tileGenCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                parameters.tileGenCS.EnableKeyword("SCATTERING");
            }
            parameters.tileGenKernel = parameters.tileGenCS.FindKernel("TileGenPass");

            parameters.tileNeighbourhoodCS = m_Resources.shaders.motionBlurNeighborhoodTileCS;
            parameters.tileNeighbourhoodCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                parameters.tileNeighbourhoodCS.EnableKeyword("SCATTERING");
            }
            parameters.tileNeighbourhoodKernel = parameters.tileNeighbourhoodCS.FindKernel("TileNeighbourhood");

            parameters.tileMergeCS = m_Resources.shaders.motionBlurMergeTileCS;
            parameters.tileMergeKernel = parameters.tileMergeCS.FindKernel("TileMerge");

            parameters.motionBlurCS = m_Resources.shaders.motionBlurCS;
            parameters.motionBlurCS.shaderKeywords = null;
            CoreUtils.SetKeyword(parameters.motionBlurCS, "ENABLE_ALPHA", m_EnableAlpha);
            parameters.motionBlurKernel = parameters.motionBlurCS.FindKernel("MotionBlurCS");

            parameters.motionblurSupportScattering = m_MotionBlurSupportsScattering;

            return parameters;
        }

        static void DoMotionBlur(in MotionBlurParameters motionBlurParams, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle depthTexture, RTHandle motionVectorTexture,
            RTHandle preppedMotionVec, RTHandle minMaxTileVel,
            RTHandle maxTileNeigbourhood, RTHandle tileToScatterMax,
            RTHandle tileToScatterMin)
        {
            int tileSize = 32;

            if (motionBlurParams.motionblurSupportScattering)
            {
                tileSize = 16;
            }

            // -----------------------------------------------------------------------------
            // Prep motion vectors

            // - Pack normalized motion vectors and linear depth in R11G11B10
            ComputeShader cs;
            int kernel;
            int threadGroupX;
            int threadGroupY;
            int groupSizeX = 8;
            int groupSizeY = 8;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurMotionVecPrep)))
            {
                cs = motionBlurParams.motionVecPrepCS;
                kernel = motionBlurParams.motionVecPrepKernel;
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, depthTexture);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams.motionBlurParams2);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams3, motionBlurParams.motionBlurParams3);

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraMotionVectorsTexture, motionVectorTexture);

                cmd.SetComputeMatrixParam(cs, HDShaderIDs._PrevVPMatrixNoTranslation, motionBlurParams.camera.mainViewConstants.prevViewProjMatrixNoCameraTrans);
                cmd.SetComputeMatrixParam(cs, HDShaderIDs._CurrVPMatrixNoTranslation, motionBlurParams.camera.mainViewConstants.viewProjectionNoCameraTrans);

                threadGroupX = (motionBlurParams.camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (motionBlurParams.camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, motionBlurParams.camera.viewCount);
            }


            // -----------------------------------------------------------------------------
            // Generate MinMax motion vectors tiles

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurTileMinMax)))
            {
                // We store R11G11B10 with RG = Max vel and B = Min vel magnitude
                cs = motionBlurParams.tileGenCS;
                kernel = motionBlurParams.tileGenKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams.motionBlurParams2);


                if (motionBlurParams.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                }

                threadGroupX = (motionBlurParams.camera.actualWidth + (tileSize - 1)) / tileSize;
                threadGroupY = (motionBlurParams.camera.actualHeight + (tileSize - 1)) / tileSize;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, motionBlurParams.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Generate max tiles neigbhourhood

            using (new ProfilingScope(cmd, motionBlurParams.motionblurSupportScattering ? ProfilingSampler.Get(HDProfileId.MotionBlurTileScattering) : ProfilingSampler.Get(HDProfileId.MotionBlurTileNeighbourhood)))
            {
                cs = motionBlurParams.tileNeighbourhoodCS;
                kernel = motionBlurParams.tileNeighbourhoodKernel;


                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, motionBlurParams.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams.motionBlurParams2);

                if (motionBlurParams.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                }
                groupSizeX = 8;
                groupSizeY = 8;
                threadGroupX = ((int)motionBlurParams.tileTargetSize.x + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = ((int)motionBlurParams.tileTargetSize.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, motionBlurParams.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Merge min/max info spreaded above.

            if (motionBlurParams.motionblurSupportScattering)
            {
                cs = motionBlurParams.tileMergeCS;
                kernel = motionBlurParams.tileMergeKernel;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, motionBlurParams.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams.motionBlurParams2);

                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, motionBlurParams.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Blur kernel
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurKernel)))
            {
                cs = motionBlurParams.motionBlurCS;
                kernel = motionBlurParams.motionBlurKernel;

                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, motionBlurParams.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams.motionBlurParams2);

                groupSizeX = 16;
                groupSizeY = 16;
                threadGroupX = (motionBlurParams.camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (motionBlurParams.camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, motionBlurParams.camera.viewCount);
            }
        }

        #endregion

        #region Panini Projection

        struct PaniniProjectionParameters
        {
            public ComputeShader paniniProjectionCS;
            public int paniniProjectionKernel;

            public Vector4 paniniParams;

            public int width;
            public int height;
            public int viewCount;
        }

        PaniniProjectionParameters PreparePaniniProjectionParameters(HDCamera camera)
        {
            PaniniProjectionParameters parameters = new PaniniProjectionParameters();

            parameters.width = camera.actualWidth;
            parameters.height = camera.actualHeight;
            parameters.viewCount = camera.viewCount;

            parameters.paniniProjectionCS = m_Resources.shaders.paniniProjectionCS;
            parameters.paniniProjectionCS.shaderKeywords = null;

            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            if (1f - Mathf.Abs(paniniD) > float.Epsilon)
            {
                parameters.paniniProjectionCS.EnableKeyword("GENERIC");
            }
            else
            {
                parameters.paniniProjectionCS.EnableKeyword("UNITDISTANCE");
            }

            parameters.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
            parameters.paniniProjectionKernel = parameters.paniniProjectionCS.FindKernel("KMain");

            return parameters;
        }

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse!
        static void DoPaniniProjection(in PaniniProjectionParameters parameters, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            var cs = parameters.paniniProjectionCS;
            int kernel = parameters.paniniProjectionKernel;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, parameters.paniniParams);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (parameters.width + 7) / 8, (parameters.height + 7) / 8, parameters.viewCount);
        }

        Vector2 CalcViewExtents(HDCamera camera)
        {
            float fovY = camera.camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = (float)camera.actualWidth / (float)camera.actualHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        Vector2 CalcCropExtents(HDCamera camera, float d)
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

        struct BloomParameters
        {
            public ComputeShader bloomPrefilterCS;
            public ComputeShader bloomBlurCS;
            public ComputeShader bloomUpsampleCS;

            public int bloomPrefilterKernel;
            public int bloomBlurKernel;
            public int bloomDownsampleKernel;
            public int bloomUpsampleKernel;

            public int viewCount;
            public int bloomMipCount;

            public float bloomScatterParam;
            public Vector4 thresholdParams;
            public Vector4[] bloomMipInfo;
        }

        BloomParameters PrepareBloomParameters(HDCamera camera)
        {
            BloomParameters parameters = new BloomParameters();
            parameters.viewCount = camera.viewCount;
            parameters.bloomMipCount = m_BloomMipCount;
            parameters.bloomMipInfo = m_BloomMipsInfo;

            parameters.bloomPrefilterCS = m_Resources.shaders.bloomPrefilterCS;
            parameters.bloomPrefilterKernel = parameters.bloomPrefilterCS.FindKernel("KMain");

            parameters.bloomPrefilterCS.shaderKeywords = null;
            if (m_Bloom.highQualityPrefiltering)
                parameters.bloomPrefilterCS.EnableKeyword("HIGH_QUALITY");
            else
                parameters.bloomPrefilterCS.EnableKeyword("LOW_QUALITY");
            if (m_EnableAlpha)
                parameters.bloomPrefilterCS.EnableKeyword("ENABLE_ALPHA");

            parameters.bloomBlurCS = m_Resources.shaders.bloomBlurCS;
            parameters.bloomBlurKernel = parameters.bloomBlurCS.FindKernel("KMain");
            parameters.bloomDownsampleKernel = parameters.bloomBlurCS.FindKernel("KDownsample");

            parameters.bloomUpsampleCS = m_Resources.shaders.bloomUpsampleCS;
            parameters.bloomUpsampleCS.shaderKeywords = null;

            var highQualityFiltering = m_Bloom.highQualityFiltering;
            // We switch to bilinear upsampling as it goes less wide than bicubic and due to our border/RTHandle handling, going wide on small resolution
            // where small mips have a strong influence, might result problematic.
            if (camera.actualWidth < 800 || camera.actualHeight < 450) highQualityFiltering = false;

            if (highQualityFiltering)
                parameters.bloomUpsampleCS.EnableKeyword("HIGH_QUALITY");
            else
                parameters.bloomUpsampleCS.EnableKeyword("LOW_QUALITY");

            parameters.bloomUpsampleKernel = parameters.bloomUpsampleCS.FindKernel("KMain");

            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            parameters.bloomScatterParam = scatter;

            parameters.thresholdParams = GetBloomThresholdParams();

            return parameters;
        }

        void ComputeBloomMipSizesAndScales(HDCamera camera)
        {
            var resolution = m_Bloom.resolution;
            float scaleW = 1f / ((int)resolution / 2f);
            float scaleH = 1f / ((int)resolution / 2f);

            // If the scene is less than 50% of 900p, then we operate on full res, since it's going to be cheap anyway and this might improve quality in challenging situations.
            if (camera.actualWidth < 800 || camera.actualHeight < 450)
            {
                scaleW = 1.0f;
                scaleH = 1.0f;
            }

            if (m_Bloom.anamorphic.value)
            {
                // Positive anamorphic ratio values distort vertically - negative is horizontal
                float anamorphism = m_PhysicalCamera.anamorphism * 0.5f;
                scaleW *= anamorphism < 0 ? 1f + anamorphism : 1f;
                scaleH *= anamorphism > 0 ? 1f - anamorphism : 1f;
            }

            // Determine the iteration count
            int maxSize = Mathf.Max(camera.actualWidth, camera.actualHeight);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 2 - (resolution == BloomResolution.Half ? 0 : 1));
            m_BloomMipCount = Mathf.Clamp(iterations, 1, k_MaxBloomMipCount);

            for (int i = 0; i < m_BloomMipCount; i++)
            {
                float p = 1f / Mathf.Pow(2f, i + 1f);
                float sw = scaleW * p;
                float sh = scaleH * p;
                int pw, ph;
                if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
                {
                    pw = Mathf.Max(1, Mathf.CeilToInt(sw * camera.actualWidth));
                    ph = Mathf.Max(1, Mathf.CeilToInt(sh * camera.actualHeight));
                }
                else
                {
                    pw = Mathf.Max(1, Mathf.RoundToInt(sw * camera.actualWidth));
                    ph = Mathf.Max(1, Mathf.RoundToInt(sh * camera.actualHeight));
                }
                var scale = new Vector2(sw, sh);
                var pixelSize = new Vector2Int(pw, ph);

                m_BloomMipsInfo[i] = new Vector4(pw, ph, sw, sh);
            }
        }

        Vector4 GetBloomThresholdParams()
        {
            const float k_Softness = 0.5f;
            float lthresh = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = lthresh * k_Softness + 1e-5f;
            return new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
        }

        void PrepareUberBloomParameters(ref UberPostParameters parameters, HDCamera camera)
        {
            float intensity = Mathf.Pow(2f, m_Bloom.intensity.value) - 1f; // Makes intensity easier to control
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            int dirtEnabled = m_Bloom.dirtTexture.value != null && m_Bloom.dirtIntensity.value > 0f ? 1 : 0;
            float dirtRatio = (float)dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = (float)camera.actualWidth / (float)camera.actualHeight;
            var dirtTileOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value * intensity;

            if (dirtRatio > screenRatio)
            {
                dirtTileOffset.x = screenRatio / dirtRatio;
                dirtTileOffset.z = (1f - dirtTileOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtTileOffset.y = dirtRatio / screenRatio;
                dirtTileOffset.w = (1f - dirtTileOffset.y) * 0.5f;
            }

            parameters.bloomDirtTexture = dirtTexture;
            parameters.bloomParams = new Vector4(intensity, dirtIntensity, 1f, dirtEnabled);
            parameters.bloomTint = (Vector4)tint;
            parameters.bloomDirtTileOffset = dirtTileOffset;
            parameters.bloomThreshold = GetBloomThresholdParams();
            parameters.bloomBicubicParams = new Vector4(m_BloomMipsInfo[0].x, m_BloomMipsInfo[0].y, 1.0f / m_BloomMipsInfo[0].x, 1.0f / m_BloomMipsInfo[0].y);
        }

        static void DoBloom(in BloomParameters bloomParameters, CommandBuffer cmd, RTHandle source, RTHandle[] bloomMipsDown, RTHandle[] bloomMipsUp)
        {
            // All the computes for this effect use the same group size so let's use a local
            // function to simplify dispatches
            // Make sure the thread group count is sufficient to draw the guard bands
            void DispatchWithGuardBands(ComputeShader shader, int kernelId, in Vector2Int size, in int viewCount)
            {
                int w = size.x;
                int h = size.y;

                if (w < source.rt.width && w % 8 < k_RTGuardBandSize)
                    w += k_RTGuardBandSize;
                if (h < source.rt.height && h % 8 < k_RTGuardBandSize)
                    h += k_RTGuardBandSize;

                cmd.DispatchCompute(shader, kernelId, (w + 7) / 8, (h + 7) / 8, viewCount);
            }

            // Pre-filtering
            ComputeShader cs;
            int kernel;

            {
                var size = new Vector2Int((int)bloomParameters.bloomMipInfo[0].x, (int)bloomParameters.bloomMipInfo[0].y);
                cs = bloomParameters.bloomPrefilterCS;
                kernel = bloomParameters.bloomPrefilterKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, bloomMipsUp[0]); // Use m_BloomMipsUp as temp target
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomThreshold, bloomParameters.thresholdParams);
                DispatchWithGuardBands(cs, kernel, size, bloomParameters.viewCount);

                cs = bloomParameters.bloomBlurCS;
                kernel = bloomParameters.bloomBlurKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, bloomMipsUp[0]);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, bloomMipsDown[0]);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                DispatchWithGuardBands(cs, kernel, size, bloomParameters.viewCount);
            }

            // Blur pyramid
            kernel = bloomParameters.bloomDownsampleKernel;

            for (int i = 0; i < bloomParameters.bloomMipCount - 1; i++)
            {
                var src = bloomMipsDown[i];
                var dst = bloomMipsDown[i + 1];
                var size = new Vector2Int((int)bloomParameters.bloomMipInfo[i + 1].x, (int)bloomParameters.bloomMipInfo[i + 1].y);

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, src);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                DispatchWithGuardBands(cs, kernel, size, bloomParameters.viewCount);
            }

            // Upsample & combine
            cs = bloomParameters.bloomUpsampleCS;
            kernel = bloomParameters.bloomUpsampleKernel;

            for (int i = bloomParameters.bloomMipCount - 2; i >= 0; i--)
            {
                var low = (i == bloomParameters.bloomMipCount - 2) ? bloomMipsDown : bloomMipsUp;
                var srcLow = low[i + 1];
                var srcHigh = bloomMipsDown[i];
                var dst = bloomMipsUp[i];
                var highSize = new Vector2Int((int)bloomParameters.bloomMipInfo[i].x, (int)bloomParameters.bloomMipInfo[i].y);
                var lowSize = new Vector2Int((int)bloomParameters.bloomMipInfo[i + 1].x, (int)bloomParameters.bloomMipInfo[i + 1].y);

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputLowTexture, srcLow);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHighTexture, srcHigh);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(bloomParameters.bloomScatterParam, 0f, 0f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomBicubicParams, new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
                DispatchWithGuardBands(cs, kernel, highSize, bloomParameters.viewCount);
            }
        }

        #endregion

        #region Lens Distortion

        void PrepareLensDistortionParameters(ref UberPostParameters parameters, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.LensDistortion) != UberPostFeatureFlags.LensDistortion)
                return;

            parameters.uberPostCS.EnableKeyword("LENS_DISTORTION");

            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            parameters.lensDistortionParams1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            parameters.lensDistortionParams2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );
        }

        #endregion

        #region Chromatic Aberration

        void PrepareChromaticAberrationParameters(ref UberPostParameters parameters, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;

            parameters.uberPostCS.EnableKeyword("CHROMATIC_ABERRATION");

            var spectralLut = m_ChromaticAberration.spectralLut.value;

            // If no spectral lut is set, use a pre-generated one
            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, TextureFormat.RGB24, false)
                    {
                        name = "Chromatic Aberration Spectral LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new[]
                    {
                        new Color(1f, 0f, 0f),
                        new Color(0f, 1f, 0f),
                        new Color(0f, 0f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }

            parameters.spectralLut = spectralLut;
            parameters.chromaticAberrationParameters = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples, 0f, 0f);
        }

        #endregion

        #region Vignette

        void PrepareVignetteParameters(ref UberPostParameters parameters, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            parameters.uberPostCS.EnableKeyword("VIGNETTE");

            if (m_Vignette.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                parameters.vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
                parameters.vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? 1f : 0f);
                parameters.vignetteColor = m_Vignette.color.value;
                parameters.vignetteMask = Texture2D.blackTexture;
            }
            else // Masked
            {
                var color = m_Vignette.color.value;
                color.a = Mathf.Clamp01(m_Vignette.opacity.value);

                parameters.vignetteParams1 = new Vector4(0f, 0f, 1f, 0f);
                parameters.vignetteColor = color;
                parameters.vignetteMask = m_Vignette.mask.value;
            }
        }

        #endregion

        #region Color Grading

        struct ColorGradingParameters
        {
            public ComputeShader builderCS;
            public int builderKernel;

            public int lutSize;

            public Vector4 colorFilter;
            public Vector3 lmsColorBalance;
            public Vector4 hueSatCon;
            public Vector4 channelMixerR;
            public Vector4 channelMixerG;
            public Vector4 channelMixerB;
            public Vector4 shadows;
            public Vector4 midtones;
            public Vector4 highlights;
            public Vector4 shadowsHighlightsLimits;
            public Vector4 lift;
            public Vector4 gamma;
            public Vector4 gain;
            public Vector4 splitShadows;
            public Vector4 splitHighlights;

            public ColorCurves curves;
            public HableCurve hableCurve;

            public Vector4 miscParams;

            public Texture externalLuT;
            public float lutContribution;

            public TonemappingMode tonemappingMode;
        }

        ColorGradingParameters PrepareColorGradingParameters()
        {
            var parameters = new ColorGradingParameters();

            parameters.tonemappingMode = m_TonemappingFS ? m_Tonemapping.mode.value : TonemappingMode.None;

            parameters.builderCS = m_Resources.shaders.lutBuilder3DCS;
            parameters.builderKernel = parameters.builderCS.FindKernel("KBuild");

            // Setup lut builder compute & grab the kernel we need
            parameters.builderCS.shaderKeywords = null;

            if (m_Tonemapping.IsActive() && m_TonemappingFS)
            {
                switch (parameters.tonemappingMode)
                {
                    case TonemappingMode.Neutral: parameters.builderCS.EnableKeyword("TONEMAPPING_NEUTRAL"); break;
                    case TonemappingMode.ACES: parameters.builderCS.EnableKeyword("TONEMAPPING_ACES"); break;
                    case TonemappingMode.Custom: parameters.builderCS.EnableKeyword("TONEMAPPING_CUSTOM"); break;
                    case TonemappingMode.External: parameters.builderCS.EnableKeyword("TONEMAPPING_EXTERNAL"); break;
                }
            }
            else
            {
                parameters.builderCS.EnableKeyword("TONEMAPPING_NONE");
            }

            parameters.lutSize = m_LutSize;

            //parameters.colorFilter;
            parameters.lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            parameters.hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            parameters.channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value / 100f, m_ChannelMixer.redOutGreenIn.value / 100f, m_ChannelMixer.redOutBlueIn.value / 100f, 0f);
            parameters.channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            parameters.channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value / 100f, m_ChannelMixer.blueOutGreenIn.value / 100f, m_ChannelMixer.blueOutBlueIn.value / 100f, 0f);

            ComputeShadowsMidtonesHighlights(out parameters.shadows, out parameters.midtones, out parameters.highlights, out parameters.shadowsHighlightsLimits);
            ComputeLiftGammaGain(out parameters.lift, out parameters.gamma, out parameters.gain);
            ComputeSplitToning(out parameters.splitShadows, out parameters.splitHighlights);

            // Be careful, if m_Curves is modified between preparing the render pass and executing it, result will be wrong.
            // However this should be fine for now as all updates should happen outisde rendering.
            parameters.curves = m_Curves;

            if (parameters.tonemappingMode == TonemappingMode.Custom)
            {
                parameters.hableCurve = m_HableCurve;
                parameters.hableCurve.Init(
                    m_Tonemapping.toeStrength.value,
                    m_Tonemapping.toeLength.value,
                    m_Tonemapping.shoulderStrength.value,
                    m_Tonemapping.shoulderLength.value,
                    m_Tonemapping.shoulderAngle.value,
                    m_Tonemapping.gamma.value
                );
            }
            else if (parameters.tonemappingMode == TonemappingMode.External)
            {
                parameters.externalLuT = m_Tonemapping.lutTexture.value;
                parameters.lutContribution = m_Tonemapping.lutContribution.value;
            }

            parameters.colorFilter = m_ColorAdjustments.colorFilter.value.linear;
            parameters.miscParams = new Vector4(m_ColorGradingFS ? 1f : 0f, 0f, 0f, 0f);

            return parameters;
        }

        // TODO: User lut support
        static void DoColorGrading(in ColorGradingParameters parameters,
            RTHandle internalLogLuT,
            CommandBuffer cmd)
        {
            var builderCS = parameters.builderCS;
            var builderKernel = parameters.builderKernel;

            // Fill-in constant buffers & textures
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._OutputTexture, internalLogLuT);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Size, new Vector4(parameters.lutSize, 1f / (parameters.lutSize - 1f), 0f, 0f));
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorBalance, parameters.lmsColorBalance);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorFilter, parameters.colorFilter);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerRed, parameters.channelMixerR);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerGreen, parameters.channelMixerG);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerBlue, parameters.channelMixerB);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._HueSatCon, parameters.hueSatCon);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Lift, parameters.lift);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gamma, parameters.gamma);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gain, parameters.gain);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Shadows, parameters.shadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Midtones, parameters.midtones);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Highlights, parameters.highlights);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShaHiLimits, parameters.shadowsHighlightsLimits);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitShadows, parameters.splitShadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitHighlights, parameters.splitHighlights);

            // YRGB
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveMaster, parameters.curves.master.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveRed, parameters.curves.red.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveGreen, parameters.curves.green.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveBlue, parameters.curves.blue.value.GetTexture());

            // Secondary curves
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsHue, parameters.curves.hueVsHue.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsSat, parameters.curves.hueVsSat.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveLumVsSat, parameters.curves.lumVsSat.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveSatVsSat, parameters.curves.satVsSat.value.GetTexture());

            // Artist-driven tonemap curve
            if (parameters.tonemappingMode == TonemappingMode.Custom)
            {
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._CustomToneCurve, parameters.hableCurve.uniforms.curve);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentA, parameters.hableCurve.uniforms.toeSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentB, parameters.hableCurve.uniforms.toeSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentA, parameters.hableCurve.uniforms.midSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentB, parameters.hableCurve.uniforms.midSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentA, parameters.hableCurve.uniforms.shoSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentB, parameters.hableCurve.uniforms.shoSegmentB);
            }
            else if (parameters.tonemappingMode == TonemappingMode.External)
            {
                cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._LogLut3D, parameters.externalLuT);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._LogLut3D_Params, new Vector4(1f / parameters.lutSize, parameters.lutSize - 1f, parameters.lutContribution, 0f));
            }

            // Misc parameters
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Params, parameters.miscParams);

            // Generate the lut
            // See the note about Metal & Intel in LutBuilder3D.compute
            // GetKernelThreadGroupSizes  is currently broken on some binary versions.
            //builderCS.GetKernelThreadGroupSizes(builderKernel, out uint threadX, out uint threadY, out uint threadZ);
            uint threadX = 4;
            uint threadY = 4;
            uint threadZ = 4;
            cmd.DispatchCompute(builderCS, builderKernel,
                (int)((parameters.lutSize + threadX - 1u) / threadX),
                (int)((parameters.lutSize + threadY - 1u) / threadY),
                (int)((parameters.lutSize + threadZ - 1u) / threadZ)
            );
        }

        // Returns color balance coefficients in the LMS space
        public static Vector3 GetColorBalanceCoeffs(float temperature, float tint)
        {
            // Range ~[-1.5;1.5] works best
            float t1 = temperature / 65f;
            float t2 = tint / 65f;

            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
            float y = ColorUtils.StandardIlluminantY(x) + t2 * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = ColorUtils.CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        void ComputeShadowsMidtonesHighlights(out Vector4 shadows, out Vector4 midtones, out Vector4 highlights, out Vector4 limits)
        {
            float weight;

            shadows = m_ShadowsMidtonesHighlights.shadows.value;
            shadows.x = Mathf.GammaToLinearSpace(shadows.x);
            shadows.y = Mathf.GammaToLinearSpace(shadows.y);
            shadows.z = Mathf.GammaToLinearSpace(shadows.z);
            weight = shadows.w * (Mathf.Sign(shadows.w) < 0f ? 1f : 4f);
            shadows.x = Mathf.Max(shadows.x + weight, 0f);
            shadows.y = Mathf.Max(shadows.y + weight, 0f);
            shadows.z = Mathf.Max(shadows.z + weight, 0f);
            shadows.w = 0f;

            midtones = m_ShadowsMidtonesHighlights.midtones.value;
            midtones.x = Mathf.GammaToLinearSpace(midtones.x);
            midtones.y = Mathf.GammaToLinearSpace(midtones.y);
            midtones.z = Mathf.GammaToLinearSpace(midtones.z);
            weight = midtones.w * (Mathf.Sign(midtones.w) < 0f ? 1f : 4f);
            midtones.x = Mathf.Max(midtones.x + weight, 0f);
            midtones.y = Mathf.Max(midtones.y + weight, 0f);
            midtones.z = Mathf.Max(midtones.z + weight, 0f);
            midtones.w = 0f;

            highlights = m_ShadowsMidtonesHighlights.highlights.value;
            highlights.x = Mathf.GammaToLinearSpace(highlights.x);
            highlights.y = Mathf.GammaToLinearSpace(highlights.y);
            highlights.z = Mathf.GammaToLinearSpace(highlights.z);
            weight = highlights.w * (Mathf.Sign(highlights.w) < 0f ? 1f : 4f);
            highlights.x = Mathf.Max(highlights.x + weight, 0f);
            highlights.y = Mathf.Max(highlights.y + weight, 0f);
            highlights.z = Mathf.Max(highlights.z + weight, 0f);
            highlights.w = 0f;

            limits = new Vector4(
                m_ShadowsMidtonesHighlights.shadowsStart.value,
                m_ShadowsMidtonesHighlights.shadowsEnd.value,
                m_ShadowsMidtonesHighlights.highlightsStart.value,
                m_ShadowsMidtonesHighlights.highlightsEnd.value
            );
        }

        void ComputeLiftGammaGain(out Vector4 lift, out Vector4 gamma, out Vector4 gain)
        {
            lift = m_LiftGammaGain.lift.value;
            lift.x = Mathf.GammaToLinearSpace(lift.x) * 0.15f;
            lift.y = Mathf.GammaToLinearSpace(lift.y) * 0.15f;
            lift.z = Mathf.GammaToLinearSpace(lift.z) * 0.15f;

            float lumLift = ColorUtils.Luminance(lift);
            lift.x = lift.x - lumLift + lift.w;
            lift.y = lift.y - lumLift + lift.w;
            lift.z = lift.z - lumLift + lift.w;
            lift.w = 0f;

            gamma = m_LiftGammaGain.gamma.value;
            gamma.x = Mathf.GammaToLinearSpace(gamma.x) * 0.8f;
            gamma.y = Mathf.GammaToLinearSpace(gamma.y) * 0.8f;
            gamma.z = Mathf.GammaToLinearSpace(gamma.z) * 0.8f;

            float lumGamma = ColorUtils.Luminance(gamma);
            gamma.w += 1f;
            gamma.x = 1f / Mathf.Max(gamma.x - lumGamma + gamma.w, 1e-03f);
            gamma.y = 1f / Mathf.Max(gamma.y - lumGamma + gamma.w, 1e-03f);
            gamma.z = 1f / Mathf.Max(gamma.z - lumGamma + gamma.w, 1e-03f);
            gamma.w = 0f;

            gain = m_LiftGammaGain.gain.value;
            gain.x = Mathf.GammaToLinearSpace(gain.x) * 0.8f;
            gain.y = Mathf.GammaToLinearSpace(gain.y) * 0.8f;
            gain.z = Mathf.GammaToLinearSpace(gain.z) * 0.8f;

            float lumGain = ColorUtils.Luminance(gain);
            gain.w += 1f;
            gain.x = gain.x - lumGain + gain.w;
            gain.y = gain.y - lumGain + gain.w;
            gain.z = gain.z - lumGain + gain.w;
            gain.w = 0f;
        }

        void ComputeSplitToning(out Vector4 shadows, out Vector4 highlights)
        {
            // As counter-intuitive as it is, to make split-toning work the same way it does in
            // Adobe products we have to do all the maths in sRGB... So do not convert these to
            // linear before sending them to the shader, this isn't a bug!
            shadows = m_SplitToning.shadows.value;
            highlights = m_SplitToning.highlights.value;

            // Balance is stored in `shadows.w`
            shadows.w = m_SplitToning.balance.value / 100f;
            highlights.w = 0f;
        }

        #endregion

        #region FXAA
        struct FXAAParameters
        {
            public ComputeShader fxaaCS;
            public int fxaaKernel;

            public int width;
            public int height;
            public int viewCount;
        }

        FXAAParameters PrepareFXAAParameters(HDCamera camera)
        {
            FXAAParameters parameters = new FXAAParameters();
            parameters.fxaaCS = m_Resources.shaders.FXAACS;
            parameters.fxaaKernel = parameters.fxaaCS.FindKernel("FXAA");

            parameters.width = camera.actualWidth;
            parameters.height = camera.actualHeight;
            parameters.viewCount = camera.viewCount;

            return parameters;
        }

        void DoFXAA(in FXAAParameters parameters, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            var cs = parameters.fxaaCS;
            int kernel = parameters.fxaaKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (parameters.width + 7) / 8, (parameters.height + 7) / 8, parameters.viewCount);
        }

        #endregion

        #region SMAA

        struct SMAAParameters
        {
            public Material smaaMaterial;
            public Texture smaaAreaTex;
            public Texture smaaSearchTex;

            public Vector4 smaaRTMetrics;
        }

        SMAAParameters PrepareSMAAParameters(HDCamera camera)
        {
            SMAAParameters parameters = new SMAAParameters();
            parameters.smaaMaterial = m_SMAAMaterial;
            parameters.smaaAreaTex = m_Resources.textures.SMAAAreaTex;
            parameters.smaaSearchTex = m_Resources.textures.SMAASearchTex;
            parameters.smaaMaterial.shaderKeywords = null;

            switch (camera.SMAAQuality)
            {
                case HDAdditionalCameraData.SMAAQualityLevel.Low:
                    parameters.smaaMaterial.EnableKeyword("SMAA_PRESET_LOW");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                    parameters.smaaMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.High:
                    parameters.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
                default:
                    parameters.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
            }

            parameters.smaaRTMetrics = new Vector4(1.0f / camera.actualWidth, 1.0f / camera.actualHeight, camera.actualWidth, camera.actualHeight);

            return parameters;
        }

        static void DoSMAA(in SMAAParameters parameters, CommandBuffer cmd, RTHandle source, RTHandle smaaEdgeTex, RTHandle smaaBlendTex, RTHandle destination, RTHandle depthBuffer)
        {
            parameters.smaaMaterial.SetVector(HDShaderIDs._SMAARTMetrics, parameters.smaaRTMetrics);
            parameters.smaaMaterial.SetTexture(HDShaderIDs._SMAAAreaTex, parameters.smaaAreaTex);
            parameters.smaaMaterial.SetTexture(HDShaderIDs._SMAASearchTex, parameters.smaaSearchTex);
            parameters.smaaMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SMAA);
            parameters.smaaMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SMAA);

            // -----------------------------------------------------------------------------
            // Clear
            CoreUtils.SetRenderTarget(cmd, smaaEdgeTex, ClearFlag.Color);
            CoreUtils.SetRenderTarget(cmd, smaaBlendTex, ClearFlag.Color);

            // -----------------------------------------------------------------------------
            // EdgeDetection stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, source);
            HDUtils.DrawFullScreen(cmd, parameters.smaaMaterial, smaaEdgeTex, depthBuffer, null, (int)SMAAStage.EdgeDetection);

            // -----------------------------------------------------------------------------
            // BlendWeights stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, smaaEdgeTex);
            HDUtils.DrawFullScreen(cmd, parameters.smaaMaterial, smaaBlendTex, depthBuffer, null, (int)SMAAStage.BlendWeights);

            // -----------------------------------------------------------------------------
            // NeighborhoodBlending stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, source);
            parameters.smaaMaterial.SetTexture(HDShaderIDs._SMAABlendTex, smaaBlendTex);
            HDUtils.DrawFullScreen(cmd, parameters.smaaMaterial, destination, null, (int)SMAAStage.NeighborhoodBlending);
        }

        #endregion

        #region CAS
        struct CASParameters
        {
            public ComputeShader casCS;
            public int initKernel;
            public int mainKernel;
            public int viewCount;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int outputHeight;
        }

        CASParameters PrepareContrastAdaptiveSharpeningParameters(HDCamera camera)
        {
            CASParameters parameters = new CASParameters();

            parameters.casCS = m_Resources.shaders.contrastAdaptiveSharpenCS;
            parameters.initKernel = parameters.casCS.FindKernel("KInitialize");
            parameters.mainKernel = parameters.casCS.FindKernel("KMain");

            parameters.viewCount = camera.viewCount;

            parameters.inputWidth = camera.actualWidth;
            parameters.inputHeight = camera.actualHeight;
            parameters.outputWidth = Mathf.RoundToInt(camera.finalViewport.width);
            parameters.outputHeight = Mathf.RoundToInt(camera.finalViewport.height);

            return parameters;
        }

        static void DoContrastAdaptiveSharpening(in CASParameters parameters, CommandBuffer cmd, RTHandle source, RTHandle destination, ComputeBuffer casParametersBuffer)
        {
            var cs = parameters.casCS;
            int kInit = parameters.initKernel;
            int kMain = parameters.mainKernel;
            if (kInit >= 0 && kMain >= 0)
            {
                cmd.SetComputeFloatParam(cs, HDShaderIDs._Sharpness, 1);
                cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._InputTexture, source);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._InputTextureDimensions, new Vector4(parameters.inputWidth, parameters.inputHeight));
                cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._OutputTexture, destination);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._OutputTextureDimensions, new Vector4(parameters.outputWidth, parameters.outputHeight));

                cmd.SetComputeBufferParam(cs, kInit, "CasParameters", casParametersBuffer);
                cmd.SetComputeBufferParam(cs, kMain, "CasParameters", casParametersBuffer);

                cmd.DispatchCompute(cs, kInit, 1, 1, 1);

                int dispatchX = HDUtils.DivRoundUp(parameters.outputWidth, 16);
                int dispatchY = HDUtils.DivRoundUp(parameters.outputHeight, 16);

                cmd.DispatchCompute(cs, kMain, dispatchX, dispatchY, parameters.viewCount);
            }
        }

        #endregion

        #region Final Pass

        struct FinalPassParameters
        {
            public bool             postProcessEnabled;
            public Material         finalPassMaterial;
            public HDCamera         hdCamera;
            public BlueNoise        blueNoise;
            public bool             flipY;
            public System.Random    random;
            public bool             useFXAA;
            public bool             enableAlpha;
            public bool             keepAlpha;

            public bool             filmGrainEnabled;
            public Texture          filmGrainTexture;
            public float            filmGrainIntensity;
            public float            filmGrainResponse;

            public bool             ditheringEnabled;
        }

        FinalPassParameters PrepareFinalPass(HDCamera hdCamera, BlueNoise blueNoise, bool flipY)
        {
            FinalPassParameters parameters = new FinalPassParameters();

            // General
            parameters.postProcessEnabled = m_PostProcessEnabled;
            parameters.finalPassMaterial = m_FinalPassMaterial;
            parameters.hdCamera = hdCamera;
            parameters.blueNoise = blueNoise;
            parameters.flipY = flipY;
            parameters.random = m_Random;
            parameters.enableAlpha = m_EnableAlpha;
            parameters.keepAlpha = m_KeepAlpha;

            var dynResHandler = DynamicResolutionHandler.instance;
            bool dynamicResIsOn = hdCamera.isMainGameView && dynResHandler.DynamicResolutionEnabled();
            parameters.useFXAA = hdCamera.antialiasing == AntialiasingMode.FastApproximateAntialiasing && !dynamicResIsOn && m_AntialiasingFS;

            // Film Grain
            parameters.filmGrainEnabled = m_FilmGrain.IsActive() && m_FilmGrainFS;
            if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                parameters.filmGrainTexture = m_Resources.textures.filmGrainTex[(int)m_FilmGrain.type.value];
            else
                parameters.filmGrainTexture = m_FilmGrain.texture.value;
            parameters.filmGrainIntensity = m_FilmGrain.intensity.value;
            parameters.filmGrainResponse = m_FilmGrain.response.value;

            // Dithering
            parameters.ditheringEnabled = hdCamera.dithering && m_DitheringFS;

            return parameters;
        }

        static void DoFinalPass(in FinalPassParameters  parameters,
            RTHandle                source,
            RTHandle                afterPostProcessTexture,
            RenderTargetIdentifier  destination,
            RTHandle                alphaTexture,
            CommandBuffer           cmd)
        {
            // Final pass has to be done in a pixel shader as it will be the one writing straight
            // to the backbuffer eventually

            Material finalPassMaterial = parameters.finalPassMaterial;
            HDCamera hdCamera = parameters.hdCamera;
            bool flipY = parameters.flipY;

            finalPassMaterial.shaderKeywords = null;
            finalPassMaterial.SetTexture(HDShaderIDs._InputTexture, source);

            var dynResHandler = DynamicResolutionHandler.instance;
            bool dynamicResIsOn = hdCamera.isMainGameView && dynResHandler.DynamicResolutionEnabled();

            if (dynamicResIsOn)
            {
                switch (dynResHandler.filter)
                {
                    case DynamicResUpscaleFilter.Bilinear:
                        finalPassMaterial.EnableKeyword("BILINEAR");
                        break;
                    case DynamicResUpscaleFilter.CatmullRom:
                        finalPassMaterial.EnableKeyword("CATMULL_ROM_4");
                        break;
                    case DynamicResUpscaleFilter.Lanczos:
                        finalPassMaterial.EnableKeyword("LANCZOS");
                        break;
                    case DynamicResUpscaleFilter.ContrastAdaptiveSharpen:
                        finalPassMaterial.EnableKeyword("CONTRASTADAPTIVESHARPEN");
                        break;
                }
            }

            if (parameters.postProcessEnabled)
            {
                if (parameters.useFXAA)
                    finalPassMaterial.EnableKeyword("FXAA");

                if (parameters.filmGrainEnabled)
                {
                    if (parameters.filmGrainTexture != null) // Fail safe if the resources asset breaks :/
                    {
                        #if HDRP_DEBUG_STATIC_POSTFX
                        float offsetX = 0;
                        float offsetY = 0;
                        #else
                        float offsetX = (float)(parameters.random.NextDouble());
                        float offsetY = (float)(parameters.random.NextDouble());
                        #endif

                        finalPassMaterial.EnableKeyword("GRAIN");
                        finalPassMaterial.SetTexture(HDShaderIDs._GrainTexture, parameters.filmGrainTexture);
                        finalPassMaterial.SetVector(HDShaderIDs._GrainParams, new Vector2(parameters.filmGrainIntensity * 4f, parameters.filmGrainResponse));

                        float uvScaleX = parameters.hdCamera.actualWidth / (float)parameters.filmGrainTexture.width;
                        float uvScaleY = parameters.hdCamera.actualHeight / (float)parameters.filmGrainTexture.height;
                        float scaledOffsetX = offsetX * uvScaleX;
                        float scaledOffsetY = offsetY * uvScaleY;

                        finalPassMaterial.SetVector(HDShaderIDs._GrainTextureParams, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                    }
                }

                if (parameters.ditheringEnabled)
                {
                    var blueNoiseTexture = parameters.blueNoise.textureArray16L;

                    #if HDRP_DEBUG_STATIC_POSTFX
                    int textureId = 0;
                    #else
                    int textureId = (int)hdCamera.GetCameraFrameCount() % blueNoiseTexture.depth;
                    #endif

                    finalPassMaterial.EnableKeyword("DITHER");
                    finalPassMaterial.SetTexture(HDShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                    finalPassMaterial.SetVector(HDShaderIDs._DitherParams, new Vector3(parameters.hdCamera.actualWidth / blueNoiseTexture.width,
                        parameters.hdCamera.actualHeight / blueNoiseTexture.height, textureId));
                }
            }

            finalPassMaterial.SetTexture(HDShaderIDs._AlphaTexture, alphaTexture);
            finalPassMaterial.SetFloat(HDShaderIDs._KeepAlpha, parameters.keepAlpha ? 1.0f : 0.0f);

            if (parameters.enableAlpha)
                finalPassMaterial.EnableKeyword("ENABLE_ALPHA");
            else
                finalPassMaterial.DisableKeyword("ENABLE_ALPHA");

            finalPassMaterial.SetVector(HDShaderIDs._UVTransform,
                flipY
                ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f)
                : new Vector4(1.0f,  1.0f, 0.0f, 0.0f)
            );

            finalPassMaterial.SetVector(HDShaderIDs._ViewPortSize,
                new Vector4(hdCamera.finalViewport.width, hdCamera.finalViewport.height, 1.0f / hdCamera.finalViewport.width, 1.0f / hdCamera.finalViewport.height));

            // Blit to backbuffer
            Rect backBufferRect = hdCamera.finalViewport;

            // When post process is not the final pass, we render at (0,0) so that subsequent rendering does not have to bother about viewports.
            // Final viewport is handled in the final blit in this case
            if (!HDUtils.PostProcessIsFinalPass(hdCamera))
            {
                backBufferRect.x = backBufferRect.y = 0;
            }

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
            {
                finalPassMaterial.EnableKeyword("APPLY_AFTER_POST");
                finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, afterPostProcessTexture);
            }
            else
            {
                finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, TextureXR.GetBlackTexture());
            }

            HDUtils.DrawFullScreen(cmd, backBufferRect, finalPassMaterial, destination);
        }

        #endregion
    }
}
