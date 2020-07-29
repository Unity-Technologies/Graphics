using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct SSSAreaRayTraceParameters
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

        SSSAreaRayTraceParameters PrepareSSSAreaRayTraceParameters(HDCamera hdCamera, HDAdditionalLightData additionalLightData, LightData lightData, int lightIndex)
        {
            SSSAreaRayTraceParameters sssartParams = new SSSAreaRayTraceParameters();

            // Set the camera parameters
            sssartParams.texWidth = hdCamera.actualWidth;
            sssartParams.texHeight = hdCamera.actualHeight;
            sssartParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            sssartParams.numSamples = additionalLightData.numRayTracingSamples;
            sssartParams.lightIndex = lightIndex;
            // We need to build the world to area light matrix
            sssartParams.worldToLocalMatrix.SetColumn(0, lightData.right);
            sssartParams.worldToLocalMatrix.SetColumn(1, lightData.up);
            sssartParams.worldToLocalMatrix.SetColumn(2, lightData.forward);
            // Compensate the  relative rendering if active
            Vector3 lightPositionWS = lightData.positionRWS;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                lightPositionWS += hdCamera.camera.transform.position;
            }
            sssartParams.worldToLocalMatrix.SetColumn(3, lightPositionWS);
            sssartParams.worldToLocalMatrix.m33 = 1.0f;
            sssartParams.worldToLocalMatrix = m_WorldToLocalArea.inverse;
            sssartParams.historyValidity = EvaluateHistoryValidity(hdCamera);
            sssartParams.filterTracedShadow = additionalLightData.filterTracedShadow;
            sssartParams.areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
            sssartParams.filterSize = additionalLightData.filterSizeTraced;

            // Kernels
            sssartParams.areaRaytracingShadowPrepassKernel = m_AreaRaytracingShadowPrepassKernel;
            sssartParams.areaRaytracingShadowNewSampleKernel = m_AreaRaytracingShadowNewSampleKernel;
            sssartParams.areaShadowApplyTAAKernel = m_AreaShadowApplyTAAKernel;
            sssartParams.areaUpdateAnalyticHistoryKernel = m_AreaUpdateAnalyticHistoryKernel;
            sssartParams.areaUpdateShadowHistoryKernel = m_AreaUpdateShadowHistoryKernel;
            sssartParams.areaEstimateNoiseKernel = m_AreaEstimateNoiseKernel;
            sssartParams.areaFirstDenoiseKernel = m_AreaFirstDenoiseKernel;
            sssartParams.areaSecondDenoiseKernel = m_AreaSecondDenoiseKernel;
            sssartParams.areaShadowNoDenoiseKernel = m_AreaShadowNoDenoiseKernel;

            // Other parameters
            // Grab the acceleration structure for the target camera
            sssartParams.accelerationStructure = RequestAccelerationStructure();
            sssartParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            sssartParams.screenSpaceShadowsCS = m_ScreenSpaceShadowsCS;
            sssartParams.screenSpaceShadowsRT = m_ScreenSpaceShadowsRT;
            sssartParams.screenSpaceShadowsFilterCS = m_ScreenSpaceShadowsFilterCS;
            sssartParams.scramblingTex = m_Asset.renderPipelineResources.textures.scramblingTex;
            BlueNoise blueNoise = GetBlueNoiseManager();
            sssartParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return sssartParams;
        }

        struct SSSAreaRayTraceResources
        {
            // Input Buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public ComputeBuffer lightData;
            public RTHandle gbuffer0;
            public RTHandle gbuffer1;
            public RTHandle gbuffer2;
            public RTHandle gbuffer3;
            public RTHandle cookieAtlasTexture;
            public RTHandle shadowHistoryArray;
            public RTHandle analyticHistoryArray;

            // Intermediate buffers
            public RTHandle directionBuffer;
            public RTHandle rayLengthBuffer;
            public RTHandle intermediateBufferRGBA0;
            public RTHandle intermediateBufferRGBA1;
            public RTHandle intermediateBufferRG0;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output buffers
            public RTHandle screenSpaceShadowTextureArray;
        }

        SSSAreaRayTraceResources PrepareSSSAreaRayTraceResources(HDCamera hdCamera, RTHandle directionBuffer, RTHandle rayLengthBuffer,
                                                                    RTHandle intermediateBufferRGBA0, RTHandle intermediateBufferRGBA1, RTHandle intermediateBufferRG0,
                                                                    RTHandle shadowHistoryArray, RTHandle analyticHistoryArray)
        {
            SSSAreaRayTraceResources sssartResources = new SSSAreaRayTraceResources();

            // Input Buffers
            sssartResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            sssartResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            sssartResources.lightData = m_LightLoopLightData.lightData;
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                sssartResources.gbuffer0 = m_GbufferManager.GetBuffer(0);
                sssartResources.gbuffer1 = m_GbufferManager.GetBuffer(1);
                sssartResources.gbuffer2 = m_GbufferManager.GetBuffer(2);
                sssartResources.gbuffer3 = m_GbufferManager.GetBuffer(3);
            }
            else
            {
                sssartResources.gbuffer0 = TextureXR.GetBlackTexture();
                sssartResources.gbuffer1 = TextureXR.GetBlackTexture();
                sssartResources.gbuffer2 = TextureXR.GetBlackTexture();
                sssartResources.gbuffer3 = TextureXR.GetBlackTexture();
            }
            sssartResources.cookieAtlasTexture = m_TextureCaches.lightCookieManager.atlasTexture;
            sssartResources.shadowHistoryArray = shadowHistoryArray;
            sssartResources.analyticHistoryArray = analyticHistoryArray;

            // Intermediate buffers
            sssartResources.directionBuffer = directionBuffer;
            sssartResources.rayLengthBuffer = rayLengthBuffer;
            sssartResources.intermediateBufferRGBA0 = intermediateBufferRGBA0;
            sssartResources.intermediateBufferRGBA1 = intermediateBufferRGBA1;
            sssartResources.intermediateBufferRG0 = intermediateBufferRG0;

            // Debug textures
            RayCountManager rayCountManager = GetRayCountManager();
            sssartResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output buffers
            sssartResources.screenSpaceShadowTextureArray = m_ScreenSpaceShadowTextureArray;

            return sssartResources;
        }

        static void ExecuteSSSAreaRayTrace(CommandBuffer cmd, SSSAreaRayTraceParameters sssartParams, SSSAreaRayTraceResources sssartResources)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, sssartParams.ditheredTextureSet);

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (sssartParams.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (sssartParams.texHeight + (areaTileSize - 1)) / areaTileSize;

            // We have noticed from extensive profiling that ray-trace shaders are not as effective for running per-pixel computation. In order to reduce that,
            // we do a first prepass that compute the analytic term and probability and generates the first integration sample

            // Bind the light data
            cmd.SetComputeBufferParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._LightDatas, sssartResources.lightData);
            cmd.SetComputeMatrixParam(sssartParams.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, sssartParams.worldToLocalMatrix);
            cmd.SetComputeIntParam(sssartParams.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, sssartParams.lightIndex);

            sssartParams.shaderVariablesRayTracingCB._RaytracingNumSamples = sssartParams.numSamples;
            ConstantBuffer.PushGlobal(cmd, sssartParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Bind the input buffers
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[0], sssartResources.gbuffer0);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[1], sssartResources.gbuffer1);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[2], sssartResources.gbuffer2);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[3], sssartResources.gbuffer3);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._CookieAtlas, sssartResources.cookieAtlasTexture);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._StencilTexture, sssartResources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the output buffers
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.intermediateBufferRGBA0);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
            cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
            cmd.DispatchCompute(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowPrepassKernel, numTilesX, numTilesY, sssartParams.viewCount);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RayCountTexture, sssartResources.rayCountTexture);

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, sssartParams.accelerationStructure);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(sssartParams.screenSpaceShadowsRT, "VisibilityDXR");

            // Input data
            cmd.SetRayTracingBufferParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._LightDatas, sssartResources.lightData);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
            cmd.SetRayTracingIntParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, sssartParams.lightIndex);

            // Output data
            cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.intermediateBufferRGBA0);

            // Evaluate the intersection
            cmd.DispatchRays(sssartParams.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)sssartParams.texWidth, (uint)sssartParams.texHeight, (uint)sssartParams.viewCount);

            // Let's do the following samples (if any)
            for (int sampleIndex = 1; sampleIndex < sssartParams.numSamples; ++sampleIndex)
            {
                // Update global Constant Buffer
                sssartParams.shaderVariablesRayTracingCB._RaytracingNumSamples = sssartParams.numSamples;
                sssartParams.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIndex;
                ConstantBuffer.PushGlobal(cmd, sssartParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Bind the light data
                cmd.SetComputeBufferParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._LightDatas, sssartResources.lightData);
                cmd.SetComputeIntParam(sssartParams.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetAreaLight, sssartParams.lightIndex);
                cmd.SetComputeMatrixParam(sssartParams.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, sssartParams.worldToLocalMatrix);

                // Input Buffers
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[0], sssartResources.gbuffer0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[1], sssartResources.gbuffer1);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[2], sssartResources.gbuffer2);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[3], sssartResources.gbuffer3);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._CookieAtlas, sssartResources.cookieAtlasTexture);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._StencilTexture, sssartResources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                // Output buffers
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsCS, sssartParams.areaRaytracingShadowNewSampleKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Input buffers
                cmd.SetRayTracingBufferParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._LightDatas, sssartResources.lightData);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, sssartResources.intermediateBufferRGBA1);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, sssartResources.directionBuffer);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, sssartResources.rayLengthBuffer);
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetRayTracingIntParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetAreaLight, sssartParams.lightIndex);

                // Output buffers
                cmd.SetRayTracingTextureParam(sssartParams.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, sssartResources.intermediateBufferRGBA0);

                // Evaluate the intersection
                cmd.DispatchRays(sssartParams.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)sssartParams.texWidth, (uint)sssartParams.texHeight, (uint)sssartParams.viewCount);
            }

            if (sssartParams.filterTracedShadow)
            {
                Vector4 shadowChannelMask0 = new Vector4();
                Vector4 shadowChannelMask1 = new Vector4();
                Vector4 shadowChannelMask2 = new Vector4();
                GetShadowChannelMask(sssartParams.areaShadowSlot, ScreenSpaceShadowType.Area, ref shadowChannelMask0);
                GetShadowChannelMask(sssartParams.areaShadowSlot, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask1);
                GetShadowChannelMask(sssartParams.areaShadowSlot + 1, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask2);

                // Global parameters
                cmd.SetComputeIntParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._RaytracingDenoiseRadius, sssartParams.filterSize);
                cmd.SetComputeIntParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, sssartParams.areaShadowSlot / 4);
                cmd.SetComputeVectorParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, shadowChannelMask0);
                cmd.SetComputeVectorParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskSn, shadowChannelMask1);
                cmd.SetComputeVectorParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskUn, shadowChannelMask2);

                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._AreaShadowHistory, sssartResources.shadowHistoryArray);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, sssartResources.analyticHistoryArray);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeFloatParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._HistoryValidity, sssartParams.historyValidity);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowApplyTAAKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Update the shadow history buffer
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticProbBuffer, sssartResources.intermediateBufferRG0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticHistoryBuffer, sssartResources.analyticHistoryArray);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateAnalyticHistoryKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Update the analytic history buffer
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateShadowHistoryKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateShadowHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, sssartResources.shadowHistoryArray);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaUpdateShadowHistoryKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Inject parameters for noise estimation
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, HDShaderIDs._ScramblingTexture, sssartParams.scramblingTex);

                // Noise estimation pre-pass
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA0);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaEstimateNoiseKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Reinject parameters for denoising
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, sssartResources.screenSpaceShadowTextureArray);

                // First denoising pass
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA1);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaFirstDenoiseKernel, numTilesX, numTilesY, sssartParams.viewCount);

                // Re-inject parameters for denoising
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaSecondDenoiseKernel, HDShaderIDs._DepthTexture, sssartResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaSecondDenoiseKernel, HDShaderIDs._NormalBufferTexture, sssartResources.normalBuffer);

                // Second (and final) denoising pass
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaSecondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaSecondDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, sssartResources.intermediateBufferRGBA0);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaSecondDenoiseKernel, numTilesX, numTilesY, sssartParams.viewCount);
            }
            else
            {
                Vector4 shadowChannelMask0 = new Vector4();
                int areaShadowSlice = sssartParams.areaShadowSlot / 4;
                GetShadowChannelMask(sssartParams.areaShadowSlot, ScreenSpaceShadowType.Area, ref shadowChannelMask0);
                cmd.SetComputeVectorParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, shadowChannelMask0);
                cmd.SetComputeIntParam(sssartParams.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, areaShadowSlice);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowNoDenoiseKernel, HDShaderIDs._DenoiseInputTexture, sssartResources.intermediateBufferRGBA0);
                cmd.SetComputeTextureParam(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowNoDenoiseKernel, HDShaderIDs._ScreenSpaceShadowsTextureRW, sssartResources.screenSpaceShadowTextureArray);
                cmd.DispatchCompute(sssartParams.screenSpaceShadowsFilterCS, sssartParams.areaShadowNoDenoiseKernel, numTilesX, numTilesY, sssartParams.viewCount);
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

            SSSAreaRayTraceParameters sssartParams = PrepareSSSAreaRayTraceParameters(hdCamera, additionalLightData, lightData, lightIndex);
            SSSAreaRayTraceResources sssartResources = PrepareSSSAreaRayTraceResources(hdCamera, directionBuffer, rayLengthBuffer,
                                                                                        intermediateBufferRGBA0, intermediateBufferRGBA1, intermediateBufferRG0,
                                                                                        shadowHistoryArray, analyticHistoryArray);
            ExecuteSSSAreaRayTrace(cmd, sssartParams, sssartResources);

            // IF we had to filter, then we have to execute this
            if (additionalLightData.filterTracedShadow)
            {
                int areaShadowSlot = m_lightList.lights[lightIndex].screenSpaceShadowIndex;
                // Write the result texture to the screen space shadow buffer
                WriteScreenSpaceShadowParameters wsssParams = PrepareWriteScreenSpaceShadowParameters(hdCamera, areaShadowSlot, ScreenSpaceShadowType.Area);
                WriteScreenSpaceShadowResources wsssResources = PrepareWriteScreenSpaceShadowResources(intermediateBufferRGBA0);
                ExecuteWriteScreenSpaceShadow(cmd, wsssParams, wsssResources);

                // Do not forget to update the identification of shadow history usage
                hdCamera.PropagateShadowHistory(additionalLightData, areaShadowSlot, GPULightType.Rectangle);
            }
        }
    }
}
