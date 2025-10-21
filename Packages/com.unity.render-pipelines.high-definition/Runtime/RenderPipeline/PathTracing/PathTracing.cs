using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

// Enable the denoising code path only on windows 64
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
using UnityEngine.Rendering.Denoising;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Options for sky importance sampling, in path tracing.
    /// </summary>
    public enum SkyImportanceSamplingMode
    {
        /// <summary>
        /// Enables importance sampling for HDRI skies only.
        /// </summary>
        HDRIOnly,

        /// <summary>
        /// Always enables sky importance sampling.
        /// </summary>
        On,

        /// <summary>
        /// Always disables sky importance sampling.
        /// </summary>
        Off
    }

    /// <summary>
    /// Options for noise index calculation per sample in path tracing.
    /// </summary>
    public enum SeedMode
    {

        /// <summary>
        /// The non repeating mode bases the seed on the camera frame count. This avoids screen-based artefacts when using Path Tracing with the Recorder package.
        /// </summary>
        NonRepeating,

        /// <summary>
        /// The repeating mode resets the seed to zero when the accumulation of samples resets. This allows for easier debugging through deterministic behavior per frame.
        /// </summary>
        Repeating,

        /// <summary>
        /// The custom mode allows you to choose the seed through a script by setting the customSeed parameter on the PathTracing volume override.
        /// </summary>
        Custom
    }

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    // For the HDRP path tracer we only enable a subset of the denoisers that are available in the denoising plugin

    /// <summary>
    /// Available denoiser types for the HDRP path tracer.
    /// </summary>
    public enum HDDenoiserType
    {
        /// <summary>
        /// Do not perform any denoising.
        /// </summary>
        None = DenoiserType.None,

        /// <summary>
        /// Use the NVIDIA Optix Denoiser back-end.
        /// </summary>
        [InspectorName("Intel Open Image Denoise")]
        OpenImageDenoise = DenoiserType.OpenImageDenoise,

        /// <summary>
        /// Use the Radeon Image Filter back-end.
        /// </summary>
        [InspectorName("NVIDIA Optix Denoiser")]
        Optix = DenoiserType.Optix
    }
#endif

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="SkyImportanceSamplingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class SkyImportanceSamplingParameter : VolumeParameter<SkyImportanceSamplingMode>
    {
        /// <summary>
        /// Creates a new <see cref="SkyImportanceSamplingParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public SkyImportanceSamplingParameter(SkyImportanceSamplingMode value, bool overrideState = false) : base(value, overrideState) { }
    }


    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="SeedMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class SeedModeParameter : VolumeParameter<SeedMode>
    {
        /// <summary>
        /// Creates a new <see cref="SeedModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public SeedModeParameter(SeedMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// Manages the settings for the Path Tracing effect.
    /// </summary>
    /// <remarks>
    /// Add a Path Tracing Volume Override to a Volume in your scene to enable and configure the path tracing effect.
    ///
    /// Enable path tracing in HDRP to interactively visualize highly accurate, physically-based lighting,
    /// reflections and global illumination during the scene design process. It's especially useful for tasks like pre-visualization,
    /// look development, or validating lighting setups in projects focused on high-quality visuals, such as architectural
    /// visualization, automotive design, or cinematic production. This feature is best suited for powerful hardware setups
    /// with GPUs that are compatible with DirectX Raytracing (DXR). Path tracing can be also used with the Unity Recorder to capture high fidelity animations.
    /// </remarks>
    /// <example>
    /// <para>The following example shows how to add or modify a Path Tracing Volume Override from a script.</para>
    /// <code source="../../../../../Documentation~/Path-Tracing-Example.cs"/>
    /// </example>
    [Serializable, VolumeComponentMenu("Ray Tracing/Path Tracing")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Ray-Tracing-Path-Tracing")]
    [DisplayInfo(name = "Path Tracing")]
    public sealed class PathTracing : VolumeComponent
    {
        /// <summary>
        /// Enables path tracing (thus disabling most other passes).
        /// </summary>
        [Tooltip("Enables path tracing (thus disabling most other passes).")]
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// Defines the layers that path tracing should include.
        /// </summary>
        [Tooltip("Defines the layers that path tracing should include.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Defines the maximum number of paths cast within each pixel, over time (one per frame).
        /// </summary>
        [Tooltip("Defines the maximum number of paths cast within each pixel, over time (one per frame).")]
        public ClampedIntParameter maximumSamples = new ClampedIntParameter(256, 1, 16384);

        /// <summary>
        /// Defines the minimum number of bounces for each path, in [1, 32].
        /// </summary>
        [Tooltip("Defines the minimum number of bounces for each path, in [1, 32].")]
        public ClampedIntParameter minimumDepth = new ClampedIntParameter(1, 1, 32);

        /// <summary>
        /// Defines the maximum number of bounces for each path, in [minimumDepth, 32].
        /// </summary>
        [Tooltip("Defines the maximum number of bounces for each path, in [minimumDepth, 32].")]
        public ClampedIntParameter maximumDepth = new ClampedIntParameter(4, 1, 32);

        /// <summary>
        /// Defines the maximum, post-exposed luminance computed for indirect path segments.
        /// </summary>
        [Tooltip("Defines the maximum, post-exposed luminance computed for indirect path segments. Lower values help prevent noise and fireflies (very bright pixels), but introduce bias by darkening the overall result. Increase this value if your image looks too dark.")]
        public MinFloatParameter maximumIntensity = new MinFloatParameter(10f, 0f);

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)

        /// <summary>
        /// Enables denoising for the converged path tracer frame
        /// </summary>
        [Tooltip("Enables denoising for the converged path tracer frame")]
        public DenoiserParameter denoising = new DenoiserParameter(HDDenoiserType.None);

        /// <summary>
        /// Improves the detail retention after denoising by using albedo and normal AOVs.
        /// </summary>
        [Tooltip("Improves the detail of the denoised image with its albedo and normal AOVs")]
        [InspectorName("Use AOVs")]
        public BoolParameter useAOVs = new BoolParameter(true);

        /// <summary>
        /// Enables temporally stable denoising when recording animation sequences (only affects recording / multi-frame accumulation when using the Optix denoiser)
        /// </summary>
        [Tooltip("Enables temporally-stable denoising when recording animation sequences (only affects recording / multi-frame accumulation)")]
        public BoolParameter temporal = new BoolParameter(false);

        /// <summary>
        /// Enables separate denoising of the volumetrics scattering results, but with extra GPU memory usage.
        /// </summary>
        [Tooltip("Enables the denoising of volumetric fog in a separate pass. This gives a smoother result at the expense of extra GPU memory usage. The extra pass does not take into account the temporal parameter of the denoiser.")]
        public BoolParameter separateVolumetrics = new BoolParameter(false);

        /// <summary>
        /// Controls whether denoising will be asynchronous (non-blocking) for the scene view camera.
        /// </summary>
        public BoolParameter asyncDenoising = new BoolParameter(true);
#endif

        /// <summary>
        /// Defines if and when sky importance sampling is enabled. It should be turned on for sky models with high contrast and bright spots, and turned off for smooth, uniform skies.
        /// </summary>
        [Tooltip("Defines if and when sky importance sampling is enabled. It should be turned on for sky models with high contrast and bright spots, and turned off for smooth, uniform skies.")]
        public SkyImportanceSamplingParameter skyImportanceSampling = new SkyImportanceSamplingParameter(SkyImportanceSamplingMode.HDRIOnly);

        /// <summary>
        /// Defines the number of tiles (X: width, Y: height) and the indices of the current tile (Z: i in [0, width[, W: j in [0, height[) for interleaved tiled rendering.
        /// </summary>
        [Tooltip("Defines the number of tiles (X: width, Y: height) and the indices of the current tile (Z: i in [0, width[, W: j in [0, height[) for interleaved tiled rendering.")]
        public Vector4Parameter tilingParameters = new Vector4Parameter(new Vector4(1, 1, 0, 0));


        /// <summary>
        /// Defines the mode used to calculate the noise index.
        /// </summary>
        [Tooltip("Defines the mode used to calculate the noise index used per path tracing sample.")]
        public SeedModeParameter seedMode = new SeedModeParameter(SeedMode.NonRepeating);

        /// <summary>
        /// Defines the noise index to be used in the custom SeedMode. This value should be set through a script and is ignored in other modes.
        /// </summary>
        [HideInInspector]
        public IntParameter customSeed = new IntParameter(0);
    }

    public partial class HDRenderPipeline
    {
        PathTracing m_PathTracingSettings = null;

#if UNITY_EDITOR
        uint  m_CacheMaxIteration = 0;

#endif // UNITY_EDITOR
        uint m_CacheLightCount = 0;
        int m_CameraID = 0;
        int m_SkyHash = -1;
        int m_DebugMaterialOverrideHash = -1;
        bool m_RenderSky = true;

        RTHandle m_FrameTextureRT;       // Stores the per-pixel results of path tracing for one frame
        RTHandle m_SkyBGTextureRT;       // Stores the sky background as seem from the camera
        RTHandle m_SkyCDFTextureRT;      // Stores latlon sky data (CDF) for importance sampling
        RTHandle m_SkyMarginalTextureRT; // Stores latlon sky data (Marginal) for importance sampling

        TextureHandle m_FrameTexture;
        TextureHandle m_SkyBGTexture;
        TextureHandle m_SkyCDFTexture;
        TextureHandle m_SkyMarginalTexture;

        int m_skySamplingSize;     // value used for the latlon sky texture (width = 2*size, height = size)

        List<Tuple<TextureHandle, HDCameraFrameHistoryType>> pathTracedAOVs;

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnUndoRecorded;
            Undo.undoRedoPerformed += OnSceneEdit;
            SceneView.duringSceneGui += OnSceneGui;
#endif // UNITY_EDITOR

            // Texture storing the result of one iteration (one per frame) of path tracing
            m_FrameTextureRT = RTHandles.Alloc(Vector2.one, format: GraphicsFormat.R32G32B32A32_SFloat, slices: TextureXR.slices,
                        dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, name: "PathTracingFrameBuffer");

            // Texture storing the sky background, matching the rasterization one
            m_SkyBGTextureRT = RTHandles.Alloc(Vector2.one, format: GraphicsFormat.R32G32B32A32_SFloat, slices: TextureXR.slices,
                        dimension: TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: false, name: "PathTracingSkyBackgroundBuffer");

            // Textures used to importance sample the sky (aka environment sampling)
            m_skySamplingSize = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize * 2;

            m_SkyCDFTextureRT = RTHandles.Alloc(m_skySamplingSize * 2, m_skySamplingSize, format: GraphicsFormat.R32_SFloat, slices: 1,
                        dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, name: "PathTracingSkySamplingBuffer1");

            m_SkyMarginalTextureRT = RTHandles.Alloc(m_skySamplingSize, 1, format: GraphicsFormat.R32_SFloat, slices: 1,
                        dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, name: "PathTracingSkySamplingBuffer2");

            pathTracedAOVs = new List<Tuple<TextureHandle, HDCameraFrameHistoryType>>(3);
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnUndoRecorded;
            Undo.undoRedoPerformed -= OnSceneEdit;
            SceneView.duringSceneGui -= OnSceneGui;
#endif // UNITY_EDITOR

            m_FrameTextureRT?.Release();
            m_SkyBGTextureRT?.Release();
            m_SkyCDFTextureRT?.Release();
            m_SkyMarginalTextureRT?.Release();

            m_FrameTexture = TextureHandle.nullHandle;
            m_SkyBGTexture = TextureHandle.nullHandle;
            m_SkyCDFTexture = TextureHandle.nullHandle;
            m_SkyMarginalTexture = TextureHandle.nullHandle;
        }

        void ImportPathTracingTargetsToRenderGraph()
        {
            if (!m_FrameTexture.IsValid())
                m_FrameTexture = m_RenderGraph.ImportTexture(m_FrameTextureRT);

            if (!m_SkyBGTexture.IsValid())
                m_SkyBGTexture = m_RenderGraph.ImportTexture(m_SkyBGTextureRT);

            if (!m_SkyCDFTexture.IsValid())
                m_SkyCDFTexture = m_RenderGraph.ImportTexture(m_SkyCDFTextureRT);

            if (!m_SkyMarginalTexture.IsValid())
                m_SkyMarginalTexture = m_RenderGraph.ImportTexture(m_SkyMarginalTextureRT);
        }

        /// <summary>
        /// Resets path tracing accumulation for all cameras.
        /// </summary>
        public void ResetPathTracing()
        {
            m_RenderSky = true;
            m_SubFrameManager.Reset();
        }

        /// <summary>
        /// Resets path tracing accumulation for a specific camera.
        /// </summary>
        /// <param name="hdCamera">Camera for which the accumulation is reset.</param>
        public void ResetPathTracing(HDCamera hdCamera)
        {
            int camID = hdCamera.camera.GetEntityId();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);
            ResetPathTracing(camID, camData);
        }

        internal CameraData ResetPathTracing(int camID, CameraData camData)
        {
            m_RenderSky = true;
            camData.ResetIteration();
            m_SubFrameManager.SetCameraData(camID, camData);

            return camData;
        }

        private Vector4 ComputeDoFConstants(HDCamera hdCamera, PathTracing settings)
        {
            var dofSettings = hdCamera.volumeStack.GetComponent<DepthOfField>();
            bool enableDof = (dofSettings.focusMode.value == DepthOfFieldMode.UsePhysicalCamera) && !(hdCamera.camera.cameraType == CameraType.SceneView);

            // focalLength is in mm, so we need to convert to meters. We also want the aperture radius, not diameter, so we divide by two.
            float apertureRadius = (enableDof && hdCamera.camera.aperture > 0) ? 0.5f * 0.001f * hdCamera.camera.focalLength / hdCamera.camera.aperture : 0.0f;

            float focusDistance = (dofSettings.focusDistanceMode.value == FocusDistanceMode.Volume) ? dofSettings.focusDistance.value : hdCamera.camera.focusDistance;

            return new Vector4(apertureRadius, focusDistance, 0.0f, 0.0f);
        }

        private bool IsSkySamplingEnabled(HDCamera hdCamera)
        {
            switch (m_PathTracingSettings.skyImportanceSampling.value)
            {
                case SkyImportanceSamplingMode.On:
                    return true;

                case SkyImportanceSamplingMode.Off:
                    return false;

                default: // HDRI Only
                    var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
                    return visualEnvironment.skyType.value == (int)SkyType.HDRI;
            }
        }

#if UNITY_EDITOR

        private void InitPathTracingSettingsCache()
        {
            m_CacheMaxIteration = (uint)m_PathTracingSettings.maximumSamples.value;
        }

        private void OnSceneEdit()
        {
            bool doPathTracingReset = true;

            // If we just change the sample count, we don't necessarily want to reset iteration
            if (m_PathTracingSettings && m_CacheMaxIteration != m_PathTracingSettings.maximumSamples.value)
            {
                m_RenderSky = true;
                m_CacheMaxIteration = (uint)m_PathTracingSettings.maximumSamples.value;
                m_SubFrameManager.SelectiveReset(m_CacheMaxIteration);

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                // We have to reset the status of any active denoisers so the denoiser will run again when we have max samples
                m_SubFrameManager.ResetDenoisingStatus();
#endif
                doPathTracingReset = false;
            }

            if (doPathTracingReset)
                ResetPathTracing();
        }

        private UndoPropertyModification[] OnUndoRecorded(UndoPropertyModification[] modifications)
        {
            OnSceneEdit();

            return modifications;
        }

        private void OnSceneGui(SceneView sv)
        {
            if (Event.current.type == EventType.MouseDrag)
                m_SubFrameManager.Reset(sv.camera.GetEntityId());
        }

#endif // UNITY_EDITOR

        private AccelerationStructureSize GetAccelerationStructureSize(HDCamera hdCamera)
        {
            AccelerationStructureSize accelSize;

            RayTracingAccelerationStructure accel = RequestAccelerationStructure(hdCamera);
            accelSize.memUsage = accel != null ? accel.GetSize() : 0;
            accelSize.instCount = accel != null ? accel.GetInstanceCount() : 0;

            return accelSize;
        }

        private CameraData CheckDirtiness(HDCamera hdCamera, int camID, CameraData camData)
        {
            bool isCameraDirty = false;
            // Check resolution dirtiness
            if (hdCamera.actualWidth != camData.width || hdCamera.actualHeight != camData.height)
            {
                camData.width = (uint)hdCamera.actualWidth;
                camData.height = (uint)hdCamera.actualHeight;
                isCameraDirty = true;
            }

            // Check sky dirtiness
            bool enabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
            if (enabled != camData.skyEnabled)
            {
                camData.skyEnabled = enabled;
                isCameraDirty = true;
            }

            // Check fog dirtiness
            enabled = Fog.IsFogEnabled(hdCamera);
            if (enabled != camData.fogEnabled)
            {
                camData.fogEnabled = enabled;
                isCameraDirty = true;
            }

            // Check acceleration structure dirtiness
            AccelerationStructureSize accelSize = GetAccelerationStructureSize(hdCamera);
            if (accelSize != camData.accelSize)
            {
                camData.accelSize = accelSize;
                isCameraDirty = true;
            }

            bool isSceneDirty = false;
            // Check materials dirtiness
            if (GetMaterialDirtiness(hdCamera))
            {
                ResetMaterialDirtiness(hdCamera);
                isSceneDirty = true;
            }

            // Check light or geometry transforms dirtiness
            if (GetTransformDirtiness(hdCamera))
            {
                ResetTransformDirtiness(hdCamera);
                isSceneDirty = true;
            }

            // Check debug material override dirtiness
            int debugMaterialOverrideHash = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.ComputeOverrideHash();
            if (debugMaterialOverrideHash != m_DebugMaterialOverrideHash)
            {
                m_DebugMaterialOverrideHash = debugMaterialOverrideHash;
                isSceneDirty = true;
            }

            // Check lights dirtiness
            if (m_CacheLightCount != m_WorldLights.totalLightCount)
            {
                m_CacheLightCount = (uint)m_WorldLights.totalLightCount;
                isSceneDirty = true;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                isCameraDirty = true;
            }

            // If nothing but the camera has changed, re-render the sky texture
            if (camID != m_CameraID)
            {
                m_RenderSky = true;
                m_CameraID = camID;
            }

            if (isSceneDirty)
            {
                ResetPathTracing();
                // Make sure to return the newly reset camera data
                return m_SubFrameManager.GetCameraData(camID);
            }

            if (isCameraDirty)
            {
                return ResetPathTracing(camID, camData);
            }

            return camData;
        }

        static RTHandle PathTracingHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_PathTracingHistoryBuffer{1}", viewName, frameIndex));
        }

        class RenderPathTracingData
        {
            public RayTracingShader shader;
            public CameraData cameraData;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRaytracingCB;
            public Color backgroundColor;
            public Matrix4x4 pixelCoordToViewDirWS;
            public Vector4 dofParameters;
            public Vector4 tilingParameters;
            public int width, height;
            public int skySize;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;

            public Texture skyReflection;
            public TextureHandle skyBG;
            public TextureHandle skyCDF;
            public TextureHandle skyMarginal;

            public TextureHandle output;

            public bool enableAOVs;
            public TextureHandle albedoAOV;
            public TextureHandle normalAOV;
            public TextureHandle motionVectorAOV;
            public bool enableVolumetricScattering;
            public TextureHandle volumetricScatteringAOV;

            public bool enableDecals;

#if ENABLE_SENSOR_SDK
            public Action<UnityEngine.Rendering.CommandBuffer> prepareDispatchRays;
#endif
        }

        void RenderPathTracingFrame(RenderGraph renderGraph, HDCamera hdCamera, in CameraData cameraData, TextureHandle pathTracingBuffer, TextureHandle albedo, TextureHandle normal, TextureHandle motionVector, TextureHandle volumetricScattering)
        {
            using (var builder = renderGraph.AddUnsafePass<RenderPathTracingData>("Render Path Tracing Frame", out var passData))
            {
#if ENABLE_SENSOR_SDK
                passData.shader = hdCamera.pathTracingShaderOverride ? hdCamera.pathTracingShaderOverride : rayTracingResources.pathTracingRT;
                passData.prepareDispatchRays = hdCamera.prepareDispatchRays;
#else
                passData.shader = rayTracingResources.pathTracingRT;
#endif
                passData.cameraData = cameraData;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet256SPP();
                passData.backgroundColor = hdCamera.backgroundColorHDR;
                passData.pixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                passData.dofParameters = ComputeDoFConstants(hdCamera, m_PathTracingSettings);
                passData.tilingParameters = m_PathTracingSettings.tilingParameters.value;
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.skySize = IsSkySamplingEnabled(hdCamera) ? m_skySamplingSize : 0;
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.lightCluster = RequestLightCluster();

                passData.shaderVariablesRaytracingCB = m_ShaderVariablesRayTracingCB;
                // This doesn't actually do anything in the path tracing shaders
                passData.shaderVariablesRaytracingCB._RaytracingNumSamples = (int)m_SubFrameManager.subFrameCount;
                passData.shaderVariablesRaytracingCB._RaytracingMinRecursion = m_PathTracingSettings.minimumDepth.value;
#if NO_RAY_RECURSION
                passData.shaderVariablesRaytracingCB._RaytracingMaxRecursion = 1;
#else
                passData.shaderVariablesRaytracingCB._RaytracingMaxRecursion = m_PathTracingSettings.maximumDepth.value;
#endif
                passData.shaderVariablesRaytracingCB._RaytracingIntensityClamp = m_PathTracingSettings.maximumIntensity.value;
                int seed = m_PathTracingSettings.seedMode == SeedMode.Repeating ? (int)cameraData.currentIteration : (((int)hdCamera.GetCameraFrameCount() - 1) % m_PathTracingSettings.maximumSamples.max);
                passData.shaderVariablesRaytracingCB._RaytracingSampleIndex = m_PathTracingSettings.seedMode == SeedMode.Custom ? m_PathTracingSettings.customSeed.value : seed;

                passData.skyReflection = m_SkyManager.GetSkyReflection(hdCamera);
                passData.skyBG = m_SkyBGTexture;
                builder.UseTexture(passData.skyBG, AccessFlags.Read);
                passData.skyCDF = m_SkyCDFTexture;
                builder.UseTexture(passData.skyCDF, AccessFlags.Read);
                passData.skyMarginal = m_SkyMarginalTexture;
                builder.UseTexture(passData.skyMarginal, AccessFlags.Read);

                passData.output = pathTracingBuffer;
                builder.UseTexture(passData.output, AccessFlags.Write);

                // AOVs
                passData.enableAOVs = albedo.IsValid() && normal.IsValid() && motionVector.IsValid();
                if (passData.enableAOVs)
                {
                    passData.albedoAOV = albedo;
                    builder.UseTexture(passData.albedoAOV, AccessFlags.Write);
                    passData.normalAOV = normal;
                    builder.UseTexture(passData.normalAOV, AccessFlags.Write);
                    passData.motionVectorAOV = motionVector;
                    builder.UseTexture(passData.motionVectorAOV, AccessFlags.Write);
                }
                passData.enableVolumetricScattering = volumetricScattering.IsValid();
                if (passData.enableVolumetricScattering)
                {
                    passData.volumetricScatteringAOV = volumetricScattering;
                    builder.UseTexture(passData.volumetricScatteringAOV, AccessFlags.Write);
                }

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (RenderPathTracingData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Define the shader pass to use for the path tracing pass
                        natCmd.SetRayTracingShaderPass(data.shader, "PathTracingDXR");

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.shader, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Update the global constant buffer
                        ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRaytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // LightLoop data
                        natCmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, data.lightCluster.GetCluster());

                        // Global sky data
                        natCmd.SetGlobalInt(HDShaderIDs._PathTracingCameraSkyEnabled, data.cameraData.skyEnabled ? 1 : 0);
                        natCmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureWidth, 2 * data.skySize);
                        natCmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureHeight, data.skySize);
                        natCmd.SetGlobalTexture(HDShaderIDs._SkyTexture, data.skyReflection);
                        natCmd.SetGlobalTexture(HDShaderIDs._SkyCameraTexture, data.skyBG);
                        natCmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyCDFTexture, data.skyCDF);
                        natCmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyMarginalTexture, data.skyMarginal);

                        // Further sky-related data for the ray miss
                        natCmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingCameraClearColor, data.backgroundColor);

                        // Data used in the camera rays generation
                        natCmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._FrameTexture, data.output);
                        natCmd.SetRayTracingMatrixParam(data.shader, HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDirWS);
                        natCmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingDoFParameters, data.dofParameters);
                        natCmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingTilingParameters, data.tilingParameters);


                        if (data.enableDecals)
                            DecalSystem.instance.SetAtlas(natCmd); // for clustered decals

#if ENABLE_SENSOR_SDK
                        // SensorSDK can do its own camera rays generation
                        data.prepareDispatchRays?.Invoke(natCmd);
#endif

                        // AOVs
                        if (data.enableAOVs)
                        {
                            natCmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._AlbedoAOV, data.albedoAOV);
                            natCmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._NormalAOV, data.normalAOV);
                            natCmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._MotionVectorAOV, data.motionVectorAOV);
                        }
                        if (data.enableVolumetricScattering)
                        {
                            natCmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._VolumetricScatteringAOV, data.volumetricScatteringAOV);
                        }

                        // Run the computation
                        var shaderName = data.enableAOVs ?
                                         (data.enableVolumetricScattering ? "RayGenVolScatteringAOV" : "RayGenAOV") :
                                         (data.enableVolumetricScattering ? "RayGenVolScattering" : "RayGen");
                        natCmd.DispatchRays(data.shader, shaderName, (uint)data.width, (uint)data.height, 1, null);
                    });
            }
        }

        // Simpler variant used by path tracing, without depth buffer or volumetric computations
        void RenderSkyBackground(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle skyBuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            // Override the exposure texture, as we need a neutral value for this render
            SetGlobalTexture(renderGraph, HDShaderIDs._ExposureTexture, m_EmptyExposureTexture);

            // Parts of sky rendering may access shadowmap-related uniforms.
            // In the full path tracing path, these uniforms won't actually be used,
            // but they still need to be populated with neutral values,
            // or we get errors about unpopulated uniforms.
            HDShadowManager.BindDefaultShadowGlobalResources(renderGraph);

            m_SkyManager.RenderSky(renderGraph, hdCamera, skyBuffer, CreateDepthBuffer(renderGraph, true, MSAASamples.None), "Render Sky Background for Path Tracing");

            // Restore the regular exposure texture
            SetGlobalTexture(renderGraph, HDShaderIDs._ExposureTexture, GetExposureTexture(hdCamera));
        }

        class RenderSkySamplingPassData
        {
            public ComputeShader shader;
            public int k0;
            public int k1;
            public int size;
            public TextureHandle outputCDF;
            public TextureHandle outputMarginal;
        }

        // Prepares data (CDF) to be able to importance sample the sky afterwards
        void RenderSkySamplingData(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (!rayTracingResources.pathTracingSkySamplingDataCS)
                return;

            using (var builder = renderGraph.AddUnsafePass<RenderSkySamplingPassData>("Render Sky Sampling Data for Path Tracing", out var passData))
            {
                passData.shader = rayTracingResources.pathTracingSkySamplingDataCS;
                passData.k0 = passData.shader.FindKernel("ComputeCDF");
                passData.k1 = passData.shader.FindKernel("ComputeMarginal");
                passData.size = m_skySamplingSize;
                passData.outputCDF = m_SkyCDFTexture;
                builder.UseTexture(passData.outputCDF, AccessFlags.Write);
                passData.outputMarginal = m_SkyMarginalTexture;
                builder.UseTexture(passData.outputMarginal, AccessFlags.Write);

                builder.SetRenderFunc(
                    (RenderSkySamplingPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        natCmd.SetComputeIntParam(data.shader, HDShaderIDs._PathTracingSkyTextureWidth, data.size * 2);
                        natCmd.SetComputeIntParam(data.shader, HDShaderIDs._PathTracingSkyTextureHeight, data.size);

                        natCmd.SetComputeTextureParam(data.shader, data.k0, HDShaderIDs._PathTracingSkyCDFTexture, data.outputCDF);
                        natCmd.SetComputeTextureParam(data.shader, data.k0, HDShaderIDs._PathTracingSkyMarginalTexture, data.outputMarginal);
                        natCmd.DispatchCompute(data.shader, data.k0, 1, data.size, 1);

                        natCmd.SetComputeTextureParam(data.shader, data.k1, HDShaderIDs._PathTracingSkyMarginalTexture, data.outputMarginal);
                        natCmd.DispatchCompute(data.shader, data.k1, 1, 1, 1);
                    });
            }
        }

        // This is the method to call from the main render loop
        TextureHandle RenderPathTracing(RenderGraph renderGraph, ScriptableRenderContext renderContext, HDCamera hdCamera, TextureHandle colorBuffer)
        {
#if UNITY_EDITOR
            if (m_PathTracingSettings == null)
            {
                m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();
                InitPathTracingSettingsCache();
            }
            else
#endif
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!rayTracingResources.pathTracingRT || !m_PathTracingSettings.enable.value)
                return TextureHandle.nullHandle;

            var motionVector = TextureHandle.nullHandle;
            var albedo = TextureHandle.nullHandle;
            var normal = TextureHandle.nullHandle;
            var volumetricScattering = TextureHandle.nullHandle;

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            bool needsAOVs = m_PathTracingSettings.denoising.value != HDDenoiserType.None && (m_PathTracingSettings.useAOVs.value || m_PathTracingSettings.temporal.value);
            bool needsVolumetricFogAOV = m_PathTracingSettings.denoising.value != HDDenoiserType.None && m_PathTracingSettings.separateVolumetrics.value;

            if (needsAOVs)
            {
                TextureDesc aovDesc = new TextureDesc(hdCamera.actualWidth, hdCamera.actualHeight, true, true)
                {
                    colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = true,
                    clearColor = Color.black,
                    enableRandomWrite = true,
                    useMipMap = false,
                    autoGenerateMips = false,
                    name = "Path traced AOV buffer"
                };
                motionVector = renderGraph.CreateTexture(aovDesc);
                albedo = renderGraph.CreateTexture(aovDesc);
                normal = renderGraph.CreateTexture(aovDesc);
            }

            if (needsVolumetricFogAOV)
            {
                TextureDesc aovDesc = new TextureDesc(hdCamera.actualWidth, hdCamera.actualHeight, true, true)
                {
                    colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
                    bindTextureMS = false,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = true,
                    clearColor = Color.black,
                    enableRandomWrite = true,
                    useMipMap = false,
                    autoGenerateMips = false,
                    name = "Path traced volumetrics AOV buffer"
                };
                volumetricScattering = renderGraph.CreateTexture(aovDesc);
            }
            pathTracedAOVs.Clear();
#endif

            int camID = hdCamera.camera.GetEntityId();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);

            ImportPathTracingTargetsToRenderGraph();

            // Set up the subframe manager for correct accumulation in case of multiframe accumulation
            // Check if the camera has a valid history buffer and if not reset the accumulation.
            // This can happen if a script disables and re-enables the camera (case 1337843).
            if (!hdCamera.isPersistent && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingOutput) == null)
                m_SubFrameManager.Reset(camID);

            if (!m_SubFrameManager.isRecording)
            {
                // Check if things have changed and if we need to restart the accumulation
                camData = CheckDirtiness(hdCamera, camID, camData);

                // If we are recording, the max iteration is set/overridden by the subframe manager, otherwise we read it from the path tracing volume
                m_SubFrameManager.subFrameCount = (uint)m_PathTracingSettings.maximumSamples.value;
            }
            else
            {
                // When recording, as we bypass dirtiness checks which update camData, we need to indicate whether we want to render a sky or not
                camData.skyEnabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
                m_SubFrameManager.SetCameraData(camID, camData);
            }

            if (!hdCamera.ActiveRayTracingAccumulation())
            {
                camData.ResetIteration();
                m_SubFrameManager.subFrameCount = 1;
            }

            if (camData.currentIteration < m_SubFrameManager.subFrameCount)
            {
                // Keep a sky texture around, that we compute only once per accumulation (except when recording, with potential camera motion blur)
                if (m_RenderSky || m_SubFrameManager.isRecording)
                {
                    RenderSkyBackground(m_RenderGraph, hdCamera, m_SkyBGTexture);
                    m_RenderSky = false;

                    if (IsSkySamplingEnabled(hdCamera) && m_SkyHash != hdCamera.lightingSky.skyParametersHash)
                    {
                        RenderSkySamplingData(m_RenderGraph, hdCamera);
                        m_SkyHash = hdCamera.lightingSky.skyParametersHash;
                    }
                }

                RenderPathTracingFrame(m_RenderGraph, hdCamera, camData, m_FrameTexture, albedo, normal, motionVector, volumetricScattering);

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                bool denoise = m_PathTracingSettings.denoising.value != HDDenoiserType.None;
                // Note: for now we enable AOVs when temporal is also enabled, because this seems to work better with Optix.
                if (denoise && (m_PathTracingSettings.useAOVs.value || m_PathTracingSettings.temporal.value))
                {
                    pathTracedAOVs.Add(new Tuple<TextureHandle, HDCameraFrameHistoryType>(albedo, HDCameraFrameHistoryType.PathTracingAlbedo));
                    pathTracedAOVs.Add(new Tuple<TextureHandle, HDCameraFrameHistoryType>(normal, HDCameraFrameHistoryType.PathTracingNormal));
                }

                if (denoise && m_PathTracingSettings.temporal.value)
                {
                    pathTracedAOVs.Add(new Tuple<TextureHandle, HDCameraFrameHistoryType>(motionVector, HDCameraFrameHistoryType.PathTracingMotionVector));
                }

                if (denoise && m_PathTracingSettings.separateVolumetrics.value)
                {
                    pathTracedAOVs.Add(new Tuple<TextureHandle, HDCameraFrameHistoryType>(volumetricScattering, HDCameraFrameHistoryType.PathTracingVolumetricFog));
                }
#endif
            }

            RenderAccumulation(m_RenderGraph, hdCamera, m_FrameTexture, colorBuffer, pathTracedAOVs, true);

            RenderDenoisePass(m_RenderGraph, renderContext, hdCamera, colorBuffer);

            return colorBuffer;
        }
    }

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DenoiserParameter"/> value.
    /// </summary>
    [Serializable]
    public sealed class DenoiserParameter : VolumeParameter<HDDenoiserType>
    {
        /// <summary>
        /// Creates a new <see cref="DenoiserParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DenoiserParameter(HDDenoiserType value, bool overrideState = false) : base(value, overrideState) { }
    }
#endif
}
