using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System.Collections.Generic;

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
        public BoolParameter clear = new BoolParameter(false);
    }

    internal class ProbeDynamicGISystem
    {
        RenderPipelineResources m_Resources;
        RenderPipelineSettings m_Settings;

        Vector3Int m_LastAllocatedDimensions = new Vector3Int(-1, -1, -1);

        internal ProbeDynamicGISystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = defaultResources;
        }

        internal struct DynamicGIAPV
        {
            public RTHandle L0_L1Rx;
            public RTHandle L1_G_ry;
            public RTHandle L1_B_rz;

            public void Allocate(Vector3Int dimension)
            {
                L0_L1Rx = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L0 and L1R.x dynamic GI APV");
                L1_G_ry = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 G and L1R.y dynamic GI APV");
                L1_B_rz = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 B and L1R.z dynamic GI APV");

                Graphics.SetRenderTarget(L0_L1Rx, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);

                Graphics.SetRenderTarget(L1_G_ry, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);

                Graphics.SetRenderTarget(L1_B_rz, 0, CubemapFace.Unknown, depthSlice: -1);
                GL.Clear(false, true, Color.clear);
            }

            public void Cleanup()
            {
                RTHandles.Release(L0_L1Rx);
                RTHandles.Release(L1_G_ry);
                RTHandles.Release(L1_B_rz);
            }
        }

        DynamicGIAPV m_DynamicGIAPV;
        DynamicGIAPV m_PrevDynamicGI;


        internal void SwapHistory()
        {
            var tmp = m_PrevDynamicGI;
            m_PrevDynamicGI = m_DynamicGIAPV;
            m_DynamicGIAPV = tmp;
        }

        internal struct DynamicGIResources
        {
            public RTHandle L0_L1Rx;
            public RTHandle L1_G_ry;
            public RTHandle L1_B_rz;
        }

        internal DynamicGIResources GetRuntimeResources()
        {
            DynamicGIResources dr;
            dr.L0_L1Rx = m_DynamicGIAPV.L0_L1Rx;
            dr.L1_G_ry = m_DynamicGIAPV.L1_G_ry;
            dr.L1_B_rz = m_DynamicGIAPV.L1_B_rz;
            return dr;
        }

        // Note: The dimensions passed need to match whatever is set for the main (static ligthing) Reference Volume
        internal void AllocateDynamicGIResources(Vector3Int dimensions)
        {
            if (m_LastAllocatedDimensions != dimensions)
            {
                m_DynamicGIAPV.Cleanup();
                m_PrevDynamicGI.Cleanup();

                m_DynamicGIAPV.Allocate(dimensions);
                m_PrevDynamicGI.Allocate(dimensions);
                m_LastAllocatedDimensions = dimensions;
            }

            ProbeReferenceVolume.instance.InitExtraDataBuffers();
        }

        internal void CleanupDynamicGIResources()
        {
            m_DynamicGIAPV.Cleanup();
            m_PrevDynamicGI.Cleanup();
        }

        // ---------------------------------------------------------------------
        // --------------------------- Force Nuke ------------------------------
        // ---------------------------------------------------------------------
        class ClearTexturesData
        {
            public TextureHandle apvL0L1rx;
            public TextureHandle apvL1Gry;
            public TextureHandle apvL1Brz;
        }

        void ClearTextureContent(RenderGraph renderGraph, DynamicGIAPV apvToClear)
        {
            var apvL0L1rxHandle = renderGraph.ImportTexture(apvToClear.L0_L1Rx);
            var apvL1GryHandle = renderGraph.ImportTexture(apvToClear.L1_G_ry);
            var apvL1BrzHandle = renderGraph.ImportTexture(apvToClear.L1_B_rz);

            using (var builder = renderGraph.AddRenderPass<ClearTexturesData>("Clear APVs", out var passData, ProfilingSampler.Get(HDProfileId.ClearBuffers)))
            {
                passData.apvL0L1rx = apvL0L1rxHandle;
                passData.apvL1Gry = apvL1GryHandle;
                passData.apvL1Brz = apvL1BrzHandle;

                builder.SetRenderFunc(
                    (ClearTexturesData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetRenderTarget(data.apvL0L1rx, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                        ctx.cmd.SetRenderTarget(data.apvL1Gry, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                        ctx.cmd.SetRenderTarget(data.apvL1Brz, 0, CubemapFace.Unknown, depthSlice: -1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);
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

            public int probeCount;
            public Vector4 injectionParameters;
            public Vector4 injectionParameters2;
            public Vector4 injectionParameters3;
            public Vector4 injectionParameters4;

            public ComputeBufferHandle probeFinalExtraDataBuffer;
            public ComputeBufferHandle probePositionsBuffer;
            public ComputeBufferHandle irradianceCacheBuffer;
            public ComputeBufferHandle prevIrradianceCacheBuffer;

            public TextureHandle apvL0L1rx;
            public TextureHandle apvL1Gry;
            public TextureHandle apvL1Brz;

            public TextureHandle prevApvL0L1rx;
            public TextureHandle prevApvL1Gry;
            public TextureHandle prevApvL1Brz;

            public bool clear;

            public int[] chunkIndices;
        }

        void PrepareLightPropagationData(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera, ref LightPropagationData data)
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
                buffers.ClearIrradianceCaches();
            }

            data.irradianceCacheBuffer = renderGraph.ImportComputeBuffer(buffers.irradianceCache);
            data.prevIrradianceCacheBuffer = renderGraph.ImportComputeBuffer(buffers.prevIrradianceCache);

            float probeDistance = ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
            data.injectionParameters = new Vector4(probeDistance, giSettings.minDist.value, giSettings.bias.value, data.probeCount);
            data.injectionParameters2 = new Vector4(ProbeReferenceVolume.instance.poolDimension.x, ProbeReferenceVolume.instance.poolDimension.y, ProbeReferenceVolume.instance.poolDimension.z, ProbeReferenceVolume.instance.chunkSizeInProbes);
            data.injectionParameters3 = new Vector4(giSettings.primaryDecay.value, giSettings.leakMultiplier.value, giSettings.intensityScale.value, giSettings.antiRingingFactor.value);
            data.injectionParameters4 = new Vector4(buffers.hitProbesAxisCount, buffers.missProbesAxisCount, probeDistance, giSettings.propagationDecay.value);


            data.chunkIndices = chunkIndices;

            data.apvL0L1rx = renderGraph.ImportTexture(m_DynamicGIAPV.L0_L1Rx); ;
            data.apvL1Gry = renderGraph.ImportTexture(m_DynamicGIAPV.L1_G_ry);
            data.apvL1Brz = renderGraph.ImportTexture(m_DynamicGIAPV.L1_B_rz);
            data.prevApvL0L1rx = renderGraph.ImportTexture(m_PrevDynamicGI.L0_L1Rx);
            data.prevApvL1Gry = renderGraph.ImportTexture(m_PrevDynamicGI.L1_G_ry);
            data.prevApvL1Brz = renderGraph.ImportTexture(m_PrevDynamicGI.L1_B_rz);
        }

        void LightPropagation(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<LightPropagationData>("Inject Direct Light in Dynamic GI APV", out var passData, ProfilingSampler.Get(HDProfileId.InjectInDynamicAPV)))
            {
                PrepareLightPropagationData(renderGraph, buffers, chunkIndices, hdCamera, ref passData);

                builder.SetRenderFunc(
                    (LightPropagationData data, RenderGraphContext ctx) =>
                    {
                        var cs = data.propagationCS;
                        var kernel = data.hitKernel;
                        var cmd = ctx.cmd;

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.prevApvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.prevApvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.prevApvL1Brz);

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.apvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.apvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.apvL1Brz);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, data.probePositionsBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, data.probeFinalExtraDataBuffer);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, data.prevIrradianceCacheBuffer);

                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams0, data.injectionParameters);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams1, data.injectionParameters2);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams2, data.injectionParameters3);
                        cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams3, data.injectionParameters4);

                        Vector4[] indicesArray = new Vector4[8];

                        for (int i = 0; i < data.chunkIndices.Length; ++i)
                        {
                            indicesArray[i / 4][i % 4] = data.chunkIndices[i];
                        }

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._ChunkIndices, indicesArray);

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeExtraData.NeighbourAxis);

                        const int probesPerGroup = 64;
                        int groupCount = HDUtils.DivRoundUp((int)data.injectionParameters4.x, probesPerGroup);

                        cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

                        kernel = data.missKernel;

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, data.prevApvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, data.prevApvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, data.prevApvL1Brz);

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.apvL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.apvL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.apvL1Brz);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, data.probePositionsBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, data.probeFinalExtraDataBuffer);

                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, data.irradianceCacheBuffer);
                        cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, data.prevIrradianceCacheBuffer);

                        groupCount = HDUtils.DivRoundUp((int)data.injectionParameters4.y, probesPerGroup);
                        cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

                        if (data.clear)
                        {
                            cmd.SetRenderTarget(data.apvL0L1rx, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.apvL1Gry, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.apvL1Brz, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.prevApvL0L1rx, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.prevApvL1Gry, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                            cmd.SetRenderTarget(data.prevApvL1Brz, 0, CubemapFace.Unknown, -1);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                        }
                    });
            }
        }

        internal void LightPropagation(RenderGraph renderGraph, HDCamera hdCamera, bool forceClear)
        {
            if (forceClear)
            {
                ClearTextureContent(renderGraph, m_DynamicGIAPV);
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

            public int probeCount;
            public Vector4 combineParameters;
            public Vector4 poolDimensions;
            public int[] chunkIndices;

            public TextureHandle firstL0L1rx;
            public TextureHandle firstL1Gry;
            public TextureHandle firstL1Brz;
            public Texture3D secondL0L1rx;
            public Texture3D secondL1Gry;
            public Texture3D secondL1Brz;

            public ComputeBufferHandle irradianceCacheBuffer;
        }

        void PrepareCombineProbeVolumeData(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, Vector2 blendFactors, int[] chunkIndices, HDCamera hdCamera, ref CombineProbeVolumesData data)
        {
            data.combinePVCS = m_Resources.shaders.combineProbeVolumesCS;
            data.combineKernel = data.combinePVCS.FindKernel("CombineIrradianceCacheAndPV");

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


            data.firstL0L1rx = renderGraph.ImportTexture(m_DynamicGIAPV.L0_L1Rx);
            data.firstL1Gry = renderGraph.ImportTexture(m_DynamicGIAPV.L1_G_ry);
            data.firstL1Brz = renderGraph.ImportTexture(m_DynamicGIAPV.L1_B_rz);
            var rr = ProbeReferenceVolume.instance.GetRuntimeResources();
            data.secondL0L1rx = rr.L0_L1rx;
            data.secondL1Gry = rr.L1_G_ry;
            data.secondL1Brz = rr.L1_B_rz;


            //var apvL1GryHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_G_ry);
            //var apvL1BrzHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_B_rz);

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

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, data.firstL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, data.firstL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, data.firstL1Brz);

                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL0_L1Rx, data.secondL0L1rx);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1G_L1Ry, data.secondL1Gry);
                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1B_L1Rz, data.secondL1Brz);

                        cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeExtraData.NeighbourAxis);

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
                if (buffer.probeCount > 0)
                    CombineProbeVolumes(renderGraph, buffer, indices, hdCamera);
            }
        }
    }
}
