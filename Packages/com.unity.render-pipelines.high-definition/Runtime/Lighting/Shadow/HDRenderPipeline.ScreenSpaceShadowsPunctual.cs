using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct PunctualShadowProperties
    {
        public GPULightType lightType;
        public bool softShadow;
        public int lightIndex;
        public float lightRadius;
        public float lightConeAngle;
        public float lightSizeX;
        public float lightSizeY;
        public Vector3 lightPosition;
        public int kernelSize;
        public bool distanceBasedDenoiser;
    }

    public partial class HDRenderPipeline
    {
        static float EvaluateHistoryValidityPointShadow(HDCamera hdCamera, LightData lightData, HDAdditionalLightData additionalLightData)
        {
            // We need to set the history as invalid if the light has moved (rotated or translated),
            float historyValidity = 1.0f;
            if (hdCamera.shadowHistoryUsage[lightData.screenSpaceShadowIndex].transform != additionalLightData.transform.localToWorldMatrix
                || !hdCamera.ValidShadowHistory(additionalLightData, lightData.screenSpaceShadowIndex, lightData.lightType))
                historyValidity = 0.0f;

            // We need to check if the camera implied an invalidation
            historyValidity *= EvaluateHistoryValidity(hdCamera);

            return historyValidity;
        }

        TextureHandle DenoisePunctualScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            HDAdditionalLightData additionalLightData, in LightData lightData, PunctualShadowProperties properties,
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

            // Temporal denoising
            temporalFilterResult = temporalFilter.DenoiseBuffer(renderGraph, hdCamera,
                depthBuffer, normalBuffer, motionVetorsBuffer, historyValidityBuffer,
                noisyBuffer, shadowHistoryArray,
                distanceBuffer, shadowHistoryDistanceArray,
                velocityBuffer,
                shadowHistoryValidityArray,
                lightData.screenSpaceShadowIndex / 4, m_ShadowChannelMask0, m_ShadowChannelMask0,
                additionalLightData.distanceBasedFiltering, true, historyValidity);

            // Spatial denoising
            HDDiffuseShadowDenoiser shadowDenoiser = GetDiffuseShadowDenoiser();
            TextureHandle denoisedBuffer = shadowDenoiser.DenoiseBufferSphere(renderGraph, hdCamera,
                depthBuffer, normalBuffer,
                temporalFilterResult.outputSignal, temporalFilterResult.outputSignalDistance,
                properties);

            // Now that we have overridden this history, mark is as used by this light
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
            public bool distanceBasedFiltering;
            public int numShadowSamples;
            public bool semiTransparentShadow;
            public GPULightType lightType;
            public PunctualShadowProperties properties;

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

            PunctualShadowProperties props = new PunctualShadowProperties();
            props.lightType = lightData.lightType;
            props.lightIndex = lightIndex;
            props.softShadow = additionalLightData.legacyLight.shapeRadius > 0.0 ? true : false;
            props.lightRadius = additionalLightData.legacyLight.shapeRadius;
            props.lightPosition = additionalLightData.transform.position;
            props.kernelSize = additionalLightData.filterSizeTraced;
            props.lightConeAngle = additionalLightData.legacyLight.spotAngle * Mathf.PI / 180.0f;
            props.distanceBasedDenoiser = additionalLightData.distanceBasedFiltering;

            switch (lightData.lightType)
            {
                case (GPULightType.ProjectorPyramid):
                {
                    // Scale up one of the pyramind light angles based on aspect ratio
                    // We reuse _RaytracingLightSizeX and _RaytracingLightSizeY for the pyramid angles here
                    if (additionalLightData.legacyLight.innerSpotAngle < additionalLightData.legacyLight.spotAngle)
                    {
                        float tanInnerSpotAngle = Mathf.Tan(additionalLightData.legacyLight.innerSpotAngle * Mathf.PI / 360f);
                        float tanSpotAngle = Mathf.Tan(additionalLightData.legacyLight.spotAngle * Mathf.PI / 360f);
                        float aspectRatio = tanInnerSpotAngle / tanSpotAngle;

                        props.lightSizeX = props.lightConeAngle;
                        props.lightSizeY = 2.0f * Mathf.Atan(tanSpotAngle / aspectRatio);
                    }
                    else
                    {
                        props.lightSizeX = additionalLightData.legacyLight.innerSpotAngle * Mathf.Deg2Rad;
                        props.lightSizeY = props.lightConeAngle;
                    }
                }
                break;
                default:
                {
                    props.lightSizeX = additionalLightData.legacyLight.areaSize.x;
                    props.lightSizeY = additionalLightData.legacyLight.areaSize.y;
                }
                break;
            }

            using (var builder = renderGraph.AddUnsafePass<RTSPunctualTracePassData>("Punctual RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingLightShadow)))
            {
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // If the surface is infinitively small, we force it to one sample.
                passData.numShadowSamples = props.softShadow ? additionalLightData.numRayTracingSamples : 1;
                passData.distanceBasedFiltering = additionalLightData.distanceBasedFiltering;
                passData.semiTransparentShadow = additionalLightData.semiTransparentShadow;
                passData.lightType = lightData.lightType;
                passData.properties = props;

                // Kernels
                passData.clearShadowKernel = m_ClearShadowTexture;
                switch (lightData.lightType)
                {
                    case GPULightType.Point:
                        passData.shadowKernel = m_RaytracingPointShadowSample; break;
                    case GPULightType.Spot: // Cone
                        passData.shadowKernel = m_RaytracingSpotShadowSample; break;
                    case GPULightType.ProjectorPyramid:
                        passData.shadowKernel = m_RaytracingProjectorPyramidShadowSample; break;
                    case GPULightType.ProjectorBox:
                        passData.shadowKernel = m_RaytracingProjectorBoxShadowSample; break;
                    default:
                        passData.shadowKernel = m_RaytracingSpotShadowSample; break;
                }

                // Grab the acceleration structure for the target camera
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;
                passData.screenSpaceShadowRT = m_ScreenSpaceShadowsRT;
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                // Input Buffer
                passData.depthStencilBuffer = depthBuffer;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });
                passData.rayLengthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Ray Length Buffer" });

                // Debug buffers
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);

                // Output Buffers
                passData.velocityBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8_SNorm, enableRandomWrite = true, name = "Velocity Buffer" });
                builder.UseTexture(passData.velocityBuffer, AccessFlags.ReadWrite);
                passData.distanceBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" });
                builder.UseTexture(passData.distanceBuffer, AccessFlags.ReadWrite);
                passData.outputShadowBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "RT Sphere Shadow" });
                builder.UseTexture(passData.outputShadowBuffer, AccessFlags.ReadWrite);

                builder.SetRenderFunc(
                    (RTSPunctualTracePassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // Clear the integration textures
                        natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                        natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.velocityBuffer);
                        natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        if (data.distanceBasedFiltering)
                        {
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.distanceBuffer);
                            natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);
                        }

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.screenSpaceShadowRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Define the shader pass to use for the reflection pass
                        natCmd.SetRayTracingShaderPass(data.screenSpaceShadowRT, "VisibilityDXR");


                        if (data.lightType == GPULightType.ProjectorBox ||
                            data.lightType == GPULightType.ProjectorPyramid)
                        {
                            natCmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingLightSizeX, data.properties.lightSizeX);
                            natCmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingLightSizeY, data.properties.lightSizeY);
                        }

                        // Bind the light & sampling data
                        natCmd.SetComputeIntParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingTargetLight, data.properties.lightIndex);
                        natCmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingLightRadius, data.properties.lightRadius);
                        if (data.lightType == GPULightType.Spot)
                        {
                            natCmd.SetComputeFloatParam(data.screenSpaceShadowCS, HDShaderIDs._RaytracingLightAngle, data.properties.lightConeAngle);
                        }

                        // Loop through the samples of this frame
                        for (int sampleIdx = 0; sampleIdx < data.numShadowSamples; ++sampleIdx)
                        {
                            // Update global constant buffer
                            data.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIdx;
                            data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.numShadowSamples;
                            ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                            // Input Buffer
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                            // Output buffers
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.shadowKernel, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);

                            // Generate a new direction
                            natCmd.DispatchCompute(data.screenSpaceShadowCS, data.shadowKernel, numTilesX, numTilesY, data.viewCount);

                            // Define the shader pass to use for the shadow pass
                            natCmd.SetRayTracingShaderPass(data.screenSpaceShadowRT, "VisibilityDXR");

                            // Set ray count texture
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                            // Input buffers
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);

                            // Output buffer
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._VelocityBuffer, data.velocityBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDistanceBufferRW, data.distanceBuffer);

                            CoreUtils.SetKeyword(natCmd, "TRANSPARENT_COLOR_SHADOW", data.semiTransparentShadow);

                            natCmd.DispatchRays(data.screenSpaceShadowRT, data.semiTransparentShadow ? m_RayGenSemiTransparentShadowSegmentSingleName : m_RayGenShadowSegmentSingleName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount, null);
                            CoreUtils.SetKeyword(natCmd, "TRANSPARENT_COLOR_SHADOW", false);
                        }
                    });
                pointShadowBuffer = passData.outputShadowBuffer;
                velocityBuffer = passData.velocityBuffer;
                distanceBuffer = passData.distanceBuffer;
            }

            // If required, denoise the shadow
            if (additionalLightData.filterTracedShadow && props.softShadow)
            {
                pointShadowBuffer = DenoisePunctualScreenSpaceShadow(renderGraph, hdCamera,
                    additionalLightData, lightData, props,
                    depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer,
                    pointShadowBuffer, velocityBuffer, distanceBuffer);
            }

            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadow(renderGraph, hdCamera, pointShadowBuffer, screenSpaceShadowArray, lightData.screenSpaceShadowIndex, ScreenSpaceShadowType.GrayScale);
        }
    }
}
