using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        TextureHandle DenoiseDirectionalScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVetorsBuffer, TextureHandle historyValidityBuffer,
            TextureHandle noisyBuffer, TextureHandle velocityBuffer, TextureHandle distanceBuffer)
        {
            // Is the history still valid?
            int dirShadowIndex = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex & (int)LightDefinitions.s_ScreenSpaceShadowIndexMask;
            float historyValidity = EvaluateHistoryValidityDirectionalShadow(hdCamera, dirShadowIndex, m_CurrentSunLightAdditionalLightData);

            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = RequestShadowHistoryBuffer(hdCamera);
            RTHandle shadowHistoryDistanceArray = RequestShadowHistoryDistanceBuffer(hdCamera);
            RTHandle shadowHistoryValidityArray = RequestShadowHistoryValidityBuffer(hdCamera);

            // Evaluate the slot of the directional light (given that it may be a color shadow, we need to use the mask to get the actual slot indices)
            GetShadowChannelMask(dirShadowIndex, m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);
            GetShadowChannelMask(dirShadowIndex, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask1);

            // Apply the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();
            HDTemporalFilter.TemporalDenoiserArrayOutputData temporalFilterResult = temporalFilter.DenoiseBuffer(renderGraph, hdCamera,
                depthBuffer, normalBuffer, motionVetorsBuffer, historyValidityBuffer,
                noisyBuffer, shadowHistoryArray,
                distanceBuffer, shadowHistoryDistanceArray,
                velocityBuffer,
                shadowHistoryValidityArray,
                dirShadowIndex / 4, m_ShadowChannelMask0, m_ShadowChannelMask1,
                true, !m_CurrentSunLightAdditionalLightData.colorShadow, historyValidity);

            // Apply the spatial denoiser
            HDDiffuseShadowDenoiser shadowDenoiser = GetDiffuseShadowDenoiser();
            TextureHandle denoisedBuffer = shadowDenoiser.DenoiseBufferDirectional(renderGraph, hdCamera,
                depthBuffer, normalBuffer,
                temporalFilterResult.outputSignal, temporalFilterResult.outputSignalDistance,
                m_CurrentSunLightAdditionalLightData.filterSizeTraced, m_CurrentSunLightAdditionalLightData.angularDiameter * 0.5f, !m_CurrentSunLightAdditionalLightData.colorShadow);

            // Now that we have overriden this history, mark is as used by this light
            hdCamera.PropagateShadowHistory(m_CurrentSunLightAdditionalLightData, dirShadowIndex, GPULightType.Directional);

            return denoisedBuffer;
        }

        class RTSDirectionalTracePassData
        {
            public RTShadowDirectionalTraceParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle directionBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle velocityBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle outputShadowBuffer;
        }

        void RenderRayTracedDirectionalScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVetorsBuffer, TextureHandle historyValidityBuffer,
            TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            TextureHandle directionalShadow;
            TextureHandle velocityBuffer;
            TextureHandle distanceBuffer;
            RTShadowDirectionalTraceParameters rtsdtParams = PrepareRTShadowDirectionalTraceParameters(hdCamera, m_CurrentSunLightAdditionalLightData);
            using (var builder = renderGraph.AddRenderPass<RTSDirectionalTracePassData>("Directional RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
            {
                passData.parameters = rtsdtParams;

                // Input Buffer
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });

                // Debug buffers
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);

                // Output Buffers
                passData.velocityBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8_SNorm, enableRandomWrite = true, clearBuffer = true, name = "Velocity Buffer" }));
                passData.distanceBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, clearBuffer = true, name = "Distance Buffer" }));
                passData.outputShadowBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, clearBuffer = true, name = "RT Directional Shadow" }));

                builder.SetRenderFunc(
                    (RTSDirectionalTracePassData data, RenderGraphContext context) =>
                    {
                        RTShadowDirectionalTraceResources resources = new RTShadowDirectionalTraceResources();
                        resources.depthStencilBuffer = data.depthStencilBuffer;
                        resources.normalBuffer = data.normalBuffer;
                        resources.directionBuffer = data.directionBuffer;
                        resources.rayCountTexture = data.rayCountTexture;
                        resources.velocityBuffer = data.velocityBuffer;
                        resources.distanceBuffer = data.distanceBuffer;
                        resources.outputShadowBuffer = data.outputShadowBuffer;
                        ExecuteSSSDirectionalTrace(context.cmd, data.parameters, resources);
                    });
                directionalShadow = passData.outputShadowBuffer;
                velocityBuffer = passData.velocityBuffer;
                distanceBuffer = passData.distanceBuffer;
            }

            // If required, denoise the shadow
            if (m_CurrentSunLightAdditionalLightData.filterTracedShadow && rtsdtParams.softShadow)
            {
                directionalShadow = DenoiseDirectionalScreenSpaceShadow(renderGraph, hdCamera,
                    depthBuffer, normalBuffer, motionVetorsBuffer, historyValidityBuffer,
                    directionalShadow, velocityBuffer, distanceBuffer);
            }

            int dirShadowIndex = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex & (int)LightDefinitions.s_ScreenSpaceShadowIndexMask;
            ScreenSpaceShadowType shadowType = m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale;

            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadow(renderGraph, hdCamera, directionalShadow, screenSpaceShadowArray, dirShadowIndex, shadowType);
        }

        class SSSDirectionalTracePassData
        {
            public SSShadowDirectionalParameters parameters;
            public TextureHandle normalBuffer;
            public TextureHandle screenSpaceShadowArray;
        }

        void RenderDirectionalLightScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidityBuffer,
            TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            // Should we be executing anything really?
            bool screenSpaceShadowRequired = m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow();

            // Render directional screen space shadow if required
            if (screenSpaceShadowRequired)
            {
                bool rayTracedDirectionalRequired = m_CurrentSunLightAdditionalLightData.WillRenderRayTracedShadow();
                // If the shadow is flagged as ray traced, we need to evaluate it completely
                if (rayTracedDirectionalRequired)
                    RenderRayTracedDirectionalScreenSpaceShadow(renderGraph, hdCamera, depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer, rayCountTexture, screenSpaceShadowArray);
                else
                {
                    using (var builder = renderGraph.AddRenderPass<SSSDirectionalTracePassData>("Directional RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
                    {
                        passData.parameters = PrepareSSShadowDirectionalParameters();
                        passData.normalBuffer = builder.ReadTexture(normalBuffer);
                        passData.screenSpaceShadowArray = builder.ReadWriteTexture(screenSpaceShadowArray);

                        builder.SetRenderFunc(
                            (SSSDirectionalTracePassData data, RenderGraphContext context) =>
                            {
                                ExecuteSSShadowDirectional(context.cmd, data.parameters, context.renderGraphPool.GetTempMaterialPropertyBlock(), data.normalBuffer, data.screenSpaceShadowArray);
                            });
                    }
                }
            }
        }
    }
}
