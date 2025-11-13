using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class StopNanPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorStopNaNs";

        Material m_Material;
        bool m_IsValid;

        public StopNanPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Stop NaNs");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        private class StopNaNsPassData
        {
            internal TextureHandle sourceTexture;
            internal Material stopNaN;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.isStopNaNEnabled)
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
                passData.stopNaN = m_Material;
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
