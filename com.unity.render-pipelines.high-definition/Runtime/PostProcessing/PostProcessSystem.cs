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
    sealed class PostProcessSystem
    {
        private enum SMAAStage
        {
            EdgeDetection = 0,
            BlendWeights = 1,
            NeighborhoodBlending = 2
        }

        GraphicsFormat m_ColorFormat               = GraphicsFormat.B10G11R11_UFloatPack32;
        const GraphicsFormat k_CoCFormat           = GraphicsFormat.R16_SFloat;
        const GraphicsFormat k_ExposureFormat      = GraphicsFormat.R32G32_SFloat;

        readonly RenderPipelineResources m_Resources;
        Material m_FinalPassMaterial;
        Material m_ClearBlackMaterial;
        Material m_SMAAMaterial;
        Material m_TemporalAAMaterial;

        MaterialPropertyBlock m_TAAHistoryBlitPropertyBlock = new MaterialPropertyBlock();
        MaterialPropertyBlock m_TAAPropertyBlock = new MaterialPropertyBlock();

        // Exposure data
        const int k_ExposureCurvePrecision = 128;
        readonly Color[] m_ExposureCurveColorArray = new Color[k_ExposureCurvePrecision];
        readonly int[] m_ExposureVariants = new int[4];

        Texture2D m_ExposureCurveTexture;
        RTHandle m_EmptyExposureTexture; // RGHalf

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
        readonly RTHandle[] m_BloomMipsDown = new RTHandle[k_MaxBloomMipCount + 1];
        readonly RTHandle[] m_BloomMipsUp = new RTHandle[k_MaxBloomMipCount + 1];
        RTHandle m_BloomTexture;

        // Chromatic aberration data
        Texture2D m_InternalSpectralLut;

        // Color grading data
        readonly int m_LutSize;
        RTHandle m_InternalLogLut; // ARGBHalf
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

        // Physical camera ref
        HDPhysicalCamera m_PhysicalCamera;
        static readonly HDPhysicalCamera m_DefaultPhysicalCamera = new HDPhysicalCamera();

        // Misc (re-usable)
        RTHandle m_TempTexture1024; // RGHalf
        RTHandle m_TempTexture32;   // RGHalf

        // HDRP has the following behavior regarding alpha:
        // - If post processing is disabled, the alpha channel of the rendering passes (if any) will be passed to the frame buffer by the final pass
        // - If post processing is enabled, then post processing passes will either copy (exposure, color grading, etc) or process (DoF, TAA, etc) the alpha channel, if one exists.
        // If the user explicitly requests a color buffer without alpha for post-processing (for performance reasons) but the rendering passes have alpha, then the alpha will be copied.
        readonly bool m_EnableAlpha;
        readonly bool m_KeepAlpha; 
        RTHandle m_AlphaTexture; // RHalf

        readonly TargetPool m_Pool;

        readonly bool m_UseSafePath;
        bool m_PostProcessEnabled;
        bool m_AnimatedMaterialsEnabled;

        bool m_MotionBlurSupportsScattering;

        // Max guard band size is assumed to be 8 pixels
        const int k_RTGuardBandSize = 4;

        // Uber feature map to workaround the lack of multi_compile in compute shaders
        readonly Dictionary<int, string> m_UberPostFeatureMap = new Dictionary<int, string>();

        readonly System.Random m_Random;

        HDRenderPipeline m_HDInstance;

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

            // Project-wise LUT size for all grading operations - meaning that internal LUTs and
            // user-provided LUTs will have to be this size
            var settings = hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings;
            m_LutSize = settings.lutSize;
            var lutFormat = (GraphicsFormat)settings.lutFormat;

            // Feature maps
            // Must be kept in sync with variants defined in UberPost.compute
            PushUberFeature(UberPostFeatureFlags.None);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration);
            PushUberFeature(UberPostFeatureFlags.Vignette);
            PushUberFeature(UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion);
            PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion);

            //Alpha mask variants:
            {
                PushUberFeature(UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.Vignette | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.LensDistortion | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.LensDistortion | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion | UberPostFeatureFlags.EnableAlpha);
                PushUberFeature(UberPostFeatureFlags.ChromaticAberration | UberPostFeatureFlags.Vignette | UberPostFeatureFlags.LensDistortion | UberPostFeatureFlags.EnableAlpha);
            }

            // Grading specific
            m_HableCurve = new HableCurve();
            m_InternalLogLut = RTHandles.Alloc(
                name: "Color Grading Log Lut",
                dimension: TextureDimension.Tex3D,
                width: m_LutSize,
                height: m_LutSize,
                slices: m_LutSize,
                depthBufferBits: DepthBits.None,
                colorFormat: lutFormat,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                anisoLevel: 0,
                useMipMap: false,
                enableRandomWrite: true
            );

            // Setup a default exposure textures and clear it to neutral values so that the exposure
            // multiplier is 1 and thus has no effect
            // Beware that 0 in EV100 maps to a multiplier of 0.833 so the EV100 value in this
            // neutral exposure texture isn't 0
            m_EmptyExposureTexture = RTHandles.Alloc(1, 1, colorFormat: k_ExposureFormat,
                enableRandomWrite: true, name: "Empty EV100 Exposure"
            );

            m_MotionBlurSupportsScattering = SystemInfo.IsFormatSupported(GraphicsFormat.R32_UInt, FormatUsage.LoadStore) && SystemInfo.IsFormatSupported(GraphicsFormat.R16_UInt, FormatUsage.LoadStore);
            // TODO: Remove this line when atomic bug in HLSLcc is fixed.
            m_MotionBlurSupportsScattering = m_MotionBlurSupportsScattering && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan);
            // TODO: Write a version that uses structured buffer instead of texture to do atomic as Metal doesn't support atomics on textures.
            m_MotionBlurSupportsScattering = m_MotionBlurSupportsScattering && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal);

            var tex = new Texture2D(1, 1, TextureFormat.RGHalf, false, true);
            tex.SetPixel(0, 0, new Color(1f, ColorUtils.ConvertExposureToEV100(1f), 0f, 0f));
            tex.Apply();
            Graphics.Blit(tex, m_EmptyExposureTexture);
            CoreUtils.Destroy(tex);

            // Initialize our target pool to ease RT management
            m_Pool = new TargetPool();

            // Use a custom RNG, we don't want to mess with the Unity one that the users might be
            // relying on (breaks determinism in their code)
            m_Random = new System.Random();

            // Misc targets
            m_TempTexture1024 = RTHandles.Alloc(
                1024, 1024, colorFormat: GraphicsFormat.R16G16_SFloat,
                enableRandomWrite: true, name: "Average Luminance Temp 1024"
            );

            m_TempTexture32 = RTHandles.Alloc(
                32, 32, colorFormat: GraphicsFormat.R16G16_SFloat,
                enableRandomWrite: true, name: "Average Luminance Temp 32"
            );
   
            m_ColorFormat = (GraphicsFormat)hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings.bufferFormat;
            m_KeepAlpha = false;

            // if both rendering and post-processing support an alpha channel, then post-processing will process (or copy) the alpha
            m_EnableAlpha = hdAsset.currentPlatformRenderPipelineSettings.supportsAlpha && hdAsset.currentPlatformRenderPipelineSettings.postProcessSettings.supportsAlpha;

            if (m_EnableAlpha == false)
            {
                // if only rendering has an alpha channel (and not post-processing), then we just copy the alpha to the output (but we don't process it).
                m_KeepAlpha = hdAsset.currentPlatformRenderPipelineSettings.supportsAlpha;
            }

            if (m_KeepAlpha)
            {
                m_AlphaTexture = RTHandles.Alloc(
                   Vector2.one, slices: TextureXR.slices, dimension: TextureXR.dimension,
                   colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, name: "Alpha Channel Copy"
               );
            }
        }

        public void Cleanup()
        {
            m_Pool.Cleanup();

            RTHandles.Release(m_EmptyExposureTexture);
            RTHandles.Release(m_TempTexture1024);
            RTHandles.Release(m_TempTexture32);
            RTHandles.Release(m_AlphaTexture);
            CoreUtils.Destroy(m_ExposureCurveTexture);
            CoreUtils.Destroy(m_InternalSpectralLut);
            RTHandles.Release(m_InternalLogLut);
            CoreUtils.Destroy(m_FinalPassMaterial);
            CoreUtils.Destroy(m_ClearBlackMaterial);
            CoreUtils.SafeRelease(m_BokehNearKernel);
            CoreUtils.SafeRelease(m_BokehFarKernel);
            CoreUtils.SafeRelease(m_BokehIndirectCmd);
            CoreUtils.SafeRelease(m_NearBokehTileList);
            CoreUtils.SafeRelease(m_FarBokehTileList);
            CoreUtils.SafeRelease(m_ContrastAdaptiveSharpen);

            m_EmptyExposureTexture      = null;
            m_TempTexture1024           = null;
            m_TempTexture32             = null;
            m_AlphaTexture              = null;
            m_ExposureCurveTexture      = null;
            m_InternalSpectralLut       = null;
            m_InternalLogLut            = null;
            m_FinalPassMaterial         = null;
            m_ClearBlackMaterial        = null;
            m_BokehNearKernel           = null;
            m_BokehFarKernel            = null;
            m_BokehIndirectCmd          = null;
            m_NearBokehTileList         = null;
            m_FarBokehTileList          = null;
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

            // Handle fixed exposure & disabled pre-exposure by forcing an exposure multiplier of 1
            if (!m_ExposureControlFS)
            {
                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, m_EmptyExposureTexture);
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, m_EmptyExposureTexture);
            }
            else
            {
                if (IsExposureFixed())
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FixedExposure)))
                    {
                        DoFixedExposure(cmd, camera);
                    }
                }

                cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(camera));
                cmd.SetGlobalTexture(HDShaderIDs._PrevExposureTexture, GetPreviousExposureTexture(camera));
            }
        }

        void PoolSourceGuard(ref RTHandle src, RTHandle dst, RTHandle colorBuffer)
        {
            // Special case to handle the source buffer, we only want to send it back to our
            // target pool if it's not the input color buffer
            if (src != colorBuffer) m_Pool.Recycle(src);
            src = dst;
        }

        public void Render(CommandBuffer cmd, HDCamera camera, BlueNoise blueNoise, RTHandle colorBuffer, RTHandle afterPostProcessTexture, RTHandle lightingBuffer, RenderTargetIdentifier finalRT, RTHandle depthBuffer, bool flipY)
        {
            var dynResHandler = DynamicResolutionHandler.instance;

            m_Pool.SetHWDynamicResolutionState(camera);

            void PoolSource(ref RTHandle src, RTHandle dst)
            {
                PoolSourceGuard(ref src, dst, colorBuffer);
            }

            bool isSceneView = camera.camera.cameraType == CameraType.SceneView;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PostProcessing)))
            {
            	// Save the alpha and apply it back into the final pass if rendering in fp16 and post-processing in r11g11b10
                if(m_KeepAlpha)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.AlphaCopy)))
                    {
                        DoCopyAlpha(cmd, camera, colorBuffer);
                    }
                }
                var source = colorBuffer;

                if (m_PostProcessEnabled)
                {
                    // Guard bands (also known as "horrible hack") to avoid bleeding previous RTHandle
                    // content into smaller viewports with some effects like Bloom that rely on bilinear
                    // filtering and can't use clamp sampler and the likes
                    // Note: some platforms can't clear a partial render target so we directly draw black triangles
                    {
                        int w = camera.actualWidth;
                        int h = camera.actualHeight;
                        cmd.SetRenderTarget(source, 0, CubemapFace.Unknown, -1);

                        if (w < source.rt.width || h < source.rt.height)
                        {
                            cmd.SetViewport(new Rect(w, 0, k_RTGuardBandSize, h));
                            cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
                            cmd.SetViewport(new Rect(0, h, w + k_RTGuardBandSize, k_RTGuardBandSize));
                            cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
                        }
                    }

                    // Optional NaN killer before post-processing kicks in
                    bool stopNaNs = camera.stopNaNs && m_StopNaNFS;

                #if UNITY_EDITOR
                    if (isSceneView)
                        stopNaNs = HDRenderPipelinePreferences.sceneViewStopNaNs;
                #endif

                    if (stopNaNs)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.StopNaNs)))
                        {
                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                            DoStopNaNs(cmd, camera, source, destination);
                            PoolSource(ref source, destination);
                        }
                    }
                }

                // Dynamic exposure - will be applied in the next frame
                // Not considered as a post-process so it's not affected by its enabled state
                if (!IsExposureFixed() && m_ExposureControlFS)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DynamicExposure)))
                    {
                        DoDynamicExposure(cmd, camera, source, lightingBuffer);

                        // On reset history we need to apply dynamic exposure immediately to avoid
                        // white or black screen flashes when the current exposure isn't anywhere
                        // near 0
                        if (camera.resetPostProcessingHistory)
                        {
                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);

                            var cs = m_Resources.shaders.applyExposureCS;
                            int kernel = cs.FindKernel("KMain");

                            // Note: we call GetPrevious instead of GetCurrent because the textures
                            // are swapped internally as the system expects the texture will be used
                            // on the next frame. So the actual "current" for this frame is in
                            // "previous".
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureTexture, GetPreviousExposureTexture(camera));
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

                            PoolSource(ref source, destination);
                    }
                }
                }

                if (m_PostProcessEnabled)
                {
                    // Temporal anti-aliasing goes first
                    bool taaEnabled = false;

                    if (m_AntialiasingFS)
                    {
                        taaEnabled = camera.antialiasing == AntialiasingMode.TemporalAntialiasing;

                        if (taaEnabled)
                        {
                            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
                            {
                                var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                                DoTemporalAntialiasing(cmd, camera, source, destination, depthBuffer);
                                PoolSource(ref source, destination);
                            }
                        }
                        else if (camera.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                        {
                            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SMAA)))
                            {
                                var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                                DoSMAA(cmd, camera, source, destination, depthBuffer);
                                PoolSource(ref source, destination);
                            }
                        }
                    }

                    if (camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessBeforePP)))
                        {
                            foreach (var typeString in HDRenderPipeline.defaultAsset.beforePostProcessCustomPostProcesses)
                                RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));
                        }
                    }

                    // Depth of Field is done right after TAA as it's easier to just re-project the CoC
                    // map rather than having to deal with all the implications of doing it before TAA
                    if (m_DepthOfField.IsActive() && !isSceneView && m_DepthOfFieldFS)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfField)))
                        {
                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                            DoDepthOfField(cmd, camera, source, destination, taaEnabled);
                            PoolSource(ref source, destination);
                        }
                    }

                    // Motion blur after depth of field for aesthetic reasons (better to see motion
                    // blurred bokeh rather than out of focus motion blur)
                    if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !camera.resetPostProcessingHistory && m_MotionBlurFS)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                        {
                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                            DoMotionBlur(cmd, camera, source, destination);
                            PoolSource(ref source, destination);
                        }
                    }

                    // Panini projection is done as a fullscreen pass after all depth-based effects are
                    // done and before bloom kicks in
                    // This is one effect that would benefit from an overscan mode or supersampling in
                    // HDRP to reduce the amount of resolution lost at the center of the screen
                    if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PaniniProjection)))
                        {
                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                            DoPaniniProjection(cmd, camera, source, destination);
                            PoolSource(ref source, destination);
                        }
                    }

                    // Combined post-processing stack - always runs if postfx is enabled
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UberPost)))
                    {
                        // Feature flags are passed to all effects and it's their responsibility to check
                        // if they are used or not so they can set default values if needed
                        var cs = m_Resources.shaders.uberPostCS;
                        var featureFlags = GetUberFeatureFlags(isSceneView);
                        int kernel = GetUberKernel(cs, featureFlags);

                        // Generate the bloom texture
                        bool bloomActive = m_Bloom.IsActive() && m_BloomFS;

                        if (bloomActive)
                        {
                            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.Bloom)))
                            {
                                DoBloom(cmd, camera, source, cs, kernel);
                            }
                        }
                        else
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._BloomTexture, TextureXR.GetBlackTexture());
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._BloomDirtTexture, Texture2D.blackTexture);
                            cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomParams, Vector4.zero);
                        }

                        // Build the color grading lut
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ColorGradingLUTBuilder)))
                        {
                            DoColorGrading(cmd, cs, kernel);
                        }

                        // Setup the rest of the effects
                        DoLensDistortion(cmd, cs, kernel, featureFlags);
                        DoChromaticAberration(cmd, cs, kernel, featureFlags);
                        DoVignette(cmd, cs, kernel, featureFlags);

                        // Run
                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);

                        bool outputColorLog = m_HDInstance.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ColorLog;
                        cmd.SetComputeVectorParam(cs, "_DebugFlags", new Vector4(outputColorLog ? 1 : 0, 0, 0, 0));
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                        cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
                        m_HDInstance.PushFullScreenDebugTexture(camera, cmd, destination, FullScreenDebugMode.ColorLog);

                        // Cleanup
                        if (bloomActive) m_Pool.Recycle(m_BloomTexture);
                        m_BloomTexture = null;

                        PoolSource(ref source, destination);
                    }

                    if (camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterPP)))
                        {
                            foreach (var typeString in HDRenderPipeline.defaultAsset.afterPostProcessCustomPostProcesses)
                                RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));
                        }
                    }
                }

                if (dynResHandler.DynamicResolutionEnabled() &&     // Dynamic resolution is on.
                    camera.antialiasing == AntialiasingMode.FastApproximateAntialiasing &&
                    m_AntialiasingFS)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FXAA)))
                    {
                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                        DoFXAA(cmd, camera, source, destination);
                        PoolSource(ref source, destination);
                    }
                }

                // Contrast Adaptive Sharpen Upscaling
                if (dynResHandler.DynamicResolutionEnabled() &&
                    dynResHandler.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                    {
                        var destination = m_Pool.Get(Vector2.one , m_ColorFormat);
                        
                        var cs = m_Resources.shaders.contrastAdaptiveSharpenCS;
                        int kInit = cs.FindKernel("KInitialize");
                        int kMain = cs.FindKernel("KMain");
                        if (kInit >= 0 && kMain >= 0)
                        {
                            cmd.SetComputeFloatParam(cs, HDShaderIDs._Sharpness, 1);
                            cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._InputTexture, source);
                            cmd.SetComputeVectorParam(cs, HDShaderIDs._InputTextureDimensions, new Vector4(source.rt.width,source.rt.height));
                            cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._OutputTexture, destination);
                            cmd.SetComputeVectorParam(cs, HDShaderIDs._OutputTextureDimensions, new Vector4(destination.rt.width, destination.rt.height));

                            ValidateComputeBuffer(ref m_ContrastAdaptiveSharpen, 2, sizeof(uint) * 4);

                            cmd.SetComputeBufferParam(cs, kInit, "CasParameters", m_ContrastAdaptiveSharpen);
                            cmd.SetComputeBufferParam(cs, kMain, "CasParameters", m_ContrastAdaptiveSharpen);

                            cmd.DispatchCompute(cs, kInit, 1, 1, 1);

                            int dispatchX = (int)System.Math.Ceiling(destination.rt.width / 16.0f);
                            int dispatchY = (int)System.Math.Ceiling(destination.rt.height / 16.0f);

                            cmd.DispatchCompute(cs, kMain, dispatchX, dispatchY, camera.viewCount);
                        }

                        PoolSource(ref source, destination);
                    }
                }

                // Final pass
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FinalPost)))
                {
                    DoFinalPass(cmd, camera, blueNoise, source, afterPostProcessTexture, finalRT, flipY);
                    PoolSource(ref source, null);
                }
            }

            camera.resetPostProcessingHistory = false;
        }

        void PushUberFeature(UberPostFeatureFlags flags)
        {
            // Use an int for the key instead of the enum itself to avoid GC pressure due to the
            // lack of a default comparer
            int iflags = (int)flags;
            m_UberPostFeatureMap.Add(iflags, "KMain_Variant" + iflags);
        }

        int GetUberKernel(ComputeShader cs, UberPostFeatureFlags flags)
        {
            bool success = m_UberPostFeatureMap.TryGetValue((int)flags, out var kernelName);
            Assert.IsTrue(success);
            return cs.FindKernel(kernelName);
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

        void DoStopNaNs(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            var cs = m_Resources.shaders.nanKillerCS;
            int kernel = cs.FindKernel("KMain");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
        }

        #endregion

        #region Copy Alpha

        void DoCopyAlpha(CommandBuffer cmd, HDCamera camera, RTHandle source)
        {
            var cs = m_Resources.shaders.copyAlphaCS;
            int kernel = cs.FindKernel("KMain");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_AlphaTexture);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
        }

        #endregion

        #region Exposure

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsExposureFixed() => m_Exposure.mode.value == ExposureMode.Fixed || m_Exposure.mode.value == ExposureMode.UsePhysicalCamera;

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

        void DoFixedExposure(CommandBuffer cmd, HDCamera camera)
        {
            var cs = m_Resources.shaders.exposureCS;

            GrabExposureHistoryTextures(camera, out var prevExposure, out _);

            int kernel = 0;

            if (m_Exposure.mode.value == ExposureMode.Fixed)
            {
                kernel = cs.FindKernel("KFixedExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.fixedExposure.value, 0f, 0f, 0f));
            }
            else if (m_Exposure.mode == ExposureMode.UsePhysicalCamera)
            {
                kernel = cs.FindKernel("KManualCameraExposure");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation.value, m_PhysicalCamera.aperture, m_PhysicalCamera.shutterSpeed, m_PhysicalCamera.iso));
            }

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, prevExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        static void GrabExposureHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                // r: multiplier, g: EV100
                return rtHandleSystem.Alloc(1, 1, colorFormat: k_ExposureFormat,
                    enableRandomWrite: true, name: $"Exposure Texture ({id}) {frameIndex}"
                );
            }

            // We rely on the RT history system that comes with HDCamera, but because it is swapped
            // at the beginning of the frame and exposure is applied with a one-frame delay it means
            // that 'current' and 'previous' are swapped
            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Exposure)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Exposure, Allocator, 2);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
        }

        void PrepareExposureCurveData(AnimationCurve curve, out float min, out float max)
        {
            if (m_ExposureCurveTexture == null)
            {
                m_ExposureCurveTexture = new Texture2D(k_ExposureCurvePrecision, 1, TextureFormat.RHalf, false, true)
                {
                    name = "Exposure Curve",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

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
                    pixels[i] = new Color(curve.Evaluate(min + step * i), 0f, 0f, 0f);
            }

            m_ExposureCurveTexture.SetPixels(pixels);
            m_ExposureCurveTexture.Apply();
        }

        // TODO: Handle light buffer as a source for average luminance
        void DoDynamicExposure(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer, RTHandle lightingBuffer)
        {
            var cs = m_Resources.shaders.exposureCS;
            int kernel;

            GrabExposureHistoryTextures(camera, out var prevExposure, out var nextExposure);

            // Setup variants
            var adaptationMode = m_Exposure.adaptationMode.value;

            if (!Application.isPlaying || camera.resetPostProcessingHistory)
                adaptationMode = AdaptationMode.Fixed;

            if (camera.resetPostProcessingHistory)
            {
                kernel = cs.FindKernel("KReset");
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, prevExposure);
                cmd.DispatchCompute(cs, kernel, 1, 1, 1);
            }

            m_ExposureVariants[0] = 1; // (int)exposureSettings.luminanceSource.value;
            m_ExposureVariants[1] = (int)m_Exposure.meteringMode.value;
            m_ExposureVariants[2] = (int)adaptationMode;
            m_ExposureVariants[3] = 0;

            // Pre-pass
            //var sourceTex = exposureSettings.luminanceSource == LuminanceSource.LightingBuffer
            //    ? lightingBuffer
            //    : colorBuffer;
            var sourceTex = colorBuffer;

            kernel = cs.FindKernel("KPrePass");
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, sourceTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_TempTexture1024);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = cs.FindKernel("KReduction");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_TempTexture32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (m_Exposure.mode.value == ExposureMode.Automatic)
            {
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation.value, m_Exposure.limitMin.value, m_Exposure.limitMax.value, 0f));
                m_ExposureVariants[3] = 1;
            }
            else if (m_Exposure.mode.value == ExposureMode.CurveMapping)
            {
                PrepareExposureCurveData(m_Exposure.curveMap.value, out float min, out float max);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, m_ExposureCurveTexture);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, new Vector4(m_Exposure.compensation.value, min, max, 0f));
                m_ExposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, new Vector4(m_Exposure.adaptationSpeedLightToDark.value, m_Exposure.adaptationSpeedDarkToLight.value, 0f, 0f));
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, m_ExposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_TempTexture32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        #endregion

        #region Temporal Anti-aliasing

        void DoTemporalAntialiasing(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, RTHandle depthBuffer)
        {
            GrabTemporalAntialiasingHistoryTextures(camera, out var prevHistory, out var nextHistory);

            if (m_EnableAlpha)
            {
                m_TemporalAAMaterial.EnableKeyword("ENABLE_ALPHA");
            }

            if (camera.resetPostProcessingHistory)
            {
                m_TAAHistoryBlitPropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
                var rtScaleSource = source.rtHandleProperties.rtHandleScale;
                m_TAAHistoryBlitPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(rtScaleSource.x, rtScaleSource.y, 0.0f, 0.0f));
                m_TAAHistoryBlitPropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                HDUtils.DrawFullScreen(cmd, HDUtils.GetBlitMaterial(source.rt.dimension), prevHistory, m_TAAHistoryBlitPropertyBlock, 0);
                HDUtils.DrawFullScreen(cmd, HDUtils.GetBlitMaterial(source.rt.dimension), nextHistory, m_TAAHistoryBlitPropertyBlock, 0);
            }

            m_TAAPropertyBlock.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ExcludeFromTAA);
            m_TAAPropertyBlock.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ExcludeFromTAA);
            m_TAAPropertyBlock.SetVector(HDShaderIDs._RTHandleScaleHistory, camera.historyRTHandleProperties.rtHandleScale);
            m_TAAPropertyBlock.SetTexture(HDShaderIDs._InputTexture, source);
            m_TAAPropertyBlock.SetTexture(HDShaderIDs._InputHistoryTexture, prevHistory);

            CoreUtils.SetRenderTarget(cmd, destination, depthBuffer);
            cmd.SetRandomWriteTarget(1, nextHistory);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, destination.rtHandleProperties.rtHandleScale); // <- above blits might have changed the scale
            cmd.DrawProcedural(Matrix4x4.identity, m_TemporalAAMaterial, 0, MeshTopology.Triangles, 3, 1, m_TAAPropertyBlock);
            cmd.DrawProcedural(Matrix4x4.identity, m_TemporalAAMaterial, 1, MeshTopology.Triangles, 3, 1, m_TAAPropertyBlock);
            cmd.ClearRandomWriteTargets();
        }

        void GrabTemporalAntialiasingHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one, TextureXR.slices, DepthBits.None, dimension: TextureXR.dimension,
                    filterMode: FilterMode.Bilinear, colorFormat: m_ColorFormat,
                    enableRandomWrite: true, useDynamicScale: true, name: "TAA History"
                );
            }

            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing, Allocator, 2);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.TemporalAntialiasing);
        }

        #endregion

        #region Depth Of Field
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
        void DoDepthOfField(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, bool taaEnabled)
        {
            bool nearLayerActive = m_DepthOfField.IsNearLayerActive();
            bool farLayerActive = m_DepthOfField.IsFarLayerActive();

            Assert.IsTrue(nearLayerActive || farLayerActive);

            bool bothLayersActive = nearLayerActive && farLayerActive;
            bool useTiles = !camera.xr.singlePassEnabled;
            bool hqFiltering = m_DepthOfField.highQualityFiltering;

            const uint kIndirectNearOffset = 0u * sizeof(uint);
            const uint kIndirectFarOffset  = 3u * sizeof(uint);

            // -----------------------------------------------------------------------------
            // Data prep
            // The number of samples & max blur sizes are scaled according to the resolution, with a
            // base scale of 1.0 for 1080p output

            int bladeCount = m_PhysicalCamera.bladeCount;

            float rotation = (m_PhysicalCamera.aperture - HDPhysicalCamera.kMinAperture) / (HDPhysicalCamera.kMaxAperture - HDPhysicalCamera.kMinAperture);
            rotation *= (360f / bladeCount) * Mathf.Deg2Rad; // TODO: Crude approximation, make it correct

            float ngonFactor = 1f;
            if (m_PhysicalCamera.curvature.y - m_PhysicalCamera.curvature.x > 0f)
                ngonFactor = (m_PhysicalCamera.aperture - m_PhysicalCamera.curvature.x) / (m_PhysicalCamera.curvature.y - m_PhysicalCamera.curvature.x);

            ngonFactor = Mathf.Clamp01(ngonFactor);
            ngonFactor = Mathf.Lerp(ngonFactor, 0f, Mathf.Abs(m_PhysicalCamera.anamorphism));

            float anamorphism = m_PhysicalCamera.anamorphism / 4f;
            float barrelClipping = m_PhysicalCamera.barrelClipping / 3f;

            float scale = 1f / (float)m_DepthOfField.resolution;
            var screenScale = new Vector2(scale, scale);
            int targetWidth = Mathf.RoundToInt(camera.actualWidth * scale);
            int targetHeight = Mathf.RoundToInt(camera.actualHeight * scale);
            int threadGroup8X = (targetWidth + 7) / 8;
            int threadGroup8Y = (targetHeight + 7) / 8;

            cmd.SetGlobalVector(HDShaderIDs._TargetScale, new Vector4((float)m_DepthOfField.resolution, scale, 0f, 0f));

            float resolutionScale = (camera.actualHeight / 1080f) * (scale * 2f);
            int farSamples = Mathf.CeilToInt(m_DepthOfField.farSampleCount * resolutionScale);
            int nearSamples = Mathf.CeilToInt(m_DepthOfField.nearSampleCount * resolutionScale);
            // We want at least 3 samples for both far and near
            farSamples = Mathf.Max(3, farSamples);
            nearSamples = Mathf.Max(3, nearSamples);

            float farMaxBlur = m_DepthOfField.farMaxBlur * resolutionScale;
            float nearMaxBlur = m_DepthOfField.nearMaxBlur * resolutionScale;

            // If TAA is enabled we use the camera history system to grab CoC history textures, but
            // because these don't use the same RTHandle system as the global one we'll have a
            // different scale than _RTHandleScale so we need to handle our own
            var cocHistoryScale = RTHandles.rtHandleProperties.rtHandleScale;

            ComputeShader cs;
            int kernel;

            // -----------------------------------------------------------------------------
            // Temporary targets prep

            RTHandle pingNearRGB = null, pongNearRGB = null, nearCoC = null, nearAlpha = null,
                dilatedNearCoC = null, pingFarRGB = null, pongFarRGB = null, farCoC = null;

            if (nearLayerActive)
            {
                pingNearRGB = m_Pool.Get(screenScale, m_ColorFormat);
                pongNearRGB = m_Pool.Get(screenScale, m_ColorFormat);
                nearCoC = m_Pool.Get(screenScale, k_CoCFormat);
                nearAlpha = m_Pool.Get(screenScale, k_CoCFormat);
                dilatedNearCoC = m_Pool.Get(screenScale, k_CoCFormat);
            }

            if (farLayerActive)
            {
                pingFarRGB = m_Pool.Get(screenScale, m_ColorFormat, true);
                pongFarRGB = m_Pool.Get(screenScale, m_ColorFormat);
                farCoC = m_Pool.Get(screenScale, k_CoCFormat, true);
            }

            var fullresCoC = m_Pool.Get(Vector2.one, k_CoCFormat);

            // -----------------------------------------------------------------------------
            // Render logic

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldKernel)))
            {
                // -----------------------------------------------------------------------------
                // Pass: generate bokeh kernels
                // Given that we allow full customization of near & far planes we'll need a separate
                // kernel for each layer

                cs = m_Resources.shaders.depthOfFieldKernelCS;
                kernel = cs.FindKernel("KParametricBlurKernel");

                // Near samples
                if (nearLayerActive)
                {
                    ValidateComputeBuffer(ref m_BokehNearKernel, nearSamples * nearSamples, sizeof(uint));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(nearSamples, ngonFactor, bladeCount, rotation));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(anamorphism, 0f, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, m_BokehNearKernel);
                    cmd.DispatchCompute(cs, kernel, Mathf.CeilToInt((nearSamples * nearSamples) / 64f), 1, 1);
                }

                // Far samples
                if (farLayerActive)
                {
                    ValidateComputeBuffer(ref m_BokehFarKernel, farSamples * farSamples, sizeof(uint));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params1, new Vector4(farSamples, ngonFactor, bladeCount, rotation));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params2, new Vector4(anamorphism, 0f, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, m_BokehFarKernel);
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

                cs = m_Resources.shaders.depthOfFieldCoCCS;

                if (m_DepthOfField.focusMode.value == DepthOfFieldMode.UsePhysicalCamera)
                {
                    // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                    float F = camera.camera.focalLength / 1000f;
                    float A = camera.camera.focalLength / m_PhysicalCamera.aperture;
                    float P = m_DepthOfField.focusDistance.value;
                    float maxCoC = (A * F) / Mathf.Max((P - F), 1e-6f);

                    kernel = cs.FindKernel("KMainPhysical");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(P, maxCoC, 0f, 0f));
                }
                else // DepthOfFieldMode.Manual
                {
                    float nearEnd = m_DepthOfField.nearFocusEnd.value;
                    float nearStart = Mathf.Min(m_DepthOfField.nearFocusStart.value, nearEnd - 1e-5f);
                    float farStart = Mathf.Max(m_DepthOfField.farFocusStart.value, nearEnd);
                    float farEnd = Mathf.Max(m_DepthOfField.farFocusEnd.value, farStart + 1e-5f);

                    kernel = cs.FindKernel("KMainManual");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(nearStart, nearEnd, farStart, farEnd));
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, fullresCoC);
                cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

                // -----------------------------------------------------------------------------
                // Pass: re-project CoC if TAA is enabled

                if (taaEnabled)
                {
                    GrabCoCHistory(camera, out var prevCoCTex, out var nextCoCTex);
                    cocHistoryScale = new Vector2(camera.historyRTHandleProperties.rtHandleScale.z, camera.historyRTHandleProperties.rtHandleScale.w);

                    cs = m_Resources.shaders.depthOfFieldCoCReprojectCS;
                    kernel = cs.FindKernel("KMain");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(camera.resetPostProcessingHistory ? 0f : 0.91f, cocHistoryScale.x, cocHistoryScale.y, 0f));
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, fullresCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHistoryCoCTexture, prevCoCTex);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, nextCoCTex);
                    cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

                    // Cleanup the main CoC texture as we don't need it anymore and use the
                    // re-projected one instead for the following steps
                    m_Pool.Recycle(fullresCoC);
                    fullresCoC = nextCoCTex;
                }

                m_HDInstance.PushFullScreenDebugTexture(camera, cmd, fullresCoC, FullScreenDebugMode.DepthOfFieldCoc);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldPrefilter)))
            {
                // -----------------------------------------------------------------------------
                // Pass: downsample & prefilter CoC and layers
                // We only need to pre-multiply the CoC for the far layer; if only near is being
                // rendered we can use the downsampled color target as-is
                // TODO: We may want to add an anti-flicker here

                cs = m_Resources.shaders.depthOfFieldPrefilterCS;

                if(m_EnableAlpha)
                {
                    // kernels with alpha channel:
                    kernel = cs.FindKernel(m_DepthOfField.resolution == DepthOfFieldResolution.Full ?
                    (bothLayersActive ? "KMainNearFarFullResAlpha" : nearLayerActive ? "KMainNearFullResAlpha" : "KMainFarFullResAlpha") :
                    (bothLayersActive ? "KMainNearFarAlpha" : nearLayerActive ? "KMainNearAlpha" : "KMainFarAlpha"));
                }
                else
                {
                    // kernels without alpha channel:
                    kernel = cs.FindKernel(m_DepthOfField.resolution == DepthOfFieldResolution.Full ?
                    (bothLayersActive ? "KMainNearFarFullRes" : nearLayerActive ? "KMainNearFullRes" : "KMainFarFullRes") :
                    (bothLayersActive ? "KMainNearFar" : nearLayerActive ? "KMainNear" : "KMainFar"));
                }

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

                cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);
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

                    if (m_UseSafePath)
                    {
                        // The other compute fails hard on Intel because of texture format issues
                        cs = m_Resources.shaders.depthOfFieldMipSafeCS;
                        kernel = cs.FindKernel(m_EnableAlpha? "KMainAlpha" : "KMain");
                        var mipScale = scale;

                        for (int i = 0; i < 4; i++)
                        {
                            mipScale *= 0.5f;
                            var size = new Vector2Int(Mathf.RoundToInt(camera.actualWidth * mipScale), Mathf.RoundToInt(camera.actualHeight * mipScale));
                            var mip = m_Pool.Get(new Vector2(mipScale, mipScale), m_ColorFormat);

                            cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                            int gx = (size.x + 7) / 8;
                            int gy = (size.y + 7) / 8;

                            // Downsample
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, mip);
                            cmd.DispatchCompute(cs, kernel, gx, gy, camera.viewCount);

                            // Copy to mip
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, mip);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pingFarRGB, i + 1);
                            cmd.DispatchCompute(cs, kernel, gx, gy, camera.viewCount);

                            m_Pool.Recycle(mip);
                        }
                    }
                    else
                    {
                        cs = m_Resources.shaders.depthOfFieldMipCS;
                        kernel = cs.FindKernel(m_EnableAlpha ? "KMainColorAlpha": "KMainColor");
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB, 0);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip1, pingFarRGB, 1);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip2, pingFarRGB, 2);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip3, pingFarRGB, 3);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip4, pingFarRGB, 4);
                        cmd.DispatchCompute(cs, kernel, tx, ty, camera.viewCount);
                    }

                    cs = m_Resources.shaders.depthOfFieldMipCS;
                    kernel = cs.FindKernel("KMainCoC");
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, farCoC, 0);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip1, farCoC, 1);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip2, farCoC, 2);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip3, farCoC, 3);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputMip4, farCoC, 4);
                    cmd.DispatchCompute(cs, kernel, tx, ty, camera.viewCount);
                }
            }

            if (nearLayerActive)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldDilate)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: dilate the near CoC

                    cs = m_Resources.shaders.depthOfFieldDilateCS;
                    kernel = cs.FindKernel("KMain");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(targetWidth - 1, targetHeight - 1, 0f, 0f));

                    int passCount = Mathf.CeilToInt((nearMaxBlur + 2f) / 4f);

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, nearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, dilatedNearCoC);
                    cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);

                    if (passCount > 1)
                    {
                        // Ping-pong
                        var src = dilatedNearCoC;
                        var dst = m_Pool.Get(screenScale, k_CoCFormat);

                        for (int i = 1; i < passCount; i++)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, src);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputCoCTexture, dst);
                            cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);
                            CoreUtils.Swap(ref src, ref dst);
                        }

                        dilatedNearCoC = src;
                        m_Pool.Recycle(dst);
                    }
                }
            }

            if (useTiles)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldTileMax)))
                {
                    // -----------------------------------------------------------------------------
                    // Pass: tile-max classification
                    // Tile coordinates are stored as 16bit (good enough for resolutions up to 64K)

                    ValidateComputeBuffer(ref m_BokehIndirectCmd, 3 * 2, sizeof(uint), ComputeBufferType.IndirectArguments);
                    ValidateComputeBuffer(ref m_NearBokehTileList, threadGroup8X * threadGroup8Y, sizeof(uint), ComputeBufferType.Append);
                    ValidateComputeBuffer(ref m_FarBokehTileList, threadGroup8X * threadGroup8Y, sizeof(uint), ComputeBufferType.Append);
                    m_NearBokehTileList.SetCounterValue(0u);
                    m_FarBokehTileList.SetCounterValue(0u);

                    // Clear the indirect command buffer
                    cs = m_Resources.shaders.depthOfFieldTileMaxCS;
                    kernel = cs.FindKernel("KClear");
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, m_BokehIndirectCmd);
                    cmd.DispatchCompute(cs, kernel, 1, 1, 1);

                    // Build the tile list & indirect command buffer
                    kernel = cs.FindKernel(bothLayersActive ? "KMainNearFar" : nearLayerActive ? "KMainNear" : "KMainFar");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(targetWidth - 1, targetHeight - 1, 0f, 0f));
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, m_BokehIndirectCmd);

                    if (nearLayerActive)
                    {
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputNearCoCTexture, dilatedNearCoC);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._NearTileList, m_NearBokehTileList);
                    }

                    if (farLayerActive)
                    {
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputFarCoCTexture, farCoC);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._FarTileList, m_FarBokehTileList);
                    }

                    cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, 1);
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

                    cs = m_Resources.shaders.depthOfFieldGatherCS;
                    kernel = m_EnableAlpha ?
                        cs.FindKernel(useTiles ? "KMainFarTilesAlpha" : "KMainFarAlpha") :
                        cs.FindKernel(useTiles ? "KMainFarTiles" : "KMainFar");

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(farSamples, farSamples * farSamples, barrelClipping, farMaxBlur));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(targetWidth, targetHeight, 1f / targetWidth, 1f / targetHeight));
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingFarRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, farCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongFarRGB);
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, m_BokehFarKernel);

                    if (useTiles)
                    {
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._TileList, m_FarBokehTileList);
                        cmd.DispatchCompute(cs, kernel, m_BokehIndirectCmd, kIndirectFarOffset);
                    }
                    else
                    {
                        cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);
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
                        cs = m_Resources.shaders.depthOfFieldCombineCS;
                        kernel = cs.FindKernel(m_EnableAlpha ? "KMainPreCombineFarAlpha" : "KMainPreCombineFar");
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingNearRGB);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputFarTexture, pongFarRGB);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, farCoC);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongNearRGB);
                        cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);

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

                    cs = m_Resources.shaders.depthOfFieldGatherCS;
                    kernel = m_EnableAlpha ?
                        cs.FindKernel(useTiles ? "KMainNearTilesAlpha" : "KMainNearAlpha") :
                        cs.FindKernel(useTiles ? "KMainNearTiles" : "KMainNear");
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(nearSamples, nearSamples * nearSamples, barrelClipping, nearMaxBlur));
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(targetWidth, targetHeight, 1f / targetWidth, 1f / targetHeight));
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, pingNearRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputCoCTexture, nearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputDilatedCoCTexture, dilatedNearCoC);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, pongNearRGB);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputAlphaTexture, nearAlpha);
                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._BokehKernel, m_BokehNearKernel);

                    if (useTiles)
                    {
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._TileList, m_NearBokehTileList);
                        cmd.DispatchCompute(cs, kernel, m_BokehIndirectCmd, kIndirectNearOffset);
                    }
                    else
                    {
                        cmd.DispatchCompute(cs, kernel, threadGroup8X, threadGroup8Y, camera.viewCount);
                    }
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfFieldCombine)))
            {
                // -----------------------------------------------------------------------------
                // Pass: combine blurred layers with source color

                cs = m_Resources.shaders.depthOfFieldCombineCS;

                if(m_EnableAlpha)
                {
                    if (m_DepthOfField.resolution == DepthOfFieldResolution.Full)
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarFullResAlpha" : nearLayerActive ? "KMainNearFullResAlpha" : "KMainFarFullResAlpha");
                    else if (hqFiltering)
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarHighQAlpha" : nearLayerActive ? "KMainNearHighQAlpha" : "KMainFarHighQAlpha");
                    else
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarLowQAlpha" : nearLayerActive ? "KMainNearLowQAlpha" : "KMainFarLowQAlpha");
                }
                else
                {
                    if (m_DepthOfField.resolution == DepthOfFieldResolution.Full)
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarFullRes" : nearLayerActive ? "KMainNearFullRes" : "KMainFarFullRes");
                    else if (hqFiltering)
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarHighQ" : nearLayerActive ? "KMainNearHighQ" : "KMainFarHighQ");
                    else
                        kernel = cs.FindKernel(bothLayersActive ? "KMainNearFarLowQ" : nearLayerActive ? "KMainNearLowQ" : "KMainFarLowQ");
                }

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
                cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Cleanup

            if (farLayerActive)
            {
                m_Pool.Recycle(pingFarRGB);
                m_Pool.Recycle(pongFarRGB);
                m_Pool.Recycle(farCoC);
            }

            if (nearLayerActive)
            {
                m_Pool.Recycle(pingNearRGB);
                m_Pool.Recycle(pongNearRGB);
                m_Pool.Recycle(nearCoC);
                m_Pool.Recycle(nearAlpha);
                m_Pool.Recycle(dilatedNearCoC);
            }

            if (!taaEnabled)
                m_Pool.Recycle(fullresCoC); // Already cleaned up if TAA is enabled
        }

        static void GrabCoCHistory(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R16_SFloat,
                    dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, name: "CoC History"
                );
            }

            next = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC)
                ?? camera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC, Allocator, 2);
            previous = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.DepthOfFieldCoC);
        }

        #endregion

        #region Motion Blur

        void DoMotionBlur(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            // -----------------------------------------------------------------------------

            int tileSize = 32;

            if (m_MotionBlurSupportsScattering)
            {
                tileSize = 16;
            }

            int tileTexWidth = Mathf.CeilToInt(camera.actualWidth / tileSize);
            int tileTexHeight = Mathf.CeilToInt(camera.actualHeight / tileSize);
            Vector2 tileTexScale = new Vector2((float)tileTexWidth / camera.actualWidth, (float)tileTexHeight / camera.actualHeight);
            Vector4 tileTargetSize = new Vector4(tileTexWidth, tileTexHeight, 1.0f / tileTexWidth, 1.0f / tileTexHeight);

            RTHandle preppedMotionVec = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);
            RTHandle minMaxTileVel = m_Pool.Get(tileTexScale, GraphicsFormat.B10G11R11_UFloatPack32);
            RTHandle maxTileNeigbourhood = m_Pool.Get(tileTexScale, GraphicsFormat.B10G11R11_UFloatPack32);
            RTHandle tileToScatterMax = null;
            RTHandle tileToScatterMin = null;
            if (m_MotionBlurSupportsScattering)
            {
                tileToScatterMax = m_Pool.Get(tileTexScale, GraphicsFormat.R32_UInt);
                tileToScatterMin = m_Pool.Get(tileTexScale, GraphicsFormat.R16_SFloat);
            }

            float screenMagnitude = (new Vector2(camera.actualWidth, camera.actualHeight).magnitude);
            Vector4 motionBlurParams0 = new Vector4(
                screenMagnitude,
                screenMagnitude * screenMagnitude,
                m_MotionBlur.minimumVelocity.value,
                m_MotionBlur.minimumVelocity.value * m_MotionBlur.minimumVelocity.value
            );


            Vector4 motionBlurParams1 = new Vector4(
                m_MotionBlur.intensity.value,
                m_MotionBlur.maximumVelocity.value / screenMagnitude,
                0.25f, // min/max velocity ratio for high quality.
                m_MotionBlur.cameraRotationVelocityClamp.value
            );

            uint sampleCount = (uint)m_MotionBlur.sampleCount;
            Vector4 motionBlurParams2 = new Vector4(
                m_MotionBlurSupportsScattering ? (sampleCount + (sampleCount & 1)) : sampleCount,
                tileSize,
                m_MotionBlur.depthComparisonExtent.value,
                m_MotionBlur.cameraMotionBlur.value ? 0.0f : 1.0f
            );
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
                cs = m_Resources.shaders.motionBlurMotionVecPrepCS;
                kernel = cs.FindKernel("MotionVecPreppingCS");
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams2);

                cmd.SetComputeMatrixParam(cs, HDShaderIDs._PrevVPMatrixNoTranslation, camera.mainViewConstants.prevViewProjMatrixNoCameraTrans);

                threadGroupX = (camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }


            // -----------------------------------------------------------------------------
            // Generate MinMax motion vectors tiles

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurTileMinMax)))
            {
                // We store R11G11B10 with RG = Max vel and B = Min vel magnitude
                cs = m_Resources.shaders.motionBlurTileGenCS;
                if (m_MotionBlurSupportsScattering)
                {
                    kernel = cs.FindKernel("TileGenPass_Scattering");
                }
                else
                {
                    kernel = cs.FindKernel("TileGenPass");
                }
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams1);

                if (m_MotionBlurSupportsScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                }

                threadGroupX = (camera.actualWidth + (tileSize - 1)) / tileSize;
                threadGroupY = (camera.actualHeight + (tileSize - 1)) / tileSize;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Generate max tiles neigbhourhood

            using (new ProfilingScope(cmd, m_MotionBlurSupportsScattering ? ProfilingSampler.Get(HDProfileId.MotionBlurTileScattering) : ProfilingSampler.Get(HDProfileId.MotionBlurTileNeighbourhood)))
            {
                cs = m_Resources.shaders.motionBlurTileGenCS;
                if (m_MotionBlurSupportsScattering)
                {
                    kernel = cs.FindKernel("TileNeighbourhood_Scattering");
                }
                else
                {
                    kernel = cs.FindKernel("TileNeighbourhood");
                }
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                if (m_MotionBlurSupportsScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                }
                groupSizeX = 8;
                groupSizeY = 8;
                threadGroupX = (tileTexWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (tileTexHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Merge min/max info spreaded above.

            if (m_MotionBlurSupportsScattering)
            {
                kernel = cs.FindKernel("TileMinMaxMerge");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, tileToScatterMax);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, tileToScatterMin);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Blur kernel
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurKernel)))
            {
                cs = m_Resources.shaders.motionBlurCS;
                kernel = cs.FindKernel("MotionBlurCS");
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, maxTileNeigbourhood);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, motionBlurParams2);

                groupSizeX = 16;
                groupSizeY = 16;
                threadGroupX = (camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }


            // -----------------------------------------------------------------------------
            // Recycle RTs

            m_Pool.Recycle(minMaxTileVel);
            m_Pool.Recycle(maxTileNeigbourhood);
            m_Pool.Recycle(preppedMotionVec);
            if (m_MotionBlurSupportsScattering)
            {
                m_Pool.Recycle(tileToScatterMax);
                m_Pool.Recycle(tileToScatterMin);
            }
        }

        #endregion

        #region Panini Projection

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse!
        void DoPaniniProjection(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            var cs = m_Resources.shaders.paniniProjectionCS;
            int kernel = 1f - Mathf.Abs(paniniD) > float.Epsilon
                ? cs.FindKernel("KMainGeneric")
                : cs.FindKernel("KMainUnitDistance");

            cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS));
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
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

        // TODO: All of this could be simplified and made faster once we have the ability to bind mips as SRV
        unsafe void DoBloom(CommandBuffer cmd, HDCamera camera, RTHandle source, ComputeShader uberCS, int uberKernel)
        {
            var resolution = m_Bloom.resolution;
            var highQualityFiltering = m_Bloom.highQualityFiltering;
            float scaleW = 1f / ((int)resolution / 2f);
            float scaleH = 1f / ((int)resolution / 2f);

            // If the scene is less than 50% of 900p, then we operate on full res, since it's going to be cheap anyway and this might improve quality in challenging situations.
            // Also we switch to bilinear upsampling as it goes less wide than bicubic and due to our border/RTHandle handling, going wide on small resolution
            // where small mips have a strong influence, might result problematic.
            if (camera.actualWidth < 800 || camera.actualHeight < 450)
            {
                scaleW = 1.0f;
                scaleH = 1.0f;
                highQualityFiltering = false;
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
            int mipCount = Mathf.Clamp(iterations, 1, k_MaxBloomMipCount);
            var mipSizes = stackalloc Vector2Int[mipCount];

            // Thresholding
            // A value of 0 in the UI will keep energy conservation
            const float k_Softness = 0.5f;
            float lthresh = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = lthresh * k_Softness + 1e-5f;
            var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);

            // Prepare targets
            // We could have a single texture with mips but because we can't bind individual mips as
            // SRVs right now we have to ping-pong between buffers and make the code more
            // complicated than it should be
            for (int i = 0; i < mipCount; i++)
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

                mipSizes[i] = pixelSize;
                m_BloomMipsDown[i] = m_Pool.Get(scale, m_ColorFormat);
                m_BloomMipsUp[i] = m_Pool.Get(scale, m_ColorFormat);
            }

            // All the computes for this effect use the same group size so let's use a local
            // function to simplify dispatches
            // Make sure the thread group count is sufficient to draw the guard bands
            void DispatchWithGuardBands(ComputeShader shader, int kernelId, in Vector2Int size)
            {
                int w = size.x;
                int h = size.y;

                if (w < source.rt.width && w % 8 < k_RTGuardBandSize)
                    w += k_RTGuardBandSize;
                if (h < source.rt.height && h % 8 < k_RTGuardBandSize)
                    h += k_RTGuardBandSize;

                cmd.DispatchCompute(shader, kernelId, (w + 7) / 8, (h + 7) / 8, camera.viewCount);
            }

            // Pre-filtering
            ComputeShader cs;
            int kernel;

            {
                var size = mipSizes[0];
                cs = m_Resources.shaders.bloomPrefilterCS;
                kernel = cs.FindKernel("KMain");

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_BloomMipsUp[0]); // Use m_BloomMipsUp as temp target
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomThreshold, threshold);
                DispatchWithGuardBands(cs, kernel, size);

                cs = m_Resources.shaders.bloomBlurCS;
                kernel = cs.FindKernel("KMain"); // Only blur

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_BloomMipsUp[0]);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_BloomMipsDown[0]);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                DispatchWithGuardBands(cs, kernel, size);
            }

            // Blur pyramid
            kernel = cs.FindKernel("KMainDownsample");

            for (int i = 0; i < mipCount - 1; i++)
            {
                var src = m_BloomMipsDown[i];
                var dst = m_BloomMipsDown[i + 1];
                var size = mipSizes[i + 1];

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, src);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                DispatchWithGuardBands(cs, kernel, size);
            }

            // Upsample & combine
            cs = m_Resources.shaders.bloomUpsampleCS;
            kernel = cs.FindKernel(highQualityFiltering ? "KMainHighQ" : "KMainLowQ");

            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);

            for (int i = mipCount - 2; i >= 0; i--)
            {
                var low = (i == mipCount - 2) ? m_BloomMipsDown : m_BloomMipsUp;
                var srcLow = low[i + 1];
                var srcHigh = m_BloomMipsDown[i];
                var dst = m_BloomMipsUp[i];
                var highSize = mipSizes[i];
                var lowSize = mipSizes[i + 1];

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputLowTexture, srcLow);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHighTexture, srcHigh);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(scatter, 0f, 0f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomBicubicParams, new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
                DispatchWithGuardBands(cs, kernel, highSize);
            }

            // Cleanup
            for (int i = 0; i < mipCount; i++)
            {
                m_Pool.Recycle(m_BloomMipsDown[i]);
                if (i > 0) m_Pool.Recycle(m_BloomMipsUp[i]);
            }

            // Set uber data
            var bloomSize = mipSizes[0];
            m_BloomTexture = m_BloomMipsUp[0];

            float intensity = Mathf.Pow(2f, m_Bloom.intensity.value) - 1f; // Makes intensity easier to control
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            // Lens dirtiness
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
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

            cmd.SetComputeTextureParam(uberCS, uberKernel, HDShaderIDs._BloomTexture, m_BloomTexture);
            cmd.SetComputeTextureParam(uberCS, uberKernel, HDShaderIDs._BloomDirtTexture, dirtTexture);
            cmd.SetComputeVectorParam(uberCS, HDShaderIDs._BloomParams, new Vector4(intensity, dirtIntensity, 1f, dirtEnabled));
            cmd.SetComputeVectorParam(uberCS, HDShaderIDs._BloomTint, (Vector4)tint);
            cmd.SetComputeVectorParam(uberCS, HDShaderIDs._BloomBicubicParams, new Vector4(bloomSize.x, bloomSize.y, 1f / bloomSize.x, 1f / bloomSize.y));
            cmd.SetComputeVectorParam(uberCS, HDShaderIDs._BloomDirtScaleOffset, dirtTileOffset);
            cmd.SetComputeVectorParam(uberCS, HDShaderIDs._BloomThreshold, threshold);
        }

        #endregion

        #region Lens Distortion

        void DoLensDistortion(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.LensDistortion) != UberPostFeatureFlags.LensDistortion)
                return;

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

            cmd.SetComputeVectorParam(cs, HDShaderIDs._DistortionParams1, p1);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DistortionParams2, p2);
        }

        #endregion

        #region Chromatic Aberration

        void DoChromaticAberration(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;

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

                    m_InternalSpectralLut.SetPixels(new []
                    {
                        new Color(1f, 0f, 0f),
                        new Color(0f, 1f, 0f),
                        new Color(0f, 0f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }

            var settings = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples, 0f, 0f);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ChromaSpectralLut, spectralLut);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ChromaParams, settings);
        }

        #endregion

        #region Vignette

        void DoVignette(CommandBuffer cmd, ComputeShader cs, int kernel, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            if (m_Vignette.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams2, new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? 1f : 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, m_Vignette.color.value);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, Texture2D.blackTexture);
            }
            else // Masked
            {
                var color = m_Vignette.color.value;
                color.a = Mathf.Clamp01(m_Vignette.opacity.value);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteParams1, new Vector4(0f, 0f, 1f, 0f));
                cmd.SetComputeVectorParam(cs, HDShaderIDs._VignetteColor, color);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._VignetteMask, m_Vignette.mask.value);
            }
        }

        #endregion

        #region Color Grading

        // TODO: User lut support
        void DoColorGrading(CommandBuffer cmd, ComputeShader cs, int kernel)
        {
            // Prepare data
            var lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            var hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            var channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value   / 100f, m_ChannelMixer.redOutGreenIn.value   / 100f, m_ChannelMixer.redOutBlueIn.value   / 100f, 0f);
            var channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            var channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value  / 100f, m_ChannelMixer.blueOutGreenIn.value  / 100f, m_ChannelMixer.blueOutBlueIn.value  / 100f, 0f);

            ComputeShadowsMidtonesHighlights(out var shadows, out var midtones, out var highlights, out var shadowsHighlightsLimits);
            ComputeLiftGammaGain(out var lift, out var gamma, out var gain);
            ComputeSplitToning(out var splitShadows, out var splitHighlights);

            // Setup lut builder compute & grab the kernel we need
            var tonemappingMode = m_TonemappingFS ? m_Tonemapping.mode.value : TonemappingMode.None;
            var builderCS = m_Resources.shaders.lutBuilder3DCS;
            string kernelName = "KBuild_NoTonemap";

            if (m_Tonemapping.IsActive())
            {
                switch (tonemappingMode)
                {
                    case TonemappingMode.Neutral:  kernelName = "KBuild_NeutralTonemap"; break;
                    case TonemappingMode.ACES:     kernelName = "KBuild_AcesTonemap"; break;
                    case TonemappingMode.Custom:   kernelName = "KBuild_CustomTonemap"; break;
                    case TonemappingMode.External: kernelName = "KBuild_ExternalTonemap"; break;
                }
            }

            int builderKernel = builderCS.FindKernel(kernelName);

            // Fill-in constant buffers & textures
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._OutputTexture, m_InternalLogLut);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Size, new Vector4(m_LutSize, 1f / (m_LutSize - 1f), 0f, 0f));
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorBalance, lmsColorBalance);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorFilter, m_ColorAdjustments.colorFilter.value.linear);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerRed, channelMixerR);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerGreen, channelMixerG);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerBlue, channelMixerB);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._HueSatCon, hueSatCon);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Lift, lift);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gamma, gamma);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gain, gain);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Shadows, shadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Midtones, midtones);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Highlights, highlights);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShaHiLimits, shadowsHighlightsLimits);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitShadows, splitShadows);
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitHighlights, splitHighlights);

            // YRGB
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveMaster, m_Curves.master.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveRed, m_Curves.red.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveGreen, m_Curves.green.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveBlue, m_Curves.blue.value.GetTexture());

            // Secondary curves
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsHue, m_Curves.hueVsHue.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsSat, m_Curves.hueVsSat.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveLumVsSat, m_Curves.lumVsSat.value.GetTexture());
            cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveSatVsSat, m_Curves.satVsSat.value.GetTexture());

            // Artist-driven tonemap curve
            if (tonemappingMode == TonemappingMode.Custom)
            {
                m_HableCurve.Init(
                    m_Tonemapping.toeStrength.value,
                    m_Tonemapping.toeLength.value,
                    m_Tonemapping.shoulderStrength.value,
                    m_Tonemapping.shoulderLength.value,
                    m_Tonemapping.shoulderAngle.value,
                    m_Tonemapping.gamma.value
                );

                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._CustomToneCurve, m_HableCurve.uniforms.curve);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentA, m_HableCurve.uniforms.toeSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentB, m_HableCurve.uniforms.toeSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentA, m_HableCurve.uniforms.midSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentB, m_HableCurve.uniforms.midSegmentB);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentA, m_HableCurve.uniforms.shoSegmentA);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentB, m_HableCurve.uniforms.shoSegmentB);
            }
            else if (tonemappingMode == TonemappingMode.External)
            {
                cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._LogLut3D, m_Tonemapping.lutTexture.value);
                cmd.SetComputeVectorParam(builderCS, HDShaderIDs._LogLut3D_Params, new Vector4(1f / m_LutSize, m_LutSize - 1f, m_Tonemapping.lutContribution.value, 0f));
            }

            // Misc parameters
            cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Params, new Vector4(m_ColorGradingFS ? 1f : 0f, 0f, 0f, 0f));

            // Generate the lut
            // See the note about Metal & Intel in LutBuilder3D.compute
            builderCS.GetKernelThreadGroupSizes(builderKernel, out uint threadX, out uint threadY, out uint threadZ);
            cmd.DispatchCompute(builderCS, builderKernel,
                (int)((m_LutSize + threadX - 1u) / threadX),
                (int)((m_LutSize + threadY - 1u) / threadY),
                (int)((m_LutSize + threadZ - 1u) / threadZ)
            );

            // This should be EV100 instead of EV but given that EV100(0) isn't equal to 1, it means
            // we can't use 0 as the default neutral value which would be confusing to users
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);

            // Setup the uber shader
            var logLutSettings = new Vector4(1f / m_LutSize, m_LutSize - 1f, postExposureLinear, 0f);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._LogLut3D, m_InternalLogLut);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._LogLut3D_Params, logLutSettings);
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

        void DoFXAA(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            var cs = m_Resources.shaders.FXAACS;
            int kernel = cs.FindKernel("FXAA");
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
        }

        #endregion

        #region SMAA

        void DoSMAA(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, RTHandle depthBuffer)
        {
            RTHandle SMAAEdgeTex = m_Pool.Get(Vector2.one, GraphicsFormat.R8G8B8A8_UNorm);
            RTHandle SMAABlendTex = m_Pool.Get(Vector2.one, GraphicsFormat.R8G8B8A8_UNorm);

            // -----------------------------------------------------------------------------

            m_SMAAMaterial.SetVector(HDShaderIDs._SMAARTMetrics, new Vector4(1.0f / camera.actualWidth, 1.0f / camera.actualHeight, camera.actualWidth, camera.actualHeight));

            m_SMAAMaterial.SetTexture(HDShaderIDs._SMAAAreaTex, m_Resources.textures.SMAAAreaTex);
            m_SMAAMaterial.SetTexture(HDShaderIDs._SMAASearchTex, m_Resources.textures.SMAASearchTex);
            m_SMAAMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SMAA);
            m_SMAAMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SMAA);

            switch(camera.SMAAQuality)
            {
                case HDAdditionalCameraData.SMAAQualityLevel.Low:
                    m_SMAAMaterial.EnableKeyword("SMAA_PRESET_LOW");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                    m_SMAAMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.High:
                    m_SMAAMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
                default:
                    m_SMAAMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
            }

            // -----------------------------------------------------------------------------
            // Clear
            CoreUtils.SetRenderTarget(cmd, SMAAEdgeTex, ClearFlag.Color);
            CoreUtils.SetRenderTarget(cmd, SMAABlendTex, ClearFlag.Color);

            // -----------------------------------------------------------------------------
            // EdgeDetection stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, source);
            HDUtils.DrawFullScreen(cmd, m_SMAAMaterial, SMAAEdgeTex, depthBuffer, null, (int)SMAAStage.EdgeDetection);

            // -----------------------------------------------------------------------------
            // BlendWeights stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, SMAAEdgeTex);
            HDUtils.DrawFullScreen(cmd, m_SMAAMaterial, SMAABlendTex, depthBuffer, null, (int)SMAAStage.BlendWeights);

            // -----------------------------------------------------------------------------
            // NeighborhoodBlending stage
            cmd.SetGlobalTexture(HDShaderIDs._InputTexture, source);
            m_SMAAMaterial.SetTexture(HDShaderIDs._SMAABlendTex, SMAABlendTex);
            HDUtils.DrawFullScreen(cmd, m_SMAAMaterial, destination, null, (int)SMAAStage.NeighborhoodBlending);

            // -----------------------------------------------------------------------------
            m_Pool.Recycle(SMAAEdgeTex);
            m_Pool.Recycle(SMAABlendTex);
        }

        #endregion

        #region Final Pass

        void DoFinalPass(CommandBuffer cmd, HDCamera camera, BlueNoise blueNoise, RTHandle source, RTHandle afterPostProcessTexture, RenderTargetIdentifier destination, bool flipY)
        {
            // Final pass has to be done in a pixel shader as it will be the one writing straight
            // to the backbuffer eventually

            m_FinalPassMaterial.shaderKeywords = null;
            m_FinalPassMaterial.SetTexture(HDShaderIDs._InputTexture, source);

            var dynResHandler = DynamicResolutionHandler.instance;
            bool dynamicResIsOn = camera.isMainGameView && dynResHandler.DynamicResolutionEnabled();

            if (dynamicResIsOn)
            {
                switch (dynResHandler.filter)
                {
                    case DynamicResUpscaleFilter.Bilinear:
                        m_FinalPassMaterial.EnableKeyword("BILINEAR");
                        break;
                    case DynamicResUpscaleFilter.CatmullRom:
                        m_FinalPassMaterial.EnableKeyword("CATMULL_ROM_4");
                        break;
                    case DynamicResUpscaleFilter.Lanczos:
                        m_FinalPassMaterial.EnableKeyword("LANCZOS");
                        break;
                    case DynamicResUpscaleFilter.ContrastAdaptiveSharpen:
                        m_FinalPassMaterial.EnableKeyword("CONTRASTADAPTIVESHARPEN");
                        break;
                }
            }

            if (m_PostProcessEnabled)
            {
                if (camera.antialiasing == AntialiasingMode.FastApproximateAntialiasing && !dynamicResIsOn && m_AntialiasingFS)
                    m_FinalPassMaterial.EnableKeyword("FXAA");

                if (m_FilmGrain.IsActive() && m_FilmGrainFS)
                {
                    var texture = m_FilmGrain.texture.value;

                    if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                        texture = m_Resources.textures.filmGrainTex[(int)m_FilmGrain.type.value];

                    if (texture != null) // Fail safe if the resources asset breaks :/
                    {
                        #if HDRP_DEBUG_STATIC_POSTFX
                        int offsetX = 0;
                        int offsetY = 0;
                        #else
                        int offsetX = (int)(m_Random.NextDouble() * texture.width);
                        int offsetY = (int)(m_Random.NextDouble() * texture.height);
                        #endif

                        m_FinalPassMaterial.EnableKeyword("GRAIN");
                        m_FinalPassMaterial.SetTexture(HDShaderIDs._GrainTexture, texture);
                        m_FinalPassMaterial.SetVector(HDShaderIDs._GrainParams, new Vector2(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
                        m_FinalPassMaterial.SetVector(HDShaderIDs._GrainTextureParams, new Vector4(texture.width, texture.height, offsetX, offsetY));
                    }
                }

                if (camera.dithering && m_DitheringFS)
                {
                    var blueNoiseTexture = blueNoise.textureArray16L;

                    #if HDRP_DEBUG_STATIC_POSTFX
                    int textureId = 0;
                    #else
                    int textureId = Time.frameCount % blueNoiseTexture.depth;
                    #endif

                    m_FinalPassMaterial.EnableKeyword("DITHER");
                    m_FinalPassMaterial.SetTexture(HDShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                    m_FinalPassMaterial.SetVector(HDShaderIDs._DitherParams, new Vector3(blueNoiseTexture.width, blueNoiseTexture.height, textureId));
                }
            }

            if (m_KeepAlpha)
            {
                m_FinalPassMaterial.SetTexture(HDShaderIDs._AlphaTexture, m_AlphaTexture);
                m_FinalPassMaterial.SetFloat(HDShaderIDs._KeepAlpha, 1.0f);
            }
            else
            {
                m_FinalPassMaterial.SetTexture(HDShaderIDs._AlphaTexture, TextureXR.GetWhiteTexture());
                m_FinalPassMaterial.SetFloat(HDShaderIDs._KeepAlpha, 0.0f);
            }

            if (m_EnableAlpha)
            {
                m_FinalPassMaterial.EnableKeyword("ENABLE_ALPHA");
            }

            m_FinalPassMaterial.SetVector(HDShaderIDs._UVTransform,
                flipY
                ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f)
                : new Vector4(1.0f,  1.0f, 0.0f, 0.0f)
            );

            // Blit to backbuffer
            Rect backBufferRect = camera.finalViewport;

            // When post process is not the final pass, we render at (0,0) so that subsequent rendering does not have to bother about viewports.
            // Final viewport is handled in the final blit in this case
            if (!HDUtils.PostProcessIsFinalPass())
            {
                if (dynResHandler.HardwareDynamicResIsEnabled())
                {
                    var scaledSize = dynResHandler.GetLastScaledSize();
                    backBufferRect.width = scaledSize.x;
                    backBufferRect.height = scaledSize.y;
                }
                backBufferRect.x = backBufferRect.y = 0;
            }

            if (camera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
            {
                m_FinalPassMaterial.EnableKeyword("APPLY_AFTER_POST");
                m_FinalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, afterPostProcessTexture);
            }
            else
            {
                m_FinalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, TextureXR.GetBlackTexture());
            }

            HDUtils.DrawFullScreen(cmd, backBufferRect, m_FinalPassMaterial, destination);
        }

        #endregion

        #region User Post Processes

        internal void DoUserAfterOpaqueAndSky(CommandBuffer cmd, HDCamera camera, RTHandle colorBuffer)
        {
            if (!camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return;

            RTHandle source = colorBuffer;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterOpaqueAndSky)))
            {
                bool needsBlitToColorBuffer = false;
                foreach (var typeString in HDRenderPipeline.defaultAsset.beforeTransparentCustomPostProcesses)
                    needsBlitToColorBuffer |= RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));

                if (needsBlitToColorBuffer)
                {
                    Rect backBufferRect = camera.finalViewport;
                    HDUtils.BlitCameraTexture(cmd, source, colorBuffer);
                }
            }

            PoolSourceGuard(ref source, null, colorBuffer);
        }

        bool RenderCustomPostProcess(CommandBuffer cmd, HDCamera camera, ref RTHandle source, RTHandle colorBuffer, Type customPostProcessComponentType)
        {
            if (customPostProcessComponentType == null)
                return false;

            var stack = camera.volumeStack;

            if (stack.GetComponent(customPostProcessComponentType) is CustomPostProcessVolumeComponent customPP)
            {
                customPP.SetupIfNeeded();

                if (customPP is IPostProcessComponent pp && pp.IsActive())
                {
                    if (camera.camera.cameraType != CameraType.SceneView || customPP.visibleInSceneView)
                    {
                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                        CoreUtils.SetRenderTarget(cmd, destination);
                        {
                            cmd.BeginSample(customPP.name);
                            customPP.Render(cmd, camera, source, destination);
                            cmd.EndSample(customPP.name);
                        }
                        PoolSourceGuard(ref source, destination, colorBuffer);

                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Render Target Management Utilities

        // Quick utility class to manage temporary render targets for post-processing and keep the
        // code readable.
        class TargetPool
        {
            readonly Dictionary<int, Stack<RTHandle>> m_Targets;
            int m_Tracker;
            bool m_HasHWDynamicResolution;

            public TargetPool()
            {
                m_Targets = new Dictionary<int, Stack<RTHandle>>();
                m_Tracker = 0;
                m_HasHWDynamicResolution = false;
            }

            public void Cleanup()
            {
                foreach (var kvp in m_Targets)
                {
                    var stack = kvp.Value;

                    if (stack == null)
                        continue;

                    while (stack.Count > 0)
                        RTHandles.Release(stack.Pop());
                }

                m_Targets.Clear();
            }

            public void SetHWDynamicResolutionState(HDCamera camera)
            {
                bool needsHW = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
                if (m_Targets.Count > 0 && needsHW != m_HasHWDynamicResolution)
                {
                    // If any target has no dynamic resolution enabled, but we require it or vice versa, we need to cleanup the pool.
                    bool missDynamicScale = false;
                    foreach (var kvp in m_Targets)
                    {
                        var stack = kvp.Value;

                        if (stack == null)
                            continue;

                        // We found a RT with incorrect dynamic scale setting
                        if (stack.Count > 0 && (stack.Peek().rt.useDynamicScale != needsHW))
                        {
                            missDynamicScale = true;
                            break;
                        }
                    }

                    if (missDynamicScale)
                    {
                        Cleanup();
                    }
                    m_HasHWDynamicResolution = needsHW;
                }
            }

            public RTHandle Get(in Vector2 scaleFactor, GraphicsFormat format, bool mipmap = false)
            {
                var hashCode = ComputeHashCode(scaleFactor.x, scaleFactor.y, (int)format, mipmap);

                if (m_Targets.TryGetValue(hashCode, out var stack) && stack.Count > 0)
                    return stack.Pop();

                var rt = RTHandles.Alloc(
                    scaleFactor, TextureXR.slices, DepthBits.None, colorFormat: format, dimension: TextureXR.dimension,
                    useMipMap: mipmap, enableRandomWrite: true, useDynamicScale: true, name: "Post-processing Target Pool " + m_Tracker
                );

                m_Tracker++;
                return rt;
            }

            public void Recycle(RTHandle rt)
            {
                Assert.IsNotNull(rt);
                var hashCode = ComputeHashCode(rt.scaleFactor.x, rt.scaleFactor.y, (int)rt.rt.graphicsFormat, rt.rt.useMipMap);

                if (!m_Targets.TryGetValue(hashCode, out var stack))
                {
                    stack = new Stack<RTHandle>();
                    m_Targets.Add(hashCode, stack);
                }

                stack.Push(rt);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int ComputeHashCode(float scaleX, float scaleY, int format, bool mipmap)
            {
                int hashCode = 17;

                unchecked
                {
                    unsafe
                    {
                        hashCode = hashCode * 23 + *((int*)&scaleX);
                        hashCode = hashCode * 23 + *((int*)&scaleY);
                    }

                    hashCode = hashCode * 23 + format;
                    hashCode = hashCode * 23 + (mipmap ? 1 : 0);
                }

                return hashCode;
            }
        }

        #endregion
    }
}
