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
        /// Defines the minimum number of bounces for each path.
        /// </summary>
        [Tooltip("Defines the minimum number of bounces for each path.")]
        public ClampedIntParameter minimumDepth = new ClampedIntParameter(1, 1, 10);

        /// <summary>
        /// Defines the maximum number of bounces for each path.
        /// </summary>
        [Tooltip("Defines the maximum number of bounces for each path.")]
        public ClampedIntParameter maximumDepth = new ClampedIntParameter(4, 1, 10);

        /// <summary>
        /// Defines the maximum intensity value computed for a path segment.
        /// </summary>
        [Tooltip("Defines the maximum intensity value computed for a path segment.")]
        public ClampedFloatParameter maximumIntensity = new ClampedFloatParameter(10f, 0f, 100f);

        public PathTracing()
        {
            displayName = "Path Tracing (Preview)";
        }
    }
    public partial class HDRenderPipeline
    {
        const string m_PathTracingRayGenShaderName = "RayGen";
        uint currentIteration = 0;
#if UNITY_EDITOR
        uint maxIteration = 0;
#endif // UNITY_EDITOR

        void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += UndoRecordedCallback;
            Undo.undoRedoPerformed += UndoPerformedCallback;
#endif // UNITY_EDITOR
        }

        void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= UndoRecordedCallback;
            Undo.undoRedoPerformed -= UndoPerformedCallback;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR

        private void ResetIteration()
        {
            // If we just changed the sample count, we don't want to reset the iteration
            PathTracing pathTracingSettings = VolumeManager.instance.stack.GetComponent<PathTracing>();
            if (maxIteration != pathTracingSettings.maximumSamples.value)
                maxIteration = (uint) pathTracingSettings.maximumSamples.value;
            else
                currentIteration = 0;
        }

        private UndoPropertyModification[] UndoRecordedCallback(UndoPropertyModification[] modifications)
        {
            ResetIteration();

            return modifications;
        }

        private void UndoPerformedCallback()
        {
            ResetIteration();
        }

#endif // UNITY_EDITOR

        private void CheckCameraChange(HDCamera hdCamera)
        {
            if (hdCamera.mainViewConstants.nonJitteredViewProjMatrix != (hdCamera.mainViewConstants.prevViewProjMatrix))
                currentIteration = 0;
        }

        static RTHandle PathTracingHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("PathTracingHistoryBuffer{0}", frameIndex));
        }

        void RenderPathTracing(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            RayTracingShader pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            PathTracing pathTracingSettings = hdCamera.volumeStack.GetComponent<PathTracing>();

            // Check the validity of the state before moving on with the computation
            if (!pathTracingShader || !pathTracingSettings.enable.value)
                return;

            CheckCameraChange(hdCamera);

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
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rayTracingSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingNumSamples, pathTracingSettings.maximumSamples.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMinRecursion, pathTracingSettings.minimumDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, pathTracingSettings.maximumDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingIntensityClamp, pathTracingSettings.maximumIntensity.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingCameraNearPlane, hdCamera.camera.nearClipPlane);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._CameraColorTextureRW, outputTexture);
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, (int)currentIteration++);

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
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

            // Additional data for path tracing
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._AccumulatedFrameTexture, history);
            cmd.SetRayTracingMatrixParam(pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, hdCamera.mainViewConstants.pixelCoordToViewDirWS);

            // Run the computation
            cmd.DispatchRays(pathTracingShader, m_PathTracingRayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
        }
    }
}
