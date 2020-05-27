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
                ctx.cmd.SetGlobalTexture(shaderId, ctx.resources.GetTexture(texture));
            else
                ctx.cmd.SetGlobalTexture(shaderId, ctx.resources.GetTexture(ctx.defaultResources.blackTexture));
        }

        void BindShadowGlobalResources(RenderGraph renderGraph, in ShadowResult shadowResult)
        {
            using (var builder = renderGraph.AddRenderPass<BindShadowGlobalResourcesPassData>("BindShadowGlobalResources", out var passData))
            {
                passData.shadowResult = ReadShadowResult(shadowResult, builder);
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

        TextureHandle AllocateMomentAtlas(RenderGraph renderGraph, string name, int shaderID = 0)
        {
            return renderGraph.CreateTexture(new TextureDesc(width / 2, height / 2)
                    { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true }, shaderID);
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
                    passData.momentAtlasTexture1 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, string.Format("{0}Moment", m_Name)));
                    passData.momentAtlasTexture2 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, string.Format("{0}MomentCopy", m_Name)));

                    result = passData.momentAtlasTexture1;
                }
                else if (passData.parameters.blurAlgorithm == BlurAlgorithm.IM)
                {
                    passData.momentAtlasTexture1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = string.Format("{0}Moment", m_Name), enableRandomWrite = true }));
                    passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = string.Format("{0}IntermediateSummedArea", m_Name), enableRandomWrite = true }));
                    passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = string.Format("{0}SummedArea", m_Name), enableRandomWrite = true }));

                    result = passData.momentAtlasTexture1;
                }


                builder.SetRenderFunc(
                (RenderShadowsPassData data, RenderGraphContext context) =>
                {
                    RTHandle atlasTexture = context.resources.GetTexture(data.atlasTexture);
                    RenderShadows(  data.parameters,
                                    atlasTexture,
                                    data.shadowDrawSettings,
                                    context.renderContext, context.cmd);

                    if (data.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                    {
                        RTHandle[] momentTextures = context.renderGraphPool.GetTempArray<RTHandle>(2);
                        momentTextures[0] = context.resources.GetTexture(data.momentAtlasTexture1);
                        momentTextures[1] = context.resources.GetTexture(data.momentAtlasTexture2);

                        EVSMBlurMoments(data.parameters, atlasTexture, momentTextures, context.cmd);
                    }
                    else if (data.parameters.blurAlgorithm == BlurAlgorithm.IM)
                    {
                        RTHandle momentAtlas = context.resources.GetTexture(data.momentAtlasTexture1);
                        RTHandle intermediateSummedArea = context.resources.GetTexture(data.intermediateSummedAreaTexture);
                        RTHandle summedArea = context.resources.GetTexture(data.summedAreaTexture);
                        IMBlurMoment(data.parameters, atlasTexture, momentAtlas, intermediateSummedArea, summedArea, context.cmd);
                    }
                });

                return result;
            }
        }
    }
}
