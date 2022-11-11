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
        private static void Rasterize(CommandBuffer cmd, RasterizerResources resources)
        {
            // Push the parameters to all the stages.
            ConstantBuffer.PushGlobal(cmd, resources.shaderVariables, ShaderIDs._ConstantBuffer);

            var buffers = resources.buffers;
            var shadingAtlasCurrent = resources.ShadingSampleAtlas.currentAtlas;
            var shadingAtlasPrevious = resources.ShadingSampleAtlas.previousAtlas;
            var prefixResources   = GPUPrefixSum.SupportResources.Load(buffers.prefixResources);
#region StagePrepare
            using (new ProfilingScope(cmd, new ProfilingSampler("Prepare")))
            {
                // Reset counters
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 0, 1, 1 ,1);

                // Reset per-bin data
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinCountersBuffer, buffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 1, ShaderIDs._BinIndicesBuffer, buffers.binIndices);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 1, DivRoundUp(resources.shaderVariables._BinCount, 1024), 1, 1);

                // Reset clusters counters
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 2, ShaderIDs._ClusterCountersBuffer, buffers.clusterCounters);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 2, DivRoundUp(resources.shaderVariables._ClusterCount, 1024), 1, 1);
            }
#endregion

#region GroupSetup
            using (new ProfilingScope(cmd, new ProfilingSampler("Group Setup")))
            {
                for (int i = 0; i < resources.rendererData.Length; ++i)
                {
                    var renderData = resources.rendererData[i];
                    var renderer = renderData.rendererData;
                    var perRendererData = renderData.persistentData;

                    // Reset per-renderer counters & vertex visibility
                    cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 4, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 4, 1, 1 ,1);

                    cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 0, ShaderIDs._ShadingSampleVisibilityBuffer, buffers.shadingScratchBuffer);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", renderer.mesh.vertexCount + 1);
                    cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 0, DivRoundUp(renderer.mesh.vertexCount + 1, 256), 1 ,1);

                    using (new ProfilingScope(cmd, new ProfilingSampler("Vertex Setup")))
                    using (new BindRendererToComputeKernel(cmd, renderer))
                        {
                            cmd.SetComputeIntParam(renderer.vertexSetupCompute, "_VertexOffset", resources.offsetsVertex[i]);
                            cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                            cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                            cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex2RecordBuffer, buffers.vertexStream2);
                            cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex3RecordBuffer, buffers.vertexStream3);
                            cmd.DispatchCompute(renderer.vertexSetupCompute, 0, DivRoundUp(renderer.mesh.vertexCount, 128), 1, 1);
                        }

                    using (new ProfilingScope(cmd, new ProfilingSampler("Segment Setup")))
                    {
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, "_LODBuffer", renderer.lodBuffer);
                        cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, "_SegmentsPerLine", renderer.segmentsPerLine);
                        cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, "_LineCount", renderer.lineCount);
                        cmd.SetComputeFloatParam(resources.systemResources.stageSetupSegmentCS, "_LOD", renderer.lodMode != RendererLODMode.None ? renderer.lod : 1f);

                        cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, "_VertexOffset", resources.offsetsVertex[i]);
                        cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, "_SegmentOffset", (int)renderer.mesh.GetIndexCount(0) / 2); // TODO: Rename to segment count
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._IndexBuffer, renderer.indexBuffer);
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                        cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._ShadingSampleVisibilityBuffer, buffers.shadingScratchBuffer);
                        cmd.SetComputeTextureParam(resources.systemResources.stageSetupSegmentCS, 0, "_CameraDepthTexture", resources.depthTexture);
                        cmd.DispatchCompute(resources.systemResources.stageSetupSegmentCS, 0, DivRoundUp((int)renderer.mesh.GetIndexCount(0) / 2, ShaderVariables.NumLaneSegmentSetup), 1, 1);
                    }

                    using (new ProfilingScope(cmd, new ProfilingSampler("Shading Prepare")))
                    {
                        bool atlasSizeChanged = perRendererData.shadingAtlasAllocation.previousAllocationSize > 0 &&
                                                perRendererData.shadingAtlasAllocation.previousAllocationSize !=
                                                perRendererData.shadingAtlasAllocation.currentAllocationSize;

                        if (renderData.rendererData.shadingFraction < 1.0f)
                        {
                            int sampleIDOffset = perRendererData.updateCount % Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE;
                            int maxSamplesToShade = (int)(renderData.rendererData.shadingFraction * perRendererData.shadingAtlasAllocation.currentAllocationSize);
                            //reproject shading from previous frame or clear
                            if (perRendererData.shadingAtlasAllocation.previousAllocationOffset != -1 && perRendererData.updateCount > 0 &&!atlasSizeChanged)
                            {
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", perRendererData.shadingAtlasAllocation.currentAllocationSize);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_ShadingAtlasSampleOffset", perRendererData.shadingAtlasAllocation.currentAllocationOffset);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SourceShadingAtlasSampleOffset", perRendererData.shadingAtlasAllocation.previousAllocationOffset);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureWidth", shadingAtlasCurrent.width);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureHeight", shadingAtlasCurrent.height);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SourceTextureWidth", shadingAtlasPrevious.width);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SourceTextureHeight", shadingAtlasPrevious.width);

                                cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingSamplesTexture, shadingAtlasCurrent);
                                cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingScratchTexture, shadingAtlasPrevious);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 4, DivRoundUp(perRendererData.shadingAtlasAllocation.currentAllocationSize, 256), 1 ,1);
                            }
                            else
                            {
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", perRendererData.shadingAtlasAllocation.currentAllocationSize);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_ShadingAtlasSampleOffset", perRendererData.shadingAtlasAllocation.currentAllocationOffset);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureWidth", shadingAtlasCurrent.width);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureHeight", shadingAtlasCurrent.height);
                                cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 2, ShaderIDs._ShadingSamplesTexture, shadingAtlasCurrent);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 2, DivRoundUp(perRendererData.shadingAtlasAllocation.currentAllocationSize, 256), 1 ,1);
                            }

                            if (maxSamplesToShade > 1)
                            {
                                //clear histogram
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,3,  "_HistogramBuffer", buffers.shadingSampleHistogram);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 3, 1, 1 ,1);

                                //calculate histogram for sample "ID"s
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", perRendererData.shadingAtlasAllocation.currentAllocationSize);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleIDOffset", sampleIDOffset);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,7,  "_HistogramBuffer", buffers.shadingSampleHistogram);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,7,  ShaderIDs._ShadingSampleVisibilityBuffer, buffers.shadingScratchBuffer);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 7, DivRoundUp(perRendererData.shadingAtlasAllocation.currentAllocationSize, Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE), 1 ,1);

                                //prefixsum the histogram
                                resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                                {
                                    exclusive = true,
                                    inputCount = Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE,
                                    input = buffers.shadingSampleHistogram,
                                    supportResources = prefixResources
                                });

                                //select highest ID to shade
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_MaxSamplesToShade", maxSamplesToShade);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8,  "_HistogramBuffer", buffers.shadingSampleHistogram);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8, "_PrefixSumBuffer", prefixResources.output);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8,  ShaderIDs._ShadingSampleVisibilityBuffer, buffers.shadingScratchBuffer);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 8, 1, 1 ,1);

                                //mark rejected IDs as non visible
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", perRendererData.shadingAtlasAllocation.currentAllocationSize);
                                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleIDOffset", sampleIDOffset);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,9,  "_HistogramBuffer", buffers.shadingSampleHistogram);
                                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,9,  ShaderIDs._ShadingSampleVisibilityBuffer, buffers.shadingScratchBuffer);
                                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 9, DivRoundUp(perRendererData.shadingAtlasAllocation.currentAllocationSize, 256), 1 ,1);
                            }

                        }

                        //prefixsum visible samples
                        resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                        {
                            exclusive = true,
                            inputCount = renderer.mesh.vertexCount + 1,
                            input = buffers.shadingScratchBuffer,
                            supportResources = prefixResources
                        });


                        {
                            //clear compaction buffer
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 1, ShaderIDs._ShadingCompactionBuffer, buffers.shadingScratchBuffer);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", renderer.mesh.vertexCount);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 1, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);

                            // Write out mapping from compacted shading index to non-compated
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 5, ShaderIDs._ShadingCompactionBuffer, buffers.shadingScratchBuffer);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 5, "_PrefixSumBuffer", prefixResources.output);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", renderer.mesh.vertexCount);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 5, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);

                        }
                    }

                    using (new ProfilingScope(cmd, new ProfilingSampler("Shading")))
                    {
                        // TODO: Cull shading samples from culled segments
                        // TODO: Choose atlas size based on vertex count (NearestPow2(Sqrt(VertexCount)))

                        Vector2Int scratchAtlasSize = buffers.shadingScratchTextureDimensions;

                        var offscreenViewport = new Rect(
                            0, 0, scratchAtlasSize.x, scratchAtlasSize.y
                        );

                        var mpb = new MaterialPropertyBlock();
                        {
                            mpb.SetBuffer(ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                            mpb.SetBuffer(ShaderIDs._Vertex2RecordBuffer, buffers.vertexStream2);
                            mpb.SetBuffer(ShaderIDs._Vertex3RecordBuffer, buffers.vertexStream3);

                            // TODO: Might need to set the premultiply keyword as well for fog.
                            // TODO: Currently hard coding HDRP blendmode value (pre-multiply).
                            mpb.SetFloat("_BlendMode", 4f);

                            mpb.SetBuffer(ShaderIDs._CounterBuffer, buffers.counterBuffer);
                            mpb.SetBuffer(ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                            mpb.SetBuffer(ShaderIDs._ShadingCompactionBuffer, buffers.shadingScratchBuffer);
                            mpb.SetInteger(ShaderIDs._SoftwareLineOffscreenAtlasWidth, scratchAtlasSize.x);
                            mpb.SetInteger(ShaderIDs._SoftwareLineOffscreenAtlasHeight, scratchAtlasSize.y);
                            mpb.SetInteger(ShaderIDs._ShadingSampleVisibilityCount, renderer.mesh.vertexCount);
                            mpb.SetInteger("_VertexOffset", resources.offsetsVertex[i]);
                            // Need to manually reinterpret the bits from uint -> float (and then back to uint on GPU..).
                            mpb.SetVector("unity_RenderingLayer", new Vector4(BitConverter.Int32BitsToSingle((int)renderer.renderingLayerMask), 0f, 0f, 0f));

                            mpb.CopySHCoefficientArraysFrom( new []{ renderer.probe } );
                        }

                        CoreUtils.SetRenderTarget(cmd, buffers.shadingScratchTexture);
                        cmd.SetViewport(offscreenViewport);
                        CoreUtils.ClearRenderTarget(cmd, ClearFlag.Color, Color.black); //workaround for what seems like a bug in xbox, writes from scratch to shading atlas seem to get previous values of scratch. TODO: investigate what is missing


                        cmd.DrawProcedural(Matrix4x4.identity, renderer.material, renderer.offscreenShadingPass, MeshTopology.Triangles, 6, 1, mpb);
                    }

                    //unpack shading samples from compacted scratch to the shading atlas
                    {

                        Vector2Int scratchAtlasSize = buffers.shadingScratchTextureDimensions;
                        Vector2Int sampleAtlasSize = Vector2Int.one * 4096;
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureWidth", sampleAtlasSize.x);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_TargetTextureHeight", sampleAtlasSize.y);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SourceTextureWidth", scratchAtlasSize.x);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SourceTextureHeight", scratchAtlasSize.y);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_ShadingAtlasSampleOffset", perRendererData.shadingAtlasAllocation.currentAllocationOffset);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, "_SampleCount", renderer.mesh.vertexCount);
                        cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingCompactionBuffer, buffers.shadingScratchBuffer);
                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingScratchTexture, buffers.shadingScratchTexture);
                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingSamplesTexture, shadingAtlasCurrent);
                        cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 6, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);
                    }

                    // Update the offsets.
                    {
                        cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 5, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                        cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 5, 1, 1 ,1);
                    }
                }
            }
#endregion

#region BuildClusters
            using (new ProfilingScope(cmd, new ProfilingSampler("Build Clusters")))
            {
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 3, ShaderIDs._ClusterRangesBuffer, buffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 3, DivRoundUp(resources.shaderVariables._ClusterDepth, 64), 1, 1);
            }
#endregion

#region StageBinning
            using (new ProfilingScope(cmd, new ProfilingSampler("Binning Stage")))
            {
                // Derive a dispatch launch size from the amount of bin records.
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 1, ShaderIDs._BinningArgsBuffer, buffers.binningIndirectArgs);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 1, 1, 1, 1);

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS,  0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._SegmentRecordBuffer,   buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRecordBuffer,   buffers.recordBufferCluster);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._CounterBuffer,         buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._BinCountersBuffer,     buffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterCountersBuffer, buffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterBinCS, 0, ShaderIDs._ClusterRangesBuffer, buffers.clusterRanges);
                cmd.DispatchCompute(resources.systemResources.stageRasterBinCS, 0, buffers.binningIndirectArgs, 0);
            }
#endregion

#region StageWorkQueue
            // Generate the offset indices into the global work queue.

            var resourceSortTiles = GPUSort.SupportResources.Load(buffers.binSortResources);

            using (new ProfilingScope(cmd, new ProfilingSampler("Work Queue")))
            {
                using (new ProfilingScope(cmd, new ProfilingSampler("Prefix Sum Segments")))
                {
                    resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                    {
                        exclusive = true,
                        inputCount = resources.shaderVariables._ClusterCount,
                        input = buffers.clusterCounters,
                        supportResources = prefixResources
                    });
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Build")))
                {
                    // Derive a dispatch launch size from the amount of bin records.
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 0, ShaderIDs._OutputWorkQueueArgs, buffers.workQueueArgs);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 0, 1, 1, 1);

                    // Indirectly dispatch the work queue construction.
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._BinOffsetsBuffer, prefixResources.output);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._ClusterRecordBuffer, buffers.recordBufferCluster);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 1, ShaderIDs._WorkQueueBuffer, buffers.workQueue);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 1, buffers.workQueueArgs, 0);
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Active Clusters")))
                {
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._BinCountersBuffer,     buffers.binCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ClusterCountersBuffer, buffers.clusterCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._ActiveClusterIndices, buffers.activeClusterIndices);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 3, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 3, DivRoundUp(resources.shaderVariables._ClusterCount, 1024), 1, 1);
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Count Active Bins")))
                {
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._BinCountersBuffer, buffers.binCounters);
                    cmd.SetComputeBufferParam(resources.systemResources.stageWorkQueue, 2, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stageWorkQueue, 2, DivRoundUp(resources.shaderVariables._BinCount, 1024), 1, 1);
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Sort Bins")))
                {
                    resources.systemResources.gpuSort.Dispatch(cmd, new GPUSort.Args
                    {
                        count = (uint) resources.shaderVariables._BinCount,
                        maxDepth = (uint) resources.shaderVariables._BinCount,
                        inputKeys = buffers.binCounters,
                        inputValues = buffers.binIndices,
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
                if (resources.rendererData.Any(o => !ShaderUtil.IsPassCompiled(o.rendererData.material, o.rendererData.offscreenShadingPass)))
                {
                    fineStageKernel = 5;
                }
                #endif

                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._BinOffsetsBuffer, prefixResources.output);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._BinCountersBuffer, buffers.binCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._WorkQueueBuffer, buffers.workQueue);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._WorkQueueBinListBuffer, resourceSortTiles.sortBufferValues);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ClusterCountersBuffer, buffers.clusterCounters);
                cmd.SetComputeBufferParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ClusterRangesBuffer, buffers.clusterRanges);

                cmd.SetComputeTextureParam(resources.systemResources.stageRasterFineCS, fineStageKernel, ShaderIDs._ShadingSamplesTexture, shadingAtlasCurrent);

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
