using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct ShadowResult
    {
        public TextureHandle punctualShadowResult;
        public TextureHandle cachedPunctualShadowResult;
        public TextureHandle directionalShadowResult;
        public TextureHandle areaShadowResult;
        public TextureHandle cachedAreaShadowResult;
    }

    partial class HDShadowManager
    {
        internal static ShadowResult ReadShadowResult(in ShadowResult shadowResult, RenderGraphBuilder builder)
        {
            var result = new ShadowResult();

            if (shadowResult.punctualShadowResult.IsValid())
                result.punctualShadowResult = builder.ReadTexture(shadowResult.punctualShadowResult);
            if (shadowResult.directionalShadowResult.IsValid())
                result.directionalShadowResult = builder.ReadTexture(shadowResult.directionalShadowResult);
            if (shadowResult.areaShadowResult.IsValid())
                result.areaShadowResult = builder.ReadTexture(shadowResult.areaShadowResult);
            if (shadowResult.cachedPunctualShadowResult.IsValid())
                result.cachedPunctualShadowResult = builder.ReadTexture(shadowResult.cachedPunctualShadowResult);
            if (shadowResult.cachedAreaShadowResult.IsValid())
                result.cachedAreaShadowResult = builder.ReadTexture(shadowResult.cachedAreaShadowResult);

            return result;
        }

        internal ShadowResult RenderShadows(RenderGraph renderGraph, in ShaderVariablesGlobal globalCB, HDCamera hdCamera, CullingResults cullResults)
        {
            var result = new ShadowResult();
            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequestCount != 0)
            {
                result.punctualShadowResult = m_Atlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Punctual Lights Shadows rendering");
                result.directionalShadowResult = m_CascadeAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Directional Light Shadows rendering");
                result.areaShadowResult = m_AreaLightShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Area Light Shadows rendering");
            	result.cachedPunctualShadowResult = cachedShadowManager.punctualShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Punctual Lights Shadows rendering");
            	result.cachedAreaShadowResult = cachedShadowManager.areaShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Area Lights Shadows rendering");
            }

            // TODO RENDERGRAPH
            // Not really good to bind things globally here (makes lifecycle of the textures fuzzy)
            // Probably better to bind it explicitly where needed (deferred lighting and forward/debug passes)
            BindShadowGlobalResources(renderGraph, result);

            return result;
        }

        class BindShadowGlobalResourcesPassData
        {
            public ShadowResult shadowResult;
        }


        static void BindAtlasTexture(RenderGraphContext ctx, TextureHandle texture, int shaderId)
        {
            if (texture.IsValid())
                ctx.cmd.SetGlobalTexture(shaderId, texture);
            else
                ctx.cmd.SetGlobalTexture(shaderId, ctx.defaultResources.blackTexture);
        }

        void BindShadowGlobalResources(RenderGraph renderGraph, in ShadowResult shadowResult)
        {
            using (var builder = renderGraph.AddRenderPass<BindShadowGlobalResourcesPassData>("BindShadowGlobalResources", out var passData))
            {
                passData.shadowResult = ReadShadowResult(shadowResult, builder);
                builder.AllowPassPruning(false);
                builder.SetRenderFunc(
                (BindShadowGlobalResourcesPassData data, RenderGraphContext ctx) =>
                {
                    BindAtlasTexture(ctx, data.shadowResult.punctualShadowResult, HDShaderIDs._ShadowmapAtlas);
                    BindAtlasTexture(ctx, data.shadowResult.directionalShadowResult, HDShaderIDs._ShadowmapCascadeAtlas);
                    BindAtlasTexture(ctx, data.shadowResult.areaShadowResult, HDShaderIDs._ShadowmapAreaAtlas);
                    BindAtlasTexture(ctx, data.shadowResult.cachedPunctualShadowResult, HDShaderIDs._CachedShadowmapAtlas);
                    BindAtlasTexture(ctx, data.shadowResult.cachedAreaShadowResult, HDShaderIDs._CachedAreaLightShadowmapAtlas);
                });
            }
        }
    }

    partial class HDShadowAtlas
    {
        class RenderShadowsPassData
        {
            public TextureHandle atlasTexture;
            public TextureHandle momentAtlasTexture1;
            public TextureHandle momentAtlasTexture2;
            public TextureHandle intermediateSummedAreaTexture;
            public TextureHandle summedAreaTexture;

            public RenderShadowsParameters parameters;
            public ShadowDrawingSettings shadowDrawSettings;
        }

        TextureHandle AllocateMomentAtlas(RenderGraph renderGraph, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(width / 2, height / 2)
                    { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true });
        }

        internal TextureHandle RenderShadows(RenderGraph renderGraph, CullingResults cullResults, in ShaderVariablesGlobal globalCB, FrameSettings frameSettings, string shadowPassName)
        {
            TextureHandle result = new TextureHandle();

            if (m_ShadowRequests.Count == 0)
                return result;

            using (var builder = renderGraph.AddRenderPass<RenderShadowsPassData>(shadowPassName, out var passData, ProfilingSampler.Get(HDProfileId.RenderShadowMaps)))
            {
                passData.parameters = PrepareRenderShadowsParameters(globalCB);
                // TODO: Get rid of this and refactor to use the same kind of API than RendererList
                passData.shadowDrawSettings = new ShadowDrawingSettings(cullResults, 0);
                passData.shadowDrawSettings.useRenderingLayerMaskTest = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
                passData.atlasTexture = builder.WriteTexture(
                        renderGraph.CreateTexture(  new TextureDesc(width, height)
                            { filterMode = m_FilterMode, depthBufferBits = m_DepthBufferBits, isShadowMap = true, name = m_Name, clearBuffer = passData.parameters.debugClearAtlas }));

                result = passData.atlasTexture;

                if (passData.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                {
                    passData.momentAtlasTexture1 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, m_MomentName));
                    passData.momentAtlasTexture2 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, m_MomentCopyName));

                    result = passData.momentAtlasTexture1;
                }
                else if (passData.parameters.blurAlgorithm == BlurAlgorithm.IM)
                {
                    passData.momentAtlasTexture1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = m_MomentName, enableRandomWrite = true }));
                    passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_IntermediateSummedAreaName, enableRandomWrite = true }));
                    passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_SummedAreaName, enableRandomWrite = true }));

                    result = passData.momentAtlasTexture1;
                }


                builder.SetRenderFunc(
                (RenderShadowsPassData data, RenderGraphContext context) =>
                {
                    RenderShadows(  data.parameters,
                                    data.atlasTexture,
                                    data.shadowDrawSettings,
                                    context.renderContext, context.cmd);

                    if (data.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                    {
                        RTHandle[] momentTextures = context.renderGraphPool.GetTempArray<RTHandle>(2);
                        momentTextures[0] = data.momentAtlasTexture1;
                        momentTextures[1] = data.momentAtlasTexture2;

                        EVSMBlurMoments(data.parameters, data.atlasTexture, momentTextures, context.cmd);
                    }
                    else if (data.parameters.blurAlgorithm == BlurAlgorithm.IM)
                    {
                        IMBlurMoment(data.parameters, data.atlasTexture, data.momentAtlasTexture1, data.intermediateSummedAreaTexture, data.summedAreaTexture, context.cmd);
                    }
                });

                return result;
            }
        }
    }
}
