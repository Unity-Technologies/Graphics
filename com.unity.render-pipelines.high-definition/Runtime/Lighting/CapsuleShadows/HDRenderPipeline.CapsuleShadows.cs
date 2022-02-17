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
        public bool inLightLoop;

        public void Clear()
        {
            bounds.Clear();
            occluders.Clear();
            directUsesSphereBounds = false;
            directCount = 0;
            indirectCount = 0;
            inLightLoop = false;
        }
    }

    public partial class HDRenderPipeline
    {
        internal const int k_MaxDirectShadowCapsulesOnScreen = 1024;
        internal const int k_MaxIndirectShadowCapsulesOnScreen = 1024;
        internal const int k_MaxCapsuleShadowCasters = 4;

        CapsuleOccluderList m_CapsuleOccluders;
        ComputeBuffer m_CapsuleOccluderDataBuffer;
        List<CapsuleShadowCaster> m_CapsuleShadowCasters;
        ComputeBuffer m_CapsuleShadowCastersBuffer;

        internal void InitializeCapsuleShadows()
        {
            m_CapsuleOccluders.bounds = new List<OrientedBBox>();
            m_CapsuleOccluders.occluders = new List<CapsuleOccluderData>();
            m_CapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxDirectShadowCapsulesOnScreen + k_MaxIndirectShadowCapsulesOnScreen, Marshal.SizeOf(typeof(CapsuleOccluderData)));
            m_CapsuleShadowCasters = new List<CapsuleShadowCaster>();
            m_CapsuleShadowCastersBuffer = new ComputeBuffer(k_MaxCapsuleShadowCasters, Marshal.SizeOf(typeof(CapsuleShadowCaster)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_CapsuleShadowCastersBuffer);
            m_CapsuleShadowCasters = null;
            CoreUtils.SafeRelease(m_CapsuleOccluderDataBuffer);
            m_CapsuleOccluders.occluders = null;
            m_CapsuleOccluders.bounds = null;
        }

        void BuildCapsuleShadowCasterList(HDCamera hdCamera, CullingResults cullResults, CapsuleShadowsVolumeComponent capsuleShadows)
        {
            if (capsuleShadows.enableIndirectShadows.value && capsuleShadows.indirectRangeFactor.value > 0.0f)
            {
                m_CapsuleShadowCasters.Add(new CapsuleShadowCaster()
                {
                    casterType = (uint)CapsuleShadowCasterType.AmbientOcclusion, // TODO: other indirect types
                    sliceIndex = 0,
                    // TODO: other fields
                });
            }
            if (capsuleShadows.enableDirectShadows.value)
            {                
                Vector3 originWS = Vector3.zero;
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                    originWS = hdCamera.camera.transform.position;

                HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
                for (int i = 0; i < cullResults.visibleLights.Length && m_CapsuleShadowCasters.Count < k_MaxCapsuleShadowCasters; ++i)
                {
                    var visibleLight = cullResults.visibleLights[i];
                    Light light = visibleLight.light;

                    int dataIndex = lightEntities.FindEntityDataIndex(light);
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        continue;

                    HDAdditionalLightData lightData = lightEntities.hdAdditionalLightData[dataIndex];
                    if (!lightData.enableCapsuleShadows || lightData.capsuleShadowRange == 0.0f)
                        continue;

                    switch (light.type)
                    {
                        case LightType.Directional:
                        {
                            float theta = Mathf.Max(lightData.angularDiameter, lightData.capsuleShadowMinimumAngle) * Mathf.Deg2Rad * 0.5f;
                            m_CapsuleShadowCasters.Add(new CapsuleShadowCaster()
                            {
                                casterType = (uint)CapsuleShadowCasterType.Directional,
                                sliceIndex = (uint)m_CapsuleShadowCasters.Count,
                                shadowRange = lightData.capsuleShadowRange,
                                tanTheta = Mathf.Tan(theta),
                                directionWS = -visibleLight.GetForward().normalized,
                                cosTheta = Mathf.Cos(theta),
                            });
                            break;
                        }

                        case LightType.Point:
                        case LightType.Spot:
                        {
                            float minTheta = lightData.capsuleShadowMinimumAngle * Mathf.Deg2Rad * 0.5f;
                            m_CapsuleShadowCasters.Add(new CapsuleShadowCaster()
                            {
                                casterType = (uint)CapsuleShadowCasterType.Point,
                                sliceIndex = (uint)m_CapsuleShadowCasters.Count,
                                shadowRange = Mathf.Min(lightData.capsuleShadowRange, light.range),
                                cosTheta = Mathf.Cos(minTheta),
                                positionRWS = visibleLight.GetPosition() - originWS,
                                radiusWS = lightData.shapeRadius,
                            });
                            break;
                        }

                        default:
                            break;
                    }
                }
            }
        }

        void BuildCapsuleOccluderListForLightLoop(HDCamera hdCamera, CullingResults cullResults, CapsuleShadowsVolumeComponent capsuleShadows)
        {
            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                originWS = hdCamera.camera.transform.position;

            bool enableDirectShadows = false;
            bool enableIndirectShadows = false;
            Vector3 singleCasterDir = Vector3.up;
            float singleCasterRange = 0.0f;
            float singleCasterTanTheta = 0.0f;
            for (int i = 0; i < m_CapsuleShadowCasters.Count; ++i)
            {
                if (m_CapsuleShadowCasters[i].isIndirect)
                {
                    enableIndirectShadows = true;
                }
                else
                {
                    enableDirectShadows = true;
                    if (m_CapsuleShadowCasters[i].isDirectional && singleCasterRange == 0.0f)
                    {
                        singleCasterDir = m_CapsuleShadowCasters[i].directionWS;
                        singleCasterRange = m_CapsuleShadowCasters[i].shadowRange;
                        singleCasterTanTheta  = m_CapsuleShadowCasters[i].tanTheta;
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
            for (int i = 0; i < m_CapsuleShadowCasters.Count; ++i)
                maxRange = Mathf.Max(maxRange, m_CapsuleShadowCasters[i].shadowRange);

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

                if (capsuleShadows.indirectShadowMethod.value == CapsuleIndirectShadowMethod.DirectionAtCapsule)
                {
                    int count = indirectOccluders.Count;
                    var positions = new Vector3[count];
                    var lighting = new SphericalHarmonicsL2[count];

                    for (int i = 0; i < count; ++i)
                        positions[i] = indirectOccluders[i].centerRWS + originWS;

                    LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lighting, null);

                    Vector3 luma = new Vector3(0.2126729f, 0.7151522f, 0.0721750f);
                    const int R = 0, G = 1, B = 2;
                    const int X = 3, Y = 1, Z = 2;
                    Vector3 directionBias = capsuleShadows.indirectDirectionBias.value;
                    for (int i = 0; i < count; ++i)
                    {
                        SphericalHarmonicsL2 probe = lighting[i];
                        Vector3 L1_X = new Vector3(probe[R, X], probe[G, X], probe[B, X]);
                        Vector3 L1_Y = new Vector3(probe[R, Y], probe[G, Y], probe[B, Y]);
                        Vector3 L1_Z = new Vector3(probe[R, Z], probe[G, Z], probe[B, Z]);
                        Vector3 L1_Vec = new Vector3(Vector3.Dot(L1_X, luma), Vector3.Dot(L1_Y, luma), Vector3.Dot(L1_Z, luma));

                        CapsuleOccluderData data = indirectOccluders[i];
                        data.indirectDirWS = (L1_Vec.normalized + directionBias).normalized;
                        indirectOccluders[i] = data;
                    }
                }

                m_CapsuleOccluders.bounds.AddRange(indirectBounds);
                m_CapsuleOccluders.occluders.AddRange(indirectOccluders);
            }
        }

        void BuildCapsuleOccluderListForPrePass(HDCamera hdCamera, CullingResults cullResults, CapsuleShadowsVolumeComponent capsuleShadows)
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

        void BuildCapsuleCasterAndOccluderLists(HDCamera hdCamera, CullingResults cullResults)
        {
            m_CapsuleShadowCasters.Clear();
            m_CapsuleOccluders.Clear();

            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            m_CapsuleOccluders.inLightLoop = (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop);

            BuildCapsuleShadowCasterList(hdCamera, cullResults, capsuleShadows);
            if (m_CapsuleShadowCasters.Count == 0)
                return;

            if (m_CapsuleOccluders.inLightLoop)
                BuildCapsuleOccluderListForLightLoop(hdCamera, cullResults, capsuleShadows);
            else
                BuildCapsuleOccluderListForPrePass(hdCamera, cullResults, capsuleShadows);
        }

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CullingResults cullResults)
        {
            BuildCapsuleCasterAndOccluderLists(hdCamera, cullResults);
            m_CapsuleOccluderDataBuffer.SetData(m_CapsuleOccluders.occluders);
            if (!m_CapsuleOccluders.inLightLoop)
                m_CapsuleShadowCastersBuffer.SetData(m_CapsuleShadowCasters);
            return m_CapsuleOccluders;
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            uint directCountAndFlags
                = (uint)m_CapsuleOccluders.directCount
                | ((uint)capsuleShadows.directShadowMethod.value << (int)CapsuleShadowFlags.MethodShift)
                | (capsuleShadows.fadeDirectSelfShadow.value ? (uint)CapsuleShadowFlags.FadeSelfShadowBit : 0);

            uint indirectCountAndFlags
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
                if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.PrePassHalfResolution)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.HalfResBit;
                if (m_CapsuleOccluders.occluders.Count > 0)
                {
                    for (int i = 0; i < m_CapsuleShadowCasters.Count; ++i)
                    {
                        if (m_CapsuleShadowCasters[i].isIndirect)
                            directCountAndFlags |= (uint)CapsuleShadowFlags.IndirectEnabledBit;
                        else 
                            directCountAndFlags |= (uint)CapsuleShadowFlags.DirectEnabledBit;
                    }
                }

                if (capsuleShadows.useSplitDepthRange.value)
                    directCountAndFlags |= (uint)CapsuleShadowFlags.SplitDepthRangeBit;
            }

            switch (capsuleShadows.indirectShadowMethod.value)
            {
                case CapsuleIndirectShadowMethod.AmbientOcclusion:
                    indirectCountAndFlags |= ((uint)capsuleShadows.ambientOcclusionMethod.value << (int)CapsuleShadowFlags.ExtraShift);
                    break;
                case CapsuleIndirectShadowMethod.DirectionAtSurface:
                case CapsuleIndirectShadowMethod.DirectionAtCapsule:
                    // no extra flags
                    break;
            }

            cb._CapsuleDirectShadowCountAndFlags = directCountAndFlags;
            cb._CapsuleIndirectShadowCountAndFlags = indirectCountAndFlags;
            cb._CapsuleIndirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
            cb._CapsuleIndirectMinimumVisibility = capsuleShadows.indirectMinVisibility.value;
            cb._CapsuleIndirectDirectionBias = capsuleShadows.indirectDirectionBias.value;
            cb._CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * capsuleShadows.indirectAngularDiameter.value);
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluderData, m_CapsuleOccluderDataBuffer);
        }

        struct CapsuleShadowParameters
        {
            public bool isFullResolution;
            public GraphicsFormat textureFormat;
            public Vector2Int upscaledSize;
            public Vector2Int renderSize;
            public CapsuleTileDebugMode tileDebugMode;
        }

        class BuildCapsuleShadowVolumesPassData
        {
            public ComputeShader cs;
            public int kernel;

            public int occluderCount;
            public int casterCount;
            public ComputeBufferHandle occluders;
            public ComputeBufferHandle casters;
            public ComputeBufferHandle volumes;
            public ComputeBufferHandle counters;
        }

        class CapsuleShadowsRenderPassData
        {
            public ComputeShader cs;
            public int kernel;

            public CapsuleTileDebugMode tileDebugMode;
            public TextureHandle tileDebugOutput;

            public ComputeBufferHandle volumes;
            public ComputeBufferHandle counters;
            public Vector2Int upscaledSize;
            public Vector2Int renderSize;
            public TextureHandle renderOutput;
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
            public Vector2Int depthPyramidTextureSize;
            public Vector2Int firstDepthMipOffset;
        }

        class CapsuleShadowsUpscalePassData
        {
            public ComputeShader cs;
            public int kernel;

            public Vector2Int upscaledSize;
            public TextureHandle upscaleOutput;
            public TextureHandle renderOutput;
            public TextureHandle depthPyramid;
        }

        struct CapsuleBuildVolumeOutput
        {
            public ComputeBufferHandle volumes;
            public ComputeBufferHandle counters;
        }

        CapsuleBuildVolumeOutput BuildCapsuleShadowVolumes(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddRenderPass<BuildCapsuleShadowVolumesPassData>("Capsule Shadow Build Volumes", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsBuildVolumes)))
            {
                int occluderCount = m_CapsuleOccluders.occluders.Count;
                int casterCount = m_CapsuleShadowCasters.Count;
                int sizePerVolume = Marshal.SizeOf(typeof(CapsuleOccluderData)) + Marshal.SizeOf(typeof(CapsuleShadowCaster));

                CapsuleBuildVolumeOutput volumeOutput = new CapsuleBuildVolumeOutput()
                {
                    volumes = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(occluderCount * casterCount, sizePerVolume)),
                    counters = renderGraph.CreateComputeBuffer(new ComputeBufferDesc(1, 4)),
                };

                passData.cs = defaultResources.shaders.capsuleShadowsBuildVolumesCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.occluderCount = occluderCount;
                passData.casterCount = casterCount;
                passData.occluders = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CapsuleOccluderDataBuffer));
                passData.casters = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CapsuleShadowCastersBuffer));
                passData.volumes = builder.WriteComputeBuffer(volumeOutput.volumes);
                passData.counters = builder.WriteComputeBuffer(volumeOutput.counters);

                builder.SetRenderFunc(
                    (BuildCapsuleShadowVolumesPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleOccluderData, data.occluders);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCasters, data.casters);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVolumes, data.volumes);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);
                        ctx.cmd.SetComputeIntParam(data.cs, HDShaderIDs._CapsuleOccluderCount, data.occluderCount);
                        ctx.cmd.SetComputeIntParam(data.cs, HDShaderIDs._CapsuleCasterCount, data.casterCount);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, HDUtils.DivRoundUp(data.occluderCount * data.casterCount, 64), 1, 1);
                    });

                return volumeOutput;
            }
        }

        TextureHandle RenderCapsuleShadows(
            RenderGraph renderGraph,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            in CapsuleBuildVolumeOutput volumeOutput,
            ref TextureHandle capsuleTileDebugTexture,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsRenderPassData>("Capsule Shadows Render", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsRender)))
            {
                var renderOutput = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * (parameters.isFullResolution ? 1.0f : 0.5f), dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = parameters.textureFormat,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Render"
                    });

                passData.cs = defaultResources.shaders.capsuleShadowsRenderCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.tileDebugMode = parameters.tileDebugMode;
                if (parameters.tileDebugMode != CapsuleTileDebugMode.None)
                {
                    passData.tileDebugOutput = builder.WriteTexture(renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * (parameters.isFullResolution ? 1.0f : 0.5f)/8.0f, dynamicResolution: true, xrReady: true)
                        {
                            colorFormat = GraphicsFormat.R16_UInt,
                            enableRandomWrite = true,
                            name = "Capsule Tile Debug"
                        }));
                    capsuleTileDebugTexture = passData.tileDebugOutput;
                }

                passData.volumes = builder.ReadComputeBuffer(volumeOutput.volumes);
                passData.counters = builder.ReadComputeBuffer(volumeOutput.counters);
                passData.renderSize = parameters.renderSize;
                passData.upscaledSize = parameters.upscaledSize;
                passData.renderOutput = builder.WriteTexture(renderOutput);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.depthPyramidTextureSize = depthMipInfo.textureSize;
                passData.firstDepthMipOffset = depthMipInfo.mipLevelOffsets[1];

                builder.SetRenderFunc(
                    (CapsuleShadowsRenderPassData data, RenderGraphContext ctx) =>
                    {
                        Func<Vector2Int, Vector4> sizeAndRcp = (size) => { return new Vector4(size.x, size.y, 1.0f/size.x, 1.0f/size.y); };

                        Texture renderTexture = data.renderOutput;
                        Vector2Int renderTextureSize = new Vector2Int(renderTexture.width, renderTexture.height);

                        ShaderVariablesCapsuleShadows cb = new ShaderVariablesCapsuleShadows { };
                        cb._CapsuleUpscaledSize = sizeAndRcp(data.upscaledSize);
                        cb._CapsuleRenderTextureSize = sizeAndRcp(renderTextureSize);
                        cb._DepthPyramidTextureSize = sizeAndRcp(data.depthPyramidTextureSize);
                        cb._FirstDepthMipOffsetX = (uint)data.firstDepthMipOffset.x;
                        cb._FirstDepthMipOffsetY = (uint)data.firstDepthMipOffset.y;
                        cb._CapsuleTileDebugMode = (uint)data.tileDebugMode;
                        ConstantBuffer.Push(ctx.cmd, cb, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsRenderOutput, data.renderOutput);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowVolumes, data.volumes);
                        ctx.cmd.SetComputeBufferParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowCounters, data.counters);

                        bool useTileDebug = (data.tileDebugMode != CapsuleTileDebugMode.None);
                        if (useTileDebug)
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleTileDebug, data.tileDebugOutput);
                        CoreUtils.SetKeyword(data.cs, "CAPSULE_TILE_DEBUG", useTileDebug);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, HDUtils.DivRoundUp(data.renderSize.x, 8), HDUtils.DivRoundUp(data.renderSize.y, 8), 1);
                    });

                return renderOutput;
            }
        }

        TextureHandle UpscaleCapsuleShadows(
            RenderGraph renderGraph,
            TextureHandle renderOutput,
            TextureHandle depthPyramid,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            in CapsuleShadowParameters parameters)
        {
            using (var builder = renderGraph.AddRenderPass<CapsuleShadowsUpscalePassData>("Capsule Shadows Upscale", out var passData, ProfilingSampler.Get(HDProfileId.CapsuleShadowsUpscale)))
            {
                var upscaleOutput = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = parameters.textureFormat,
                        enableRandomWrite = true,
                        name = "Capsule Shadows Upscale"
                    });

                passData.cs = defaultResources.shaders.capsuleShadowsUpscaleCS;
                passData.kernel = passData.cs.FindKernel("Main");

                passData.upscaledSize = parameters.upscaledSize;
                passData.upscaleOutput = builder.WriteTexture(upscaleOutput);
                passData.renderOutput = builder.ReadTexture(renderOutput);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);

                builder.SetRenderFunc(
                    (CapsuleShadowsUpscalePassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesCapsuleShadows>(ctx.cmd, data.cs, HDShaderIDs.ShaderVariablesCapsuleShadows);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsTexture, data.upscaleOutput);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CapsuleShadowsRenderOutput, data.renderOutput);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);

                        ctx.cmd.DispatchCompute(data.cs, data.kernel, HDUtils.DivRoundUp(data.upscaledSize.x, 8), HDUtils.DivRoundUp(data.upscaledSize.y, 8), 1);
                    });

                return upscaleOutput;
            }
        }

        internal TextureHandle RenderCapsuleShadows(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle depthPyramid,
            TextureHandle normalBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo,
            ref TextureHandle capsuleTileDebugTexture)
        {
            TextureHandle result;

            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop || m_CapsuleOccluders.occluders.Count == 0 || m_CapsuleShadowCasters.Count == 0)
            {
                result = renderGraph.defaultResources.blackTextureXR;
            }
            else
            {
                bool isFullResolution = (capsuleShadows.pipeline.value == CapsuleShadowPipeline.PrePassFullResolution);
                Vector2Int upscaledSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
                Vector2Int renderSize;
                if (isFullResolution)
                    renderSize = upscaledSize;
                else
                    renderSize = new Vector2Int(Mathf.RoundToInt(0.5f*upscaledSize.x), Mathf.RoundToInt(0.5f*upscaledSize.y));

                CapsuleShadowParameters parameters = new CapsuleShadowParameters()
                {
                    isFullResolution = isFullResolution,
                    textureFormat = (capsuleShadows.textureFormat == CapsuleShadowTextureFormat.U16) ? GraphicsFormat.R16G16_UNorm : GraphicsFormat.R8G8_UNorm,
                    upscaledSize = upscaledSize,
                    renderSize = renderSize,
                    tileDebugMode = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleTileDebugMode,
                };

                var volumeOutput = BuildCapsuleShadowVolumes(renderGraph);

                result = RenderCapsuleShadows(
                    renderGraph,
                    depthPyramid,
                    normalBuffer,
                    depthMipInfo,
                    volumeOutput,
                    ref capsuleTileDebugTexture,
                    in parameters);

                if (!isFullResolution)
                {
                    result = UpscaleCapsuleShadows(
                        renderGraph,
                        result,
                        depthPyramid,
                        depthMipInfo,
                        in parameters);
                }
            }

            PushFullScreenDebugTexture(m_RenderGraph, result, FullScreenDebugMode.CapsuleShadows);
            return result;
        }
    }
}
