using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class Fsr1UpscalePostProcessPass : ScriptableRenderPass, IDisposable
    {
        Material m_Material;
        bool m_IsValid;

        // Input
        public TextureHandle sourceTexture { get; set; }
        // Output
        public TextureHandle destinationTexture { get; set; }

        public Fsr1UpscalePostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit FSR Upscaling");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        private class PostProcessingFinalFSRScalePassData
        {
            internal TextureHandle sourceTexture;
            internal Material material;
            internal Vector2 fsrInputSize;
            internal Vector2 fsrOutputSize;
            internal bool enableAlphaOutput;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for Fsr1UpscalePostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for Fsr1UpscalePostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var srcDesc = renderGraph.GetTextureDesc(sourceTexture);
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
                return;
            }
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
