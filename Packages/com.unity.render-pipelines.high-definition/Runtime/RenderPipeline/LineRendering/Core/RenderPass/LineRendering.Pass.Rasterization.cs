using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    partial class LineRendering
    {

        private static void ExecuteRasterizationPass(CommandBuffer cmd, RasterizationPassData resources)
        {
            var buffers = resources.sharedBuffers;
            var transientBuffers = resources.transientBuffers;
            var shadingSampleAtlas = resources.sharedBuffers.groupShadingSampleAtlas;
            var prefixResources   = GPUPrefixSum.SupportResources.Load(transientBuffers.prefixResources);

            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stagePrepareCS,    ShaderIDs._ConstantBuffer);
            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stageRasterBinCS,  ShaderIDs._ConstantBuffer);
            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stageWorkQueue,    ShaderIDs._ConstantBuffer);
            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stageRasterFineCS, ShaderIDs._ConstantBuffer);

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesRasterizationSetup)))
            {
                // Reset per-bin data
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinIndicesBuffer, transientBuffers.binIndices);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 1, DivRoundUp(resources.binCount, 1024), 1, 1);

                // Reset clusters counters
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 2, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 2, DivRoundUp(resources.clusterCount, 1024), 1, 1);
            }

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesBuildClusters)))
            {
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 3, DivRoundUp(resources.clusterDepth, 64), 1, 1);
            }

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesBinningStage)))
            {
                // Derive a dispatch launch size from the amount of bin records.
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._BinningArgsBuffer, transientBuffers.binningIndirectArgs);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 1, 1, 1, 1);

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS,  0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._SegmentRecordBuffer,   buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRecordBuffer,   transientBuffers.recordBufferCluster);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._CounterBuffer,         buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._BinCountersBuffer,     transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 0, transientBuffers.binningIndirectArgs, 0);
            }

            // Generate the offset indices into the global work queue.
            var resourceSortTiles = GPUSort.SupportResources.Load(transientBuffers.binSortResources);

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesWorkQueue)))
            {
                resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                {
                    exclusive = true,
                    inputCount = resources.clusterCount,
                    input = transientBuffers.clusterCounters,
                    supportResources = prefixResources
                });

                // Derive a dispatch launch size from the amount of bin records.
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 0, ShaderIDs._OutputWorkQueueArgs, transientBuffers.workQueueArgs);
                cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 0, 1, 1, 1);

                // Indirectly dispatch the work queue construction.
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._BinOffsetsBuffer, prefixResources.output);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._ClusterRecordBuffer, transientBuffers.recordBufferCluster);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._WorkQueueBuffer, transientBuffers.workQueue);
                cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 1, transientBuffers.workQueueArgs, 0);

                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._BinCountersBuffer,     transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ActiveClusterIndices, transientBuffers.activeClusterIndices);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 3, DivRoundUp(resources.clusterCount, 1024), 1, 1);

                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 2, DivRoundUp(resources.binCount, 1024), 1, 1);

                resources.systemResources.gpuSort.Dispatch(cmd, new GPUSort.Args
                {
                    count = (uint) resources.binCount,
                    maxDepth = (uint) resources.binCount,
                    inputKeys = transientBuffers.binCounters,
                    inputValues = transientBuffers.binIndices,
                    resources = resourceSortTiles
                });

                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 4, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 4, "_FineRasterDispatchArgs", transientBuffers.fineRasterArgs);
                cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 4, 1, 1, 1);
            }

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesFineRaster)))
            {
                int fineStageKernel;

                if (resources.debugModeIndex >= 0)
                {
                    fineStageKernel = 4;
                    cmd.SetComputeIntParam(resources.systemResources.stageRasterFineCS, "_HairDebugMode", resources.debugModeIndex);
                }
                else
                {
                    fineStageKernel = resources.qualityModeIndex;
                }

                #if UNITY_EDITOR
                if (resources.renderDataStillHasShadersCompiling)
                {
                    fineStageKernel = 5;
                }
                #endif

                // We must manually bind this to avoid certain scenarios where it doesn't get bound earlier in the pipeline for a frame.
                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, "_CameraDepthTexture", resources.depthRT);

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._Vertex3RecordBuffer, buffers.vertexStream3);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._BinOffsetsBuffer, prefixResources.output);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._WorkQueueBuffer, transientBuffers.workQueue);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._WorkQueueBinListBuffer, resourceSortTiles.sortBufferValues);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS,  fineStageKernel, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);

                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ShadingSamplesTexture, shadingSampleAtlas);

                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetColor, resources.renderTargets.color);
                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetDepth, resources.renderTargets.depth);
                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetMV, resources.renderTargets.motion);

                cmd.DispatchCompute(resources.systemResources.stageRasterFineCS, fineStageKernel, transientBuffers.fineRasterArgs, 0);
            }
        }
    }
}
