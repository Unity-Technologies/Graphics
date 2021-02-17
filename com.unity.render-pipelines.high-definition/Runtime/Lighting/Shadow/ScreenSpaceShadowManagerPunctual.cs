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
    }
}
