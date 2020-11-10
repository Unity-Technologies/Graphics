using System.Collections.Generic;
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

        internal void RenderShadows(RenderGraph renderGraph, in ShaderVariablesGlobal globalCB, HDCamera hdCamera, CullingResults cullResults, ref ShadowResult result)
        {
            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequestCount != 0 &&
                (hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects) || hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects)))
            {
                result.cachedPunctualShadowResult = cachedShadowManager.punctualShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Punctual Lights Shadows rendering");
                cachedShadowManager.punctualShadowAtlas.AddBlitRequestsForUpdatedShadows(m_Atlas);

                if (ShaderConfig.s_AreaLights == 1)
                {
                    result.cachedAreaShadowResult = cachedShadowManager.areaShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Area Lights Shadows rendering");
                    cachedShadowManager.areaShadowAtlas.AddBlitRequestsForUpdatedShadows(m_AreaLightShadowAtlas);
                }

                BlitCachedShadows(renderGraph, ref result);

                result.punctualShadowResult = m_Atlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Punctual Lights Shadows rendering");
                result.directionalShadowResult = m_CascadeAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Directional Light Shadows rendering");
                if (ShaderConfig.s_AreaLights == 1)
                    result.areaShadowResult = m_AreaLightShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Area Light Shadows rendering");
            }

            // TODO RENDERGRAPH
            // Not really good to bind things globally here (makes lifecycle of the textures fuzzy)
            // Probably better to bind it explicitly where needed (deferred lighting and forward/debug passes)
            // We can probably remove this when we have only one code path and can clean things up a bit.
            BindShadowGlobalResources(renderGraph, result);
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
                builder.AllowPassCulling(false);
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

        class BlitCachedShadowPassData
        {
            public TextureHandle sourceCachedAtlas;
            public TextureHandle atlasTexture;

            public HDDynamicShadowAtlas.ShadowBlitParameters shadowBlitParameters;
        }

        internal void BlitCachedShadows(RenderGraph renderGraph, ref ShadowResult shadowResult)
        {
            if (m_Atlas.HasPendingBlitsRequests())
            {
                using (var builder = renderGraph.AddRenderPass<BlitCachedShadowPassData>("Blit Punctual Mixed Cached Shadows", out var passData, ProfilingSampler.Get(HDProfileId.BlitPunctualMixedCachedShadowMaps)))
                {
                    passData.shadowBlitParameters = m_Atlas.PrepareShadowBlitParameters(cachedShadowManager.punctualShadowAtlas, m_BlitShadowMaterial, m_BlitShadowPropertyBlock);

                    passData.sourceCachedAtlas = builder.ReadTexture(shadowResult.cachedPunctualShadowResult);
                    passData.atlasTexture = builder.WriteTexture(shadowResult.punctualShadowResult);

                    builder.SetRenderFunc(
                    (BlitCachedShadowPassData data, RenderGraphContext ctx) =>
                    {
                        HDDynamicShadowAtlas.BlitCachedIntoAtlas(data.shadowBlitParameters, data.atlasTexture, data.sourceCachedAtlas, ctx.cmd);
                    });
                }
            }

            if (ShaderConfig.s_AreaLights == 1 && m_AreaLightShadowAtlas.HasPendingBlitsRequests())
            {
                using (var builder = renderGraph.AddRenderPass<BlitCachedShadowPassData>("Blit Area Mixed Cached Shadows", out var passData, ProfilingSampler.Get(HDProfileId.BlitAreaMixedCachedShadowMaps)))
                {
                    passData.shadowBlitParameters = m_AreaLightShadowAtlas.PrepareShadowBlitParameters(cachedShadowManager.areaShadowAtlas, m_BlitShadowMaterial, m_BlitShadowPropertyBlock);

                    passData.sourceCachedAtlas = builder.ReadTexture(shadowResult.cachedAreaShadowResult);
                    passData.atlasTexture = builder.WriteTexture(shadowResult.areaShadowResult);

                    builder.SetRenderFunc(
                    (BlitCachedShadowPassData data, RenderGraphContext ctx) =>
                    {
                        HDDynamicShadowAtlas.BlitCachedIntoAtlas(data.shadowBlitParameters, data.atlasTexture, data.sourceCachedAtlas, ctx.cmd);
                    });
                }
            }
        }
    }

    partial class HDShadowAtlas
    {
        protected TextureHandle m_Output;

        protected void InitializeRenderGraphOutput(RenderGraph renderGraph)
        {
            // TODO RENDERGRAPH remove null tests when we have only one path. RenderGraph should always be present.
            if (renderGraph != null)
            {
                if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
                {
                    m_Output = renderGraph.CreateSharedTexture(GetMomentAtlasDesc(m_MomentName));
                }
                else if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
                {
                    m_Output = renderGraph.CreateSharedTexture(new TextureDesc(width, height)
                        { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = m_MomentName, enableRandomWrite = true });
                }
                else
                {
                    m_Output = renderGraph.CreateSharedTexture(GetShadowMapTextureDesc(false));
                }
            }
        }

        protected void CleanupRenderGraphOutput(RenderGraph renderGraph)
        {
            if (renderGraph != null && m_Output.IsValid())
            {
                renderGraph.ReleaseSharedTexture(m_Output);
            }
        }


        public TextureDesc GetShadowMapTextureDesc(bool clearBuffer = false)
        {
            return new TextureDesc(width, height)
                { filterMode = m_FilterMode, depthBufferBits = m_DepthBufferBits, isShadowMap = true, name = m_Name, clearBuffer = clearBuffer };
        }

        class RenderShadowsPassData
        {
            public TextureHandle atlasTexture;
            public TextureHandle momentAtlasTexture1;
            public TextureHandle momentAtlasTexture2;
            public TextureHandle intermediateSummedAreaTexture;
            public TextureHandle summedAreaTexture;

            public RenderShadowsParameters parameters;
            public ShadowDrawingSettings shadowDrawSettings;

            public bool isRenderingOnACache;
        }

        TextureDesc GetMomentAtlasDesc(string name)
        {
            return new TextureDesc(width / 2, height / 2)
                    { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true };
        }

        internal TextureHandle RenderShadows(RenderGraph renderGraph, CullingResults cullResults, in ShaderVariablesGlobal globalCB, FrameSettings frameSettings, string shadowPassName)
        {
            if (m_ShadowRequests.Count == 0)
            {
                return renderGraph.defaultResources.blackTexture;
            }

            using (var builder = renderGraph.AddRenderPass<RenderShadowsPassData>(shadowPassName, out var passData, ProfilingSampler.Get(HDProfileId.RenderShadowMaps)))
            {
                passData.parameters = PrepareRenderShadowsParameters(globalCB);
                // TODO: Get rid of this and refactor to use the same kind of API than RendererList
                passData.shadowDrawSettings = new ShadowDrawingSettings(cullResults, 0);
                passData.shadowDrawSettings.useRenderingLayerMaskTest = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
                passData.isRenderingOnACache = m_IsACacheForShadows;

                if (passData.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                {
                    passData.atlasTexture = builder.WriteTexture(renderGraph.CreateTexture(GetShadowMapTextureDesc(passData.parameters.debugClearAtlas)));
                    passData.momentAtlasTexture1 = builder.WriteTexture(m_Output);
                    passData.momentAtlasTexture2 = builder.WriteTexture(renderGraph.CreateTexture(GetMomentAtlasDesc(m_MomentCopyName)));
                }
                else if (passData.parameters.blurAlgorithm == BlurAlgorithm.IM)
                {
                    passData.atlasTexture = builder.WriteTexture(renderGraph.CreateTexture(GetShadowMapTextureDesc(passData.parameters.debugClearAtlas)));
                    passData.momentAtlasTexture1 = builder.WriteTexture(m_Output);
                    passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_IntermediateSummedAreaName, enableRandomWrite = true }));
                    passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_SummedAreaName, enableRandomWrite = true }));
                }
                else
                {
                    passData.atlasTexture = builder.WriteTexture(m_Output);
                }


                builder.SetRenderFunc(
                (RenderShadowsPassData data, RenderGraphContext context) =>
                {
                    RenderShadows(  data.parameters,
                                    data.atlasTexture,
                                    data.shadowDrawSettings,
                                    context.renderContext,
                                    data.isRenderingOnACache,
                                    context.cmd);

                    if (data.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                    {
                        RTHandle[] momentTextures = context.renderGraphPool.GetTempArray<RTHandle>(2);
                        momentTextures[0] = data.momentAtlasTexture1;
                        momentTextures[1] = data.momentAtlasTexture2;

                        EVSMBlurMoments(data.parameters, data.atlasTexture, momentTextures, data.isRenderingOnACache, context.cmd);
                    }
                    else if (data.parameters.blurAlgorithm == BlurAlgorithm.IM)
                    {
                        IMBlurMoment(data.parameters, data.atlasTexture, data.momentAtlasTexture1, data.intermediateSummedAreaTexture, data.summedAreaTexture, context.cmd);
                    }
                });

                return m_Output;
            }
        }
    }
}
