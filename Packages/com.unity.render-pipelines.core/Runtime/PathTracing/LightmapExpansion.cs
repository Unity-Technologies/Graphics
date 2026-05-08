using Unity.Mathematics;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using static UnityEngine.PathTracing.Lightmapping.LightmapIntegrationHelpers;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal static class ExpansionShaderIDs
    {
        public static readonly int GBuffer = Shader.PropertyToID("g_GBuffer");
        public static readonly int DestinationTexture = Shader.PropertyToID("g_DestinationTexture");
        public static readonly int DestinationX = Shader.PropertyToID("g_DestinationX");
        public static readonly int DestinationY = Shader.PropertyToID("g_DestinationY");
        public static readonly int ExpandedTexelSampleWidth = Shader.PropertyToID("g_ExpandedTexelSampleWidth");
        public static readonly int Float4Buffer = Shader.PropertyToID("g_Float4Buffer");
        public static readonly int Float4BufferLength = Shader.PropertyToID("g_Float4BufferLength");
        public static readonly int BinaryBufferSize = Shader.PropertyToID("g_BinaryBufferSize");
        public static readonly int BinaryGroupSize = Shader.PropertyToID("g_BinaryGroupSize");
        public static readonly int SourceBuffer = Shader.PropertyToID("g_SourceBuffer");
        public static readonly int SourceStride = Shader.PropertyToID("g_SourceStride");
        public static readonly int GBufferLength = Shader.PropertyToID("g_GBufferLength");
        public static readonly int CompactedGBuffer = Shader.PropertyToID("g_CompactedGBuffer");
        public static readonly int CompactedGBufferLength = Shader.PropertyToID("g_CompactedGBufferLength");
        public static readonly int InstanceWidth = Shader.PropertyToID("g_InstanceWidth");
        public static readonly int ThreadGroupSizeX = Shader.PropertyToID("g_ThreadGroupSizeX");
        public static readonly int AccumulationDispatchBuffer = Shader.PropertyToID("g_AccumulationDispatchBuffer");
        public static readonly int ClearDispatchBuffer = Shader.PropertyToID("g_ClearDispatchBuffer");
        public static readonly int CopyDispatchBuffer = Shader.PropertyToID("g_CopyDispatchBuffer");
        public static readonly int ReduceDispatchBuffer = Shader.PropertyToID("g_ReduceDispatchBuffer");
        public static readonly int ChunkSize = Shader.PropertyToID("g_ChunkSize");
        public static readonly int ChunkOffsetX = Shader.PropertyToID("g_ChunkOffsetX");
        public static readonly int ChunkOffsetY = Shader.PropertyToID("g_ChunkOffsetY");
        public static readonly int PassSampleCount = Shader.PropertyToID("g_PassSampleCount");
        public static readonly int SampleOffset = Shader.PropertyToID("g_SampleOffset");
    }

    internal static class ExpansionHelpers
    {
        /// <summary>
        /// Clears the expanded output buffer to zero before the accumulation loop begins.
        /// </summary>
        static readonly ProfilerMarker k_ClearExpanded =
            new ProfilerMarker(ProfilerCategory.Render, "Clear (Expanded)",
                MarkerFlags.Default | MarkerFlags.SampleGPU);

        /// <summary>
        /// Traces rays from each lightmap texel into the scene to populate the GBuffer with
        /// world-space hit data. This ray-tracing dispatch is a prerequisite for all
        /// subsequent accumulation passes.
        /// </summary>
        static readonly ProfilerMarker k_UVSampling =
            new ProfilerMarker(ProfilerCategory.Render, "UV Sampling",
                MarkerFlags.Default | MarkerFlags.SampleGPU);

        /// <summary>
        /// Compacts the GBuffer by stream-compacting out invalid (missed-ray) texels so the
        /// accumulation dispatch only processes valid hits.
        /// </summary>
        static readonly ProfilerMarker k_CompactGBuffer =
            new ProfilerMarker(ProfilerCategory.Render, "Compact GBuffer",
                MarkerFlags.Default | MarkerFlags.SampleGPU);

        /// <summary>
        /// Performs one stage of the parallel-reduction sum over expanded sample groups.
        /// Called repeatedly in a loop where each invocation halves the remaining group count
        /// until all samples within each texel are summed.
        /// </summary>
        static readonly ProfilerMarker k_BinarySum =
            new ProfilerMarker(ProfilerCategory.Render, "Binary Sum",
                MarkerFlags.Default | MarkerFlags.SampleGPU);

        /// <summary>
        /// Copies the accumulated, reduced texel values from the expanded buffer into the
        /// final lightmap texture at the correct instance offset and chunk bounds.
        /// </summary>
        static readonly ProfilerMarker k_CopyToLightmap =
            new ProfilerMarker(ProfilerCategory.Render, "Copy to Lightmap",
                MarkerFlags.Default | MarkerFlags.SampleGPU);

        static internal int PopulateAccumulationIndirectDispatch(CommandBuffer cmd, ComputeShader populateShader, int populateKernel, uint expandedSampleWidth, GraphicsBuffer compactedGbufferLength, GraphicsBuffer accumulationDispatchBuffer)
        {
            cmd.SetComputeIntParam(populateShader, ExpansionShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            cmd.SetComputeBufferParam(populateShader, populateKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGbufferLength);
            cmd.SetComputeBufferParam(populateShader, populateKernel, ExpansionShaderIDs.AccumulationDispatchBuffer, accumulationDispatchBuffer);
            cmd.DispatchCompute(populateShader, populateKernel, 1, 1, 1);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, accumulationDispatchBuffer, "accumulationDispatchBuffer", LogBufferType.UInt);
            return 1;
        }

        static internal int PopulateClearExpandedOutputIndirectDispatch(CommandBuffer cmd, ComputeShader populateClearDispatch, int populateClearDispatchKernel, uint clearThreadGroupSizeX, uint expandedSampleWidth, GraphicsBuffer compactedGBufferLength, GraphicsBuffer clearDispatchBuffer)
        {
            // Populate the expanded clear indirect dispatch buffer - using the compacted size.
            cmd.SetComputeIntParam(populateClearDispatch, ExpansionShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            cmd.SetComputeIntParam(populateClearDispatch, ExpansionShaderIDs.ThreadGroupSizeX, (int)clearThreadGroupSizeX);
            cmd.SetComputeBufferParam(populateClearDispatch, populateClearDispatchKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            cmd.SetComputeBufferParam(populateClearDispatch, populateClearDispatchKernel, ExpansionShaderIDs.ClearDispatchBuffer, clearDispatchBuffer);
            cmd.DispatchCompute(populateClearDispatch, populateClearDispatchKernel, 1, 1, 1);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, clearDispatchBuffer, "clearDispatchBuffer", LogBufferType.UInt);
            return 1;
        }

        static internal int ClearExpandedOutput(CommandBuffer cmd, ComputeShader clearExpandedOutput, int clearExpandedOutputKernel, GraphicsBuffer expandedOutput, GraphicsBuffer clearDispatchBuffer)
        {
            Debug.Assert(expandedOutput.stride == 16);
            // Clear the output buffers.
            cmd.SetComputeIntParam(clearExpandedOutput, ExpansionShaderIDs.Float4BufferLength, expandedOutput.count);
            cmd.SetComputeBufferParam(clearExpandedOutput, clearExpandedOutputKernel, ExpansionShaderIDs.Float4Buffer, expandedOutput);
            cmd.BeginSample(k_ClearExpanded);
            cmd.DispatchCompute(clearExpandedOutput, clearExpandedOutputKernel, clearDispatchBuffer, 0);
            cmd.EndSample(k_ClearExpanded);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, expandedOutput, "expandedOutput", LogBufferType.Float4);
            return 1;
        }

        static internal void GenerateGBuffer(
            CommandBuffer cmd,
            IRayTracingShader gBufferShader,
            GraphicsBuffer gBuffer,
            GraphicsBuffer traceScratchBuffer,
            SamplingResources samplingResources,
            UVAccelerationStructure uvAS,
            UVFallbackBuffer uvFallbackBuffer,
            GraphicsBuffer compactedGBufferLength,
            GraphicsBuffer compactedTexelIndices,
            Vector2Int instanceTexelOffset,
            uint2 chunkOffset,
            uint chunkSize,
            uint expandedSampleWidth,
            uint passSampleCount,
            uint sampleOffset,
            AntiAliasingType aaType,
            uint superSampleWidth       // the width of the superSampleWidth x superSampleWidth grid used when not using stochastic anti-aliasing
            )
        {
            var stochasticAntiAliasing = aaType == AntiAliasingType.Stochastic;

            // bind buffers
            gBufferShader.SetAccelerationStructure(cmd, "g_UVAccelStruct", uvAS._uvAS);
            gBufferShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            SamplingResources.Bind(cmd, samplingResources);
            uvFallbackBuffer.BindChunked(cmd, gBufferShader, instanceTexelOffset, chunkOffset, chunkSize);
            gBufferShader.SetBufferParam(cmd, ExpansionShaderIDs.CompactedGBuffer, compactedTexelIndices);
            gBufferShader.SetBufferParam(cmd, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            gBufferShader.SetIntParam(cmd, ExpansionShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            gBufferShader.SetIntParam(cmd, ExpansionShaderIDs.PassSampleCount, (int)passSampleCount);
            gBufferShader.SetIntParam(cmd, ExpansionShaderIDs.SampleOffset, (int)sampleOffset);

            // set antialiasing parameters
            gBufferShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.StochasticAntialiasing, stochasticAntiAliasing ? 1 : 0);
            gBufferShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SuperSampleWidth, (int)superSampleWidth);

            // dispatch the shader
            Debug.Assert(gBufferShader is HardwareRayTracingShader || GraphicsHelpers.DivUp((int)chunkSize, gBufferShader.GetThreadGroupSizes().x) < 65536, "Chunk size is too large for the shader to handle.");
            cmd.BeginSample(k_UVSampling);
            gBufferShader.Dispatch(cmd, traceScratchBuffer, chunkSize * expandedSampleWidth, 1, 1);
            cmd.EndSample(k_UVSampling);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, gBuffer, "gBuffer", LogBufferType.HitEntry);
        }

        static internal float2[] DebugGBuffer(CommandBuffer cmd, BakeInstance instance, LightmappingContext lightmappingContext, uint expandedSampleWidth, uint passSampleCount)
        {
            // Get the length of the compacted GBuffer length to know how many samples there are.
            uint[] compactedLength = new uint[1];
            GraphicsHelpers.Flush(cmd);
            lightmappingContext.IntegratorContext.CompactedGBufferLength.GetData(compactedLength);
            uint sampleCount = compactedLength[0] * expandedSampleWidth;

            // Run the debug shader to retrieve the samples.
            var lightmapSamplesExpanded = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(sampleCount), sizeof(float) * 2);
            var instanceGeometryIndex = instance.IsProceduralTerrain
                ? -1
                : lightmappingContext.World.PathTracingWorld.GetAccelerationStructure().GeometryPool.GetInstanceGeometryIndex(instance.Mesh);
            var terrainIndex = instance.TerrainIndex;
            lightmappingContext.IntegratorContext.GBufferDebugShader.Accumulate(
                    cmd,
                    instance.LocalToWorldMatrix,
                    instance.LocalToWorldMatrixNormals,
                    instanceGeometryIndex,
                    terrainIndex,
                    lightmappingContext.World.PathTracingWorld,
                    lightmappingContext.GBuffer,
                    expandedSampleWidth,
                    lightmapSamplesExpanded,
                    lightmappingContext.IntegratorContext.CompactedGBufferLength
                );
            GraphicsHelpers.Flush(cmd);

            // Retrieve the samples from the GPU.
            float2[] uvSampleData = new float2[sampleCount];
            lightmapSamplesExpanded.GetData(uvSampleData, 0, 0, (int)sampleCount);
            lightmapSamplesExpanded.Dispose();

            // Filter the samples
            float4 scaleAndOffset = instance.SourceLightmapST;
            System.Collections.Generic.List<float2> filteredSamples = new System.Collections.Generic.List<float2>();
            for (int i = 0; i < uvSampleData.Length; ++i)
            {
                // For some passes the expanded buffer is not filled completely and we need to account for that.
                var localSampleIndex = i % expandedSampleWidth;
                if (localSampleIndex < passSampleCount)
                {
                    // this is a valid sample - apply lightmap ST to map into lightmap space
                    var sample = uvSampleData[i];
                    var transformedUV = sample * scaleAndOffset.xy + scaleAndOffset.zw;
                    filteredSamples.Add(transformedUV);
                }
            }

            return filteredSamples.ToArray();
        }

        static internal int CompactGBuffer(CommandBuffer cmd, ComputeShader compactGBuffer, int compactGBufferKernel, uint instanceWidth, uint chunkSize, uint2 chunkOffset, UVFallbackBuffer uvFallbackBuffer, GraphicsBuffer compactedGBufferLength, GraphicsBuffer compactedTexelIndices)
        {
            compactGBuffer.GetKernelThreadGroupSizes(compactGBufferKernel, out uint gbuf_x, out _, out _);
            cmd.SetBufferData(compactedGBufferLength, new uint[] { 0 });
            cmd.SetComputeIntParam(compactGBuffer, ExpansionShaderIDs.ChunkSize, (int)chunkSize);
            cmd.SetComputeIntParam(compactGBuffer, ExpansionShaderIDs.InstanceWidth, (int)instanceWidth);
            cmd.SetComputeIntParam(compactGBuffer, ExpansionShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            cmd.SetComputeIntParam(compactGBuffer, ExpansionShaderIDs.ChunkOffsetY, (int)chunkOffset.y);
            cmd.SetComputeTextureParam(compactGBuffer, compactGBufferKernel, UVFallbackBufferBuilderShaderIDs.UvFallback, uvFallbackBuffer.UVFallbackRT);
            cmd.SetComputeBufferParam(compactGBuffer, compactGBufferKernel, ExpansionShaderIDs.CompactedGBuffer, compactedTexelIndices);
            cmd.SetComputeBufferParam(compactGBuffer, compactGBufferKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            cmd.BeginSample(k_CompactGBuffer);
            cmd.DispatchCompute(compactGBuffer, compactGBufferKernel, GraphicsHelpers.DivUp((int)chunkSize, gbuf_x), 1, 1);
            cmd.EndSample(k_CompactGBuffer);

            //System.Console.WriteLine($"compactGBufferThreadGroupSizeX: {gbuf_x}");
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, compactedGBufferLength, "compactedGBufferLength", LogBufferType.UInt);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, compactedTexelIndices, "compactedTexelIndices", LogBufferType.UInt);
            return 1;
        }

        static internal int PopulateReduceExpandedOutputIndirectDispatch(CommandBuffer cmd, ComputeShader populateReduceExpandedOutput, int populateReduceExpandedOutputKernel, uint reduceThreadGroupSizeX, uint expandedSampleWidth, GraphicsBuffer compactedGBufferLength, GraphicsBuffer reduceDispatchBuffer)
        {
            // Populate the reduce copy indirect dispatch buffer - using the compacted size.
            cmd.SetComputeIntParam(populateReduceExpandedOutput, ExpansionShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            cmd.SetComputeIntParam(populateReduceExpandedOutput, ExpansionShaderIDs.ThreadGroupSizeX, (int)reduceThreadGroupSizeX);
            cmd.SetComputeBufferParam(populateReduceExpandedOutput, populateReduceExpandedOutputKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            cmd.SetComputeBufferParam(populateReduceExpandedOutput, populateReduceExpandedOutputKernel, ExpansionShaderIDs.ReduceDispatchBuffer, reduceDispatchBuffer);
            cmd.DispatchCompute(populateReduceExpandedOutput, populateReduceExpandedOutputKernel, 1, 1, 1);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, reduceDispatchBuffer, "reduceDispatchBuffer");
            return 1;
        }

        static internal int ReduceExpandedOutput(CommandBuffer cmd, ComputeShader binaryGroupSumLeftShader, int binaryGroupSumLeftKernel, GraphicsBuffer expandedOutput, int expandedDispatchSize, uint expandedSampleWidth, GraphicsBuffer reduceDispatch)
        {
            Debug.Assert(math.ispow2(expandedSampleWidth));

            // the expandedOutput buffer contains groups of expandedSampleWidth samples
            // do binary summation within the sample groups ending up with summed samples in the leftmost sample for each texel
            cmd.SetComputeBufferParam(binaryGroupSumLeftShader, binaryGroupSumLeftKernel, ExpansionShaderIDs.Float4Buffer, expandedOutput);
            cmd.SetComputeIntParam(binaryGroupSumLeftShader, ExpansionShaderIDs.BinaryBufferSize, expandedDispatchSize);
            int groupSize = 1;
            int dispatches = 0;
            while (groupSize < expandedSampleWidth)
            {
                cmd.SetComputeIntParam(binaryGroupSumLeftShader, ExpansionShaderIDs.BinaryGroupSize, groupSize);
                cmd.BeginSample(k_BinarySum);
                cmd.DispatchCompute(binaryGroupSumLeftShader, binaryGroupSumLeftKernel, reduceDispatch, 0);
                cmd.EndSample(k_BinarySum);
                groupSize *= 2;
                dispatches++;
            }
            return dispatches;
        }


        static internal int PopulateCopyToLightmapIndirectDispatch(CommandBuffer cmd, ComputeShader populateCopyToLightmap, int populateCopyToLightmapKernel, uint copyThreadGroupSizeX, GraphicsBuffer compactedGBufferLength, GraphicsBuffer copyDispatch)
        {
            // Populate the expanded copy indirect dispatch buffer - using the compacted size.
            cmd.SetComputeIntParam(populateCopyToLightmap, ExpansionShaderIDs.ThreadGroupSizeX, (int)copyThreadGroupSizeX);
            cmd.SetComputeBufferParam(populateCopyToLightmap, populateCopyToLightmapKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            cmd.SetComputeBufferParam(populateCopyToLightmap, populateCopyToLightmapKernel, ExpansionShaderIDs.CopyDispatchBuffer, copyDispatch);
            cmd.DispatchCompute(populateCopyToLightmap, populateCopyToLightmapKernel, 1, 1, 1);
            //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, copyDispatch, "copyDispatch");
            return 1;
        }

        static internal int CopyToLightmap(CommandBuffer cmd, ComputeShader copyToLightmap, int copyToLightmapKernel, uint expandedSampleWidth, int instanceWidth, Vector2Int instanceTexelOffset, uint2 chunkOffset, GraphicsBuffer compactedGBufferLength, GraphicsBuffer compactedTexelIndices, GraphicsBuffer expandedOutput, GraphicsBuffer copyDispatch, RenderTexture output)
        {
            cmd.SetComputeBufferParam(copyToLightmap, copyToLightmapKernel, ExpansionShaderIDs.SourceBuffer, expandedOutput);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.SourceStride, (int)expandedSampleWidth);
            cmd.SetComputeTextureParam(copyToLightmap, copyToLightmapKernel, ExpansionShaderIDs.DestinationTexture, output);
            cmd.SetComputeBufferParam(copyToLightmap, copyToLightmapKernel, ExpansionShaderIDs.CompactedGBuffer, compactedTexelIndices);
            cmd.SetComputeBufferParam(copyToLightmap, copyToLightmapKernel, ExpansionShaderIDs.CompactedGBufferLength, compactedGBufferLength);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.InstanceWidth, instanceWidth);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.DestinationX, instanceTexelOffset.x);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.DestinationY, instanceTexelOffset.y);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            cmd.SetComputeIntParam(copyToLightmap, ExpansionShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            copyToLightmap.GetKernelThreadGroupSizes(copyToLightmapKernel, out uint copyx, out uint _, out _);
            cmd.BeginSample(k_CopyToLightmap);
            cmd.DispatchCompute(copyToLightmap, copyToLightmapKernel, copyDispatch, 0);
            cmd.EndSample(k_CopyToLightmap);
            return 1;
        }
    }
}
