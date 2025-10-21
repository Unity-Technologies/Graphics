using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using CommonResourceData = UnityEngine.Rendering.Universal.UniversalResourceData;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawShadow2DPass : ScriptableRenderPass
    {
        static readonly string k_ShadowPass = "Shadow2D UnsafePass";
        static readonly string k_ShadowVolumetricPass = "Shadow2D Volumetric UnsafePass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ShadowPass);
        private static readonly ProfilingSampler m_ProfilingSamplerVolume = new ProfilingSampler(k_ShadowVolumetricPass);

        private static void ExecuteShadowPass(UnsafeCommandBuffer cmd, PassData passData, Light2D light, int batchIndex)
        {
            cmd.SetRenderTarget(passData.shadowTextures[batchIndex], passData.shadowDepth);

            // Reusing the depth/stencil so we have to clear it
            cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1, 0);

            var projectedShadowMaterial = passData.rendererData.GetProjectedShadowMaterial();
            var projectedUnshadowMaterial = passData.rendererData.GetProjectedUnshadowMaterial();

            ShadowRendering.PrerenderShadows(cmd, passData.rendererData, ref passData.layerBatch, light, 0, light.shadowIntensity);
        }

        internal class PassData
        {
            internal LayerBatch layerBatch;
            internal Renderer2DData rendererData;
            internal TextureHandle[] shadowTextures;
            internal TextureHandle shadowDepth;
        }

        public void Render(RenderGraph graph, ContextContainer frameData, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;
            var layerBatch = frameData.Get<Universal2DRenderingData>().layerBatches[batchIndex];

            if (!layerBatch.lightStats.useShadows ||
                isVolumetric && !layerBatch.lightStats.useVolumetricShadowLights)
                return;

            var passName = !isVolumetric ? k_ShadowPass : k_ShadowVolumetricPass;
            var profilingSampler = !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume;
            LayerDebug.FormatPassName(layerBatch, ref passName);

            using (var builder = graph.AddUnsafePass<PassData>(passName, out var passData, LayerDebug.GetProfilingSampler(passName, profilingSampler)))
            {
                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.shadowTextures = universal2DResourceData.shadowTextures[batchIndex];
                passData.shadowDepth = universal2DResourceData.shadowDepth;

                for (var i = 0; i < passData.shadowTextures.Length; i++)
                    builder.UseTexture(passData.shadowTextures[i], AccessFlags.Write);

                builder.UseTexture(passData.shadowDepth, AccessFlags.Write);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    for (int i = 0; i < data.layerBatch.shadowIndices.Count; ++i)
                    {
                        var cmd = context.cmd;
                        var index = data.layerBatch.shadowIndices[i];
                        var light = data.layerBatch.lights[index];

                        // Shadow Pass
                        ExecuteShadowPass(cmd, data, light, i);
                    }
                });
            }
        }
    }
}
