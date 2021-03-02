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
        const GraphicsFormat k_ExposureFormat   = GraphicsFormat.R32G32_SFloat;

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
        Vector4 m_BloomBicubicParams; // Needed for uber pass

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
        bool m_ExposureControlFS;
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

        bool m_IsDoFHisotoryValid = false;

        static void SetExposureTextureToEmpty(RTHandle exposureTexture)
        {
            var tex = new Texture2D(1, 1, GraphicsFormat.R16G16_SFloat, TextureCreationFlags.None);
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
            m_ExposureControlFS     = frameSettings.IsEnabled(FrameSettingsField.ExposureControl);
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
            if (!m_ExposureControlFS)
            {
                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, m_EmptyExposureTexture);
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, m_EmptyExposureTexture);
            }
            else
            {
                // Fix exposure is store in Exposure Textures at the beginning of the frame as there is no need for color buffer
                // Dynamic exposure (Auto, curve) is store in Exposure Textures at the end of the frame (as it rely on color buffer)
                // Texture current and previous are swapped at the beginning of the frame.
                bool isFixedExposure = IsExposureFixed(camera);
                if (isFixedExposure)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FixedExposure)))
                    {
                        DoFixedExposure(camera, cmd);
                    }
                }

                // Note: GetExposureTexture(camera) must be call AFTER the call of DoFixedExposure to be correctly taken into account
                // When we use Dynamic Exposure and we reset history we can't use pre-exposure (as there is no information)
                // For this reasons we put neutral value at the beginning of the frame in Exposure textures and
                // apply processed exposure from color buffer at the end of the Frame, only for a single frame.
                // After that we re-use the pre-exposure system
                RTHandle currentExposureTexture = (camera.resetPostProcessingHistory && !isFixedExposure) ? m_EmptyExposureTexture : GetExposureTexture(camera);

                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, currentExposureTexture);
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, GetPreviousExposureTexture(camera));
            }
        }

        static void ValidateComputeBuffer(ref ComputeBuffer cb, int size, int stride, ComputeBufferType type = ComputeBufferType.Default)
        {
            if (cb == null || cb.count < size)
            {
                CoreUtils.SafeRelease(cb);
                cb = new ComputeBuffer(size, stride, type);
            }
        }

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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsExposureFixed(HDCamera camera) => m_Exposure.mode.value == ExposureMode.Fixed || m_Exposure.mode.value == ExposureMode.UsePhysicalCamera
            #if UNITY_EDITOR
        || (camera.camera.cameraType == CameraType.SceneView && HDAdditionalSceneViewSettings.sceneExposureOverriden)
            #endif
        ;

        public RTHandle GetExposureTexture(HDCamera camera)
        {
            // 1x1 pixel, holds the current exposure multiplied in the red channel and EV100 value
            // in the green channel
            // One frame delay + history RTs being flipped at the beginning of the frame means we
            // have to grab the exposure marked as "previous"
            var rt = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
            return rt ?? m_EmptyExposureTexture;
        }

        public RTHandle GetPreviousExposureTexture(HDCamera camera)
        {
            // See GetExposureTexture
            var rt = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Exposure);
            return rt ?? m_EmptyExposureTexture;
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

        void DoFixedExposure(HDCamera hdCamera, CommandBuffer cmd)
        {
            ComputeShader cs = m_Resources.shaders.exposureCS;
            int kernel = 0;
            Vector4 exposureParams;
            Vector4 exposureParams2 = new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);
            if (m_Exposure.mode.value == ExposureMode.Fixed
#if UNITY_EDITOR
                || HDAdditionalSceneViewSettings.sceneExposureOverriden && hdCamera.camera.cameraType == CameraType.SceneView
#endif
            )
            {
                kernel = cs.FindKernel("KFixedExposure");
                exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, m_Exposure.fixedExposure.value, 0f, 0f);
#if UNITY_EDITOR
                if (HDAdditionalSceneViewSettings.sceneExposureOverriden && hdCamera.camera.cameraType == CameraType.SceneView)
                {
                    exposureParams = new Vector4(0.0f, HDAdditionalSceneViewSettings.sceneExposure, 0f, 0f);
                }
#endif
            }
            else // ExposureMode.UsePhysicalCamera
            {
                kernel = cs.FindKernel("KManualCameraExposure");
                exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, m_PhysicalCamera.aperture, m_PhysicalCamera.shutterSpeed, m_PhysicalCamera.iso);
            }

            RTHandle prevExposure;
            GrabExposureHistoryTextures(hdCamera, out prevExposure, out _);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, exposureParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, prevExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        static void GrabExposureHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                // r: multiplier, g: EV100
                var rt = rtHandleSystem.Alloc(1, 1, colorFormat: k_ExposureFormat,
                    enableRandomWrite: true, name: $"{id} Exposure Texture {frameIndex}"
                );
                SetExposureTextureToEmpty(rt);
                return rt;
            }

            // We rely on the RT history system that comes with HDCamera, but because it is swapped
            // at the beginning of the frame and exposure is applied with a one-frame delay it means
            // that 'current' and 'previous' are swapped
            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Exposure)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Exposure, Allocator, 2);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
        }

        void PrepareExposureCurveData(out float min, out float max)
        {
            var curve = m_Exposure.curveMap.value;
            var minCurve = m_Exposure.limitMinCurveMap.value;
            var maxCurve = m_Exposure.limitMaxCurveMap.value;

            if (m_ExposureCurveTexture == null)
            {
                m_ExposureCurveTexture = new Texture2D(k_ExposureCurvePrecision, 1, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None)
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
            public ComputeShader dofCoCPyramidCS;
            public int dofCoCPyramidKernel;
            public ComputeShader pbDoFGatherCS;
            public int pbDoFGatherKernel;

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
            parameters.dofCoCPyramidCS = m_Resources.shaders.DoFCoCPyramidCS;
            parameters.dofCoCPyramidKernel = parameters.dofCoCPyramidCS.FindKernel("KMainCoCPyramid");
            parameters.pbDoFGatherCS = m_Resources.shaders.dofGatherCS;
            parameters.pbDoFGatherKernel = parameters.pbDoFGatherCS.FindKernel("KMain");

            parameters.camera = camera;
            parameters.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            parameters.nearLayerActive = m_DepthOfField.IsNearLayerActive();
            parameters.farLayerActive = m_DepthOfField.IsFarLayerActive();
            parameters.highQualityFiltering = m_DepthOfField.highQualityFiltering;
            parameters.useTiles = !camera.xr.singlePassEnabled;

            parameters.resolution = m_DepthOfField.resolution;

            float scale = 1f / (float)parameters.resolution;
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

        static void GetMipMapDimensions(RTHandle texture, int lod, out int width, out int height)
        {
            width = texture.rt.width;
            height = texture.rt.height;

            for (int level = 0; level < lod; ++level)
            {
                // Note: When the texture/mip size is an odd number, the size of the next level is rounded down.
                // That's why we cannot find the actual size by doing (size >> lod).
                width /= 2;
                height /= 2;
            }
        }

        static void DoPhysicallyBasedDepthOfField(in DepthOfFieldParameters dofParameters, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle fullresCoC, RTHandle prevCoCHistory, RTHandle nextCoCHistory, RTHandle motionVecTexture, RTHandle sourcePyramid, RTHandle depthBuffer, bool taaEnabled)
        {
            float scale = 1f / (float)dofParameters.resolution;
            int targetWidth = Mathf.RoundToInt(dofParameters.camera.actualWidth * scale);
            int targetHeight = Mathf.RoundToInt(dofParameters.camera.actualHeight * scale);

            // Map the old "max radius" parameters to a bigger range, so we can work on more challenging scenes
            float maxRadius = Mathf.Max(dofParameters.farMaxBlur, dofParameters.nearMaxBlur);
            float cocLimit = Mathf.Clamp(4 * maxRadius, 1, 64); //[1, 16] --> [1, 64]

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
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(cocLimit, 0.0f, cocScale, cocBias));
                }
                else
                {
                    float nearEnd = dofParameters.nearFocusEnd;
                    float nearStart = Mathf.Min(dofParameters.nearFocusStart, nearEnd - 1e-5f);
                    float farStart = Mathf.Max(dofParameters.farFocusStart, nearEnd);
                    float farEnd = Mathf.Max(dofParameters.farFocusEnd, farStart + 1e-5f);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(farStart, nearEnd, 1.0f / (farEnd - farStart), 1.0f / (nearStart - nearEnd)));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(dofParameters.nearMaxBlur, dofParameters.farMaxBlur, 0, 0));
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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCombine)))
            {
                cs = dofParameters.pbDoFGatherCS;
                kernel = dofParameters.pbDoFGatherKernel;
                float sampleCount = Mathf.Max(dofParameters.nearSampleCount, dofParameters.farSampleCount);
                float anamorphism = dofParameters.physicalCameraAnamorphism / 4f;

                float mipLevel = 1 + Mathf.Ceil(Mathf.Log(cocLimit, 2));
                GetMipMapDimensions(fullresCoC, (int)mipLevel, out var mipMapWidth, out var mipMapHeight);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(sampleCount, cocLimit, anamorphism, 0.0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(mipLevel, mipMapWidth, mipMapHeight, (float)dofParameters.resolution));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourcePyramid != null ? sourcePyramid : source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                BlueNoise.BindDitheredTextureSet(cmd, dofParameters.ditheredTextureSet);
                cmd.DispatchCompute(cs, kernel, (dofParameters.camera.actualWidth + 7) / 8, (dofParameters.camera.actualHeight + 7) / 8, dofParameters.camera.viewCount);
            }
        }

        #endregion
    }
}
