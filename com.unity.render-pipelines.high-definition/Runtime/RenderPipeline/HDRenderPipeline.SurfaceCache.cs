using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {

        struct SurfaceCacheBufferOutput
        {
            public TextureHandle surfaceCacheAlbedo;
            public TextureHandle surfaceCacheNormal;
            public TextureHandle surfaceCacheDepth;
            public TextureHandle surfaceCacheLit;
            public static SurfaceCacheBufferOutput NewDefault()
            {
                return new SurfaceCacheBufferOutput()
                {
                    surfaceCacheAlbedo = TextureHandle.nullHandle,
                    surfaceCacheNormal = TextureHandle.nullHandle,
                    surfaceCacheDepth = TextureHandle.nullHandle,
                    surfaceCacheLit = TextureHandle.nullHandle,
                };
            }
        }

        class SurfaceCacheData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
            public RenderBRGBindingData BRGBindingData;
        }

        void RenderSurfaceCache(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera,
            CullingResults cull, ref PrepassOutput output)
        {
            output.surfaceCacheBuffer = SurfaceCacheBufferOutput.NewDefault();
            //var BRGBindingData = RenderBRG.GetRenderBRGMaterialBindingData();

            /*if (!BRGBindingData.valid)
            {
                output.surfaceCacheBuffer.surfaceCacheAlbedo = renderGraph.defaultResources.blackUIntTextureXR;
                output.surfaceCacheBuffer.surfaceCacheNormal = renderGraph.defaultResources.blackUIntTextureXR;
                output.surfaceCacheBuffer.surfaceCacheLit = renderGraph.defaultResources.blackUIntTextureXR;
                return;
            }*/
            TextureHandle surfaceCacheAlbedo, surfaceCacheNormal, surfaceCacheDepth;
            using (var builder = renderGraph.AddRenderPass<SurfaceCacheData>("SurfaceCacheMaterial", out var passData,
                       ProfilingSampler.Get(HDProfileId.SurfaceCacheMaterial)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                //output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                surfaceCacheDepth = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    new TextureDesc(4096, 4096)
                    {
                        colorFormat = GraphicsFormat.D16_UNorm,
                        clearBuffer = true,
                        name = "SurfaceCacheDepth",
                    }), DepthAccess.ReadWrite);
                surfaceCacheAlbedo = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(4096, 4096)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
                        clearBuffer = true,
                        name = "SurfaceCacheAlbedo",
                    }), 0);

                surfaceCacheNormal = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(4096, 4096)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_SNorm,
                        clearBuffer = true,
                        name = "SurfaceCacheNormal",
                    }), 1);

                //passData.BRGBindingData = BRGBindingData;
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera,
                        HDShaderPassNames.s_SurfaceCacheName,
                        m_CurrentRendererConfigurationBakedLighting,
                        new RenderQueueRange()
                        {
                            lowerBound = (int)HDRenderQueue.Priority.Opaque,
                            upperBound = (int)HDRenderQueue.Priority.Visibility
                        })));

                builder.SetRenderFunc(
                    (SurfaceCacheData data, RenderGraphContext context) =>
                    {
                        //data.BRGBindingData.globalGeometryPool.BindResourcesGlobal(context.cmd);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }

            output.surfaceCacheBuffer.surfaceCacheAlbedo = surfaceCacheAlbedo;
            output.surfaceCacheBuffer.surfaceCacheNormal = surfaceCacheNormal;
        }

    }
}
