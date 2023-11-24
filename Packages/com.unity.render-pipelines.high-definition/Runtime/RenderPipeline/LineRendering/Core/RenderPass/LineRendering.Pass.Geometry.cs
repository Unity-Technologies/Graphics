using System;
using System.Linq;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering
{
    public partial class LineRendering
    {
        static void ExecuteGeometryPass(CommandBuffer cmd, GeometryPassData resources)
        {
            var buffers = resources.sharedBuffers;
            var transientBuffers = resources.transientBuffers;
            var shadingHistoryAtlasCurrent = resources.shadingAtlas.current;
            var shadingHistoryAtlasPrevious = resources.shadingAtlas.previous;
            var shadingSampleAtlas = resources.sharedBuffers.groupShadingSampleAtlas;
            var shadingSampleAtlasDimensions = resources.sharedBuffers.groupShadingSampleAtlasDimensions;
            var prefixResources   = GPUPrefixSum.SupportResources.Load(transientBuffers.prefixResources);

            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stagePrepareCS,      ShaderIDs._ConstantBuffer);
            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stageSetupSegmentCS, ShaderIDs._ConstantBuffer);
            resources.shaderVariablesBuffer.Set(cmd, resources.systemResources.stageShadingSetupCS, ShaderIDs._ConstantBuffer);

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesGeometrySetup)))
            {
                // Reset counters
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 0, 1, 1 ,1);
            }

            for (int i = 0; i < resources.rendererData.Length; ++i)
            {
                var renderer = resources.rendererData[i];

                // Grab this renderer's shading atlas allocation information.
                var shadingAtlasAllocation = GetShadingAtlasAllocationForRenderer(renderer);

                // Reset per-renderer counters & vertex visibility
                cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 4, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 4, 1, 1 ,1);

                cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 0, ShaderIDs._ShadingSampleVisibilityBuffer, transientBuffers.shadingScratchBuffer);
                cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, renderer.mesh.vertexCount + 1);
                cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 0, DivRoundUp(renderer.mesh.vertexCount + 1, 256), 1 ,1);

                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesVertexSetup)))
                using (new BindRendererToComputeKernel(cmd, renderer))
                {
                    resources.shaderVariablesBuffer.Set(cmd, renderer.vertexSetupCompute, ShaderIDs._ConstantBuffer);

                    cmd.SetComputeFloatParam(renderer.vertexSetupCompute, ShaderIDs._LOD, renderer.lodMode != RendererLODMode.None ? renderer.lod : 1f);
                    cmd.SetComputeIntParam(renderer.vertexSetupCompute, ShaderIDs._VertexOffset, resources.offsetsVertex[i]);
                    cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                    cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex1RecordBuffer, buffers.vertexStream1);
                    cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex2RecordBuffer, buffers.vertexStream2);
                    cmd.SetComputeBufferParam(renderer.vertexSetupCompute, 0, ShaderIDs._Vertex3RecordBuffer, buffers.vertexStream3);
                    cmd.DispatchCompute(renderer.vertexSetupCompute, 0, DivRoundUp(renderer.mesh.vertexCount, 128), 1, 1);
                }

                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesSegmentSetup)))
                {
                    bool needs16BitIndices = renderer.mesh.indexFormat == IndexFormat.UInt16;
                    cmd.SetKeyword(resources.systemResources.stageSetupSegmentCS, Instance.m_SegmentIndicesKeywords[(int)IndexFormat.UInt16], needs16BitIndices);
                    cmd.SetKeyword(resources.systemResources.stageSetupSegmentCS, Instance.m_SegmentIndicesKeywords[(int)IndexFormat.UInt32], !needs16BitIndices);

                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._LODBuffer, renderer.lodBuffer);
                    cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, ShaderIDs._SegmentsPerLine, renderer.segmentsPerLine);
                    cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, ShaderIDs._LineCount, renderer.lineCount);
                    cmd.SetComputeFloatParam(resources.systemResources.stageSetupSegmentCS, ShaderIDs._LOD, renderer.lodMode != RendererLODMode.None ? renderer.lod : 1f);

                    cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, ShaderIDs._VertexOffset, resources.offsetsVertex[i]);
                    cmd.SetComputeIntParam(resources.systemResources.stageSetupSegmentCS, ShaderIDs._SegmentOffset, (int)renderer.mesh.GetIndexCount(0) / 2); // TODO: Rename to segment count
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._IndexBuffer, renderer.indexBuffer);
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS,  0, ShaderIDs._ViewSpaceDepthRangeBuffer, buffers.viewSpaceDepthRange);
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.SetComputeBufferParam(resources.systemResources.stageSetupSegmentCS, 0, ShaderIDs._ShadingSampleVisibilityBuffer, transientBuffers.shadingScratchBuffer);
                    cmd.SetComputeTextureParam(resources.systemResources.stageSetupSegmentCS, 0, HDShaderIDs._CameraDepthTexture, resources.depthRT);
                    cmd.DispatchCompute(resources.systemResources.stageSetupSegmentCS, 0, DivRoundUp((int)renderer.mesh.GetIndexCount(0) / 2, ShaderVariables.NumLaneSegmentSetup), 1, 1);
                }

                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesShadingPrepare)))
                {
                    bool atlasSizeChanged = shadingAtlasAllocation.previousSize > 0 &&
                                            shadingAtlasAllocation.previousSize !=
                                            shadingAtlasAllocation.currentSize;

                    if (renderer.shadingFraction < 1.0f)
                    {
                        int sampleIDOffset = shadingAtlasAllocation.updateCount % GeometryPassData.Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE;
                        int maxSamplesToShade = (int)(renderer.shadingFraction * shadingAtlasAllocation.currentSize);

                        //reproject shading from previous frame or clear
                        if (shadingAtlasAllocation.previousOffset != -1 && shadingAtlasAllocation.updateCount > 0 &&!atlasSizeChanged)
                        {
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, shadingAtlasAllocation.currentSize);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._ShadingAtlasSampleOffset, shadingAtlasAllocation.currentOffset);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceShadingAtlasSampleOffset, shadingAtlasAllocation.previousOffset);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureWidth, shadingHistoryAtlasCurrent.rt.width);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS,  ShaderIDs._TargetTextureHeight, shadingHistoryAtlasCurrent.rt.height);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureWidth, shadingHistoryAtlasPrevious.rt.width);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureHeight, shadingHistoryAtlasPrevious.rt.height);

                            cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingSamplesTexture, shadingHistoryAtlasCurrent);
                            cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingScratchTexture, shadingHistoryAtlasPrevious);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 4, DivRoundUp(shadingAtlasAllocation.currentSize, 256), 1 ,1);
                        }
                        else
                        {
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, shadingAtlasAllocation.currentSize);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._ShadingAtlasSampleOffset, shadingAtlasAllocation.currentOffset);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureWidth, shadingHistoryAtlasCurrent.rt.width);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureHeight, shadingHistoryAtlasCurrent.rt.height);
                            cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 2, ShaderIDs._ShadingSamplesTexture, shadingHistoryAtlasCurrent);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 2, DivRoundUp(shadingAtlasAllocation.currentSize, 256), 1 ,1);
                        }

                        if (maxSamplesToShade > 0)
                        {
                            //clear histogram
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,3,  ShaderIDs._HistogramBuffer, transientBuffers.shadingSampleHistogram);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 3, 1, 1 ,1);

                            //calculate histogram for sample "ID"s
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, shadingAtlasAllocation.currentSize);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleIDOffset, sampleIDOffset);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,7,  ShaderIDs._HistogramBuffer, transientBuffers.shadingSampleHistogram);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,7,  ShaderIDs._ShadingSampleVisibilityBuffer, transientBuffers.shadingScratchBuffer);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 7, DivRoundUp(shadingAtlasAllocation.currentSize, GeometryPassData.Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE), 1 ,1);

                            //prefixsum the histogram
                            resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                            {
                                exclusive = true,
                                inputCount = GeometryPassData.Buffers.SHADING_SAMPLE_HISTOGRAM_SIZE,
                                input = transientBuffers.shadingSampleHistogram,
                                supportResources = prefixResources
                            });

                            //select highest ID to shade
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._MaxSamplesToShade, maxSamplesToShade);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8,  ShaderIDs._HistogramBuffer, transientBuffers.shadingSampleHistogram);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8, ShaderIDs._PrefixSumBuffer, prefixResources.output);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,8,  ShaderIDs._ShadingSampleVisibilityBuffer, transientBuffers.shadingScratchBuffer);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 8, 1, 1 ,1);

                            //mark rejected IDs as non visible
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, shadingAtlasAllocation.currentSize);
                            cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleIDOffset, sampleIDOffset);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,9,  ShaderIDs._HistogramBuffer, transientBuffers.shadingSampleHistogram);
                            cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS,9,  ShaderIDs._ShadingSampleVisibilityBuffer, transientBuffers.shadingScratchBuffer);
                            cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 9, DivRoundUp(shadingAtlasAllocation.currentSize, 256), 1 ,1);
                        }

                    }

                    //prefixsum visible samples
                    resources.systemResources.gpuPrefixSum.DispatchDirect(cmd, new GPUPrefixSum.DirectArgs
                    {
                        exclusive = true,
                        inputCount = renderer.mesh.vertexCount + 1,
                        input = transientBuffers.shadingScratchBuffer,
                        supportResources = prefixResources
                    });

                    {
                        //clear compaction buffer
                        cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 1, ShaderIDs._ShadingCompactionBuffer, transientBuffers.shadingScratchBuffer);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, renderer.mesh.vertexCount);
                        cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 1, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);

                        // Write out mapping from compacted shading index to non-compated
                        cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 5, ShaderIDs._ShadingCompactionBuffer, transientBuffers.shadingScratchBuffer);
                        cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 5, ShaderIDs._PrefixSumBuffer, prefixResources.output);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, renderer.mesh.vertexCount);
                        cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 5, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);
                    }
                }

                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.LinesShading)))
                {
                    Vector2Int scratchAtlasSize = transientBuffers.shadingScratchTextureDimensions;

                    var offscreenViewport = new Rect(
                        0, 0, scratchAtlasSize.x, scratchAtlasSize.y
                    );

                    var mpb = resources.materialPropertyBlock;
                    {
                        resources.shaderVariablesBuffer.Set(mpb, ShaderIDs._ConstantBuffer);

                        mpb.SetBuffer(ShaderIDs._Vertex0RecordBuffer, buffers.vertexStream0);
                        mpb.SetBuffer(ShaderIDs._Vertex2RecordBuffer, buffers.vertexStream2);
                        mpb.SetBuffer(ShaderIDs._Vertex3RecordBuffer, buffers.vertexStream3);

                        // TODO: Might need to set the premultiply keyword as well for fog.
                        // TODO: Currently hard coding HDRP blendmode value (pre-multiply).
                        mpb.SetFloat("_BlendMode", 4f);

                        mpb.SetMatrix("_InverseCamMatNoJitter", resources.matrixIVP);

                        mpb.SetBuffer(ShaderIDs._CounterBuffer, buffers.counterBuffer);
                        mpb.SetBuffer(ShaderIDs._SegmentRecordBuffer, buffers.recordBufferSegment);
                        mpb.SetBuffer(ShaderIDs._ShadingCompactionBuffer, transientBuffers.shadingScratchBuffer);
                        mpb.SetInteger(ShaderIDs._SoftwareLineOffscreenAtlasWidth, scratchAtlasSize.x);
                        mpb.SetInteger(ShaderIDs._SoftwareLineOffscreenAtlasHeight, scratchAtlasSize.y);
                        mpb.SetInteger(ShaderIDs._ShadingSampleVisibilityCount, renderer.mesh.vertexCount);
                        mpb.SetInteger("_VertexOffset", resources.offsetsVertex[i]);

                        // Need to manually reinterpret the bits from uint -> float (and then back to uint on GPU..).
                        mpb.SetVector("unity_RenderingLayer", new Vector4(BitConverter.Int32BitsToSingle((int)renderer.renderingLayerMask), 0f, 0f, 0f));
                        mpb.SetVector("unity_RendererBounds_Min", renderer.bounds.min);
                        mpb.SetVector("unity_RendererBounds_Max", renderer.bounds.max);

                        mpb.CopySHCoefficientArraysFrom( new []{ renderer.probe } );
                    }

                    CoreUtils.SetRenderTarget(cmd, transientBuffers.shadingScratchTexture);
                    cmd.SetViewport(offscreenViewport);

                    // Workaround for what seems like a bug in xbox, writes from scratch to shading atlas seem to get previous values of scratch.
                    CoreUtils.ClearRenderTarget(cmd, ClearFlag.Color, Color.black);

                    cmd.DrawProcedural(Matrix4x4.identity, renderer.material, renderer.offscreenShadingPass, MeshTopology.Triangles, 6, 1, mpb);
                }

                //if using shading history,unpack compacted shading samples first to history, then to the group shading atlas. Otherwise, directly unpack to group atlas
                if(renderer.shadingFraction < 1)
                {
                    //copy to history
                    {
                        Vector2Int scratchAtlasSize = transientBuffers.shadingScratchTextureDimensions;
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureWidth, shadingHistoryAtlasCurrent.rt.width);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureHeight, shadingHistoryAtlasCurrent.rt.height);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureWidth, scratchAtlasSize.x);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureHeight, scratchAtlasSize.y);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._ShadingAtlasSampleOffset, shadingAtlasAllocation.currentOffset);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, renderer.mesh.vertexCount);
                        cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingCompactionBuffer, transientBuffers.shadingScratchBuffer);
                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingScratchTexture, transientBuffers.shadingScratchTexture);
                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingSamplesTexture, shadingHistoryAtlasCurrent);
                        cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 6, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);
                    }

                    //copy history to group shading atlas
                    {
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, shadingAtlasAllocation.currentSize);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._ShadingAtlasSampleOffset, resources.offsetsVertex[i]);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceShadingAtlasSampleOffset, shadingAtlasAllocation.currentOffset);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureWidth, shadingSampleAtlasDimensions.x);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureHeight, shadingSampleAtlasDimensions.y);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureWidth, shadingHistoryAtlasCurrent.rt.width);
                        cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureHeight, shadingHistoryAtlasCurrent.rt.height);

                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingSamplesTexture, shadingSampleAtlas);
                        cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 4, ShaderIDs._ShadingScratchTexture, shadingHistoryAtlasCurrent);
                        cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 4, DivRoundUp(shadingAtlasAllocation.currentSize, 256), 1 ,1);
                    }

                }
                else
                {
                    Vector2Int scratchAtlasSize = transientBuffers.shadingScratchTextureDimensions;
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureWidth, shadingSampleAtlasDimensions.x);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._TargetTextureHeight, shadingSampleAtlasDimensions.y);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureWidth, scratchAtlasSize.x);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SourceTextureHeight, scratchAtlasSize.y);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._ShadingAtlasSampleOffset, resources.offsetsVertex[i]);
                    cmd.SetComputeIntParam(resources.systemResources.stageShadingSetupCS, ShaderIDs._SampleCount, renderer.mesh.vertexCount);
                    cmd.SetComputeBufferParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingCompactionBuffer, transientBuffers.shadingScratchBuffer);
                    cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingScratchTexture, transientBuffers.shadingScratchTexture);
                    cmd.SetComputeTextureParam(resources.systemResources.stageShadingSetupCS, 6, ShaderIDs._ShadingSamplesTexture, shadingSampleAtlas);
                    cmd.DispatchCompute(resources.systemResources.stageShadingSetupCS, 6, DivRoundUp(renderer.mesh.vertexCount, 256), 1, 1);
                }

                // Update the offsets.
                {
                    cmd.SetComputeBufferParam(resources.systemResources.stagePrepareCS, 5, ShaderIDs._CounterBuffer, buffers.counterBuffer);
                    cmd.DispatchCompute(resources.systemResources.stagePrepareCS, 5, 1, 1 ,1);
                }
            }
        }
    }
}
