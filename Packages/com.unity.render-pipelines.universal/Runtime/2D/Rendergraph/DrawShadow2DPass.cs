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
        private static readonly ProfilingSampler m_ExecuteProfilingSampler = new ProfilingSampler("Draw Shadow");
        private static readonly ProfilingSampler m_ExecuteLightProfilingSampler = new ProfilingSampler("Draw Light");

        TextureHandle[] intermediateTexture = new TextureHandle[1];
        static List<Light2D> intermediateLight = new List<Light2D>(1);

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void ExecuteShadowPass(UnsafeCommandBuffer cmd, DrawLight2DPass.PassData passData, Light2D light)
        {
            using (new ProfilingScope(cmd, m_ExecuteProfilingSampler))
            {
                cmd.SetRenderTarget(passData.shadowMap, passData.shadowDepth);
                cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1, 0);

                var projectedShadowMaterial = passData.rendererData.GetProjectedShadowMaterial();
                var projectedUnshadowMaterial = passData.rendererData.GetProjectedUnshadowMaterial();

                ShadowRendering.PrerenderShadows(cmd, passData.rendererData, ref passData.layerBatch, light, 0, light.shadowIntensity);
            }
        }

        public void Render(RenderGraph graph, ContextContainer frameData, Renderer2DData rendererData, ref LayerBatch layerBatch, int batchIndex, bool isVolumetric = false)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            CommonResourceData commonResourceData = frameData.Get<CommonResourceData>();

            if (!layerBatch.lightStats.useShadows ||
                isVolumetric && !layerBatch.lightStats.useVolumetricShadowLights)
                return;

            var shadowTexture = universal2DResourceData.shadowsTexture;
            var depthTexture = universal2DResourceData.shadowsDepth;

            using (var builder = graph.AddUnsafePass<DrawLight2DPass.PassData>(!isVolumetric ? k_ShadowPass : k_ShadowVolumetricPass, out var passData, !isVolumetric ? m_ProfilingSampler : m_ProfilingSamplerVolume))
            {
                passData.layerBatch = layerBatch;
                passData.rendererData = rendererData;
                passData.isVolumetric = isVolumetric;
                passData.shadowMap = shadowTexture;
                passData.shadowDepth = depthTexture;
                passData.normalMap = layerBatch.lightStats.useNormalMap ? universal2DResourceData.normalsTexture[batchIndex] : TextureHandle.nullHandle;

                if (!isVolumetric)
                {
                    passData.lightTextures = universal2DResourceData.lightTextures[batchIndex];
                    passData.depthTexture = universal2DResourceData.intermediateDepth;
                    builder.UseTexture(passData.depthTexture, AccessFlags.Write);
                }
                else
                {
                    intermediateTexture[0] = commonResourceData.activeColorTexture;
                    passData.lightTextures = intermediateTexture;
                }

                if (passData.lightTexturesRT == null || passData.lightTexturesRT.Length != passData.lightTextures.Length)
                    passData.lightTexturesRT = new RenderTargetIdentifier[passData.lightTextures.Length];

                for (int i = 0; i < passData.lightTextures.Length; ++i)
                    builder.UseTexture(passData.lightTextures[i], AccessFlags.Write);

                if (layerBatch.lightStats.useNormalMap)
                    builder.UseTexture(universal2DResourceData.normalsTexture[batchIndex]);

                builder.UseTexture(shadowTexture, AccessFlags.Write);
                builder.UseTexture(depthTexture, AccessFlags.Write);

                foreach (var light in layerBatch.shadowLights)
                {
                    if (light == null || !light.m_CookieSpriteTextureHandle.IsValid())
                        continue;

                    if (!isVolumetric || (isVolumetric && light.volumetricEnabled))
                        builder.UseTexture(light.m_CookieSpriteTextureHandle);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((DrawLight2DPass.PassData data, UnsafeGraphContext context) =>
                {
                    for (int i = 0; i < data.layerBatch.shadowLights.Count; ++i)
                    {
                        intermediateLight.Clear();
                        intermediateLight.Add(data.layerBatch.shadowLights[i]);

                        var cmd = context.cmd;

                        // Shadow Pass
                        ExecuteShadowPass(cmd, data, intermediateLight[0]);

                        // Set up MRT
                        if (Renderer2D.supportsMRT && !data.isVolumetric)
                        {
                            for (int j = 0; j < data.lightTextures.Length; ++j)
                                data.lightTexturesRT[j] = data.lightTextures[j];

                            cmd.SetRenderTarget(data.lightTexturesRT, data.depthTexture);
                        }
                        else
                            cmd.SetRenderTarget(data.lightTextures[0]);

                        // Light Pass
                        using (new ProfilingScope(cmd, DrawLight2DPass.m_ProfilingSamplerLowLevel))
                        {
                            DrawLight2DPass.ExecuteUnsafe(cmd, data, ref data.layerBatch, intermediateLight, true);
                        }
                    }
                });
            }
        }
    }
}
