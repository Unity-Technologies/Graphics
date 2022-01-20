using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeDynamicGI : VolumeComponent
    {
        public ClampedFloatParameter bias = new ClampedFloatParameter(0.05f, 0.0f, 0.33f);
        public ClampedFloatParameter minDist = new ClampedFloatParameter(0.01f, 0.0f, 0.2f);
        public ClampedFloatParameter primaryDecay = new ClampedFloatParameter(1.015f, 1.01f, 1.4f);
        public ClampedFloatParameter propagationDecay = new ClampedFloatParameter(1.015f, 1.01f, 1.4f);
        public ClampedFloatParameter leakMultiplier = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter intensityScale = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        public ClampedFloatParameter antiRingingFactor = new ClampedFloatParameter(0.0f, 0.0f, 3.14f);


        public ClampedFloatParameter updateDistanceBehindCamera = new ClampedFloatParameter(10.0f, 0.0f, 100.0f);
        public ClampedFloatParameter updateDistanceInFrontOfCamera = new ClampedFloatParameter(300.0f, 0.0f, 500.0f);


        public BoolParameter clear = new BoolParameter(false);


        ///////// --- HW START
        public LayerMaskParameter rtLayerMask = new LayerMaskParameter(-1);
        //// --- HW END
    }

    internal class ProbeDynamicGISystem
    {
        HDRenderPipelineRuntimeResources m_Resources;
        RenderPipelineSettings m_Settings;

        Vector3Int m_LastAllocatedDimensions = new Vector3Int(-1, -1, -1);

        internal ProbeDynamicGISystem(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = defaultResources;
        }

        internal struct DynamicGIAPV
        {
            public RTHandle L0_L1Rx;
            public RTHandle L1_G_ry;
            public RTHandle L1_B_rz;

            public RTHandle L2_0;
            public RTHandle L2_1;
            public RTHandle L2_2;
            public RTHandle L2_3;


            public void Allocate(Vector3Int dimension, RenderPipelineSettings settings)
            {
                L0_L1Rx = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L0 and L1R.x dynamic GI APV");
                L1_G_ry = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 G and L1R.y dynamic GI APV");
                L1_B_rz = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 B and L1R.z dynamic GI APV");


                if (settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    L2_0 = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L2_R dynamic GI APV");
                    L2_1 = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L2_G dynamic GI APV");
                    L2_2 = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L2_B dynamic GI APV");
                    L2_3 = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L2_C dynamic GI APV");
                }

                Graphics.SetRenderTarget(L0_L1Rx, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);

                Graphics.SetRenderTarget(L1_G_ry, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);

                Graphics.SetRenderTarget(L1_B_rz, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);

                if (settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    Graphics.SetRenderTarget(L2_0, 0, CubemapFace.Unknown, depthSlice: -1);
                    GL.Clear(false, true, Color.clear);

                    Graphics.SetRenderTarget(L2_1, 0, CubemapFace.Unknown, depthSlice: -1);
                    GL.Clear(false, true, Color.clear);

                    Graphics.SetRenderTarget(L2_2, 0, CubemapFace.Unknown, depthSlice: -1);
                    GL.Clear(false, true, Color.clear);

                    Graphics.SetRenderTarget(L2_3, 0, CubemapFace.Unknown, depthSlice: -1);
                    GL.Clear(false, true, Color.clear);
                }
            }

            public void Cleanup(RenderPipelineSettings settings)
            {
                RTHandles.Release(L0_L1Rx);
                RTHandles.Release(L1_G_ry);
                RTHandles.Release(L1_B_rz);

                if (settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    RTHandles.Release(L2_0);
                    RTHandles.Release(L2_1);
                    RTHandles.Release(L2_2);
                    RTHandles.Release(L2_3);
                }
            }
        }

        // TODO_FCC: NOTE We need an additional output APV only because original textures are texture3d and cannot write into them.
        DynamicGIAPV m_OutputAPV;
        DynamicGIAPV m_PrevDynamicGI;

        internal struct DynamicGIResources
        {
            public RTHandle L0_L1Rx;
            public RTHandle L1_G_ry;
            public RTHandle L1_B_rz;

            public RTHandle L2_0;
            public RTHandle L2_1;
            public RTHandle L2_2;
            public RTHandle L2_3;
        }

        internal DynamicGIResources GetRuntimeResources()
        {
            DynamicGIResources dr;
            dr.L0_L1Rx = m_OutputAPV.L0_L1Rx;
            dr.L1_G_ry = m_OutputAPV.L1_G_ry;
            dr.L1_B_rz = m_OutputAPV.L1_B_rz;

            dr.L2_0 = m_OutputAPV.L2_0;
            dr.L2_1 = m_OutputAPV.L2_1;
            dr.L2_2 = m_OutputAPV.L2_2;
            dr.L2_3 = m_OutputAPV.L2_3;

            return dr;
        }

        // Note: The dimensions passed need to match whatever is set for the main (static ligthing) Reference Volume
        internal void AllocateDynamicGIResources(Vector3Int dimensions)
        {
            if (m_LastAllocatedDimensions != dimensions)
            {
                CleanupDynamicGIResources();

                m_OutputAPV.Allocate(dimensions, m_Settings);
                m_PrevDynamicGI.Allocate(dimensions, m_Settings);
                m_LastAllocatedDimensions = dimensions;
            }
        }

        internal void CleanupDynamicGIResources()
        {
            m_OutputAPV.Cleanup(m_Settings);
            m_PrevDynamicGI.Cleanup(m_Settings);
        }

        // ---------------------------------------------------------------------
        // ----------------------------- Culling -------------------------------
        // ---------------------------------------------------------------------
        // ?
        // SHOULD DEFINITIVELY CULL CELLS. Though not here.

         // We produce two lists
         // BURSTIFYING the mega naive way.
        //struct CullProbesJob : IJobParallelFor
        //{
        //    [ReadOnly] public NativeArray<Vector3> probeWorldPos;
        //    public NativeArray<bool> survivingProbes;

        //    public Vector3 cameraPos;
        //    public Vector3 cameraForwardVec;
        //    public float thresholdFront;
        //    public float thresholdBack;

        //    public void Execute(int index)
        //    {
        //        Vector3 pos = probeWorldPos[index];

        //        Vector3 toCamera = pos - cameraPos;
        //        float d = Vector3.Dot(toCamera, cameraForwardVec);
        //        bool surving = (d > 0 && d < thresholdFront) || (d < 0 && -d < thresholdBack);
        //        survivingProbes[index] = surving;
        //    }
        //}

        //static internal List<uint> CullExtraData(HDCamera hdCamera, ProbeReferenceVolume.Cell cell)
        //{
        //    int probeCount = cell.probePositions.Length;
        //    using (var probeLocations = new NativeArray<Vector3>(cell.probePositions, Allocator.TempJob))
        //    using (var surviving = new NativeArray<bool>(cell.probePositions.Length, Allocator.TempJob))
        //    {
        //        var dynGI = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();
        //        Vector3 cameraLoc = hdCamera.camera.transform.position;
        //        Vector3 cameraFwd = hdCamera.camera.transform.forward;
        //        float threshFront = dynGI.updateDistanceInFrontOfCamera.value;
        //        float threshBack = dynGI.updateDistanceBehindCamera.value;

        //        const int kBatchSize = 512;

        //        new CullProbesJob
        //        {
        //            probeWorldPos = probeLocations,
        //            survivingProbes = surviving,
        //            cameraPos = cameraLoc,
        //            cameraForwardVec = cameraFwd,
        //            thresholdFront = threshFront,
        //            thresholdBack = threshBack

        //        }.Schedule(probeCount, kBatchSize).Complete();

        //        // We consolidate in one list the surviving one, the sorting is pointless if we go through the raytracing temp scale
        //        bool rayTracingUpdate = true; // TODO_HW TODO_FCC: For HW sake I'll assume we always do the RT stuff.

        //        List<uint> finalCulledData = new List<uint>(cell.probeExtraDataBuffers.hitProbesAxisCount + cell.probeExtraDataBuffers.missProbesAxisCount);

        //        if (rayTracingUpdate)
        //        {
        //            for (int probe = 0; probe < surviving.Length; ++probe)
        //            {
        //                if (surviving[probe])
        //                {
        //                    for (int axis = 0; axis < ProbeDynamicGIManager.s_AxisCount; ++axis)
        //                    {
        //                        int entryIdx = probe * ProbeDynamicGIManager.s_AxisCount + axis;

        //                        finalCulledData.Add(cell.packedExtraData[entryIdx].packedAlbedoAndDist);
        //                        finalCulledData.Add(cell.packedExtraData[entryIdx].packedNormal);
        //                        finalCulledData.Add(cell.packedExtraData[entryIdx].packedIndices);
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            // Now we have the cull info, we put in the right list (NOTE, is this too slow?)

        //            List<uint> hitIndices = new List<uint>(cell.probeExtraDataBuffers.hitProbesAxisCount * 3);
        //            List<uint> missesIndices = new List<uint>(cell.probeExtraDataBuffers.missProbesAxisCount * 3);

        //            for (int probe = 0; probe < surviving.Length; ++probe)
        //            {
        //                if (surviving[probe])
        //                {
        //                    // We add the extra data, but we need to check what is a hit and what a miss.
        //                    for (int axis = 0; axis < ProbeDynamicGIManager.s_AxisCount; ++axis)
        //                    {
        //                        int entryIdx = probe * ProbeDynamicGIManager.s_AxisCount + axis;

        //                        if (cell.packedExtraData[entryIdx].hit)
        //                        {
        //                            hitIndices.Add(cell.packedExtraData[entryIdx].packedAlbedoAndDist);
        //                            hitIndices.Add(cell.packedExtraData[entryIdx].packedNormal);
        //                            hitIndices.Add(cell.packedExtraData[entryIdx].packedIndices);
        //                        }
        //                        else
        //                        {
        //                            missesIndices.Add(cell.packedExtraData[entryIdx].packedAlbedoAndDist);
        //                            missesIndices.Add(cell.packedExtraData[entryIdx].packedNormal);
        //                            missesIndices.Add(cell.packedExtraData[entryIdx].packedIndices);
        //                        }
        //                    }
        //                }
        //            }

        //            // Consolidate TODO: Can be made faster.
        //            cell.probeExtraDataBuffers.hitProbesAxisCount = hitIndices.Count / 3;
        //            cell.probeExtraDataBuffers.missProbesAxisCount = missesIndices.Count / 3;
        //            // TODO: This I think GC Alloc, lol.
        //            finalCulledData.AddRange(hitIndices);
        //            finalCulledData.AddRange(missesIndices);
        //        }

        //        return finalCulledData;
        //    }
        //}


        // Create the indirect arg lists and append buffers from the output buffer coming from GenerateExtraDataRealtime.
        internal struct DynamicGIInputs
        {
            public ComputeBufferHandle hitsExtraData;
            public ComputeBufferHandle missesExtraData;
            public ComputeBufferHandle counts;
            public ComputeBufferHandle indirectArgs;
            public int culledCount;
        }

        DynamicGIInputs DynamicGIInputGen(CommandBuffer cmd, LightPropagationData propData)
        {
            var cs = propData.inputGenCS;
            var kernel = propData.indirectArgsClearKernel;
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, propData.inputs.indirectArgs);
            cmd.SetComputeBufferParam(cs, kernel, "_Counts", propData.inputs.counts);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);

            cs = propData.bucketingCS;
            kernel = propData.probeCountKernel;
            ((ComputeBuffer)propData.inputs.hitsExtraData).SetCounterValue(0u);
            ((ComputeBuffer)propData.inputs.missesExtraData).SetCounterValue(0u);

            cmd.SetComputeBufferParam(cs, kernel, "_Counts", propData.inputs.counts);
            cmd.SetComputeIntParam(cs, "_Count", propData.inputs.culledCount);
            cmd.SetComputeBufferParam(cs, kernel, "_Hits", propData.inputs.hitsExtraData);
            cmd.SetComputeBufferParam(cs, kernel, "_Misses", propData.inputs.missesExtraData);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, propData.probeFinalExtraDataBuffer);
            var groupCount = HDUtils.DivRoundUp(propData.inputs.culledCount, 64);
            cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

            // Gen the indirect buffer
            cs = propData.inputGenCS;
            kernel = propData.inputGenKernel;
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IndirectBuffer, propData.inputs.indirectArgs);
            cmd.SetComputeBufferParam(cs, kernel, "_Counts", propData.inputs.counts);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);

            return propData.inputs;
        }

        // ---------------------------------------------------------------------
        // --------------------------- Force Nuke ------------------------------
        // ---------------------------------------------------------------------
        class ClearTexturesData
        {
            public TextureHandle apvL0L1rx;
            public TextureHandle apvL1Gry;
            public TextureHandle apvL1Brz;

            public TextureHandle apvL2_0;
            public TextureHandle apvL2_1;
            public TextureHandle apvL2_2;
            public TextureHandle apvL2_3;
        }

        void ClearTextureContent(RenderGraph renderGraph, DynamicGIAPV apvToClear)
        {
            var apvL0L1rxHandle = renderGraph.ImportTexture(apvToClear.L0_L1Rx);
            var apvL1GryHandle = renderGraph.ImportTexture(apvToClear.L1_G_ry);
            var apvL1BrzHandle = renderGraph.ImportTexture(apvToClear.L1_B_rz);

            var apvL20Handle = renderGraph.ImportTexture(apvToClear.L2_0);
            var apvL21Handle = renderGraph.ImportTexture(apvToClear.L2_1);
            var apvL22Handle = renderGraph.ImportTexture(apvToClear.L2_2);
            var apvL23Handle = renderGraph.ImportTexture(apvToClear.L2_3);

            using (var builder = renderGraph.AddRenderPass<ClearTexturesData>("Clear APVs", out var passData, ProfilingSampler.Get(HDProfileId.ClearBuffers)))
            {
                passData.apvL0L1rx = apvL0L1rxHandle;
                passData.apvL1Gry = apvL1GryHandle;
                passData.apvL1Brz = apvL1BrzHandle;

                passData.apvL2_0 = apvL20Handle;
                passData.apvL2_1 = apvL21Handle;
                passData.apvL2_2 = apvL22Handle;
                passData.apvL2_3 = apvL23Handle;

                builder.SetRenderFunc(
                    (ClearTexturesData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetRenderTarget(data.apvL0L1rx, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                        ctx.cmd.SetRenderTarget(data.apvL1Gry, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                        ctx.cmd.SetRenderTarget(data.apvL1Brz, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            ctx.cmd.SetRenderTarget(data.apvL2_0, 0, CubemapFace.Unknown, depthSlice: -1);
                            ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                            ctx.cmd.SetRenderTarget(data.apvL2_1, 0, CubemapFace.Unknown, depthSlice: -1);
                            ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                            ctx.cmd.SetRenderTarget(data.apvL2_2, 0, CubemapFace.Unknown, depthSlice: -1);
                            ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                            ctx.cmd.SetRenderTarget(data.apvL2_3, 0, CubemapFace.Unknown, depthSlice: -1);
                            ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                        }
                    });
            }
        }

        // ---------------------------------------------------------------------
        // --------------------------- Injection -------------------------------
        // ---------------------------------------------------------------------

        class LightPropagationData
        {
            public ComputeShader propagationCS;
            public int hitKernel;
            public int missKernel;
            public int indirectHitKernel;
            public int inidrectMissKernel;

            public ComputeShader inputGenCS;
            public int inputGenKernel;
            public ComputeShader bucketingCS;
            public int probeCountKernel;
            public int indirectArgsClearKernel;
            public DynamicGIInputs inputs;

            public int probeCount;
            public Vector4 injectionParameters;
            public Vector4 injectionParameters2;
            public Vector4 injectionParameters3;
            public Vector4 injectionParameters4;
            public Vector4 injectionParameters5;

            public ComputeBufferHandle probeFinalExtraDataBuffer;
            public ComputeBufferHandle probePositionsBuffer;
            public ComputeBufferHandle irradianceCacheBuffer;
            public ComputeBufferHandle prevIrradianceCacheBuffer;

            public TextureHandle apvL0L1rx;
            public TextureHandle apvL1Gry;
            public TextureHandle apvL1Brz;

            public TextureHandle apvL2_0;
            public TextureHandle apvL2_1;
            public TextureHandle apvL2_2;
            public TextureHandle apvL2_3;

            public TextureHandle prevApvL0L1rx;
            public TextureHandle prevApvL1Gry;
            public TextureHandle prevApvL1Brz;

            public TextureHandle prevApvL2_0;
            public TextureHandle prevApvL2_1;
            public TextureHandle prevApvL2_2;
            public TextureHandle prevApvL2_3;

            public bool clear;

            public int[] chunkIndices;

            
        }

        void PrepareLightPropagationData(RenderGraph renderGraph, RenderGraphBuilder builder, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera, ref LightPropagationData data)
        {
            data.propagationCS = m_Resources.shaders.probeGIInjectionCS;
            data.missKernel = data.propagationCS.FindKernel("GatherFirstBounceMiss");
            data.hitKernel = data.propagationCS.FindKernel("GatherFirstBounceHit");

            data.probeCount = buffers.probeCount;
            data.probePositionsBuffer = renderGraph.ImportComputeBuffer(buffers.probeLocationBuffer);
            data.probeFinalExtraDataBuffer = renderGraph.ImportComputeBuffer(buffers.finalExtraDataBuffer);

            var giSettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();

            data.clear = giSettings.clear.value || (hdCamera.cameraFrameCount == 0);

            if (data.clear)
            {
                buffers.ClearIrradianceCaches(data.probeCount, ProbeDynamicGIManager.s_AxisCount);
            }

            data.irradianceCacheBuffer = renderGraph.ImportComputeBuffer(buffers.irradianceCache);
            data.prevIrradianceCacheBuffer = renderGraph.ImportComputeBuffer(buffers.prevIrradianceCache);

            float probeDistance = ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
            data.injectionParameters = new Vector4(probeDistance, giSettings.minDist.value, giSettings.bias.value, data.probeCount);
            data.injectionParameters2 = new Vector4(ProbeReferenceVolume.instance.poolDimension.x, ProbeReferenceVolume.instance.poolDimension.y, ProbeReferenceVolume.instance.poolDimension.z, ProbeReferenceVolume.instance.chunkSizeInProbes);
            data.injectionParameters3 = new Vector4(giSettings.primaryDecay.value, giSettings.leakMultiplier.value, giSettings.intensityScale.value, giSettings.antiRingingFactor.value);
            data.injectionParameters4 = new Vector4(buffers.hitProbesAxisCount, buffers.missProbesAxisCount, probeDistance, giSettings.propagationDecay.value);
            data.injectionParameters5 = new Vector4(giSettings.updateDistanceBehindCamera.value, giSettings.updateDistanceInFrontOfCamera.value, 0.0f, 0.0f);


            data.chunkIndices = chunkIndices;

            data.apvL0L1rx = renderGraph.ImportTexture(m_OutputAPV.L0_L1Rx);
            data.apvL1Gry = renderGraph.ImportTexture(m_OutputAPV.L1_G_ry);
            data.apvL1Brz = renderGraph.ImportTexture(m_OutputAPV.L1_B_rz);

            data.apvL2_0 = renderGraph.ImportTexture(m_OutputAPV.L2_0);
            data.apvL2_1 = renderGraph.ImportTexture(m_OutputAPV.L2_1);
            data.apvL2_2 = renderGraph.ImportTexture(m_OutputAPV.L2_2);
            data.apvL2_3 = renderGraph.ImportTexture(m_OutputAPV.L2_3);


            data.prevApvL0L1rx = renderGraph.ImportTexture(m_PrevDynamicGI.L0_L1Rx);
            data.prevApvL1Gry = renderGraph.ImportTexture(m_PrevDynamicGI.L1_G_ry);
            data.prevApvL1Brz = renderGraph.ImportTexture(m_PrevDynamicGI.L1_B_rz);

            data.prevApvL2_0 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_0);
            data.prevApvL2_1 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_1);
            data.prevApvL2_2 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_2);
            data.prevApvL2_3 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_3);



            // For input generation.
            data.inputGenCS = m_Resources.shaders.dynGIIndirectArgGen;
            data.inputGenKernel = data.inputGenCS.FindKernel("DynGIIndirectArgs");
            data.indirectArgsClearKernel = data.inputGenCS.FindKernel("KClear");

            data.bucketingCS = m_Resources.shaders.countAndBucketCS;
            data.probeCountKernel = data.bucketingCS.FindKernel("KCountAndBucket");

            data.inputs.culledCount = buffers.hitProbesAxisCount + buffers.missProbesAxisCount;
            // data.inputs.hitsExtraData =
            data.inputs.indirectArgs = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(3 * 2, sizeof(uint), ComputeBufferType.IndirectArguments) { name = "Dyn GI Input gen Indirect Cmd" });
            data.inputs.hitsExtraData = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(data.probeCount * ProbeDynamicGIManager.s_AxisCount, 3*sizeof(uint), ComputeBufferType.Append) { name = "Hit List" });
            data.inputs.missesExtraData = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(data.probeCount * ProbeDynamicGIManager.s_AxisCount, 3 * sizeof(uint), ComputeBufferType.Append) { name = "Miss List" });
            data.inputs.counts = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(2, sizeof(uint)) { name = "Counts" });

            data.propagationCS.shaderKeywords = null;
            data.propagationCS.EnableKeyword("INDIRECT");
        }

        void LightPropagation(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<LightPropagationData>("Inject Direct Light in Dynamic GI APV", out var passData, ProfilingSampler.Get(HDProfileId.InjectInDynamicAPV)))
            {
                PrepareLightPropagationData(renderGraph, builder, buffers, chunkIndices, hdCamera, ref passData);

                builder.SetRenderFunc(
                    (LightPropagationData data, RenderGraphContext ctx) =>
                    {
                        var inputs = DynamicGIInputGen(ctx.cmd, data);

                        var cs = data.propagationCS;
                        var kernel = data.hitKernel;
                        var cmd = ctx.cmd;

                        cmd.SetComputeBufferParam(cs, kernel, "_Counts", inputs.counts);
                        cmd.SetComputeBufferParam(cs, kernel, "_Hits", inputs.hitsExtraData);

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.prevApvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.prevApvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.prevApvL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_0, data.prevApvL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_1, data.prevApvL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_2, data.prevApvL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_3, data.prevApvL2_3);
                        }


                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.apvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.apvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.apvL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_0, data.apvL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_1, data.apvL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_2, data.apvL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_3, data.apvL2_3);
                        }

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, data.probePositionsBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, data.probeFinalExtraDataBuffer);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, data.prevIrradianceCacheBuffer);

                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams0, data.injectionParameters);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams1, data.injectionParameters2);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams2, data.injectionParameters3);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams3, data.injectionParameters4);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams4, data.injectionParameters5);

                        Vector4[] indicesArray = new Vector4[8];

                        for (int i = 0; i < data.chunkIndices.Length; ++i)
                        {
                            indicesArray[i / 4][i % 4] = data.chunkIndices[i];
                        }

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._ChunkIndices, indicesArray);

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeDynamicGIManager.NeighbourAxis);

                        const int probesPerGroup = 64;
                        int groupCount = HDUtils.DivRoundUp((int)data.injectionParameters4.x, probesPerGroup);

                        cmd.DispatchCompute(cs, kernel, inputs.indirectArgs, 0);

                        //cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

                        kernel = data.missKernel;

                        cmd.SetComputeBufferParam(cs, kernel, "_Counts", inputs.counts);
                        cmd.SetComputeBufferParam(cs, kernel, "_Misses", inputs.missesExtraData);

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.prevApvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.prevApvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.prevApvL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_0, data.prevApvL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_1, data.prevApvL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_2, data.prevApvL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_3, data.prevApvL2_3);
                        }

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.apvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.apvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.apvL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_0, data.apvL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_1, data.apvL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_2, data.apvL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_3, data.apvL2_3);
                        }

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, data.probePositionsBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, data.probeFinalExtraDataBuffer);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, data.prevIrradianceCacheBuffer);

                        if (data.injectionParameters4.y > 0)
                        {
                            groupCount = HDUtils.DivRoundUp((int)data.injectionParameters4.y, probesPerGroup);
                            cmd.DispatchCompute(cs, kernel, inputs.indirectArgs, 3 * sizeof(uint));
                            //cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);
                        }

                        if (data.clear)
                        {
                            cmd.SetRenderTarget(data.apvL0L1rx, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.apvL1Gry, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.apvL1Brz, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);

                            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                cmd.SetRenderTarget(data.apvL2_0, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.apvL2_1, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.apvL2_2, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.apvL2_3, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                            }


                            cmd.SetRenderTarget(data.prevApvL0L1rx, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.prevApvL1Gry, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.prevApvL1Brz, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);

                            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                cmd.SetRenderTarget(data.prevApvL2_0, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.prevApvL2_1, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.prevApvL2_2, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                                cmd.SetRenderTarget(data.prevApvL2_3, 0, CubemapFace.Unknown, depthSlice: -1);
                                cmd.ClearRenderTarget(false, true, Color.clear);
                            }
                        }
                    });
            }
        }

        internal void LightPropagation(RenderGraph renderGraph, HDCamera hdCamera, bool forceClear)
        {
            if (forceClear)
            {
                ClearTextureContent(renderGraph, m_OutputAPV);
                ClearTextureContent(renderGraph, m_PrevDynamicGI);
            }

            var buffersToProcess = ProbeReferenceVolume.instance.GetExtraDataBuffers();
            var chunkIndices = ProbeReferenceVolume.instance.GetChunkIndices();
            for (int i = 0; i < buffersToProcess.Count; ++i)
            {
                var buffer = buffersToProcess[i];

                var indices = chunkIndices[i];

                if (buffer.finalExtraDataBuffer != null && buffer.probeCount > 0)
                    LightPropagation(renderGraph, buffer, indices, hdCamera);
            }
        }

        // ---------------------------------------------------------------------
        // ---------------------------- Combine --------------------------------
        // ---------------------------------------------------------------------

        class CombineProbeVolumesData
        {
            public ComputeShader combinePVCS;
            public int combineKernel;
            public int updateHistoryKernel;

            public int probeCount;
            public Vector4 combineParameters;
            public Vector4 poolDimensions;
            public int[] chunkIndices;

            public TextureHandle outputL0L1rx;
            public TextureHandle outputL1Gry;
            public TextureHandle outputL1Brz;

            public TextureHandle outputL2_0;
            public TextureHandle outputL2_1;
            public TextureHandle outputL2_2;
            public TextureHandle outputL2_3;

            public Texture3D secondL0L1rx;
            public Texture3D secondL1Gry;
            public Texture3D secondL1Brz;

            public Texture3D secondL2_0;
            public Texture3D secondL2_1;
            public Texture3D secondL2_2;
            public Texture3D secondL2_3;

            public TextureHandle historyL0L1rx;
            public TextureHandle historyL1Gry;
            public TextureHandle historyL1Brz;

            public TextureHandle historyL2_0;
            public TextureHandle historyL2_1;
            public TextureHandle historyL2_2;
            public TextureHandle historyL2_3;

            public ComputeBufferHandle irradianceCacheBuffer;
        }

        void PrepareCombineProbeVolumeData(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, Vector2 blendFactors, int[] chunkIndices, HDCamera hdCamera, ref CombineProbeVolumesData data)
        {
            data.combinePVCS = m_Resources.shaders.combineProbeVolumesCS;
            data.combineKernel = data.combinePVCS.FindKernel("CombineIrradianceCacheAndPV");
            data.updateHistoryKernel = data.combinePVCS.FindKernel("WriteToHistory");

            data.probeCount = buffers.probeCount;

            var giSettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();
            if (giSettings.clear.value)
            {
                data.combineParameters = new Vector4(blendFactors.x, 0, buffers.probeCount, giSettings.antiRingingFactor.value);
            }
            else
            {
                data.combineParameters = new Vector4(blendFactors.x, blendFactors.y, buffers.probeCount, giSettings.antiRingingFactor.value);
            }
            data.poolDimensions = new Vector4(ProbeReferenceVolume.instance.poolDimension.x, ProbeReferenceVolume.instance.poolDimension.y, ProbeReferenceVolume.instance.poolDimension.z, ProbeReferenceVolume.instance.chunkSizeInProbes);
            data.chunkIndices = chunkIndices;

            data.irradianceCacheBuffer = renderGraph.ImportComputeBuffer(buffers.irradianceCache);

            data.outputL0L1rx = renderGraph.ImportTexture(m_OutputAPV.L0_L1Rx);
            data.outputL1Gry = renderGraph.ImportTexture(m_OutputAPV.L1_G_ry);
            data.outputL1Brz = renderGraph.ImportTexture(m_OutputAPV.L1_B_rz);

            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                data.outputL2_0 = renderGraph.ImportTexture(m_OutputAPV.L2_0);
                data.outputL2_1 = renderGraph.ImportTexture(m_OutputAPV.L2_1);
                data.outputL2_2 = renderGraph.ImportTexture(m_OutputAPV.L2_2);
                data.outputL2_3 = renderGraph.ImportTexture(m_OutputAPV.L2_3);
            }


            var rr = ProbeReferenceVolume.instance.GetRuntimeResources();
            data.secondL0L1rx = rr.L0_L1rx;
            data.secondL1Gry = rr.L1_G_ry;
            data.secondL1Brz = rr.L1_B_rz;

            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                data.secondL2_0 = rr.L2_0;
                data.secondL2_1 = rr.L2_1;
                data.secondL2_2 = rr.L2_2;
                data.secondL2_3 = rr.L2_3;
            }


            data.historyL0L1rx = renderGraph.ImportTexture(m_PrevDynamicGI.L0_L1Rx);
            data.historyL1Gry = renderGraph.ImportTexture(m_PrevDynamicGI.L1_G_ry);
            data.historyL1Brz = renderGraph.ImportTexture(m_PrevDynamicGI.L1_B_rz);

            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                data.historyL2_0 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_0);
                data.historyL2_1 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_1);
                data.historyL2_2 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_2);
                data.historyL2_3 = renderGraph.ImportTexture(m_PrevDynamicGI.L2_3);
            }
        }

        void CombineProbeVolumes(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<CombineProbeVolumesData>("Combine APV and Irradiance Cache", out var passData, ProfilingSampler.Get(HDProfileId.CombineDynamicGI)))
            {
                PrepareCombineProbeVolumeData(renderGraph, buffers, new Vector2(1.0f, 1.0f), chunkIndices, hdCamera, ref passData);

                builder.SetRenderFunc(
                    (CombineProbeVolumesData data, RenderGraphContext ctx) =>
                    {
                        var cs = data.combinePVCS;
                        var kernel = data.combineKernel;
                        var cmd = ctx.cmd;

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.outputL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.outputL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.outputL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_0, data.outputL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_1, data.outputL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_2, data.outputL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL2_3, data.outputL2_3);
                        }

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.historyL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.historyL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.historyL1Brz);


                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL0_L1Rx, data.secondL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1G_L1Ry, data.secondL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1B_L1Rz, data.secondL1Brz);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL2_0, data.secondL2_0);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL2_1, data.secondL2_1);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL2_2, data.secondL2_2);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL2_3, data.secondL2_3);
                        }

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeDynamicGIManager.NeighbourAxis);

                        Vector4[] indicesArray = new Vector4[8];

                        for (int i = 0; i < data.chunkIndices.Length; ++i)
                        {
                            indicesArray[i / 4][i % 4] = data.chunkIndices[i];
                        }

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._ChunkIndices, indicesArray);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._PVCombineParameters, data.combineParameters);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams1, data.poolDimensions);

                        const int groupSize = 64;
                        int groupCount = HDUtils.DivRoundUp(data.probeCount, groupSize);
                        cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

                        if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        {
                            kernel = data.updateHistoryKernel;
                            // We need to copy history separately to avoid issues with UAV

                            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);

                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.historyL0L1rx);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.historyL1Gry);
                            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.historyL1Brz);

                            if (m_Settings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_0, data.historyL2_0);
                                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_1, data.historyL2_1);
                                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_2, data.historyL2_2);
                                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL2_3, data.historyL2_3);
                            }

                            cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);
                        }
                    });
            }
        }

        internal void CombineDynamicAndStaticPV(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var chunkIndices = ProbeReferenceVolume.instance.GetChunkIndices();
            var buffersToProcess = ProbeReferenceVolume.instance.GetExtraDataBuffers();

            for (int i = 0; i < chunkIndices.Count; ++i)
            {
                var buffer = buffersToProcess[i];
                var indices = chunkIndices[i];
                CombineProbeVolumes(renderGraph, buffer, indices, hdCamera);
            }
        }
    }
}
