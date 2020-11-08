using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        TextureHandle DenoisePunctualScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
                                                    HDAdditionalLightData additionalLightData, in LightData lightData,
                                                    TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVetorsBuffer,
                                                    TextureHandle noisyBuffer, TextureHandle velocityBuffer, TextureHandle distanceBufferI)
        {
            // Is the history still valid?
            float historyValidity = EvaluateHistoryValidityPointShadow(hdCamera, lightData, additionalLightData);

            // Evaluate the channel mask
            GetShadowChannelMask(lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);

            // Apply the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();
            HDTemporalFilter.TemporalDenoiserArrayOutputData temporalFilterResult;


            // Only set the distance based denoising buffers if required.
            RTHandle shadowHistoryDistanceArray = null;
            TextureHandle distanceBuffer = new TextureHandle();
            if (additionalLightData.distanceBasedFiltering)
            {
                distanceBuffer = distanceBufferI;
                // Request the distance history buffer
                shadowHistoryDistanceArray = RequestShadowHistoryDistanceBuffer(hdCamera);
            }

            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = RequestShadowHistoryBuffer(hdCamera);
            RTHandle shadowHistoryValidityArray = RequestShadowHistoryValidityBuffer(hdCamera);

            temporalFilterResult = temporalFilter.DenoiseBuffer(renderGraph, hdCamera,
                            depthBuffer, normalBuffer, motionVetorsBuffer,
                            noisyBuffer, shadowHistoryArray,
                            distanceBuffer, shadowHistoryDistanceArray,
                            velocityBuffer,
                            shadowHistoryValidityArray,
                            lightData.screenSpaceShadowIndex / 4, m_ShadowChannelMask0, m_ShadowChannelMask0,
                            additionalLightData.distanceBasedFiltering, true, historyValidity);

            TextureHandle denoisedBuffer;
            if (additionalLightData.distanceBasedFiltering)
            {
                HDDiffuseShadowDenoiser shadowDenoiser = GetDiffuseShadowDenoiser();
                denoisedBuffer = shadowDenoiser.DenoiseBufferSphere(renderGraph, hdCamera,
                                depthBuffer, normalBuffer,
                                temporalFilterResult.outputSignal, temporalFilterResult.outputSignalDistance,
                                additionalLightData.filterSizeTraced, additionalLightData.transform.position, additionalLightData.shapeRadius);
            }
            else
            {
                HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                denoisedBuffer = simpleDenoiser.DenoiseBufferNoHistory(renderGraph, hdCamera,
                            depthBuffer, normalBuffer,
                            temporalFilterResult.outputSignal,
                            additionalLightData.filterSizeTraced, true);
            }

            // Now that we have overriden this history, mark is as used by this light
            hdCamera.PropagateShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType);

            return denoisedBuffer;
        }

        class RTSPunctualTracePassData
        {
            public SSSPunctualRayTraceParameters parameters;
            // Input Buffers
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;

            // Intermediate buffers
            public TextureHandle directionBuffer;
            public TextureHandle rayLengthBuffer;

            // Debug textures
            public TextureHandle rayCountTexture;

            // Output buffers
            public TextureHandle velocityBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle outputShadowBuffer;
        }

        void RenderPunctualScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera
        , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex,
            PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            TextureHandle pointShadowBuffer;
            TextureHandle velocityBuffer;
            TextureHandle distanceBuffer;
            SSSPunctualRayTraceParameters rtsptParams = PrepareSSSPunctualRayTraceParameters(hdCamera, additionalLightData, lightData, lightIndex);
            using (var builder = renderGraph.AddRenderPass<RTSPunctualTracePassData>("Punctual RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
            {
                passData.parameters = rtsptParams;

                // Input Buffer
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });
                passData.rayLengthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Ray Length Buffer" });

                // Debug buffers
                passData.rayCountTexture = builder.ReadTexture(builder.WriteTexture(rayCountTexture));

                // Output Buffers
                passData.velocityBuffer = builder.ReadTexture(builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8_SNorm, enableRandomWrite = true, name = "Velocity Buffer" })));
                passData.distanceBuffer = builder.ReadTexture(builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" })));
                passData.outputShadowBuffer = builder.ReadTexture(builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "RT Sphere Shadow" })));

                builder.SetRenderFunc(
                (RTSPunctualTracePassData data, RenderGraphContext context) =>
                {
                    SSSPunctualRayTraceResources resources = new SSSPunctualRayTraceResources();
                    resources.depthStencilBuffer = data.depthStencilBuffer;
                    resources.normalBuffer = data.normalBuffer;
                    resources.directionBuffer = data.directionBuffer;
                    resources.rayLengthBuffer = data.rayLengthBuffer;
                    resources.rayCountTexture = data.rayCountTexture;
                    resources.velocityBuffer = data.velocityBuffer;
                    resources.distanceBuffer = data.distanceBuffer;
                    resources.outputShadowBuffer = data.outputShadowBuffer;
                    ExecuteSSSPunctualRayTrace(context.cmd, data.parameters, resources);
                });
                pointShadowBuffer = passData.outputShadowBuffer;
                velocityBuffer = passData.velocityBuffer;
                distanceBuffer = passData.distanceBuffer;
            }

            // If required, denoise the shadow
            if (additionalLightData.filterTracedShadow && rtsptParams.softShadow)
            {
                pointShadowBuffer = DenoisePunctualScreenSpaceShadow(renderGraph, hdCamera,
                                                additionalLightData, lightData,
                                                depthBuffer, normalBuffer, motionVectorsBuffer,
                                                pointShadowBuffer, velocityBuffer, distanceBuffer);
            }

            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadow(renderGraph, hdCamera, pointShadowBuffer, screenSpaceShadowArray, lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale);
        }
    }
}
