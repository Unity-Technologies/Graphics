using UnityEngine.Rendering;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class RayCountManager
    {
        // Indices of the values that we can query
        public enum RayCountValues
        {
            AmbientOcclusion = 0,
            Reflection = 1,
            AreaShadow = 2,
            Total = 3
        }
        // Texture that keeps track of the ray count per pixel
        public RTHandleSystem.RTHandle rayCountTexture { get { return m_RayCountTexture; } }
        RTHandleSystem.RTHandle m_RayCountTexture = null;

        // Buffer that holds the reductions of the ray count
        ComputeBuffer m_ReducedRayCountBuffer0 = null;
        ComputeBuffer m_ReducedRayCountBuffer1 = null;
        ComputeBuffer m_ReducedRayCountBuffer2 = null;

        // CPU Buffer that holds the current values
        uint[] m_ReducedRayCountValues = new uint[4];
        
        // HDRP Resources
        DebugDisplaySettings m_DebugDisplaySettings;
        RenderPipelineResources m_PipelineResources;

        // Given that the requests are guaranteed to be executed in order we use a queue to store it
        Queue<AsyncGPUReadbackRequest> rayCountReadbacks = new Queue<AsyncGPUReadbackRequest>();

        public void Init(RenderPipelineResources renderPipelineResources, DebugDisplaySettings currentDebugDisplaySettings)
        {
            // Keep track of the external resources
            m_DebugDisplaySettings = currentDebugDisplaySettings;
            m_PipelineResources = renderPipelineResources;

            m_RayCountTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32G32B32A32_UInt, enableRandomWrite: true, useMipMap: false, name: "RayCountTexture");

            // We only require 3 buffers (this supports a maximal size of 8192x8192)
            m_ReducedRayCountBuffer0 = new ComputeBuffer(4 * 256 * 256, sizeof(uint));
            m_ReducedRayCountBuffer1 = new ComputeBuffer(4 * 32 * 32, sizeof(uint));
            m_ReducedRayCountBuffer2 = new ComputeBuffer(4, sizeof(uint));

            // Initialize the cpu ray count (Optional)
            for(int i = 0; i < 4; ++i)
            {
                m_ReducedRayCountValues[i] = 0;
            }
        }

        public void Release()
        {
            RTHandles.Release(m_RayCountTexture);
            CoreUtils.SafeRelease(m_ReducedRayCountBuffer0);
            CoreUtils.SafeRelease(m_ReducedRayCountBuffer1);
            CoreUtils.SafeRelease(m_ReducedRayCountBuffer2);
        }

        public void ClearRayCount(CommandBuffer cmd, HDCamera camera)
        {
            // We only want to do the clears only if the debug display is active
            if (m_DebugDisplaySettings.data.countRays)
            {
                // Get the compute shader to use
                ComputeShader countCompute = m_PipelineResources.shaders.countTracedRays;

                // Grab the kernel that we will be using for the clear
                int currentKenel = countCompute.FindKernel("ClearBuffer");

                // We only clear the 256x256 texture, the clear will then implicitly propagate to the lower resolutions
                cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer0);
                cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 256);
                int tileSize = 256 / 32;
                cmd.DispatchCompute(countCompute, currentKenel, tileSize, tileSize, 1);

                // Clear the ray count texture (that ensures that we don't have to check what we are reading while we reduce)
                HDUtils.SetRenderTarget(cmd, camera, m_RayCountTexture, ClearFlag.Color);
            }
        }

        public int RayCountIsEnabled()
        {
            return m_DebugDisplaySettings.data.countRays ? 1 : 0;
        }

        public void EvaluateRayCount(CommandBuffer cmd, HDCamera camera)
        {
            if (m_DebugDisplaySettings.data.countRays)
            {
                using (new ProfilingSample(cmd, "Raytracing Debug Overlay", CustomSamplerId.RaytracingDebug.GetSampler()))
                {
                    // Get the size of the viewport to process
                    int currentWidth = camera.actualWidth;
                    int currentHeight = camera.actualHeight;

                    // Get the compute shader
                    ComputeShader countCompute = m_PipelineResources.shaders.countTracedRays;

                    // Grab the kernel that we will be using for the reduction
                    int currentKenel = countCompute.FindKernel("TextureReduction");

                    // Compute the dispatch dimensions
                    int areaTileSize = 32;
                    int dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                    int dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                    // Do we need three passes
                    if (dispatchHeight > 32  || dispatchWidth > 32)
                    {
                        // Bind the texture and the 256x256 buffer
                        cmd.SetComputeTextureParam(countCompute, currentKenel, HDShaderIDs._InputRayCountTexture, m_RayCountTexture);
                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer0);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 256);
                        cmd.DispatchCompute(countCompute, currentKenel, dispatchWidth, dispatchHeight, 1);

                        // Let's move to the next reduction pass
                        currentWidth /= 32;
                        currentHeight /= 32;

                        // Grab the kernel that we will be using for the reduction
                        currentKenel = countCompute.FindKernel("BufferReduction");

                        // Compute the dispatch dimensions
                        dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                        dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._InputRayCountBuffer, m_ReducedRayCountBuffer0);
                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer1);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._InputBufferDimension, 256);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 32);
                        cmd.DispatchCompute(countCompute, currentKenel, dispatchWidth, dispatchHeight, 1);

                        // Let's move to the next reduction pass
                        currentWidth /= 32;
                        currentHeight /= 32;

                        // Compute the dispatch dimensions
                        dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                        dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._InputRayCountBuffer, m_ReducedRayCountBuffer1);
                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer2);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._InputBufferDimension, 32);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 1);
                        cmd.DispatchCompute(countCompute, currentKenel, dispatchWidth, dispatchHeight, 1);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(countCompute, currentKenel, HDShaderIDs._InputRayCountTexture, m_RayCountTexture);
                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer1);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 32);
                        cmd.DispatchCompute(countCompute, currentKenel, dispatchWidth, dispatchHeight, 1);

                        // Let's move to the next reduction pass
                        currentWidth /= 32;
                        currentHeight /= 32;

                        // Grab the kernel that we will be using for the reduction
                        currentKenel = countCompute.FindKernel("BufferReduction");

                        // Compute the dispatch dimensions
                        dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                        dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._InputRayCountBuffer, m_ReducedRayCountBuffer1);
                        cmd.SetComputeBufferParam(countCompute, currentKenel, HDShaderIDs._OutputRayCountBuffer, m_ReducedRayCountBuffer2);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._InputBufferDimension, 32);
                        cmd.SetComputeIntParam(countCompute, HDShaderIDs._OutputBufferDimension, 1);
                        cmd.DispatchCompute(countCompute, currentKenel, dispatchWidth, dispatchHeight, 1);
                    }

                    // Enqueue an Async read-back for the single value
                    AsyncGPUReadbackRequest singleReadBack = AsyncGPUReadback.Request(m_ReducedRayCountBuffer2, 4 * sizeof(uint), 0);
                    rayCountReadbacks.Enqueue(singleReadBack);

                }
            }
        }

        public float GetRaysPerFrame(RayCountValues rayCountValue)
        {
            if (!m_DebugDisplaySettings.data.countRays)
            {
                return 0.0f;
            }
            else
            {
                while(rayCountReadbacks.Peek().done || rayCountReadbacks.Peek().hasError ==  true)
                {
                    if (rayCountReadbacks.Peek().done)
                    {
                        // Grab the native array from this readback
                        NativeArray<uint> sampleCount = rayCountReadbacks.Peek().GetData<uint>();
                        for(int i = 0; i < 4; ++i)
                        {
                            m_ReducedRayCountValues[i] = sampleCount[i];
                        }
                    }
                    rayCountReadbacks.Dequeue();
                }

                return m_ReducedRayCountValues[(int)rayCountValue];
            }
        }


    }
#endif
}
