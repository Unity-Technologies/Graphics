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
        public ClampedFloatParameter artificialBoost = new ClampedFloatParameter(1.0f, 0.0f, 3.0f);
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
            public RTHandle DEBUG;

            public void Allocate(Vector3Int dimension)
            {
                L0_L1Rx = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L0 and L1R.x dynamic GI APV");
                L1_G_ry = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 G and L1R.y dynamic GI APV");
                L1_B_rz = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "L1 B and L1R.z dynamic GI APV");

                DEBUG = RTHandles.Alloc(dimension.x, dimension.y, dimension.z, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: "DEBUG2");

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
                RTHandles.Release(DEBUG);
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

            var dbg = renderGraph.ImportTexture(apvToClear.DEBUG);

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

        struct DirectLightInjectionParameters
        {
            public ComputeShader injectionCS;
            public ComputeShader injection2CS;
            public ComputeShader injection3CS;
            public int injectionKernel;

            public int injectionKernelHit;
            public int injectionKernelMiss;

            public int probeCount;
            public Vector4 injectionParameters;
            public Vector4 injectionParameters2;
            public Vector4 injectionParameters3;
            public Vector4 injectionParameters4;
            public Vector4 cellOrigin;
            public ComputeBuffer probeFinalExtraDataBuffer;
            public ComputeBuffer probePositionsBuffer;
            public ComputeBuffer irradianceCacheBuffer;
            public ComputeBuffer prevIrradianceCacheBuffer;

            public bool clear;

            public int[] chunkIndices;
        }

        DirectLightInjectionParameters PrepareInjectionParameters(ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            DirectLightInjectionParameters parameters;
            parameters.injectionCS = m_Resources.shaders.probeGIInjectionCS;
            parameters.injection2CS = m_Resources.shaders.probeGIInjectionV2CS;
            parameters.injection3CS = m_Resources.shaders.probeGIInjectionV3CS;
            parameters.injectionKernel = parameters.injectionCS.FindKernel("GatherFirstBounce");

            parameters.injectionKernelHit = parameters.injection3CS.FindKernel("GatherFirstBounceHit");
            parameters.injectionKernelMiss = parameters.injection3CS.FindKernel("GatherFirstBounceMiss");

            parameters.probeCount = buffers.probeCount;
            parameters.probePositionsBuffer = buffers.probeLocationBuffer;
            parameters.probeFinalExtraDataBuffer = buffers.finalExtraDataBuffer;
            parameters.cellOrigin = new Vector4(0, 0, 0, 0);


            var giSettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();

            parameters.clear = giSettings.clear.value || (hdCamera.cameraFrameCount == 0);

            float probeDistance = ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
            parameters.injectionParameters = new Vector4(probeDistance, giSettings.minDist.value, giSettings.bias.value, parameters.probeCount);
            parameters.injectionParameters2 = new Vector4(ProbeReferenceVolume.instance.poolDimension.x, ProbeReferenceVolume.instance.poolDimension.y, ProbeReferenceVolume.instance.poolDimension.z, ProbeReferenceVolume.instance.chunkSizeInProbes);
            parameters.injectionParameters3 = new Vector4(giSettings.primaryDecay.value, giSettings.leakMultiplier.value, giSettings.artificialBoost.value, giSettings.antiRingingFactor.value);
            parameters.injectionParameters4 = new Vector4(buffers.hitProbesAxisCount, buffers.missProbesAxisCount, ProbeReferenceVolume.instance.MinDistanceBetweenProbes(), giSettings.propagationDecay.value);


            parameters.chunkIndices = chunkIndices;

            if (parameters.clear)
            {
                buffers.ClearIrradianceCaches();
            }

            parameters.irradianceCacheBuffer = buffers.irradianceCache;
            parameters.prevIrradianceCacheBuffer = buffers.prevIrradianceCache;

            return parameters;
        }

        static void InjectDirectLighting(in DirectLightInjectionParameters parameters,
            RTHandle apvL0L1rx, RTHandle apvL1Gry, RTHandle apvL1Brz, RTHandle debug,
            RTHandle prevApvL0L1rx, RTHandle prevApvL1Gry, RTHandle prevApvL1Brz, CommandBuffer cmd)
        {
            var cs = parameters.injection3CS;
            var kernel = parameters.injectionKernelHit;

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, prevApvL0L1rx);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, prevApvL1Gry);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, prevApvL1Brz);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, apvL0L1rx);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, apvL1Gry);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, apvL1Brz);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, debug);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, parameters.probePositionsBuffer);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, parameters.probeFinalExtraDataBuffer);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, parameters.irradianceCacheBuffer);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, parameters.prevIrradianceCacheBuffer);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._CellOrigin, parameters.cellOrigin);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams0, parameters.injectionParameters);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams1, parameters.injectionParameters2);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams2, parameters.injectionParameters3);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams3, parameters.injectionParameters4);

            Vector4[] indicesArray = new Vector4[8];

            for (int i = 0; i < parameters.chunkIndices.Length; ++i)
            {
                indicesArray[i / 4][i % 4] = parameters.chunkIndices[i];
            }

            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._ChunkIndices, indicesArray);

            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeExtraData.NeighbourAxis);

            const int probesPerGroup = 64; // Must match GROUP_SIZE in FirstBounceGeneration.compute
            int groupCount = HDUtils.DivRoundUp((int)parameters.injectionParameters4.x, probesPerGroup);

            cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

            kernel = parameters.injectionKernelMiss;

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL0_L1Rx, prevApvL0L1rx);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1G_L1Ry, prevApvL1Gry);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PrevAPVResL1B_L1Rz, prevApvL1Brz);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, apvL0L1rx);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, apvL1Gry);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, apvL1Brz);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, debug);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._ProbeWorldLocations, parameters.probePositionsBuffer);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PackedProbeExtraData, parameters.probeFinalExtraDataBuffer);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, parameters.irradianceCacheBuffer);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._PrevIrradianceCache, parameters.prevIrradianceCacheBuffer);


            groupCount = HDUtils.DivRoundUp((int)parameters.injectionParameters4.y, probesPerGroup);
            cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);

            if (parameters.clear)
            {
                cmd.SetRenderTarget(apvL0L1rx, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(apvL1Gry, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(apvL1Brz, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(prevApvL0L1rx, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(prevApvL1Gry, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.SetRenderTarget(prevApvL1Brz, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, Color.clear);
            }
        }

        class InjectDirectLightData
        {
            public DirectLightInjectionParameters parameters;
            public TextureHandle apvL0L1rx;
            public TextureHandle apvL1Gry;
            public TextureHandle apvL1Brz;

            public TextureHandle prevApvL0L1rx;
            public TextureHandle prevApvL1Gry;
            public TextureHandle prevApvL1Brz;


            public TextureHandle debug;

            public ComputeBufferHandle positionBuffer;
            public ComputeBufferHandle finalExtraDataBuffer;
        }


        internal void InjectDirectLighting(RenderGraph renderGraph, HDCamera hdCamera, bool forceClear)
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
                if (buffer.finalExtraDataBuffer != null)
                    InjectDirectLighting(renderGraph, buffer, indices, hdCamera);
            }
        }

        void InjectDirectLighting(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            var apvL0L1rxHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L0_L1Rx);
            var apvL1GryHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_G_ry);
            var apvL1BrzHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_B_rz);

            var prevApvL0L1rxHandle = renderGraph.ImportTexture(m_PrevDynamicGI.L0_L1Rx);
            var prevApvL1GryHandle = renderGraph.ImportTexture(m_PrevDynamicGI.L1_G_ry);
            var prevApvL1BrzHandle = renderGraph.ImportTexture(m_PrevDynamicGI.L1_B_rz);
            var prevDBG = renderGraph.ImportTexture(m_PrevDynamicGI.DEBUG);

            var dbg = renderGraph.ImportTexture(m_DynamicGIAPV.DEBUG);

            var positionBufferHandle = renderGraph.ImportComputeBuffer(buffers.probeLocationBuffer);

            var parameters = PrepareInjectionParameters(buffers, chunkIndices, hdCamera);

            using (var builder = renderGraph.AddRenderPass<InjectDirectLightData>("Inject Direct Light in Dynamic GI APV", out var passData, ProfilingSampler.Get(HDProfileId.InjectInDynamicAPV)))
            {
                passData.parameters = parameters;
                passData.apvL0L1rx = apvL0L1rxHandle;
                passData.apvL1Gry = apvL1GryHandle;
                passData.apvL1Brz = apvL1BrzHandle;
                passData.prevApvL0L1rx = prevApvL0L1rxHandle;
                passData.prevApvL1Gry = prevApvL1GryHandle;
                passData.prevApvL1Brz = prevApvL1BrzHandle;

                passData.debug = dbg;
                passData.positionBuffer = positionBufferHandle;
                passData.finalExtraDataBuffer = renderGraph.ImportComputeBuffer(buffers.finalExtraDataBuffer);

                builder.SetRenderFunc(
                    (InjectDirectLightData data, RenderGraphContext ctx) =>
                    {
                        InjectDirectLighting(data.parameters,
                            data.apvL0L1rx,
                            data.apvL1Gry,
                            data.apvL1Brz,
                            data.debug,
                            data.prevApvL0L1rx,
                            data.prevApvL1Gry,
                            data.prevApvL1Brz,
                            ctx.cmd);
                    });
            }
        }

        // ---------------------------------------------------------------------
        // ---------------------------- Combine --------------------------------
        // ---------------------------------------------------------------------

        struct ProbeVolumeCombineParameters
        {
            public ComputeShader combinePVCS;
            public int combineKernel;
            public int combineKernel2;
            public int probeCount;
            public Vector4 combineParameters;
            public Vector4 poolDimensions;
            public int[] chunkIndices;
            public ComputeBuffer irradianceCacheBuffer;
        }

        ProbeVolumeCombineParameters PrepareCombineParameters(ProbeExtraDataBuffers buffers, Vector2 blendFactors, int[] chunkIndices, HDCamera hdCamera)
        {
            ProbeVolumeCombineParameters parameters;
            parameters.combinePVCS = m_Resources.shaders.combineProbeVolumesCS;
            parameters.combineKernel = parameters.combinePVCS.FindKernel("CombineProbeVolumes");
            parameters.combineKernel2 = parameters.combinePVCS.FindKernel("CombineIrradianceCacheAndPV");
            parameters.probeCount = buffers.probeCount;

            var giSettings = hdCamera.volumeStack.GetComponent<ProbeDynamicGI>();
            if (giSettings.clear.value)
            {
                parameters.combineParameters = new Vector4(blendFactors.x, 0, buffers.probeCount, giSettings.antiRingingFactor.value);
            }
            else
            {
                parameters.combineParameters = new Vector4(blendFactors.x, blendFactors.y, buffers.probeCount, giSettings.antiRingingFactor.value);
            }
            parameters.poolDimensions = new Vector4(ProbeReferenceVolume.instance.poolDimension.x, ProbeReferenceVolume.instance.poolDimension.y, ProbeReferenceVolume.instance.poolDimension.z, ProbeReferenceVolume.instance.chunkSizeInProbes);
            parameters.chunkIndices = chunkIndices;

            parameters.irradianceCacheBuffer = buffers.irradianceCache;


            return parameters;
        }

        static void CombineProbeVolumes(in ProbeVolumeCombineParameters parameters,
            RTHandle firstPV0, RTHandle firstPV1, RTHandle firstPV2,
            Texture3D secondPV0, Texture3D secondPV1, Texture3D secondPV2,
            CommandBuffer cmd)
        {
            var cs = parameters.combinePVCS;
            var kernel = parameters.combineKernel2;

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL0_L1Rx, firstPV0);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1G_L1Ry, firstPV1);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._RWAPVResL1B_L1Rz, firstPV2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL0_L1Rx, secondPV0);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1G_L1Ry, secondPV1);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._APVResL1B_L1Rz, secondPV2);

            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._RayAxis, ProbeExtraData.NeighbourAxis);

            Vector4[] indicesArray = new Vector4[8];

            for (int i = 0; i < parameters.chunkIndices.Length; ++i)
            {
                indicesArray[i / 4][i % 4] = parameters.chunkIndices[i];
            }

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._IrradianceCache, parameters.irradianceCacheBuffer);

            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._ChunkIndices, indicesArray);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._PVCombineParameters, parameters.combineParameters);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DynamicGIParams1, parameters.poolDimensions);

            const int groupSize = 64; // Must match GROUP_SIZE in CombineProbeVolumes.compute
            int groupCount = HDUtils.DivRoundUp(parameters.probeCount, groupSize);
            cmd.DispatchCompute(cs, kernel, groupCount, 1, 1);
        }

        class CombineProbeVolumesData
        {
            public ProbeVolumeCombineParameters parameters;
            public TextureHandle firstL0L1rx;
            public TextureHandle firstL1Gry;
            public TextureHandle firstL1Brz;
            public Texture3D secondL0L1rx;
            public Texture3D secondL1Gry;
            public Texture3D secondL1Brz;
        }

        internal void CombineDynamicAndStaticPV(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var chunkIndices = ProbeReferenceVolume.instance.GetChunkIndices();
            var buffersToProcess = ProbeReferenceVolume.instance.GetExtraDataBuffers();

            for (int i = 0; i < chunkIndices.Count; ++i)
            {
                var buffer = buffersToProcess[i];
                var indices = chunkIndices[i];
                CombineDynamicAndStaticPV(renderGraph, buffer, indices, hdCamera);
            }
        }

        internal void CombineDynamicAndStaticPV(RenderGraph renderGraph, ProbeExtraDataBuffers buffers, int[] chunkIndices, HDCamera hdCamera)
        {
            var apvL0L1rxHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L0_L1Rx);
            var apvL1GryHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_G_ry);
            var apvL1BrzHandle = renderGraph.ImportTexture(m_DynamicGIAPV.L1_B_rz);

            var rr = ProbeReferenceVolume.instance.GetRuntimeResources();

            var parameters = PrepareCombineParameters(buffers, new Vector2(1.0f , 1.0f), chunkIndices, hdCamera);

            using (var builder = renderGraph.AddRenderPass<CombineProbeVolumesData>("CombineDynamicGI", out var passData, ProfilingSampler.Get(HDProfileId.CombineDynamicGI)))
            {
                passData.parameters = parameters;
                passData.firstL0L1rx = apvL0L1rxHandle;
                passData.firstL1Gry = apvL1GryHandle;
                passData.firstL1Brz = apvL1BrzHandle;
                passData.secondL0L1rx = rr.L0_L1rx;
                passData.secondL1Gry = rr.L1_G_ry;
                passData.secondL1Brz = rr.L1_B_rz;

                builder.SetRenderFunc(
                    (CombineProbeVolumesData data, RenderGraphContext ctx) =>
                    {
                        CombineProbeVolumes(data.parameters,
                            data.firstL0L1rx,
                            data.firstL1Gry,
                            data.firstL1Brz,
                            data.secondL0L1rx,
                            data.secondL1Gry,
                            data.secondL1Brz,
                            ctx.cmd);
                    });
            }
        }
    }
}
