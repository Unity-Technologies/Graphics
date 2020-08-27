using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct SSSPunctualRayTraceParameters
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
        }

        SSSPunctualRayTraceParameters PrepareSSSPunctualRayTraceParameters(HDCamera hdCamera, HDAdditionalLightData additionalLightData, LightData lightData, int lightIndex)
        {
            SSSPunctualRayTraceParameters ssprtParams = new SSSPunctualRayTraceParameters();

            // Set the camera parameters
            ssprtParams.texWidth = hdCamera.actualWidth;
            ssprtParams.texHeight = hdCamera.actualHeight;
            ssprtParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            ssprtParams.softShadow = additionalLightData.shapeRadius > 0.0 ? true : false;
            // If the surface is infinitively small, we force it to one sample.
            ssprtParams.numShadowSamples = ssprtParams.softShadow ? additionalLightData.numRayTracingSamples : 1;
            ssprtParams.distanceBasedFiltering = additionalLightData.distanceBasedFiltering;
            ssprtParams.semiTransparentShadow = additionalLightData.semiTransparentShadow;
            ssprtParams.lightType = lightData.lightType;
            ssprtParams.spotAngle = additionalLightData.legacyLight.spotAngle;
            ssprtParams.shapeRadius = additionalLightData.shapeRadius;
            ssprtParams.lightIndex = lightIndex;

            // Kernels
            ssprtParams.clearShadowKernel = m_ClearShadowTexture;
            ssprtParams.shadowKernel = lightData.lightType == GPULightType.Point ? m_RaytracingPointShadowSample : m_RaytracingSpotShadowSample;

            // Grab the acceleration structure for the target camera
            ssprtParams.accelerationStructure = RequestAccelerationStructure();
            ssprtParams.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;
            ssprtParams.screenSpaceShadowRT = m_ScreenSpaceShadowsRT;
            ssprtParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            ssprtParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return ssprtParams;
        }

        struct SSSPunctualRayTraceResources
        {
            // Input Buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Intermediate buffers
            public RTHandle directionBuffer;
            public RTHandle rayLengthBuffer;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output buffers
            public RTHandle velocityBuffer;
            public RTHandle distanceBuffer;
            public RTHandle outputShadowBuffer;
        }

        SSSPunctualRayTraceResources PrepareSSSPunctualRayTraceResources(RTHandle velocityBuffer, RTHandle directionBuffer, RTHandle rayLengthBuffer, RTHandle distanceBuffer, RTHandle outputShadowBuffer)
        {
            SSSPunctualRayTraceResources ssprtResources = new SSSPunctualRayTraceResources();

            // Input Buffers
            ssprtResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            ssprtResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();

            // Intermediate buffers
            ssprtResources.directionBuffer = directionBuffer;
            ssprtResources.rayLengthBuffer = rayLengthBuffer;

            // Debug textures
            RayCountManager rayCountManager = GetRayCountManager();
            ssprtResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output buffers
            ssprtResources.velocityBuffer = velocityBuffer;
            ssprtResources.distanceBuffer = distanceBuffer;
            ssprtResources.outputShadowBuffer = outputShadowBuffer;

            return ssprtResources;
        }

        static void ExecuteSSSPunctualRayTrace(CommandBuffer cmd, SSSPunctualRayTraceParameters ssprtParams, SSSPunctualRayTraceResources ssprtResources)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, ssprtParams.ditheredTextureSet);

            // Evaluate the dispatch parameters
            int shadowTileSize = 8;
            int numTilesX = (ssprtParams.texWidth + (shadowTileSize - 1)) / shadowTileSize;
            int numTilesY = (ssprtParams.texHeight + (shadowTileSize - 1)) / shadowTileSize;

            // Clear the integration textures
            cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, ssprtResources.outputShadowBuffer);
            cmd.DispatchCompute(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, numTilesX, numTilesY, ssprtParams.viewCount);

            cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, ssprtResources.velocityBuffer);
            cmd.DispatchCompute(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, numTilesX, numTilesY, ssprtParams.viewCount);

            if (ssprtParams.distanceBasedFiltering)
            {
                cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, ssprtResources.distanceBuffer);
                cmd.DispatchCompute(ssprtParams.screenSpaceShadowCS, ssprtParams.clearShadowKernel, numTilesX, numTilesY, ssprtParams.viewCount);
            }

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingAccelerationStructureName, ssprtParams.accelerationStructure);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(ssprtParams.screenSpaceShadowRT, "VisibilityDXR");

            // Loop through the samples of this frame
            for (int sampleIdx = 0; sampleIdx < ssprtParams.numShadowSamples; ++sampleIdx)
            {
                // Update global constant buffer
                ssprtParams.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIdx;
                ssprtParams.shaderVariablesRayTracingCB._RaytracingNumSamples = ssprtParams.numShadowSamples;
                ConstantBuffer.PushGlobal(cmd, ssprtParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Bind the light & sampling data
                cmd.SetComputeIntParam(ssprtParams.screenSpaceShadowCS, HDShaderIDs._RaytracingTargetAreaLight, ssprtParams.lightIndex);
                cmd.SetComputeFloatParam(ssprtParams.screenSpaceShadowCS, HDShaderIDs._RaytracingLightRadius, ssprtParams.shapeRadius);

                // If this is a spot light, inject the spot angle in radians
                if (ssprtParams.lightType == GPULightType.Spot)
                {
                    float spotAngleRadians = ssprtParams.spotAngle * (float)Math.PI / 180.0f;
                    cmd.SetComputeFloatParam(ssprtParams.screenSpaceShadowCS, HDShaderIDs._RaytracingSpotAngle, spotAngleRadians);
                }

                // Input Buffer
                cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.shadowKernel, HDShaderIDs._DepthTexture, ssprtResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.shadowKernel, HDShaderIDs._NormalBufferTexture, ssprtResources.normalBuffer);

                // Output buffers
                cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.shadowKernel, HDShaderIDs._RaytracingDirectionBuffer, ssprtResources.directionBuffer);
                cmd.SetComputeTextureParam(ssprtParams.screenSpaceShadowCS, ssprtParams.shadowKernel, HDShaderIDs._RayTracingLengthBuffer, ssprtResources.rayLengthBuffer);

                // Generate a new direction
                cmd.DispatchCompute(ssprtParams.screenSpaceShadowCS, ssprtParams.shadowKernel, numTilesX, numTilesY, ssprtParams.viewCount);

                // Define the shader pass to use for the shadow pass
                cmd.SetRayTracingShaderPass(ssprtParams.screenSpaceShadowRT, "VisibilityDXR");

                // Set ray count texture
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RayCountTexture, ssprtResources.rayCountTexture);

                // Input buffers
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._DepthTexture, ssprtResources.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._NormalBufferTexture, ssprtResources.normalBuffer);
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingDirectionBuffer, ssprtResources.directionBuffer);
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RayTracingLengthBuffer, ssprtResources.rayLengthBuffer);

                // Output buffer
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RaytracedShadowIntegration, ssprtResources.outputShadowBuffer);
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._VelocityBuffer, ssprtResources.velocityBuffer);
                cmd.SetRayTracingTextureParam(ssprtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingDistanceBufferRW, ssprtResources.distanceBuffer);

                CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", ssprtParams.semiTransparentShadow);
                cmd.DispatchRays(ssprtParams.screenSpaceShadowRT, ssprtParams.semiTransparentShadow ? m_RayGenSemiTransparentShadowSegmentSingleName : m_RayGenShadowSegmentSingleName, (uint)ssprtParams.texWidth, (uint)ssprtParams.texHeight, (uint)ssprtParams.viewCount);
                CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", false);
            }
        }

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

        void DenoisePunctualScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera,
                                                HDAdditionalLightData additionalLightData, in LightData lightData,
                                                RTHandle velocityBuffer, RTHandle distanceBufferI, RTHandle shadowBuffer)
        {
            // Request the additional temporary buffers we shall be using
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Is the history still valid?
            float historyValidity = EvaluateHistoryValidityPointShadow(hdCamera, lightData, additionalLightData);

            // Evaluate the channel mask
            GetShadowChannelMask(lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);

            HDTemporalFilter temporalFilter = GetTemporalFilter();

            // Only set the distance based denoising buffers if required.
            RTHandle distanceBuffer = null;
            RTHandle shadowHistoryDistanceArray = null;
            RTHandle denoisedDistanceBuffer = null;
            if (additionalLightData.distanceBasedFiltering)
            {
                distanceBuffer = distanceBufferI;
                shadowHistoryDistanceArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowDistanceValidity)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowDistanceValidity, ShadowHistoryDistanceBufferAllocatorFunction, 1); ;
                denoisedDistanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RG1);
            }

            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory, ShadowHistoryBufferAllocatorFunction, 1);
            RTHandle shadowHistoryValidityArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity, ShadowHistoryValidityBufferAllocatorFunction, 1);

            // Apply the temporal denoiser
            temporalFilter.DenoiseBuffer(cmd, hdCamera, shadowBuffer, shadowHistoryArray,
                                shadowHistoryValidityArray,
                                velocityBuffer,
                                intermediateBuffer1,
                                lightData.screenSpaceShadowIndex / 4, m_ShadowChannelMask0,
                                distanceBuffer, shadowHistoryDistanceArray, denoisedDistanceBuffer, m_ShadowChannelMask0,
                                additionalLightData.distanceBasedFiltering, singleChannel: true, historyValidity: historyValidity);


            if (additionalLightData.distanceBasedFiltering)
            {
                // Apply the spatial denoiser
                HDDiffuseShadowDenoiser shadowDenoiser = GetDiffuseShadowDenoiser();
                shadowDenoiser.DenoiseBufferSphere(cmd, hdCamera, intermediateBuffer1, denoisedDistanceBuffer, shadowBuffer, additionalLightData.filterSizeTraced, additionalLightData.transform.position, additionalLightData.shapeRadius);
            }
            else
            {
                HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                simpleDenoiser.DenoiseBufferNoHistory(cmd, hdCamera, intermediateBuffer1, shadowBuffer, additionalLightData.filterSizeTraced, singleChannel: true);
            }

            // Now that we have overriden this history, mark is as used by this light
            hdCamera.PropagateShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType);
        }

        void RenderPunctualScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera
            , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex)
        {
            // Request the intermediate buffers we shall be using
            RTHandle outputShadowBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
            RTHandle velocityBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R1);
            RTHandle distanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RG0);
            RTHandle rayLengthBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);

            // Ray trace for shadow evaluation
            SSSPunctualRayTraceParameters ssprtParams = PrepareSSSPunctualRayTraceParameters(hdCamera, additionalLightData, lightData, lightIndex);
            SSSPunctualRayTraceResources ssprtResources = PrepareSSSPunctualRayTraceResources(velocityBuffer, directionBuffer, rayLengthBuffer, distanceBuffer, outputShadowBuffer);
            ExecuteSSSPunctualRayTrace(cmd, ssprtParams, ssprtResources);

            // If required, denoise the shadow
            if (additionalLightData.filterTracedShadow && ssprtParams.softShadow)
            {
                DenoisePunctualScreenSpaceShadow(cmd, hdCamera, additionalLightData, lightData, velocityBuffer, distanceBuffer, outputShadowBuffer);
            }

            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadowParameters wsssParams = PrepareWriteScreenSpaceShadowParameters(hdCamera, lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale);
            WriteScreenSpaceShadowResources wsssResources = PrepareWriteScreenSpaceShadowResources(outputShadowBuffer);
            ExecuteWriteScreenSpaceShadow(cmd, wsssParams, wsssResources);
        }
    }
}
