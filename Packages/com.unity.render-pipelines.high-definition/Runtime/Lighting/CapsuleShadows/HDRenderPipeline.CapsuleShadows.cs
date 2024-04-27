using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    struct CapsuleShadowReservation : IComparable<CapsuleShadowReservation>
    {
        public int category;        // lower numbers given priority
        public float distance;      // then lower distances within each category
        public int lightIndex;

        public CapsuleShadowReservation(int category, float distance, int lightIndex)
        {
            this.category = category;
            this.distance = distance;
            this.lightIndex = lightIndex;
        }

        public int CompareTo(CapsuleShadowReservation other)
        {
            if (category != other.category)
                return (category < other.category) ? -1 : 1;

            if (distance != other.distance)
                return (distance < other.distance) ? -1 : 1;

            if (lightIndex != other.lightIndex)
                return (lightIndex < other.lightIndex) ? -1 : 1;

            return 0;
        }
    }

    internal class CapsuleShadowAllocator
    {
        internal const int k_MaxCasters = (int)CapsuleShadowConstants.MaxShadowCasterCount;

        internal List<CapsuleShadowReservation> m_Reservations;
        internal List<CapsuleShadowCaster> m_Casters;
        internal bool m_DirectEnabled;

        internal CapsuleShadowAllocator()
        {
            m_Reservations = new List<CapsuleShadowReservation>();
            m_Reservations.Capacity = 1 + k_MaxCasters;
            m_Casters = new List<CapsuleShadowCaster>();
        }

        internal void Reset(bool directEnabled, bool indirectEnabled)
        {
            m_Reservations.Clear();
            m_Casters.Clear();
            m_DirectEnabled = directEnabled;
            if (indirectEnabled)
            {
                m_Reservations.Add(new CapsuleShadowReservation(-1, 0.0f, -1));
                m_Casters.Add(new CapsuleShadowCaster(CapsuleShadowCasterType.Indirect, (uint)RenderingLayerMask.Everything, 0.0f, 1.0f));
            }
        }

        bool DirectShadowsEnabled(HDAdditionalLightData additionalLightData)
        {
            return m_DirectEnabled
                && additionalLightData.enableCapsuleShadows
                && additionalLightData.capsuleShadowRange > 0.0f;
        }

        internal void ReserveCaster(HDLightType lightType, HDAdditionalLightData additionalLightData, float distanceToCamera, int lightIndex)
        {
            if (!DirectShadowsEnabled(additionalLightData))
                return;

            var category = (lightType == HDLightType.Directional) ? 0 : 1; // directional first, then the rest
            var res = new CapsuleShadowReservation(category, distanceToCamera, lightIndex);
            int index = m_Reservations.BinarySearch(res);
            if (index < 0)
            {
                m_Reservations.Insert(~index, res);
                if (m_Reservations.Count > k_MaxCasters)
                    m_Reservations.RemoveAt(m_Reservations.Count - 1);
            }
        }

        internal int AllocateCaster(int lightIndex, Light light, HDAdditionalLightData additionalLightData, HDCamera hdCamera)
        {
            if (!DirectShadowsEnabled(additionalLightData))
                return -1;

            int casterIndex = 0;
            for (;; ++casterIndex)
            {
                if (casterIndex == m_Reservations.Count)
                    return -1;
                if (m_Reservations[casterIndex].lightIndex == lightIndex)
                    break;
            }

            while (casterIndex >= m_Casters.Count)
                m_Casters.Add(new CapsuleShadowCaster());

            float shadowRange = additionalLightData.capsuleShadowRange;
            uint shadowLayers = additionalLightData.GetShadowLayers();
            switch (light.type)
            {
                case LightType.Directional:
                {
                    float cosTheta = Mathf.Cos(Mathf.Max(additionalLightData.angularDiameter, additionalLightData.capsuleShadowMinimumAngle) * Mathf.Deg2Rad * 0.5f);
                    m_Casters[casterIndex] = new CapsuleShadowCaster(CapsuleShadowCasterType.Directional, shadowLayers, shadowRange, cosTheta)
                    {
                        directionWS = -light.transform.forward.normalized,
                    };
                    return casterIndex;
                }

                case LightType.Point:
                {
                    shadowRange = Mathf.Min(light.range, shadowRange);
                    float maxCosTheta = Mathf.Cos(additionalLightData.capsuleShadowMinimumAngle * Mathf.Deg2Rad * 0.5f);

                    Vector3 originWS = Vector3.zero;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        originWS = hdCamera.camera.transform.position;

                    m_Casters[casterIndex] = new CapsuleShadowCaster(CapsuleShadowCasterType.Point, shadowLayers, shadowRange, maxCosTheta)
                    {
                        lightRange = light.range,
                        positionRWS = light.transform.position - originWS,
                        radiusWS = additionalLightData.shapeRadius,
                    };
                    return casterIndex;
                }

                case LightType.Spot:
                {
                    shadowRange = Mathf.Min(light.range, shadowRange);
                    float maxCosTheta = Mathf.Cos(additionalLightData.capsuleShadowMinimumAngle * Mathf.Deg2Rad * 0.5f);

                    Vector3 originWS = Vector3.zero;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        originWS = hdCamera.camera.transform.position;

                    m_Casters[casterIndex] = new CapsuleShadowCaster(CapsuleShadowCasterType.Spot, shadowLayers, shadowRange, maxCosTheta)
                    {
                        lightRange = light.range,
                        directionWS = -light.transform.forward.normalized,
                        spotCosTheta = Mathf.Cos(light.spotAngle * Mathf.Deg2Rad * 0.5f),
                        positionRWS = light.transform.position - originWS,
                        radiusWS = additionalLightData.shapeRadius,
                    };
                    return casterIndex;
                }

                default:
                    break;
            }
            return -1;
        }
    }

    public partial class HDRenderPipeline
    {
        internal const int k_MaxCapsuleOccluders = 1024;
        internal const int k_LUTWidth = 32;
        internal const int k_LUTHeight = 32;
        internal const int k_LUTDepth = 32;

        List<CapsuleShadowOccluder> m_CapsuleOccluders;
        GraphicsBuffer m_CapsuleOccluderDataBuffer;
        CapsuleShadowAllocator m_CapsuleShadowAllocator;
        GraphicsBuffer m_CapsuleShadowCastersBuffer;
        RTHandle m_CapsuleShadowLUT;
        bool m_CapsuleShadowLUTValid;

        internal void InitializeCapsuleShadows()
        {
            m_CapsuleOccluders = new List<CapsuleShadowOccluder>();
            m_CapsuleOccluderDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxCapsuleOccluders, Marshal.SizeOf(typeof(CapsuleShadowOccluder)));
            m_CapsuleShadowAllocator = new CapsuleShadowAllocator();
            m_CapsuleShadowCastersBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, CapsuleShadowAllocator.k_MaxCasters, Marshal.SizeOf(typeof(CapsuleShadowCaster)));
            m_CapsuleShadowLUT = RTHandles.Alloc(
                k_LUTWidth, k_LUTHeight, k_LUTDepth,
                dimension: TextureDimension.Tex3D,
                colorFormat: GraphicsFormat.R16_UNorm,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                enableRandomWrite: true,
                name: "Capsule Shadows LUT");
            m_CapsuleShadowLUTValid = false;
        }

        internal void CleanupCapsuleShadows()
        {
            RTHandles.Release(m_CapsuleShadowLUT);
            CoreUtils.SafeRelease(m_CapsuleShadowCastersBuffer);
            m_CapsuleShadowAllocator = null;
            CoreUtils.SafeRelease(m_CapsuleOccluderDataBuffer);
            m_CapsuleOccluders = null;
        }

        internal int GetMaxCapsuleShadowCasters()
        {
            return CapsuleShadowAllocator.k_MaxCasters - 1;
        }

        internal void ResetCapsuleShadowAllocator(HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            m_CapsuleOccluders.Clear();
            m_CapsuleShadowAllocator.Reset(
                capsuleShadows.enableDirectShadows.value,
                capsuleShadows.enableIndirectShadows.value && capsuleShadows.indirectRangeFactor.value > 0.0f);
        }

        internal void FinishCapsuleShadowAllocator(HDCamera hdCamera)
        {
            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                originWS = hdCamera.camera.transform.position;

            int ignoredCount = 0;

            // write the occluder list
            foreach (CapsuleOccluder occluder in CapsuleOccluderManager.instance.occluders)
            {
                if (m_CapsuleOccluders.Count >= k_MaxCapsuleOccluders)
                {
                    CapsuleOccluderManager.instance.AddIgnoredOccluder(occluder);
                    ignoredCount++;
                }
                else
                {
                    if (occluder != null)
                    {
                        m_CapsuleOccluders.Add(occluder.GetPackedData(originWS));
                    }
                }
            }

            if (ignoredCount > 0)
            {
                Debug.LogWarning($"There are to many CapsuleOccluders in the Scene, {ignoredCount} Capsules will be ignored!");
            }

            // upload occluders and casters
            m_CapsuleOccluderDataBuffer.SetData(m_CapsuleOccluders);
            m_CapsuleShadowCastersBuffer.SetData(m_CapsuleShadowAllocator.m_Casters);
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            cb._CapsuleShadowsGlobalFlags = (uint)GetCapsuleShadowFlags(capsuleShadows);
            cb._CapsuleIndirectMinimumVisibility = capsuleShadows.indirectMinVisibility.value;
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            // only needed for CapsuleShadowPipeline.InLightLoop
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleShadowOccluders, m_CapsuleOccluderDataBuffer);
        }

        Vector4 SizeAndRcp(Vector2Int size)
        {
            return new Vector4(size.x, size.y, 1.0f/size.x, 1.0f/size.y);
        }

        class CapsuleShadowsBuildLUTPassData
        {
            public ComputeShader cs;
            public int kernel;

            public TextureHandle lut;
        }

        TextureHandle CapsuleShadowsBuildLUT(
            RenderGraph renderGraph,
            in CapsuleShadowParameters parameters)
        {
            TextureHandle lut = renderGraph.ImportTexture(m_CapsuleShadowLUT);
            if (m_CapsuleShadowLUTValid && !parameters.rebuildLUT)
                return lut;

            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildLUTPassData>("Capsule Shadows Build LUT", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildLUT)))
            {
                passData.cs = defaultResources.shaders.capsuleShadowsBuildLUTCS;
                passData.kernel = passData.cs.FindKernel("Main");
                passData.lut = builder.WriteTexture(lut);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildLUTPassData data, RenderGraphContext ctx) =>
                    {
                        Vector3Int lutSize = new Vector3Int(k_LUTWidth, k_LUTHeight, k_LUTDepth);
                        Vector4 lutCoordScale = new Vector4(
                            1.0f/(lutSize.x - 1),
                            1.0f/(lutSize.y - 1),
                            1.0f/(lutSize.z - 1),
                            0.0f);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsLUT, lut);
                        ctx.cmd.SetComputeVectorParam(data.cs, HDShaderIDs._CapsuleShadowsLUTCoordScale, lutCoordScale);

                        Vector3Int dispatchSize = HDUtils.DivRoundUp(lutSize, 4);
                        ctx.cmd.DispatchCompute(data.cs, data.kernel, dispatchSize.x, dispatchSize.y, dispatchSize.z);
                    });

                m_CapsuleShadowLUTValid = true;
                return lut;
            }
        }

        struct CapsuleShadowsDepthTilesOutput
        {
            public TextureHandle tileDepthRanges;
            public BufferHandle counters;
        }

        class CapsuleShadowsBuildDepthTilesPassData
        {
            public ComputeShader cs;
            public int kernel;

            public ShaderVariablesCapsuleShadowsBuildTiles cb;
            public Vector2Int renderSizeInTiles;
            public int viewCount;
            public TextureHandle depthPyramid;

            public TextureHandle tileDepthRanges;
            public BufferHandle counters;
        }

        CapsuleShadowsDepthTilesOutput CapsuleShadowsBuildDepthTiles(
            RenderGraph renderGraph,
            TextureHandle depthPyramid,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildDepthTilesPassData>("Capsule Shadows Build Depth Tiles", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildDepthTiles)))
            {
                int occluderCount = parameters.occluderCount;
                int casterCount = parameters.casterCount;

                CapsuleShadowsDepthTilesOutput output = new CapsuleShadowsDepthTilesOutput()
                {
                    tileDepthRanges = renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * parameters.renderScale/8.0f, dynamicResolution: true, xrReady: true)
                        {
                            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
                            enableRandomWrite = true,
                            name = "Capsule Shadows Depth Ranges"
                        }),
                    counters = renderGraph.CreateBuffer(new BufferDesc((int)CapsuleShadowCounterSlot.Count, sizeof(uint), GraphicsBuffer.Target.IndirectArguments)),
                };

                passData.cs = defaultResources.shaders.capsuleShadowsBuildDepthTilesCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.cb = new ShaderVariablesCapsuleShadowsBuildTiles()
                {
                    _CapsuleUpscaledSize = SizeAndRcp(parameters.upscaledSize),

                    _CapsuleCoarseTileSizeInFineTilesX = (uint)parameters.coarseTileSizeInFineTiles.x,
                    _CapsuleCoarseTileSizeInFineTilesY = (uint)parameters.coarseTileSizeInFineTiles.y,
                    _CapsuleRenderSizeInCoarseTilesX = (uint)parameters.renderSizeInCoarseTiles.x,
                    _CapsuleRenderSizeInCoarseTilesY = (uint)parameters.renderSizeInCoarseTiles.y,

                    _CapsuleOccluderCount = (uint)occluderCount,
                    _CapsuleCasterCount = (uint)casterCount,
                    _CapsuleShadowFlags = (uint)parameters.flags,
                    _CapsuleShadowsViewCount = (uint)parameters.viewCount,

                    _CapsuleDepthMipOffsetX = (uint)parameters.depthMipOffset.x,
                    _CapsuleDepthMipOffsetY = (uint)parameters.depthMipOffset.y,
                    _CapsuleIndirectRangeFactor = parameters.indirectRangeFactor,
                };
                passData.renderSizeInTiles = parameters.renderSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.counters = builder.WriteBuffer(output.counters);
                passData.tileDepthRanges = builder.WriteTexture(output.tileDepthRanges);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildDepthTilesPassData data, RenderGraphContext ctx) =>
                    {
                        using (ListPool<uint>.Get(out List<uint> counterData))
                        {
                            // CapsuleShadowIndirectIndex.CoarseTileDepthRangeBase
                            for (int i = 0; i < (int)CapsuleShadowConstants.MaxCoarseTileCount; ++i)
                            {
                                counterData.Add(0xffffffffU);
                                counterData.Add(0);
                            }
                            // CapsuleShadowIndirectIndex.CoarseTileShadowCountBase
                            for (int i = 0; i < (int)CapsuleShadowConstants.MaxCoarseTileCount; ++i)
                                counterData.Add(0);
                            // CapsuleShadowIndirectIndex.TileListDispatchArg
                            counterData.Add(0);
                            counterData.Add(1);
                            counterData.Add(1);
                            Debug.Assert(counterData.Count == (int)CapsuleShadowCounterSlot.Count);
                            ctx.cmd.SetBufferData(data.counters, counterData);
                        }

                        ConstantBuffer.Push(ctx.cmd, data.cb, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsBuildTiles);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileDepthRanges, data.tileDepthRanges);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.renderSizeInTiles.x, data.renderSizeInTiles.y, data.viewCount);
                    });
                return output;
            }
        }

        struct CapsuleShadowsOccluderListOutput
        {
            public BufferHandle occluders;
            public BufferHandle casters;
            public BufferHandle volumes;
        }

        class CapsuleShadowsBuildOccluderListPassData
        {
            public ComputeShader cs;
            public int kernel;

            public int maxVolumeCount;
            public int viewCount;
            public BufferHandle occluders;
            public BufferHandle casters;
            public BufferHandle volumes;
            public BufferHandle counters;
        }

        CapsuleShadowsOccluderListOutput CapsuleShadowsBuildOccluderList(
            RenderGraph renderGraph,
            in CapsuleShadowsDepthTilesOutput depthTilesOutput,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildOccluderListPassData>("Capsule Shadows Build Occluder List", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildOccluderList)))
            {
                int maxVolumeCount
                    = parameters.occluderCount
                    * parameters.casterCount
                    * parameters.renderSizeInCoarseTiles.x
                    * parameters.renderSizeInCoarseTiles.y
                    * parameters.viewCount;

                CapsuleShadowsOccluderListOutput output = new CapsuleShadowsOccluderListOutput()
                {
                    occluders = renderGraph.ImportBuffer(m_CapsuleOccluderDataBuffer),
                    casters = renderGraph.ImportBuffer(m_CapsuleShadowCastersBuffer),
                    volumes = renderGraph.CreateBuffer(new BufferDesc(maxVolumeCount, Marshal.SizeOf(typeof(CapsuleShadowVolume)))),
                };

                passData.cs = defaultResources.shaders.capsuleShadowsBuildOccluderListCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.maxVolumeCount = maxVolumeCount;
                passData.viewCount = parameters.viewCount;
                passData.occluders = builder.ReadBuffer(output.occluders);
                passData.casters = builder.ReadBuffer(output.casters);
                passData.volumes = builder.WriteBuffer(output.volumes);
                passData.counters = builder.WriteBuffer(depthTilesOutput.counters);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildOccluderListPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadowsBuildTiles>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsBuildTiles);

                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowOccluders, data.occluders);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCasters, data.casters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVolumes, data.volumes);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, HDUtils.DivRoundUp(data.maxVolumeCount, 64), 1, 1);
                    });

                return output;
            }
        }

        internal struct CapsuleShadowsRenderOutput
        {
            public TextureHandle visibility;
            public TextureHandle tileBits;
        }

        class CapsuleShadowsRenderPassData
        {
            public ComputeShader cs;
            public int kernel;

            public ShaderVariablesCapsuleShadowsRender cb;
            public Vector2Int renderSizeInTiles;
            public int viewCount;
            public TextureHandle lut;
            public TextureHandle tileDebugOutput;
            public BufferHandle occluders;
            public BufferHandle casters;
            public BufferHandle volumes;
            public BufferHandle counters;
            public TextureHandle visibility;
            public TextureHandle tileBits;
            public TextureHandle normalBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle tileDepthRanges;
        }

        CapsuleShadowsRenderOutput CapsuleShadowsRender(
            RenderGraph renderGraph,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            TextureHandle lut,
            in CapsuleShadowsDepthTilesOutput depthTilesOutput,
            in CapsuleShadowsOccluderListOutput occluderListOutput,
            in CapsuleShadowsTileDebugOutput debugOutput,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsRenderPassData>("Capsule Shadows Render", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsRender)))
            {
                CapsuleShadowsRenderOutput renderOutput = new CapsuleShadowsRenderOutput()
                {
                    visibility = renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * parameters.renderScale, dynamicResolution: true)
                        {
                            dimension = TextureDimension.Tex2DArray,
                            colorFormat = GraphicsFormat.R16_UNorm,
                            slices = parameters.sliceCount * parameters.viewCount,
                            enableRandomWrite = true,
                            name = "Capsule Shadows Render"
                        }),
                    tileBits = renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * parameters.renderScale/8.0f, dynamicResolution: true, xrReady: true)
                        {
                            colorFormat = GraphicsFormat.R8_UInt,
                            enableRandomWrite = true,
                            name = "Capsule Shadows Tile Bits"
                        }),
                };

                Vector3Int lutSize = new Vector3Int(k_LUTWidth, k_LUTHeight, k_LUTDepth);
                Vector3 lutCoordScale = new Vector3(
                    (float)(lutSize.x - 1)/(float)lutSize.x,
                    (float)(lutSize.y - 1)/(float)lutSize.y,
                    (float)(lutSize.z - 1)/(float)lutSize.z);
                Vector3 lutCoordOffset = new Vector3(
                    0.5f/lutSize.x,
                    0.5f/lutSize.y,
                    0.5f/lutSize.z);

                passData.cs = defaultResources.shaders.capsuleShadowsRenderCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.cb = new ShaderVariablesCapsuleShadowsRender()
                {
                    _CapsuleUpscaledSize = SizeAndRcp(parameters.upscaledSize),

                    _CapsuleOccluderCount = (uint)parameters.occluderCount,
                    _CapsuleCasterCount = (uint)parameters.casterCount,
                    _CapsuleShadowFlags = (uint)parameters.flags,
                    _CapsuleIndirectRangeFactor = parameters.indirectRangeFactor,

                    _CapsuleUpscaledSizeInTilesX = (uint)parameters.upscaledSizeInTiles.x,
                    _CapsuleUpscaledSizeInTilesY = (uint)parameters.upscaledSizeInTiles.y,
                    _CapsuleCoarseTileSizeInFineTilesX = (uint)parameters.coarseTileSizeInFineTiles.x,
                    _CapsuleCoarseTileSizeInFineTilesY = (uint)parameters.coarseTileSizeInFineTiles.y,

                    _CapsuleRenderSizeInTilesX = (uint)parameters.renderSizeInTiles.x,
                    _CapsuleRenderSizeInTilesY = (uint)parameters.renderSizeInTiles.y,
                    _CapsuleRenderSizeInCoarseTilesX = (uint)parameters.renderSizeInCoarseTiles.x,
                    _CapsuleRenderSizeInCoarseTilesY = (uint)parameters.renderSizeInCoarseTiles.y,

                    _CapsuleRenderSizeX = (uint)parameters.renderSize.x,
                    _CapsuleRenderSizeY = (uint)parameters.renderSize.y,
                    _CapsuleTileDebugMode = (uint)parameters.tileDebugMode,
                    _CapsuleDebugCasterIndex = m_CurrentDebugDisplaySettings.data.capsuleShadowCasterIndex,

                    _CapsuleDepthMipOffsetX = (uint)parameters.depthMipOffset.x,
                    _CapsuleDepthMipOffsetY = (uint)parameters.depthMipOffset.y,
                    _CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * 30.0f),

                    _CapsuleShadowsLUTCoordScale = lutCoordScale,
                    _CapsuleShadowsLUTCoordOffset = lutCoordOffset,
                };
                passData.renderSizeInTiles = parameters.renderSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.lut = builder.ReadTexture(lut);
                if (parameters.tileDebugMode != CapsuleTileDebugMode.None)
                    passData.tileDebugOutput = builder.WriteTexture(debugOutput.textureHandle);
                passData.occluders = builder.ReadBuffer(occluderListOutput.occluders);
                passData.casters = builder.ReadBuffer(occluderListOutput.casters);
                passData.volumes = builder.ReadBuffer(occluderListOutput.volumes);
                passData.counters = builder.ReadBuffer(depthTilesOutput.counters);
                passData.visibility = builder.WriteTexture(renderOutput.visibility);
                passData.tileBits = builder.WriteTexture(renderOutput.tileBits);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.tileDepthRanges = builder.ReadTexture(depthTilesOutput.tileDepthRanges);

                builder.SetRenderFunc(
                    (CapsuleShadowsRenderPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Push(ctx.cmd, data.cb, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsRender);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsLUT, data.lut);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVisibilityOutput, data.visibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileBitsOutput, data.tileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileDepthRanges, data.tileDepthRanges);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowOccluders, data.occluders);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCasters, data.casters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVolumes, data.volumes);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);

                        bool useTileDebug = ((CapsuleTileDebugMode)data.cb._CapsuleTileDebugMode != CapsuleTileDebugMode.None);
                        bool enableRayTracedReference = ((CapsuleShadowFlags)data.cb._CapsuleShadowFlags).HasFlag(CapsuleShadowFlags.ShowRayTracedReference);
                        if (useTileDebug)
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileDebug, data.tileDebugOutput);
                        CoreUtils.SetKeyword(ctx.cmd, data.cs, "ENABLE_CAPSULE_TILE_DEBUG", useTileDebug);
                        CoreUtils.SetKeyword(ctx.cmd, data.cs, "ENABLE_CAPSULE_RAY_TRACED_REFERENCE", enableRayTracedReference);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.renderSizeInTiles.x, data.renderSizeInTiles.y, data.viewCount);
                    });

                return renderOutput;
            }
        }

        class CapsuleShadowsBuildTileListPassData
        {
            public ComputeShader cs;
            public int kernel;

            public Vector2Int renderSizeInTiles;
            public int viewCount;
            public TextureHandle filteredTileBits;
            public TextureHandle renderTileBits;
            public BufferHandle counters;
            public BufferHandle filterTileList;
        }

        class CapsuleShadowsFilterPassData
        {
            public ComputeShader cs;
            public int kernel;

            public TextureHandle filteredVisibility;
            public TextureHandle renderVisibility;
            public TextureHandle renderTileBits;
            public TextureHandle depthPyramid;
            public BufferHandle counters;
            public BufferHandle filterTileList;
        }

        CapsuleShadowsRenderOutput CapsuleShadowsFilter(
            RenderGraph renderGraph,
            TextureHandle depthPyramid,
            in CapsuleShadowsDepthTilesOutput depthTilesOutput,
            in CapsuleShadowsOccluderListOutput occluderListOutput,
            in CapsuleShadowsRenderOutput renderOutput,
            in CapsuleShadowParameters parameters)
        {
            CapsuleShadowsRenderOutput filterOutput = new CapsuleShadowsRenderOutput()
            {
                visibility = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * parameters.renderScale, dynamicResolution: true)
                    {
                        dimension = TextureDimension.Tex2DArray,
                        colorFormat = GraphicsFormat.R16G16_SFloat,
                        slices = (1 + parameters.sliceCount) * parameters.viewCount, // one extra slice for depth moments
                        enableRandomWrite = true,
                        name = "Capsule Shadows Filtered"
                    }),
                tileBits = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * parameters.renderScale/8.0f, dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = GraphicsFormat.R8_UInt,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Tile Bits Filtered"
                    }),
            };

            int maxTileCount = (parameters.renderSizeInTiles.x + 1)*(parameters.renderSizeInTiles.y + 1)*parameters.viewCount;
            int tileListEntrySizeInBytes = 2*sizeof(uint);
            BufferHandle filterTileList = renderGraph.CreateBuffer(new BufferDesc(maxTileCount, tileListEntrySizeInBytes));

            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildTileListPassData>("Capsule Shadows Build Tile List", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildTileList)))
            {
                passData.cs = defaultResources.shaders.capsuleShadowsBuildFilterTileListCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.renderSizeInTiles = parameters.renderSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.filteredTileBits = builder.WriteTexture(filterOutput.tileBits);
                passData.renderTileBits = builder.ReadTexture(renderOutput.tileBits);
                passData.counters = builder.WriteBuffer(depthTilesOutput.counters);
                passData.filterTileList = builder.WriteBuffer(filterTileList);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildTileListPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadowsRender>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsRender);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileBitsOutput, data.filteredTileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileBits, data.renderTileBits);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowFilterTileList, data.filterTileList);

                        Vector2Int sizeInGroups = HDUtils.DivRoundUp(data.renderSizeInTiles, 8);
                        ctx.cmd.DispatchCompute(data.cs, data.kernel, sizeInGroups.x, sizeInGroups.y, data.viewCount);
                    });
             }

            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsFilterPassData>("Capsule Shadows Filter", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsFilter)))
            {
                passData.cs = defaultResources.shaders.capsuleShadowsBlurMomentsCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.filteredVisibility = builder.WriteTexture(filterOutput.visibility);
                passData.renderVisibility = builder.ReadTexture(renderOutput.visibility);
                passData.renderTileBits = builder.ReadTexture(renderOutput.tileBits);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.counters = builder.ReadBuffer(depthTilesOutput.counters);
                passData.filterTileList = builder.ReadBuffer(filterTileList);

                builder.SetRenderFunc(
                    (CapsuleShadowsFilterPassData data, RenderGraphContext ctx) =>
                    {
                        // HACK: set for lightloop to use later
                        Texture renderOutput = data.filteredVisibility;
                        Vector2Int renderOutputSize = new Vector2Int(renderOutput.width, renderOutput.height);
                        if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
                            renderOutputSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(renderOutputSize);
                        ctx.cmd.SetGlobalVector(HDShaderIDs._CapsuleShadowsRenderOutputSize, SizeAndRcp(renderOutputSize));

                        ConstantBuffer.Set<ShaderVariablesCapsuleShadowsRender>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsRender);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVisibilityOutput, data.filteredVisibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVisibility, data.renderVisibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileBits, data.renderTileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowFilterTileList, data.filterTileList);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.counters, ((int)CapsuleShadowCounterSlot.TileListDispatchArg)*sizeof(uint));
                    });
            }

            return filterOutput;
        }

        class CapsuleShadowsDebugCopyPassData
        {
            public ComputeShader cs;
            public int kernel;

            public Vector2Int upscaledSizeInTiles;
            public int viewCount;
            public TextureHandle visibility;
            public TextureHandle tileBits;
            public TextureHandle debugOutput;
        }

        TextureHandle CapsuleShadowsDebugCopy(
            RenderGraph renderGraph,
            in CapsuleShadowsRenderOutput renderOutput,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsDebugCopyPassData>("Capsule Shadows Debug Copy", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsDebugCopy)))
            {
                var debugOutput = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = GraphicsFormat.R16_UNorm,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Debug"
                    });

                passData.cs = defaultResources.shaders.capsuleShadowsDebugCopyCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.upscaledSizeInTiles = parameters.upscaledSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.visibility = builder.ReadTexture(renderOutput.visibility);
                passData.tileBits = builder.ReadTexture(renderOutput.tileBits);
                passData.debugOutput = builder.WriteTexture(debugOutput);

                builder.SetRenderFunc(
                    (CapsuleShadowsDebugCopyPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadowsRender>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadowsRender);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVisibility, data.visibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowTileBits, data.tileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowDebugOutput, data.debugOutput);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.upscaledSizeInTiles.x, data.upscaledSizeInTiles.y, data.viewCount);
                    });

                return debugOutput;
            }
        }

        struct CapsuleShadowParameters
        {
            public bool rebuildLUT;
            public CapsuleShadowFlags flags;
            public float indirectRangeFactor;
            public int occluderCount;
            public int casterCount;
            public int viewCount;
            public int sliceCount;
            public float renderScale;
            public Vector2Int depthMipOffset;
            public Vector2Int upscaledSize;
            public Vector2Int upscaledSizeInTiles;
            public Vector2Int renderSize;
            public Vector2Int renderSizeInTiles;
            public Vector2Int coarseTileSizeInFineTiles;
            public Vector2Int renderSizeInCoarseTiles;
            public CapsuleTileDebugMode tileDebugMode;
        }

        Vector2Int FitGridToImage(Vector2Int size, int maxGridCellCount)
        {
            if (size.x > size.y)
            {
                float yf = Mathf.Sqrt((float)(maxGridCellCount*size.y)/(float)size.x);
                int y = Math.Max(1, Mathf.RoundToInt(yf));
                return new Vector2Int(maxGridCellCount/y, y);
            }
            else
            {
                float xf = Mathf.Sqrt((float)(maxGridCellCount*size.x)/(float)size.y);
                int x = Math.Max(1, Mathf.RoundToInt(xf));
                return new Vector2Int(x, maxGridCellCount/x);
            }
        }

        CapsuleShadowFlags GetCapsuleShadowFlags(CapsuleShadowsVolumeComponent capsuleShadows)
        {
            CapsuleShadowResolution resolution = capsuleShadows.resolution.value;
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsOverrideResolution)
                resolution = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsResolution;

            CapsuleShadowFlags flags = (CapsuleShadowFlags)0;

            if (m_CapsuleOccluders.Count > 0)
            {
                for (int i = 0; i < m_CapsuleShadowAllocator.m_Casters.Count; ++i)
                {
                    if (m_CapsuleShadowAllocator.m_Casters[i].GetCasterType() == CapsuleShadowCasterType.Indirect)
                        flags |= CapsuleShadowFlags.IndirectEnabled;
                    else
                        flags |= CapsuleShadowFlags.DirectEnabled;
                }
            }

            if (capsuleShadows.fadeSelfShadow.value)
                flags |= CapsuleShadowFlags.FadeSelfShadow;
            if (capsuleShadows.fullCapsuleOcclusion.value)
                flags |= CapsuleShadowFlags.FullCapsuleOcclusion;
            if (capsuleShadows.fullCapsuleAmbientOcclusion.value)
                flags |= CapsuleShadowFlags.FullCapsuleAmbientOcclusion;
            if (resolution == CapsuleShadowResolution.Quarter)
                flags |= CapsuleShadowFlags.QuarterResolution;
            if (HDUtils.hdrpSettings.supportLightLayers)
                flags |= CapsuleShadowFlags.LayerMaskEnabled;

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsShowRayTracedReference)
                flags |= CapsuleShadowFlags.ShowRayTracedReference;
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsUseCheckboardDepths)
                flags |= CapsuleShadowFlags.UseCheckerboardDepths;
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsUseCoarseCulling)
                flags |= CapsuleShadowFlags.UseCoarseCulling;
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsUseSplitDepthRange)
                flags |= CapsuleShadowFlags.UseSplitDepthRange;
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsUseSparseTiles)
                flags |= CapsuleShadowFlags.UseSparseTiles;

            return flags;
        }

        internal struct CapsuleShadowsTileDebugOutput
        {
            public TextureHandle textureHandle;
            public Vector2 uvScale;
            public Vector2Int tileSize;
            public int limit;
        }

        void CapsuleShadowsSetupDebugOutput(
            RenderGraph renderGraph,
            ref CapsuleShadowsTileDebugOutput debugOutput,
            in CapsuleShadowParameters parameters)
        {
            if (parameters.tileDebugMode == CapsuleTileDebugMode.None)
                return;

            debugOutput.textureHandle = renderGraph.CreateTexture(
                new TextureDesc(Vector2.one * parameters.renderScale/8.0f, dynamicResolution: true, xrReady: true)
                {
                    colorFormat = GraphicsFormat.R16_UInt,
                    enableRandomWrite = true,
                    name = "Capsule Fine Tile Debug"
                });
            debugOutput.uvScale = (Vector2)parameters.upscaledSize * parameters.renderScale / 8.0f;

            Vector2Int fineTileSize = new Vector2Int(8, 8) * (parameters.flags.HasFlag(CapsuleShadowFlags.QuarterResolution) ? 4 : 2);
            switch (parameters.tileDebugMode)
            {
                case CapsuleTileDebugMode.CoarseCapsules:
                    debugOutput.tileSize = fineTileSize * parameters.coarseTileSizeInFineTiles;
                    break;

                default:
                    debugOutput.tileSize = fineTileSize;
                    break;
            }

            int fineCapsuleLimit = 32;
            int coarseCapsuleLimit = 16*fineCapsuleLimit;
            switch (parameters.tileDebugMode)
            {
                case CapsuleTileDebugMode.DepthRanges:          debugOutput.limit = 2;                      break;
                case CapsuleTileDebugMode.CoarseCapsules:       debugOutput.limit = coarseCapsuleLimit;     break;
                case CapsuleTileDebugMode.FineDirectCapsules:   debugOutput.limit = fineCapsuleLimit;       break;
                case CapsuleTileDebugMode.FineIndirectCapsules: debugOutput.limit = fineCapsuleLimit;       break;
                case CapsuleTileDebugMode.FineActiveLights:     debugOutput.limit = CapsuleShadowAllocator.k_MaxCasters;    break;
                case CapsuleTileDebugMode.FilteredLights:       debugOutput.limit = CapsuleShadowAllocator.k_MaxCasters;    break;
            }
        }

        internal CapsuleShadowsRenderOutput RenderCapsuleShadows(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            ref CapsuleShadowsTileDebugOutput debugOutput)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            CapsuleShadowsRenderOutput renderOutput;
            if (m_CapsuleOccluders.Count == 0 || m_CapsuleShadowAllocator.m_Casters.Count == 0)
            {
                renderOutput.visibility = renderGraph.defaultResources.blackTextureArray;
                renderOutput.tileBits = renderGraph.defaultResources.blackUIntTextureXR;
            }
            else
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.CapsuleShadows)))
                {
                    CapsuleShadowFlags flags = GetCapsuleShadowFlags(capsuleShadows);
                    float renderScale = flags.HasFlag(CapsuleShadowFlags.QuarterResolution) ? 0.25f : 0.5f;

                    Vector2Int upscaledSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
                    Vector2Int renderSize = new Vector2Int(Mathf.RoundToInt(renderScale*upscaledSize.x), Mathf.RoundToInt(renderScale*upscaledSize.y));

                    Vector2Int renderSizeInTiles = HDUtils.DivRoundUp(renderSize, 8);
                    Vector2Int renderSizeInCoarseTiles = FitGridToImage(renderSizeInTiles, (int)CapsuleShadowConstants.MaxCoarseTileCountPerView);
                    Vector2Int coarseTileSizeInFineTiles = HDUtils.DivRoundUp(renderSizeInTiles, renderSizeInCoarseTiles);

                    int occluderCount = m_CapsuleOccluders.Count;
                    int casterCount = m_CapsuleShadowAllocator.m_Casters.Count;
                    int viewCount = hdCamera.viewCount;

                    int depthMipIndex = flags.HasFlag(CapsuleShadowFlags.QuarterResolution) ? 2 : 1;
                    Vector2Int depthMipOffset = flags.HasFlag(CapsuleShadowFlags.UseCheckerboardDepths)
                        ? depthMipInfo.mipLevelOffsetsCheckerboard[depthMipIndex]
                        : depthMipInfo.mipLevelOffsets[depthMipIndex];

                    CapsuleShadowParameters parameters = new CapsuleShadowParameters()
                    {
                        rebuildLUT = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowsRebuildLUT,
                        flags = flags,
                        indirectRangeFactor = capsuleShadows.indirectRangeFactor.value,
                        occluderCount = occluderCount,
                        casterCount = casterCount,
                        viewCount = viewCount,
                        sliceCount = casterCount * viewCount,
                        renderScale = renderScale,
                        depthMipOffset = depthMipOffset,
                        upscaledSize = upscaledSize,
                        upscaledSizeInTiles = HDUtils.DivRoundUp(upscaledSize, 8),
                        renderSize = renderSize,
                        renderSizeInTiles = renderSizeInTiles,
                        coarseTileSizeInFineTiles = coarseTileSizeInFineTiles,
                        renderSizeInCoarseTiles = renderSizeInCoarseTiles,
                        tileDebugMode = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleTileDebugMode,
                    };

                    CapsuleShadowsSetupDebugOutput(renderGraph, ref debugOutput, in parameters);

                    var lut = CapsuleShadowsBuildLUT(renderGraph, in parameters);

                    var depthTilesOutput = CapsuleShadowsBuildDepthTiles(renderGraph, depthPyramid, depthMipInfo, in parameters);

                    var occluderListOutput = CapsuleShadowsBuildOccluderList(renderGraph, depthTilesOutput, in parameters);

                    renderOutput = CapsuleShadowsRender(
                        renderGraph,
                        depthPyramid,
                        normalBuffer,
                        lut,
                        depthTilesOutput,
                        occluderListOutput,
                        in debugOutput,
                        in parameters);

                    renderOutput = CapsuleShadowsFilter(
                        renderGraph,
                        depthPyramid,
                        depthTilesOutput,
                        occluderListOutput,
                        renderOutput,
                        in parameters);

                    if (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.CapsuleShadows)
                    {
                        var debug = CapsuleShadowsDebugCopy(
                            renderGraph,
                            renderOutput,
                            in parameters);

                        PushFullScreenDebugTexture(renderGraph, debug, FullScreenDebugMode.CapsuleShadows);
                    }
                }
            }
            return renderOutput;
        }
    }
}
