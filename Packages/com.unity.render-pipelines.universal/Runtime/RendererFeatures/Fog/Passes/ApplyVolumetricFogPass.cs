#if VOLUMETRIC_FOG

using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class ApplyVolumetricFogPass : ScriptableRenderPass, IDisposable
    {
        Material m_Material;
        Material m_MaterialWithOpacity;

        class ApplyFogPassData
        {
            internal Material material;
            internal TextureHandle cameraColor;
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

            if (!fogData.volumetricFogEnabled)
            {
                if (!fogData.analyticFogEnabled)
                    return;
            }
            else if (!fogData.vbuffer.IsValid())
            {
                return;
            }

            var cameraColor = resourceData.activeColorTexture;

            bool multipleScatteringEnabled = fogData.volumetricFogEnabled && fogData.multipleScatteringIntensity > 0.0f;
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

                passData.cameraColor = cameraColor;
                if (fogData.volumetricFogEnabled)
                    builder.UseTexture(fogData.vbuffer, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                passData.material = multipleScatteringEnabled ? m_MaterialWithOpacity : m_Material;

                builder.SetRenderFunc(static (ApplyFogPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;

                    RTHandle colorHdl = data.cameraColor;
                    Vector2 viewportScale = colorHdl.useScaling
                        ? new Vector2(colorHdl.rtHandleProperties.rtHandleScale.x, colorHdl.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;

                    data.material.SetVector(ShaderPropertyId.blitScaleBias, new Vector4(viewportScale.x, viewportScale.y, 0, 0));

                    cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }

            fogData.opticalFogOpacity = opacityTexture;
        }
    }
}

#endif
