using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class DeferredLightingRTRPassData
        {
            public DeferredLightingRTParameters parameters;
            public TextureHandle directionBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public Texture skyTexture;
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle distanceBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle litBuffer;
        }

        TextureHandle DeferredLightingRT(RenderGraph renderGraph, in DeferredLightingRTParameters parameters, TextureHandle directionBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, Texture skyTexture, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<DeferredLightingRTRPassData>("Deferred Lighting Ray Tracing", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDeferredLighting)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                // Input Buffers
                passData.directionBuffer = builder.ReadTexture(directionBuffer);
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.skyTexture = skyTexture;

                // Temporary buffers
                passData.gbuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, enableRandomWrite = true, name = "GBuffer0" });
                passData.gbuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer1" });
                passData.gbuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer2" });
                passData.gbuffer3 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = Builtin.GetLightingBufferFormat(), enableRandomWrite = true, name = "GBuffer3" });
                passData.distanceBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" });

                // Output buffers
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                passData.litBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Deferred Lighting Result" }));

                builder.SetRenderFunc(
                (DeferredLightingRTRPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    DeferredLightingRTResources rtrDirGenResources = new DeferredLightingRTResources();
                    rtrDirGenResources.directionBuffer = data.directionBuffer;
                    rtrDirGenResources.depthStencilBuffer = data.depthStencilBuffer;
                    rtrDirGenResources.normalBuffer = data.normalBuffer;
                    rtrDirGenResources.skyTexture = data.skyTexture;
                    rtrDirGenResources.gbuffer0 = data.gbuffer0;
                    rtrDirGenResources.gbuffer1 = data.gbuffer1;
                    rtrDirGenResources.gbuffer2 = data.gbuffer2;
                    rtrDirGenResources.gbuffer3 = data.gbuffer3;
                    rtrDirGenResources.distanceBuffer = data.distanceBuffer;
                    rtrDirGenResources.rayCountTexture = data.rayCountTexture;
                    rtrDirGenResources.litBuffer = data.litBuffer;
                    RenderRaytracingDeferredLighting(ctx.cmd, data.parameters, rtrDirGenResources);
                });

                return passData.litBuffer;
            }
        }
    }
}
