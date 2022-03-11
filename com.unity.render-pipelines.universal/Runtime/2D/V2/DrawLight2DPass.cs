using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawLight2DPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly ProfilingSampler m_ProfilingDrawLights = new ProfilingSampler("Draw 2D Lights");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        public static readonly string[] k_WriteBlendStyleKeywords =
        {
            "WRITE_SHAPE_LIGHT_TYPE_0", "WRITE_SHAPE_LIGHT_TYPE_1", "WRITE_SHAPE_LIGHT_TYPE_2", "WRITE_SHAPE_LIGHT_TYPE_3"
        };
        private RTHandle[] gbuffers;
        private LayerBatch layerBatch;
        private RTHandle m_DepthHandle;

        public DrawLight2DPass(Renderer2DData data)
        {
            rendererData = data;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingDrawLights))
            {
                cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / rendererData.hdrEmulationScale);

                for (var blendStyleIndex = 0; blendStyleIndex < 4; blendStyleIndex++)
                {
                    var visibleLights = layerBatch.GetLights(blendStyleIndex);
                    if (visibleLights.Count == 0)
                        continue;

                    cmd.EnableShaderKeyword(k_WriteBlendStyleKeywords[blendStyleIndex]);

                    foreach (var light in visibleLights)
                    {
                        var lightMaterial = rendererData.GetLightMaterial(light, false);
                        RendererLighting.SetGeneralLightShaderGlobals(this, cmd, light);

                        if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled ||
                            light.lightType == Light2D.LightType.Point)
                            RendererLighting.SetPointLightShaderGlobals(this, cmd, light);

                        if (light.lightType == Light2D.LightType.Point)
                        {
                            RendererLighting.DrawPointLight(cmd, light, light.lightMesh, lightMaterial);
                        }
                        else
                        {
                            cmd.DrawMesh(light.lightMesh, light.transform.localToWorldMatrix, lightMaterial);
                        }
                    }

                    cmd.DisableShaderKeyword(k_WriteBlendStyleKeywords[blendStyleIndex]);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(gbuffers, m_DepthHandle);
        }

        public Renderer2DData rendererData { get; }

        public void Setup(LayerBatch layerBatch, RTHandle[] gbuffers, RTHandle depthHandle)
        {
            this.layerBatch = layerBatch;
            this.gbuffers = gbuffers;
            this.m_DepthHandle = depthHandle;
        }
    }
}
