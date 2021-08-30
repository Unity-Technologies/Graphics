using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// The different ray count values that can be asked for.
    /// </summary>
    [GenerateHLSL]
    public enum RayCountValues
    {
        /// <summary>Ray count for the ray traced ambient occlusion effect.</summary>
        AmbientOcclusion = 0,
        /// <summary>Ray count for the ray traced directional shadow effect.</summary>
        ShadowDirectional = 1,
        /// <summary>Ray count for the ray traced point shadow effect.</summary>
        ShadowPointSpot = 2,
        /// <summary>Ray count for the ray traced area shadow effect.</summary>
        ShadowAreaLight = 3,
        /// <summary>Ray count for the forward ray traced indirect diffuse effect.</summary>
        DiffuseGI_Forward = 4,
        /// <summary>Ray count for the deferred ray traced indirect diffuse effect.</summary>
        DiffuseGI_Deferred = 5,
        /// <summary>Ray count for the forward ray traced reflection effect.</summary>
        ReflectionForward = 6,
        /// <summary>Ray count for the deferred ray traced reflection effect.</summary>
        ReflectionDeferred = 7,
        /// <summary>Ray count for the recursive rendering effect.</summary>
        Recursive = 8,
        /// <summary>Total number of ray count values that may be requested.</summary>
        Count = 9,
        /// <summary>Total number of entries.</summary>
        Total = 10
    }

    class RayCountManager
    {
        // Buffer that holds the reductions of the ray count
        ComputeBuffer m_ReducedRayCountBufferOutput = null;

        // CPU Buffer that holds the current values
        uint[] m_ReducedRayCountValues = new uint[(int)RayCountValues.Count];

        // HDRP Resources
        ComputeShader m_RayCountCS;

        // Flag that defines if ray counting is enabled for the current frame
        bool m_IsActive = false;
        bool m_RayTracingSupported = false;

        // Given that the requests are guaranteed to be executed in order we use a queue to store it
        Queue<AsyncGPUReadbackRequest> m_RayCountReadbacks = new Queue<AsyncGPUReadbackRequest>();

        public void Init(HDRenderPipelineRayTracingResources rayTracingResources)
        {
            // Keep track of the compute shader we are going to use
            m_RayCountCS = rayTracingResources.countTracedRays;

            // We only require 3 buffers (this supports a maximal size of 8192x8192)
            m_ReducedRayCountBufferOutput = new ComputeBuffer((int)RayCountValues.Count + 1, sizeof(uint));

            // Initialize the CPU  ray count (Optional)
            for (int i = 0; i < (int)RayCountValues.Count; ++i)
            {
                m_ReducedRayCountValues[i] = 0;
            }

            // By default, this is not active
            m_IsActive = false;
            m_RayTracingSupported = true;
        }

        public void Release()
        {
            CoreUtils.SafeRelease(m_ReducedRayCountBufferOutput);
        }

        public int RayCountIsEnabled()
        {
            return m_IsActive ? 1 : 0;
        }

        static public TextureHandle CreateRayCountTexture(RenderGraph renderGraph)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R16_UInt,
                slices = TextureXR.slices * (int)RayCountValues.Count,
                dimension = TextureDimension.Tex2DArray,
                clearBuffer = true,
                enableRandomWrite = true,
                name = "RayCountTextureDebug"
            });
        }

        class EvaluateRayCountPassData
        {
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle rayCountTexture;

            public ComputeBufferHandle reducedRayCountBuffer0;
            public ComputeBufferHandle reducedRayCountBuffer1;

            public ComputeBuffer reducedRayCountBufferOutput;

            public ComputeShader rayCountCS;
            public int rayCountKernel;
            public int clearKernel;
            public int width;
            public int height;

            public Queue<AsyncGPUReadbackRequest> rayCountReadbacks;
        }

        void PrepareEvaluateRayCountPassData(in RenderGraphBuilder builder, EvaluateRayCountPassData data, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle rayCountTexture)
        {
            data.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
            data.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
            data.rayCountTexture = builder.ReadTexture(rayCountTexture);

            data.reducedRayCountBuffer0 = builder.CreateTransientComputeBuffer(new ComputeBufferDesc((int)RayCountValues.Count * 256 * 256, sizeof(uint)));
            data.reducedRayCountBuffer1 = builder.CreateTransientComputeBuffer(new ComputeBufferDesc((int)RayCountValues.Count * 32 * 32, sizeof(uint)));
            data.reducedRayCountBufferOutput = m_ReducedRayCountBufferOutput;

            data.rayCountCS = m_RayCountCS;
            data.rayCountKernel = m_RayCountCS.FindKernel("TextureReduction");
            data.clearKernel = m_RayCountCS.FindKernel("ClearBuffer");
            data.width = hdCamera.actualWidth;
            data.height = hdCamera.actualHeight;

            data.rayCountReadbacks = m_RayCountReadbacks;
        }

        public void EvaluateRayCount(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle rayCountTexture)
        {
            if (m_IsActive)
            {
                using (var builder = renderGraph.AddRenderPass<EvaluateRayCountPassData>("RenderRayCountOverlay", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDebugOverlay)))
                {
                    PrepareEvaluateRayCountPassData(builder, passData, hdCamera, colorBuffer, depthBuffer, rayCountTexture);

                    builder.SetRenderFunc(
                        (EvaluateRayCountPassData data, RenderGraphContext ctx) =>
                        {
                            // Get the size of the viewport to process
                            int currentWidth = data.width;
                            int currentHeight = data.height;

                            var rayCountCS = data.rayCountCS;

                            // Grab the kernel that we will be using for the reduction
                            int currentKenel = data.rayCountKernel;

                            // Compute the dispatch dimensions
                            int areaTileSize = 32;
                            int dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                            int dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                            // Do we need three passes
                            if (dispatchHeight > 32 || dispatchWidth > 32)
                            {
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBuffer0);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 256 * (int)RayCountValues.Count);
                                int tileSize = 256 / 32;
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, tileSize, tileSize, 1);

                                // Bind the texture and the 256x256 buffer
                                ctx.cmd.SetComputeTextureParam(rayCountCS, currentKenel, HDShaderIDs._InputRayCountTexture, data.rayCountTexture);
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBuffer0);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 256 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, dispatchWidth, dispatchHeight, 1);

                                // Let's move to the next reduction pass
                                currentWidth /= 32;
                                currentHeight /= 32;

                                // Grab the kernel that we will be using for the reduction
                                currentKenel = rayCountCS.FindKernel("BufferReduction");

                                // Compute the dispatch dimensions
                                dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                                dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._InputRayCountBuffer, data.reducedRayCountBuffer0);
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBuffer1);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._InputBufferDimension, 256 * (int)RayCountValues.Count);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 32 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, dispatchWidth, dispatchHeight, 1);

                                // Let's move to the next reduction pass
                                currentWidth /= 32;
                                currentHeight /= 32;

                                // Compute the dispatch dimensions
                                dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                                dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._InputRayCountBuffer, data.reducedRayCountBuffer1);
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBufferOutput);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._InputBufferDimension, 32 * (int)RayCountValues.Count);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 1 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, dispatchWidth, dispatchHeight, 1);
                            }
                            else
                            {
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBuffer1);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 32 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, 1, 1, 1);

                                ctx.cmd.SetComputeTextureParam(rayCountCS, currentKenel, HDShaderIDs._InputRayCountTexture, data.rayCountTexture);
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBuffer1);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 32 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, dispatchWidth, dispatchHeight, 1);

                                // Let's move to the next reduction pass
                                currentWidth /= 32;
                                currentHeight /= 32;

                                // Grab the kernel that we will be using for the reduction
                                currentKenel = rayCountCS.FindKernel("BufferReduction");

                                // Compute the dispatch dimensions
                                dispatchWidth = Mathf.Max(1, (currentWidth + (areaTileSize - 1)) / areaTileSize);
                                dispatchHeight = Mathf.Max(1, (currentHeight + (areaTileSize - 1)) / areaTileSize);

                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._InputRayCountBuffer, data.reducedRayCountBuffer1);
                                ctx.cmd.SetComputeBufferParam(rayCountCS, currentKenel, HDShaderIDs._OutputRayCountBuffer, data.reducedRayCountBufferOutput);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._InputBufferDimension, 32 * (int)RayCountValues.Count);
                                ctx.cmd.SetComputeIntParam(rayCountCS, HDShaderIDs._OutputBufferDimension, 1 * (int)RayCountValues.Count);
                                ctx.cmd.DispatchCompute(rayCountCS, currentKenel, dispatchWidth, dispatchHeight, 1);
                            }

                            // Enqueue an Async read-back for the single value
                            AsyncGPUReadbackRequest singleReadBack = AsyncGPUReadback.Request(data.reducedRayCountBufferOutput, (int)RayCountValues.Count * sizeof(uint), 0);
                            data.rayCountReadbacks.Enqueue(singleReadBack);
                        });
                }
            }
        }

        public uint GetRaysPerFrame(RayCountValues rayCountValue)
        {
            if (!m_RayTracingSupported || !m_IsActive)
            {
                return 0;
            }
            else
            {
                while (m_RayCountReadbacks.Peek().done || m_RayCountReadbacks.Peek().hasError == true)
                {
                    // If this has an error, just skip it
                    if (!m_RayCountReadbacks.Peek().hasError)
                    {
                        // Grab the native array from this readback
                        NativeArray<uint> sampleCount = m_RayCountReadbacks.Peek().GetData<uint>();
                        for (int i = 0; i < (int)RayCountValues.Count; ++i)
                        {
                            m_ReducedRayCountValues[i] = sampleCount[i];
                        }
                    }
                    m_RayCountReadbacks.Dequeue();
                }

                if (rayCountValue != RayCountValues.Total)
                {
                    return m_ReducedRayCountValues[(int)rayCountValue];
                }
                else
                {
                    uint rayCount = 0;
                    for (int i = 0; i < (int)RayCountValues.Count; ++i)
                    {
                        rayCount += (uint)m_ReducedRayCountValues[i];
                    }
                    return rayCount;
                }
            }
        }
    }
}
