using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

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
    /// A volume component that holds settings for the Path Tracing effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Ray Tracing/Path Tracing (Preview)", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Ray-Tracing-Path-Tracing")]
    public sealed class PathTracing : VolumeComponent
    {
        /// <summary>
        /// Enables path tracing (thus disabling most other passes).
        /// </summary>
        [Tooltip("Enables path tracing (thus disabling most other passes).")]
        public BoolParameter enable = new BoolParameter(false);

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
        /// Defines the minimum number of bounces for each path, in [1, 10].
        /// </summary>
        [Tooltip("Defines the minimum number of bounces for each path, in [1, 10].")]
        public ClampedIntParameter minimumDepth = new ClampedIntParameter(1, 1, 10);

        /// <summary>
        /// Defines the maximum number of bounces for each path, in [minimumDepth, 10].
        /// </summary>
        [Tooltip("Defines the maximum number of bounces for each path, in [minimumDepth, 10].")]
        public ClampedIntParameter maximumDepth = new ClampedIntParameter(4, 1, 10);

        /// <summary>
        /// Defines the maximum, post-exposed luminance computed for indirect path segments.
        /// </summary>
        [Tooltip("Defines the maximum, post-exposed luminance computed for indirect path segments. Lower values help against noise and fireflies (very bright pixels), but introduce bias by darkening the overall result. Increase this value if your image looks too dark.")]
        public MinFloatParameter maximumIntensity = new MinFloatParameter(10f, 0f);

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
        /// Default constructor for the path tracing volume component.
        /// </summary>
        public PathTracing()
        {
            displayName = "Path Tracing (Preview)";
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
        bool m_RenderSky = true;

        TextureHandle m_FrameTexture;       // Stores the per-pixel results of path tracing for one frame
        TextureHandle m_SkyBGTexture;       // Stores the sky background as seem from the camera
        TextureHandle m_SkyCDFTexture;      // Stores latlon sky data (CDF) for importance sampling
        TextureHandle m_SkyMarginalTexture; // Stores latlon sky data (Marginal) for importance sampling

        int m_skySamplingSize;     // value used for the latlon sky texture (width = 2*size, height = size)

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnUndoRecorded;
            Undo.undoRedoPerformed += OnSceneEdit;
            SceneView.duringSceneGui += OnSceneGui;
#endif // UNITY_EDITOR

            TextureDesc td = new TextureDesc(Vector2.one, true, true);
            td.colorFormat = GraphicsFormat.R32G32B32A32_SFloat;
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
            td.colorFormat = GraphicsFormat.R32_SFloat;
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
            float apertureRadius = (enableDof && hdCamera.physicalParameters.aperture > 0) ? 0.5f * 0.001f * hdCamera.camera.focalLength / hdCamera.physicalParameters.aperture : 0.0f;

            float focusDistance = (dofSettings.focusDistanceMode.value == FocusDistanceMode.Volume) ? dofSettings.focusDistance.value : hdCamera.physicalParameters.focusDistance;

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

        private void OnSceneEdit()
        {
            // If we just change the sample count, we don't necessarily want to reset iteration
            if (m_PathTracingSettings && m_CacheMaxIteration != m_PathTracingSettings.maximumSamples.value)
            {
                m_RenderSky = true;
                m_CacheMaxIteration = (uint)m_PathTracingSettings.maximumSamples.value;
                m_SubFrameManager.SelectiveReset(m_CacheMaxIteration);
            }
            else
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

        private CameraData CheckDirtiness(HDCamera hdCamera, int camID, CameraData camData)
        {
            // Check resolution dirtiness
            if (hdCamera.actualWidth != camData.width || hdCamera.actualHeight != camData.height)
            {
                camData.width = (uint)hdCamera.actualWidth;
                camData.height = (uint)hdCamera.actualHeight;
                return ResetPathTracing(camID, camData);
            }

            // Check sky dirtiness
            bool enabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
            if (enabled != camData.skyEnabled)
            {
                camData.skyEnabled = enabled;
                return ResetPathTracing(camID, camData);
            }

            // Check fog dirtiness
            enabled = Fog.IsFogEnabled(hdCamera);
            if (enabled != camData.fogEnabled)
            {
                camData.fogEnabled = enabled;
                return ResetPathTracing(camID, camData);
            }

            // Check acceleration structure dirtiness
            ulong accelSize = RequestAccelerationStructure(hdCamera).GetSize();
            if (accelSize != camData.accelSize)
            {
                camData.accelSize = accelSize;
                return ResetPathTracing(camID, camData);
            }

            // Check materials dirtiness
            if (GetMaterialDirtiness(hdCamera))
            {
                ResetMaterialDirtiness(hdCamera);
                ResetPathTracing();
                return camData;
            }

            // Check light or geometry transforms dirtiness
            if (GetTransformDirtiness(hdCamera))
            {
                ResetTransformDirtiness(hdCamera);
                ResetPathTracing();
                return camData;
            }

            // Check lights dirtiness
            if (m_CacheLightCount != m_RayTracingLights.lightCount)
            {
                m_CacheLightCount = (uint)m_RayTracingLights.lightCount;
                ResetPathTracing();
                return camData;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                return ResetPathTracing(camID, camData);
            }

            // If nothing but the camera has changed, re-render the sky texture
            if (camID != m_CameraID)
            {
                m_RenderSky = true;
                m_CameraID = camID;
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

#if ENABLE_SENSOR_SDK
            public Action<UnityEngine.Rendering.CommandBuffer> prepareDispatchRays;
#endif
        }

        void RenderPathTracingFrame(RenderGraph renderGraph, HDCamera hdCamera, in CameraData cameraData, TextureHandle pathTracingBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<RenderPathTracingData>("Render Path Tracing Frame", out var passData))
            {
#if ENABLE_SENSOR_SDK
                passData.shader = hdCamera.pathTracingShaderOverride ? hdCamera.pathTracingShaderOverride : m_GlobalSettings.renderPipelineRayTracingResources.pathTracingRT;
                passData.prepareDispatchRays = hdCamera.prepareDispatchRays;
#else
                passData.shader = m_GlobalSettings.renderPipelineRayTracingResources.pathTracingRT;
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
                passData.shaderVariablesRaytracingCB._RaytracingNumSamples = (int)m_SubFrameManager.subFrameCount;
                passData.shaderVariablesRaytracingCB._RaytracingMinRecursion = m_PathTracingSettings.minimumDepth.value;
#if NO_RAY_RECURSION
                passData.shaderVariablesRaytracingCB._RaytracingMaxRecursion = 1;
#else
                passData.shaderVariablesRaytracingCB._RaytracingMaxRecursion = m_PathTracingSettings.maximumDepth.value;
#endif
                passData.shaderVariablesRaytracingCB._RaytracingIntensityClamp = m_PathTracingSettings.maximumIntensity.value;
                passData.shaderVariablesRaytracingCB._RaytracingSampleIndex = (int)cameraData.currentIteration;

                passData.skyReflection = m_SkyManager.GetSkyReflection(hdCamera);
                passData.skyBG = builder.ReadTexture(m_SkyBGTexture);
                passData.skyCDF = builder.ReadTexture(m_SkyCDFTexture);
                passData.skyMarginal = builder.ReadTexture(m_SkyMarginalTexture);

                passData.output = builder.WriteTexture(pathTracingBuffer);

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
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, data.lightCluster.GetLightDatas());

                        // Global sky data
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingCameraSkyEnabled, data.cameraData.skyEnabled ? 1 : 0);
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureWidth, 2 * data.skySize);
                        ctx.cmd.SetGlobalInt(HDShaderIDs._PathTracingSkyTextureHeight, data.skySize);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, data.skyReflection);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyCDFTexture, data.skyCDF);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._PathTracingSkyMarginalTexture, data.skyMarginal);

                        // Further sky-related data for the ray miss
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingCameraClearColor, data.backgroundColor);
                        ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._SkyCameraTexture, data.skyBG);

                        // Data used in the camera rays generation
                        ctx.cmd.SetRayTracingTextureParam(data.shader, HDShaderIDs._FrameTexture, data.output);
                        ctx.cmd.SetRayTracingMatrixParam(data.shader, HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDirWS);
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingDoFParameters, data.dofParameters);
                        ctx.cmd.SetRayTracingVectorParam(data.shader, HDShaderIDs._PathTracingTilingParameters, data.tilingParameters);

#if ENABLE_SENSOR_SDK
                        // SensorSDK can do its own camera rays generation
                        data.prepareDispatchRays?.Invoke(ctx.cmd);
#endif
                        // Run the computation
                        ctx.cmd.DispatchRays(data.shader, "RayGen", (uint)data.width, (uint)data.height, 1);
                    });
            }
        }

        // Simpler variant used by path tracing, without depth buffer or volumetric computations
        void RenderSkyBackground(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle skyBuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            using (var builder = renderGraph.AddRenderPass<RenderSkyPassData>("Render Sky Background for Path Tracing", out var passData))
            {
                passData.sunLight = GetMainLight();
                passData.hdCamera = hdCamera;
                passData.colorBuffer = builder.WriteTexture(skyBuffer);
                passData.depthTexture = builder.WriteTexture(CreateDepthBuffer(renderGraph, true, MSAASamples.None));
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.skyManager = m_SkyManager;

                builder.SetRenderFunc(
                    (RenderSkyPassData data, RenderGraphContext ctx) =>
                    {
                        // Override the exposure texture, as we need a neutral value for this render
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, m_EmptyExposureTexture);

                        data.skyManager.RenderSky(data.hdCamera, data.sunLight, data.colorBuffer, data.depthTexture, data.debugDisplaySettings, ctx.cmd);

                        // Restore the regular exposure texture
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._ExposureTexture, GetExposureTexture(hdCamera));
                    });
            }
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
            if (!m_GlobalSettings.renderPipelineRayTracingResources.pathTracingSkySamplingDataCS)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderSkySamplingPassData>("Render Sky Sampling Data for Path Tracing", out var passData))
            {
                passData.shader = m_GlobalSettings.renderPipelineRayTracingResources.pathTracingSkySamplingDataCS;
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
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!m_GlobalSettings.renderPipelineRayTracingResources.pathTracingRT || !m_PathTracingSettings.enable.value)
                return TextureHandle.nullHandle;

            int camID = hdCamera.camera.GetInstanceID();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);

            // Check if the camera has a valid history buffer and if not reset the accumulation.
            // This can happen if a script disables and re-enables the camera (case 1337843).
            if (!hdCamera.isPersistent && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing) == null)
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
                // When recording, as be bypass dirtiness checks which update camData, we need to indicate whether we want to render a sky or not
                camData.skyEnabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
                m_SubFrameManager.SetCameraData(camID, camData);
            }

#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
            {
                camData.ResetIteration();
                m_SubFrameManager.subFrameCount = 1;
            }
#endif

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

                RenderPathTracingFrame(m_RenderGraph, hdCamera, camData, m_FrameTexture);
            }

            RenderAccumulation(m_RenderGraph, hdCamera, m_FrameTexture, colorBuffer, true);

            return colorBuffer;
        }
    }
}
