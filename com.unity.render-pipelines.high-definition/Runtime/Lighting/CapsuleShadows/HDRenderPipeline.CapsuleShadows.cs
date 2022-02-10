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
    };

    public partial class HDRenderPipeline
    {
        struct CapsuleShadowDirectionalLight
        {
            public Vector3 direction;
            public float cosTheta;
            public float tanTheta;
            public float range;

            public CapsuleShadowDirectionalLight(Vector3 _direction, float _theta, float _range)
            {
                direction = _direction;
                cosTheta = Mathf.Cos(_theta);
                tanTheta = Mathf.Tan(_theta);
                range = _range;
            }
        }

        internal const int k_MaxDirectShadowCapsulesOnScreen = 1024;
        internal const int k_MaxIndirectShadowCapsulesOnScreen = 1024;

        CapsuleOccluderList m_CapsuleOccluders;
        ComputeBuffer m_CapsuleOccluderDataBuffer;
        CapsuleShadowDirectionalLight m_DirectionalLight;

        internal void InitializeCapsuleShadows()
        {
            m_CapsuleOccluders.bounds = new List<OrientedBBox>();
            m_CapsuleOccluders.occluders = new List<CapsuleOccluderData>();
            m_CapsuleOccluderDataBuffer = new ComputeBuffer(k_MaxDirectShadowCapsulesOnScreen + k_MaxIndirectShadowCapsulesOnScreen, Marshal.SizeOf(typeof(CapsuleOccluderData)));
        }

        internal void CleanupCapsuleShadows()
        {
            CoreUtils.SafeRelease(m_CapsuleOccluderDataBuffer);
            m_CapsuleOccluders.occluders = null;
            m_CapsuleOccluders.bounds = null;
        }

        internal CapsuleOccluderList PrepareVisibleCapsuleOccludersList(HDCamera hdCamera, CommandBuffer cmd, CullingResults cullResults)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            Vector3 originWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                originWS = hdCamera.camera.transform.position;

            // if there is a single light with capsule shadows, build optimised bounds
            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            CapsuleShadowDirectionalLight singleLight = new CapsuleShadowDirectionalLight { };
            bool optimiseBoundsForLight = false;
            float maxRange = 0.0f;
            for (int i = 0; i < cullResults.visibleLights.Length; ++i)
            {
                VisibleLight visibleLight = cullResults.visibleLights[i];
                Light light = visibleLight.light;

                int dataIndex = lightEntities.FindEntityDataIndex(light);
                if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    continue;

                HDAdditionalLightData lightData = lightEntities.hdAdditionalLightData[dataIndex];
                if (lightData.enableCapsuleShadows)
                {
                    maxRange = Mathf.Max(maxRange, lightData.capsuleShadowRange);

                    if (light.type == LightType.Directional && !optimiseBoundsForLight)
                    {
                        // optimise for this light, continue checking through the visible list
                        optimiseBoundsForLight = true;
                        singleLight = new CapsuleShadowDirectionalLight(
                            -visibleLight.GetForward().normalized,
                            Mathf.Max(lightData.angularDiameter, lightData.capsuleShadowMinimumAngle) * Mathf.Deg2Rad * 0.5f,
                            lightData.capsuleShadowRange);
                    }
                    else
                    {
                        // cannot optimise for a single directional light, disable and early out
                        singleLight = new CapsuleShadowDirectionalLight { };
                        optimiseBoundsForLight = false;
                        break;
                    }
                }
            }
            m_DirectionalLight = singleLight;

            m_CapsuleOccluders.Clear();
            m_CapsuleOccluders.directUsesSphereBounds = !optimiseBoundsForLight;
            m_CapsuleOccluders.inLightLoop = capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop;

            bool enableDirectShadows = capsuleShadows.enableDirectShadows.value;
            float indirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
            bool enableIndirectShadows = capsuleShadows.enableIndirectShadows.value && indirectRangeFactor > 0.0f;
            if (enableDirectShadows || enableIndirectShadows)
            {
                using (ListPool<OrientedBBox>.Get(out List<OrientedBBox> indirectBounds))
                using (ListPool<CapsuleOccluderData>.Get(out List<CapsuleOccluderData> indirectOccluders))
                {
                    bool scalePenumbraAlongX = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.capsuleShadowMethod == CapsuleShadowMethod.Ellipsoid;
                    var occluders = CapsuleOccluderManager.instance.occluders;
                    foreach (CapsuleOccluder occluder in occluders)
                    {
                        CapsuleOccluderData data = occluder.GetOccluderData(originWS);

                        if (enableDirectShadows && m_CapsuleOccluders.directCount < k_MaxDirectShadowCapsulesOnScreen)
                        {
                            OrientedBBox bounds;
                            if (optimiseBoundsForLight)
                            {
                                // align local X with the capsule axis
                                Vector3 localZ = singleLight.direction;
                                Vector3 localY = Vector3.Cross(localZ, data.axisDirWS).normalized;
                                Vector3 localX = Vector3.Cross(localY, localZ);

                                // capsule bounds, extended along light direction
                                Vector3 centerRWS = data.centerRWS;
                                Vector3 halfExtentLS = new Vector3(
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localX)) * data.offset + data.radius,
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localY)) * data.offset + data.radius,
                                    Mathf.Abs(Vector3.Dot(data.axisDirWS, localZ)) * data.offset + data.radius);
                                halfExtentLS.z += 0.5f * singleLight.range;
                                centerRWS -= (0.5f * singleLight.range) * localZ;

                                // expand by max penumbra
                                float penumbraSize = Mathf.Tan(singleLight.tanTheta) * singleLight.range;
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

            m_CapsuleOccluderDataBuffer.SetData(m_CapsuleOccluders.occluders);

            return m_CapsuleOccluders;
        }

        internal void UpdateShaderVariablesGlobalCapsuleShadows(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();

            uint indirectCountAndFlags
                = (uint)m_CapsuleOccluders.indirectCount
                | ((uint)capsuleShadows.indirectShadowMethod.value << (int)CapsuleIndirectShadowFlags.MethodShift);

            switch (capsuleShadows.pipeline.value)
            {
                case CapsuleShadowPipeline.InLightLoop:
                    indirectCountAndFlags |= (uint)CapsuleIndirectShadowFlags.LightLoopBit;
                    break;
                case CapsuleShadowPipeline.PrePassFullResolution:
                    break;
                case CapsuleShadowPipeline.PrePassHalfResolution:
                    indirectCountAndFlags |= (uint)CapsuleIndirectShadowFlags.HalfResBit;
                    break;
            }

            switch (capsuleShadows.indirectShadowMethod.value)
            {
                case CapsuleIndirectShadowMethod.AmbientOcclusion:
                    indirectCountAndFlags |= ((uint)capsuleShadows.ambientOcclusionMethod.value << (int)CapsuleIndirectShadowFlags.ExtraShift);
                    break;
                case CapsuleIndirectShadowMethod.DirectionAtSurface:
                case CapsuleIndirectShadowMethod.DirectionAtCapsule:
                    // no extra flags
                    break;
            }

            cb._CapsuleDirectShadowCount = (uint)m_CapsuleOccluders.directCount;
            cb._CapsuleIndirectShadowCountAndFlags = indirectCountAndFlags;
            cb._CapsuleIndirectRangeFactor = capsuleShadows.indirectRangeFactor.value;
            cb._CapsuleIndirectMinimumVisibility = capsuleShadows.indirectMinVisibility.value;
            cb._CapsuleIndirectDirectionBias = capsuleShadows.indirectDirectionBias.value;
            cb._CapsuleIndirectCosAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * capsuleShadows.indirectAngularDiameter.value);
        }

        internal void BindGlobalCapsuleShadowBuffers(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._CapsuleOccluderDatas, m_CapsuleOccluderDataBuffer);
        }

        class RenderCapsuleShadowsPassData
        {
            public ComputeShader capsuleCS;
            public int kernel;

            public CapsuleShadowDirectionalLight directionalLight;
            public bool isFullResolution;

            public TextureHandle output;
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
            public ComputeBufferHandle capsuleOccluderDatas;
            public Vector2Int firstDepthMipOffset;
        }

        internal TextureHandle RenderCapsuleShadows(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, in HDUtils.PackedMipChainInfo depthMipInfo)
        {
            CapsuleShadowsVolumeComponent capsuleShadows = hdCamera.volumeStack.GetComponent<CapsuleShadowsVolumeComponent>();
            if (capsuleShadows.pipeline.value == CapsuleShadowPipeline.InLightLoop)
                return renderGraph.defaultResources.blackTextureXR;

            using (var builder = renderGraph.AddRenderPass<RenderCapsuleShadowsPassData>("Capsule Shadows", out var passData, ProfilingSampler.Get(HDProfileId.RenderCapsuleShadows)))
            {
                bool isFullResolution = (capsuleShadows.pipeline.value == CapsuleShadowPipeline.PrePassFullResolution);

                passData.capsuleCS = defaultResources.shaders.capsuleShadowsCS;
                passData.kernel = passData.capsuleCS.FindKernel("CapsuleShadowMain");

                passData.directionalLight = m_DirectionalLight;
                passData.isFullResolution = isFullResolution;

                passData.output = builder.WriteTexture(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * (isFullResolution ? 1.0f : 0.5f), dynamicResolution: true, xrReady: true)
                    {
                        colorFormat = GraphicsFormat.R16G16_UNorm,
                        enableRandomWrite = true,
                        name = "Capsule Shadows"
                    }));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.capsuleOccluderDatas = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CapsuleOccluderDataBuffer));
                passData.firstDepthMipOffset = depthMipInfo.mipLevelOffsets[1];

                builder.SetRenderFunc(
                    (RenderCapsuleShadowsPassData data, RenderGraphContext ctx) =>
                    {
                        RTHandle output = data.output;
                        Vector2Int size = output.GetScaledSize(output.rtHandleProperties.currentViewportSize);

                        ShaderVariablesCapsuleShadows cb = new ShaderVariablesCapsuleShadows { };
                        cb._OutputSize = new Vector4(size.x, size.y, 1.0f/size.x, 1.0f/size.y);

                        cb._CapsuleLightDir = passData.directionalLight.direction;
                        cb._CapsuleLightCosTheta = passData.directionalLight.cosTheta;

                        cb._CapsuleLightTanTheta = passData.directionalLight.tanTheta;
                        cb._CapsuleShadowRange = passData.directionalLight.range;

                        cb._FirstDepthMipOffsetX = (uint)data.firstDepthMipOffset.x;
                        cb._FirstDepthMipOffsetY = (uint)data.firstDepthMipOffset.y;
                        cb._CapsulesFullResolution = data.isFullResolution ? 1U : 0U;

                        ConstantBuffer.Push(ctx.cmd, cb, data.capsuleCS, HDShaderIDs._ShaderVariablesCapsuleShadows);
                        ctx.cmd.SetComputeTextureParam(data.capsuleCS, data.kernel, HDShaderIDs._CapsuleShadowTexture, data.output);
                        ctx.cmd.SetComputeTextureParam(data.capsuleCS, data.kernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.capsuleCS, data.kernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeBufferParam(data.capsuleCS, data.kernel, HDShaderIDs._CapsuleOccluderDatas, data.capsuleOccluderDatas);

                        ctx.cmd.DispatchCompute(data.capsuleCS, data.kernel, HDUtils.DivRoundUp(size.x, 8), HDUtils.DivRoundUp(size.y, 8), 1);
                    });

                return passData.output;
            }
        }
    }
}
