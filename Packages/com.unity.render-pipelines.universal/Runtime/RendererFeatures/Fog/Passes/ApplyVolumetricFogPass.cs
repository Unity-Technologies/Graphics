#if VOLUMETRIC_FOG

using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    internal class ApplyVolumetricFogPass : ScriptableRenderPass, IDisposable
    {
        static class ShaderIDs
        {
            public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
            public static readonly int _MaxFogDistance = Shader.PropertyToID("_MaxFogDistance");
            public static readonly int _VBufferLastSliceDist = Shader.PropertyToID("_VBufferLastSliceDist");
            public static readonly int _VolumetricFilteringEnabled = Shader.PropertyToID("_VolumetricFilteringEnabled");
        }

        static readonly MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        Material m_Material;
        Material m_MaterialWithOpacity;

        class ApplyFogPassData
        {
            internal Material material;
            internal TextureHandle cameraColor;
            internal TextureHandle vbuffer;
            internal Vector4 encodingParams;
            internal Vector4 decodingParams;
            internal Vector4 viewportSize;
            internal float vbufferLastSliceDist;
            internal float rcpSliceCount;
            internal Vector4 fogColor;
            internal float maxFogDistance;
            internal bool volumetricFilteringEnabled;
        }

        public ApplyVolumetricFogPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Material = CoreUtils.CreateEngineMaterial(resources.applyVolumetricFogPS);
            m_MaterialWithOpacity = CoreUtils.CreateEngineMaterial(resources.applyVolumetricFogPS);
            m_MaterialWithOpacity.EnableKeyword("_WRITE_FOG_OPACITY");
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_MaterialWithOpacity);
            m_Material = null;
            m_MaterialWithOpacity = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            if (!fogData.vbuffer.IsValid())
                return;

            var cameraColor = resourceData.activeColorTexture;

            bool multipleScatteringEnabled = fogData.multipleScatteringIntensity > 0.0f;
            TextureHandle opacityTexture = TextureHandle.nullHandle;
            if (multipleScatteringEnabled)
            {
                // When the screen-space multiple-scattering post pass is going to run, we need to hand
                // it the per-pixel fog opacity. The cheapest way to produce that is to have the apply
                // shader output it as a second render target while it's already evaluating the fog.
                var opacityDesc = renderGraph.GetTextureDesc(cameraColor);
                opacityDesc.name = "Volumetric Fog Opacity";
                opacityDesc.format = GraphicsFormat.R8_UNorm;
                opacityDesc.msaaSamples = MSAASamples.None;
                opacityDesc.useMipMap = false;
                opacityDesc.autoGenerateMips = false;
                opacityDesc.enableRandomWrite = false;
                opacityTexture = renderGraph.CreateTexture(opacityDesc);
            }

            using (var builder = renderGraph.AddRasterRenderPass<ApplyFogPassData>("Apply Volumetric Fog", out var passData))
            {
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.ReadWrite);
                if (multipleScatteringEnabled)
                    builder.SetRenderAttachment(opacityTexture, 1, AccessFlags.Write);

                // Input textures
                passData.cameraColor = cameraColor;
                passData.vbuffer = fogData.vbuffer;
                builder.UseTexture(fogData.vbuffer, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                // Material and uniforms
                passData.material = multipleScatteringEnabled ? m_MaterialWithOpacity : m_Material;
                passData.encodingParams = fogData.encodingParams;
                passData.decodingParams = fogData.decodingParams;
                passData.viewportSize = fogData.viewportSize;
                {
                    float lastSliceDepth = 1.0f - 0.5f / fogData.vBufferParams.sliceCount;
                    passData.vbufferLastSliceDist =
                        fogData.decodingParams.x * Mathf.Pow(2.0f, lastSliceDepth * fogData.decodingParams.y)
                        + fogData.decodingParams.z;
                }
                passData.rcpSliceCount = 1.0f / fogData.vBufferParams.sliceCount;
                passData.fogColor = fogData.fogColor;
                passData.maxFogDistance = fogData.maxFogDistance;
                passData.volumetricFilteringEnabled = fogData.enableGaussian;

                builder.SetRenderFunc(static (ApplyFogPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;

                    RTHandle colorHdl = data.cameraColor;
                    Vector2 viewportScale = colorHdl.useScaling
                        ? new Vector2(colorHdl.rtHandleProperties.rtHandleScale.x, colorHdl.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;

                    s_PropertyBlock.Clear();
                    s_PropertyBlock.SetVector(ShaderPropertyId.blitScaleBias, new Vector4(viewportScale.x, viewportScale.y, 0, 0));
                    s_PropertyBlock.SetTexture(SharedIDs._VBuffer, data.vbuffer);
                    s_PropertyBlock.SetVector(SharedIDs._DepthEncodingParams, data.encodingParams);
                    s_PropertyBlock.SetVector(SharedIDs._DepthDecodingParams, data.decodingParams);
                    s_PropertyBlock.SetVector(SharedIDs._VBufferSize, data.viewportSize);
                    s_PropertyBlock.SetFloat(SharedIDs._VBufferRcpSliceCount, data.rcpSliceCount);
                    s_PropertyBlock.SetFloat(ShaderIDs._VBufferLastSliceDist, data.vbufferLastSliceDist);
                    s_PropertyBlock.SetVector(ShaderIDs._FogColor, data.fogColor);
                    s_PropertyBlock.SetFloat(ShaderIDs._MaxFogDistance, data.maxFogDistance);
                    s_PropertyBlock.SetFloat(ShaderIDs._VolumetricFilteringEnabled, data.volumetricFilteringEnabled ? 1.0f : 0.0f);

                    cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
                });
            }

            fogData.opticalFogOpacity = opacityTexture;
        }
    }
}

#endif
