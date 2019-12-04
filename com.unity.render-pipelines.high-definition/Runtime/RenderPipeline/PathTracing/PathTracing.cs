#define ENABLE_STOPWATCH
#define ENABLE_IMAGE_CAPTURE

using System;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Path Tracing (Preview)")]
    public sealed class PathTracing : VolumeComponent
    {
        [Tooltip("Enables path tracing (thus disabling most other passes).")]
        public BoolParameter enable = new BoolParameter(false);

        [Tooltip("Defines the layers that path tracing should include.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        [Tooltip("Defines the maximum number of paths cast within each pixel, over time (one per frame).")]
        public ClampedIntParameter maximumSamples = new ClampedIntParameter(256, 1, 4096);

        [Tooltip("Defines the minimum number of bounces for each path.")]
        public ClampedIntParameter minimumDepth = new ClampedIntParameter(1, 1, 10);

        [Tooltip("Defines the maximum number of bounces for each path.")]
        public ClampedIntParameter maximumDepth = new ClampedIntParameter(4, 1, 10);

        [Tooltip("Defines the maximum intensity value computed for a path.")]
        public ClampedFloatParameter maximumIntensity = new ClampedFloatParameter(10f, 0f, 100f);
    }
    public partial class HDRenderPipeline
    {
        const string m_PathTracingRayGenShaderName = "RayGen";
        uint currentIteration = 0;
        uint maxIteration = 0;

        RTHandle m_VarianceTexture = null;
        RTHandle m_MaxVariance = null;
        RTHandle m_ScratchBuffer = null;

        Stopwatch m_Timer = new Stopwatch();
        Queue<AsyncGPUReadbackRequest> _requests = new Queue<AsyncGPUReadbackRequest>();

        public void InitPathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += UndoRecordedCallback;
            Undo.undoRedoPerformed += UndoPerformedCallback;
#endif // UNITY_EDITOR

            m_VarianceTexture = RTHandles.Alloc(Vector2.one, slices: TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex2DArray, enableRandomWrite: true, useMipMap: false, name: "VarianceTexture");
            m_MaxVariance = RTHandles.Alloc(Vector2.one, slices: TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureDimension.Tex2DArray, enableRandomWrite: true, useMipMap: false, name: "MaxVarianceTexture");
            m_ScratchBuffer = RTHandles.Alloc(Vector2.one, slices: TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureDimension.Tex2DArray, enableRandomWrite: true, useMipMap: false, name: "ScratchBuffer");
        }

        public void ReleasePathTracing()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= UndoRecordedCallback;
            Undo.undoRedoPerformed -= UndoPerformedCallback;
#endif // UNITY_EDITOR

            RTHandles.Release(m_VarianceTexture);
            RTHandles.Release(m_MaxVariance);
            RTHandles.Release(m_ScratchBuffer);
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

        public void RenderPathTracing(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
#if ENABLE_STOPWATCH
            if (currentIteration == 1)
            {
                m_Timer.Reset();
                m_Timer.Start();
            }
#endif

            RayTracingShader pathTracingShader = m_Asset.renderPipelineRayTracingResources.pathTracing;
            PathTracing pathTracingSettings = VolumeManager.instance.stack.GetComponent<PathTracing>();

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
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();
            RayTracingSettings rayTracingSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();

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

            // Adaptive sampling resources 
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._VarianceTexture, m_VarianceTexture);
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._MaxVariance, m_MaxVariance);
            cmd.SetRayTracingTextureParam(pathTracingShader, HDShaderIDs._ScratchBuffer, m_ScratchBuffer);

            // Run the computation
            cmd.DispatchRays(pathTracingShader, m_PathTracingRayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

#if ENABLE_STOPWATCH
            // TODO: right now we don't detect "early" convergence with adaptive sampling
            if (currentIteration == pathTracingSettings.maximumSamples.value)
            {
                m_Timer.Stop();
                
                TimeSpan ts = m_Timer.Elapsed;
                Debug.Log($"Congergence time: {ts.TotalSeconds}");
#if ENABLE_IMAGE_CAPTURE
                // request an async readback of the accumulation buffer
                _requests.Enqueue(AsyncGPUReadback.Request(history));
#endif
            }
#if ENABLE_IMAGE_CAPTURE
            // read the data when the async transfer is done
            while (_requests.Count > 0)
            {
                var req = _requests.Peek();

                if (req.hasError)
                {
                    Debug.Log("GPU readback error detected.");
                    _requests.Dequeue();
                }
                else if (req.done)
                {
                    var buffer = req.GetData<Vector4>();
                    SaveEXRFile(buffer, hdCamera.camera.pixelWidth, hdCamera.camera.pixelHeight);
                    _requests.Dequeue();
                }
                else
                {
                    break;
                }
            }
#endif //ENABLE_IMAGE_CAPTURE
#endif //ENABLE_STOPWATCH
        }

        void SaveEXRFile(NativeArray<Vector4> buffer, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            tex.SetPixelData(buffer.ToArray(), 0);
            tex.Apply();
            File.WriteAllBytes("converged.exr", ImageConversion.EncodeToEXR(tex));
            Debug.Log("Wrote converged image to disk.");

        }
    }
}
