using System;
using Unity.Mathematics;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal static class BakeLightmapDriver
    {
        public class LightmapBakeState
        {
            public uint SampleIndex;
            public UInt64 TexelIndex;

            public void Init()
            {
                SampleIndex = 0;
                TexelIndex = 0;
            }

            public void Tick(uint passSampleCount, uint totalSampleCount, UInt64 chunkTexelCount, UInt64 totalTexelCount, out bool instanceIsDone, out bool chunkIsDone)
            {
                instanceIsDone = false;
                chunkIsDone = false;

                SampleIndex += passSampleCount;
                Debug.Assert(SampleIndex <= totalSampleCount);
                if (SampleIndex == totalSampleCount)
                {
                    // a chunk is done since we have reached `totalSampleCount`
                    chunkIsDone = true;
                    TexelIndex += chunkTexelCount;
                    SampleIndex = 0;
                }
                Debug.Assert(TexelIndex <= totalTexelCount);
                if (TexelIndex == totalTexelCount)
                {
                    // an instance is done since we have reached `totalTexelCount`
                    instanceIsDone = true;
                }
            }
        }

        public struct IntegrationSettings
        {
            public RayTracingBackend Backend;
            public uint MaxDispatchesPerFlush;      // how many dispatches to do before flushing the GPU
            public bool DebugDispatches;

            public static readonly IntegrationSettings Default = new IntegrationSettings
            {
                Backend = RayTracingBackend.Compute,
                MaxDispatchesPerFlush = 1,
                DebugDispatches = false
            };
        }

        public class LightmapBakeSettings
        {
            public uint AOSampleCount = 32;
            public uint DirectSampleCount = 32;
            public uint IndirectSampleCount = 512;
            public uint ValiditySampleCount = 512;

            public AntiAliasingType AOAntiAliasingType = AntiAliasingType.Stochastic;
            public AntiAliasingType DirectAntiAliasingType = AntiAliasingType.SuperSampling;
            public AntiAliasingType IndirectAntiAliasingType = AntiAliasingType.Stochastic;
            public AntiAliasingType ValidityAntiAliasingType = AntiAliasingType.Stochastic;

            public uint BounceCount = 4;
            public uint DirectLightingEvaluationCount = 4;
            public uint IndirectLightingEvaluationCount = 1;
            public float AOMaxDistance = 1.0f;
            public float PushOff = 0.00001f;
            public UInt64 ExpandedBufferSize = 262144;
            public uint GetSampleCount(IntegratedOutputType integratedOutputType)
            {
                switch (integratedOutputType)
                {
                    case IntegratedOutputType.AO: return AOSampleCount;
                    case IntegratedOutputType.Direct: return DirectSampleCount;
                    case IntegratedOutputType.DirectionalityDirect: return DirectSampleCount;
                    case IntegratedOutputType.Indirect: return IndirectSampleCount;
                    case IntegratedOutputType.DirectionalityIndirect: return IndirectSampleCount;
                    case IntegratedOutputType.Validity: return ValiditySampleCount;
                    case IntegratedOutputType.ShadowMask: return DirectSampleCount;
                    default:
                        Debug.Assert(false, "Unexpected case.");
                        return 0;
                }
            }

            public AntiAliasingType GetAntiAliasingType(IntegratedOutputType integratedOutputType)
            {
                switch (integratedOutputType)
                {
                    case IntegratedOutputType.AO: return AOAntiAliasingType;
                    case IntegratedOutputType.Direct: return DirectAntiAliasingType;
                    case IntegratedOutputType.DirectionalityDirect: return DirectAntiAliasingType;
                    case IntegratedOutputType.Indirect: return IndirectAntiAliasingType;
                    case IntegratedOutputType.DirectionalityIndirect: return IndirectAntiAliasingType;
                    case IntegratedOutputType.Validity: return ValidityAntiAliasingType;
                    case IntegratedOutputType.ShadowMask: return DirectAntiAliasingType;
                    default:
                        Debug.Assert(false, "Unexpected case.");
                        return 0;
                }
            }
        }

        static bool IsNewChunkStarted(
            uint maxChunkSize,
            uint instanceWidth,
            uint instanceHeight,
            uint currentChunkTexelOffset,   // current starting texel index for the chunk, linear offset into instanceWidth*instanceHeight
            uint currentSampleIndex,        // current sample index for the chunk
            uint maxSamplesPerTexel,        // total sample count per texel
            out uint chunkSize,             // number of texels to process in a single pass
            out uint expandedSampleWidth,   // number of expanded samples per texel, power of two, this might exceed the required sample count
            out uint passSampleCount,       // the actual number of samples to take, this might be smaller than the expanded sample width
            out uint2 chunkOffset           // the chunk offset in 2D
            )
        {
            // this function should only be called when there is work to do
            Debug.Assert(currentSampleIndex < maxSamplesPerTexel);
            Debug.Assert(maxChunkSize > 0);
            Debug.Assert(instanceWidth > 0);
            Debug.Assert(instanceHeight > 0);
            Debug.Assert(currentChunkTexelOffset < instanceWidth * instanceHeight);
            chunkOffset = new uint2((uint)(currentChunkTexelOffset % (UInt64)instanceWidth), (uint)(currentChunkTexelOffset / (UInt64)instanceWidth));
            Debug.Assert(chunkOffset.x < instanceWidth);
            Debug.Assert(chunkOffset.y < instanceHeight);
            uint remainingTexels = (uint)instanceWidth - chunkOffset.x + ((uint)(instanceHeight - 1) - chunkOffset.y) * (uint)instanceWidth;
            Debug.Assert(remainingTexels > 0);
            uint remainingSampleCount = math.max(0, maxSamplesPerTexel - currentSampleIndex);
            Debug.Assert(remainingSampleCount > 0);

            // Choose the size of chunk to take
            chunkSize = math.min(remainingTexels, maxChunkSize); // Take as many *texels* as possible in a single pass - this is done to reduce the number of dispatches for compaction, reduction and `copy to lightmap`
            Debug.Assert(chunkSize <= maxChunkSize);

            // Calculate the maximum number of samples that can be taken in a single pass
            uint maxSamplesPerChunk = math.min(maxChunkSize / chunkSize, maxSamplesPerTexel); // The maximum number of samples we can take for the current chunk
            Debug.Assert(chunkSize * maxSamplesPerChunk <= maxChunkSize);

            // Sample count expansion needs to be a power of 2 - calculate the expansion width
            expandedSampleWidth = math.ceilpow2(maxSamplesPerChunk);
            if (expandedSampleWidth > maxSamplesPerChunk) expandedSampleWidth /= 2;
            Debug.Assert(expandedSampleWidth >= 1);
            Debug.Assert(expandedSampleWidth * chunkSize <= maxChunkSize);

            // Calculate how many samples we can take in this pass
            passSampleCount = math.min(maxSamplesPerChunk, math.min(remainingSampleCount, expandedSampleWidth));
            Debug.Assert(passSampleCount > 0);
            Debug.Assert(passSampleCount <= expandedSampleWidth);
            Debug.Assert(passSampleCount + currentSampleIndex <= maxSamplesPerTexel);
            return currentSampleIndex == 0; // When the sample count has rolled back to zero a new chunk has started
        }

        internal static uint AccumulateLightmapInstance(
            LightmapBakeState bakeState,
            BakeInstance instance,
            LightmapBakeSettings lightmapBakeSettings,
            IntegratedOutputType integratedOutputType,
            LightmappingContext lightmappingContext,
            UVAccelerationStructure uvAS,
            UVFallbackBuffer uvFallbackBuffer,
            bool doDirectionality,
            out uint chunkSize,
            out bool instanceIsDone)
        {
            CommandBuffer cmd = lightmappingContext.GetCommandBuffer();
            GraphicsBuffer traceScratchBuffer = lightmappingContext.TraceScratchBuffer;
            var ctx = lightmappingContext.IntegratorContext;
            var expansionShaders = ctx.ExpansionShaders;
            bool doDirectional = doDirectionality || integratedOutputType == IntegratedOutputType.ShadowMask; // Shadowmask uses the directional buffer to store the sample count - so also process that
            Vector2Int instanceTexelOffset = instance.TexelOffset;

            {
                var maxSampleCountPerTexel = lightmapBakeSettings.GetSampleCount(integratedOutputType);
                var instanceWidth = instance.TexelSize.x;
                var instanceHeight = instance.TexelSize.y;
                var instanceTexelCount = (UInt64)instanceWidth * (UInt64)instanceHeight;
                var sampleOffset = bakeState.SampleIndex;
                var maxChunkSize = (uint)lightmappingContext.ExpandedOutput.count;
                bool newChunkStarted = IsNewChunkStarted(
                    maxChunkSize,
                    (uint)instanceWidth,
                    (uint)instanceHeight,
                    (uint)bakeState.TexelIndex,
                    sampleOffset,
                    maxSampleCountPerTexel,
                    out chunkSize,
                    out uint expandedSampleWidth,
                    out uint passSampleCount,
                    out uint2 chunkOffset);

                if (newChunkStarted)
                {
                    // compact the texels
                    ExpansionHelpers.CompactGBuffer(cmd, expansionShaders, ctx.CompactGBufferKernel, (uint)instanceWidth, chunkSize, chunkOffset, uvFallbackBuffer, ctx.CompactedGBufferLength, lightmappingContext.CompactedTexelIndices);

                    // clear the expanded output buffer
                    // Populate the expanded clear indirect dispatch buffer - using the compacted size.
                    expansionShaders.GetKernelThreadGroupSizes(ctx.ClearBufferKernel, out uint clearThreadGroupSizeX, out uint clearThreadGroupSizeY, out uint clearThreadGroupSizeZ);
                    Debug.Assert(clearThreadGroupSizeY == 1 && clearThreadGroupSizeZ == 1);
                    ExpansionHelpers.PopulateClearExpandedOutputIndirectDispatch(cmd, expansionShaders, ctx.PopulateClearDispatchKernel, clearThreadGroupSizeX, expandedSampleWidth, ctx.CompactedGBufferLength, ctx.ClearDispatchBuffer);
                    // Clear the output buffers.
                    ExpansionHelpers.ClearExpandedOutput(cmd, expansionShaders, ctx.ClearBufferKernel, lightmappingContext.ExpandedOutput, ctx.ClearDispatchBuffer);
                    if (doDirectional)
                        ExpansionHelpers.ClearExpandedOutput(cmd, expansionShaders, ctx.ClearBufferKernel, lightmappingContext.ExpandedDirectional, ctx.ClearDispatchBuffer);
                }

                // Work out the super sampling resolution. It's the width of the N x N supersampling kernel. Find the largest perfect square that is less than or equal to the max sample count per texel.
                uint superSampleWidth = Math.Max(1, (uint)Math.Sqrt(maxSampleCountPerTexel));

                // generate the GBuffer
                ExpansionHelpers.GenerateGBuffer(
                    cmd,
                    lightmappingContext.IntegratorContext.GBufferShader,
                    lightmappingContext.GBuffer,
                    traceScratchBuffer,
                    lightmappingContext.IntegratorContext.SamplingResources,
                    uvAS,
                    uvFallbackBuffer,
                    ctx.CompactedGBufferLength,
                    lightmappingContext.CompactedTexelIndices,
                    instanceTexelOffset,
                    chunkOffset,
                    chunkSize,
                    expandedSampleWidth,
                    passSampleCount,
                    sampleOffset,
                    lightmapBakeSettings.GetAntiAliasingType(integratedOutputType),
                    superSampleWidth
                    );

                GraphicsBuffer expandedDirectional = lightmappingContext.ExpandedDirectional;
                var instanceGeometryIndex = lightmappingContext.World.PathTracingWorld.GetAccelerationStructure().GeometryPool.GetInstanceGeometryIndex(instance.Mesh);

                bool debugGBuffer = false;
                if (debugGBuffer)
                {
                    Debug.Log($"Lightmap resolution: {lightmappingContext.Width} x {lightmappingContext.Height}");
                    Debug.Log($"Instance resolution: {instanceWidth} x {instanceHeight}");
                    Debug.Log($"Instance offset: {instanceTexelOffset}");
                    Debug.Log($"Sample count: {maxSampleCountPerTexel}");
                    var occupancy = (double)(passSampleCount * chunkSize) / (double)maxChunkSize * 100.0;
                    Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Occupancy: {0:F2}%", occupancy));
                    // write out the lightmap UV samples
                    var uvSampleData = ExpansionHelpers.DebugGBuffer(cmd, instance, lightmappingContext, expandedSampleWidth, passSampleCount);
                    string sampleOutput = new("");
                    foreach (var sample in uvSampleData)
                        sampleOutput += string.Format(System.Globalization.CultureInfo.InvariantCulture, "float2({0}, {1})\n", sample.x, sample.y);
                    
                    System.Console.WriteLine(sampleOutput);
                }

                // accumulate the lightmap texel
                cmd.BeginSample("AccumulateLightmapInstance");

                switch (integratedOutputType)
                {
                    case IntegratedOutputType.AO:
                    {
                        lightmappingContext.IntegratorContext.LightmapAOIntegrator.Accumulate(
                            cmd,
                            passSampleCount,
                            bakeState.SampleIndex,
                            instance.LocalToWorldMatrix,
                            instance.LocalToWorldMatrixNormals,
                            instanceGeometryIndex,
                            instance.TexelSize,
                            chunkOffset,
                            lightmappingContext.World.PathTracingWorld,
                            traceScratchBuffer,
                            lightmappingContext.GBuffer,
                            expandedSampleWidth,
                            lightmappingContext.ExpandedOutput,
                            lightmappingContext.CompactedTexelIndices,
                            lightmappingContext.IntegratorContext.CompactedGBufferLength,
                            lightmapBakeSettings.PushOff,
                            lightmapBakeSettings.AOMaxDistance,
                            newChunkStarted
                        );
                        break;
                    }
                    case IntegratedOutputType.Validity:
                    {
                        lightmappingContext.IntegratorContext.LightmapValidityIntegrator.Accumulate(
                            cmd,
                            passSampleCount,
                            bakeState.SampleIndex,
                            instance.LocalToWorldMatrix,
                            instance.LocalToWorldMatrixNormals,
                            instanceGeometryIndex,
                            instance.TexelSize,
                            chunkOffset,
                            lightmappingContext.World.PathTracingWorld,
                            traceScratchBuffer,
                            lightmappingContext.GBuffer,
                            expandedSampleWidth,
                            lightmappingContext.ExpandedOutput,
                            lightmappingContext.CompactedTexelIndices,
                            lightmappingContext.IntegratorContext.CompactedGBufferLength,
                            lightmapBakeSettings.PushOff,
                            newChunkStarted
                        );
                        break;
                    }
                    case IntegratedOutputType.Direct:
                    case IntegratedOutputType.DirectionalityDirect:
                    {
                        lightmappingContext.IntegratorContext.LightmapDirectIntegrator.Accumulate(
                            cmd,
                            passSampleCount,
                            bakeState.SampleIndex,
                            instance.LocalToWorldMatrix,
                            instance.LocalToWorldMatrixNormals,
                            instanceGeometryIndex,
                            instance.TexelSize,
                            chunkOffset,
                            lightmappingContext.World.PathTracingWorld,
                            traceScratchBuffer,
                            lightmappingContext.GBuffer,
                            expandedSampleWidth,
                            lightmappingContext.ExpandedOutput,
                            expandedDirectional,
                            lightmappingContext.CompactedTexelIndices,
                            lightmappingContext.IntegratorContext.CompactedGBufferLength,
                            instance.ReceiveShadows,
                            lightmapBakeSettings.PushOff,
                            lightmapBakeSettings.DirectLightingEvaluationCount,
                            newChunkStarted
                        );
                        break;
                    }
                    case IntegratedOutputType.Indirect:
                    case IntegratedOutputType.DirectionalityIndirect:
                    {
                        lightmappingContext.IntegratorContext.LightmapIndirectIntegrator.Accumulate(
                            cmd,
                            passSampleCount,
                            bakeState.SampleIndex,
                            lightmapBakeSettings.BounceCount,
                            instance.LocalToWorldMatrix,
                            instance.LocalToWorldMatrixNormals,
                            instanceGeometryIndex,
                            instance.TexelSize,
                            chunkOffset,
                            lightmappingContext.World.PathTracingWorld,
                            traceScratchBuffer,
                            lightmappingContext.GBuffer,
                            expandedSampleWidth,
                            lightmappingContext.ExpandedOutput,
                            expandedDirectional,
                            lightmappingContext.CompactedTexelIndices,
                            lightmappingContext.IntegratorContext.CompactedGBufferLength,
                            lightmapBakeSettings.PushOff,
                            lightmapBakeSettings.IndirectLightingEvaluationCount,
                            newChunkStarted
                        );
                        break;
                    }
                    case IntegratedOutputType.ShadowMask:
                    {
                        lightmappingContext.IntegratorContext.LightmapShadowMaskIntegrator.Accumulate(
                            cmd,
                            passSampleCount,
                            bakeState.SampleIndex,
                            instance.LocalToWorldMatrix,
                            instance.LocalToWorldMatrixNormals,
                            instanceGeometryIndex,
                            instance.TexelSize,
                            chunkOffset,
                            lightmappingContext.World.PathTracingWorld,
                            traceScratchBuffer,
                            lightmappingContext.GBuffer,
                            expandedSampleWidth,
                            lightmappingContext.ExpandedOutput,
                            expandedDirectional,
                            lightmappingContext.CompactedTexelIndices,
                            lightmappingContext.IntegratorContext.CompactedGBufferLength,
                            instance.ReceiveShadows,
                            lightmapBakeSettings.PushOff,
                            lightmapBakeSettings.DirectLightingEvaluationCount,
                            newChunkStarted
                        );
                        break;
                    }
                }
                cmd.EndSample("AccumulateLightmapInstance");

                //LightmapIntegrationHelpers.LogGraphicsBuffer(cmd, lightmappingContext.ExpandedOutput, "expandedOutput", LightmapIntegrationHelpers.LogBufferType.Float4);

                // Update the baking state
                bakeState.Tick(
                    passSampleCount,
                    maxSampleCountPerTexel,
                    chunkSize,
                    instanceTexelCount,
                    out instanceIsDone,
                    out bool chunkIsDone);

                if (chunkIsDone)
                {
                    int maxExpandedDispatchSize = instanceWidth * instanceHeight * (int)expandedSampleWidth;
                    // Gather to lightmap -> first reduce to output resolution
                    // Populate the reduce indirect dispatch buffer - using the compacted size.
                    expansionShaders.GetKernelThreadGroupSizes(ctx.ReductionKernel, out uint reduceThreadGroupSizeX, out uint reduceThreadGroupSizeY, out uint reduceThreadGroupSizeZ);
                    Debug.Assert(reduceThreadGroupSizeY == 1 && reduceThreadGroupSizeZ == 1);
                    ExpansionHelpers.PopulateReduceExpandedOutputIndirectDispatch(cmd, expansionShaders, ctx.PopulateReduceDispatchKernel, reduceThreadGroupSizeX, expandedSampleWidth, ctx.CompactedGBufferLength, ctx.ReduceDispatchBuffer);
                    ExpansionHelpers.ReduceExpandedOutput(cmd, expansionShaders, ctx.ReductionKernel, lightmappingContext.ExpandedOutput, maxExpandedDispatchSize, expandedSampleWidth, ctx.ReduceDispatchBuffer);
                    if (doDirectional)
                        ExpansionHelpers.ReduceExpandedOutput(cmd, expansionShaders, ctx.ReductionKernel, lightmappingContext.ExpandedDirectional, maxExpandedDispatchSize, expandedSampleWidth, ctx.ReduceDispatchBuffer);

                    // Populate the copy indirect dispatch buffer - using the compacted size.
                    expansionShaders.GetKernelThreadGroupSizes(ctx.CopyToLightmapKernel, out uint copyThreadGroupSizeX, out uint copyThreadGroupSizeY, out uint copyThreadGroupSizeZ);
                    Debug.Assert(copyThreadGroupSizeY == 1 && copyThreadGroupSizeZ == 1);
                    ExpansionHelpers.PopulateCopyToLightmapIndirectDispatch(cmd, expansionShaders, ctx.PopulateCopyDispatchKernel, copyThreadGroupSizeX, ctx.CompactedGBufferLength, ctx.CopyDispatchBuffer);
                    ExpansionHelpers.CopyToLightmap(cmd, expansionShaders, ctx.CopyToLightmapKernel, expandedSampleWidth, instanceWidth, instanceTexelOffset, chunkOffset, ctx.CompactedGBufferLength, lightmappingContext.CompactedTexelIndices, lightmappingContext.ExpandedOutput, ctx.CopyDispatchBuffer, lightmappingContext.AccumulatedOutput);
                    if (doDirectional)
                        ExpansionHelpers.CopyToLightmap(cmd, expansionShaders, ctx.CopyToLightmapKernel, expandedSampleWidth, instanceWidth, instanceTexelOffset, chunkOffset, ctx.CompactedGBufferLength, lightmappingContext.CompactedTexelIndices, lightmappingContext.ExpandedDirectional, ctx.CopyDispatchBuffer, lightmappingContext.AccumulatedDirectionalOutput);
                }
                return passSampleCount;
            }
        }
    }
}
