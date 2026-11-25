#if VOLUMETRIC_FOG

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    internal class FogVolumesPass : ScriptableRenderPass, IDisposable
    {
        static class ShaderIDs
        {
            public static readonly int _FogVolumes = Shader.PropertyToID("_FogVolumes");
            public static readonly int _FogVolumeRunStart = Shader.PropertyToID("_FogVolumeRunStart");
            public static readonly int _FogVolumeDensityTexture = Shader.PropertyToID("_FogVolumeDensityTexture");
            public static readonly int _FogVolumeDensityTextureMode = Shader.PropertyToID("_FogVolumeDensityTextureMode");
        }

        // Must match k_DensityTextureMode* in FogVolume.shader.
        enum DensityTextureMode
        {
            None,
            Color,
            Alpha,
            Red,
        }

        const int k_QuadVertexCount = 6;

        readonly MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();
        Material m_Material;

        class PassData
        {
            public Material material;
            public MaterialPropertyBlock propertyBlock;

            public FogVolumeManager fogVolumeManager;

            public Matrix4x4 coordToViewDirWS;

            public BufferHandle fogVolumeBuffer;
            public TextureHandle vbufferDensity;
        }

        public FogVolumesPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Material = CoreUtils.CreateEngineMaterial(resources?.fogVolumePS);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || !SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
                return;

            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();
            if (!fogData.vbufferDensity.IsValid())
                return;

            var fogVolumeManager = ((UniversalRenderer)frameData.Get<UniversalCameraData>().renderer).fogVolumeManager;

            // Culling completes at execution, so only the total count is known here.
            int maxFogVolumeCount = fogVolumeManager.fogVolumeCount;
            if (maxFogVolumeCount == 0)
                return;

            using (var builder = renderGraph.AddUnsafePass<PassData>("Fog Volumes", out var passData))
            {
                passData.material = m_Material;
                passData.propertyBlock = m_PropertyBlock;
                passData.fogVolumeManager = fogVolumeManager;
                passData.coordToViewDirWS = fogData.vBufferParams.coordToViewDirWS;

                var fogVolumeBufferDesc = new BufferDesc(
                    maxFogVolumeCount,
                    UnsafeUtility.SizeOf<FogVolumeData>(),
                    GraphicsBuffer.Target.Structured)
                {
                    name = "Fog Volume Data"
                };
                passData.fogVolumeBuffer = renderGraph.CreateBuffer(fogVolumeBufferDesc);
                builder.UseBuffer(passData.fogVolumeBuffer, AccessFlags.Write);

                passData.vbufferDensity = fogData.vbufferDensity;
                builder.UseTexture(passData.vbufferDensity, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                {
                    var fogVolumeManager = data.fogVolumeManager;
                    int fogVolumeCount = fogVolumeManager.CompleteCulling();
                    if (fogVolumeCount == 0)
                        return;

                    var fogVolumeData = fogVolumeManager.visibleFogVolumeData;
                    var blendModes = fogVolumeManager.visibleBlendModes;

                    var cmd = context.cmd;
                    GraphicsBuffer fogVolumeBuffer = data.fogVolumeBuffer;

                    cmd.SetBufferData(fogVolumeBuffer, fogVolumeData, 0, 0, fogVolumeCount);
                    cmd.SetRenderTarget(data.vbufferDensity, 0, CubemapFace.Unknown, -1);

                    var properties = data.propertyBlock;
                    properties.SetBuffer(ShaderIDs._FogVolumes, fogVolumeBuffer);
                    properties.SetMatrix(SharedIDs._VBufferCoordToViewDirWS, data.coordToViewDirWS);

                    // Instances blend in order, so consecutive volumes sharing a blend mode and density texture share a draw.
                    int runStart = 0;
                    while (runStart < fogVolumeCount)
                    {
                        var blendMode = blendModes[runStart];
                        Texture densityTexture = null;
                        bool hasTexturedVolume = false;
                        uint maxSliceCount = 0;

                        int runEnd = runStart;
                        for (; runEnd < fogVolumeCount; ++runEnd)
                        {
                            if (blendModes[runEnd] != blendMode)
                                break;

                            if (fogVolumeData[runEnd].densityMode == (uint)FogVolumeDensityMode.Texture)
                            {
                                var fogVolumeTexture = fogVolumeManager.GetVisibleFogVolume(runEnd).densityTexture;
                                if (!hasTexturedVolume)
                                {
                                    densityTexture = fogVolumeTexture;
                                    hasTexturedVolume = true;
                                }
                                else if (fogVolumeTexture != densityTexture)
                                    break;
                            }

                            maxSliceCount = Math.Max(maxSliceCount, fogVolumeData[runEnd].sliceCount);
                        }

                        var densityTextureMode = DensityTextureMode.None;

                        // Ignore density textures that aren't 3D.
                        if (densityTexture != null && densityTexture.dimension == TextureDimension.Tex3D)
                        {
                            var format = densityTexture.graphicsFormat;
                            if (GraphicsFormatUtility.IsAlphaOnlyFormat(format))
                                densityTextureMode = DensityTextureMode.Alpha;
                            else if (GraphicsFormatUtility.GetColorComponentCount(format) == 1)
                                densityTextureMode = DensityTextureMode.Red;
                            else
                                densityTextureMode = DensityTextureMode.Color;

                            properties.SetTexture(ShaderIDs._FogVolumeDensityTexture, densityTexture);
                        }

                        properties.SetInteger(ShaderIDs._FogVolumeDensityTextureMode, (int)densityTextureMode);
                        properties.SetInteger(ShaderIDs._FogVolumeRunStart, runStart);

                        cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            (int)blendMode,
                            MeshTopology.Triangles,
                            k_QuadVertexCount * (int)maxSliceCount,
                            runEnd - runStart,
                            properties);

                        runStart = runEnd;
                    }
                });
            }
        }
    }
}

#endif
