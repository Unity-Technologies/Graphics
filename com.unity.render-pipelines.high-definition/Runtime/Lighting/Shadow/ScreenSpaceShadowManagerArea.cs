using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct RTSAreaRayTraceParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public int numSamples;
            public int lightIndex;
            public Matrix4x4 worldToLocalMatrix;
            public float historyValidity;
            public bool filterTracedShadow;
            public int areaShadowSlot;
            public int filterSize;

            // Kernels
            public int areaRaytracingShadowPrepassKernel;
            public int areaRaytracingShadowNewSampleKernel;
            public int areaShadowApplyTAAKernel;
            public int areaUpdateAnalyticHistoryKernel;
            public int areaUpdateShadowHistoryKernel;
            public int areaEstimateNoiseKernel;
            public int areaFirstDenoiseKernel;
            public int areaSecondDenoiseKernel;
            public int areaShadowNoDenoiseKernel;

            // Other parameters
            public RayTracingAccelerationStructure accelerationStructure;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public ComputeShader screenSpaceShadowsCS;
            public ComputeShader screenSpaceShadowsFilterCS;
            public RayTracingShader screenSpaceShadowsRT;
            public Texture2D scramblingTex;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        RTSAreaRayTraceParameters PrepareRTSAreaRayTraceParameters(HDCamera hdCamera, HDAdditionalLightData additionalLightData, LightData lightData, int lightIndex)
        {
            RTSAreaRayTraceParameters rtsartParams = new RTSAreaRayTraceParameters();

            // Set the camera parameters
            rtsartParams.texWidth = hdCamera.actualWidth;
            rtsartParams.texHeight = hdCamera.actualHeight;
            rtsartParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            rtsartParams.numSamples = additionalLightData.numRayTracingSamples;
            rtsartParams.lightIndex = lightIndex;
            // We need to build the world to area light matrix
            rtsartParams.worldToLocalMatrix.SetColumn(0, lightData.right);
            rtsartParams.worldToLocalMatrix.SetColumn(1, lightData.up);
            rtsartParams.worldToLocalMatrix.SetColumn(2, lightData.forward);
            // Compensate the  relative rendering if active
            Vector3 lightPositionWS = lightData.positionRWS;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                lightPositionWS -= hdCamera.camera.transform.position;
            }
            rtsartParams.worldToLocalMatrix.SetColumn(3, lightPositionWS);
            rtsartParams.worldToLocalMatrix.m33 = 1.0f;
            rtsartParams.worldToLocalMatrix = m_WorldToLocalArea.inverse;
            rtsartParams.historyValidity = EvaluateHistoryValidity(hdCamera);
            rtsartParams.filterTracedShadow = additionalLightData.filterTracedShadow;
            rtsartParams.areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
            rtsartParams.filterSize = additionalLightData.filterSizeTraced;

            // Kernels
            rtsartParams.areaRaytracingShadowPrepassKernel = m_AreaRaytracingShadowPrepassKernel;
            rtsartParams.areaRaytracingShadowNewSampleKernel = m_AreaRaytracingShadowNewSampleKernel;
            rtsartParams.areaShadowApplyTAAKernel = m_AreaShadowApplyTAAKernel;
            rtsartParams.areaUpdateAnalyticHistoryKernel = m_AreaUpdateAnalyticHistoryKernel;
            rtsartParams.areaUpdateShadowHistoryKernel = m_AreaUpdateShadowHistoryKernel;
            rtsartParams.areaEstimateNoiseKernel = m_AreaEstimateNoiseKernel;
            rtsartParams.areaFirstDenoiseKernel = m_AreaFirstDenoiseKernel;
            rtsartParams.areaSecondDenoiseKernel = m_AreaSecondDenoiseKernel;
            rtsartParams.areaShadowNoDenoiseKernel = m_AreaShadowNoDenoiseKernel;

            // Other parameters
            // Grab the acceleration structure for the target camera
            rtsartParams.accelerationStructure = RequestAccelerationStructure();
            rtsartParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            rtsartParams.screenSpaceShadowsCS = m_ScreenSpaceShadowsCS;
            rtsartParams.screenSpaceShadowsRT = m_ScreenSpaceShadowsRT;
            rtsartParams.screenSpaceShadowsFilterCS = m_ScreenSpaceShadowsFilterCS;
            rtsartParams.scramblingTex = m_Asset.renderPipelineResources.textures.scramblingTex;
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtsartParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rtsartParams;
        }

        struct RTSAreaRayTraceResources
        {
            // Input Buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle motionVectorsBuffer;
            public RTHandle gbuffer0;
            public RTHandle gbuffer1;
            public RTHandle gbuffer2;
            public RTHandle gbuffer3;
            public RTHandle shadowHistoryArray;
            public RTHandle analyticHistoryArray;

            // Intermediate buffers
            public RTHandle directionBuffer;
            public RTHandle rayLengthBuffer;
            public RTHandle intermediateBufferRGBA1;
            public RTHandle intermediateBufferRG0;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output buffers
            public RTHandle outputShadowTexture;
        }

        RTSAreaRayTraceResources PrepareRTSAreaRayTraceResources(HDCamera hdCamera, RTHandle directionBuffer, RTHandle rayLengthBuffer,
                                                                    RTHandle intermediateBufferRGBA0, RTHandle intermediateBufferRGBA1, RTHandle intermediateBufferRG0,
                                                                    RTHandle shadowHistoryArray, RTHandle analyticHistoryArray)
        {
            RTSAreaRayTraceResources rtsartResources = new RTSAreaRayTraceResources();

            // Input Buffers
            rtsartResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtsartResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtsartResources.motionVectorsBuffer = m_SharedRTManager.GetMotionVectorsBuffer();
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                rtsartResources.gbuffer0 = m_GbufferManager.GetBuffer(0);
                rtsartResources.gbuffer1 = m_GbufferManager.GetBuffer(1);
                rtsartResources.gbuffer2 = m_GbufferManager.GetBuffer(2);
                rtsartResources.gbuffer3 = m_GbufferManager.GetBuffer(3);
            }
            else
            {
                rtsartResources.gbuffer0 = TextureXR.GetBlackTexture();
                rtsartResources.gbuffer1 = TextureXR.GetBlackTexture();
                rtsartResources.gbuffer2 = TextureXR.GetBlackTexture();
                rtsartResources.gbuffer3 = TextureXR.GetBlackTexture();
            }
            rtsartResources.shadowHistoryArray = shadowHistoryArray;
            rtsartResources.analyticHistoryArray = analyticHistoryArray;

            // Intermediate buffers
            rtsartResources.directionBuffer = directionBuffer;
            rtsartResources.rayLengthBuffer = rayLengthBuffer;
            rtsartResources.intermediateBufferRGBA1 = intermediateBufferRGBA1;
            rtsartResources.intermediateBufferRG0 = intermediateBufferRG0;

            // Debug textures
            RayCountManager rayCountManager = GetRayCountManager();
            rtsartResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output texture
            rtsartResources.outputShadowTexture = intermediateBufferRGBA0;

            return rtsartResources;
        }

        static void ExecuteSSSAreaRayTrace(CommandBuffer cmd, RTSAreaRayTraceParameters parameters, RTSAreaRayTraceResources sssartResources)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, parameters.ditheredTextureSet);

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (parameters.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (parameters.texHeight + (areaTileSize - 1)) / areaTileSize;

            // We have noticed from extensive profiling that ray-trace shaders are not as effective for running per-pixel computation. In order to reduce that,
            // we do a first prepass that compute the analytic term and probability and generates the first integration sample

            // Bind the light data
            cmd.SetComputeMatrixParam(parameters.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, parameters.worldToLocalMatrix);
            cmd.SetComputeIntParam(parameters.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, parameters.lightIndex);

            parameters.shaderVariablesRayTracingCB._RaytracingNumSamples = parameters.numSamples;
            ConstantBuffer.PushGlobal(cmd, parameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Bind the input buffers
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[0], sssartResources.gbuffer0);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[1], sssartResources.gbuffer1);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[2], sssartResources.gbuffer2);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[3], sssartResources.gbuffer3);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._StencilTexture, sssartResources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the output buffers
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.outputShadowTexture);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
            cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
            cmd.DispatchCompute(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowPrepassKernel, numTilesX, numTilesY, parameters.viewCount);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RayCountTexture, sssartResources.rayCountTexture);

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, parameters.accelerationStructure);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(parameters.screenSpaceShadowsRT, "VisibilityDXR");

            // Input data
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
            cmd.SetRayTracingIntParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, parameters.lightIndex);

            // Output data
            cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.outputShadowTexture);

            // Evaluate the intersection
            cmd.DispatchRays(parameters.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)parameters.texWidth, (uint)parameters.texHeight, (uint)parameters.viewCount);

            // Let's do the following samples (if any)
            for (int sampleIndex = 1; sampleIndex < parameters.numSamples; ++sampleIndex)
            {
                // Update global Constant Buffer
                parameters.shaderVariablesRayTracingCB._RaytracingNumSamples = parameters.numSamples;
                parameters.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIndex;
                ConstantBuffer.PushGlobal(cmd, parameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Bind the light data
                cmd.SetComputeIntParam(parameters.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, parameters.lightIndex);
                cmd.SetComputeMatrixParam(parameters.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, parameters.worldToLocalMatrix);

                // Input Buffers
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[0], sssartResources.gbuffer0);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[1], sssartResources.gbuffer1);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[2], sssartResources.gbuffer2);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[3], sssartResources.gbuffer3);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._StencilTexture, sssartResources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                // Output buffers
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.DispatchCompute(parameters.screenSpaceShadowsCS, parameters.areaRaytracingShadowNewSampleKernel, numTilesX, numTilesY, parameters.viewCount);

                // Input buffers
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetRayTracingIntParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, parameters.lightIndex);

                // Output buffers
                cmd.SetRayTracingTextureParam(parameters.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.outputShadowTexture);

                // Evaluate the intersection
                cmd.DispatchRays(parameters.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)parameters.texWidth, (uint)parameters.texHeight, (uint)parameters.viewCount);
            }

            if (parameters.filterTracedShadow)
            {
                Vector4 shadowChannelMask0 = new Vector4();
                Vector4 shadowChannelMask1 = new Vector4();
                Vector4 shadowChannelMask2 = new Vector4();
                GetShadowChannelMask(parameters.areaShadowSlot, ScreenSpaceShadowType.Area, ref shadowChannelMask0);
                GetShadowChannelMask(parameters.areaShadowSlot, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask1);
                GetShadowChannelMask(parameters.areaShadowSlot + 1, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask2);

                // Global parameters
                cmd.SetComputeIntParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._RaytracingDenoiseRadius, parameters.filterSize);
                cmd.SetComputeIntParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, parameters.areaShadowSlot / 4);
                cmd.SetComputeVectorParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, shadowChannelMask0);
                cmd.SetComputeVectorParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskSn, shadowChannelMask1);
                cmd.SetComputeVectorParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskUn, shadowChannelMask2);

                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._CameraMotionVectorsTexture, sssartResources.motionVectorsBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._AreaShadowHistory, sssartResources.shadowHistoryArray);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, sssartResources.analyticHistoryArray);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.outputShadowTexture);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeFloatParam(parameters.screenSpaceShadowsFilterCS, HDShaderIDs._HistoryValidity, parameters.historyValidity);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowApplyTAAKernel, numTilesX, numTilesY, parameters.viewCount);
                
                // Update the shadow history buffer
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticHistoryBuffer, sssartResources.analyticHistoryArray);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateAnalyticHistoryKernel, numTilesX, numTilesY, parameters.viewCount);

                // Update the analytic history buffer
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateShadowHistoryKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateShadowHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, sssartResources.shadowHistoryArray);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaUpdateShadowHistoryKernel, numTilesX, numTilesY, parameters.viewCount);

                // Inject parameters for noise estimation
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, HDShaderIDs._ScramblingTexture, parameters.scramblingTex);

                // Noise estimation pre-pass
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.outputShadowTexture);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaEstimateNoiseKernel, numTilesX, numTilesY, parameters.viewCount);

                // Reinject parameters for denoising
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaFirstDenoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaFirstDenoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);

                // First denoising pass
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaFirstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.outputShadowTexture);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaFirstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA1);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaFirstDenoiseKernel, numTilesX, numTilesY, parameters.viewCount);

                // Re-inject parameters for denoising
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaSecondDenoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaSecondDenoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);

                // Second (and final) denoising pass
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaSecondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaSecondDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.outputShadowTexture);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaSecondDenoiseKernel, numTilesX, numTilesY, parameters.viewCount);
            }
            else
            {
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowNoDenoiseKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetComputeTextureParam(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowNoDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.outputShadowTexture);
                cmd.DispatchCompute(parameters.screenSpaceShadowsFilterCS, parameters.areaShadowNoDenoiseKernel, numTilesX, numTilesY, parameters.viewCount);
            }
        }

        void RenderAreaScreenSpaceShadow(CommandBuffer cmd, HDCamera hdCamera
        , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex)
        {
            RTHandle intermediateBufferRG0 = GetRayTracingBuffer(InternalRayTracingBuffers.RG0);
            RTHandle intermediateBufferRGBA0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBufferRGBA1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
            RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
            RTHandle rayLengthBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Distance);

            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistory, ShadowHistoryBufferAllocatorFunction, 1);
            RTHandle analyticHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedShadowHistoryValidity, ShadowHistoryValidityBufferAllocatorFunction, 1);

            RTSAreaRayTraceParameters sssartParams = PrepareRTSAreaRayTraceParameters(hdCamera, additionalLightData, lightData, lightIndex);
            RTSAreaRayTraceResources sssartResources = PrepareRTSAreaRayTraceResources(hdCamera, directionBuffer, rayLengthBuffer,
                                                                                        intermediateBufferRGBA0, intermediateBufferRGBA1, intermediateBufferRG0,
                                                                                        shadowHistoryArray, analyticHistoryArray);
            ExecuteSSSAreaRayTrace(cmd, sssartParams, sssartResources);

            int areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
            // Write the result texture to the screen space shadow buffer
            WriteScreenSpaceShadowParameters wsssParams = PrepareWriteScreenSpaceShadowParameters(hdCamera, areaShadowSlot, ScreenSpaceShadowType.Area);
            WriteScreenSpaceShadowResources wsssResources = PrepareWriteScreenSpaceShadowResources(intermediateBufferRGBA0);
            ExecuteWriteScreenSpaceShadow(cmd, wsssParams, wsssResources);

            // IF we had to filter, then we have to execute this
            if (additionalLightData.filterTracedShadow)
            {
                // Do not forget to update the identification of shadow history usage
                hdCamera.PropagateShadowHistory(additionalLightData, areaShadowSlot, GPULightType.Rectangle);
            }
        }

        class RTShadowAreaPassData
        {
            public RTSAreaRayTraceParameters parameters;
            // Input Buffers
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle shadowHistoryArray;
            public TextureHandle analyticHistoryArray;

            // Intermediate buffers
            public TextureHandle directionBuffer;
            public TextureHandle rayLengthBuffer;
            public TextureHandle intermediateBufferRGBA1;
            public TextureHandle intermediateBufferRG0;

            // Debug textures
            public TextureHandle rayCountTexture;

            // Output buffers
            public TextureHandle outputShadowTexture;
        }

        void RenderAreaScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera
        , in LightData lightData, HDAdditionalLightData additionalLightData, int lightIndex,
            PrepassOutput prepassOutput, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle rayCountTexture, TextureHandle screenSpaceShadowArray)
        {
            // Grab the history buffers for shadows
            RTHandle shadowHistoryArray = RequestShadowHistoryBuffer(hdCamera);
            RTHandle analyticHistoryArray = RequestShadowHistoryValidityBuffer(hdCamera);

            TextureHandle areaShadow;
            using (var builder = renderGraph.AddRenderPass<RTShadowAreaPassData>("Screen Space Shadows Debug", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingAreaLightShadow)))
            {
                passData.parameters = PrepareRTSAreaRayTraceParameters(hdCamera, additionalLightData, lightData, lightIndex);
                // Input Buffers
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);

                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    passData.gbuffer0 = builder.ReadTexture(prepassOutput.gbuffer.mrt[0]);
                    passData.gbuffer1 = builder.ReadTexture(prepassOutput.gbuffer.mrt[1]);
                    passData.gbuffer2 = builder.ReadTexture(prepassOutput.gbuffer.mrt[2]);
                    passData.gbuffer3 = builder.ReadTexture(prepassOutput.gbuffer.mrt[3]);
                }
                else
                {
                    passData.gbuffer0 = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                    passData.gbuffer1 = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                    passData.gbuffer2 = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                    passData.gbuffer3 = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                }

                passData.shadowHistoryArray = builder.ReadTexture(builder.WriteTexture(renderGraph.ImportTexture(shadowHistoryArray)));
                passData.analyticHistoryArray = builder.ReadTexture(builder.WriteTexture(renderGraph.ImportTexture(analyticHistoryArray)));

                // Intermediate buffers
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });
                passData.rayLengthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Ray Length Buffer" });
                passData.intermediateBufferRGBA1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Buffer RGBA1" }); ;
                passData.intermediateBufferRG0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Buffer RG0" });

                // Debug textures
                passData.rayCountTexture = builder.ReadTexture(builder.WriteTexture(rayCountTexture));

                // Output buffers
                passData.outputShadowTexture = builder.ReadTexture(builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Shadow Buffer" })));
                builder.SetRenderFunc(
                (RTShadowAreaPassData data, RenderGraphContext context) =>
                {
                    RTSAreaRayTraceResources resources = new RTSAreaRayTraceResources();
                    // Input Buffers
                    resources.depthStencilBuffer = data.depthStencilBuffer;
                    resources.normalBuffer = data.normalBuffer;
                    resources.motionVectorsBuffer = data.motionVectorsBuffer;
                    resources.gbuffer0 = data.gbuffer0;
                    resources.gbuffer1 = data.gbuffer1;
                    resources.gbuffer2 = data.gbuffer2;
                    resources.gbuffer3 = data.gbuffer3;
                    resources.shadowHistoryArray = data.shadowHistoryArray;
                    resources.analyticHistoryArray = data.analyticHistoryArray;

                    // Intermediate buffers
                    resources.directionBuffer = data.directionBuffer;
                    resources.rayLengthBuffer = data.rayLengthBuffer;
                    resources.intermediateBufferRGBA1 = data.intermediateBufferRGBA1;
                    resources.intermediateBufferRG0 = data.intermediateBufferRG0;

                    // Debug textures
                    resources.rayCountTexture = data.rayCountTexture;

                    // Output buffers
                    resources.outputShadowTexture = data.outputShadowTexture;
                    ExecuteSSSAreaRayTrace(context.cmd, data.parameters, resources);
                });
                areaShadow = passData.outputShadowTexture;
            }

            int areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
            WriteScreenSpaceShadow(renderGraph, hdCamera, areaShadow, screenSpaceShadowArray, areaShadowSlot, ScreenSpaceShadowType.Area);

            if (additionalLightData.filterTracedShadow)
            {
                // Do not forget to update the identification of shadow history usage
                hdCamera.PropagateShadowHistory(additionalLightData, areaShadowSlot, GPULightType.Rectangle);
            }
        }
    }
}
