using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    partial class LineRendering
    {

        private static void ExecuteRasterizationPass(CommandBuffer cmd, RasterizationPassData resources)
        {
            // Push the parameters to all the stages.
            int constantBufferSize;
            unsafe
            {
                constantBufferSize = sizeof(ShaderVariables);
            }
            cmd.SetComputeConstantBufferParam(resources.systemResources.stagePrepareCS, ShaderIDs._ConstantBuffer, resources.sharedBuffers.constantBuffer, 0, constantBufferSize);
            cmd.SetComputeConstantBufferParam(resources.systemResources.stageRasterBinCS, ShaderIDs._ConstantBuffer, resources.sharedBuffers.constantBuffer, 0, constantBufferSize);
            cmd.SetComputeConstantBufferParam(resources.systemResources.stageWorkQueue, ShaderIDs._ConstantBuffer, resources.sharedBuffers.constantBuffer, 0, constantBufferSize);
            cmd.SetComputeConstantBufferParam(resources.systemResources.stageRasterFineCS, ShaderIDs._ConstantBuffer, resources.sharedBuffers.constantBuffer, 0, constantBufferSize);

            var buffers = resources.sharedBuffers;
            var transientBuffers = resources.transientBuffers;
            var shadingSampleAtlas = resources.sharedBuffers.groupShadingSampleAtlas;
            var prefixResources   = GPUPrefixSum.SupportResources.Load(transientBuffers.prefixResources);

#region Setup
            using (new ProfilingScope(cmd, new ProfilingSampler("Rasterization Setup")))
            {
                // Reset per-bin data
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinIndicesBuffer, transientBuffers.binIndices);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 1, DivRoundUp(resources.shaderVariables._BinCount, 1024), 1, 1);

                // Reset clusters counters
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 2, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 2, DivRoundUp(resources.shaderVariables._ClusterCount, 1024), 1, 1);
            }
#endregion

#region BuildClusters
            using (new ProfilingScope(cmd, new ProfilingSampler("Build Clusters")))
            {
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 3, DivRoundUp(resources.shaderVariables._ClusterDepth, 64), 1, 1);
            }
#endregion

#region StageBinning
            using (new ProfilingScope(cmd, new ProfilingSampler("Binning Stage")))
            {
                // Derive a dispatch launch size from the amount of bin records.
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._BinningArgsBuffer, transientBuffers.binningIndirectArgs);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 1, 1, 1, 1);

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS,  0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._SegmentRecordBuffer,   buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRecordBuffer,   transientBuffers.recordBufferCluster);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._CounterBuffer,         buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._BinCountersBuffer,     transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 0, transientBuffers.binningIndirectArgs, 0);
            }
#endregion

#region StageWorkQueue
            // Generate the offset indices into the global work queue.

            var resourceSortTiles = GPUSort.SupportResources.Load(transientBuffers.binSortResources);

            using (new ProfilingScope(cmd, new ProfilingSampler("Work Queue")))
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Prefix Sum Segments")))
                {
                    resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                    {
                        exclusive = true,
                        inputCount = resources.shaderVariables._ClusterCount,
                        input = transientBuffers.clusterCounters,
                        supportResources = prefixResources
                    });
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Build")))
                {
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
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Active Clusters")))
                {
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._BinCountersBuffer,     transientBuffers.binCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ActiveClusterIndices, transientBuffers.activeClusterIndices);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 3, DivRoundUp(resources.shaderVariables._ClusterCount, 1024), 1, 1);
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Count Active Bins")))
                {
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 2, DivRoundUp(resources.shaderVariables._BinCount, 1024), 1, 1);
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Sort Bins")))
                {
                    resources.systemResources.gpuSort.Dispatch(cmd, new GPUSort.Args
                    {
                        count = (uint) resources.shaderVariables._BinCount,
                        maxDepth = (uint) resources.shaderVariables._BinCount,
                        inputKeys = transientBuffers.binCounters,
                        inputValues = transientBuffers.binIndices,
                        resources = resourceSortTiles
                    });
                }
            }
#endregion

#region StageFine
            using (new ProfilingScope(cmd, new ProfilingSampler("Fine Stage")))
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

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._BinOffsetsBuffer, prefixResources.output);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._BinCountersBuffer, transientBuffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._WorkQueueBuffer, transientBuffers.workQueue);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._WorkQueueBinListBuffer, resourceSortTiles.sortBufferValues);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ClusterCountersBuffer, transientBuffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ClusterRangesBuffer, transientBuffers.clusterRanges);

                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ShadingSamplesTexture, shadingSampleAtlas);

                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetColor, resources.renderTargets.color);
                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetDepth, resources.renderTargets.depth);
                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._OutputTargetMV, resources.renderTargets.motion);

                // Launch for every wave on the device.
                // TODO: Querying the correct number of thread groups to launch for device saturation.
                cmd.DispatchCompute(resources.systemResources.stageRasterFineCS, fineStageKernel, 4 * 360, 1, 1);
            }
#endregion
        }
    }
}
