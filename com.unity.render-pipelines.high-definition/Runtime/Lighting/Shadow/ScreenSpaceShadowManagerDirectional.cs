using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        MaterialPropertyBlock directionalShadowPB = new MaterialPropertyBlock();
        struct RTShadowDirectionalTraceParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public bool softShadow;
            public int numShadowSamples;
            public bool colorShadow;
            public float maxShadowLength;

            // Kernels
            public int clearShadowKernel;
            public int directionalShadowSample;

            // Other parameters
            public RayTracingShader screenSpaceShadowRT;
            public ComputeShader screenSpaceShadowCS;
            public RayTracingAccelerationStructure accelerationStructure;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        RTShadowDirectionalTraceParameters PrepareRTShadowDirectionalTraceParameters(HDCamera hdCamera, HDAdditionalLightData additionalLightData)
        {
            RTShadowDirectionalTraceParameters rtsdtParams = new RTShadowDirectionalTraceParameters();
            RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Set the camera parameters
            rtsdtParams.texWidth = hdCamera.actualWidth;
            rtsdtParams.texHeight = hdCamera.actualHeight;
            rtsdtParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            rtsdtParams.softShadow = additionalLightData.angularDiameter > 0.0 ? true : false;
            // If the surface is infinitively small, we force it to one sample.
            rtsdtParams.numShadowSamples = rtsdtParams.softShadow ? additionalLightData.numRayTracingSamples : 1;
            rtsdtParams.colorShadow = m_CurrentSunLightAdditionalLightData.colorShadow;
            rtsdtParams.maxShadowLength = rayTracingSettings.directionalShadowRayLength.value;

            // Kernels
            rtsdtParams.clearShadowKernel = m_ClearShadowTexture;
            rtsdtParams.directionalShadowSample = m_RaytracingDirectionalShadowSample;

            // Grab the acceleration structure for the target camera
            rtsdtParams.accelerationStructure = RequestAccelerationStructure();
            rtsdtParams.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;
            rtsdtParams.screenSpaceShadowRT = m_ScreenSpaceShadowsRT;
            rtsdtParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtsdtParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rtsdtParams;
        }

        struct RTShadowDirectionalTraceResources
        {
            // Input Buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Intermediate buffers
            public RTHandle directionBuffer;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output buffers
            public RTHandle velocityBuffer;
            public RTHandle distanceBuffer;
            public RTHandle outputShadowBuffer;
        }

        RTShadowDirectionalTraceResources PrepareSSSDirectionalTraceResources(RTHandle velocityBuffer, RTHandle directionBuffer, RTHandle distanceBuffer, RTHandle outputShadowBuffer)
        {
            RTShadowDirectionalTraceResources rtsdtResources = new RTShadowDirectionalTraceResources();

            // Input Buffers
            rtsdtResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtsdtResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();

            // Intermediate buffers
            rtsdtResources.directionBuffer = directionBuffer;

            // Debug textures
            RayCountManager rayCountManager = GetRayCountManager();
            rtsdtResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output buffers
            rtsdtResources.velocityBuffer = velocityBuffer;
            rtsdtResources.distanceBuffer = distanceBuffer;
            rtsdtResources.outputShadowBuffer = outputShadowBuffer;

            return rtsdtResources;
        }

        static void ExecuteSSSDirectionalTrace(CommandBuffer cmd, RTShadowDirectionalTraceParameters rtsdtParams, RTShadowDirectionalTraceResources rtsdtResources)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtsdtParams.ditheredTextureSet);

            // Evaluate the dispatch parameters
            int shadowTileSize = 8;
            int numTilesX = (rtsdtParams.texWidth + (shadowTileSize - 1)) / shadowTileSize;
            int numTilesY = (rtsdtParams.texHeight + (shadowTileSize - 1)) / shadowTileSize;

            // Clear the integration texture
            cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, rtsdtResources.outputShadowBuffer);
            cmd.DispatchCompute(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, numTilesX, numTilesY, rtsdtParams.viewCount);

            cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, rtsdtResources.velocityBuffer);
            cmd.DispatchCompute(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, numTilesX, numTilesY, rtsdtParams.viewCount);

            cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, rtsdtResources.distanceBuffer);
            cmd.DispatchCompute(rtsdtParams.screenSpaceShadowCS, rtsdtParams.clearShadowKernel, numTilesX, numTilesY, rtsdtParams.viewCount);

            // Grab and bind the acceleration structure for the target camera
            cmd.SetRayTracingAccelerationStructure(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingAccelerationStructureName, rtsdtParams.accelerationStructure);

            // Make sure the right closest hit/any hit will be triggered by using the right multi compile
            CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", rtsdtParams.colorShadow);

            // Define which ray generation shaders we shall be using
            string directionaLightShadowShader = rtsdtParams.colorShadow ? m_RayGenDirectionalColorShadowSingleName : m_RayGenDirectionalShadowSingleName;

            // Loop through the samples of this frame
            for (int sampleIdx = 0; sampleIdx < rtsdtParams.numShadowSamples; ++sampleIdx)
            {
                // Update global Constant Buffer
                rtsdtParams.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIdx;
                rtsdtParams.shaderVariablesRayTracingCB._RaytracingNumSamples = rtsdtParams.numShadowSamples;
                ConstantBuffer.PushGlobal(cmd, rtsdtParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Input Buffer
                cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.directionalShadowSample, HDShaderIDs._DepthTexture, rtsdtResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.directionalShadowSample, HDShaderIDs._NormalBufferTexture, rtsdtResources.normalBuffer);

                // Output buffer
                cmd.SetComputeTextureParam(rtsdtParams.screenSpaceShadowCS, rtsdtParams.directionalShadowSample, HDShaderIDs._RaytracingDirectionBuffer, rtsdtResources.directionBuffer);

                // Generate a new direction
                cmd.DispatchCompute(rtsdtParams.screenSpaceShadowCS, rtsdtParams.directionalShadowSample, numTilesX, numTilesY, rtsdtParams.viewCount);

                // Define the shader pass to use for the shadow pass
                cmd.SetRayTracingShaderPass(rtsdtParams.screenSpaceShadowRT, "VisibilityDXR");

                // Input Uniforms
                cmd.SetRayTracingFloatParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._DirectionalMaxRayLength, rtsdtParams.maxShadowLength);

                // Set ray count texture
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._RayCountTexture, rtsdtResources.rayCountTexture);

                // Input buffers
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._DepthTexture, rtsdtResources.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._NormalBufferTexture, rtsdtResources.normalBuffer);
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingDirectionBuffer, rtsdtResources.directionBuffer);

                // Output buffer
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, rtsdtParams.colorShadow ? HDShaderIDs._RaytracedColorShadowIntegration : HDShaderIDs._RaytracedShadowIntegration, rtsdtResources.outputShadowBuffer);
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._VelocityBuffer, rtsdtResources.velocityBuffer);
                cmd.SetRayTracingTextureParam(rtsdtParams.screenSpaceShadowRT, HDShaderIDs._RaytracingDistanceBufferRW, rtsdtResources.distanceBuffer);

                // Evaluate the visibility
                cmd.DispatchRays(rtsdtParams.screenSpaceShadowRT, directionaLightShadowShader, (uint)rtsdtParams.texWidth, (uint)rtsdtParams.texHeight, (uint)rtsdtParams.viewCount);
            }

            // Now that we are done with the ray tracing bit, disable the multi compile that was potentially enabled
            CoreUtils.SetKeyword(cmd, "TRANSPARENT_COLOR_SHADOW", false);
        }

        static float EvaluateHistoryValidityDirectionalShadow(HDCamera hdCamera, int dirShadowIndex, HDAdditionalLightData additionalLightData)
        {
            // We need to set the history as invalid if the directional light has rotated
            float historyValidity = 1.0f;
            if (additionalLightData.previousTransform.rotation != additionalLightData.transform.localToWorldMatrix.rotation
                || !hdCamera.ValidShadowHistory(additionalLightData, dirShadowIndex, GPULightType.Directional))
                historyValidity = 0.0f;

#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                historyValidity = 0.0f;
            else
#endif
                // We need to check if something invalidated the history buffers
                historyValidity *= EvaluateHistoryValidity(hdCamera);

            return historyValidity;
        }

        void DenoiseDirectionalScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera, RTHandle velocityBuffer, RTHandle distanceBuffer, RTHandle inoutBuffer)
        {
            RTHandle intermediateBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
            RTHandle intermediateDistanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RG0);
            // Is the history still valid?
            int dirShadowIndex = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex & (int)LightDefinitions.s_ScreenSpaceShadowIndexMask;
            float historyValidity = EvaluateHistoryValidityDirectionalShadow(hdCamera, dirShadowIndex, m_CurrentSunLightAdditionalLightData);

            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = RequestShadowHistoryBuffer(hdCamera);
            RTHandle shadowHistoryValidityArray = RequestShadowHistoryValidityBuffer(hdCamera);
            RTHandle shadowHistoryDistanceArray = RequestShadowHistoryDistanceBuffer(hdCamera);

            // Grab the slot of the directional light (given that it may be a color shadow, we need to use the mask to get the actual slot index)
            GetShadowChannelMask(dirShadowIndex, m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask0);
            GetShadowChannelMask(dirShadowIndex, ScreenSpaceShadowType.GrayScale, ref m_ShadowChannelMask1);

            // Apply the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();
            temporalFilter.DenoiseBuffer(cmd, hdCamera, inoutBuffer, shadowHistoryArray,
                                                        shadowHistoryValidityArray,
                                                        velocityBuffer,
                                                        intermediateBuffer,
                                                        dirShadowIndex / 4, m_ShadowChannelMask0,
                                                        distanceBuffer, shadowHistoryDistanceArray, intermediateDistanceBuffer, m_ShadowChannelMask1,
                                                        true, singleChannel: !m_CurrentSunLightAdditionalLightData.colorShadow, historyValidity: historyValidity);

            // Apply the spatial denoiser
            HDDiffuseShadowDenoiser shadowDenoiser = GetDiffuseShadowDenoiser();
            shadowDenoiser.DenoiseBufferDirectional(cmd, hdCamera, intermediateBuffer, intermediateDistanceBuffer, inoutBuffer, m_CurrentSunLightAdditionalLightData.filterSizeTraced,  m_CurrentSunLightAdditionalLightData.angularDiameter * 0.5f, singleChannel: !m_CurrentSunLightAdditionalLightData.colorShadow);

            // Now that we have overriden this history, mark is as used by this light
            hdCamera.PropagateShadowHistory(m_CurrentSunLightAdditionalLightData, dirShadowIndex, GPULightType.Directional);
        }

        void RenderRayTracedDirectionalScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Request the intermediate buffers we shall be using
            RTHandle outputShadowBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
            RTHandle velocityBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R1);
            RTHandle distanceBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);

            // Ray trace for shadow evaluation
            RTShadowDirectionalTraceParameters rtsdtParams = PrepareRTShadowDirectionalTraceParameters(hdCamera, m_CurrentSunLightAdditionalLightData);
            RTShadowDirectionalTraceResources rtsdtResources = PrepareSSSDirectionalTraceResources(velocityBuffer, directionBuffer, distanceBuffer, outputShadowBuffer);
            ExecuteSSSDirectionalTrace(cmd, rtsdtParams, rtsdtResources);

            // If required, denoise the shadow
            if (m_CurrentSunLightAdditionalLightData.filterTracedShadow && rtsdtParams.softShadow)
            {
                DenoiseDirectionalScreenSpaceShadow(cmd, hdCamera, velocityBuffer, distanceBuffer, outputShadowBuffer);
            }

            // Write the result texture to the screen space shadow buffer
            int dirShadowIndex = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex & (int)LightDefinitions.s_ScreenSpaceShadowIndexMask;
            WriteScreenSpaceShadowParameters wsssParams = PrepareWriteScreenSpaceShadowParameters(hdCamera, dirShadowIndex, m_CurrentSunLightAdditionalLightData.colorShadow ? ScreenSpaceShadowType.Color : ScreenSpaceShadowType.GrayScale);
            WriteScreenSpaceShadowResources wsssResources = PrepareWriteScreenSpaceShadowResources(outputShadowBuffer);
            ExecuteWriteScreenSpaceShadow(cmd, wsssParams, wsssResources);
        }

        struct SSShadowDirectionalParameters
        {
            public int depthSlice;
        }

        SSShadowDirectionalParameters PrepareSSShadowDirectionalParameters()
        {
            SSShadowDirectionalParameters sssdParams = new SSShadowDirectionalParameters();
            sssdParams.depthSlice = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex;
            return sssdParams;
        }

        static void ExecuteSSShadowDirectional(CommandBuffer cmd, SSShadowDirectionalParameters sssdParams, MaterialPropertyBlock mpb, RTHandle normalBuffer, RTHandle textureArray)
        {
            // If it is screen space but not ray traced, then we can rely on the shadow map
            // WARNING: This pattern only works because we can only have one directional and the directional shadow is evaluated first.
            CoreUtils.SetRenderTarget(cmd, textureArray, depthSlice: sssdParams.depthSlice);
            mpb.SetTexture(HDShaderIDs._NormalBufferTexture, normalBuffer);
            HDUtils.DrawFullScreen(cmd, s_ScreenSpaceShadowsMat, textureArray, mpb);
        }

        void RenderDirectionalLightScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Should we be executing anything really?
            bool screenSpaceShadowRequired = m_CurrentSunLightAdditionalLightData != null && m_CurrentSunLightAdditionalLightData.WillRenderScreenSpaceShadow();

            // Render directional screen space shadow if required
            if (screenSpaceShadowRequired)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
                {
                    bool rayTracedDirectionalRequired = m_CurrentSunLightAdditionalLightData.WillRenderRayTracedShadow();
                    // If the shadow is flagged as ray traced, we need to evaluate it completely
                    if (rayTracedDirectionalRequired)
                        RenderRayTracedDirectionalScreenSpaceShadow(cmd, hdCamera);
                    else
                    {
                        SSShadowDirectionalParameters sssdParams = PrepareSSShadowDirectionalParameters();
                        ExecuteSSShadowDirectional(cmd, sssdParams, directionalShadowPB, m_SharedRTManager.GetNormalBuffer(), m_ScreenSpaceShadowTextureArray);
                    }
                }
            }
        }
    }
}
