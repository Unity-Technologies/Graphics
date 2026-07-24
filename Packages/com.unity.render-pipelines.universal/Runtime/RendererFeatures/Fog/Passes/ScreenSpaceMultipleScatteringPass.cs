#if VOLUMETRIC_FOG

using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class ScreenSpaceMultipleScatteringPass : ScriptableRenderPass, IDisposable
    {
        static class ShaderIDs
        {
            public static readonly int _ColorPyramidTexture = Shader.PropertyToID("_ColorPyramidTexture");
            public static readonly int _OpticalFogOpacity = Shader.PropertyToID("_OpticalFogOpacity");
            public static readonly int _MultipleScatteringIntensity = Shader.PropertyToID("_MultipleScatteringIntensity");
        }

        static readonly MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        Material m_Material;
        MipGenerator m_MipGenerator;

        class PassData
        {
            public Material material;
            public TextureHandle cameraColor;
            public TextureHandle colorPyramid;
            public TextureHandle opticalFogOpacity;
            public float intensity;
        }

        public ScreenSpaceMultipleScatteringPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            var shader = resources != null ? resources.screenSpaceMultipleScatteringPS : null;
            m_Material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;

            m_MipGenerator = new MipGenerator();
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_MipGenerator?.Release();
            m_MipGenerator = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();
            if (fogData.multipleScatteringIntensity <= 0.0f)
                return;
            if (!fogData.opticalFogOpacity.IsValid())
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var cameraColor = resourceData.activeColorTexture;

            // Mip-mapped pyramid target. MipGenerator copies mip 0, then writes higher mips with
            // separable Gaussian blurs (compute path) or H/V blit passes (PS fallback).
            TextureDesc pyramidDesc = renderGraph.GetTextureDesc(cameraColor);
            pyramidDesc.name = "Volumetric Fog Color Pyramid";
            pyramidDesc.msaaSamples = MSAASamples.None;
            pyramidDesc.useMipMap = true;
            pyramidDesc.autoGenerateMips = false;
            pyramidDesc.enableRandomWrite = SystemInfo.supportsComputeShaders;
            TextureHandle pyramidTexture = renderGraph.CreateTexture(pyramidDesc);

            var viewportSize = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
            m_MipGenerator.RenderColorGaussianPyramid(renderGraph, viewportSize, cameraColor, pyramidTexture);

            bool sourceIsArray = cameraData.cameraTargetDescriptor.dimension == TextureDimension.Tex2DArray;
            CoreUtils.SetKeyword(m_Material, ShaderKeywordStrings.DisableTexture2DXArray, !sourceIsArray);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Volumetric Fog SSMS", out var passData))
            {
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);

                passData.material = m_Material;
                passData.cameraColor = cameraColor;
                passData.colorPyramid = pyramidTexture;
                builder.UseTexture(passData.colorPyramid, AccessFlags.Read);
                passData.opticalFogOpacity = fogData.opticalFogOpacity;
                builder.UseTexture(passData.opticalFogOpacity, AccessFlags.Read);
                passData.intensity = fogData.multipleScatteringIntensity;

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    RTHandle colorHdl = data.cameraColor;
                    Vector2 viewportScale = colorHdl.useScaling
                        ? new Vector2(colorHdl.rtHandleProperties.rtHandleScale.x, colorHdl.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;

                    s_PropertyBlock.Clear();
                    s_PropertyBlock.SetVector(ShaderPropertyId.blitScaleBias, new Vector4(viewportScale.x, viewportScale.y, 0, 0));
                    s_PropertyBlock.SetTexture(ShaderIDs._ColorPyramidTexture, data.colorPyramid);
                    s_PropertyBlock.SetTexture(ShaderIDs._OpticalFogOpacity, data.opticalFogOpacity);
                    s_PropertyBlock.SetFloat(ShaderIDs._MultipleScatteringIntensity, data.intensity);

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
                });
            }
        }
    }
}

#endif
