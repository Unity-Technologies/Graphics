using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class StopNanPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorStopNaNs";

        PostProcessMaterial m_Material;

        public StopNanPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = URPProfilingSamplers.StopNaNs;

            m_Material = new PostProcessMaterial(shader, passName);
        }

        public override void Dispose()
        {
            m_Material.Dispose();
        }

        private class StopNaNsPassData
        {
            internal TextureHandle sourceTexture;
            internal Material stopNaN;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.isStopNaNEnabled)
                return;

            if (!m_Material.Validate())
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, sourceTexture, k_TargetName, true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<StopNaNsPassData>(passName, out var passData,
                       profilingSampler))
            {
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.ReadWrite);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                passData.stopNaN = m_Material.material;
                builder.SetRenderFunc(static (StopNaNsPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    Vector2 viewportScale = sourceTextureHdl.useScaling? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.stopNaN, 0);
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
