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

#if UNITY_EDITOR
        uint  m_CacheMaxIteration = 0;
#endif // UNITY_EDITOR
        ulong m_CacheAccelSize = 0;
        uint  m_CacheLightCount = 0;
        uint  m_CacheCameraWidth = 0;
        uint  m_CacheCameraHeight = 0;

        bool m_CameraSkyEnabled;
        bool m_FogEnabled;

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
            // If we just change the sample count, we don't want to reset iteration
            if (m_PathTracingSettings && m_CacheMaxIteration != m_PathTracingSettings.maximumSamples.value)
            {
                m_CacheMaxIteration = (uint) m_PathTracingSettings.maximumSamples.value;
                if (m_SubFrameManager.iteration >= m_CacheMaxIteration)
                    ResetPathTracing();
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
                ResetPathTracing();
        }

#endif // UNITY_EDITOR

        private void CheckDirtiness(HDCamera hdCamera)
        {
            if (m_SubFrameManager.isRecording)
            {
                return;
            }

            // Check camera clear mode dirtiness
            bool enabled = (hdCamera.clearColorMode == HDCameraData.ClearColorMode.Sky);
            if (enabled != m_CameraSkyEnabled)
            {
                m_CameraSkyEnabled = enabled;
                ResetPathTracing();
                return;
            }

            // Check camera resolution dirtiness
            if (hdCamera.actualWidth != m_CacheCameraWidth || hdCamera.actualHeight != m_CacheCameraHeight)
            {
                m_CacheCameraWidth = (uint) hdCamera.actualWidth;
                m_CacheCameraHeight = (uint) hdCamera.actualHeight;
                ResetPathTracing();
                return;
            }

            // Check camera matrix dirtiness
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
            {
                ResetPathTracing();
                return;
            }

            // Check fog dirtiness
            enabled = Fog.IsFogEnabled(hdCamera);
            if (enabled != m_FogEnabled)
            {
                m_FogEnabled = enabled;
                ResetPathTracing();
                return;
            }

            // Check materials dirtiness
            if (m_MaterialsDirty)
            {
                ResetPathTracing();
                m_MaterialsDirty = false;
                return;
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

            // Grab the acceleration structure and the list of HD lights for the target camera
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();

            if (!m_SubFrameManager.isRecording)
            {
                // If we are recording, the max iteration is set/overridden by the subframe manager, otherwise we read it from the path tracing volume
                m_SubFrameManager.subFrameCount = (uint)m_PathTracingSettings.maximumSamples.value;
            }

#if UNITY_HDRP_DXR_TESTS_DEFINE
			if (Application.isPlaying)
            	m_SubFrameManager.subFrameCount = 1;
#endif

            uint currentIteration = m_SubFrameManager.iteration;
            if (currentIteration < m_SubFrameManager.subFrameCount)
            {
			    // Define the shader pass to use for the path tracing pass
                cmd.SetRayTracingShaderPass(pathTracingShader, "PathTracingDXR");

                // Set the acceleration structure for the pass
                cmd.SetRayTracingAccelerationStructure(pathTracingShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                // Inject the ray-tracing sampling data
                cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambled256Tex);
                cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

                // Update the global constant buffer
                m_ShaderVariablesRayTracingCB._RaytracingNumSamples = (int)m_SubFrameManager.subFrameCount;
                m_ShaderVariablesRayTracingCB._RaytracingMinRecursion = m_PathTracingSettings.minimumDepth.value;
                m_ShaderVariablesRayTracingCB._RaytracingMaxRecursion = m_PathTracingSettings.maximumDepth.value;
                m_ShaderVariablesRayTracingCB._RaytracingIntensityClamp = m_PathTracingSettings.maximumIntensity.value;
                m_ShaderVariablesRayTracingCB._RaytracingSampleIndex = (int)m_SubFrameManager.iteration;
                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // LightLoop data
                cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
                cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());

                // Set the data for the ray miss
                cmd.SetRayTracingIntParam(pathTracingShader, HDShaderIDs._RaytracingCameraSkyEnabled, m_CameraSkyEnabled ? 1 : 0);
                cmd.SetRayTracingVectorParam(pathTracingShader, HDShaderIDs._RaytracingCameraClearColor, hdCamera.backgroundColorHDR);
                cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

                // Additional data for path tracing
                cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._RadianceTexture, m_RadianceTexture);
                cmd.SetRayTracingMatrixParam(pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, hdCamera.mainViewConstants.pixelCoordToViewDirWS);
                cmd.SetRayTracingVectorParam(pathTracingShader, HDShaderIDs._PathTracedDoFConstants, ComputeDoFConstants(hdCamera, m_PathTracingSettings));
                cmd.SetRayTracingVectorParam(pathTracingShader, HDShaderIDs._InvViewportScaleBias, HDUtils.ComputeInverseViewportScaleBias(hdCamera));

                // Run the computation
                cmd.DispatchRays(pathTracingShader, "RayGen", (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
            }
            RenderAccumulation(hdCamera, cmd, m_RadianceTexture, outputTexture, true);
        }
    }
}
