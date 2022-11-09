using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace UnityEngine.Rendering.HighDefinition
{
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
        bool m_RenderSky = true;

        TextureHandle m_FrameTexture; // stores the per-pixel results of path tracing for one frame
        TextureHandle m_SkyTexture; // stores the sky background

        void InitPathTracing(RenderGraph renderGraph)
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

            td.name = "PathTracingFrameBuffer";
            td.enableRandomWrite = true;
            m_FrameTexture = renderGraph.CreateSharedTexture(td);

            td.name = "PathTracingSkyBuffer";
            td.enableRandomWrite = false;
            m_SkyTexture = renderGraph.CreateSharedTexture(td);
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnUndoRecorded;
            Undo.undoRedoPerformed -= OnSceneEdit;
            SceneView.duringSceneGui -= OnSceneGui;
#endif // UNITY_EDITOR
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

            bool isSceneDirty = false;
            // Check materials dirtiness
            if (m_MaterialsDirty)
            {
                m_MaterialsDirty = false;
                ResetPathTracing();
                isSceneDirty = true;
            }

            // Check light or geometry transforms dirtiness
            if (m_TransformDirty)
            {
                m_TransformDirty = false;
                ResetPathTracing();
                isSceneDirty = true;
            }

            // Check lights dirtiness
            if (m_CacheLightCount != m_RayTracingLights.lightCount)
            {
                m_CacheLightCount = (uint)m_RayTracingLights.lightCount;
                ResetPathTracing();
                isCameraDirty = true;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                isCameraDirty = true;
            }

            // Check acceleration structure dirtiness
            var rtas = RequestAccelerationStructure();
            ulong accelSize = (rtas != null) ? rtas.GetSize() : 0;
            if (accelSize != camData.accelSize)
            {
                camData.accelSize = accelSize;
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
                return camData;
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
            public RayTracingShader pathTracingShader;
            public CameraData cameraData;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRaytracingCB;
            public Color backgroundColor;
            public Texture skyReflection;
            public Matrix4x4 pixelCoordToViewDirWS;
            public Vector4 dofParameters;
            public Vector4 tilingParameters;
            public int width, height;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;

            public TextureHandle output;
            public TextureHandle sky;
        }

        TextureHandle RenderPathTracing(RenderGraph renderGraph, HDCamera hdCamera, in CameraData cameraData, TextureHandle pathTracingBuffer, TextureHandle skyBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<RenderPathTracingData>("Render PathTracing", out var passData))
            {
                passData.pathTracingShader = m_GlobalSettings.renderPipelineRayTracingResources.pathTracing;
                passData.cameraData = cameraData;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet256SPP();
                passData.backgroundColor = hdCamera.backgroundColorHDR;
                passData.skyReflection = m_SkyManager.GetSkyReflection(hdCamera);
                passData.pixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                passData.dofParameters = ComputeDoFConstants(hdCamera, m_PathTracingSettings);
                passData.tilingParameters = m_PathTracingSettings.tilingParameters.value;
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.accelerationStructure = RequestAccelerationStructure();
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

                passData.output = builder.WriteTexture(pathTracingBuffer);
                passData.sky = builder.ReadTexture(skyBuffer);

                builder.SetRenderFunc(
                    (RenderPathTracingData data, RenderGraphContext ctx) =>
                    {
                        // Define the shader pass to use for the path tracing pass
                        ctx.cmd.SetRayTracingShaderPass(data.pathTracingShader, "PathTracingDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.pathTracingShader, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Update the global constant buffer
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRaytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // LightLoop data
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, data.lightCluster.GetCluster());
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, data.lightCluster.GetLightDatas());

                        // Set the data for the ray miss
                        ctx.cmd.SetRayTracingIntParam(data.pathTracingShader, HDShaderIDs._RaytracingCameraSkyEnabled, data.cameraData.skyEnabled ? 1 : 0);
                        ctx.cmd.SetRayTracingVectorParam(data.pathTracingShader, HDShaderIDs._RaytracingCameraClearColor, data.backgroundColor);
                        ctx.cmd.SetRayTracingTextureParam(data.pathTracingShader, HDShaderIDs._SkyCameraTexture, data.sky);
                        ctx.cmd.SetRayTracingTextureParam(data.pathTracingShader, HDShaderIDs._SkyTexture, data.skyReflection);

                        // Additional data for path tracing
                        ctx.cmd.SetRayTracingTextureParam(data.pathTracingShader, HDShaderIDs._FrameTexture, data.output);
                        ctx.cmd.SetRayTracingMatrixParam(data.pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDirWS);
                        ctx.cmd.SetRayTracingVectorParam(data.pathTracingShader, HDShaderIDs._PathTracingDoFParameters, data.dofParameters);
                        ctx.cmd.SetRayTracingVectorParam(data.pathTracingShader, HDShaderIDs._PathTracingTilingParameters, data.tilingParameters);

                        // Run the computation
                        ctx.cmd.DispatchRays(data.pathTracingShader, "RayGen", (uint)data.width, (uint)data.height, 1);
                    });

                return passData.output;
            }
        }

        // Simpler variant used by path tracing, without depth buffer or volumetric computations
        void RenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle skyBuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            using (var builder = renderGraph.AddRenderPass<RenderSkyPassData>("Render Sky for Path Tracing", out var passData))
            {
                passData.visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
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

        TextureHandle RenderPathTracing(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
        {
            RayTracingShader pathTracingShader = m_GlobalSettings.renderPipelineRayTracingResources.pathTracing;
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!pathTracingShader || !m_PathTracingSettings.enable.value)
                return TextureHandle.nullHandle;

            int camID = hdCamera.camera.GetInstanceID();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);

            // Check if the camera has a valid history buffer and if not reset the accumulation.
            // This can happen if a script disables and re-enables the camera (case 1337843).
            if (!hdCamera.isPersistent && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing) == null)
            {
                m_SubFrameManager.Reset(camID);
            }

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
                    RenderSky(m_RenderGraph, hdCamera, m_SkyTexture);
                    m_RenderSky = false;
                }

                RenderPathTracing(m_RenderGraph, hdCamera, camData, m_FrameTexture, m_SkyTexture);
            }

            RenderAccumulation(m_RenderGraph, hdCamera, m_FrameTexture, colorBuffer, true);

            return colorBuffer;
        }
    }
}
