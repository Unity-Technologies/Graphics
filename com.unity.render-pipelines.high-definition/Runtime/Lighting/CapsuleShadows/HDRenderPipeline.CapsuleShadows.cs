using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct CapsuleShadowList
    {
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
    }

    internal struct CapsuleOccluderList
    {
        public List<OrientedBBox> bounds;
        public List<CapsuleOccluderData> occluders;
        public bool directUsesSphereBounds;
        public int directCount;
        public int indirectCount;

        public void Clear()
        {
            bounds.Clear();
            occluders.Clear();
            directUsesSphereBounds = false;
            directCount = 0;
            indirectCount = 0;
        }
    }

    internal class CapsuleShadowAllocator
    {
        internal const int k_MaxCasters = CapsuleShadowCaster.maxCapsuleShadowCasterCount;

        internal List<CapsuleShadowCaster> m_Casters;
        internal bool m_DirectEnabled;

        internal CapsuleShadowAllocator()
        {
            m_Casters = new List<CapsuleShadowCaster>();
        }

        internal void Clear(bool directEnabled)
        {
            m_Casters.Clear();
            m_DirectEnabled = directEnabled;
        }

        internal void AllocateIndirect(CapsuleShadowsVolumeComponent capsuleShadows)
        {
            if (capsuleShadows.enableIndirectShadows.value && capsuleShadows.indirectRangeFactor.value > 0.0f)
            {
                // TODO: other fields
                m_Casters.Add(new CapsuleShadowCaster(CapsuleShadowCasterType.Indirect, 0.0f, 1.0f));
            }
        }

        internal int AllocateCaster(Light light, HDAdditionalLightData additionalLightData, HDCamera hdCamera)
        {
            if (!m_DirectEnabled)
                return -1;

            float shadowRange = additionalLightData.capsuleShadowRange;
            if (!additionalLightData.enableCapsuleShadows || shadowRange == 0.0f)
                return -1;

            int casterIndex = m_Casters.Count;
            if (casterIndex >= k_MaxCasters)
                return -1;

            switch (light.type)
            {
                case LightType.Directional:
                {
                    float cosTheta = Mathf.Cos(Mathf.Max(additionalLightData.angularDiameter, additionalLightData.capsuleShadowMinimumAngle) * Mathf.Deg2Rad * 0.5f);
                    m_Casters.Add(new CapsuleShadowCaster(CapsuleShadowCasterType.Directional, shadowRange, cosTheta)
                    {
                        directionWS = -light.transform.forward.normalized,
                    });
                    return casterIndex;
                }

                case LightType.Point:
                {
                    shadowRange = Mathf.Min(light.range, shadowRange);
                    float maxCosTheta = Mathf.Cos(additionalLightData.capsuleShadowMinimumAngle * Mathf.Deg2Rad * 0.5f);

                    Vector3 originWS = Vector3.zero;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        originWS = hdCamera.camera.transform.position;

                    m_Casters.Add(new CapsuleShadowCaster(CapsuleShadowCasterType.Point, shadowRange, maxCosTheta)
                    {
                        lightRange = light.range,
                        positionRWS = light.transform.position - originWS,
                        radiusWS = additionalLightData.shapeRadius,
                    });
                    return casterIndex;
                }

                case LightType.Spot:
                {
                    shadowRange = Mathf.Min(light.range, shadowRange);
                    float maxCosTheta = Mathf.Cos(additionalLightData.capsuleShadowMinimumAngle * Mathf.Deg2Rad * 0.5f);

                    Vector3 originWS = Vector3.zero;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                        originWS = hdCamera.camera.transform.position;

                    m_Casters.Add(new CapsuleShadowCaster(CapsuleShadowCasterType.Spot, shadowRange, maxCosTheta)
                    {
                        lightRange = light.range,
                        directionWS = -light.transform.forward.normalized,
                        spotCosTheta = Mathf.Cos(light.spotAngle * Mathf.Deg2Rad * 0.5f),
                        positionRWS = light.transform.position - originWS,
                        radiusWS = additionalLightData.shapeRadius,
                    });
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
        internal const int k_MaxDirectShadowCapsulesOnScreen = 1024;
        internal const int k_MaxIndirectShadowCapsulesOnScreen = 1024;

        CapsuleOccluderList m_CapsuleOccluders;
        ComputeBuffer m_CapsuleOccluderDataBuffer;
        CapsuleShadowAllocator m_CapsuleShadowAllocator;
        ComputeBuffer m_CapsuleShadowCastersBuffer;

        internal void InitializeCapsuleShadows()
        {
            m_CapsuleOccluders.bounds = new List<OrientedBBox>();
            m_CapsuleOccluders.occluders = new List<CapsuleOccluderData>();
            m_CapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxDirectShadowCapsulesOnScreen + k_MaxIndirectShadowCapsulesOnScreen, Marshal.SizeOf(typeof(CapsuleOccluderData)));
            m_CapsuleShadowAllocator = new CapsuleShadowAllocator();
            m_CapsuleShadowCastersBuffer = new ComputeBuffer(CapsuleShadowAllocator.k_MaxCasters, Marshal.SizeOf(typeof(CapsuleShadowCaster)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_CapsuleShadowCastersBuffer);
            m_CapsuleShadowAllocator = null;
            CoreUtils.SafeRelease(m_CapsuleOccluderDataBuffer);
            m_CapsuleOccluders.occluders = null;
            m_CapsuleOccluders.bounds = null;
        }

        internal int GetMaxCapsuleShadows()
        {
            return CapsuleShadowAllocator.k_MaxCasters - 1;
        }

        void BuildCapsuleCastersListForLightLoop(HDCamera hdCamera, CullingResults cullResults, CapsuleShadowsVolumeComponent capsuleShadows)
        {
            if (capsuleShadows.enableDirectShadows.value)
            {                
                Vector3 originWS = Vector3.zero;
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                    originWS = hdCamera.camera.transform.position;

                HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
                for (int i = 0; i < cullResults.visibleLights.Length; ++i)
                {
                    var visibleLight = cullResults.visibleLights[i];
                    Light light = visibleLight.light;

                    int dataIndex = lightEntities.FindEntityDataIndex(light);
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        continue;

                    HDAdditionalLightData lightData = lightEntities.hdAdditionalLightData[dataIndex];
                    int casterIndex = m_CapsuleShadowAllocator.AllocateCaster(light, lightData, hdCamera);
                    if (casterIndex == -1)
                        break;
                }
            }
        }

        void BuildCapsuleOccluderListForLightLoop(HDCamera hdCamera, CullingResults cullResults, CapsuleShadowsVolumeComponent capsuleShadows)
        {
            if (m_CapsuleShadowAllocator.m_Casters.Count == 0)
                return;

            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                originWS = hdCamera.camera.transform.position;

            bool enableDirectShadows = false;
            bool enableIndirectShadows = false;
            Vector3 singleCasterDir = Vector3.up;
            float singleCasterRange = 0.0f;
            float singleCasterTanTheta = 0.0f;
            for (int i = 0; i < m_CapsuleShadowAllocator.m_Casters.Count; ++i)
            {
                if (m_CapsuleShadowAllocator.m_Casters[i].GetCasterType() == CapsuleShadowCasterType.Indirect)
                {
                    enableIndirectShadows = true;
                }
                else
                {
                    enableDirectShadows = true;
                    if (m_CapsuleShadowAllocator.m_Casters[i].GetCasterType() == CapsuleShadowCasterType.Directional && singleCasterRange == 0.0f)
                    {
                        singleCasterDir = m_CapsuleShadowAllocator.m_Casters[i].directionWS;
                        singleCasterRange = m_CapsuleShadowAllocator.m_Casters[i].shadowRange;

                        float cosTheta  = m_CapsuleShadowAllocator.m_Casters[i].maxCosTheta;
                        float sinTheta = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - cosTheta*cosTheta));
                        singleCasterTanTheta = sinTheta/cosTheta;
                    }
                    else
                    {
                        // not a single directional light, cannot optimise bounds
                        singleCasterRange = -1.0f;
                    }
                }
            }
            bool optimiseBoundsForLight = (singleCasterRange > 0.0f);

            m_CapsuleOccluders.directUsesSphereBounds = !optimiseBoundsForLight;

            float maxRange = 0.0f;
            for (int i = 0; i < m_CapsuleShadowAllocator.m_Casters.Count; ++i)
                maxRange = Mathf.Max(maxRange, m_CapsuleShadowAllocator.m_Casters[i].shadowRange);

            float indirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
            using (ListPool<OrientedBBox>.Get(out List<OrientedBBox> indirectBounds))
            using (ListPool<CapsuleOccluderData>.Get(out List<CapsuleOccluderData> indirectOccluders))
            {
                bool scalePenumbraAlongX = (capsuleShadows.directShadowMethod == CapsuleShadowMethod.Ellipsoid);
                foreach (CapsuleOccluder occluder in CapsuleOccluderManager.instance.occluders)
                {
                    CapsuleOccluderData data = occluder.GetOccluderData(originWS);

                    if (enableDirectShadows && m_CapsuleOccluders.directCount < k_MaxDirectShadowCapsulesOnScreen)
                    {
                        OrientedBBox bounds;
                        if (optimiseBoundsForLight)
                        {
                            // align local X with the capsule axis
                            Vector3 localZ = singleCasterDir;
                            Vector3 localY = Vector3.Cross(localZ, data.axisDirWS).normalized;
                            Vector3 localX = Vector3.Cross(localY, localZ);

                            // capsule bounds, extended along light direction
                            Vector3 centerRWS = data.centerRWS;
                            Vector3 halfExtentLS = new Vector3(
                                Mathf.Abs(Vector3.Dot(data.axisDirWS, localX)) * data.offset + data.radius,
                                Mathf.Abs(Vector3.Dot(data.axisDirWS, localY)) * data.offset + data.radius,
                                Mathf.Abs(Vector3.Dot(data.axisDirWS, localZ)) * data.offset + data.radius);
                            halfExtentLS.z += 0.5f * singleCasterRange;
                            centerRWS -= (0.5f * singleCasterRange) * localZ;

                            // expand by max penumbra
                            float penumbraSize = singleCasterTanTheta * singleCasterRange;
                            halfExtentLS.x += scalePenumbraAlongX ? (penumbraSize*(data.offset + data.radius)/data.radius) : penumbraSize;
                            halfExtentLS.y += penumbraSize;

                            bounds = new OrientedBBox(new Matrix4x4(
                                2.0f * halfExtentLS.x * localX,
                                2.0f * halfExtentLS.y * localY,
                                2.0f * halfExtentLS.z * localZ,
                                centerRWS));
                        }
                        else
                        {
                            // max distance from *surface* of capsule
                            float length = 2.0f * (data.offset + data.radius + maxRange);
                            bounds = new OrientedBBox(
                                Matrix4x4.TRS(data.centerRWS, Quaternion.identity, new Vector3(length, length, length)));
                        }

                        // Frustum cull on the CPU for now.
                        if (GeometryUtils.Overlap(bounds, hdCamera.frustum, 6, 8))
                        {
                            m_CapsuleOccluders.bounds.Add(bounds);
                            m_CapsuleOccluders.occluders.Add(data);
                            m_CapsuleOccluders.directCount += 1;
                        }
                    }

                    if (enableIndirectShadows && m_CapsuleOccluders.indirectCount < k_MaxIndirectShadowCapsulesOnScreen)
                    {
                        float length = 2.0f * (data.offset + data.radius*(1.0f + indirectRangeFactor));
                        OrientedBBox bounds = new OrientedBBox(
                            Matrix4x4.TRS(data.centerRWS, Quaternion.identity, new Vector3(length, length, length)));
                        if (GeometryUtils.Overlap(bounds, hdCamera.frustum, 6, 8))
                        {
                            indirectBounds.Add(bounds);
                            indirectOccluders.Add(data);
                            m_CapsuleOccluders.indirectCount += 1;
                        }    
                    }
                }

                m_CapsuleOccluders.bounds.AddRange(indirectBounds);
                m_CapsuleOccluders.occluders.AddRange(indirectOccluders);
            }
        }

        void BuildCapsuleOccluderListForPrePass(HDCamera hdCamera)
        {
            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                originWS = hdCamera.camera.transform.position;

            foreach (CapsuleOccluder occluder in CapsuleOccluderManager.instance.occluders)
            {
                if (m_CapsuleOccluders.occluders.Count >= k_MaxDirectShadowCapsulesOnScreen)
                    break;
                m_CapsuleOccluders.occluders.Add(occluder.GetOccluderData(originWS));
            }
        }

        void BuildCapsuleIndirectData(HDCamera hdCamera, CapsuleShadowsVolumeComponent capsuleShadows)
        {
            if (capsuleShadows.indirectShadowMethod.value != CapsuleIndirectShadowMethod.DirectionAtCapsule)
            {
                Vector3 originWS = Vector3.zero;
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                    originWS = hdCamera.camera.transform.position;

                int beginIndex = m_CapsuleOccluders.directCount; // only non-zero when pipeline is CapsuleShadowPipeline.InLightLoop
                int endIndex = m_CapsuleOccluders.occluders.Count;

                using (ListPool<Vector3>.Get(out List<Vector3> positions))
                using (ListPool<SphericalHarmonicsL2>.Get(out List<SphericalHarmonicsL2> probes))
                {
                    for (int i = beginIndex; i != endIndex; ++i)
                    {
                        positions.Add(m_CapsuleOccluders.occluders[i].centerRWS + originWS);
                        probes.Add(new SphericalHarmonicsL2());
                    }

                    LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, probes, null);

                    Vector3 luma = new Vector3(0.2126729f, 0.7151522f, 0.0721750f);
                    const int R = 0, G = 1, B = 2;
                    const int X = 3, Y = 1, Z = 2;
                    Vector3 directionBias = capsuleShadows.indirectDirectionBias.value;
                    for (int i = beginIndex; i != endIndex; ++i)
                    {
                        SphericalHarmonicsL2 probe = probes[i - beginIndex];
                        Vector3 L1_X = new Vector3(probe[R, X], probe[G, X], probe[B, X]);
                        Vector3 L1_Y = new Vector3(probe[R, Y], probe[G, Y], probe[B, Y]);
                        Vector3 L1_Z = new Vector3(probe[R, Z], probe[G, Z], probe[B, Z]);
                        Vector3 L1_Vec = new Vector3(Vector3.Dot(L1_X, luma), Vector3.Dot(L1_Y, luma), Vector3.Dot(L1_Z, luma));

                        CapsuleOccluderData data = m_CapsuleOccluders.occluders[i];
                        data.indirectDirWS = (L1_Vec.normalized + directionBias).normalized;
                        m_CapsuleOccluders.occluders[i] = data;
                    }
                }
            }
        }

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CullingResults cullResults)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            m_CapsuleOccluders.Clear();
            m_CapsuleShadowAllocator.Clear(capsuleShadows.enableDirectShadows.value);
            m_CapsuleShadowAllocator.AllocateIndirect(capsuleShadows);

            if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop)
            {
                BuildCapsuleCastersListForLightLoop(hdCamera, cullResults, capsuleShadows);
                BuildCapsuleOccluderListForLightLoop(hdCamera, cullResults, capsuleShadows);
            }

            return m_CapsuleOccluders;
        }

        internal void WriteCapsuleOccluderDataAfterLightsPrepared(HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            if (capsuleShadows.pipeline.value != CapsuleShadowPipeline.InLightLoop)
            {
                // caster list is built as a side-effect calling CapsuleShadowAllocator.AllocateCaster for visible lights
                BuildCapsuleOccluderListForPrePass(hdCamera);
            }

            BuildCapsuleIndirectData(hdCamera, capsuleShadows);

            m_CapsuleOccluderDataBuffer.SetData(m_CapsuleOccluders.occluders);
            m_CapsuleShadowCastersBuffer.SetData(m_CapsuleShadowAllocator.m_Casters);
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            uint directCountAndFlags
                = (uint)m_CapsuleOccluders.directCount
                | ((uint)capsuleShadows.directShadowMethod.value << (int)CapsuleShadowFlags.MethodShift)
                | (capsuleShadows.fadeDirectSelfShadow.value ? (uint)CapsuleShadowFlags.FadeSelfShadowBit : 0);

            uint indirectCountAndExtra
                = (uint)m_CapsuleOccluders.indirectCount
                | ((uint)capsuleShadows.indirectShadowMethod.value << (int)CapsuleShadowFlags.MethodShift);

            if (m_CapsuleOccluders.indirectCount != 0)
                directCountAndFlags |= (uint)CapsuleShadowFlags.IndirectEnabledBit;

            if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop)
            { 
                directCountAndFlags |= (uint)CapsuleShadowFlags.LightLoopBit;
                if (m_CapsuleOccluders.directCount != 0)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.DirectEnabledBit;
                if (m_CapsuleOccluders.indirectCount != 0)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.IndirectEnabledBit;
            }
            else
            {
                if (capsuleShadows.resolution.value == CapsuleShadowResolution.Half)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.HalfResBit;
                if (capsuleShadows.skipEmptyTiles.value)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.NeedsTileCheckBit;
                if (m_CapsuleOccluders.occluders.Count > 0)
                {
                    for (int i = 0; i < m_CapsuleShadowAllocator.m_Casters.Count; ++i)
                    {
                        directCountAndFlags |= (m_CapsuleShadowAllocator.m_Casters[i].GetCasterType() == CapsuleShadowCasterType.Indirect)
                             ? (uint)CapsuleShadowFlags.IndirectEnabledBit
                             : (uint)CapsuleShadowFlags.DirectEnabledBit;
                    }
                }
                if (capsuleShadows.useSplitDepthRange.value)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.SplitDepthRangeBit;
            }

            switch (capsuleShadows.indirectShadowMethod.value)
            {
                case CapsuleIndirectShadowMethod.AmbientOcclusion:
                    indirectCountAndExtra |= ((uint)capsuleShadows.ambientOcclusionMethod.value << (int)CapsuleShadowFlags.ExtraShift);
                    break;
                case CapsuleIndirectShadowMethod.DirectionAtSurface:
                case CapsuleIndirectShadowMethod.DirectionAtCapsule:
                    // no extra flags
                    break;
            }

            cb._CapsuleDirectShadowCountAndFlags = directCountAndFlags;
            cb._CapsuleIndirectShadowCountAndExtra = indirectCountAndExtra;
            cb._CapsuleIndirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
            cb._CapsuleIndirectMinimumVisibility = capsuleShadows.indirectMinVisibility.value;
            cb._CapsuleIndirectDirectionBias = capsuleShadows.indirectDirectionBias.value;
            cb._CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * capsuleShadows.indirectAngularDiameter.value);
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            // only needed for CapsuleShadowPipeline.InLightLoop
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluders, m_CapsuleOccluderDataBuffer);
        }

        struct CapsuleShadowsBuildOutput
        {
            public ComputeBufferHandle shadowCasters;
            public ComputeBufferHandle occluderShadows;
            public ComputeBufferHandle indirectArgs;
        }

        class CapsuleShadowsBuildOccluderListPassData
        {
            public ComputeShader cs;
            public int kernel;

            public int occluderCount;
            public int casterCount;
            public ComputeBufferHandle occluders;
            public ComputeBufferHandle shadowCasters;
            public ComputeBufferHandle occluderShadows;
            public ComputeBufferHandle indirectArgs;
        }

        CapsuleShadowsBuildOutput CapsuleShadowsBuildOccluderList(RenderGraph renderGraph, in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildOccluderListPassData>("Capsule Shadows Build Occluder List", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildOccluderList)))
            {
                int occluderCount = m_CapsuleOccluders.occluders.Count;
                int casterCount = parameters.casterCount;

                CapsuleShadowsBuildOutput buildOutput = new CapsuleShadowsBuildOutput()
                {
                    shadowCasters = renderGraph.ImportComputeBuffer(m_CapsuleShadowCastersBuffer),
                    occluderShadows = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(occluderCount * casterCount, Marshal.SizeOf(typeof(CapsuleOccluderData)))),
                    indirectArgs = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(CapsuleShadowCaster.capsuleShadowIndirectUintCount, sizeof(uint), ComputeBufferType.IndirectArguments)),
                };

                passData.cs = defaultResources.shaders.capsuleShadowsBuildOccluderListCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.occluderCount = occluderCount;
                passData.casterCount = casterCount;
                passData.occluders = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CapsuleOccluderDataBuffer));
                passData.shadowCasters = builder.ReadComputeBuffer(buildOutput.shadowCasters);
                passData.occluderShadows = builder.WriteComputeBuffer(buildOutput.occluderShadows);
                passData.indirectArgs = builder.WriteComputeBuffer(buildOutput.indirectArgs);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildOccluderListPassData data, RenderGraphContext ctx) =>
                    {
                        using (ListPool<uint>.Get(out List<uint> indirectArgsData))
                        {
                            for (int i = 0; i < CapsuleShadowCaster.capsuleShadowIndirectUintCount; ++i)
                                indirectArgsData.Add(0);
                            indirectArgsData[CapsuleShadowCaster.capsuleShadowIndirectIndexTileCount + 1] = 1; // TODO: XR view count
                            indirectArgsData[CapsuleShadowCaster.capsuleShadowIndirectIndexTileCount + 2] = 1;
                            ctx.cmd.SetBufferData(data.indirectArgs, indirectArgsData);
                        }

                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleOccluders, data.occluders);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCasters, data.shadowCasters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleOccluderShadows, data.occluderShadows);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsIndirectArgs, data.indirectArgs);
                        ctx.cmd.SetComputeIntParam(data.cs, HDShaderIDs._CapsuleOccluderCount, data.occluderCount);
                        ctx.cmd.SetComputeIntParam(data.cs, HDShaderIDs._CapsuleCasterCount, data.casterCount);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, HDUtils.DivRoundUp(data.occluderCount * data.casterCount, 64), 1, 1);
                    });

                return buildOutput;
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

            public uint debugCasterIndex;
            public CapsuleTileDebugMode tileDebugMode;
            public TextureHandle tileDebugOutput;

            public int shadowCasterCount;
            public bool skipEmptyTiles;
            public ComputeBufferHandle shadowCasters;
            public ComputeBufferHandle occluderShadows;
            public ComputeBufferHandle shadowCounters;
            public Vector2Int upscaledSize;
            public Vector2Int renderSizeInTiles;
            public Vector2Int upscaledSizeInTiles;
            public int viewCount;
            public TextureHandle visibility;
            public TextureHandle tileBits;
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
            public Vector2Int depthPyramidTextureSize;
            public Vector2Int firstDepthMipOffset;
        }

        CapsuleShadowsRenderOutput CapsuleShadowsRender(
            RenderGraph renderGraph,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            in CapsuleShadowsBuildOutput buildOutput,
            ref TextureHandle capsuleTileDebugTexture,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsRenderPassData>("Capsule Shadows Render", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsRender)))
            {
                CapsuleShadowsRenderOutput renderOutput = new CapsuleShadowsRenderOutput()
                {
                    visibility = renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * (parameters.isHalfResolution ? 0.5f : 1.0f), dynamicResolution: true)
                        {
                            dimension = TextureDimension.Tex2DArray,
                            colorFormat = parameters.textureFormat,
                            slices = parameters.sliceCount,
                            enableRandomWrite = true,
                            name = "Capsule Shadows Render"
                        }),
                    tileBits = renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * (parameters.isHalfResolution ? 0.5f : 1.0f)/8.0f, dynamicResolution: true, xrReady: true)
                        {
                            colorFormat = GraphicsFormat.R8_UInt,
                            enableRandomWrite = true,
                            name = "Capsule Shadows Tile Bits"
                        }),
                };                        

                passData.cs = defaultResources.shaders.capsuleShadowsRenderCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.debugCasterIndex = m_CurrentDebugDisplaySettings.data.capsuleShadowIndex;
                passData.tileDebugMode = parameters.tileDebugMode;
                if (parameters.tileDebugMode != CapsuleTileDebugMode.None)
                {
                    passData.tileDebugOutput = builder.WriteTexture(renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * (parameters.isHalfResolution ? 0.5f : 1.0f)/8.0f, dynamicResolution: true, xrReady: true)
                        {
                            colorFormat = GraphicsFormat.R16_UInt,
                            enableRandomWrite = true,
                            name = "Capsule Tile Debug"
                        }));
                    capsuleTileDebugTexture = passData.tileDebugOutput;
                }

                passData.shadowCasterCount = m_CapsuleShadowAllocator.m_Casters.Count;
                passData.skipEmptyTiles = parameters.skipEmptyTiles;
                passData.shadowCasters = builder.ReadComputeBuffer(buildOutput.shadowCasters);
                passData.occluderShadows = builder.ReadComputeBuffer(buildOutput.occluderShadows);
                passData.shadowCounters = builder.ReadComputeBuffer(buildOutput.indirectArgs);
                passData.upscaledSize = parameters.upscaledSize;
                passData.renderSizeInTiles = parameters.renderSizeInTiles;
                passData.upscaledSizeInTiles = parameters.upscaledSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.visibility = builder.WriteTexture(renderOutput.visibility);
                passData.tileBits = builder.WriteTexture(renderOutput.tileBits);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.depthPyramidTextureSize = depthMipInfo.textureSize;
                passData.firstDepthMipOffset = depthMipInfo.mipLevelOffsets[1];

                builder.SetRenderFunc(
                    (CapsuleShadowsRenderPassData data, RenderGraphContext ctx) =>
                    {
                        Func<Vector2Int, Vector4> sizeAndRcp = (size) => { return new Vector4(size.x, size.y, 1.0f/size.x, 1.0f/size.y); };

                        Texture renderTexture = data.visibility;
                        Vector2Int renderTextureSize = new Vector2Int(renderTexture.width, renderTexture.height);

                        ShaderVariablesCapsuleShadows cb = new ShaderVariablesCapsuleShadows { };
                        cb._CapsuleUpscaledSize = sizeAndRcp(data.upscaledSize);
                        cb._CapsuleRenderTextureSize = sizeAndRcp(renderTextureSize);
                        cb._DepthPyramidTextureSize = sizeAndRcp(data.depthPyramidTextureSize);

                        cb._FirstDepthMipOffsetX = (uint)data.firstDepthMipOffset.x;
                        cb._FirstDepthMipOffsetY = (uint)data.firstDepthMipOffset.y;
                        cb._CapsuleCasterCount = (uint)data.shadowCasterCount;
                        cb._CapsuleShadowsSkipEmptyTiles = data.skipEmptyTiles ? 1U : 0;

                        cb._CapsuleRenderSizeInTilesX = (uint)data.renderSizeInTiles.x;
                        cb._CapsuleRenderSizeInTilesY = (uint)data.renderSizeInTiles.y;
                        cb._CapsuleUpscaledSizeInTilesX = (uint)data.upscaledSizeInTiles.x;
                        cb._CapsuleUpscaledSizeInTilesY = (uint)data.upscaledSizeInTiles.y;

                        cb._CapsuleTileDebugMode = (uint)data.tileDebugMode;
                        cb._CapsuleDebugCasterIndex = data.debugCasterIndex;
                        ConstantBuffer.Push(ctx.cmd, cb, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsVisibilityOutput, data.visibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileBitsOutput, data.tileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCasters, data.shadowCasters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleOccluderShadows, data.occluderShadows);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsIndirectArgs, data.shadowCounters);

                        bool useTileDebug = (data.tileDebugMode != CapsuleTileDebugMode.None);
                        if (useTileDebug)
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleTileDebug, data.tileDebugOutput);
                        CoreUtils.SetKeyword(data.cs, "CAPSULE_TILE_DEBUG", useTileDebug);

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
            public TextureHandle upscaleTileBits;
            public TextureHandle renderTileBits;
            public ComputeBufferHandle indirectArgs;
            public ComputeBufferHandle tileList;
        }

        class CapsuleShadowsUpscalePassData
        {
            public ComputeShader cs;
            public int kernel;

            public TextureHandle upscaleVisibility;
            public TextureHandle renderVisibility;
            public TextureHandle renderTileBits;
            public TextureHandle depthPyramid;
            public ComputeBufferHandle indirectArgs;
            public ComputeBufferHandle tileList;
        }

        CapsuleShadowsRenderOutput CapsuleShadowsUpscale(
            RenderGraph renderGraph,
            in CapsuleShadowsBuildOutput buildOutput,
            in CapsuleShadowsRenderOutput renderOutput,
            TextureHandle depthPyramid,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            in CapsuleShadowParameters parameters)
        {
            CapsuleShadowsRenderOutput upscaleOutput = new CapsuleShadowsRenderOutput()
            {
                visibility = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, dynamicResolution: true)
                    {
                        dimension = TextureDimension.Tex2DArray,
                        colorFormat = parameters.textureFormat,
                        slices = parameters.sliceCount,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Upscale"
                    }),
                tileBits = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one/8.0f, dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = GraphicsFormat.R8_UInt,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Tile Bits Upscale"
                    }),
            };

            int maxTileCount = (parameters.renderSizeInTiles.x + 1)*(parameters.renderSizeInTiles.y + 1)*parameters.viewCount;
            int tileListEntrySizeInBytes = 2*sizeof(uint);
            ComputeBufferHandle tileList = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(maxTileCount, tileListEntrySizeInBytes));

            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsBuildTileListPassData>("Capsule Shadows Build Tile List", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildTileList)))
            {
                passData.cs = defaultResources.shaders.capsuleShadowsBuildTileListCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.renderSizeInTiles = parameters.renderSizeInTiles;
                passData.viewCount = parameters.viewCount;
                passData.upscaleTileBits = builder.WriteTexture(upscaleOutput.tileBits);
                passData.renderTileBits = builder.ReadTexture(renderOutput.tileBits);
                passData.indirectArgs = builder.WriteComputeBuffer(buildOutput.indirectArgs);
                passData.tileList = builder.WriteComputeBuffer(tileList);

                builder.SetRenderFunc(
                    (CapsuleShadowsBuildTileListPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadows>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileBitsOutput, data.upscaleTileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileBits, data.renderTileBits);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsIndirectArgs, data.indirectArgs);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileList, data.tileList);

                        Vector2Int sizeInGroups = HDUtils.DivRoundUp(data.renderSizeInTiles, 8);
                        ctx.cmd.DispatchCompute(data.cs, data.kernel, sizeInGroups.x, sizeInGroups.y, data.viewCount);
                    });
             }

            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsUpscalePassData>("Capsule Shadows Upscale", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsUpscale)))
            {
                passData.cs = defaultResources.shaders.capsuleShadowsUpscaleCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.upscaleVisibility = builder.WriteTexture(upscaleOutput.visibility);
                passData.renderVisibility = builder.ReadTexture(renderOutput.visibility);
                passData.renderTileBits = builder.ReadTexture(renderOutput.tileBits);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.indirectArgs = builder.ReadComputeBuffer(buildOutput.indirectArgs);
                passData.tileList = builder.ReadComputeBuffer(tileList);

                builder.SetRenderFunc(
                    (CapsuleShadowsUpscalePassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadows>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsVisibilityOutput, data.upscaleVisibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsVisibility, data.renderVisibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileBits, data.renderTileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileList, data.tileList);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.indirectArgs, CapsuleShadowCaster.capsuleShadowIndirectIndexTileCount*sizeof(uint));
                    });
            }

            return upscaleOutput;
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
                        colorFormat = parameters.textureFormat,
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
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadows>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsVisibility, data.visibility);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTileBits, data.tileBits);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsDebugOutput, data.debugOutput);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, data.upscaledSizeInTiles.x, data.upscaledSizeInTiles.y, data.viewCount);
                    });

                return debugOutput;
            }
        }

        struct CapsuleShadowParameters
        {
            public bool isHalfResolution;
            public bool skipEmptyTiles;
            public GraphicsFormat textureFormat;
            public int casterCount;
            public int viewCount;
            public int sliceCount;
            public Vector2Int upscaledSize;
            public Vector2Int upscaledSizeInTiles;
            public Vector2Int renderSize;
            public Vector2Int renderSizeInTiles;
            public CapsuleTileDebugMode tileDebugMode;
        }

        internal CapsuleShadowsRenderOutput RenderCapsuleShadows(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            ref TextureHandle capsuleTileDebugTexture)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            CapsuleShadowsRenderOutput renderOutput;
            if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop || m_CapsuleOccluders.occluders.Count == 0 || m_CapsuleShadowAllocator.m_Casters.Count == 0)
            {
                renderOutput.visibility = renderGraph.defaultResources.blackTextureArray;
                renderOutput.tileBits = renderGraph.defaultResources.blackUIntTextureXR;
            }
            else
            {
                bool isHalfResolution = (capsuleShadows.resolution.value == CapsuleShadowResolution.Half);
                bool skipEmptyTiles = capsuleShadows.skipEmptyTiles.value;
                Vector2Int upscaledSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
                Vector2Int renderSize;
                if (isHalfResolution)
                    renderSize = new Vector2Int(Mathf.RoundToInt(0.5f*upscaledSize.x), Mathf.RoundToInt(0.5f*upscaledSize.y));
                else
                    renderSize = upscaledSize;

                int casterCount = m_CapsuleShadowAllocator.m_Casters.Count;
                int viewCount = hdCamera.viewCount;
                CapsuleShadowParameters parameters = new CapsuleShadowParameters()
                {
                    isHalfResolution = isHalfResolution,
                    skipEmptyTiles = skipEmptyTiles,
                    textureFormat = (capsuleShadows.textureFormat == CapsuleShadowTextureFormat.U16) ? GraphicsFormat.R16_UNorm : GraphicsFormat.R8_UNorm,
                    casterCount = casterCount,
                    viewCount = viewCount,
                    sliceCount = casterCount * viewCount,
                    upscaledSize = upscaledSize,
                    upscaledSizeInTiles = HDUtils.DivRoundUp(upscaledSize, 8),
                    renderSize = renderSize,
                    renderSizeInTiles = HDUtils.DivRoundUp(renderSize, 8),
                    tileDebugMode = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleTileDebugMode,
                };

                var buildOutput = CapsuleShadowsBuildOccluderList(renderGraph, in parameters);

                renderOutput = CapsuleShadowsRender(
                    renderGraph,
                    depthPyramid,
                    normalBuffer,
                    depthMipInfo,
                    buildOutput,
                    ref capsuleTileDebugTexture,
                    in parameters);

                if (isHalfResolution)
                {
                    renderOutput = CapsuleShadowsUpscale(
                        renderGraph,
                        buildOutput,
                        renderOutput,
                        depthPyramid,
                        depthMipInfo,
                        in parameters);
                }

                if (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.CapsuleShadows)
                {
                    var debug = CapsuleShadowsDebugCopy(
                        renderGraph,
                        renderOutput,
                        in parameters);

                    PushFullScreenDebugTexture(renderGraph, debug, FullScreenDebugMode.CapsuleShadows);
                }
            }
            return renderOutput;
        }
    }
}
