using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class Fsr1UpscalePostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorUpscaled";

        Material m_Material;
        bool m_IsValid;

        TextureDesc m_UpscaledDesc;

        public Fsr1UpscalePostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit FSR Upscaling");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        private class PostProcessingFinalFSRScalePassData
        {
            internal Material material;
            internal TextureHandle sourceTexture;
            internal Vector2 fsrInputSize;
            internal Vector2 fsrOutputSize;
            internal bool enableAlphaOutput;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Setup(TextureDesc upscaledDesc)
        {
            m_UpscaledDesc = upscaledDesc;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var srcDesc = renderGraph.GetTextureDesc(sourceTexture);

            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, m_UpscaledDesc, k_TargetName, true, FilterMode.Point);
            var dstDesc = renderGraph.GetTextureDesc(destinationTexture);

            // FSR upscale
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalFSRScalePassData>(passName, out var passData, profilingSampler))
            {
                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                passData.material = m_Material;
                passData.fsrInputSize = new Vector2(srcDesc.width, srcDesc.height);
                passData.fsrOutputSize = new Vector2(dstDesc.width, dstDesc.height);
                passData.enableAlphaOutput = cameraData.isAlphaOutputEnabled;

                builder.SetRenderFunc(static (PostProcessingFinalFSRScalePassData data, RasterGraphContext context) =>
                {
                    var material = data.material;
                    RTHandle sourceHdl = (RTHandle)data.sourceTexture;

                    // TODO: Fsr uses global state constants.
                    FSRUtils.SetEasuConstants(context.cmd, data.fsrInputSize, data.fsrInputSize, data.fsrOutputSize);

                    material.shaderKeywords = null;
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

                    Vector2 viewportScale = sourceHdl.useScaling ? new Vector2(sourceHdl.rtHandleProperties.rtHandleScale.x, sourceHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(context.cmd, data.sourceTexture, viewportScale, material, 0);
                });
            }

            resourceData.cameraColor = destinationTexture;
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
