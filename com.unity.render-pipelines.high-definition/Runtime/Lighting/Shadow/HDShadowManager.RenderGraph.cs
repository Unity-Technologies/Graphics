using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct ShadowResult
    {
        public RenderGraphResource punctualShadowResult;
        public RenderGraphResource directionalShadowResult;
        public RenderGraphResource areaShadowResult;
    }

    partial class HDShadowManager
    {
        internal static ShadowResult ReadShadowResult(ShadowResult shadowResult, RenderGraphBuilder builder)
        {
            var result = new ShadowResult();

            if (shadowResult.punctualShadowResult.IsValid())
                result.punctualShadowResult = builder.ReadTexture(shadowResult.punctualShadowResult);
            if (shadowResult.directionalShadowResult.IsValid())
                result.directionalShadowResult = builder.ReadTexture(shadowResult.directionalShadowResult);
            if (shadowResult.areaShadowResult.IsValid())
                result.areaShadowResult = builder.ReadTexture(shadowResult.areaShadowResult);

            return result;
        }

        internal ShadowResult RenderShadows(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults)
        {
            var result = new ShadowResult();
            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequestCount == 0)
                return result;

            result.punctualShadowResult = m_Atlas.RenderShadows(renderGraph, cullResults, hdCamera.frameSettings, "Punctual Lights Shadows rendering");
            result.directionalShadowResult = m_CascadeAtlas.RenderShadows(renderGraph, cullResults, hdCamera.frameSettings, "Directional Light Shadows rendering");
            result.areaShadowResult = m_AreaLightShadowAtlas.RenderShadows(renderGraph, cullResults, hdCamera.frameSettings, "Area Light Shadows rendering");

            return result;
        }
    }

    partial class HDShadowAtlas
    {
        class RenderShadowsPassData
        {
            public RenderGraphMutableResource atlasTexture;
            public RenderGraphMutableResource momentAtlasTexture1;
            public RenderGraphMutableResource momentAtlasTexture2;
            public RenderGraphMutableResource intermediateSummedAreaTexture;
            public RenderGraphMutableResource summedAreaTexture;

            public RenderShadowsParameters parameters;
            public ShadowDrawingSettings shadowDrawSettings;
        }

        RenderGraphMutableResource AllocateMomentAtlas(RenderGraph renderGraph, string name, int shaderID = 0)
        {
            return renderGraph.CreateTexture(new TextureDesc(width / 2, height / 2)
                    { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true }, shaderID);
        }

        internal RenderGraphResource RenderShadows(RenderGraph renderGraph, CullingResults cullResults, FrameSettings frameSettings, string shadowPassName)
        {
            RenderGraphResource result = new RenderGraphResource();

            if (m_ShadowRequests.Count == 0)
                return result;

            using (var builder = renderGraph.AddRenderPass<RenderShadowsPassData>(shadowPassName, out var passData, ProfilingSampler.Get(HDProfileId.RenderShadowMaps)))
            {
                passData.parameters = PrepareRenderShadowsParameters();
                // TODO: Get rid of this and refactor to use the same kind of API than RendererList
                passData.shadowDrawSettings = new ShadowDrawingSettings(cullResults, 0);
                passData.shadowDrawSettings.useRenderingLayerMaskTest = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
                passData.atlasTexture = builder.WriteTexture(
                        renderGraph.CreateTexture(  new TextureDesc(width, height)
                            { filterMode = m_FilterMode, depthBufferBits = m_DepthBufferBits, isShadowMap = true, name = m_Name, clearBuffer = passData.parameters.debugClearAtlas }, passData.parameters.atlasShaderID));

                result = passData.atlasTexture;

                if (passData.parameters.blurAlgorithm == BlurAlgorithm.EVSM)
                {
                    passData.momentAtlasTexture1 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, string.Format("{0}Moment", m_Name), passData.parameters.momentAtlasShaderID));
                    passData.momentAtlasTexture2 = builder.WriteTexture(AllocateMomentAtlas(renderGraph, string.Format("{0}MomentCopy", m_Name)));

                    result = passData.momentAtlasTexture1;
                }
                else if (passData.parameters.blurAlgorithm == BlurAlgorithm.IM)
                {
                    passData.momentAtlasTexture1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = string.Format("{0}Moment", m_Name), enableRandomWrite = true }, passData.parameters.momentAtlasShaderID));
                    passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = string.Format("{0}IntermediateSummedArea", m_Name), enableRandomWrite = true }, passData.parameters.momentAtlasShaderID));
                    passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                                                    { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = string.Format("{0}SummedArea", m_Name), enableRandomWrite = true }, passData.parameters.momentAtlasShaderID));

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
