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
    [Serializable, VolumeComponentMenu("Ray Tracing/Path Tracing (Preview)")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Ray-Tracing-Path-Tracing" + Documentation.endURL)]
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
        /// Defines the maximum intensity value computed for a path segment.
        /// </summary>
        [Tooltip("Defines the maximum intensity value computed for a path segment.")]
        public ClampedFloatParameter maximumIntensity = new ClampedFloatParameter(10f, 0f, 100f);

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
        ulong m_CacheAccelSize = 0;
        uint  m_CacheLightCount = 0;

        RTHandle m_RadianceTexture; // stores the per-pixel results of path tracing for this frame

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnUndoRecorded;
            Undo.undoRedoPerformed += OnSceneEdit;
            SceneView.duringSceneGui += OnSceneGui;
#endif // UNITY_EDITOR

            m_RadianceTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension,
            enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
            name: "PathTracingFrameBuffer");
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnUndoRecorded;
            Undo.undoRedoPerformed -= OnSceneEdit;
            SceneView.duringSceneGui -= OnSceneGui;
#endif // UNITY_EDITOR

            RTHandles.Release(m_RadianceTexture);
        }

        internal void ResetPathTracing()
        {
            m_SubFrameManager.Reset();
        }

        private Vector4 ComputeDoFConstants(HDCamera hdCamera, PathTracing settings)
        {
            var dofSettings = hdCamera.volumeStack.GetComponent<DepthOfField>();
            bool enableDof = (dofSettings.focusMode.value == DepthOfFieldMode.UsePhysicalCamera) && !(hdCamera.camera.cameraType == CameraType.SceneView);

            // focalLength is in mm, so we need to convert to meters. We also want the aperture radius, not diameter, so we divide by two.
            float apertureRadius = (enableDof && hdCamera.physicalParameters != null && hdCamera.physicalParameters.aperture > 0) ? 0.5f * 0.001f * hdCamera.camera.focalLength / hdCamera.physicalParameters.aperture : 0.0f;

            return new Vector4(apertureRadius, dofSettings.focusDistance.value, 0.0f, 0.0f);
        }

#if UNITY_EDITOR

        private void OnSceneEdit()
        {
            // If we just change the sample count, we don't necessarily want to reset iteration
            if (m_PathTracingSettings && m_CacheMaxIteration != m_PathTracingSettings.maximumSamples.value)
            {
                m_CacheMaxIteration = (uint) m_PathTracingSettings.maximumSamples.value;
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

        private void CheckDirtiness(HDCamera hdCamera)
        {
            if (m_SubFrameManager.isRecording)
            {
                return;
            }

            // Grab the cached data for the current camera
            int camID = hdCamera.camera.GetInstanceID();
            CameraData camData = m_SubFrameManager.GetCameraData(camID);

            // Check camera resolution dirtiness
            if (hdCamera.actualWidth != camData.width || hdCamera.actualHeight != camData.height)
            {
                camData.width = (uint) hdCamera.actualWidth;
                camData.height = (uint) hdCamera.actualHeight;
                camData.ResetIteration();
                m_SubFrameManager.SetCameraData(camID, camData);
                return;
            }

            // Check camera sky dirtiness
            bool enabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
            if (enabled != camData.skyEnabled)
            {
                camData.skyEnabled = enabled;
                camData.ResetIteration();
                m_SubFrameManager.SetCameraData(camID, camData);
                return;
            }

            // Check camera fog dirtiness
            enabled = Fog.IsFogEnabled(hdCamera);
            if (enabled != camData.fogEnabled)
            {
                camData.fogEnabled = enabled;
                camData.ResetIteration();
                m_SubFrameManager.SetCameraData(camID, camData);
                return;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                camData.ResetIteration();
                m_SubFrameManager.SetCameraData(camID, camData);
                return;
            }

            // Check materials dirtiness
            if (m_MaterialsDirty)
            {
                m_MaterialsDirty = false;
                ResetPathTracing();
                return;
            }

            // Check light or geometry transforms dirtiness
            if (m_TransformDirty)
            {
                m_TransformDirty = false;
                ResetPathTracing();
            }

            // Check lights dirtiness
            if (m_CacheLightCount != m_RayTracingLights.lightCount)
            {
                m_CacheLightCount = (uint) m_RayTracingLights.lightCount;
                ResetPathTracing();
                return;
            }

            // Check geometry dirtiness
            ulong accelSize = m_CurrentRAS.GetSize();
            if (accelSize != m_CacheAccelSize)
            {
                m_CacheAccelSize = accelSize;
                ResetPathTracing();
            }
        }

        static RTHandle PathTracingHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_PathTracingHistoryBuffer{1}", viewName, frameIndex));
        }

        struct PathTracingParameters
        {
            public RayTracingShader                 pathTracingShader;
            public CameraData                       cameraData;
            public BlueNoise.DitheredTextureSet     ditheredTextureSet;
            public ShaderVariablesRaytracing        shaderVariablesRaytracingCB;
            public Color                            backgroundColor;
            public Texture                          skyReflection;
            public Matrix4x4                        pixelCoordToViewDirWS;
            public Vector4                          dofParameters;
            public int                              width, height;
            public RayTracingAccelerationStructure  accelerationStructure;
            public HDRaytracingLightCluster         lightCluster;
        }

        PathTracingParameters PreparePathTracingParameters(HDCamera hdCamera)
        {
            PathTracingParameters parameters = new PathTracingParameters();

            parameters.pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            parameters.cameraData = m_SubFrameManager.GetCameraData(hdCamera.camera.GetInstanceID());
            parameters.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet256SPP();
            parameters.backgroundColor = hdCamera.backgroundColorHDR;
            parameters.skyReflection = m_SkyManager.GetSkyReflection(hdCamera);
            parameters.pixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
            parameters.dofParameters = ComputeDoFConstants(hdCamera, m_PathTracingSettings);
            parameters.width = hdCamera.actualWidth;
            parameters.height = hdCamera.actualHeight;
            parameters.accelerationStructure = RequestAccelerationStructure();
            parameters.lightCluster = RequestLightCluster();

            parameters.shaderVariablesRaytracingCB = m_ShaderVariablesRayTracingCB;
            parameters.shaderVariablesRaytracingCB._RaytracingNumSamples = (int)m_SubFrameManager.subFrameCount;
            parameters.shaderVariablesRaytracingCB._RaytracingMinRecursion = m_PathTracingSettings.minimumDepth.value;
            parameters.shaderVariablesRaytracingCB._RaytracingMaxRecursion = m_PathTracingSettings.maximumDepth.value;
            parameters.shaderVariablesRaytracingCB._RaytracingIntensityClamp = m_PathTracingSettings.maximumIntensity.value;
            parameters.shaderVariablesRaytracingCB._RaytracingSampleIndex = (int)parameters.cameraData.currentIteration;

            return parameters;
        }

        static void RenderPathTracing(in PathTracingParameters parameters, RTHandle radianceTexture, CommandBuffer cmd)
        {
            // Define the shader pass to use for the path tracing pass
            cmd.SetRayTracingShaderPass(parameters.pathTracingShader, "PathTracingDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(parameters.pathTracingShader, HDShaderIDs._RaytracingAccelerationStructureName, parameters.accelerationStructure);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, parameters.ditheredTextureSet);

            // Update the global constant buffer
            ConstantBuffer.PushGlobal(cmd, parameters.shaderVariablesRaytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, parameters.lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, parameters.lightCluster.GetLightDatas());

            // Set the data for the ray miss
            cmd.SetRayTracingIntParam(parameters.pathTracingShader, HDShaderIDs._RaytracingCameraSkyEnabled, parameters.cameraData.skyEnabled ? 1 : 0);
            cmd.SetRayTracingVectorParam(parameters.pathTracingShader, HDShaderIDs._RaytracingCameraClearColor, parameters.backgroundColor);
            cmd.SetRayTracingTextureParam(parameters.pathTracingShader, HDShaderIDs._SkyTexture, parameters.skyReflection);

            // Additional data for path tracing
            cmd.SetRayTracingTextureParam(parameters.pathTracingShader, HDShaderIDs._RadianceTexture, radianceTexture);
            cmd.SetRayTracingMatrixParam(parameters.pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, parameters.pixelCoordToViewDirWS);
            cmd.SetRayTracingVectorParam(parameters.pathTracingShader, HDShaderIDs._PathTracedDoFConstants, parameters.dofParameters);

            // Run the computation
            cmd.DispatchRays(parameters.pathTracingShader, "RayGen", (uint)parameters.width, (uint)parameters.height, 1);
        }

        void RenderPathTracing(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture)
        {
            RayTracingShader pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!pathTracingShader || !m_PathTracingSettings.enable.value)
                return;

            if (hdCamera.viewCount > 1)
            {
                Debug.LogError("Path Tracing is not supported when using XR single-pass rendering.");
                return;
            }

            CheckDirtiness(hdCamera);

            var parameters = PreparePathTracingParameters(hdCamera);

            if (!m_SubFrameManager.isRecording)
            {
                // If we are recording, the max iteration is set/overridden by the subframe manager, otherwise we read it from the path tracing volume
                m_SubFrameManager.subFrameCount = (uint)m_PathTracingSettings.maximumSamples.value;
            }

#if UNITY_HDRP_DXR_TESTS_DEFINE
			if (Application.isPlaying)
            	m_SubFrameManager.subFrameCount = 1;
#endif

            if (parameters.cameraData.currentIteration < m_SubFrameManager.subFrameCount)
            {
                RenderPathTracing(parameters, m_RadianceTexture, cmd);
            }
            RenderAccumulation(hdCamera, m_RadianceTexture, outputTexture, true, cmd);
        }

        class RenderPathTracingData
        {
            public PathTracingParameters parameters;
            public TextureHandle output;
        }

        TextureHandle RenderPathTracing(RenderGraph renderGraph, in PathTracingParameters parameters, TextureHandle pathTracingBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<RenderPathTracingData>("Render PathTracing", out var passData))
            {
                passData.parameters = parameters;
                passData.output = builder.WriteTexture(pathTracingBuffer);

                builder.SetRenderFunc(
                (RenderPathTracingData data, RenderGraphContext ctx) =>
                {
                    RenderPathTracing(data.parameters, data.output, ctx.cmd);
                });

                return passData.output;
            }
        }


        TextureHandle RenderPathTracing(RenderGraph renderGraph, HDCamera hdCamera)
        {
            RayTracingShader pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!pathTracingShader || !m_PathTracingSettings.enable.value)
                return TextureHandle.nullHandle;

            CheckDirtiness(hdCamera);

            var parameters = PreparePathTracingParameters(hdCamera);
            TextureHandle outputTexture = CreateColorBuffer(renderGraph, hdCamera, false);
            // TODO RENDERGRAPH: This texture needs to be persistent
            // (apparently it only matters for some tests, loading a regular scene with pathtracing works even if this one is not persistent)
            // So we need to import a regular RTHandle. This is not good because it means the texture will always be allocate even if not used...
            // Refactor that when we formalize how to handle persistent textures better (with automatic lifetime and such).
            var radianceTexture = renderGraph.ImportTexture(m_RadianceTexture);

            if (!m_SubFrameManager.isRecording)
            {
                // If we are recording, the max iteration is set/overridden by the subframe manager, otherwise we read it from the path tracing volume
                m_SubFrameManager.subFrameCount = (uint)m_PathTracingSettings.maximumSamples.value;
            }


#if UNITY_HDRP_DXR_TESTS_DEFINE
			if (Application.isPlaying)
            	m_SubFrameManager.subFrameCount = 1;
#endif

            if (parameters.cameraData.currentIteration < m_SubFrameManager.subFrameCount)
            {
                RenderPathTracing(m_RenderGraph, parameters, radianceTexture);
            }

            RenderAccumulation(m_RenderGraph, hdCamera, radianceTexture, outputTexture, true);

            return outputTexture;
        }
    }
}
