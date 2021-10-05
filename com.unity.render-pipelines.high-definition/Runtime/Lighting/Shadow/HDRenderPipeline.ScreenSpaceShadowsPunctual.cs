using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static float EvaluateHistoryValidityPointShadow(HDCamera hdCamera, LightData lightData, HDAdditionalLightData additionalLightData)
        {
            // We need to set the history as invalid if the light has moved (rotated or translated),
            float historyValidity = 1.0f;
            if (additionalLightData.previousTransform != additionalLightData.transform.localToWorldMatrix
                || !hdCamera.ValidShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType))
                historyValidity = 0.0f;

            // We need to check if the camera implied an invalidation
            historyValidity *= EvaluateHistoryValidity(hdCamera);

            return historyValidity;
        }

        TextureHandle DenoisePunctualScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            HDAdditionalLightData additionalLightData, in LightData lightData,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVetorsBuffer, TextureHandle historyValidityBuffer,
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
                depthBuffer, normalBuffer, motionVetorsBuffer, historyValidityBuffer,
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
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public bool softShadow;
            public bool distanceBasedFiltering;
            public int numShadowSamples;
            public bool semiTransparentShadow;
            public GPULightType lightType;
            public float spotAngle;
            public float shapeRadius;
            public int lightIndex;

            // Kernels
            public int clearShadowKernel;
            public int shadowKernel;

            // Other parameters
            public RayTracingShader screenSpaceShadowRT;
            public ComputeShader screenSpaceShadowCS;
            public RayTracingAccelerationStructure accelerationStructure;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;

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
            PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidityBuffer, TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            TextureHandle pointShadowBuffer;
            TextureHandle velocityBuffer;
            TextureHandle distanceBuffer;

            bool softShadow = additionalLightData.shapeRadius > 0.0 ? true : false;

            using (var builder = renderGraph.AddRenderPass<RTSPunctualTracePassData>("Punctual RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
            {
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.softShadow = softShadow;
                // If the surface is infinitively small, we force it to one sample.
                passData.numShadowSamples = passData.softShadow ? additionalLightData.numRayTracingSamples : 1;
                passData.distanceBasedFiltering = additionalLightData.distanceBasedFiltering;
                passData.semiTransparentShadow = additionalLightData.semiTransparentShadow;
                passData.lightType = lightData.lightType;
                passData.spotAngle = additionalLightData.legacyLight.spotAngle;
                passData.shapeRadius = additionalLightData.shapeRadius;
                passData.lightIndex = lightIndex;

                // Kernels
                passData.clearShadowKernel = m_ClearShadowTexture;
                passData.shadowKernel = lightData.lightType == GPULightType.Point ? m_RaytracingPointShadowSample : m_RaytracingSpotShadowSample;

                // Grab the acceleration structure for the target camera
                passData.accelerationStructure = RequestAccelerationStructure();
                passData.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;
                passData.screenSpaceShadowRT = m_ScreenSpaceShadowsRT;
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                // Input Buffer
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });
                passData.rayLengthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Ray Length Buffer" });

                // Debug buffers
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);

                // Output Buffers
                passData.velocityBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8_SNorm, enableRandomWrite = true, name = "Velocity Buffer" }));
                passData.distanceBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" }));
                passData.outputShadowBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "RT Sphere Shadow" }));

                builder.SetRenderFunc(
                    (RTSPunctualTracePassData data, RenderGraphContext ctx) =>
                    {
                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // Clear the integration textures
                        ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                        ctx.cmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.velocityBuffer);
                        ctx.cmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        if (data.distanceBasedFiltering)
                        {
                            ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.distanceBuffer);
                            ctx.cmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);
                        }

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.screenSpaceShadowRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Define the shader pass to use for the reflection pass
                        ctx.cmd.SetRayTracingShaderPass(data.screenSpaceShadowRT, "VisibilityDXR");

                        // Loop through the samples of this frame
                        for (int sampleIdx = 0; sampleIdx < data.numShadowSamples; ++sampleIdx)
                        {
                            // Update global constant buffer
                            data.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIdx;
                            data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.numShadowSamples;
                            ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                            // Bind the light & sampling data
                            ctx.cmd.SetComputeIntParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingTargetAreaLight, data.lightIndex);
                            ctx.cmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingLightRadius, data.shapeRadius);

                            // If this is a spot light, inject the spot angle in radians
                            if (data.lightType == GPULightType.Spot)
                            {
                                float spotAngleRadians = data.spotAngle * (float)Math.PI / 180.0f;
                                ctx.cmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingSpotAngle, spotAngleRadians);
                            }

                            // Input Buffer
                            ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                            // Output buffers
                            ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                            ctx.cmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);

                            // Generate a new direction
                            ctx.cmd.DispatchCompute(data.screenSpaceShadowCS, data.shadowKernel, numTilesX, numTilesY, data.viewCount);

                            // Define the shader pass to use for the shadow pass
                            ctx.cmd.SetRayTracingShaderPass(data.screenSpaceShadowRT, "VisibilityDXR");

                            // Set ray count texture
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                            // Input buffers
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);

                            // Output buffer
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._VelocityBuffer, data.velocityBuffer);
                            ctx.cmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDistanceBufferRW, data.distanceBuffer);

                            CoreUtils.SetKeyword(ctx.cmd, "TRANSPARENT_COLOR_SHADOW", data.semiTransparentShadow);
                            ctx.cmd.DispatchRays(data.screenSpaceShadowRT, data.semiTransparentShadow ? m_RayGenSemiTransparentShadowSegmentSingleName : m_RayGenShadowSegmentSingleName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount);
                            CoreUtils.SetKeyword(ctx.cmd, "TRANSPARENT_COLOR_SHADOW", false);
                        }
                    });
                pointShadowBuffer = passData.outputShadowBuffer;
                velocityBuffer = passData.velocityBuffer;
                distanceBuffer = passData.distanceBuffer;
            }

            // If required, denoise the shadow
            if (additionalLightData.filterTracedShadow && softShadow)
            {
                pointShadowBuffer = DenoisePunctualScreenSpaceShadow(renderGraph, hdCamera,
                    additionalLightData, lightData,
                    depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer,
                    pointShadowBuffer, velocityBuffer, distanceBuffer);
            }

            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadow(renderGraph, hdCamera, pointShadowBuffer, screenSpaceShadowArray, lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale);
        }
    }
}
