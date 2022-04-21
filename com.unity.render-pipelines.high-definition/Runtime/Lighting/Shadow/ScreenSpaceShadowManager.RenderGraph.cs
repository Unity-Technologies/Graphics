using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        internal TextureHandle CreateScreenSpaceShadowTextureArray(RenderGraph renderGraph)
        {
            int numShadowTextures = Math.Max((int)Math.Ceiling(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots / 4.0f), 1);
            GraphicsFormat graphicsFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.screenSpaceShadowBufferFormat;
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = graphicsFormat,
                slices = numShadowTextures * TextureXR.slices,
                dimension = TextureDimension.Tex2DArray,
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "ScreenSpaceShadowArrayBuffer"
            });
        }


        class ScreenSpaceShadowDebugPassData
        {
            public SSShadowDebugParameters parameters;
            public TextureHandle screenSpaceShadowArray;
            public TextureHandle outputBuffer;
        }

        TextureHandle EvaluateShadowDebugView(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle screenSpaceShadowArray)
        {
            // If this is the right debug mode and the index we are asking for is in the range
            if (!rayTracingSupported || (m_ScreenSpaceShadowChannelSlot <= m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex))
                return m_RenderGraph.defaultResources.blackTextureXR;

            using (var builder = renderGraph.AddRenderPass<ScreenSpaceShadowDebugPassData>("Screen Space Shadows Debug", out var passData, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadowsDebug)))
            {
                passData.parameters = PrepareSSShadowDebugParameters(hdCamera, (int)m_CurrentDebugDisplaySettings.data.screenSpaceShadowIndex);
                passData.screenSpaceShadowArray = builder.ReadTexture(screenSpaceShadowArray);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "EvaluateShadowDebug" }));

                builder.SetRenderFunc(
                (ScreenSpaceShadowDebugPassData data, RenderGraphContext context) =>
                {
                    SSShadowDebugResources resources = new SSShadowDebugResources();
                    resources.screenSpaceShadowArray = data.screenSpaceShadowArray;
                    resources.outputBuffer = data.outputBuffer;
                    ExecuteShadowDebugView(context.cmd, data.parameters, resources);
                });
                return passData.outputBuffer;
            }
        }

        class WriteScreenSpaceShadowPassData
        {
            public WriteScreenSpaceShadowParameters parameters;
            public TextureHandle inputShadowBuffer;
            public TextureHandle outputShadowArrayBuffer;
        }

        void WriteScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle shadowTexture, TextureHandle screenSpaceShadowArray, int shadowIndex, ScreenSpaceShadowType shadowType)
        {
            // Write the result texture to the screen space shadow buffer
            using (var builder = renderGraph.AddRenderPass<WriteScreenSpaceShadowPassData>("Write Screen Space Shadows", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingWriteShadow)))
            {
                passData.parameters = PrepareWriteScreenSpaceShadowParameters(hdCamera, shadowIndex, shadowType);
                passData.inputShadowBuffer = builder.ReadTexture(shadowTexture);
                passData.outputShadowArrayBuffer = builder.ReadWriteTexture(screenSpaceShadowArray);

                builder.SetRenderFunc(
                (WriteScreenSpaceShadowPassData data, RenderGraphContext context) =>
                {
                    WriteScreenSpaceShadowResources resources = new WriteScreenSpaceShadowResources();
                    resources.inputShadowBuffer = data.inputShadowBuffer;
                    resources.outputShadowArrayBuffer = data.outputShadowArrayBuffer;
                    ExecuteWriteScreenSpaceShadow(context.cmd, data.parameters, resources);
                });
            }
        }

        bool RenderLightScreenSpaceShadows(RenderGraph renderGraph, HDCamera hdCamera, PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            // Loop through all the potential screen space light shadows
            for (int lightIdx = 0; lightIdx < m_ScreenSpaceShadowIndex; ++lightIdx)
            {
                // This matches the directional light
                if (!m_CurrentScreenSpaceShadowData[lightIdx].valid) continue;

                // Fetch the light data and additional light data
                LightData currentLight = m_lightList.lights[m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex];
                HDAdditionalLightData currentAdditionalLightData = m_CurrentScreenSpaceShadowData[lightIdx].additionalLightData;

                // Trigger the right algorithm based on the light type
                switch (currentLight.lightType)
                {
                    case GPULightType.Rectangle:
                        {
                            RenderAreaScreenSpaceShadow(renderGraph, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex,
                                                        prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, rayCountTexture, screenSpaceShadowArray);
                        }
                        break;
                    case GPULightType.Point:
                    case GPULightType.Spot:
                        {
                            RenderPunctualScreenSpaceShadow(renderGraph, hdCamera, currentLight, currentAdditionalLightData, m_CurrentScreenSpaceShadowData[lightIdx].lightDataIndex,
                                                            prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, rayCountTexture, screenSpaceShadowArray);
                        }
                        break;
                }
            }
            return true;
        }

        bool RequestedScreenSpaceShadows()
        {
            // We have screen space shadows that needs to be evaluated if we have one of these:
            // - A screen space directional shadow
            // - A ray traced directional shadow
            bool screenSpaceShadowDirectionalRequired = m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow();
            // - A ray traced spot or point shadow
            // - A ray traced area light shadow
            bool pointOrAreaLightShadowRequired = false;
            for (int lightIdx = 0; lightIdx < m_ScreenSpaceShadowIndex; ++lightIdx)
            {
                // This matches the directional light
                if (!m_CurrentScreenSpaceShadowData[lightIdx].valid) continue;

                pointOrAreaLightShadowRequired = true;
                break;
            }

            return screenSpaceShadowDirectionalRequired || pointOrAreaLightShadowRequired;
        }

        TextureHandle RenderScreenSpaceShadows(RenderGraph renderGraph, HDCamera hdCamera, PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle rayCountTexture)
        {
            // If screen space shadows are not supported for this camera, we are done
            bool validConditions = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows) && RequestedScreenSpaceShadows();

            if (!validConditions)
            {
                // We push the debug texture anyway if we are not evaluating any screen space shadows.
                PushFullScreenDebugTexture(m_RenderGraph, m_RenderGraph.defaultResources.whiteTextureXR, FullScreenDebugMode.ScreenSpaceShadows);
                return m_RenderGraph.defaultResources.blackTextureArrayXR;
            }

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.ScreenSpaceShadows)))
            {
                // Request the output texture
                TextureHandle screenSpaceShadowTexture = CreateScreenSpaceShadowTextureArray(renderGraph);

                // First of all we handle the directional light
                RenderDirectionalLightScreenSpaceShadow(renderGraph, hdCamera, depthBuffer, normalBuffer, motionVectorsBuffer, rayCountTexture, screenSpaceShadowTexture);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    // We handle the other light sources
                    RenderLightScreenSpaceShadows(renderGraph, hdCamera, prepassOutput, depthBuffer, normalBuffer, motionVectorsBuffer, rayCountTexture, screenSpaceShadowTexture);
                }

                // We render the debug view, if the texture is not used, it is not evaluated anyway
                TextureHandle screenSpaceShadowDebug = EvaluateShadowDebugView(renderGraph, hdCamera, screenSpaceShadowTexture);
                PushFullScreenDebugTexture(m_RenderGraph, screenSpaceShadowDebug, FullScreenDebugMode.ScreenSpaceShadows);

                return screenSpaceShadowTexture;
            }
        }
    }
}
