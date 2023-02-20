using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        private enum SMAAStage
        {
            EdgeDetection = 0,
            BlendWeights = 1,
            NeighborhoodBlending = 2
        }

        GraphicsFormat m_PostProcessColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        const GraphicsFormat k_CoCFormat = GraphicsFormat.R16_SFloat;
        internal const GraphicsFormat k_ExposureFormat = GraphicsFormat.R32G32_SFloat;

        Material m_FinalPassMaterial;
        Material m_ClearBlackMaterial;
        Material m_SMAAMaterial;
        Material m_TemporalAAMaterial;

        // Lens Flare Data-Driven
        Material m_LensFlareDataDrivenShader;
        ComputeShader m_LensFlareMergeOcclusionDataDrivenCS { get { return defaultResources.shaders.lensFlareMergeOcclusionCS; } }

        // Exposure data
        const int k_ExposureCurvePrecision = 128;
        const int k_HistogramBins = 128;   // Important! If this changes, need to change HistogramExposure.compute
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
        int m_LutSize;
        GraphicsFormat m_LutFormat;
        HableCurve m_HableCurve;

        //Viewport information
        Vector2Int m_AfterDynamicResUpscaleRes = new Vector2Int(1, 1);
        Vector2Int m_BeforeDynamicResUpscaleRes = new Vector2Int(1, 1);

        // TAA
        internal const float TAABaseBlendFactorMin = 0.6f;
        internal const float TAABaseBlendFactorMax = 0.95f;
        float[] taaSampleWeights = new float[9];

        private enum ResolutionGroup
        {
            BeforeDynamicResUpscale,
            AfterDynamicResUpscale
        }

        private ResolutionGroup resGroup { set; get; }

        private Vector2Int postProcessViewportSize { get { return resGroup == ResolutionGroup.AfterDynamicResUpscale ? m_AfterDynamicResUpscaleRes : m_BeforeDynamicResUpscaleRes; } }

        private struct PostProcessHistoryTextureAllocator
        {
            private String m_Name;
            private Vector2Int m_Size;
            private bool m_EnableMips;
            private bool m_UseDynamicScale;
            GraphicsFormat m_Format;

            public PostProcessHistoryTextureAllocator(String newName, Vector2Int newSize, GraphicsFormat format = GraphicsFormat.R16_SFloat, bool enableMips = false, bool useDynamicScale = false)
            {
                m_Name = newName;
                m_Size = newSize;
                m_EnableMips = enableMips;
                m_UseDynamicScale = useDynamicScale;
                m_Format = format;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    m_Size.x, m_Size.y, TextureXR.slices, DepthBits.None, m_Format,
                    dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: m_EnableMips, useDynamicScale: m_UseDynamicScale, name: $"{id} {m_Name} {frameIndex}"
                );
            }
        }

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

        // Prefetched frame settings (updated on every frame)
        bool m_StopNaNFS;
        bool m_DepthOfFieldFS;
        bool m_MotionBlurFS;
        bool m_PaniniProjectionFS;
        bool m_BloomFS;
        bool m_LensFlareDataDataDrivenFS;
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

        // Physical camera copy
        HDPhysicalCamera m_PhysicalCamera;

        // HDRP has the following behavior regarding alpha:
        // - If post processing is disabled, the alpha channel of the rendering passes (if any) will be passed to the frame buffer by the final pass
        // - If post processing is enabled, then post processing passes will either copy (exposure, color grading, etc) or process (DoF, TAA, etc) the alpha channel, if one exists.
        // If the user explicitly requests a color buffer without alpha for post-processing (for performance reasons) but the rendering passes have alpha, then the alpha will be copied.
        bool m_EnableAlpha;
        bool m_KeepAlpha;

        bool m_UseSafePath;
        bool m_PostProcessEnabled;
        bool m_AnimatedMaterialsEnabled;

        bool m_MotionBlurSupportsScattering;

        bool m_NonRenderGraphResourcesAvailable;

        // Max guard band size is assumed to be 8 pixels
        const int k_RTGuardBandSize = 4;

        System.Random m_Random;

        bool m_DLSSPassEnabled = false;
        Material m_DLSSBiasColorMaskMaterial;
        DLSSPass m_DLSSPass = null;
        void InitializePostProcess()
        {
            m_FinalPassMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.finalPassPS);
            m_ClearBlackMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.clearBlackPS);
            m_SMAAMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.SMAAPS);
            m_TemporalAAMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.temporalAntialiasingPS);
            m_DLSSBiasColorMaskMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.DLSSBiasColorMaskPS);

            // Lens Flare
            m_LensFlareDataDrivenShader = CoreUtils.CreateEngineMaterial(defaultResources.shaders.lensFlareDataDrivenPS);
            m_LensFlareDataDrivenShader.SetOverrideTag("RenderType", "Transparent");

            // Some compute shaders fail on specific hardware or vendors so we'll have to use a
            // safer but slower code path for them
            m_UseSafePath = SystemInfo.graphicsDeviceVendor
                .ToLowerInvariant().Contains("intel");

            // Project-wide LUT size for all grading operations - meaning that internal LUTs and
            // user-provided LUTs will have to be this size
            var postProcessSettings = asset.currentPlatformRenderPipelineSettings.postProcessSettings;
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

            m_PostProcessColorFormat = (GraphicsFormat)postProcessSettings.bufferFormat;
            m_KeepAlpha = false;

            // if both rendering and post-processing support an alpha channel, then post-processing will process (or copy) the alpha
            m_EnableAlpha = asset.currentPlatformRenderPipelineSettings.SupportsAlpha() && postProcessSettings.supportsAlpha;

            if (m_EnableAlpha == false)
            {
                // if only rendering has an alpha channel (and not post-processing), then we just copy the alpha to the output (but we don't process it).
                m_KeepAlpha = asset.currentPlatformRenderPipelineSettings.SupportsAlpha();
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

            resGroup = ResolutionGroup.BeforeDynamicResUpscale;

            m_DLSSPass = DLSSPass.Create(m_GlobalSettings);
        }

        GraphicsFormat GetPostprocessTextureFormat(HDCamera camera)
        {
            if (camera.CameraIsSceneFiltering())
            {
                return GraphicsFormat.R16G16B16A16_SFloat;
            }
            else
            {
                return m_PostProcessColorFormat;
            }
        }

        bool PostProcessEnableAlpha(HDCamera camera)
        {
            if (camera.CameraIsSceneFiltering())
            {
                return true;
            }

            return m_EnableAlpha;
        }

        void CleanupPostProcess()
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

            m_ExposureCurveTexture = null;
            m_InternalSpectralLut = null;
            m_FinalPassMaterial = null;
            m_ClearBlackMaterial = null;
            m_SMAAMaterial = null;
            m_TemporalAAMaterial = null;
            m_HistogramBuffer = null;
            m_DebugImageHistogramBuffer = null;
            m_DebugExposureData = null;
            m_DLSSPass = null;
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

        void BeginPostProcessFrame(CommandBuffer cmd, HDCamera camera, HDRenderPipeline hdInstance)
        {
            m_PostProcessEnabled = camera.frameSettings.IsEnabled(FrameSettingsField.Postprocess) && CoreUtils.ArePostProcessesEnabled(camera.camera);
            m_AnimatedMaterialsEnabled = camera.animateMaterials;
            m_AfterDynamicResUpscaleRes = new Vector2Int((int)Mathf.Round(camera.finalViewport.width), (int)Mathf.Round(camera.finalViewport.height));
            m_BeforeDynamicResUpscaleRes = new Vector2Int(camera.actualWidth, camera.actualHeight);

            // Grab a copy of the physical camera settings
            m_PhysicalCamera = camera.physicalParameters;

            // Prefetch all the volume components we need to save some cycles as most of these will
            // be needed in multiple places
            var stack = camera.volumeStack;
            m_Exposure = stack.GetComponent<Exposure>();
            m_DepthOfField = stack.GetComponent<DepthOfField>();
            m_MotionBlur = stack.GetComponent<MotionBlur>();
            m_PaniniProjection = stack.GetComponent<PaniniProjection>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_LensDistortion = stack.GetComponent<LensDistortion>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_Tonemapping = stack.GetComponent<Tonemapping>();
            m_WhiteBalance = stack.GetComponent<WhiteBalance>();
            m_ColorAdjustments = stack.GetComponent<ColorAdjustments>();
            m_ChannelMixer = stack.GetComponent<ChannelMixer>();
            m_SplitToning = stack.GetComponent<SplitToning>();
            m_LiftGammaGain = stack.GetComponent<LiftGammaGain>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_Curves = stack.GetComponent<ColorCurves>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();

            // Prefetch frame settings - these aren't free to pull so we want to do it only once
            // per frame
            var frameSettings = camera.frameSettings;
            m_StopNaNFS = frameSettings.IsEnabled(FrameSettingsField.StopNaN) && m_PostProcessEnabled;
            m_DepthOfFieldFS = frameSettings.IsEnabled(FrameSettingsField.DepthOfField) && m_PostProcessEnabled;
            m_MotionBlurFS = frameSettings.IsEnabled(FrameSettingsField.MotionBlur) && m_PostProcessEnabled;
            m_PaniniProjectionFS = frameSettings.IsEnabled(FrameSettingsField.PaniniProjection) && m_PostProcessEnabled;
            m_BloomFS = frameSettings.IsEnabled(FrameSettingsField.Bloom) && m_PostProcessEnabled;
            m_LensFlareDataDataDrivenFS = frameSettings.IsEnabled(FrameSettingsField.LensFlareDataDriven) && m_PostProcessEnabled;
            m_ChromaticAberrationFS = frameSettings.IsEnabled(FrameSettingsField.ChromaticAberration) && m_PostProcessEnabled;
            m_LensDistortionFS = frameSettings.IsEnabled(FrameSettingsField.LensDistortion) && m_PostProcessEnabled;
            m_VignetteFS = frameSettings.IsEnabled(FrameSettingsField.Vignette) && m_PostProcessEnabled;
            m_ColorGradingFS = frameSettings.IsEnabled(FrameSettingsField.ColorGrading) && m_PostProcessEnabled;
            m_TonemappingFS = frameSettings.IsEnabled(FrameSettingsField.Tonemapping) && m_PostProcessEnabled;
            m_FilmGrainFS = frameSettings.IsEnabled(FrameSettingsField.FilmGrain) && m_PostProcessEnabled;
            m_DitheringFS = frameSettings.IsEnabled(FrameSettingsField.Dithering) && m_PostProcessEnabled;
            m_AntialiasingFS = frameSettings.IsEnabled(FrameSettingsField.Antialiasing) || camera.IsTAAUEnabled();

            // Override full screen anti-aliasing when doing path tracing (which is naturally anti-aliased already)
            m_AntialiasingFS &= !camera.IsPathTracingEnabled();

            // Sanity check, cant run dlss unless the pass is not null
            m_DLSSPassEnabled = m_DLSSPass != null && camera.IsDLSSEnabled();

            m_DebugExposureCompensation = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugExposure;

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
                        DoFixedExposure(camera, cmd);
                    }
                }

                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(camera));
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, GetPreviousExposureTexture(camera));
            }

            if (m_DLSSPass != null)
                m_DLSSPass.BeginFrame(camera);
        }

        static void ValidateComputeBuffer(ref ComputeBuffer cb, int size, int stride, ComputeBufferType type = ComputeBufferType.Default)
        {
            if (cb == null || cb.count < size)
            {
                CoreUtils.SafeRelease(cb);
                cb = new ComputeBuffer(size, stride, type);
            }
        }

        bool IsDynamicResUpscaleTargetEnabled()
        {
            return resGroup == ResolutionGroup.BeforeDynamicResUpscale;
        }

        TextureHandle GetPostprocessOutputHandle(RenderGraph renderGraph, string name, bool dynamicResolution, GraphicsFormat colorFormat, bool useMipMap)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, dynamicResolution, true)
            {
                name = name,
                colorFormat = colorFormat,
                useMipMap = useMipMap,
                enableRandomWrite = true
            });
        }

        TextureHandle GetPostprocessOutputHandle(HDCamera camera, RenderGraph renderGraph, string name, bool useMipMap = false)
        {
            return GetPostprocessOutputHandle(renderGraph, name, IsDynamicResUpscaleTargetEnabled(), GetPostprocessTextureFormat(camera), useMipMap);
        }

        TextureHandle GetPostprocessOutputHandle(RenderGraph renderGraph, string name, GraphicsFormat colorFormat, bool useMipMap = false)
        {
            return GetPostprocessOutputHandle(renderGraph, name, IsDynamicResUpscaleTargetEnabled(), colorFormat, useMipMap);
        }

        TextureHandle GetPostprocessUpsampledOutputHandle(HDCamera camera, RenderGraph renderGraph, string name)
        {
            return GetPostprocessOutputHandle(renderGraph, name, false, GetPostprocessTextureFormat(camera), false);
        }

        bool GrabPostProcessHistoryTextures(
            HDCamera camera, HDCameraFrameHistoryType historyType, String name, GraphicsFormat format, out RTHandle previous, out RTHandle next, bool useMips = false)
        {
            bool validHistory = true;
            next = camera.GetCurrentFrameRT((int)historyType);
            if (next == null || (useMips == true && next.rt.mipmapCount == 1) || next.rt.width != camera.postProcessRTHistoryMaxReference.x || next.rt.height != camera.postProcessRTHistoryMaxReference.y)
            {
                validHistory = false;
                var viewportSize = new Vector2Int(camera.postProcessRTHistoryMaxReference.x, camera.postProcessRTHistoryMaxReference.y);
                var textureAllocator = new PostProcessHistoryTextureAllocator(name, viewportSize, format, useMips);

                if (next != null)
                    camera.ReleaseHistoryFrameRT((int)historyType);
                next = camera.AllocHistoryFrameRT((int)historyType, textureAllocator.Allocator, 2);
            }
            previous = camera.GetPreviousFrameRT((int)historyType);
            return validHistory;
        }

        TextureHandle RenderPostProcess(RenderGraph renderGraph,
            in PrepassOutput prepassOutput,
            TextureHandle inputColor,
            TextureHandle backBuffer,
            TextureHandle sunOcclusionTexture,
            CullingResults cullResults,
            HDCamera hdCamera,
            bool postProcessIsFinalPass)
        {
            TextureHandle afterPostProcessBuffer = RenderAfterPostProcessObjects(renderGraph, hdCamera, cullResults, prepassOutput);
            TextureHandle dest = postProcessIsFinalPass ? backBuffer : renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, false, true) { colorFormat = GetColorBufferFormat(), name = "Intermediate Postprocess buffer" });

            var motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors) ? prepassOutput.resolvedMotionVectorsBuffer : renderGraph.defaultResources.blackTextureXR;
            bool flipYInPostProcess = postProcessIsFinalPass && (hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView);

            renderGraph.BeginProfilingSampler(ProfilingSampler.Get(HDProfileId.PostProcessing));

            var postProcessHistorySizes = DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.AfterPost ? m_BeforeDynamicResUpscaleRes : m_AfterDynamicResUpscaleRes;
            hdCamera.SetPostProcessHistorySizeAndReference(postProcessHistorySizes.x, postProcessHistorySizes.y, m_AfterDynamicResUpscaleRes.x, m_AfterDynamicResUpscaleRes.y);

            var source = inputColor;
            var depthBuffer = prepassOutput.resolvedDepthBuffer;
            var depthBufferMipChain = prepassOutput.depthPyramidTexture;
            var normalBuffer = prepassOutput.resolvedNormalBuffer;
            var depthMinMaxAvgMSAA = hdCamera.msaaEnabled ? prepassOutput.depthValuesMSAA : TextureHandle.nullHandle;

            TextureHandle alphaTexture = DoCopyAlpha(renderGraph, hdCamera, source);

            // Save the post process screen size before any resolution group change
            var postProcessScreenSize = hdCamera.postProcessScreenSize;

            //The resGroup is always expected to be in BeforeDynamicResUpscale state at the beginning of post processing.
            //If this assert fails, it means that some effects prior might be using the wrong resolution.
            Assert.IsTrue(resGroup == ResolutionGroup.BeforeDynamicResUpscale, "Resolution group must always be reset before calling RenderPostProcess");

            // Note: whether a pass is really executed or not is generally inside the Do* functions.
            // with few exceptions.

            if (m_PostProcessEnabled || m_AntialiasingFS)
            {
                bool taaEnabled = m_AntialiasingFS && hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                LensFlareComputeOcclusionDataDrivenPass(renderGraph, hdCamera, depthBuffer, taaEnabled);
                if (taaEnabled)
                {
                    LensFlareMergeOcclusionDataDrivenPass(renderGraph, hdCamera, taaEnabled);
                }

                source = StopNaNsPass(renderGraph, hdCamera, source);

                source = DynamicExposurePass(renderGraph, hdCamera, source);

                if (DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.BeforePost)
                {
                    source = DoDLSSPasses(renderGraph, hdCamera, source, depthBuffer, motionVectors);
                }

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, m_GlobalSettings.beforeTAACustomPostProcesses, HDProfileId.CustomPostProcessBeforeTAA);

                // Temporal anti-aliasing goes first
                if (m_AntialiasingFS)
                {
                    if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
                    {
                        source = DoTemporalAntialiasing(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source, prepassOutput.stencilBuffer, postDoF: false, "TAA Destination");
                        if (hdCamera.IsTAAUEnabled())
                        {
                            SetCurrentResolutionGroup(renderGraph, hdCamera, ResolutionGroup.AfterDynamicResUpscale);
                        }
                        RestoreNonjitteredMatrices(renderGraph, hdCamera);
                    }
                    else if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        source = SMAAPass(renderGraph, hdCamera, depthBuffer, source);
                    }
                }

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, m_GlobalSettings.beforePostProcessCustomPostProcesses, HDProfileId.CustomPostProcessBeforePP);

                source = DepthOfFieldPass(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source, depthMinMaxAvgMSAA, prepassOutput.stencilBuffer);

                if (m_DepthOfField.IsActive() && m_SubFrameManager.isRecording && m_SubFrameManager.subFrameCount > 1 && !hdCamera.IsPathTracingEnabled())
                {
                    RenderAccumulation(m_RenderGraph, hdCamera, source, source, false);
                }

                // Motion blur after depth of field for aesthetic reasons (better to see motion
                // blurred bokeh rather than out of focus motion blur)
                source = MotionBlurPass(renderGraph, hdCamera, depthBuffer, motionVectors, source);

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, m_GlobalSettings.afterPostProcessBlursCustomPostProcesses, HDProfileId.CustomPostProcessAfterPPBlurs);

                // Panini projection is done as a fullscreen pass after all depth-based effects are
                // done and before bloom kicks in
                // This is one effect that would benefit from an overscan mode or supersampling in
                // HDRP to reduce the amount of resolution lost at the center of the screen
                source = PaniniProjectionPass(renderGraph, hdCamera, source);

                source = LensFlareDataDrivenPass(renderGraph, hdCamera, source, depthBufferMipChain, sunOcclusionTexture);

                TextureHandle bloomTexture = BloomPass(renderGraph, hdCamera, source);
                TextureHandle logLutOutput = ColorGradingPass(renderGraph, hdCamera);
                source = UberPass(renderGraph, hdCamera, logLutOutput, bloomTexture, source);
                PushFullScreenDebugTexture(renderGraph, source, hdCamera.postProcessRTScales, FullScreenDebugMode.ColorLog);

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, m_GlobalSettings.afterPostProcessCustomPostProcesses, HDProfileId.CustomPostProcessAfterPP);

                source = FXAAPass(renderGraph, hdCamera, source);

                hdCamera.didResetPostProcessingHistoryInLastFrame = hdCamera.resetPostProcessingHistory;

                hdCamera.resetPostProcessingHistory = false;
            }
            else
            {
                if (DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.BeforePost)
                {
                    source = DoDLSSPasses(renderGraph, hdCamera, source, depthBuffer, motionVectors);
                }
            }

            if (DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.AfterPost)
            {
                // AMD Fidelity FX passes
                source = ContrastAdaptiveSharpeningPass(renderGraph, hdCamera, source);
                source = EdgeAdaptiveSpatialUpsampling(renderGraph, hdCamera, source);
                source = RobustContrastAdaptiveSharpeningPass(renderGraph, hdCamera, source);
            }

            FinalPass(renderGraph, hdCamera, afterPostProcessBuffer, alphaTexture, dest, source, m_BlueNoise, flipYInPostProcess, postProcessIsFinalPass);

            bool currFrameIsTAAUpsampled = hdCamera.IsTAAUEnabled();
            bool cameraWasRunningTAA = hdCamera.previousFrameWasTAAUpsampled;
            hdCamera.previousFrameWasTAAUpsampled = currFrameIsTAAUpsampled;

            hdCamera.resetPostProcessingHistory = (cameraWasRunningTAA != currFrameIsTAAUpsampled);

            renderGraph.EndProfilingSampler(ProfilingSampler.Get(HDProfileId.PostProcessing));

            // Reset the post process size if needed, so any passes that read this data during Render Graph execute will have the expected data
            if (postProcessScreenSize != hdCamera.postProcessScreenSize)
                hdCamera.SetPostProcessScreenSize((int)postProcessScreenSize.x, (int)postProcessScreenSize.y);

            return dest;
        }

        class RestoreNonJitteredPassData
        {
            public ShaderVariablesGlobal globalCB;
            public HDCamera hdCamera;
        }

        void RestoreNonjitteredMatrices(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<RestoreNonJitteredPassData>("Restore Non-Jittered Camera Matrices", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.globalCB = m_ShaderVariablesGlobalCB;

                builder.SetRenderFunc((RestoreNonJitteredPassData data, RenderGraphContext ctx) =>
                {
                    // Note about AfterPostProcess and TAA:
                    // When TAA is enabled rendering is jittered and then resolved during the post processing pass.
                    // It means that any rendering done after post processing need to disable jittering. This is what we do with hdCamera.UpdateViewConstants(false);
                    // The issue is that the only available depth buffer is jittered so pixels would wobble around depth tested edges.
                    // In order to avoid that we decide that objects rendered after Post processes while TAA is active will not benefit from the depth buffer so we disable it.
                    data.hdCamera.UpdateAllViewConstants(false);
                    data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);

                    ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                });
            }
        }

        #region AfterPostProcess
        class AfterPostProcessPassData
        {
            public ShaderVariablesGlobal globalCB;
            public HDCamera hdCamera;
            public RendererListHandle opaqueAfterPostprocessRL;
            public RendererListHandle transparentAfterPostprocessRL;
        }

        TextureHandle RenderAfterPostProcessObjects(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults, in PrepassOutput prepassOutput)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                return renderGraph.defaultResources.blackTextureXR;

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            using (var builder = renderGraph.AddRenderPass<AfterPostProcessPassData>("After Post-Process Objects", out var passData, ProfilingSampler.Get(HDProfileId.AfterPostProcessingObjects)))
            {
                bool useDepthBuffer = !hdCamera.RequiresCameraJitter() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.ZTestAfterPostProcessTAA);

                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.hdCamera = hdCamera;
                passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque)));
                passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent)));

                var output = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = true, clearColor = Color.black, name = "OffScreen AfterPostProcess" }), 0);
                if (useDepthBuffer)
                    builder.UseDepthBuffer(prepassOutput.resolvedDepthBuffer, DepthAccess.ReadWrite);

                // If the pass is culled at runtime from the RendererList API, set the appropriate fall-back for the output
                // Here we need an opaque black texture as default (alpha = 1) due to the way the output of this pass is composed with the post-process output (see FinalPass.shader)
                output.SetFallBackResource(renderGraph.defaultResources.blackTextureXR);

                builder.SetRenderFunc(
                    (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                    {
                        // Disable camera jitter. See coment in RestoreNonjitteredMatrices
                        if (data.hdCamera.RequiresCameraJitter())
                        {
                            data.hdCamera.UpdateAllViewConstants(false);
                            data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        }

                        UpdateOffscreenRenderingConstants(ref data.globalCB, true, 1.0f);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                        DrawOpaqueRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.opaqueAfterPostprocessRL);
                        // Setup off-screen transparency here
                        DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.transparentAfterPostprocessRL);

                        // Reenable camera jitter for CustomPostProcessBeforeTAA injection point
                        if (data.hdCamera.RequiresCameraJitter())
                        {
                            data.hdCamera.UpdateAllViewConstants(true);
                            data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        }

                        UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1.0f);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    });

                return output;
            }
        }

        #endregion

        #region DLSS
        class DLSSColorMaskPassData
        {
            public Material colorMaskMaterial;
            public int destWidth;
            public int destHeight;
        }

        TextureHandle DoDLSSColorMaskPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputDepth)
        {
            TextureHandle output = TextureHandle.nullHandle;
            using (var builder = renderGraph.AddRenderPass<DLSSColorMaskPassData>("DLSS Color Mask", out var passData, ProfilingSampler.Get(HDProfileId.DeepLearningSuperSamplingColorMask)))
            {
                output = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                        clearBuffer = true,
                        clearColor = Color.black,
                        name = "DLSS Color Mask"
                    }), 0);
                builder.UseDepthBuffer(inputDepth, DepthAccess.Read);

                passData.colorMaskMaterial = m_DLSSBiasColorMaskMaterial;

                passData.destWidth = hdCamera.actualWidth;
                passData.destHeight = hdCamera.actualHeight;

                builder.SetRenderFunc(
                    (DLSSColorMaskPassData data, RenderGraphContext ctx) =>
                    {
                        Rect targetViewport = new Rect(0.0f, 0.0f, data.destWidth, data.destHeight);
                        data.colorMaskMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ExcludeFromTAA);
                        data.colorMaskMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ExcludeFromTAA);
                        ctx.cmd.SetViewport(targetViewport);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.colorMaskMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                    });
            }

            return output;
        }

        class DLSSData
        {
            public DLSSPass.Parameters parameters;
            public DLSSPass.CameraResourcesHandles resourceHandles;
        }

        TextureHandle DoDLSSPasses(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle source, TextureHandle depthBuffer, TextureHandle motionVectors)
        {
            if (!m_DLSSPassEnabled)
                return source;

            TextureHandle colorBiasMask = DoDLSSColorMaskPass(renderGraph, hdCamera, depthBuffer);
            source = DoDLSSPass(renderGraph, hdCamera, source, depthBuffer, motionVectors, colorBiasMask);
            SetCurrentResolutionGroup(renderGraph, hdCamera, ResolutionGroup.AfterDynamicResUpscale);
            return source;
        }

        TextureHandle DoDLSSPass(
            RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle source, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle biasColorMask)
        {
            using (var builder = renderGraph.AddRenderPass<DLSSData>("Deep Learning Super Sampling", out var passData, ProfilingSampler.Get(HDProfileId.DeepLearningSuperSampling)))
            {
                passData.parameters = new DLSSPass.Parameters();
                passData.parameters.hdCamera = hdCamera;
                passData.parameters.drsSettings = currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;

                var viewHandles = new DLSSPass.ViewResourceHandles();
                viewHandles.source = builder.ReadTexture(source);
                viewHandles.output = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "DLSS destination"));
                viewHandles.depth = builder.ReadTexture(depthBuffer);
                viewHandles.motionVectors = builder.ReadTexture(motionVectors);

                if (biasColorMask.IsValid())
                    viewHandles.biasColorMask = builder.ReadTexture(biasColorMask);
                else
                    viewHandles.biasColorMask = TextureHandle.nullHandle;

                passData.resourceHandles = DLSSPass.CreateCameraResources(hdCamera, renderGraph, builder, viewHandles);

                source = viewHandles.output;

                builder.SetRenderFunc(
                    (DLSSData data, RenderGraphContext ctx) =>
                    {
                        m_DLSSPass.Render(data.parameters, DLSSPass.GetCameraResources(data.resourceHandles), ctx.cmd);
                    });
            }
            return source;
        }

        #endregion

        #region Copy Alpha
        class AlphaCopyPassData
        {
            public ComputeShader copyAlphaCS;
            public int copyAlphaKernel;
            public HDCamera hdCamera;

            public TextureHandle source;
            public TextureHandle outputAlpha;
        }

        TextureHandle DoCopyAlpha(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            // Save the alpha and apply it back into the final pass if rendering in fp16 and post-processing in r11g11b10
            if (m_KeepAlpha)
            {
                using (var builder = renderGraph.AddRenderPass<AlphaCopyPassData>("Alpha Copy", out var passData, ProfilingSampler.Get(HDProfileId.AlphaCopy)))
                {
                    passData.hdCamera = hdCamera;
                    passData.copyAlphaCS = defaultResources.shaders.copyAlphaCS;
                    passData.copyAlphaKernel = passData.copyAlphaCS.FindKernel("KMain");
                    passData.source = builder.ReadTexture(source);
                    passData.outputAlpha = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { name = "Alpha Channel Copy", colorFormat = GraphicsFormat.R16_SFloat, enableRandomWrite = true }));

                    builder.SetRenderFunc(
                        (AlphaCopyPassData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.copyAlphaCS, data.copyAlphaKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.copyAlphaCS, data.copyAlphaKernel, HDShaderIDs._OutputTexture, data.outputAlpha);
                            ctx.cmd.DispatchCompute(data.copyAlphaCS, data.copyAlphaKernel, (data.hdCamera.actualWidth + 7) / 8, (data.hdCamera.actualHeight + 7) / 8, data.hdCamera.viewCount);
                        });

                    return passData.outputAlpha;
                }
            }

            return renderGraph.defaultResources.whiteTextureXR;
        }

        #endregion

        #region StopNaNs

        class StopNaNPassData
        {
            public ComputeShader nanKillerCS;
            public int nanKillerKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
        }

        TextureHandle StopNaNsPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            // Optional NaN killer before post-processing kicks in
            bool stopNaNs = hdCamera.stopNaNs && m_StopNaNFS;

#if UNITY_EDITOR
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            if (isSceneView)
                stopNaNs = HDAdditionalSceneViewSettings.sceneViewStopNaNs;
#endif
            if (stopNaNs)
            {
                using (var builder = renderGraph.AddRenderPass<StopNaNPassData>("Stop NaNs", out var passData, ProfilingSampler.Get(HDProfileId.StopNaNs)))
                {
                    passData.nanKillerCS = defaultResources.shaders.nanKillerCS;
                    passData.nanKillerKernel = passData.nanKillerCS.FindKernel("KMain");
                    passData.width = postProcessViewportSize.x;
                    passData.height = postProcessViewportSize.y;
                    passData.viewCount = hdCamera.viewCount;
                    passData.nanKillerCS.shaderKeywords = null;
                    if (PostProcessEnableAlpha(hdCamera))
                        passData.nanKillerCS.EnableKeyword("ENABLE_ALPHA");
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(hdCamera, renderGraph, "Stop NaNs Destination"));

                    builder.SetRenderFunc(
                        (StopNaNPassData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.nanKillerCS, data.nanKillerKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.nanKillerCS, data.nanKillerKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(data.nanKillerCS, data.nanKillerKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                        });

                    return passData.destination;
                }
            }

            return source;
        }

        #endregion

        #region Exposure
        internal static void SetExposureTextureToEmpty(RTHandle exposureTexture)
        {
            var tex = new Texture2D(1, 1, GraphicsFormat.R16G16_SFloat, TextureCreationFlags.None);
            tex.SetPixel(0, 0, new Color(1f, ColorUtils.ConvertExposureToEV100(1f), 0f, 0f));
            tex.Apply();
            Graphics.Blit(tex, exposureTexture);
            CoreUtils.Destroy(tex);
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

        internal RTHandle GetExposureTexture(HDCamera camera)
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
            return GetExposureTextureHandle(camera.currentExposureTextures.current);
        }

        internal RTHandle GetExposureTextureHandle(RTHandle rt)
        {
            return rt ?? m_EmptyExposureTexture;
        }

        RTHandle GetPreviousExposureTexture(HDCamera camera)
        {
            // If the history was reset in the previous frame, then the history buffers were actually rendered with a neutral EV100 exposure multiplier
            return (camera.didResetPostProcessingHistoryInLastFrame && !IsExposureFixed(camera)) ?
                m_EmptyExposureTexture : GetExposureTextureHandle(camera.currentExposureTextures.previous);
        }

        RTHandle GetExposureDebugData()
        {
            return m_DebugExposureData;
        }

        HableCurve GetCustomToneMapCurve()
        {
            return m_HableCurve;
        }

        int GetLutSize()
        {
            return m_LutSize;
        }

        ComputeBuffer GetHistogramBuffer()
        {
            return m_HistogramBuffer;
        }

        void ComputeProceduralMeteringParams(HDCamera camera, out Vector4 proceduralParams1, out Vector4 proceduralParams2)
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

        ComputeBuffer GetDebugImageHistogramBuffer()
        {
            return m_DebugImageHistogramBuffer;
        }

        void DoFixedExposure(HDCamera hdCamera, CommandBuffer cmd)
        {
            ComputeShader cs = defaultResources.shaders.exposureCS;
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

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, exposureParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, hdCamera.currentExposureTextures.current);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
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

        void PrepareExposurePassData(RenderGraph renderGraph, RenderGraphBuilder builder, HDCamera hdCamera, TextureHandle source, DynamicExposureData passData)
        {
            passData.exposureCS = defaultResources.shaders.exposureCS;
            passData.histogramExposureCS = defaultResources.shaders.histogramExposureCS;
            passData.histogramExposureCS.shaderKeywords = null;

            passData.camera = hdCamera;
            passData.viewportSize = postProcessViewportSize;

            // Setup variants
            var adaptationMode = m_Exposure.adaptationMode.value;

            if (!Application.isPlaying || hdCamera.resetPostProcessingHistory)
                adaptationMode = AdaptationMode.Fixed;

            passData.exposureVariants = m_ExposureVariants;
            passData.exposureVariants[0] = 1; // (int)exposureSettings.luminanceSource.value;
            passData.exposureVariants[1] = (int)m_Exposure.meteringMode.value;
            passData.exposureVariants[2] = (int)adaptationMode;
            passData.exposureVariants[3] = 0;

            bool useTextureMask = m_Exposure.meteringMode.value == MeteringMode.MaskWeighted && m_Exposure.weightTextureMask.value != null;
            passData.textureMeteringMask = useTextureMask ? m_Exposure.weightTextureMask.value : Texture2D.whiteTexture;

            ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMaskParams, out passData.proceduralMaskParams2);

            bool isHistogramBased = m_Exposure.mode.value == ExposureMode.AutomaticHistogram;
            bool needsCurve = (isHistogramBased && m_Exposure.histogramUseCurveRemapping.value) || m_Exposure.mode.value == ExposureMode.CurveMapping;

            passData.histogramUsesCurve = m_Exposure.histogramUseCurveRemapping.value;

            // When recording with accumulation, unity_DeltaTime is adjusted to account for the subframes.
            // To match the ganeview's exposure adaptation when recording, we adjust similarly the speed.
            float speedMultiplier = m_SubFrameManager.isRecording ? (float) m_SubFrameManager.subFrameCount : 1.0f;
            passData.adaptationParams = new Vector4(m_Exposure.adaptationSpeedLightToDark.value * speedMultiplier, m_Exposure.adaptationSpeedDarkToLight.value * speedMultiplier, 0.0f, 0.0f);

            passData.exposureMode = m_Exposure.mode.value;

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

            passData.exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, limitMin, limitMax, 0f);
            passData.exposureParams2 = new Vector4(curveMin, curveMax, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);

            passData.exposureCurve = m_ExposureCurveTexture;

            if (isHistogramBased)
            {
                ValidateComputeBuffer(ref m_HistogramBuffer, k_HistogramBins, sizeof(uint));
                m_HistogramBuffer.SetData(m_EmptyHistogram);    // Clear the histogram

                Vector2 histogramFraction = m_Exposure.histogramPercentages.value / 100.0f;
                float evRange = limitMax - limitMin;
                float histScale = 1.0f / Mathf.Max(1e-5f, evRange);
                float histBias = -limitMin * histScale;
                passData.histogramExposureParams = new Vector4(histScale, histBias, histogramFraction.x, histogramFraction.y);

                passData.histogramBuffer = m_HistogramBuffer;
                passData.histogramOutputDebugData = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView;
                if (passData.histogramOutputDebugData)
                {
                    passData.histogramExposureCS.EnableKeyword("OUTPUT_DEBUG_DATA");
                }

                passData.exposurePreparationKernel = passData.histogramExposureCS.FindKernel("KHistogramGen");
                passData.exposureReductionKernel = passData.histogramExposureCS.FindKernel("KHistogramReduce");
            }
            else
            {
                passData.exposurePreparationKernel = passData.exposureCS.FindKernel("KPrePass");
                passData.exposureReductionKernel = passData.exposureCS.FindKernel("KReduction");
            }

            GrabExposureRequiredTextures(hdCamera, out var prevExposure, out var nextExposure);

            passData.source = builder.ReadTexture(source);
            passData.prevExposure = builder.ReadTexture(renderGraph.ImportTexture(prevExposure));
            passData.nextExposure = builder.WriteTexture(renderGraph.ImportTexture(nextExposure));
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

        static void DoDynamicExposure(DynamicExposureData data, CommandBuffer cmd)
        {
            var cs = data.exposureCS;
            int kernel;

            kernel = data.exposurePreparationKernel;
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, data.source);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, data.exposureParams2);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, data.textureMeteringMask);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, data.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, data.proceduralMaskParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.tmpTarget1024);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = data.exposureReductionKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.tmpTarget1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.tmpTarget32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, data.exposureParams);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (data.exposureMode == ExposureMode.Automatic)
            {
                data.exposureVariants[3] = 1;
            }
            else if (data.exposureMode == ExposureMode.CurveMapping)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, data.exposureCurve);
                data.exposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, data.adaptationParams);
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.tmpTarget32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        static void DoHistogramBasedExposure(DynamicExposureData data, CommandBuffer cmd)
        {
            var cs = data.histogramExposureCS;
            int kernel;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, data.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, data.proceduralMaskParams2);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._HistogramExposureParams, data.histogramExposureParams);

            // Generate histogram.
            kernel = data.exposurePreparationKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, data.source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, data.textureMeteringMask);

            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, data.histogramBuffer);

            int threadGroupSizeX = 16;
            int threadGroupSizeY = 8;
            int dispatchSizeX = HDUtils.DivRoundUp(data.viewportSize.x / 2, threadGroupSizeX);
            int dispatchSizeY = HDUtils.DivRoundUp(data.viewportSize.y / 2, threadGroupSizeY);

            cmd.DispatchCompute(cs, kernel, dispatchSizeX, dispatchSizeY, 1);

            // Now read the histogram
            kernel = data.exposureReductionKernel;
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, data.exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, data.exposureParams2);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, data.adaptationParams);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, data.histogramBuffer);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.nextExposure);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, data.exposureCurve);
            data.exposureVariants[3] = 0;
            if (data.histogramUsesCurve)
            {
                data.exposureVariants[3] = 2;
            }
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);

            if (data.histogramOutputDebugData)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureDebugTexture, data.exposureDebugData);
            }

            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        class DynamicExposureData
        {
            public ComputeShader exposureCS;
            public ComputeShader histogramExposureCS;
            public int exposurePreparationKernel;
            public int exposureReductionKernel;

            public Texture textureMeteringMask;
            public Texture exposureCurve;

            public HDCamera camera;
            public Vector2Int viewportSize;

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

            public TextureHandle source;
            public TextureHandle prevExposure;
            public TextureHandle nextExposure;
            public TextureHandle exposureDebugData;
            public TextureHandle tmpTarget1024;
            public TextureHandle tmpTarget32;
        }

        class ApplyExposureData
        {
            public ComputeShader applyExposureCS;
            public int applyExposureKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle prevExposure;
        }

        TextureHandle DynamicExposurePass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            // Dynamic exposure - will be applied in the next frame
            // Not considered as a post-process so it's not affected by its enabled state

            TextureHandle exposureForImmediateApplication = TextureHandle.nullHandle;
            if (!IsExposureFixed(hdCamera) && hdCamera.exposureControlFS)
            {
                using (var builder = renderGraph.AddRenderPass<DynamicExposureData>("Dynamic Exposure", out var passData, ProfilingSampler.Get(HDProfileId.DynamicExposure)))
                {
                    PrepareExposurePassData(renderGraph, builder, hdCamera, source, passData);

                    if (m_Exposure.mode.value == ExposureMode.AutomaticHistogram)
                    {
                        passData.exposureDebugData = builder.WriteTexture(renderGraph.ImportTexture(m_DebugExposureData));
                        builder.SetRenderFunc(
                            (DynamicExposureData data, RenderGraphContext ctx) =>
                            {
                                DoHistogramBasedExposure(data, ctx.cmd);
                            });
                        exposureForImmediateApplication = passData.nextExposure;
                    }
                    else
                    {
                        passData.tmpTarget1024 = builder.CreateTransientTexture(new TextureDesc(1024, 1024, false, false)
                        { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 1024" });
                        passData.tmpTarget32 = builder.CreateTransientTexture(new TextureDesc(32, 32, false, false)
                        { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 32" });

                        builder.SetRenderFunc(
                            (DynamicExposureData data, RenderGraphContext ctx) =>
                            {
                                DoDynamicExposure(data, ctx.cmd);
                            });
                        exposureForImmediateApplication = passData.nextExposure;
                    }
                }

                if (hdCamera.resetPostProcessingHistory)
                {
                    using (var builder = renderGraph.AddRenderPass<ApplyExposureData>("Apply Exposure", out var passData, ProfilingSampler.Get(HDProfileId.ApplyExposure)))
                    {
                        passData.applyExposureCS = defaultResources.shaders.applyExposureCS;
                        passData.applyExposureCS.shaderKeywords = null;
                        passData.applyExposureKernel = passData.applyExposureCS.FindKernel("KMain");

                        if (PostProcessEnableAlpha(hdCamera))
                            passData.applyExposureCS.EnableKeyword("ENABLE_ALPHA");

                        passData.width = postProcessViewportSize.x;
                        passData.height = postProcessViewportSize.y;
                        passData.width = hdCamera.actualWidth;
                        passData.height = hdCamera.actualHeight;
                        passData.viewCount = hdCamera.viewCount;
                        passData.source = builder.ReadTexture(source);
                        passData.prevExposure = exposureForImmediateApplication;

                        TextureHandle dest = GetPostprocessOutputHandle(hdCamera, renderGraph, "Apply Exposure Destination");
                        passData.destination = builder.WriteTexture(dest);

                        builder.SetRenderFunc(
                            (ApplyExposureData data, RenderGraphContext ctx) =>
                            {
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._ExposureTexture, data.prevExposure);
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._InputTexture, data.source);
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._OutputTexture, data.destination);
                                ctx.cmd.DispatchCompute(data.applyExposureCS, data.applyExposureKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                            });

                        source = passData.destination;
                    }
                }
            }

            return source;
        }

        #endregion

        #region Custom Post Process
        void DoUserAfterOpaqueAndSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterOpaqueAndSky)))
            {
                TextureHandle source = colorBuffer;
                bool needBlitToColorBuffer = DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, motionVectors, m_GlobalSettings.beforeTransparentCustomPostProcesses);

                if (needBlitToColorBuffer)
                {
                    BlitCameraTexture(renderGraph, source, colorBuffer);
                }
            }
        }

        class CustomPostProcessData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVecTexture;
            public HDCamera hdCamera;
            public CustomPostProcessVolumeComponent customPostProcess;
            public Vector4 postProcessScales;
            public Vector2Int postProcessViewportSize;
        }

        bool DoCustomPostProcess(RenderGraph renderGraph, HDCamera hdCamera, ref TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, List<string> postProcessList)
        {
            bool customPostProcessExecuted = false;
            foreach (var typeString in postProcessList)
            {
                var customPostProcessComponentType = Type.GetType(typeString);
                if (customPostProcessComponentType == null)
                    continue;

                var stack = hdCamera.volumeStack;

                if (stack.GetComponent(customPostProcessComponentType) is CustomPostProcessVolumeComponent customPP)
                {
                    customPP.SetupIfNeeded();

                    if (customPP is IPostProcessComponent pp && pp.IsActive())
                    {
                        if (hdCamera.camera.cameraType != CameraType.SceneView || customPP.visibleInSceneView)
                        {
                            // By default custom post process volume name is empty, so we take the type name instead for debug markers.
                            string passName = String.IsNullOrEmpty(customPP.name) ? customPP.typeName : customPP.name;
                            using (var builder = renderGraph.AddRenderPass<CustomPostProcessData>(passName, out var passData))
                            {
                                // TODO RENDERGRAPH
                                // These buffer are always bound in custom post process for now.
                                // We don't have the information that they are being used or not.
                                // Until we can upgrade CustomPP to be full render graph, we'll always read and bind them globally.
                                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                                passData.motionVecTexture = builder.ReadTexture(motionVectors);

                                passData.source = builder.ReadTexture(source);
                                passData.destination = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, IsDynamicResUpscaleTargetEnabled(), true)
                                { colorFormat = GetPostprocessTextureFormat(hdCamera), enableRandomWrite = true, name = "CustomPostProcesDestination" }), 0);
                                passData.hdCamera = hdCamera;
                                passData.customPostProcess = customPP;
                                passData.postProcessScales = new Vector4(hdCamera.postProcessRTScales.x, hdCamera.postProcessRTScales.y, hdCamera.postProcessRTScalesHistory.z, hdCamera.postProcessRTScalesHistory.w);
                                passData.postProcessViewportSize = postProcessViewportSize;
                                builder.SetRenderFunc(
                                    (CustomPostProcessData data, RenderGraphContext ctx) =>
                                    {
                                        var srcRt = (RTHandle)data.source;
                                        var dstRt = (RTHandle)data.destination;

                                        // HACK FIX: for custom post process, we want the user to transparently be able to use color target regardless of the scaling occured. For example, if the user uses any of the HDUtil blit methods
                                        // which require the rtHandleProperties to set the viewport and sample scales.
                                        // In the case of DLSS and TAAU, the post process viewport and size for the color target have changed, thus we override them here.
                                        // When these upscalers arent set, behaviour is still the same (since the post process scale is the same as the global rt handle scale). So for simplicity, we always take this code path for custom post process color.
                                        var newProps = srcRt.rtHandleProperties;
                                        newProps.rtHandleScale = data.postProcessScales;
                                        newProps.currentRenderTargetSize = data.postProcessViewportSize;
                                        newProps.previousRenderTargetSize = data.postProcessViewportSize;
                                        newProps.currentViewportSize = data.postProcessViewportSize;
                                        srcRt.SetCustomHandleProperties(newProps);
                                        dstRt.SetCustomHandleProperties(newProps);

                                        // Temporary: see comment above
                                        ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthBuffer);
                                        ctx.cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                        ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);

                                        data.customPostProcess.Render(ctx.cmd, data.hdCamera, data.source, data.destination);

                                        srcRt.ClearCustomHandleProperties();
                                        dstRt.ClearCustomHandleProperties();
                                    });

                                customPostProcessExecuted = true;
                                source = passData.destination;
                            }
                        }
                    }
                }
            }

            return customPostProcessExecuted;
        }

        TextureHandle CustomPostProcessPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, List<string> postProcessList, HDProfileId profileId)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return source;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(profileId)))
            {
                DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, motionVectors, postProcessList);
            }

            return source;
        }

        #endregion

        #region Temporal Anti-aliasing

        bool GrabTemporalAntialiasingHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next, bool postDoF = false)
        {
            var historyType = postDoF ? HDCameraFrameHistoryType.TemporalAntialiasingPostDoF : HDCameraFrameHistoryType.TemporalAntialiasing;
            return GrabPostProcessHistoryTextures(camera, historyType, "TAA History", GetPostprocessTextureFormat(camera), out previous, out next);
        }

        bool GrabVelocityMagnitudeHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            return GrabPostProcessHistoryTextures(camera, HDCameraFrameHistoryType.TAAMotionVectorMagnitude, "Velocity magnitude", GraphicsFormat.R16_SFloat, out previous, out next);
        }

        void ReleasePostDoFTAAHistoryTextures(HDCamera camera)
        {
            var rt = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasingPostDoF);
            if (rt != null)
            {
                camera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasingPostDoF);
            }
        }

        class TemporalAntiAliasingData
        {
            public Material temporalAAMaterial;
            public bool resetPostProcessingHistory;
            public Vector4 previousScreenSize;
            public Vector4 taaParameters;
            public Vector4 taaParameters1;
            public float[] taaFilterWeights;
            public bool motionVectorRejection;
            public Vector4 taauParams;
            public Rect finalViewport;
            public Rect prevFinalViewport;
            public Vector4 taaScales;
            public bool runsTAAU;
            public bool runsAfterUpscale;
            public bool msaaIsEnabled;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle motionVecTexture;
            public TextureHandle depthBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle depthMipChain;
            public TextureHandle prevHistory;
            public TextureHandle nextHistory;
            public TextureHandle prevMVLen;
            public TextureHandle nextMVLen;
        }

        static readonly Vector2Int[] TAASampleOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1)
        };

        void PrepareTAAPassData(RenderGraph renderGraph, RenderGraphBuilder builder, TemporalAntiAliasingData passData, HDCamera camera,
            TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle sourceTexture, TextureHandle stencilTexture, bool postDoF, string outputName)
        {
            passData.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            float minAntiflicker = 0.0f;
            float maxAntiflicker = 3.5f;
            float motionRejectionMultiplier = Mathf.Lerp(0.0f, 250.0f, camera.taaMotionVectorRejection * camera.taaMotionVectorRejection * camera.taaMotionVectorRejection);

            // The anti flicker becomes much more aggressive on higher values
            float temporalContrastForMaxAntiFlicker = 0.7f - Mathf.Lerp(0.0f, 0.3f, Mathf.SmoothStep(0.5f, 1.0f, camera.taaAntiFlicker));

            bool TAAU = camera.IsTAAUEnabled();
            bool runsAfterUpscale = (resGroup == ResolutionGroup.AfterDynamicResUpscale);

            float antiFlickerLerpFactor = camera.taaAntiFlicker;
            float historySharpening = TAAU && postDoF ? 0.25f : camera.taaHistorySharpening;

            if (camera.camera.cameraType == CameraType.SceneView)
            {
                // Force settings for scene view.
                historySharpening = 0.25f;
                antiFlickerLerpFactor = 0.7f;
            }
            float antiFlicker = postDoF ? maxAntiflicker : Mathf.Lerp(minAntiflicker, maxAntiflicker, antiFlickerLerpFactor);
            const float historyContrastBlendStart =  0.51f;
            float historyContrastLerp =  Mathf.Clamp01((antiFlickerLerpFactor - historyContrastBlendStart) / (1.0f - historyContrastBlendStart));

            passData.taaParameters = new Vector4(historySharpening, antiFlicker, motionRejectionMultiplier, temporalContrastForMaxAntiFlicker);

            // Precompute weights used for the Blackman-Harris filter.
            float totalWeight = 0;
            for (int i = 0; i < 9; ++i)
            {
                Vector2 jitter = camera.taaJitter;
                float x = TAASampleOffsets[i].x + jitter.x;
                float y = TAASampleOffsets[i].y + jitter.y;
                float d = (x * x + y * y);

                taaSampleWeights[i] = Mathf.Exp((-0.5f / (0.22f)) * d);
                totalWeight += taaSampleWeights[i];
            }

            for (int i = 0; i < 9; ++i)
            {
                taaSampleWeights[i] /= totalWeight;
            }

            // For post dof we can be a bit more agressive with the taa base blend factor, since most aliasing has already been taken care of in the first TAA pass.
            // The following MAD operation expands the range to a new minimum (and keeps max the same).
            const float postDofMin = 0.4f;
            const float scale = (TAABaseBlendFactorMax - postDofMin) / (TAABaseBlendFactorMax - TAABaseBlendFactorMin);
            const float offset = postDofMin - TAABaseBlendFactorMin * scale;
            float taaBaseBlendFactor = postDoF ? camera.taaBaseBlendFactor * scale + offset : camera.taaBaseBlendFactor;

            passData.taaParameters1 = new Vector4(camera.camera.cameraType == CameraType.SceneView ? 0.2f : 1.0f - taaBaseBlendFactor, taaSampleWeights[0], (int)StencilUsage.ExcludeFromTAA, historyContrastLerp);

            passData.taaFilterWeights = taaSampleWeights;

            passData.temporalAAMaterial = m_TemporalAAMaterial;
            passData.temporalAAMaterial.shaderKeywords = null;

            if (PostProcessEnableAlpha(camera))
            {
                passData.temporalAAMaterial.EnableKeyword("ENABLE_ALPHA");
            }

            if (camera.taaHistorySharpening == 0)
            {
                passData.temporalAAMaterial.EnableKeyword("FORCE_BILINEAR_HISTORY");
            }

            if (camera.taaHistorySharpening != 0 && camera.taaAntiRinging && camera.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.High)
            {
                passData.temporalAAMaterial.EnableKeyword("ANTI_RINGING");
            }

            passData.motionVectorRejection = camera.taaMotionVectorRejection > 0;
            if (passData.motionVectorRejection)
            {
                passData.temporalAAMaterial.EnableKeyword("ENABLE_MV_REJECTION");
            }

            if (historyContrastLerp > 0.0f)
            {
                passData.temporalAAMaterial.EnableKeyword("HISTORY_CONTRAST_ANTI_FLICKER");
            }

            passData.runsTAAU = TAAU;
            passData.runsAfterUpscale = runsAfterUpscale;

            if (TAAU && !postDoF)
            {
                passData.temporalAAMaterial.EnableKeyword("TAA_UPSCALE");
            }
            else if (postDoF)
            {
                passData.temporalAAMaterial.EnableKeyword("POST_DOF");
            }
            else
            {
                switch (camera.TAAQuality)
                {
                    case HDAdditionalCameraData.TAAQualityLevel.Low:
                        passData.temporalAAMaterial.EnableKeyword("LOW_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.Medium:
                        passData.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.High:
                        passData.temporalAAMaterial.EnableKeyword("HIGH_QUALITY");
                        break;
                    default:
                        passData.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                }
            }

            if (TAAU || runsAfterUpscale)
            {
                passData.temporalAAMaterial.EnableKeyword("DIRECT_STENCIL_SAMPLE");
            }

            Vector2Int currentViewPort = new Vector2Int((int)passData.finalViewport.width, (int)passData.finalViewport.height);

            RTHandle prevHistory, nextHistory;
            bool validHistory = GrabTemporalAntialiasingHistoryTextures(camera, out prevHistory, out nextHistory, postDoF);

            Vector2Int prevViewPort = camera.historyRTHandleProperties.previousViewportSize;
            passData.previousScreenSize = new Vector4(prevViewPort.x, prevViewPort.y, 1.0f / prevViewPort.x, 1.0f / prevViewPort.y);
            if (TAAU || runsAfterUpscale)
                passData.previousScreenSize = new Vector4(camera.finalViewport.width, camera.finalViewport.height, 1.0f / camera.finalViewport.width, 1.0f / camera.finalViewport.height);

            passData.source = builder.ReadTexture(sourceTexture);
            passData.depthBuffer = builder.ReadTexture(depthBuffer);
            passData.motionVecTexture = builder.ReadTexture(motionVectors);
            passData.depthMipChain = builder.ReadTexture(depthBufferMipChain);
            passData.prevHistory = builder.ReadTexture(renderGraph.ImportTexture(prevHistory));
            passData.resetPostProcessingHistory = passData.resetPostProcessingHistory || !validHistory;
            if (passData.resetPostProcessingHistory)
            {
                passData.prevHistory = builder.WriteTexture(passData.prevHistory);
            }
            passData.nextHistory = builder.WriteTexture(renderGraph.ImportTexture(nextHistory));

            // Note: In case we run TAA for a second time (post-dof), we can use the same velocity history (and not write the output)
            RTHandle prevMVLen, nextMVLen;
            GrabVelocityMagnitudeHistoryTextures(camera, out prevMVLen, out nextMVLen);

            passData.prevMVLen = builder.ReadTexture(renderGraph.ImportTexture(prevMVLen));
            passData.nextMVLen = (!postDoF) ? builder.WriteTexture(renderGraph.ImportTexture(nextMVLen)) : TextureHandle.nullHandle;

            TextureHandle dest;
            if (TAAU && DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
            {
                dest = GetPostprocessUpsampledOutputHandle(camera, renderGraph, outputName);
            }
            else
            {
                dest = GetPostprocessOutputHandle(camera, renderGraph, outputName);
            }
            passData.destination = builder.WriteTexture(dest);

            bool needToUseCurrFrameSizeForHistory = camera.resetPostProcessingHistory || TAAU != camera.previousFrameWasTAAUpsampled;

            passData.prevFinalViewport = (camera.prevFinalViewport.width < 0 || needToUseCurrFrameSizeForHistory) ? camera.finalViewport : camera.prevFinalViewport;
            var mainRTScales = RTHandles.CalculateRatioAgainstMaxSize(camera.actualWidth, camera.actualHeight);

            var historyRenderingViewport = (TAAU || runsAfterUpscale) ? new Vector2(passData.prevFinalViewport.width, passData.prevFinalViewport.height) :
                (needToUseCurrFrameSizeForHistory ? RTHandles.rtHandleProperties.currentViewportSize : camera.historyRTHandleProperties.previousViewportSize);

            passData.finalViewport = (TAAU || runsAfterUpscale) ? camera.finalViewport : new Rect(0, 0, RTHandles.rtHandleProperties.currentViewportSize.x, RTHandles.rtHandleProperties.currentViewportSize.y);

            if (runsAfterUpscale)
            {
                // We are already upsampled here.
                mainRTScales = RTHandles.CalculateRatioAgainstMaxSize((int)camera.finalViewport.width, (int)camera.finalViewport.height);
            }
            Vector4 scales = new Vector4(historyRenderingViewport.x / prevHistory.rt.width, historyRenderingViewport.y / prevHistory.rt.height, mainRTScales.x, mainRTScales.y);
            passData.taaScales = scales;

            var resScale = DynamicResolutionHandler.instance.GetCurrentScale();
            float stdDev = 0.4f;
            passData.taauParams = new Vector4(1.0f / (stdDev * stdDev), 1.0f / resScale, 0.5f / resScale, resScale);

            passData.stencilBuffer =  builder.ReadTexture(stencilTexture);
            // With MSAA enabled we really don't support TAA (see docs), it should mostly work but stuff like stencil tests won't when manually sampled.
            // As a result we just set stencil to black. This flag can be used in the future to make proper support for the MSAA+TAA combo.
            passData.msaaIsEnabled = camera.msaaEnabled;
        }

        TextureHandle DoTemporalAntialiasing(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle sourceTexture, TextureHandle stencilBuffer, bool postDoF, string outputName)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalAntiAliasingData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
            {
                PrepareTAAPassData(renderGraph, builder, passData, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, sourceTexture, stencilBuffer, postDoF, outputName);

                builder.SetRenderFunc(
                    (TemporalAntiAliasingData data, RenderGraphContext ctx) =>
                    {
                        RTHandle source = data.source;
                        RTHandle nextMVLenTexture = data.nextMVLen;
                        RTHandle prevMVLenTexture = data.prevMVLen;
                        RTHandle prevHistory = (RTHandle)data.prevHistory;
                        RTHandle nextHistory = (RTHandle)data.nextHistory;

                        const int taaPass = 0;
                        const int excludeTaaPass = 1;
                        const int taauPass = 2;
                        const int copyHistoryPass = 3;

                        if (data.resetPostProcessingHistory)
                        {
                            var historyMpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                            historyMpb.SetTexture(HDShaderIDs._InputTexture, source);
                            historyMpb.SetVector(HDShaderIDs._TaaScales, data.taaScales);
                            if (data.runsTAAU || data.runsAfterUpscale)
                            {
                                Rect r = data.finalViewport;
                                HDUtils.DrawFullScreen(ctx.cmd, r, data.temporalAAMaterial, data.prevHistory, historyMpb, copyHistoryPass);
                                HDUtils.DrawFullScreen(ctx.cmd, r, data.temporalAAMaterial, data.nextHistory, historyMpb, copyHistoryPass);
                            }
                            else
                            {
                                HDUtils.DrawFullScreen(ctx.cmd, data.temporalAAMaterial, data.prevHistory, historyMpb, copyHistoryPass);
                                HDUtils.DrawFullScreen(ctx.cmd, data.temporalAAMaterial, data.nextHistory, historyMpb, copyHistoryPass);
                            }
                        }

                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ExcludeFromTAA);
                        mpb.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ExcludeFromTAA);
                        mpb.SetTexture(HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);
                        mpb.SetTexture(HDShaderIDs._InputTexture, source);
                        mpb.SetTexture(HDShaderIDs._InputHistoryTexture, data.prevHistory);
                        if (prevMVLenTexture != null && data.motionVectorRejection)
                        {
                            mpb.SetTexture(HDShaderIDs._InputVelocityMagnitudeHistory, prevMVLenTexture);
                        }

                        mpb.SetTexture(HDShaderIDs._DepthTexture, data.depthMipChain);

                        var taaHistorySize = data.previousScreenSize;

                        mpb.SetVector(HDShaderIDs._TaaPostParameters, data.taaParameters);
                        mpb.SetVector(HDShaderIDs._TaaPostParameters1, data.taaParameters1);
                        mpb.SetVector(HDShaderIDs._TaaHistorySize, taaHistorySize);
                        mpb.SetFloatArray(HDShaderIDs._TaaFilterWeights, data.taaFilterWeights);

                        mpb.SetVector(HDShaderIDs._TaauParameters, data.taauParams);
                        mpb.SetVector(HDShaderIDs._TaaScales, data.taaScales);

                        if (data.runsTAAU || data.runsAfterUpscale)
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.destination);
                        }
                        else
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.destination, data.depthBuffer);
                        }

                        ctx.cmd.SetRandomWriteTarget(1, data.nextHistory);
                        if (nextMVLenTexture != null && data.motionVectorRejection)
                        {
                            ctx.cmd.SetRandomWriteTarget(2, nextMVLenTexture);
                        }

                        Rect rect;
                        if (data.runsTAAU || data.runsAfterUpscale)
                        {
                            rect = data.finalViewport;

                            // If this is the case it means we are using MSAA. With MSAA TAA is not really supported, so we just bind a black stencil.
                            if (data.msaaIsEnabled)
                                mpb.SetTexture(HDShaderIDs._StencilTexture, ctx.defaultResources.blackTextureXR);
                            else
                                mpb.SetTexture(HDShaderIDs._StencilTexture, data.stencilBuffer, RenderTextureSubElement.Stencil);


                            HDUtils.DrawFullScreen(ctx.cmd, rect, data.temporalAAMaterial, data.destination, mpb, taauPass);
                        }
                        else
                        {
                            ctx.cmd.SetViewport(data.finalViewport);
                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.temporalAAMaterial, taaPass, MeshTopology.Triangles, 3, 1, mpb);
                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.temporalAAMaterial, excludeTaaPass, MeshTopology.Triangles, 3, 1, mpb);
                        }
                        ctx.cmd.ClearRandomWriteTargets();
                    });

                return passData.destination;
            }
        }

        #endregion

        #region SMAA
        class SMAAData
        {
            public Material smaaMaterial;
            public Texture smaaAreaTex;
            public Texture smaaSearchTex;
            public Vector4 smaaRTMetrics;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle smaaEdgeTex;
            public TextureHandle smaaBlendTex;
        }

        TextureHandle SMAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle source)
        {
            using (var builder = renderGraph.AddRenderPass<SMAAData>("Subpixel Morphological Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.SMAA)))
            {
                passData.smaaMaterial = m_SMAAMaterial;
                passData.smaaAreaTex = defaultResources.textures.SMAAAreaTex;
                passData.smaaSearchTex = defaultResources.textures.SMAASearchTex;
                passData.smaaMaterial.shaderKeywords = null;
                passData.smaaRTMetrics = new Vector4(1.0f / (float)postProcessViewportSize.x, 1.0f / (float)postProcessViewportSize.y, (float)postProcessViewportSize.x, (float)postProcessViewportSize.y);

                switch (hdCamera.SMAAQuality)
                {
                    case HDAdditionalCameraData.SMAAQualityLevel.Low:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_LOW");
                        break;
                    case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                        break;
                    case HDAdditionalCameraData.SMAAQualityLevel.High:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                        break;
                    default:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                        break;
                }

                passData.source = builder.ReadTexture(source);
                passData.depthBuffer = builder.ReadWriteTexture(depthBuffer);
                passData.smaaEdgeTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, clearBuffer = true, name = "SMAA Edge Texture" });
                passData.smaaBlendTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, clearBuffer = true, name = "SMAA Blend Texture" });

                TextureHandle dest = GetPostprocessOutputHandle(hdCamera, renderGraph, "SMAA Destination");
                passData.destination = builder.WriteTexture(dest); ;

                builder.SetRenderFunc(
                    (SMAAData data, RenderGraphContext ctx) =>
                    {
                        data.smaaMaterial.SetVector(HDShaderIDs._SMAARTMetrics, data.smaaRTMetrics);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAAAreaTex, data.smaaAreaTex);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAASearchTex, data.smaaSearchTex);
                        data.smaaMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SMAA);
                        data.smaaMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SMAA);

                        // -----------------------------------------------------------------------------
                        // EdgeDetection stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.source);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.smaaEdgeTex, data.depthBuffer, null, (int)SMAAStage.EdgeDetection);

                        // -----------------------------------------------------------------------------
                        // BlendWeights stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.smaaEdgeTex);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.smaaBlendTex, data.depthBuffer, null, (int)SMAAStage.BlendWeights);

                        // -----------------------------------------------------------------------------
                        // NeighborhoodBlending stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.source);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAABlendTex, data.smaaBlendTex);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.destination, null, (int)SMAAStage.NeighborhoodBlending);
                    });

                return passData.destination;
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
            public ComputeShader pbDoFCombineCS;
            public int pbDoFCombineKernel;
            public int minMaxCoCTileSize;

            public BlueNoise.DitheredTextureSet ditheredTextureSet;

            public HDCamera camera;
            public Vector2Int viewportSize;

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

            parameters.dofKernelCS = defaultResources.shaders.depthOfFieldKernelCS;
            parameters.dofKernelKernel = parameters.dofKernelCS.FindKernel("KParametricBlurKernel");
            parameters.dofCoCCS = defaultResources.shaders.depthOfFieldCoCCS;
            parameters.dofCoCReprojectCS = defaultResources.shaders.depthOfFieldCoCReprojectCS;
            parameters.dofCoCReprojectKernel = parameters.dofCoCReprojectCS.FindKernel("KMain");
            parameters.dofDilateCS = defaultResources.shaders.depthOfFieldDilateCS;
            parameters.dofDilateKernel = parameters.dofDilateCS.FindKernel("KMain");
            parameters.dofMipCS = defaultResources.shaders.depthOfFieldMipCS;
            if (!m_DepthOfField.physicallyBased)
            {
                parameters.dofMipColorKernel = parameters.dofMipCS.FindKernel(PostProcessEnableAlpha(camera) ? "KMainColorAlpha" : "KMainColor");
            }
            else
            {
                parameters.dofMipColorKernel = parameters.dofMipCS.FindKernel(PostProcessEnableAlpha(camera) ? "KMainColorCopyAlpha" : "KMainColorCopy");
            }
            parameters.dofMipCoCKernel = parameters.dofMipCS.FindKernel("KMainCoC");
            parameters.dofMipSafeCS = defaultResources.shaders.depthOfFieldMipSafeCS;
            parameters.dofPrefilterCS = defaultResources.shaders.depthOfFieldPrefilterCS;
            parameters.dofTileMaxCS = defaultResources.shaders.depthOfFieldTileMaxCS;
            parameters.dofTileMaxKernel = parameters.dofTileMaxCS.FindKernel("KMain");
            parameters.dofGatherCS = defaultResources.shaders.depthOfFieldGatherCS;
            parameters.dofGatherNearKernel = parameters.dofGatherCS.FindKernel("KMainNear");
            parameters.dofGatherFarKernel = parameters.dofGatherCS.FindKernel("KMainFar");
            parameters.dofCombineCS = defaultResources.shaders.depthOfFieldCombineCS;
            parameters.dofCombineKernel = parameters.dofCombineCS.FindKernel("KMain");
            parameters.dofPrecombineFarCS = defaultResources.shaders.depthOfFieldPreCombineFarCS;
            parameters.dofPrecombineFarKernel = parameters.dofPrecombineFarCS.FindKernel("KMainPreCombineFar");
            parameters.dofClearIndirectArgsCS = defaultResources.shaders.depthOfFieldClearIndirectArgsCS;
            parameters.dofClearIndirectArgsKernel = parameters.dofClearIndirectArgsCS.FindKernel("KClear");

            parameters.dofCircleOfConfusionCS = defaultResources.shaders.dofCircleOfConfusion;
            parameters.pbDoFCoCMinMaxCS = defaultResources.shaders.dofCoCMinMaxCS;
            parameters.pbDoFMinMaxKernel = parameters.pbDoFCoCMinMaxCS.FindKernel("KMainCoCMinMax");
            parameters.pbDoFDilateCS = defaultResources.shaders.dofMinMaxDilateCS;
            parameters.pbDoFDilateKernel = parameters.pbDoFDilateCS.FindKernel("KMain");
            parameters.pbDoFGatherCS = defaultResources.shaders.dofGatherCS;
            parameters.pbDoFGatherKernel = parameters.pbDoFGatherCS.FindKernel("KMain");
            parameters.pbDoFCombineCS = defaultResources.shaders.dofCombineCS;
            parameters.pbDoFCombineKernel = parameters.pbDoFGatherCS.FindKernel("KMain");
            parameters.minMaxCoCTileSize = 8;

            parameters.camera = camera;
            parameters.viewportSize = postProcessViewportSize;
            parameters.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            parameters.nearLayerActive = m_DepthOfField.IsNearLayerActive();
            parameters.farLayerActive = m_DepthOfField.IsFarLayerActive();
            parameters.highQualityFiltering = m_DepthOfField.highQualityFiltering;
            parameters.useTiles = !camera.xr.singlePassEnabled;

            parameters.resolution = m_DepthOfField.resolution;

            float scale = m_DepthOfField.physicallyBased ? 1f : 1f / (float)parameters.resolution;
            float resolutionScale = (postProcessViewportSize.y / 1080f) * (scale * 2f);

            int farSamples = Mathf.CeilToInt(m_DepthOfField.farSampleCount * resolutionScale);
            int nearSamples = Mathf.CeilToInt(m_DepthOfField.nearSampleCount * resolutionScale);
            // We want at least 3 samples for both far and near
            parameters.farSampleCount = Mathf.Max(3, farSamples);
            parameters.nearSampleCount = Mathf.Max(3, nearSamples);

            parameters.farMaxBlur = m_DepthOfField.farMaxBlur;
            parameters.nearMaxBlur = m_DepthOfField.nearMaxBlur;

            int targetWidth = Mathf.RoundToInt(postProcessViewportSize.x * scale);
            int targetHeight = Mathf.RoundToInt(postProcessViewportSize.y * scale);
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

            if (m_DepthOfField.focusDistanceMode.value == FocusDistanceMode.Volume)
                parameters.focusDistance = m_DepthOfField.focusDistance.value;
            else
                parameters.focusDistance = m_PhysicalCamera.focusDistance;

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
            parameters.dofCoCCS.shaderKeywords = null;
            parameters.dofCircleOfConfusionCS.shaderKeywords = null;

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
                parameters.pbDoFCombineCS.EnableKeyword("ENABLE_ALPHA");
            }

            if (parameters.resolution == DepthOfFieldResolution.Full)
            {
                parameters.dofPrefilterCS.EnableKeyword("FULL_RES");
                parameters.dofCombineCS.EnableKeyword("FULL_RES");
            }
            else if (parameters.highQualityFiltering)
            {
                parameters.dofPrefilterCS.EnableKeyword("HIGH_QUALITY");
                parameters.dofCombineCS.EnableKeyword("HIGH_QUALITY");
                parameters.dofGatherCS.EnableKeyword("LOW_RESOLUTION");
            }
            else
            {
                parameters.dofPrefilterCS.EnableKeyword("LOW_QUALITY");
                parameters.dofCombineCS.EnableKeyword("LOW_QUALITY");
                parameters.dofGatherCS.EnableKeyword("LOW_RESOLUTION");
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

            // Settings specific to the physically based option
            if (m_DepthOfField.physicallyBased)
            {
                parameters.dofCoCReprojectCS.EnableKeyword("ENABLE_MAX_BLENDING");
                parameters.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet256SPP();
                // Fix the resolution to half. This only affects the out-of-focus regions (and there is no visible benefit at computing those at higher res). Tiles with pixels near the focus plane always run at full res.
                parameters.resolution = DepthOfFieldResolution.Half;
            }

            if (camera.msaaEnabled)
            {
                // When MSAA is enabled, DoF should use the min depth of the MSAA samples to avoid 1-pixel ringing around in-focus objects [case 1347291]
                parameters.dofCoCCS.EnableKeyword("USE_MIN_DEPTH");
                parameters.dofCircleOfConfusionCS.EnableKeyword("USE_MIN_DEPTH");
            }

            parameters.useMipSafePath = m_UseSafePath;

            return parameters;
        }

        static void GetDoFResolutionScale(in DepthOfFieldParameters dofParameters, out float scale, out float resolutionScale)
        {
            scale = 1f / (float)dofParameters.resolution;
            resolutionScale = (dofParameters.viewportSize.y / 1080f) * 2f;
            // Note: The DoF sampling is performed in normalized space in the shader, so we don't need any scaling for half/quarter resoltion.
        }

        static float GetDoFResolutionMaxMip(in DepthOfFieldParameters dofParameters)
        {
            // For low sample counts & resolution scales, the DoF result looks very different from base resolutions (even if we scale the sample distance).
            // Thus, here we try to enforce a maximum mip to clamp to depending on the the resolution scale.
            switch (dofParameters.resolution)
            {
                case DepthOfFieldResolution.Full:
                    return 4.0f;
                case DepthOfFieldResolution.Half:
                    return 3.0f;
                default:
                    return 2.0f;
            }
        }

        static int GetDoFDilationPassCount(in DepthOfFieldParameters dofParameters, in float dofScale, in float nearMaxBlur)
        {
            return Mathf.CeilToInt((nearMaxBlur * dofScale + 2) / 4f);
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
            bool taaEnabled, RTHandle depthMinMaxAvgMSAA)
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
            int targetWidth = Mathf.RoundToInt(dofParameters.viewportSize.x * scale);
            int targetHeight = Mathf.RoundToInt(dofParameters.viewportSize.y * scale);

            cmd.SetGlobalVector(HDShaderIDs._TargetScale, new Vector4((float)dofParameters.resolution, scale, 0f, 0f));

            int farSamples = dofParameters.farSampleCount;
            int nearSamples = dofParameters.nearSampleCount;

            float farMaxBlur = dofParameters.farMaxBlur * resolutionScale;
            float nearMaxBlur = dofParameters.nearMaxBlur * resolutionScale;

            // If TAA is enabled we use the camera history system to grab CoC history textures, but
            // because these don't use the same RTHandleScale as the global one, we need to use
            // the RTHandleScale of the history RTHandles
            var cocHistoryScale = taaEnabled ? new Vector2(dofParameters.camera.postProcessRTScalesHistory.z, dofParameters.camera.postProcessRTScalesHistory.w) : dofParameters.camera.postProcessRTScales;

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
                if (dofParameters.camera.msaaEnabled)
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMinMaxAvg, depthMinMaxAvgMSAA);

                cmd.DispatchCompute(cs, kernel, (dofParameters.viewportSize.x + 7) / 8, (dofParameters.viewportSize.y + 7) / 8, dofParameters.camera.viewCount);

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
                            var size = new Vector2Int(Mathf.RoundToInt(dofParameters.viewportSize.x * mipScale), Mathf.RoundToInt(dofParameters.viewportSize.y * mipScale));
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

                    int passCount = GetDoFDilationPassCount(dofParameters, scale, nearMaxBlur);

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

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(farSamples, farMaxBlur * scale, barrelClipping, farMaxBlur));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(GetDoFResolutionMaxMip(dofParameters), 0, 0, 0));
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

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(nearSamples, nearMaxBlur * scale, barrelClipping, nearMaxBlur));
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
                cmd.DispatchCompute(cs, kernel, (dofParameters.viewportSize.x + 7) / 8, (dofParameters.viewportSize.y + 7) / 8, dofParameters.camera.viewCount);
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

        bool GrabCoCHistory(HDCamera camera, out RTHandle previous, out RTHandle next, bool useMips = false)
        {
            return GrabPostProcessHistoryTextures(camera, HDCameraFrameHistoryType.DepthOfFieldCoC, "CoC History", GraphicsFormat.R16_SFloat, out previous, out next, useMips);
        }

        static void ReprojectCoCHistory(in DepthOfFieldParameters parameters, CommandBuffer cmd, HDCamera camera, RTHandle prevCoC, RTHandle nextCoC, RTHandle motionVecTexture, ref RTHandle fullresCoC)
        {
            var cocHistoryScale = camera.postProcessRTScalesHistory;

            //Note: this reprojection creates some ghosting, we should replace it with something based on the new TAA
            ComputeShader cs = parameters.dofCoCReprojectCS;
            int kernel = parameters.dofCoCReprojectKernel;
            cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(parameters.resetPostProcessingHistory ? 0f : 0.91f, cocHistoryScale.z, cocHistoryScale.w, 0f));
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHistoryCoCTexture, prevCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, nextCoC);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraMotionVectorsTexture, motionVecTexture);
            cmd.DispatchCompute(cs, kernel, (parameters.viewportSize.x + 7) / 8, (parameters.viewportSize.y + 7) / 8, camera.viewCount);

            fullresCoC = nextCoC;
        }

        static void DoPhysicallyBasedDepthOfField(in DepthOfFieldParameters dofParameters, CommandBuffer cmd, RTHandle source, RTHandle destination, RTHandle fullresCoC, RTHandle prevCoCHistory, RTHandle nextCoCHistory, RTHandle motionVecTexture, RTHandle sourcePyramid, RTHandle depthBuffer, RTHandle minMaxCoCPing, RTHandle minMaxCoCPong, RTHandle scaledDof, bool taaEnabled, RTHandle depthMinMaxAvgMSAA)
        {
            // Currently Physically Based DoF is performed at "full" resolution (ie does not utilize DepthOfFieldResolution)
            // However, to produce similar results when switching between various resolutions, or dynamic resolution,
            // we must incorporate resolution independence, fitted with a 1920x1080 reference resolution.
            var scale = dofParameters.viewportSize / new Vector2(1920f, 1080f);
            float resolutionScale = Mathf.Min(scale.x, scale.y) * 2f;

            float farMaxBlur = resolutionScale * dofParameters.farMaxBlur;
            float nearMaxBlur = resolutionScale * dofParameters.nearMaxBlur;
            bool usePhysicalCamera = dofParameters.focusMode == DepthOfFieldMode.UsePhysicalCamera;

            // Map the old "max radius" parameters to a bigger range when driving the dof from physical camera settings, so we can work on more challenging scenes, [0, 16] --> [0, 64]
            float radiusMultiplier = usePhysicalCamera ? 4.0f : 1.0f;
            Vector2 cocLimit = new Vector2(
                Mathf.Max(radiusMultiplier * farMaxBlur, 0.01f),
                Mathf.Max(radiusMultiplier * nearMaxBlur, 0.01f));
            float maxCoc = Mathf.Max(cocLimit.x, cocLimit.y);

            ComputeShader cs;
            int kernel;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCoC)))
            {
                cs = dofParameters.dofCircleOfConfusionCS;
                kernel = dofParameters.dofCircleOfConfusionKernel;

                if (usePhysicalCamera)
                {
                    // The sensor scale is used to convert the CoC size from mm to screen pixels
                    float sensorScale;
                    bool upsampleBeforePost = dofParameters.camera.UpsampleHappensBeforePost();

                    if (dofParameters.camera.camera.gateFit == Camera.GateFitMode.Horizontal)
                        sensorScale = (0.5f / dofParameters.camera.camera.sensorSize.x) * (float)dofParameters.viewportSize.x;
                    else
                        sensorScale = (0.5f / dofParameters.camera.camera.sensorSize.y) * (float)dofParameters.viewportSize.y;

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
                if (dofParameters.camera.msaaEnabled)
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMinMaxAvg, depthMinMaxAvgMSAA);

                cmd.DispatchCompute(cs, kernel, (dofParameters.viewportSize.x + 7) / 8, (dofParameters.viewportSize.y + 7) / 8, dofParameters.camera.viewCount);

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

                    int tx = ((dofParameters.viewportSize.x >> 1) + 7) / 8;
                    int ty = ((dofParameters.viewportSize.y >> 1) + 7) / 8;
                    cmd.DispatchCompute(cs, kernel, tx, ty, dofParameters.camera.viewCount);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldDilate)))
            {
                int tileSize = dofParameters.minMaxCoCTileSize;
                int tx = ((dofParameters.viewportSize.x / tileSize) + 7) / 8;
                int ty = ((dofParameters.viewportSize.y / tileSize) + 7) / 8;

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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldGatherNear)))
            {
                cs = dofParameters.pbDoFGatherCS;
                kernel = dofParameters.pbDoFGatherKernel;
                float sampleCount = Mathf.Max(dofParameters.nearSampleCount, dofParameters.farSampleCount);
                float anamorphism = dofParameters.physicalCameraAnamorphism / 4f;

                float mipLevel = 1 + Mathf.Ceil(Mathf.Log(maxCoc, 2));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(sampleCount, maxCoc, anamorphism, 0.0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(mipLevel, 3, 1.0f / (float)dofParameters.resolution, (float)dofParameters.resolution));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourcePyramid != null ? sourcePyramid : source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, scaledDof);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileList, minMaxCoCPing, 0);
                BlueNoise.BindDitheredTextureSet(cmd, dofParameters.ditheredTextureSet);
                int scaledWidth = (dofParameters.viewportSize.x / (int)dofParameters.resolution + 7) / 8;
                int scaledHeight = (dofParameters.viewportSize.y / (int)dofParameters.resolution + 7) / 8;

                cmd.DispatchCompute(cs, kernel, scaledWidth, scaledHeight, dofParameters.camera.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCombine)))
            {
                cs = dofParameters.pbDoFCombineCS;
                kernel = dofParameters.pbDoFCombineKernel;
                float sampleCount = Mathf.Max(dofParameters.nearSampleCount, dofParameters.farSampleCount);
                float anamorphism = dofParameters.physicalCameraAnamorphism / 4f;

                float mipLevel = 1 + Mathf.Ceil(Mathf.Log(maxCoc, 2));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(sampleCount, maxCoc, anamorphism, 0.0f));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputNearTexture, scaledDof);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileList, minMaxCoCPing, 0);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);

                cmd.DispatchCompute(cs, kernel, (dofParameters.viewportSize.x + 7) / 8, (dofParameters.viewportSize.y + 7) / 8, dofParameters.camera.viewCount);
            }
        }

        class DepthofFieldData
        {
            public DepthOfFieldParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle motionVecTexture;
            public TextureHandle pingNearRGB;
            public TextureHandle pongNearRGB;
            public TextureHandle nearCoC;
            public TextureHandle nearAlpha;
            public TextureHandle dilatedNearCoC;
            public TextureHandle pingFarRGB;
            public TextureHandle pongFarRGB;
            public TextureHandle farCoC;
            public TextureHandle fullresCoC;
            public TextureHandle[] mips = new TextureHandle[4];
            public TextureHandle dilationPingPongRT;
            public TextureHandle prevCoC;
            public TextureHandle nextCoC;
            public TextureHandle depthMinMaxAvgMSAA;

            public ComputeBufferHandle bokehNearKernel;
            public ComputeBufferHandle bokehFarKernel;
            public ComputeBufferHandle bokehIndirectCmd;
            public ComputeBufferHandle nearBokehTileList;
            public ComputeBufferHandle farBokehTileList;

            public bool taaEnabled;
        }

        TextureHandle DepthOfFieldPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle source, TextureHandle depthMinMaxAvgMSAA, TextureHandle stencilTexture)
        {
            bool postDoFTAAEnabled = false;
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            bool stabilizeCoC = m_AntialiasingFS && hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            bool isOrtho = hdCamera.camera.orthographic;

            // If DLSS is enabled, we need to stabilize the CoC buffer (because the upsampled depth is jittered)
            if (m_DLSSPassEnabled)
                stabilizeCoC = true;

            // If Path tracing is enabled, then DoF is computed in the path tracer by sampling the lens aperure (when using the physical camera mode)
            bool isDoFPathTraced = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                hdCamera.IsPathTracingEnabled() &&
                hdCamera.camera.cameraType != CameraType.Preview &&
                m_DepthOfField.focusMode == DepthOfFieldMode.UsePhysicalCamera);

            // Depth of Field is done right after TAA as it's easier to just re-project the CoC
            // map rather than having to deal with all the implications of doing it before TAA
            if (m_DepthOfField.IsActive() && !isSceneView && m_DepthOfFieldFS && !isDoFPathTraced && !isOrtho)
            {
                // If we switch DoF modes and the old one was not using TAA, make sure we invalidate the history
                // Note: for Rendergraph the m_IsDoFHisotoryValid perhaps should be moved to the "pass data" struct
                if (stabilizeCoC && hdCamera.dofHistoryIsValid != m_DepthOfField.physicallyBased)
                {
                    hdCamera.resetPostProcessingHistory = true;
                }

                var dofParameters = PrepareDoFParameters(hdCamera);

                bool useHistoryMips = m_DepthOfField.physicallyBased;
                bool cocHistoryValid = GrabCoCHistory(hdCamera, out var prevCoC, out var nextCoC, useMips: useHistoryMips);
                var prevCoCHandle = renderGraph.ImportTexture(prevCoC);
                var nextCoCHandle = renderGraph.ImportTexture(nextCoC);

                using (var builder = renderGraph.AddRenderPass<DepthofFieldData>("Depth of Field", out var passData, ProfilingSampler.Get(HDProfileId.DepthOfField)))
                {
                    passData.source = builder.ReadTexture(source);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.parameters = dofParameters;
                    passData.prevCoC = builder.ReadTexture(prevCoCHandle);
                    passData.nextCoC = builder.ReadWriteTexture(nextCoCHandle);

                    if (hdCamera.msaaEnabled)
                        passData.depthMinMaxAvgMSAA = builder.ReadTexture(depthMinMaxAvgMSAA);

                    GetDoFResolutionScale(passData.parameters, out float scale, out float resolutionScale);
                    var screenScale = new Vector2(scale, scale);
                    passData.parameters.resetPostProcessingHistory = passData.parameters.resetPostProcessingHistory || !cocHistoryValid;
                    TextureHandle dest = GetPostprocessOutputHandle(hdCamera, renderGraph, "DoF Destination");
                    passData.destination = builder.WriteTexture(dest);
                    passData.motionVecTexture = builder.ReadTexture(motionVectors);
                    passData.taaEnabled = stabilizeCoC;

                    if (!m_DepthOfField.physicallyBased)
                    {
                        if (passData.parameters.nearLayerActive)
                        {
                            passData.pingNearRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = GetPostprocessTextureFormat(hdCamera), enableRandomWrite = true, name = "Ping Near RGB" });

                            passData.pongNearRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = GetPostprocessTextureFormat(hdCamera), enableRandomWrite = true, name = "Pong Near RGB" });

                            passData.nearCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Near CoC" });

                            passData.nearAlpha = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Near Alpha" });

                            passData.dilatedNearCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Dilated Near CoC" });
                        }
                        else
                        {
                            passData.pingNearRGB = TextureHandle.nullHandle;
                            passData.pongNearRGB = TextureHandle.nullHandle;
                            passData.nearCoC = TextureHandle.nullHandle;
                            passData.nearAlpha = TextureHandle.nullHandle;
                            passData.dilatedNearCoC = TextureHandle.nullHandle;
                        }

                        if (passData.parameters.farLayerActive)
                        {
                            passData.pingFarRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = GetPostprocessTextureFormat(hdCamera), useMipMap = true, enableRandomWrite = true, name = "Ping Far RGB" });

                            passData.pongFarRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = GetPostprocessTextureFormat(hdCamera), enableRandomWrite = true, name = "Pong Far RGB" });

                            passData.farCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = k_CoCFormat, useMipMap = true, enableRandomWrite = true, name = "Far CoC" });
                        }
                        else
                        {
                            passData.pingFarRGB = TextureHandle.nullHandle;
                            passData.pongFarRGB = TextureHandle.nullHandle;
                            passData.farCoC = TextureHandle.nullHandle;
                        }

                        passData.fullresCoC = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, IsDynamicResUpscaleTargetEnabled(), true)
                        { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Full res CoC" }));

                        var debugCocTexture = passData.fullresCoC;
                        var debugCocTextureScales = hdCamera.postProcessRTScales;
                        if (passData.taaEnabled)
                        {
                            debugCocTexture = passData.nextCoC;
                            debugCocTextureScales = hdCamera.postProcessRTScalesHistory;
                        }

                        float actualNearMaxBlur = passData.parameters.nearMaxBlur * resolutionScale;
                        int passCount = GetDoFDilationPassCount(dofParameters, scale, actualNearMaxBlur);

                        passData.dilationPingPongRT = TextureHandle.nullHandle;
                        if (passCount > 1)
                        {
                            passData.dilationPingPongRT = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Dilation ping pong CoC" });
                        }

                        var mipScale = scale;
                        for (int i = 0; i < 4; ++i)
                        {
                            mipScale *= 0.5f;
                            var size = new Vector2Int(Mathf.RoundToInt((float)postProcessViewportSize.x * mipScale), Mathf.RoundToInt((float)postProcessViewportSize.y * mipScale));

                            passData.mips[i] = builder.CreateTransientTexture(new TextureDesc(new Vector2(mipScale, mipScale), IsDynamicResUpscaleTargetEnabled(), true)
                            {
                                colorFormat = GetPostprocessTextureFormat(hdCamera),
                                enableRandomWrite = true,
                                name = "CoC Mip"
                            });
                        }

                        passData.bokehNearKernel = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.nearSampleCount * dofParameters.nearSampleCount, sizeof(uint)) { name = "Bokeh Near Kernel" });
                        passData.bokehFarKernel = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.farSampleCount * dofParameters.farSampleCount, sizeof(uint)) { name = "Bokeh Far Kernel" });
                        passData.bokehIndirectCmd = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(3 * 2, sizeof(uint), ComputeBufferType.IndirectArguments) { name = "Bokeh Indirect Cmd" });
                        passData.nearBokehTileList = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.threadGroup8.x * dofParameters.threadGroup8.y, sizeof(uint), ComputeBufferType.Append) { name = "Bokeh Near Tile List" });
                        passData.farBokehTileList = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.threadGroup8.x * dofParameters.threadGroup8.y, sizeof(uint), ComputeBufferType.Append) { name = "Bokeh Far Tile List" });

                        builder.SetRenderFunc(
                            (DepthofFieldData data, RenderGraphContext ctx) =>
                            {
                                var mipsHandles = ctx.renderGraphPool.GetTempArray<RTHandle>(4);

                                for (int i = 0; i < 4; ++i)
                                {
                                    mipsHandles[i] = data.mips[i];
                                }

                                ((ComputeBuffer)data.nearBokehTileList).SetCounterValue(0u);
                                ((ComputeBuffer)data.farBokehTileList).SetCounterValue(0u);

                                DoDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.depthBuffer, data.pingNearRGB, data.pongNearRGB, data.nearCoC, data.nearAlpha,
                                    data.dilatedNearCoC, data.pingFarRGB, data.pongFarRGB, data.farCoC, data.fullresCoC, mipsHandles, data.dilationPingPongRT, data.prevCoC, data.nextCoC, data.motionVecTexture,
                                    data.bokehNearKernel, data.bokehFarKernel, data.bokehIndirectCmd, data.nearBokehTileList, data.farBokehTileList, data.taaEnabled, data.depthMinMaxAvgMSAA);
                            });

                        source = passData.destination;

                        PushFullScreenDebugTexture(renderGraph, debugCocTexture, debugCocTextureScales, FullScreenDebugMode.DepthOfFieldCoc);
                    }
                    else
                    {
                        passData.fullresCoC = builder.ReadWriteTexture(GetPostprocessOutputHandle(renderGraph, "Full res CoC", k_CoCFormat, false));

                        var debugCocTexture = passData.fullresCoC;
                        var debugCocTextureScales = hdCamera.postProcessRTScales;
                        if (passData.taaEnabled)
                        {
                            debugCocTexture = passData.nextCoC;
                            debugCocTextureScales = hdCamera.postProcessRTScalesHistory;
                        }

                        passData.pongFarRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, IsDynamicResUpscaleTargetEnabled(), true)
                        { colorFormat = GetPostprocessTextureFormat(hdCamera), enableRandomWrite = true, name = "Scaled DoF" });

                        passData.pingFarRGB = builder.CreateTransientTexture(GetPostprocessOutputHandle(renderGraph, "DoF Source Pyramid", GetPostprocessTextureFormat(hdCamera), true));

                        // The size of the tile texture should be rounded-up, so we use a custom scale operator
                        // We cannot use the tile size in the scale call callback (to avoid gc alloc), so for now we use an assert
                        Assert.IsTrue(passData.parameters.minMaxCoCTileSize == 8);
                        ScaleFunc scaler = delegate (Vector2Int size) { return new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8); };

                        passData.pingNearRGB = builder.CreateTransientTexture(new TextureDesc(scaler, IsDynamicResUpscaleTargetEnabled(), true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, useMipMap = false, enableRandomWrite = true, name = "CoC Min Max Tiles" });

                        passData.pongNearRGB = builder.CreateTransientTexture(new TextureDesc(scaler, IsDynamicResUpscaleTargetEnabled(), true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, useMipMap = false, enableRandomWrite = true, name = "CoC Min Max Tiles" });

                        builder.SetRenderFunc(
                            (DepthofFieldData data, RenderGraphContext ctx) =>
                            {
                                DoPhysicallyBasedDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.fullresCoC, data.prevCoC, data.nextCoC, data.motionVecTexture, data.pingFarRGB, data.depthBuffer, data.pingNearRGB, data.pongNearRGB, data.pongFarRGB, data.taaEnabled, data.depthMinMaxAvgMSAA);
                            });

                        source = passData.destination;
                        PushFullScreenDebugTexture(renderGraph, debugCocTexture, debugCocTextureScales, FullScreenDebugMode.DepthOfFieldCoc);
                    }
                }

                // When physically based DoF is enabled, TAA runs two times, first to stabilize the color buffer before DoF and then after DoF to accumulate more aperture samples
                if (stabilizeCoC && m_DepthOfField.physicallyBased)
                {
                    source = DoTemporalAntialiasing(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source, stencilTexture, postDoF: true, "Post-DoF TAA Destination");
                    hdCamera.dofHistoryIsValid = true;
                    postDoFTAAEnabled = true;
                }
                else
                {
                    hdCamera.dofHistoryIsValid = false;
                }
            }

            if (!postDoFTAAEnabled)
            {
                ReleasePostDoFTAAHistoryTextures(hdCamera);
            }

            return source;
        }

        #endregion

        #region Lens Flare

        class LensFlareData
        {
            public LensFlareParameters parameters;
            public TextureHandle source;
            public TextureHandle depthBuffer;
            public TextureHandle occlusion;
            public HDCamera hdCamera;
            public Vector2Int viewport;
            public bool taaEnabled;
        }

        void LensFlareComputeOcclusionDataDrivenPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, bool taaEnabled)
        {
            if (m_LensFlareDataDataDrivenFS && !LensFlareCommonSRP.Instance.IsEmpty())
            {
                RTHandle occH = LensFlareCommonSRP.occlusionRT;
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);

                using (var builder = renderGraph.AddRenderPass<LensFlareData>("Lens Flare Compute Occlusion", out var passData, ProfilingSampler.Get(HDProfileId.LensFlareComputeOcclusionDataDriven)))
                {
                    passData.source = builder.WriteTexture(occlusionHandle);
                    passData.parameters = PrepareLensFlareParameters(hdCamera);
                    passData.viewport = new Vector2Int(LensFlareCommonSRP.maxLensFlareWithOcclusion, LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample);
                    passData.hdCamera = hdCamera;
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.taaEnabled = taaEnabled;

                    builder.SetRenderFunc(
                        (LensFlareData data, RenderGraphContext ctx) =>
                        {
                            float width = (float)data.viewport.x;
                            float height = (float)data.viewport.y;

                            LensFlareCommonSRP.ComputeOcclusion(
                                data.parameters.lensFlareShader, data.parameters.lensFlares, data.hdCamera.camera,
                                width, height,
                                data.parameters.usePanini, data.parameters.paniniDistance, data.parameters.paniniCropToFit, ShaderConfig.s_CameraRelativeRendering != 0,
                                data.hdCamera.mainViewConstants.worldSpaceCameraPos,
                                data.hdCamera.mainViewConstants.viewProjMatrix,
                                ctx.cmd, data.taaEnabled,
                                HDShaderIDs._FlareOcclusionTex, HDShaderIDs._FlareOcclusionIndex, HDShaderIDs._FlareTex, HDShaderIDs._FlareColorValue,
                                HDShaderIDs._FlareData0, HDShaderIDs._FlareData1, HDShaderIDs._FlareData2, HDShaderIDs._FlareData3, HDShaderIDs._FlareData4);
                        });
                }
            }
        }

        void LensFlareMergeOcclusionDataDrivenPass(RenderGraph renderGraph, HDCamera hdCamera, bool taaEnabled)
        {
            if (m_LensFlareDataDataDrivenFS && !LensFlareCommonSRP.Instance.IsEmpty())
            {
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);

                using (var builder = renderGraph.AddRenderPass<LensFlareData>("Lens Flare Merge Occlusion", out var passData, ProfilingSampler.Get(HDProfileId.LensFlareMergeOcclusionDataDriven)))
                {
                    passData.source = builder.WriteTexture(occlusionHandle);
                    passData.hdCamera = hdCamera;
                    passData.parameters = PrepareLensFlareParameters(hdCamera);
                    passData.viewport = new Vector2Int(LensFlareCommonSRP.maxLensFlareWithOcclusion, 1);

                    builder.SetRenderFunc(
                        (LensFlareData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.parameters.lensFlareMergeOcclusion, data.parameters.mergeOcclusionKernel, HDShaderIDs._LensFlareOcclusion, LensFlareCommonSRP.occlusionRT);
                            ctx.cmd.DispatchCompute(data.parameters.lensFlareMergeOcclusion, data.parameters.mergeOcclusionKernel,
                                HDUtils.DivRoundUp(LensFlareCommonSRP.maxLensFlareWithOcclusion, 8),
                                HDUtils.DivRoundUp(LensFlareCommonSRP.maxLensFlareWithOcclusionTemporalSample, 8),
                                data.hdCamera.viewCount);
                        });
                }
            }
        }

        TextureHandle LensFlareDataDrivenPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle depthBuffer, TextureHandle sunOcclusionTexture)
        {
            if (m_LensFlareDataDataDrivenFS && !LensFlareCommonSRP.Instance.IsEmpty())
            {
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);

                using (var builder = renderGraph.AddRenderPass<LensFlareData>("Lens Flare", out var passData, ProfilingSampler.Get(HDProfileId.LensFlareDataDriven)))
                {
                    passData.source = builder.WriteTexture(source);
                    passData.parameters = PrepareLensFlareParameters(hdCamera);
                    passData.viewport = postProcessViewportSize;
                    passData.hdCamera = hdCamera;
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.occlusion = builder.ReadTexture(occlusionHandle);

                    TextureHandle dest = GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "Lens Flare Destination");

                    builder.SetRenderFunc(
                        (LensFlareData data, RenderGraphContext ctx) =>
                        {
                            float width = (float)data.viewport.x;
                            float height = (float)data.viewport.y;

                            ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthBuffer);

                            LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                                data.parameters.lensFlareShader, data.parameters.lensFlares, data.hdCamera.camera, width, height,
                                data.parameters.usePanini, data.parameters.paniniDistance, data.parameters.paniniCropToFit,
                                ShaderConfig.s_CameraRelativeRendering != 0,
                                data.hdCamera.mainViewConstants.worldSpaceCameraPos,
                                data.hdCamera.mainViewConstants.nonJitteredViewProjMatrix,
                                ctx.cmd, data.source,
                                // If you pass directly 'GetLensFlareLightAttenuation' that create alloc apparently to cast to System.Func
                                // And here the lambda setup like that seem to not alloc anything.
                                (a, b, c) => { return GetLensFlareLightAttenuation(a, b, c); },
                                HDShaderIDs._FlareOcclusionTex, HDShaderIDs._FlareOcclusionIndex, HDShaderIDs._FlareTex, HDShaderIDs._FlareColorValue,
                                HDShaderIDs._FlareData0, HDShaderIDs._FlareData1, HDShaderIDs._FlareData2, HDShaderIDs._FlareData3, HDShaderIDs._FlareData4,
                                data.parameters.skipCopy);
                        });

                    PushFullScreenDebugTexture(renderGraph, source, hdCamera.postProcessRTScales, FullScreenDebugMode.LensFlareDataDriven);
                }
            }

            return source;
        }

        struct LensFlareParameters
        {
            public Material lensFlareShader;
            public ComputeShader lensFlareMergeOcclusion;
            public int mergeOcclusionKernel;
            public LensFlareCommonSRP lensFlares;
            public float paniniDistance;
            public float paniniCropToFit;
            public bool skipCopy;
            public bool usePanini;
        }

        LensFlareParameters PrepareLensFlareParameters(HDCamera camera)
        {
            LensFlareParameters parameters;

            parameters.lensFlares = LensFlareCommonSRP.Instance;
            parameters.lensFlareShader = m_LensFlareDataDrivenShader;
            parameters.lensFlareMergeOcclusion = m_LensFlareMergeOcclusionDataDrivenCS;
            parameters.mergeOcclusionKernel = m_LensFlareMergeOcclusionDataDrivenCS.FindKernel("MainCS");
            parameters.skipCopy = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.LensFlareDataDriven;

            PaniniProjection panini = camera.volumeStack.GetComponent<PaniniProjection>();

            if (panini)
            {
                parameters.usePanini = panini.IsActive();
                parameters.paniniDistance = panini.distance.value;
                parameters.paniniCropToFit = panini.cropToFit.value;
            }
            else
            {
                parameters.usePanini = false;
                parameters.paniniDistance = 0.0f;
                parameters.paniniCropToFit = 1.0f;
            }

            return parameters;
        }

        static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        {
            // Must always be true
            if (light.TryGetComponent<HDAdditionalLightData>(out var hdLightData))
            {
                switch (hdLightData.type)
                {
                    case HDLightType.Directional:
                        return LensFlareCommonSRP.ShapeAttenuationDirLight(hdLightData.transform.forward, wo);
                    case HDLightType.Point:
                        // Do nothing point are omnidirectional for the Lens Flare
                        return LensFlareCommonSRP.ShapeAttenuationPointLight();
                    case HDLightType.Spot:
                        switch (hdLightData.spotLightShape)
                        {
                            case SpotLightShape.Cone:
                                return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(hdLightData.transform.forward, wo, light.spotAngle, hdLightData.innerSpotPercent01);
                            case SpotLightShape.Box:
                                return LensFlareCommonSRP.ShapeAttenuationSpotBoxLight(hdLightData.transform.forward, wo);
                            case SpotLightShape.Pyramid:
                                return LensFlareCommonSRP.ShapeAttenuationSpotPyramidLight(hdLightData.transform.forward, wo);
                            default: throw new Exception($"GetLensFlareLightAttenuation Unknown SpotLightShape: {typeof(SpotLightShape)}: {hdLightData.type}");
                        }
                    case HDLightType.Area:
                        switch (hdLightData.areaLightShape)
                        {
                            case AreaLightShape.Tube:
                                return LensFlareCommonSRP.ShapeAttenuationAreaTubeLight(hdLightData.transform.position, hdLightData.transform.right, hdLightData.shapeWidth, cam);
                            case AreaLightShape.Rectangle:
                                return LensFlareCommonSRP.ShapeAttenuationAreaRectangleLight(hdLightData.transform.forward, wo);
                            case AreaLightShape.Disc:
                                return LensFlareCommonSRP.ShapeAttenuationAreaDiscLight(hdLightData.transform.forward, wo);
                            default: throw new Exception($"GetLensFlareLightAttenuation Unknown AreaLightShape {typeof(AreaLightShape)}: {hdLightData.type}");
                        }
                    default: throw new Exception($"GetLensFlareLightAttenuation HDLightType Unknown {typeof(HDLightType)}: {hdLightData.type}");
                }
            }

            return 1.0f;
        }

        #endregion

        #region Motion Blur

        class MotionBlurData
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
            public Vector2Int viewportSize;

            public Vector4 tileTargetSize;
            public Vector4 motionBlurParams0;
            public Vector4 motionBlurParams1;
            public Vector4 motionBlurParams2;
            public Vector4 motionBlurParams3;

            public bool motionblurSupportScattering;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle motionVecTexture;
            public TextureHandle preppedMotionVec;
            public TextureHandle minMaxTileVel;
            public TextureHandle maxTileNeigbourhood;
            public TextureHandle tileToScatterMax;
            public TextureHandle tileToScatterMin;
        }

        void PrepareMotionBlurPassData(RenderGraph renderGraph, in RenderGraphBuilder builder, MotionBlurData data, HDCamera hdCamera, TextureHandle source, TextureHandle motionVectors, TextureHandle depthTexture)
        {
            data.camera = hdCamera;
            data.viewportSize = postProcessViewportSize;

            int tileSize = 32;

            if (m_MotionBlurSupportsScattering)
            {
                tileSize = 16;
            }

            int tileTexWidth = Mathf.CeilToInt(postProcessViewportSize.x / (float)tileSize);
            int tileTexHeight = Mathf.CeilToInt(postProcessViewportSize.y / (float)tileSize);
            data.tileTargetSize = new Vector4(tileTexWidth, tileTexHeight, 1.0f / tileTexWidth, 1.0f / tileTexHeight);

            float screenMagnitude = (new Vector2(postProcessViewportSize.x, postProcessViewportSize.y).magnitude);
            data.motionBlurParams0 = new Vector4(
                screenMagnitude,
                screenMagnitude * screenMagnitude,
                m_MotionBlur.minimumVelocity.value,
                m_MotionBlur.minimumVelocity.value * m_MotionBlur.minimumVelocity.value
            );

            data.motionBlurParams1 = new Vector4(
                m_MotionBlur.intensity.value,
                m_MotionBlur.maximumVelocity.value / screenMagnitude,
                0.25f, // min/max velocity ratio for high quality.
                m_MotionBlur.cameraRotationVelocityClamp.value
            );

            uint sampleCount = (uint)m_MotionBlur.sampleCount;
            data.motionBlurParams2 = new Vector4(
                m_MotionBlurSupportsScattering ? (sampleCount + (sampleCount & 1)) : sampleCount,
                tileSize,
                m_MotionBlur.depthComparisonExtent.value,
                m_MotionBlur.cameraMotionBlur.value ? 0.0f : 1.0f
            );

            data.motionVecPrepCS = defaultResources.shaders.motionBlurMotionVecPrepCS;
            data.motionVecPrepKernel = data.motionVecPrepCS.FindKernel("MotionVecPreppingCS");
            data.motionVecPrepCS.shaderKeywords = null;

            if (!m_MotionBlur.cameraMotionBlur.value)
            {
                data.motionVecPrepCS.EnableKeyword("CAMERA_DISABLE_CAMERA");
            }
            else
            {
                var clampMode = m_MotionBlur.specialCameraClampMode.value;
                if (clampMode == CameraClampMode.None)
                    data.motionVecPrepCS.EnableKeyword("NO_SPECIAL_CLAMP");
                else if (clampMode == CameraClampMode.Rotation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_ROT_CLAMP");
                else if (clampMode == CameraClampMode.Translation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_TRANS_CLAMP");
                else if (clampMode == CameraClampMode.SeparateTranslationAndRotation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_SEPARATE_CLAMP");
                else if (clampMode == CameraClampMode.FullCameraMotionVector)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_FULL_CLAMP");
            }

            data.motionBlurParams3 = new Vector4(
                m_MotionBlur.cameraTranslationVelocityClamp.value,
                m_MotionBlur.cameraVelocityClamp.value,
                0, 0);


            data.tileGenCS = defaultResources.shaders.motionBlurGenTileCS;
            data.tileGenCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                data.tileGenCS.EnableKeyword("SCATTERING");
            }
            data.tileGenKernel = data.tileGenCS.FindKernel("TileGenPass");

            data.tileNeighbourhoodCS = defaultResources.shaders.motionBlurNeighborhoodTileCS;
            data.tileNeighbourhoodCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                data.tileNeighbourhoodCS.EnableKeyword("SCATTERING");
            }
            data.tileNeighbourhoodKernel = data.tileNeighbourhoodCS.FindKernel("TileNeighbourhood");

            data.tileMergeCS = defaultResources.shaders.motionBlurMergeTileCS;
            data.tileMergeKernel = data.tileMergeCS.FindKernel("TileMerge");

            data.motionBlurCS = defaultResources.shaders.motionBlurCS;
            data.motionBlurCS.shaderKeywords = null;
            CoreUtils.SetKeyword(data.motionBlurCS, "ENABLE_ALPHA", PostProcessEnableAlpha(hdCamera));
            data.motionBlurKernel = data.motionBlurCS.FindKernel("MotionBlurCS");

            data.motionblurSupportScattering = m_MotionBlurSupportsScattering;

            data.source = builder.ReadTexture(source);
            data.motionVecTexture = builder.ReadTexture(motionVectors);
            data.depthBuffer = builder.ReadTexture(depthTexture);

            Vector2 tileTexScale = new Vector2((float)data.tileTargetSize.x / (float)postProcessViewportSize.x, (float)data.tileTargetSize.y / (float)postProcessViewportSize.y);

            data.preppedMotionVec = builder.CreateTransientTexture(new TextureDesc(Vector2.one, IsDynamicResUpscaleTargetEnabled(), true)
            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Prepped Motion Vectors" });

            data.minMaxTileVel = builder.CreateTransientTexture(new TextureDesc(tileTexScale, IsDynamicResUpscaleTargetEnabled(), true)
            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "MinMax Tile Motion Vectors" });

            data.maxTileNeigbourhood = builder.CreateTransientTexture(new TextureDesc(tileTexScale, IsDynamicResUpscaleTargetEnabled(), true)
            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Max Neighborhood Tile" });

            data.tileToScatterMax = TextureHandle.nullHandle;
            data.tileToScatterMin = TextureHandle.nullHandle;

            if (data.motionblurSupportScattering)
            {
                data.tileToScatterMax = builder.CreateTransientTexture(new TextureDesc(tileTexScale, IsDynamicResUpscaleTargetEnabled(), true)
                { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "Tile to Scatter Max" });

                data.tileToScatterMin = builder.CreateTransientTexture(new TextureDesc(tileTexScale, IsDynamicResUpscaleTargetEnabled(), true)
                { colorFormat = GraphicsFormat.R16_SFloat, enableRandomWrite = true, name = "Tile to Scatter Min" });
            }

            data.destination = builder.WriteTexture(GetPostprocessOutputHandle(hdCamera, renderGraph, "Motion Blur Destination"));
        }

        static void DoMotionBlur(MotionBlurData data, CommandBuffer cmd)
        {
            int tileSize = 32;

            if (data.motionblurSupportScattering)
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
                cs = data.motionVecPrepCS;
                kernel = data.motionVecPrepKernel;
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, data.depthBuffer);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams3, data.motionBlurParams3);

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);

                cmd.SetComputeMatrixParam(cs, HDShaderIDs._PrevVPMatrixNoTranslation, data.camera.mainViewConstants.prevViewProjMatrixNoCameraTrans);
                cmd.SetComputeMatrixParam(cs, HDShaderIDs._CurrVPMatrixNoTranslation, data.camera.mainViewConstants.viewProjectionNoCameraTrans);

                threadGroupX = (data.viewportSize.x + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (data.viewportSize.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }


            // -----------------------------------------------------------------------------
            // Generate MinMax motion vectors tiles

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurTileMinMax)))
            {
                // We store R11G11B10 with RG = Max vel and B = Min vel magnitude
                cs = data.tileGenCS;
                kernel = data.tileGenKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, data.minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);


                if (data.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                }

                threadGroupX = (data.viewportSize.x + (tileSize - 1)) / tileSize;
                threadGroupY = (data.viewportSize.y + (tileSize - 1)) / tileSize;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Generate max tiles neigbhourhood

            using (new ProfilingScope(cmd, data.motionblurSupportScattering ? ProfilingSampler.Get(HDProfileId.MotionBlurTileScattering) : ProfilingSampler.Get(HDProfileId.MotionBlurTileNeighbourhood)))
            {
                cs = data.tileNeighbourhoodCS;
                kernel = data.tileNeighbourhoodKernel;


                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, data.minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                if (data.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                }
                groupSizeX = 8;
                groupSizeY = 8;
                threadGroupX = ((int)data.tileTargetSize.x + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = ((int)data.tileTargetSize.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Merge min/max info spreaded above.

            if (data.motionblurSupportScattering)
            {
                cs = data.tileMergeCS;
                kernel = data.tileMergeKernel;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Blur kernel
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurKernel)))
            {
                cs = data.motionBlurCS;
                kernel = data.motionBlurKernel;

                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.destination);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.source);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                groupSizeX = 16;
                groupSizeY = 16;
                threadGroupX = (data.viewportSize.x + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (data.viewportSize.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }
        }

        TextureHandle MotionBlurPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle motionVectors, TextureHandle source)
        {
            if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !hdCamera.resetPostProcessingHistory && m_MotionBlurFS)
            {
                // If we are in XR we need to check if motion blur is allowed at all.
                if (hdCamera.xr.enabled)
                {
                    if (!m_Asset.currentPlatformRenderPipelineSettings.xrSettings.allowMotionBlur)
                        return source;
                }

                using (var builder = renderGraph.AddRenderPass<MotionBlurData>("Motion Blur", out var passData, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                {
                    PrepareMotionBlurPassData(renderGraph, builder, passData, hdCamera, source, motionVectors, depthTexture);

                    builder.SetRenderFunc(
                        (MotionBlurData data, RenderGraphContext ctx) =>
                        {
                            DoMotionBlur(data, ctx.cmd);
                        });

                    source = passData.destination;
                }
            }

            return source;
        }

        #endregion

        #region Upsample Scene

        private void SetCurrentResolutionGroup(RenderGraph renderGraph, HDCamera camera, ResolutionGroup newResGroup)
        {
            if (resGroup == newResGroup)
                return;

            resGroup = newResGroup;

            // Change the post process resolution for any passes that read it during Render Graph setup
            camera.SetPostProcessScreenSize(postProcessViewportSize.x, postProcessViewportSize.y);

            // Change the post process resolution for any passes that read it during Render Graph execution
            UpdatePostProcessScreenSize(renderGraph, camera, postProcessViewportSize.x, postProcessViewportSize.y);
        }

        #endregion


        #region Panini Projection
        Vector2 CalcViewExtents(HDCamera camera)
        {
            float fovY = camera.camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = (float)postProcessViewportSize.x / (float)postProcessViewportSize.y;

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

        class PaniniProjectionData
        {
            public ComputeShader paniniProjectionCS;
            public int paniniProjectionKernel;
            public Vector4 paniniParams;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
        }

        TextureHandle PaniniProjectionPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
            {
                using (var builder = renderGraph.AddRenderPass<PaniniProjectionData>("Panini Projection", out var passData, ProfilingSampler.Get(HDProfileId.PaniniProjection)))
                {
                    passData.width = postProcessViewportSize.x;
                    passData.height = postProcessViewportSize.y;
                    passData.viewCount = hdCamera.viewCount;
                    passData.paniniProjectionCS = defaultResources.shaders.paniniProjectionCS;
                    passData.paniniProjectionCS.shaderKeywords = null;

                    float distance = m_PaniniProjection.distance.value;
                    var viewExtents = CalcViewExtents(hdCamera);
                    var cropExtents = CalcCropExtents(hdCamera, distance);

                    float scaleX = cropExtents.x / viewExtents.x;
                    float scaleY = cropExtents.y / viewExtents.y;
                    float scaleF = Mathf.Min(scaleX, scaleY);

                    float paniniD = distance;
                    float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

                    if (1f - Mathf.Abs(paniniD) > float.Epsilon)
                        passData.paniniProjectionCS.EnableKeyword("GENERIC");
                    else
                        passData.paniniProjectionCS.EnableKeyword("UNITDISTANCE");

                    if (PostProcessEnableAlpha(hdCamera))
                        passData.paniniProjectionCS.EnableKeyword("ENABLE_ALPHA");

                    passData.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
                    passData.paniniProjectionKernel = passData.paniniProjectionCS.FindKernel("KMain");

                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(hdCamera, renderGraph, "Panini Projection Destination"));

                    builder.SetRenderFunc(
                        (PaniniProjectionData data, RenderGraphContext ctx) =>
                        {
                            var cs = data.paniniProjectionCS;
                            int kernel = data.paniniProjectionKernel;

                            ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, data.paniniParams);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(cs, kernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                        });

                    source = passData.destination;
                }
            }

            return source;
        }

        #endregion

        #region Bloom

        class BloomData
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
            public Vector4[] bloomMipInfo = new Vector4[k_MaxBloomMipCount + 1];

            public float bloomScatterParam;
            public Vector4 thresholdParams;

            public TextureHandle source;
            public TextureHandle[] mipsDown = new TextureHandle[k_MaxBloomMipCount + 1];
            public TextureHandle[] mipsUp = new TextureHandle[k_MaxBloomMipCount + 1];
        }

        void PrepareBloomData(RenderGraph renderGraph, in RenderGraphBuilder builder, BloomData passData, HDCamera camera, TextureHandle source)
        {
            passData.viewCount = camera.viewCount;
            passData.bloomPrefilterCS = defaultResources.shaders.bloomPrefilterCS;
            passData.bloomPrefilterKernel = passData.bloomPrefilterCS.FindKernel("KMain");

            passData.bloomPrefilterCS.shaderKeywords = null;
            if (m_Bloom.highQualityPrefiltering)
                passData.bloomPrefilterCS.EnableKeyword("HIGH_QUALITY");
            else
                passData.bloomPrefilterCS.EnableKeyword("LOW_QUALITY");
            if (PostProcessEnableAlpha(camera))
                passData.bloomPrefilterCS.EnableKeyword("ENABLE_ALPHA");

            passData.bloomBlurCS = defaultResources.shaders.bloomBlurCS;
            passData.bloomBlurKernel = passData.bloomBlurCS.FindKernel("KMain");
            passData.bloomDownsampleKernel = passData.bloomBlurCS.FindKernel("KDownsample");
            passData.bloomUpsampleCS = defaultResources.shaders.bloomUpsampleCS;
            passData.bloomUpsampleCS.shaderKeywords = null;

            var highQualityFiltering = m_Bloom.highQualityFiltering;
            // We switch to bilinear upsampling as it goes less wide than bicubic and due to our border/RTHandle handling, going wide on small resolution
            // where small mips have a strong influence, might result problematic.
            if (postProcessViewportSize.x < 800 || postProcessViewportSize.y < 450) highQualityFiltering = false;

            if (highQualityFiltering)
                passData.bloomUpsampleCS.EnableKeyword("HIGH_QUALITY");
            else
                passData.bloomUpsampleCS.EnableKeyword("LOW_QUALITY");

            passData.bloomUpsampleKernel = passData.bloomUpsampleCS.FindKernel("KMain");
            passData.bloomScatterParam = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            passData.thresholdParams = GetBloomThresholdParams();

            var resolution = m_Bloom.resolution;
            float scaleW = 1f / ((int)resolution / 2f);
            float scaleH = 1f / ((int)resolution / 2f);

            // If the scene is less than 50% of 900p, then we operate on full res, since it's going to be cheap anyway and this might improve quality in challenging situations.
            if (postProcessViewportSize.x < 800 || postProcessViewportSize.y < 450)
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
            int maxSize = Mathf.Max(postProcessViewportSize.x, postProcessViewportSize.y);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 2 - (resolution == BloomResolution.Half ? 0 : 1));
            passData.bloomMipCount = Mathf.Clamp(iterations, 1, k_MaxBloomMipCount);

            for (int i = 0; i < passData.bloomMipCount; i++)
            {
                float p = 1f / Mathf.Pow(2f, i + 1f);
                float sw = scaleW * p;
                float sh = scaleH * p;
                int pw, ph;
                if (camera.DynResRequest.hardwareEnabled)
                {
                    pw = Mathf.Max(1, Mathf.CeilToInt(sw * postProcessViewportSize.x));
                    ph = Mathf.Max(1, Mathf.CeilToInt(sh * postProcessViewportSize.y));
                }
                else
                {
                    pw = Mathf.Max(1, Mathf.RoundToInt(sw * postProcessViewportSize.x));
                    ph = Mathf.Max(1, Mathf.RoundToInt(sh * postProcessViewportSize.y));
                }
                var scale = new Vector2(sw, sh);
                var pixelSize = new Vector2Int(pw, ph);

                passData.bloomMipInfo[i] = new Vector4(pw, ph, sw, sh);
                passData.mipsDown[i] = builder.CreateTransientTexture(new TextureDesc(scale, IsDynamicResUpscaleTargetEnabled(), true)
                { colorFormat = GetPostprocessTextureFormat(camera), enableRandomWrite = true, name = "BloomMipDown" });

                if (i != 0)
                {
                    passData.mipsUp[i] = builder.CreateTransientTexture(new TextureDesc(scale, IsDynamicResUpscaleTargetEnabled(), true)
                    { colorFormat = GetPostprocessTextureFormat(camera), enableRandomWrite = true, name = "BloomMipUp" });
                }
            }

            // the mip up 0 will be used by uber, so not allocated as transient.
            m_BloomBicubicParams = new Vector4(passData.bloomMipInfo[0].x, passData.bloomMipInfo[0].y, 1.0f / passData.bloomMipInfo[0].x, 1.0f / passData.bloomMipInfo[0].y);
            var mip0Scale = new Vector2(passData.bloomMipInfo[0].z, passData.bloomMipInfo[0].w);

            // We undo the scale here, because bloom uses these parameters for its bicubic filtering offset.
            // The bicubic filtering function is SampleTexture2DBicubic, and it requires the underlying texture's
            // unscaled pixel sizes to compute the offsets of the samples.
            // For more info please see the implementation of SampleTexture2DBicubic
            m_BloomBicubicParams.x /= RTHandles.rtHandleProperties.rtHandleScale.x;
            m_BloomBicubicParams.y /= RTHandles.rtHandleProperties.rtHandleScale.y;
            m_BloomBicubicParams.z *= RTHandles.rtHandleProperties.rtHandleScale.x;
            m_BloomBicubicParams.w *= RTHandles.rtHandleProperties.rtHandleScale.y;

            passData.mipsUp[0] = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(mip0Scale, IsDynamicResUpscaleTargetEnabled(), true)
            {
                name = "Bloom final mip up",
                colorFormat = GetPostprocessTextureFormat(camera),
                useMipMap = false,
                enableRandomWrite = true
            }));
            passData.source = builder.ReadTexture(source);
        }

        TextureHandle BloomPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            bool bloomActive = m_Bloom.IsActive() && m_BloomFS;
            TextureHandle bloomTexture = renderGraph.defaultResources.blackTextureXR;
            if (bloomActive)
            {
                using (var builder = renderGraph.AddRenderPass<BloomData>("Bloom", out var passData, ProfilingSampler.Get(HDProfileId.Bloom)))
                {
                    PrepareBloomData(renderGraph, builder, passData, hdCamera, source);

                    builder.SetRenderFunc(
                        (BloomData data, RenderGraphContext ctx) =>
                        {
                            RTHandle sourceRT = data.source;

                            // All the computes for this effect use the same group size so let's use a local
                            // function to simplify dispatches
                            // Make sure the thread group count is sufficient to draw the guard bands
                            void DispatchWithGuardBands(CommandBuffer cmd, ComputeShader shader, int kernelId, in Vector2Int size, in int viewCount)
                            {
                                int w = size.x;
                                int h = size.y;

                                if (w < sourceRT.rt.width && w % 8 < k_RTGuardBandSize)
                                    w += k_RTGuardBandSize;
                                if (h < sourceRT.rt.height && h % 8 < k_RTGuardBandSize)
                                    h += k_RTGuardBandSize;

                                cmd.DispatchCompute(shader, kernelId, (w + 7) / 8, (h + 7) / 8, viewCount);
                            }

                            // Pre-filtering
                            ComputeShader cs;
                            int kernel;
                            {
                                var size = new Vector2Int((int)data.bloomMipInfo[0].x, (int)data.bloomMipInfo[0].y);
                                cs = data.bloomPrefilterCS;
                                kernel = data.bloomPrefilterKernel;

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourceRT);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.mipsUp[0]); // Use m_BloomMipsUp as temp target
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomThreshold, data.thresholdParams);
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);

                                cs = data.bloomBlurCS;
                                kernel = data.bloomBlurKernel;

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.mipsUp[0]);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.mipsDown[0]);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);
                            }

                            // Blur pyramid
                            kernel = data.bloomDownsampleKernel;

                            for (int i = 0; i < data.bloomMipCount - 1; i++)
                            {
                                var src = data.mipsDown[i];
                                var dst = data.mipsDown[i + 1];
                                var size = new Vector2Int((int)data.bloomMipInfo[i + 1].x, (int)data.bloomMipInfo[i + 1].y);

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, src);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);
                            }

                            // Upsample & combine
                            cs = data.bloomUpsampleCS;
                            kernel = data.bloomUpsampleKernel;

                            for (int i = data.bloomMipCount - 2; i >= 0; i--)
                            {
                                var low = (i == data.bloomMipCount - 2) ? data.mipsDown : data.mipsUp;
                                var srcLow = low[i + 1];
                                var srcHigh = data.mipsDown[i];
                                var dst = data.mipsUp[i];
                                var highSize = new Vector2Int((int)data.bloomMipInfo[i].x, (int)data.bloomMipInfo[i].y);
                                var lowSize = new Vector2Int((int)data.bloomMipInfo[i + 1].x, (int)data.bloomMipInfo[i + 1].y);

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputLowTexture, srcLow);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHighTexture, srcHigh);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(data.bloomScatterParam, 0f, 0f, 0f));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomBicubicParams, new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, highSize, data.viewCount);
                            }
                        });

                    bloomTexture = passData.mipsUp[0];
                }
            }

            return bloomTexture;
        }

        #endregion

        #region Color Grading
        class ColorGradingPassData
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

            public TextureHandle logLut;
        }

        void PrepareColorGradingParameters(ColorGradingPassData passData)
        {
            passData.tonemappingMode = m_TonemappingFS ? m_Tonemapping.mode.value : TonemappingMode.None;

            passData.builderCS = defaultResources.shaders.lutBuilder3DCS;
            passData.builderKernel = passData.builderCS.FindKernel("KBuild");

            // Setup lut builder compute & grab the kernel we need
            passData.builderCS.shaderKeywords = null;

            if (m_Tonemapping.IsActive() && m_TonemappingFS)
            {
                switch (passData.tonemappingMode)
                {
                    case TonemappingMode.Neutral: passData.builderCS.EnableKeyword("TONEMAPPING_NEUTRAL"); break;
                    case TonemappingMode.ACES: passData.builderCS.EnableKeyword("TONEMAPPING_ACES"); break;
                    case TonemappingMode.Custom: passData.builderCS.EnableKeyword("TONEMAPPING_CUSTOM"); break;
                    case TonemappingMode.External: passData.builderCS.EnableKeyword("TONEMAPPING_EXTERNAL"); break;
                }
            }
            else
            {
                passData.builderCS.EnableKeyword("TONEMAPPING_NONE");
            }

            passData.lutSize = m_LutSize;

            //passData.colorFilter;
            passData.lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            passData.hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            passData.channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value / 100f, m_ChannelMixer.redOutGreenIn.value / 100f, m_ChannelMixer.redOutBlueIn.value / 100f, 0f);
            passData.channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            passData.channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value / 100f, m_ChannelMixer.blueOutGreenIn.value / 100f, m_ChannelMixer.blueOutBlueIn.value / 100f, 0f);

            ComputeShadowsMidtonesHighlights(out passData.shadows, out passData.midtones, out passData.highlights, out passData.shadowsHighlightsLimits);
            ComputeLiftGammaGain(out passData.lift, out passData.gamma, out passData.gain);
            ComputeSplitToning(out passData.splitShadows, out passData.splitHighlights);

            // Be careful, if m_Curves is modified between preparing the render pass and executing it, result will be wrong.
            // However this should be fine for now as all updates should happen outisde rendering.
            passData.curves = m_Curves;

            if (passData.tonemappingMode == TonemappingMode.Custom)
            {
                passData.hableCurve = m_HableCurve;
                passData.hableCurve.Init(
                    m_Tonemapping.toeStrength.value,
                    m_Tonemapping.toeLength.value,
                    m_Tonemapping.shoulderStrength.value,
                    m_Tonemapping.shoulderLength.value,
                    m_Tonemapping.shoulderAngle.value,
                    m_Tonemapping.gamma.value
                );
            }
            else if (passData.tonemappingMode == TonemappingMode.External)
            {
                passData.externalLuT = m_Tonemapping.lutTexture.value;
                passData.lutContribution = m_Tonemapping.lutContribution.value;
            }

            passData.colorFilter = m_ColorAdjustments.colorFilter.value.linear;
            passData.miscParams = new Vector4(m_ColorGradingFS ? 1f : 0f, 0f, 0f, 0f);
        }

        // Returns color balance coefficients in the LMS space
        static Vector3 GetColorBalanceCoeffs(float temperature, float tint)
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

        TextureHandle ColorGradingPass(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<ColorGradingPassData>("Color Grading", out var passData, ProfilingSampler.Get(HDProfileId.ColorGradingLUTBuilder)))
            {
                TextureHandle logLut = renderGraph.CreateTexture(new TextureDesc(m_LutSize, m_LutSize)
                {
                    name = "Color Grading Log Lut",
                    dimension = TextureDimension.Tex3D,
                    slices = m_LutSize,
                    depthBufferBits = DepthBits.None,
                    colorFormat = m_LutFormat,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    useMipMap = false,
                    enableRandomWrite = true
                });

                PrepareColorGradingParameters(passData);
                passData.logLut = builder.WriteTexture(logLut);

                builder.SetRenderFunc(
                    (ColorGradingPassData data, RenderGraphContext ctx) =>
                    {
                        var builderCS = data.builderCS;
                        var builderKernel = data.builderKernel;

                        // Fill-in constant buffers & textures.
                        // TODO: replace with a real constant buffers
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._OutputTexture, data.logLut);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Size, new Vector4(data.lutSize, 1f / (data.lutSize - 1f), 0f, 0f));
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorBalance, data.lmsColorBalance);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorFilter, data.colorFilter);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerRed, data.channelMixerR);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerGreen, data.channelMixerG);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerBlue, data.channelMixerB);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._HueSatCon, data.hueSatCon);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Lift, data.lift);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gamma, data.gamma);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gain, data.gain);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Shadows, data.shadows);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Midtones, data.midtones);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Highlights, data.highlights);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShaHiLimits, data.shadowsHighlightsLimits);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitShadows, data.splitShadows);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitHighlights, data.splitHighlights);

                        // YRGB
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveMaster, data.curves.master.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveRed, data.curves.red.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveGreen, data.curves.green.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveBlue, data.curves.blue.value.GetTexture());

                        // Secondary curves
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsHue, data.curves.hueVsHue.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsSat, data.curves.hueVsSat.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveLumVsSat, data.curves.lumVsSat.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveSatVsSat, data.curves.satVsSat.value.GetTexture());

                        // Artist-driven tonemap curve
                        if (data.tonemappingMode == TonemappingMode.Custom)
                        {
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._CustomToneCurve, data.hableCurve.uniforms.curve);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentA, data.hableCurve.uniforms.toeSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentB, data.hableCurve.uniforms.toeSegmentB);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentA, data.hableCurve.uniforms.midSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentB, data.hableCurve.uniforms.midSegmentB);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentA, data.hableCurve.uniforms.shoSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentB, data.hableCurve.uniforms.shoSegmentB);
                        }
                        else if (data.tonemappingMode == TonemappingMode.External)
                        {
                            ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._LogLut3D, data.externalLuT);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._LogLut3D_Params, new Vector4(1f / data.lutSize, data.lutSize - 1f, data.lutContribution, 0f));
                        }

                        // Misc parameters
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Params, data.miscParams);

                        // Generate the lut
                        // See the note about Metal & Intel in LutBuilder3D.compute
                        // GetKernelThreadGroupSizes  is currently broken on some binary versions.
                        //builderCS.GetKernelThreadGroupSizes(builderKernel, out uint threadX, out uint threadY, out uint threadZ);
                        uint threadX = 4;
                        uint threadY = 4;
                        uint threadZ = 4;
                        ctx.cmd.DispatchCompute(builderCS, builderKernel,
                            (int)((data.lutSize + threadX - 1u) / threadX),
                            (int)((data.lutSize + threadY - 1u) / threadY),
                            (int)((data.lutSize + threadZ - 1u) / threadZ)
                        );
                    });

                return passData.logLut;
            }
        }

        #endregion

        #region Uber Post
        // Grabs all active feature flags
        UberPostFeatureFlags GetUberFeatureFlags(HDCamera camera, bool isSceneView)
        {
            var flags = UberPostFeatureFlags.None;

            if (m_ChromaticAberration.IsActive() && m_ChromaticAberrationFS)
                flags |= UberPostFeatureFlags.ChromaticAberration;

            if (m_Vignette.IsActive() && m_VignetteFS)
                flags |= UberPostFeatureFlags.Vignette;

            if (m_LensDistortion.IsActive() && !isSceneView && m_LensDistortionFS)
                flags |= UberPostFeatureFlags.LensDistortion;

            if (PostProcessEnableAlpha(camera))
            {
                flags |= UberPostFeatureFlags.EnableAlpha;
            }

            return flags;
        }

        void PrepareLensDistortionParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.LensDistortion) != UberPostFeatureFlags.LensDistortion)
                return;

            data.uberPostCS.EnableKeyword("LENS_DISTORTION");

            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            data.lensDistortionParams1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            data.lensDistortionParams2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );
        }

        void PrepareChromaticAberrationParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;

            data.uberPostCS.EnableKeyword("CHROMATIC_ABERRATION");

            var spectralLut = m_ChromaticAberration.spectralLut.value;

            // If no spectral lut is set, use a pre-generated one
            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
                    {
                        name = "Chromatic Aberration Spectral LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new[]
                    {
                        new Color(1f, 0f, 0f, 1f),
                        new Color(0f, 1f, 0f, 1f),
                        new Color(0f, 0f, 1f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }

            data.spectralLut = spectralLut;
            data.chromaticAberrationParameters = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples, 0f, 0f);
        }

        void PrepareVignetteParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            data.uberPostCS.EnableKeyword("VIGNETTE");

            if (m_Vignette.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                data.vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
                data.vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? 1f : 0f);
                data.vignetteColor = m_Vignette.color.value;
                data.vignetteMask = Texture2D.blackTexture;
            }
            else // Masked
            {
                var color = m_Vignette.color.value;
                color.a = Mathf.Clamp01(m_Vignette.opacity.value);

                data.vignetteParams1 = new Vector4(0f, 0f, 1f, 0f);
                data.vignetteColor = color;
                data.vignetteMask = m_Vignette.mask.value;
            }
        }

        Vector4 GetBloomThresholdParams()
        {
            const float k_Softness = 0.5f;
            float lthresh = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = lthresh * k_Softness + 1e-5f;
            return new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
        }

        void PrepareUberBloomParameters(UberPostPassData data, HDCamera camera)
        {
            float intensity = Mathf.Pow(2f, m_Bloom.intensity.value) - 1f; // Makes intensity easier to control
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            int dirtEnabled = m_Bloom.dirtTexture.value != null && m_Bloom.dirtIntensity.value > 0f ? 1 : 0;
            float dirtRatio = (float)dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = (float)postProcessViewportSize.x / (float)postProcessViewportSize.y;
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

            data.bloomDirtTexture = dirtTexture;
            data.bloomParams = new Vector4(intensity, dirtIntensity, 1f, dirtEnabled);
            data.bloomTint = (Vector4)tint;
            data.bloomDirtTileOffset = dirtTileOffset;
            data.bloomThreshold = GetBloomThresholdParams();
            data.bloomBicubicParams = m_BloomBicubicParams;
        }

        void PrepareAlphaScaleParameters(UberPostPassData data, HDCamera camera)
        {
            if (PostProcessEnableAlpha(camera))
                data.alphaScaleBias = Compositor.CompositionManager.GetAlphaScaleAndBiasForCamera(camera);
            else
                data.alphaScaleBias = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        }

        class UberPostPassData
        {
            public ComputeShader uberPostCS;
            public int uberPostKernel;
            public bool outputColorLog;
            public bool isSearchingInHierarchy;
            public int width;
            public int height;
            public int viewCount;

            public Vector4 logLutSettings;

            public Vector4 lensDistortionParams1;
            public Vector4 lensDistortionParams2;

            public Texture spectralLut;
            public Vector4 chromaticAberrationParameters;

            public Vector4 vignetteParams1;
            public Vector4 vignetteParams2;
            public Vector4 vignetteColor;
            public Texture vignetteMask;

            public Texture bloomDirtTexture;
            public Vector4 bloomParams;
            public Vector4 bloomTint;
            public Vector4 bloomBicubicParams;
            public Vector4 bloomDirtTileOffset;
            public Vector4 bloomThreshold;

            public Vector4 alphaScaleBias;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle logLut;
            public TextureHandle bloomTexture;
        }

        TextureHandle UberPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle logLut, TextureHandle bloomTexture, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            var featureFlags = GetUberFeatureFlags(hdCamera, isSceneView);
            // If we have nothing to do in Uber post we just skip it.
            if (featureFlags == UberPostFeatureFlags.None && !m_ColorGradingFS && !m_BloomFS &&
                m_CurrentDebugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.ColorLog)
            {
                return source;
            }

            using (var builder = renderGraph.AddRenderPass<UberPostPassData>("Uber Post", out var passData, ProfilingSampler.Get(HDProfileId.UberPost)))
            {
                TextureHandle dest = GetPostprocessOutputHandle(hdCamera, renderGraph, "Uber Post Destination");

                // Feature flags are passed to all effects and it's their responsibility to check
                // if they are used or not so they can set default values if needed
                passData.uberPostCS = defaultResources.shaders.uberPostCS;
                passData.uberPostCS.shaderKeywords = null;

                passData.uberPostKernel = passData.uberPostCS.FindKernel("Uber");
                if (PostProcessEnableAlpha(hdCamera))
                {
                    passData.uberPostCS.EnableKeyword("ENABLE_ALPHA");
                }

                if (hdCamera.DynResRequest.enabled && hdCamera.DynResRequest.filter == DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres && DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.AfterPost)
                {
                    passData.uberPostCS.EnableKeyword("GAMMA2_OUTPUT");
                }

                passData.outputColorLog = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ColorLog;
                passData.isSearchingInHierarchy = CoreUtils.IsSceneFilteringEnabled();
                passData.width = postProcessViewportSize.x;
                passData.height = postProcessViewportSize.y;
                passData.viewCount = hdCamera.viewCount;

                // Color grading
                // This should be EV100 instead of EV but given that EV100(0) isn't equal to 1, it means
                // we can't use 0 as the default neutral value which would be confusing to users
                float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
                passData.logLutSettings = new Vector4(1f / m_LutSize, m_LutSize - 1f, postExposureLinear, (m_ColorGradingFS || m_TonemappingFS) ? 1 : 0);

                // Setup the rest of the effects
                PrepareLensDistortionParameters(passData, featureFlags);
                PrepareChromaticAberrationParameters(passData, featureFlags);
                PrepareVignetteParameters(passData, featureFlags);
                PrepareUberBloomParameters(passData, hdCamera);
                PrepareAlphaScaleParameters(passData, hdCamera);

                passData.source = builder.ReadTexture(source);
                passData.bloomTexture = builder.ReadTexture(bloomTexture);
                passData.logLut = builder.ReadTexture(logLut);
                passData.destination = builder.WriteTexture(dest);

                builder.SetRenderFunc(
                    (UberPostPassData data, RenderGraphContext ctx) =>
                    {
                        // Color grading
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._LogLut3D, data.logLut);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._LogLut3D_Params, data.logLutSettings);

                        // Lens distortion
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._DistortionParams1, data.lensDistortionParams1);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._DistortionParams2, data.lensDistortionParams2);

                        // Chromatic aberration
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._ChromaSpectralLut, data.spectralLut);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._ChromaParams, data.chromaticAberrationParameters);

                        // Vignette
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteParams1, data.vignetteParams1);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteParams2, data.vignetteParams2);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteColor, data.vignetteColor);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._VignetteMask, data.vignetteMask);

                        // Bloom
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._BloomTexture, data.bloomTexture);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._BloomDirtTexture, data.bloomDirtTexture);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomParams, data.bloomParams);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomTint, data.bloomTint);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomBicubicParams, data.bloomBicubicParams);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomDirtScaleOffset, data.bloomDirtTileOffset);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomThreshold, data.bloomThreshold);

                        // Alpha scale and bias (only used when alpha is enabled)
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._AlphaScaleBias, data.alphaScaleBias);

                        // Dispatch uber post
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, "_DebugFlags", new Vector4(data.outputColorLog ? 1 : 0, 0, 0, data.isSearchingInHierarchy ? 1 : 0));
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._InputTexture, data.source);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._OutputTexture, data.destination);
                        ctx.cmd.DispatchCompute(data.uberPostCS, data.uberPostKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                    });

                source = passData.destination;
            }

            return source;
        }

        #endregion

        #region FXAA
        class FXAAData
        {
            public ComputeShader fxaaCS;
            public int fxaaKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
        }

        TextureHandle FXAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled &&     // Dynamic resolution is on.
                hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing &&
                m_AntialiasingFS)
            {
                using (var builder = renderGraph.AddRenderPass<FXAAData>("FXAA", out var passData, ProfilingSampler.Get(HDProfileId.FXAA)))
                {
                    passData.fxaaCS = defaultResources.shaders.FXAACS;
                    passData.fxaaKernel = passData.fxaaCS.FindKernel("FXAA");
                    passData.width = postProcessViewportSize.x;
                    passData.height = postProcessViewportSize.y;
                    passData.viewCount = hdCamera.viewCount;

                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(hdCamera, renderGraph, "FXAA Destination")); ;

                    passData.fxaaCS.shaderKeywords = null;
                    if (PostProcessEnableAlpha(hdCamera))
                        passData.fxaaCS.EnableKeyword("ENABLE_ALPHA");

                    builder.SetRenderFunc(
                        (FXAAData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.fxaaCS, data.fxaaKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.fxaaCS, data.fxaaKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(data.fxaaCS, data.fxaaKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                        });

                    source = passData.destination;
                }
            }

            return source;
        }

        #endregion

        #region CAS
        class CASData
        {
            public ComputeShader casCS;
            public int initKernel;
            public int mainKernel;
            public int viewCount;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int outputHeight;

            public TextureHandle source;
            public TextureHandle destination;

            public ComputeBufferHandle casParametersBuffer;
        }

        TextureHandle ContrastAdaptiveSharpeningPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled && hdCamera.DynResRequest.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
            {
                using (var builder = renderGraph.AddRenderPass<CASData>("Contrast Adaptive Sharpen", out var passData, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                {
                    passData.casCS = defaultResources.shaders.contrastAdaptiveSharpenCS;
                    passData.casCS.shaderKeywords = null;
                    passData.initKernel = passData.casCS.FindKernel("KInitialize");
                    passData.mainKernel = passData.casCS.FindKernel("KMain");
                    passData.viewCount = hdCamera.viewCount;
                    passData.inputWidth = postProcessViewportSize.x;
                    passData.inputHeight = postProcessViewportSize.y;
                    passData.outputWidth = Mathf.RoundToInt(hdCamera.finalViewport.width);
                    passData.outputHeight = Mathf.RoundToInt(hdCamera.finalViewport.height);
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "Contrast Adaptive Sharpen Destination")); ;
                    passData.casParametersBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(2, sizeof(uint) * 4) { name = "Cas Parameters" });

                    builder.SetRenderFunc(
                        (CASData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeFloatParam(data.casCS, HDShaderIDs._Sharpness, 1);
                            ctx.cmd.SetComputeTextureParam(data.casCS, data.mainKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeVectorParam(data.casCS, HDShaderIDs._InputTextureDimensions, new Vector4(data.inputWidth, data.inputHeight));
                            ctx.cmd.SetComputeTextureParam(data.casCS, data.mainKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.SetComputeVectorParam(data.casCS, HDShaderIDs._OutputTextureDimensions, new Vector4(data.outputWidth, data.outputHeight));
                            ctx.cmd.SetComputeBufferParam(data.casCS, data.initKernel, "CasParameters", data.casParametersBuffer);
                            ctx.cmd.SetComputeBufferParam(data.casCS, data.mainKernel, "CasParameters", data.casParametersBuffer);
                            ctx.cmd.DispatchCompute(data.casCS, data.initKernel, 1, 1, 1);

                            int dispatchX = HDUtils.DivRoundUp(data.outputWidth, 16);
                            int dispatchY = HDUtils.DivRoundUp(data.outputHeight, 16);

                            ctx.cmd.DispatchCompute(data.casCS, data.mainKernel, dispatchX, dispatchY, data.viewCount);
                        });

                    source = passData.destination;
                }

                SetCurrentResolutionGroup(renderGraph, hdCamera, ResolutionGroup.AfterDynamicResUpscale);
            }
            return source;
        }

        #endregion

        #region RCAS
        class RCASData
        {
            public ComputeShader rcasCS;
            public int initKernel;
            public int mainKernel;
            public int viewCount;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int outputHeight;

            public TextureHandle source;
            public TextureHandle destination;

            public ComputeBufferHandle casParametersBuffer;
        }

        TextureHandle RobustContrastAdaptiveSharpeningPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled && hdCamera.DynResRequest.filter == DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres)
            {
                using (var builder = renderGraph.AddRenderPass<RCASData>("Robust Contrast Adaptive Sharpen", out var passData, ProfilingSampler.Get(HDProfileId.RobustContrastAdaptiveSharpen)))
                {
                    passData.rcasCS = defaultResources.shaders.robustContrastAdaptiveSharpenCS;
                    if (PostProcessEnableAlpha(hdCamera))
                        passData.rcasCS.EnableKeyword("ENABLE_ALPHA");
                    else
                        passData.rcasCS.DisableKeyword("ENABLE_ALPHA");
                    passData.initKernel = passData.rcasCS.FindKernel("KInitialize");
                    passData.mainKernel = passData.rcasCS.FindKernel("KMain");
                    passData.viewCount = hdCamera.viewCount;
                    passData.inputWidth = Mathf.RoundToInt(hdCamera.finalViewport.width);
                    passData.inputHeight = Mathf.RoundToInt(hdCamera.finalViewport.height);
                    passData.outputWidth = Mathf.RoundToInt(hdCamera.finalViewport.width);
                    passData.outputHeight = Mathf.RoundToInt(hdCamera.finalViewport.height);
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "Robust Contrast Adaptive Sharpen Destination"));
                    passData.casParametersBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(1, sizeof(uint) * 4) { name = "Robust Cas Parameters" });

                    builder.SetRenderFunc(
                        (RCASData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeFloatParam(data.rcasCS, HDShaderIDs._RCASScale, 1.0f);
                            ctx.cmd.SetComputeTextureParam(data.rcasCS, data.mainKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.rcasCS, data.mainKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.SetComputeBufferParam(data.rcasCS, data.initKernel, HDShaderIDs._RCasParameters, data.casParametersBuffer);
                            ctx.cmd.SetComputeBufferParam(data.rcasCS, data.mainKernel, HDShaderIDs._RCasParameters, data.casParametersBuffer);
                            ctx.cmd.DispatchCompute(data.rcasCS, data.initKernel, 1, 1, 1);

                            int dispatchX = HDUtils.DivRoundUp(data.outputWidth, 8);
                            int dispatchY = HDUtils.DivRoundUp(data.outputHeight, 8);

                            ctx.cmd.DispatchCompute(data.rcasCS, data.mainKernel, dispatchX, dispatchY, data.viewCount);
                        });

                    source = passData.destination;
                }
            }
            return source;
        }

        #endregion

        #region EASU
        class EASUData
        {
            public ComputeShader easuCS;
            public int initKernel;
            public int mainKernel;
            public int viewCount;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int outputHeight;

            public TextureHandle source;
            public TextureHandle destination;

            public ComputeBufferHandle easuParameterBuffer;
        }

        TextureHandle EdgeAdaptiveSpatialUpsampling(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled && hdCamera.DynResRequest.filter == DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres)
            {
                using (var builder = renderGraph.AddRenderPass<EASUData>("Edge Adaptive Spatial Upsampling", out var passData, ProfilingSampler.Get(HDProfileId.EdgeAdaptiveSpatialUpsampling)))
                {
                    passData.easuCS = defaultResources.shaders.edgeAdaptiveSpatialUpsamplingCS;
                    passData.easuCS.shaderKeywords = null;
                    if (PostProcessEnableAlpha(hdCamera))
                        passData.easuCS.EnableKeyword("ENABLE_ALPHA");
                    else
                        passData.easuCS.DisableKeyword("ENABLE_ALPHA");
                    passData.initKernel = passData.easuCS.FindKernel("KInitialize");
                    passData.mainKernel = passData.easuCS.FindKernel("KMain");
                    passData.viewCount = hdCamera.viewCount;
                    passData.inputWidth = hdCamera.actualWidth;
                    passData.inputHeight = hdCamera.actualHeight;
                    passData.outputWidth = Mathf.RoundToInt(hdCamera.finalViewport.width);
                    passData.outputHeight = Mathf.RoundToInt(hdCamera.finalViewport.height);
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "Edge Adaptive Spatial Upsampling"));
                    passData.easuParameterBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(4, sizeof(uint) * 4) { name = "EASU Parameters" });

                    builder.SetRenderFunc(
                        (EASUData data, RenderGraphContext ctx) =>
                        {
                            var sourceTexture = (RenderTexture)data.source;
                            var inputTextureSize = new Vector4(sourceTexture.width, sourceTexture.height);
                            if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
                            {
                                var maxScaledSz = DynamicResolutionHandler.instance.ApplyScalesOnSize(new Vector2Int(RTHandles.maxWidth, RTHandles.maxHeight));
                                inputTextureSize = new Vector4(maxScaledSz.x, maxScaledSz.y);
                            }
                            ctx.cmd.SetComputeTextureParam(data.easuCS, data.mainKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeVectorParam(data.easuCS, HDShaderIDs._EASUViewportSize, new Vector4(data.inputWidth, data.inputHeight));
                            ctx.cmd.SetComputeVectorParam(data.easuCS, HDShaderIDs._EASUInputImageSize, inputTextureSize);
                            ctx.cmd.SetComputeTextureParam(data.easuCS, data.mainKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.SetComputeVectorParam(data.easuCS, HDShaderIDs._EASUOutputSize, new Vector4(data.outputWidth, data.outputHeight, 1.0f / data.outputWidth, 1.0f / data.outputHeight));
                            ctx.cmd.SetComputeBufferParam(data.easuCS, data.initKernel, HDShaderIDs._EASUParameters, data.easuParameterBuffer);
                            ctx.cmd.SetComputeBufferParam(data.easuCS, data.mainKernel, HDShaderIDs._EASUParameters, data.easuParameterBuffer);
                            ctx.cmd.DispatchCompute(data.easuCS, data.initKernel, 1, 1, 1);

                            int dispatchX = HDUtils.DivRoundUp(data.outputWidth, 8);
                            int dispatchY = HDUtils.DivRoundUp(data.outputHeight, 8);

                            ctx.cmd.DispatchCompute(data.easuCS, data.mainKernel, dispatchX, dispatchY, data.viewCount);
                        });

                    source = passData.destination;
                }

                SetCurrentResolutionGroup(renderGraph, hdCamera, ResolutionGroup.AfterDynamicResUpscale);
            }
            return source;
        }

        #endregion

        #region Final Pass

        class FinalPassData
        {
            public bool postProcessEnabled;
            public bool performUpsampling;
            public Material finalPassMaterial;
            public HDCamera hdCamera;
            public BlueNoise blueNoise;
            public bool flipY;
            public System.Random random;
            public bool useFXAA;
            public bool enableAlpha;
            public bool keepAlpha;
            public bool dynamicResIsOn;
            public DynamicResUpscaleFilter dynamicResFilter;

            public bool filmGrainEnabled;
            public Texture filmGrainTexture;
            public float filmGrainIntensity;
            public float filmGrainResponse;

            public bool ditheringEnabled;

            public TextureHandle source;
            public TextureHandle afterPostProcessTexture;
            public TextureHandle alphaTexture;
            public TextureHandle destination;
            public bool postProcessIsFinalPass;
        }

        void FinalPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle afterPostProcessTexture, TextureHandle alphaTexture, TextureHandle finalRT, TextureHandle source, BlueNoise blueNoise, bool flipY, bool postProcessIsFinalPass)
        {
            using (var builder = renderGraph.AddRenderPass<FinalPassData>("Final Pass", out var passData, ProfilingSampler.Get(HDProfileId.FinalPost)))
            {
                // General
                passData.postProcessEnabled = m_PostProcessEnabled;
                passData.performUpsampling = DynamicResolutionHandler.instance.upsamplerSchedule == DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;
                passData.finalPassMaterial = m_FinalPassMaterial;
                passData.hdCamera = hdCamera;
                passData.blueNoise = blueNoise;
                passData.flipY = flipY;
                passData.random = m_Random;
                passData.enableAlpha = PostProcessEnableAlpha(hdCamera);
                passData.keepAlpha = m_KeepAlpha;
                passData.dynamicResIsOn = hdCamera.canDoDynamicResolution && hdCamera.DynResRequest.enabled;
                passData.dynamicResFilter = hdCamera.DynResRequest.filter;
                passData.useFXAA = hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing && !passData.dynamicResIsOn && m_AntialiasingFS;

                // Film Grain
                passData.filmGrainEnabled = m_FilmGrain.IsActive() && m_FilmGrainFS;
                if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                    passData.filmGrainTexture = defaultResources.textures.filmGrainTex[(int)m_FilmGrain.type.value];
                else
                    passData.filmGrainTexture = m_FilmGrain.texture.value;
                passData.filmGrainIntensity = m_FilmGrain.intensity.value;
                passData.filmGrainResponse = m_FilmGrain.response.value;

                // Dithering
                passData.ditheringEnabled = hdCamera.dithering && m_DitheringFS;

                passData.source = builder.ReadTexture(source);
                passData.afterPostProcessTexture = builder.ReadTexture(afterPostProcessTexture);
                passData.alphaTexture = builder.ReadTexture(alphaTexture);
                passData.destination = builder.WriteTexture(finalRT);
                passData.postProcessIsFinalPass = postProcessIsFinalPass;

                builder.SetRenderFunc(
                    (FinalPassData data, RenderGraphContext ctx) =>
                    {
                        // Final pass has to be done in a pixel shader as it will be the one writing straight
                        // to the backbuffer eventually
                        Material finalPassMaterial = data.finalPassMaterial;

                        finalPassMaterial.shaderKeywords = null;
                        finalPassMaterial.SetTexture(HDShaderIDs._InputTexture, data.source);

                        if (data.dynamicResIsOn)
                        {
                            if (data.performUpsampling)
                            {
                                switch (data.dynamicResFilter)
                                {
                                    case DynamicResUpscaleFilter.CatmullRom:
                                        finalPassMaterial.EnableKeyword("CATMULL_ROM_4");
                                        break;
                                    case DynamicResUpscaleFilter.ContrastAdaptiveSharpen:
                                    case DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres:
                                        finalPassMaterial.EnableKeyword("BYPASS");
                                        break;
                                }
                            }
                            else
                            {
                                finalPassMaterial.EnableKeyword("BYPASS");
                            }
                        }

                        if (data.postProcessEnabled)
                        {
                            if (data.useFXAA)
                                finalPassMaterial.EnableKeyword("FXAA");

                            if (data.filmGrainEnabled)
                            {
                                if (data.filmGrainTexture != null) // Fail safe if the resources asset breaks :/
                                {
#if HDRP_DEBUG_STATIC_POSTFX
                                    float offsetX = 0;
                                    float offsetY = 0;
#else
                                    float offsetX = (float)(data.random.NextDouble());
                                    float offsetY = (float)(data.random.NextDouble());
#endif

                                    finalPassMaterial.EnableKeyword("GRAIN");
                                    finalPassMaterial.SetTexture(HDShaderIDs._GrainTexture, data.filmGrainTexture);
                                    finalPassMaterial.SetVector(HDShaderIDs._GrainParams, new Vector2(data.filmGrainIntensity * 4f, data.filmGrainResponse));

                                    float uvScaleX = data.hdCamera.finalViewport.width / (float)data.filmGrainTexture.width;
                                    float uvScaleY = data.hdCamera.finalViewport.height / (float)data.filmGrainTexture.height;
                                    float scaledOffsetX = offsetX * uvScaleX;
                                    float scaledOffsetY = offsetY * uvScaleY;

                                    finalPassMaterial.SetVector(HDShaderIDs._GrainTextureParams, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                                }
                            }

                            if (data.ditheringEnabled)
                            {
                                var blueNoiseTexture = data.blueNoise.textureArray16L;

#if HDRP_DEBUG_STATIC_POSTFX
                                int textureId = 0;
#else
                                int textureId = (int)data.hdCamera.GetCameraFrameCount() % blueNoiseTexture.depth;
#endif

                                finalPassMaterial.EnableKeyword("DITHER");
                                finalPassMaterial.SetTexture(HDShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                                finalPassMaterial.SetVector(HDShaderIDs._DitherParams,
                                    new Vector3(data.hdCamera.finalViewport.width / blueNoiseTexture.width, data.hdCamera.finalViewport.height / blueNoiseTexture.height, textureId));
                            }
                        }

                        RTHandle alphaRTHandle = data.alphaTexture; // Need explicit cast otherwise we get a wrong implicit conversion to RenderTexture :/
                        finalPassMaterial.SetTexture(HDShaderIDs._AlphaTexture, alphaRTHandle);
                        finalPassMaterial.SetFloat(HDShaderIDs._KeepAlpha, data.keepAlpha ? 1.0f : 0.0f);

                        if (data.enableAlpha)
                            finalPassMaterial.EnableKeyword("ENABLE_ALPHA");
                        else
                            finalPassMaterial.DisableKeyword("ENABLE_ALPHA");

                        finalPassMaterial.SetVector(HDShaderIDs._UVTransform,
                            data.flipY
                            ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f)
                            : new Vector4(1.0f, 1.0f, 0.0f, 0.0f)
                        );

                        finalPassMaterial.SetVector(HDShaderIDs._ViewPortSize,
                            new Vector4(data.hdCamera.finalViewport.width, data.hdCamera.finalViewport.height, 1.0f / data.hdCamera.finalViewport.width, 1.0f / data.hdCamera.finalViewport.height));

                        // Blit to backbuffer
                        Rect backBufferRect = data.hdCamera.finalViewport;

                        // When post process is not the final pass, we render at (0,0) so that subsequent rendering does not have to bother about viewports.
                        // Final viewport is handled in the final blit in this case
                        if (!data.postProcessIsFinalPass)
                        {
                            backBufferRect.x = backBufferRect.y = 0;
                        }

                        if (data.hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                        {
                            finalPassMaterial.EnableKeyword("APPLY_AFTER_POST");
                            finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, data.afterPostProcessTexture);
                        }
                        else
                        {
                            finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, TextureXR.GetBlackTexture());
                        }

                        HDUtils.DrawFullScreen(ctx.cmd, backBufferRect, finalPassMaterial, data.destination);
                    });
            }
        }

        #endregion
    }
}
