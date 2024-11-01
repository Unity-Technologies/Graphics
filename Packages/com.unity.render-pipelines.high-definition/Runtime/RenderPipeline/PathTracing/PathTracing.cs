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
    /// A volume component that holds settings for the Path Tracing effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Path Tracing")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Ray-Tracing-Path-Tracing")]
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

        /// <summary>
        /// Default constructor for the path tracing volume component.
        /// </summary>
        public PathTracing()
        {
            displayName = "Path Tracing";
        }
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

        TextureHandle m_FrameTexture;       // Stores the per-pixel results of path tracing for one frame
        TextureHandle m_SkyBGTexture;       // Stores the sky background as seem from the camera
        TextureHandle m_SkyCDFTexture;      // Stores latlon sky data (CDF) for importance sampling
        TextureHandle m_SkyMarginalTexture; // Stores latlon sky data (Marginal) for importance sampling

        int m_skySamplingSize;     // value used for the latlon sky texture (width = 2*size, height = size)

        List<Tuple<TextureHandle, HDCameraFrameHistoryType>> pathTracedAOVs;

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnUndoRecorded;
            Undo.undoRedoPerformed += OnSceneEdit;
            SceneView.duringSceneGui += OnSceneGui;
#endif // UNITY_EDITOR

            TextureDesc td = new TextureDesc(Vector2.one, true, true);
            td.format = GraphicsFormat.R32G32B32A32_SFloat;
            td.useMipMap = false;
            td.autoGenerateMips = false;

            // Texture storing the result of one iteration (one per frame) of path tracing
            td.name = "PathTracingFrameBuffer";
            td.enableRandomWrite = true;
            m_FrameTexture = m_RenderGraph.CreateSharedTexture(td);

            // Texture storing the sky background, matching the rasterization one
            td.name = "PathTracingSkyBackgroundBuffer";
            td.enableRandomWrite = false;
            m_SkyBGTexture = m_RenderGraph.CreateSharedTexture(td);

            // Textures used to importance sample the sky (aka environment sampling)
            td.name = "PathTracingSkySamplingBuffer";
            td.format = GraphicsFormat.R32_SFloat;
            td.dimension = TextureDimension.Tex2D;
            td.enableRandomWrite = true;
            td.useDynamicScale = false;
            td.slices = 1;
            td.sizeMode = TextureSizeMode.Explicit;
            m_skySamplingSize = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize * 2;
            td.width = m_skySamplingSize * 2;
            td.height = m_skySamplingSize;
            m_SkyCDFTexture = m_RenderGraph.CreateSharedTexture(td, true);
            td.width = m_skySamplingSize;
            td.height = 1;
            m_SkyMarginalTexture = m_RenderGraph.CreateSharedTexture(td, true);

            pathTracedAOVs = new List<Tuple<TextureHandle, HDCameraFrameHistoryType>>(3);
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnUndoRecorded;
            Undo.undoRedoPerformed -= OnSceneEdit;
            SceneView.duringSceneGui -= OnSceneGui;
#endif // UNITY_EDITOR

            m_RenderGraph.ReleaseSharedTexture(m_SkyCDFTexture);
            m_RenderGraph.ReleaseSharedTexture(m_SkyMarginalTexture);
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
            int camID = hdCamera.camera.GetInstanceID();
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
                m_SubFrameManager.Reset(sv.camera.GetInstanceID());
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
            using (var builder = renderGraph.AddRenderPass<RenderPathTracingData>("Render Path Tracing Frame", out var passData))
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
                passData.skyBG = builder.ReadTexture(m_SkyBGTexture);
                passData.skyCDF = builder.ReadTexture(m_SkyCDFTexture);
                passData.skyMarginal = builder.ReadTexture(m_SkyMarginalTexture);

                passData.output = builder.WriteTexture(pathTracingBuffer);

                // AOVs
                passData.enableAOVs = albedo.IsValid() && normal.IsValid() && motionVector.IsValid();
                if (passData.enableAOVs)
                {
                    passData.albedoAOV = builder.WriteTexture(albedo);
                    passData.normalAOV = builder.WriteTexture(normal);
                    passData.motionVectorAOV = builder.WriteTexture(motionVector);
                }
                passData.enableVolumetricScattering = volumetricScattering.IsValid();
                if (passData.enableVolumetricScattering)
                {
                    passData.volumetricScatteringAOV = builder.WriteTexture(volumetricScattering);
                }

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (RenderPathTracingData data, RenderGraphContext ctx) =>
                    {
                        // Define the shader pass to use for the path tracing pass
                        ctx.cmd.SetRayTracingShaderPass(data.shader, "PathTracingDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.shader, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Update the global constant buffer
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRaytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // LightLoop data
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, data.lightCluster.GetCluster());

                        // Global sky data
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingCameraSkyEnabled, data.cameraData.skyEnabled ? 1 : 0);
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureWidth, 2 * data.skySize);
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureHeight, data.skySize);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, data.skyReflection);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._SkyCameraTexture, data.skyBG);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyCDFTexture, data.skyCDF);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyMarginalTexture, data.skyMarginal);

                        // Further sky-related data for the ray miss
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingCameraClearColor, data.backgroundColor);

                        // Data used in the camera rays generation
                        ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._FrameTexture, data.output);
                        ctx.cmd.SetRayTracingMatrixParam(data.shader, HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDirWS);
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingDoFParameters, data.dofParameters);
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingTilingParameters, data.tilingParameters);


                        if (data.enableDecals)
                            DecalSystem.instance.SetAtlas(ctx.cmd); // for clustered decals

#if ENABLE_SENSOR_SDK
                        // SensorSDK can do its own camera rays generation
                        data.prepareDispatchRays?.Invoke(ctx.cmd);
#endif

                        // AOVs
                        if (data.enableAOVs)
                        {
                            ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._AlbedoAOV, data.albedoAOV);
                            ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._NormalAOV, data.normalAOV);
                            ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._MotionVectorAOV, data.motionVectorAOV);
                        }
                        if (data.enableVolumetricScattering)
                        {
                            ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._VolumetricScatteringAOV, data.volumetricScatteringAOV);
                        }

                        // Run the computation
                        var shaderName = data.enableAOVs ?
                                         (data.enableVolumetricScattering ? "RayGenVolScatteringAOV" : "RayGenAOV") :
                                         (data.enableVolumetricScattering ? "RayGenVolScattering" : "RayGen");
                        ctx.cmd.DispatchRays(data.shader, shaderName, (uint)data.width, (uint)data.height, 1);
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

            using (var builder = renderGraph.AddRenderPass<RenderSkySamplingPassData>("Render Sky Sampling Data for Path Tracing", out var passData))
            {
                passData.shader = rayTracingResources.pathTracingSkySamplingDataCS;
                passData.k0 = passData.shader.FindKernel("ComputeCDF");
                passData.k1 = passData.shader.FindKernel("ComputeMarginal");
                passData.size = m_skySamplingSize;
                passData.outputCDF = builder.WriteTexture(m_SkyCDFTexture);
                passData.outputMarginal = builder.WriteTexture(m_SkyMarginalTexture);

                builder.SetRenderFunc(
                    (RenderSkySamplingPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeIntParam(data.shader, HDShaderIDs._PathTracingSkyTextureWidth, data.size * 2);
                        ctx.cmd.SetComputeIntParam(data.shader, HDShaderIDs._PathTracingSkyTextureHeight, data.size);

                        ctx.cmd.SetComputeTextureParam(data.shader, data.k0, HDShaderIDs._PathTracingSkyCDFTexture, data.outputCDF);
                        ctx.cmd.SetComputeTextureParam(data.shader, data.k0, HDShaderIDs._PathTracingSkyMarginalTexture, data.outputMarginal);
                        ctx.cmd.DispatchCompute(data.shader, data.k0, 1, data.size, 1);

                        ctx.cmd.SetComputeTextureParam(data.shader, data.k1, HDShaderIDs._PathTracingSkyMarginalTexture, data.outputMarginal);
                        ctx.cmd.DispatchCompute(data.shader, data.k1, 1, 1, 1);
                    });
            }
        }

        // This is the method to call from the main render loop
        TextureHandle RenderPathTracing(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
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

            int camID = hdCamera.camera.GetInstanceID();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);

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

            RenderDenoisePass(m_RenderGraph, hdCamera, colorBuffer);

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
