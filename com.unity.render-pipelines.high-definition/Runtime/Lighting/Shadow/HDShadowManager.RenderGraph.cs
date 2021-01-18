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
            InvalidateAtlasOutputsIfNeeded();

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

                BlitCachedShadows(renderGraph);

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

        internal void ReleaseSharedShadowAtlases(RenderGraph renderGraph)
        {
            cachedShadowManager.punctualShadowAtlas.CleanupRenderGraphOutput(renderGraph);
            if (ShaderConfig.s_AreaLights == 1)
                cachedShadowManager.areaShadowAtlas.CleanupRenderGraphOutput(renderGraph);

            cachedShadowManager.DefragAtlas(HDLightType.Point);
            cachedShadowManager.DefragAtlas(HDLightType.Spot);
            if (ShaderConfig.s_AreaLights == 1)
                cachedShadowManager.DefragAtlas(HDLightType.Area);
        }

        void InvalidateAtlasOutputsIfNeeded()
        {
            cachedShadowManager.punctualShadowAtlas.InvalidateOutputIfNeeded();
            m_Atlas.InvalidateOutputIfNeeded();
            m_CascadeAtlas.InvalidateOutputIfNeeded();
            if (ShaderConfig.s_AreaLights == 1)
            {
                cachedShadowManager.areaShadowAtlas.InvalidateOutputIfNeeded();
                m_AreaLightShadowAtlas.InvalidateOutputIfNeeded();
            }
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

        internal void BlitCachedShadows(RenderGraph renderGraph)
        {
            if (m_Atlas.HasPendingBlitsRequests())
            {
                using (var builder = renderGraph.AddRenderPass<BlitCachedShadowPassData>("Blit Punctual Mixed Cached Shadows", out var passData, ProfilingSampler.Get(HDProfileId.BlitPunctualMixedCachedShadowMaps)))
                {
                    passData.shadowBlitParameters = m_Atlas.PrepareShadowBlitParameters(cachedShadowManager.punctualShadowAtlas, m_BlitShadowMaterial, m_BlitShadowPropertyBlock);

                    passData.sourceCachedAtlas = builder.ReadTexture(cachedShadowManager.punctualShadowAtlas.GetOutputTexture(renderGraph));
                    passData.atlasTexture = builder.WriteTexture(m_Atlas.GetOutputTexture(renderGraph));

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

                    passData.sourceCachedAtlas = builder.ReadTexture(cachedShadowManager.areaShadowAtlas.GetOutputTexture(renderGraph));
                    passData.atlasTexture = builder.WriteTexture(m_AreaLightShadowAtlas.GetOutputTexture(renderGraph));

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
        bool m_UseSharedTexture;

        protected TextureHandle m_Output;


        public TextureDesc GetShadowMapTextureDesc()
        {
            return new TextureDesc(width, height)
            { filterMode = m_FilterMode, depthBufferBits = m_DepthBufferBits, isShadowMap = true, name = m_Name };
        }

        TextureDesc GetMomentAtlasDesc(string name)
        {
            return new TextureDesc(width / 2, height / 2)
            { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true };
        }

        TextureDesc GetImprovedMomentAtlasDesc()
        {
            return new TextureDesc(width, height)
            { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = m_MomentName, enableRandomWrite = true };
        }

        TextureDesc GetAtlasDesc()
        {
            switch (m_BlurAlgorithm)
            {
                case (BlurAlgorithm.None):
                    return GetShadowMapTextureDesc();
                case BlurAlgorithm.EVSM:
                    return GetMomentAtlasDesc(m_MomentName);
                case BlurAlgorithm.IM:
                    return GetImprovedMomentAtlasDesc();
            }

            return default;
        }

        public void InvalidateOutputIfNeeded()
        {
            // Since we now store the output TextureHandle (because we only want to create the texture once depending on the control flow and because of shared textures),
            // we need to be careful not to keep a "valid" handle when it's not a shared resource.
            // Indeed, if for example we don't render with the atlas for a few frames, this handle will "look" valid (with a valid index internally) but its index will not match any valid resource.
            // To avoid that, we invalidate it explicitly at the start of every frame if it's not a shared resource.
            if (!m_UseSharedTexture)
            {
                m_Output = TextureHandle.nullHandle;
            }
        }

        public TextureHandle GetOutputTexture(RenderGraph renderGraph)
        {
            if (m_UseSharedTexture)
            {
                Debug.Assert(m_Output.IsValid());
                return m_Output; // Should always be valid.
            }
            else
            {
                renderGraph.CreateTextureIfInvalid(GetAtlasDesc(), ref m_Output);
                return m_Output;
            }
        }

        protected void InitializeRenderGraphOutput(RenderGraph renderGraph, bool useSharedTexture)
        {
            // TODO RENDERGRAPH remove null tests when we have only one path. RenderGraph should always be present.
            if (renderGraph != null)
            {
                // First release if not needed anymore.
                if (m_UseSharedTexture)
                {
                    Debug.Assert(useSharedTexture, "Shadow atlas can't go from shared to non-shared texture");
                }

                m_UseSharedTexture = useSharedTexture;
                // Else it's created on the fly like a regular render graph texture.
                // Also when using shared texture (for static shadows) we want to manage lifetime manually. Otherwise this would break static shadow caching.
                if (m_UseSharedTexture)
                    m_Output = renderGraph.CreateSharedTexture(GetAtlasDesc(), explicitRelease: true);
            }
        }

        internal void CleanupRenderGraphOutput(RenderGraph renderGraph)
        {
            if (m_UseSharedTexture && renderGraph != null && m_Output.IsValid())
            {
                renderGraph.ReleaseSharedTexture(m_Output);
                m_UseSharedTexture = false;
                m_Output = TextureHandle.nullHandle;
            }
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
                    passData.atlasTexture = builder.WriteTexture(renderGraph.CreateTexture(GetShadowMapTextureDesc()));
                    passData.momentAtlasTexture1 = builder.WriteTexture(GetOutputTexture(renderGraph));
                    passData.momentAtlasTexture2 = builder.WriteTexture(renderGraph.CreateTexture(GetMomentAtlasDesc(m_MomentCopyName)));
                }
                else if (passData.parameters.blurAlgorithm == BlurAlgorithm.IM)
                {
                    passData.atlasTexture = builder.WriteTexture(renderGraph.CreateTexture(GetShadowMapTextureDesc()));
                    passData.momentAtlasTexture1 = builder.WriteTexture(GetOutputTexture(renderGraph));
                    passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                        { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_IntermediateSummedAreaName, enableRandomWrite = true }));
                    passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                        { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_SummedAreaName, enableRandomWrite = true }));
                }
                else
                {
                    passData.atlasTexture = builder.WriteTexture(GetOutputTexture(renderGraph));
                }

                builder.SetRenderFunc(
                    (RenderShadowsPassData data, RenderGraphContext context) =>
                    {
                        RenderShadows(data.parameters,
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

                return GetOutputTexture(renderGraph);
            }
        }
    }
}
