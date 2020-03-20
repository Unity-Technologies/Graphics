using System;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
    using UnityEditor;
#endif // UNITY_EDITOR

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Path Tracing effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Path Tracing (Preview)")]
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
        public ClampedIntParameter maximumSamples = new ClampedIntParameter(256, 1, 4096);

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
    }
    public partial class HDRenderPipeline
    {
        PathTracing m_PathTracingSettings = null;

        uint  m_CurrentIteration = 0;
#if UNITY_EDITOR
        uint  m_CacheMaxIteration = 0;
#endif // UNITY_EDITOR
        ulong m_CacheAccelSize = 0;
        uint  m_CacheLightCount = 0;
        uint  m_CacheCameraWidth = 0;
        uint  m_CacheCameraHeight = 0;

        bool m_CameraSkyEnabled;
        bool m_FogEnabled;

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnUndoRecorded;
            Undo.undoRedoPerformed += OnSceneEdit;
            SceneView.duringSceneGui += OnSceneGui;
#endif // UNITY_EDITOR
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnUndoRecorded;
            Undo.undoRedoPerformed -= OnSceneEdit;
            SceneView.duringSceneGui -= OnSceneGui;
#endif // UNITY_EDITOR
        }

        internal void ResetPathTracing()
        {
            m_CurrentIteration = 0;
        }

#if UNITY_EDITOR

        private void OnSceneEdit()
        {
            // If we just change the sample count, we don't want to reset iteration
            if (m_PathTracingSettings && m_CacheMaxIteration != m_PathTracingSettings.maximumSamples.value)
            {
                m_CacheMaxIteration = (uint) m_PathTracingSettings.maximumSamples.value;
                if (m_CurrentIteration >= m_CacheMaxIteration)
                    m_CurrentIteration = 0;
            }
            else
                m_CurrentIteration = 0;
        }

        private UndoPropertyModification[] OnUndoRecorded(UndoPropertyModification[] modifications)
        {
            OnSceneEdit();

            return modifications;
        }

        private void OnSceneGui(SceneView sv)
        {
            if (Event.current.type == EventType.MouseDrag)
                m_CurrentIteration = 0;
        }

#endif // UNITY_EDITOR

        private void CheckDirtiness(HDCamera hdCamera)
        {
            // Check camera clear mode dirtiness
            bool enabled = (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky);
            if (enabled != m_CameraSkyEnabled)
            {
                m_CameraSkyEnabled = enabled;
                m_CurrentIteration = 0;
                return;
            }

            // Check camera resolution dirtiness
            if (hdCamera.actualWidth != m_CacheCameraWidth || hdCamera.actualHeight != m_CacheCameraHeight)
            {
                m_CacheCameraWidth = (uint) hdCamera.actualWidth;
                m_CacheCameraHeight = (uint) hdCamera.actualHeight;
                m_CurrentIteration = 0;
                return;
            }

            // Check camera resolution dirtiness
            if (hdCamera.actualWidth != m_CacheCameraWidth || hdCamera.actualHeight != m_CacheCameraHeight)
            {
                m_CacheCameraWidth = (uint) hdCamera.actualWidth;
                m_CacheCameraHeight = (uint) hdCamera.actualHeight;
                m_CurrentIteration = 0;
                return;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                m_CurrentIteration = 0;
                return;
            }

            // Check fog dirtiness
            enabled = Fog.IsFogEnabled(hdCamera);
            if (enabled != m_FogEnabled)
            {
                m_FogEnabled = enabled;
                m_CurrentIteration = 0;
                return;
            }

            // Check materials dirtiness
            if (m_MaterialsDirty)
            {
                m_CurrentIteration = 0;
                m_MaterialsDirty = false;
                return;
            }

            // Check lights dirtiness
            if (m_CacheLightCount != m_RayTracingLights.lightCount)
            {
                m_CacheLightCount = (uint) m_RayTracingLights.lightCount;
                m_CurrentIteration = 0;
                return;
            }

            // Check geometry dirtiness
            ulong accelSize = m_CurrentRAS.GetSize();
            if (accelSize != m_CacheAccelSize)
            {
                m_CacheAccelSize = accelSize;
                m_CurrentIteration = 0;
            }
        }

        static RTHandle PathTracingHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("PathTracingHistoryBuffer{0}", frameIndex));
        }

        void RenderPathTracing(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture)
        {
            RayTracingShader pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            m_PathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!pathTracingShader || !m_PathTracingSettings.enable.value)
                return;

            CheckDirtiness(hdCamera);

            // Inject the ray-tracing sampling data
            BlueNoise blueNoiseManager = GetBlueNoiseManager();
            blueNoiseManager.BindDitheredRNGData256SPP(cmd);

            // Grab the history buffer (hijack the reflections one)
            RTHandle history = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracing, PathTracingHistoryBufferAllocatorFunction, 1);

            // Grab the acceleration structure and the list of HD lights for the target camera
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Define the shader pass to use for the path tracing pass
            cmd.SetRayTracingShaderPass(pathTracingShader, "PathTracingDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(pathTracingShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambled256Tex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

            // Inject the ray generation data
#if UNITY_HDRP_DXR_TESTS_DEFINE
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingNumSamples, Application.isPlaying ? 1 : m_PathTracingSettings.maximumSamples.value);
#else
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingNumSamples, m_PathTracingSettings.maximumSamples.value);
#endif
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMinRecursion, m_PathTracingSettings.minimumDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, m_PathTracingSettings.maximumDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingIntensityClamp, m_PathTracingSettings.maximumIntensity.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingCameraNearPlane, hdCamera.camera.nearClipPlane);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._CameraColorTextureRW, outputTexture);
            cmd.SetGlobalInt(HDShaderIDs._RaytracingSampleIndex, (int)m_CurrentIteration);

            // Compute an approximate pixel spread angle value (in radians)
            cmd.SetRayTracingFloatParam(pathTracingShader, HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Set the data for the ray miss
            cmd.SetRayTracingIntParam(pathTracingShader, HDShaderIDs._RaytracingCameraSkyEnabled, m_CameraSkyEnabled ? 1 : 0);
            cmd.SetRayTracingVectorParam(pathTracingShader, HDShaderIDs._RaytracingCameraClearColor, hdCamera.backgroundColorHDR);
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

            // Additional data for path tracing
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._AccumulatedFrameTexture, history);
            cmd.SetRayTracingMatrixParam(pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, hdCamera.mainViewConstants.pixelCoordToViewDirWS);

            // Run the computation
            cmd.DispatchRays(pathTracingShader, "RayGen", (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

            // Increment the iteration counter, if we haven't converged yet
            if (m_CurrentIteration < m_PathTracingSettings.maximumSamples.value)
                m_CurrentIteration++;
        }
    }
}
