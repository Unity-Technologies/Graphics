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
            public TextureHandle surfaceCacheEmissive;
            public TextureHandle surfaceCacheLit;
            public TextureHandle surfaceCachePosWS;
            public static SurfaceCacheBufferOutput NewDefault()
            {
                return new SurfaceCacheBufferOutput()
                {
                    surfaceCacheAlbedo = TextureHandle.nullHandle,
                    surfaceCacheNormal = TextureHandle.nullHandle,
                    surfaceCacheDepth = TextureHandle.nullHandle,
                    surfaceCacheEmissive = TextureHandle.nullHandle,
                    surfaceCacheLit = TextureHandle.nullHandle,
                    surfaceCachePosWS = TextureHandle.nullHandle,
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
            TextureHandle surfaceCacheAlbedo, surfaceCacheNormal, surfaceCacheDepth, surfaceCacheLit, surfaceCacheEmissive, surfaceCachePosWS;
            using (var builder = renderGraph.AddRenderPass<SurfaceCacheData>("SurfaceCacheMaterial", out var passData,
                       ProfilingSampler.Get(HDProfileId.SurfaceCacheMaterial)))
            {
                builder.AllowRendererListCulling(false);
                FrameSettings frameSettings = hdCamera.frameSettings;
                passData.frameSettings = frameSettings;
                Matrix4x4 prevCullingMatrix = hdCamera.camera.cullingMatrix;
                hdCamera.camera.cullingMatrix = Matrix4x4.zero;


                //output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                surfaceCacheDepth = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        depthBufferBits = DepthBits.Depth8,
                        clearBuffer = true,
                        name = "SurfaceCacheDepth",
                    }), DepthAccess.ReadWrite);
                surfaceCacheAlbedo = builder.UseColorBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
                        clearBuffer = true,
                        name = "SurfaceCacheAlbedo",
                    }), 0);

                surfaceCacheNormal = builder.UseColorBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        colorFormat = GraphicsFormat.R8G8B8A8_SNorm,
                        clearBuffer = true,
                        name = "SurfaceCacheNormal",
                    }), 1);

                surfaceCacheEmissive = builder.UseColorBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        clearBuffer = true,
                        name = "SurfaceCacheEmissive",
                    }), 2);

                surfaceCachePosWS = builder.UseColorBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        clearBuffer = true,
                        name = "SurfaceCacheEmissive",
                    }), 3);
                surfaceCacheLit = builder.UseColorBuffer(renderGraph.CreateTexture(
                    // new TextureDesc(Vector2.one, true, false)
                    new TextureDesc(4096, 4096, false, false)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        clearBuffer = true,
                        name = "SurfaceCacheLit",
                        enableRandomWrite = true,
                    }), 4);

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

                hdCamera.camera.cullingMatrix = prevCullingMatrix;
            }

            output.surfaceCacheBuffer.surfaceCacheAlbedo = surfaceCacheAlbedo;
            output.surfaceCacheBuffer.surfaceCacheNormal = surfaceCacheNormal;
            output.surfaceCacheBuffer.surfaceCacheEmissive = surfaceCacheEmissive;
            output.surfaceCacheBuffer.surfaceCachePosWS = surfaceCachePosWS;
            output.surfaceCacheBuffer.surfaceCacheLit = surfaceCacheLit;
        }

        LightingOutput RenderDeferredSurfaceCacheLighting(
            RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle depthStencilBuffer,
            TextureHandle depthPyramidTexture,
            in LightingBuffers lightingBuffers,
            in GBufferOutput gbuffer,
            in SurfaceCacheBufferOutput surfaceCacheOutput,
            in ShadowResult shadowResult,
            in BuildGPULightListOutput lightLists)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return new LightingOutput();

            using (var builder = renderGraph.AddRenderPass<DeferredLightingPassData>("Deferred Lighting SurfaceCache", out var passData))
            {
                bool debugDisplayOrSceneLightOff = CoreUtils.IsSceneLightingDisabled(hdCamera.camera) || m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

                int w = 4096; //surfaceCache width and height
                int h = 4096;
                // int w = hdCamera.actualWidth;
                // int h = hdCamera.actualHeight;
                passData.numTilesX = (w + 15) / 16;
                passData.numTilesY = (h + 15) / 16;
                passData.numTiles = passData.numTilesX * passData.numTilesY;
                passData.enableTile = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DeferredTile);
                passData.outputSplitLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering);
                passData.useComputeLightingEvaluation = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation);
                //passData.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings) && !debugDisplayOrSceneLightOff;
                passData.enableFeatureVariants = false;
                passData.enableShadowMasks = m_EnableBakeShadowMask;
                //passData.numVariants = LightDefinitions.s_NumFeatureVariants;
                passData.numVariants = 1;
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;

                // Compute Lighting
                passData.deferredComputeShader = deferredSurfaceCacheComputeShader;
                passData.viewCount = hdCamera.viewCount;

                // Full Screen Pixel (debug)
                passData.splitLightingMat = GetDeferredLightingMaterial(true /*split lighting*/, passData.enableShadowMasks, debugDisplayOrSceneLightOff);
                passData.regularLightingMat = GetDeferredLightingMaterial(false /*split lighting*/, passData.enableShadowMasks, debugDisplayOrSceneLightOff);

                passData.colorBuffer = builder.WriteTexture(surfaceCacheOutput.surfaceCacheLit);
                if (passData.outputSplitLighting)
                {
                    passData.sssDiffuseLightingBuffer = builder.WriteTexture(lightingBuffers.diffuseLightingBuffer);
                }
                else
                {
                    // TODO RENDERGRAPH: Check how to avoid this kind of pattern.
                    // Unfortunately, the low level needs this texture to always be bound with UAV enabled, so in order to avoid effectively creating the full resolution texture here,
                    // we need to create a small dummy texture.
                    passData.sssDiffuseLightingBuffer = builder.CreateTransientTexture(new TextureDesc(1, 1, true, true) { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true });
                }
                passData.depthBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthPyramidTexture);

                passData.lightingBuffers = ReadLightingBuffers(lightingBuffers, builder);

                passData.lightLayersTextureIndex = gbuffer.lightLayersTextureIndex;
                passData.shadowMaskTextureIndex = gbuffer.shadowMaskTextureIndex;

                passData.surfaceCacheOutput.surfaceCacheAlbedo =
                    builder.ReadTexture(surfaceCacheOutput.surfaceCacheAlbedo);
                passData.surfaceCacheOutput.surfaceCacheEmissive =
                    builder.ReadTexture(surfaceCacheOutput.surfaceCacheEmissive);
                passData.surfaceCacheOutput.surfaceCacheNormal =
                    builder.ReadTexture(surfaceCacheOutput.surfaceCacheNormal);
                passData.surfaceCacheOutput.surfaceCachePosWS =
                    builder.ReadTexture(surfaceCacheOutput.surfaceCachePosWS);

                HDShadowManager.ReadShadowResult(shadowResult, builder);

                passData.lightListBuffer = builder.ReadComputeBuffer(lightLists.lightList);
                passData.tileFeatureFlagsBuffer = builder.ReadComputeBuffer(lightLists.tileFeatureFlags);
                passData.tileListBuffer = builder.ReadComputeBuffer(lightLists.tileList);
                passData.dispatchIndirectBuffer = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);

                var output = new LightingOutput();
                output.colorBuffer = passData.colorBuffer;

                builder.SetRenderFunc(
                    (DeferredLightingPassData data, RenderGraphContext context) =>
                    {
                        var colorBuffers = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                        colorBuffers[0] = data.colorBuffer;

                        // TODO RENDERGRAPH: Remove these SetGlobal and properly send these textures to the deferred passes and bind them directly to compute shaders.
                        // This can wait that we remove the old code path.
                        context.cmd.SetGlobalTexture(HDShaderIDs._SurfaceCacheAlbedo, data.surfaceCacheOutput.surfaceCacheAlbedo);
                        context.cmd.SetGlobalTexture(HDShaderIDs._SurfaceCacheEmissive, data.surfaceCacheOutput.surfaceCacheEmissive);
                        context.cmd.SetGlobalTexture(HDShaderIDs._SurfaceCacheNormal, data.surfaceCacheOutput.surfaceCacheNormal);
                        context.cmd.SetGlobalTexture(HDShaderIDs._SurfaceCachePosWS, data.surfaceCacheOutput.surfaceCachePosWS);

                        if (data.lightLayersTextureIndex != -1)
                            context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, data.gbuffer[data.lightLayersTextureIndex]);
                        else
                            context.cmd.SetGlobalTexture(HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());

                        if (data.shadowMaskTextureIndex != -1)
                            context.cmd.SetGlobalTexture(HDShaderIDs._ShadowMaskTexture, data.gbuffer[data.shadowMaskTextureIndex]);
                        else
                            context.cmd.SetGlobalTexture(HDShaderIDs._ShadowMaskTexture, TextureXR.GetWhiteTexture());

                        BindGlobalLightingBuffers(data.lightingBuffers, context.cmd);

                        if (data.enableTile)
                        {
                            bool useCompute = data.useComputeLightingEvaluation && !k_PreferFragment;
                            if (useCompute)
                                RenderComputeDeferredLighting(data, colorBuffers, context.cmd, surfaceCache: true);
                            else
                                RenderComputeAsPixelDeferredLighting(data, colorBuffers, context.cmd);
                        }
                        else
                        {
                            RenderPixelDeferredLighting(data, colorBuffers, context.cmd);
                        }
                    });

                return output;
            }
        }


    }

}
