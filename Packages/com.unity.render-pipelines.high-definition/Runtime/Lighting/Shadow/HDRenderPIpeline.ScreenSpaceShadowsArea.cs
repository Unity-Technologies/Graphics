using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static void ExecuteSSSAreaRayTrace(CommandBuffer cmd, RTShadowAreaPassData data)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, data.ditheredTextureSet);

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (data.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (data.texHeight + (areaTileSize - 1)) / areaTileSize;

            // We have noticed from extensive profiling that ray-trace shaders are not as effective for running per-pixel computation. In order to reduce that,
            // we do a first prepass that compute the analytic term and probability and generates the first integration sample

            // Bind the light data
            cmd.SetComputeMatrixParam(data.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, data.worldToLocalMatrix);
            cmd.SetComputeIntParam(data.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetLight, data.lightIndex);

            data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.numSamples;
            ConstantBuffer.PushGlobal(cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Bind the input buffers
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[0], data.gbuffer0);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[1], data.gbuffer1);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[2], data.gbuffer2);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._GBufferTexture[3], data.gbuffer3);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the output buffers
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowIntegration, data.outputShadowTexture);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracedAreaShadowSample, data.intermediateBufferRGBA1);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);
            cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
            cmd.DispatchCompute(data.screenSpaceShadowsCS, data.areaRaytracingShadowPrepassKernel, numTilesX, numTilesY, data.viewCount);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(data.screenSpaceShadowsRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(data.screenSpaceShadowsRT, "VisibilityDXR");

            // Input data
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, data.intermediateBufferRGBA1);
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);
            cmd.SetRayTracingIntParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetLight, data.lightIndex);

            // Output data
            cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, data.outputShadowTexture);

            // Evaluate the intersection
            cmd.DispatchRays(data.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount);

            // Let's do the following samples (if any)
            for (int sampleIndex = 1; sampleIndex < data.numSamples; ++sampleIndex)
            {
                // Update global Constant Buffer
                data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.numSamples;
                data.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIndex;
                ConstantBuffer.PushGlobal(cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Bind the light data
                cmd.SetComputeIntParam(data.screenSpaceShadowsCS, HDShaderIDs._RaytracingTargetLight, data.lightIndex);
                cmd.SetComputeMatrixParam(data.screenSpaceShadowsCS, HDShaderIDs._RaytracingAreaWorldToLocal, data.worldToLocalMatrix);

                // Input Buffers
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[0], data.gbuffer0);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[1], data.gbuffer1);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[2], data.gbuffer2);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._GBufferTexture[3], data.gbuffer3);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                // Output buffers
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracedAreaShadowSample, data.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
                cmd.DispatchCompute(data.screenSpaceShadowsCS, data.areaRaytracingShadowNewSampleKernel, numTilesX, numTilesY, data.viewCount);

                // Input buffers
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowSample, data.intermediateBufferRGBA1);
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RayTracingLengthBuffer, data.rayLengthBuffer);
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
                cmd.SetRayTracingIntParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracingTargetLight, data.lightIndex);

                // Output buffers
                cmd.SetRayTracingTextureParam(data.screenSpaceShadowsRT, HDShaderIDs._RaytracedAreaShadowIntegration, data.outputShadowTexture);

                // Evaluate the intersection
                cmd.DispatchRays(data.screenSpaceShadowsRT, m_RayGenAreaShadowSingleName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount);
            }

            if (data.filterTracedShadow)
            {
                Vector4 shadowChannelMask0 = new Vector4();
                Vector4 shadowChannelMask1 = new Vector4();
                Vector4 shadowChannelMask2 = new Vector4();
                GetShadowChannelMask(data.areaShadowSlot, ScreenSpaceShadowType.Area, ref shadowChannelMask0);
                GetShadowChannelMask(data.areaShadowSlot, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask1);
                GetShadowChannelMask(data.areaShadowSlot + 1, ScreenSpaceShadowType.GrayScale, ref shadowChannelMask2);

                // Global parameters
                cmd.SetComputeIntParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._RaytracingDenoiseRadius, data.filterSize);
                cmd.SetComputeIntParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistorySlice, data.areaShadowSlot / 4);
                cmd.SetComputeVectorParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMask, shadowChannelMask0);
                cmd.SetComputeVectorParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskSn, shadowChannelMask1);
                cmd.SetComputeVectorParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._DenoisingHistoryMaskUn, shadowChannelMask2);

                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._AreaShadowHistory, data.shadowHistoryArray);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._AnalyticHistoryBuffer, data.analyticHistoryArray);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseInputTexture, data.outputShadowTexture);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBufferRGBA1);
                cmd.SetComputeFloatParam(data.screenSpaceShadowsFilterCS, HDShaderIDs._HistoryValidity, data.historyValidity);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaShadowApplyTAAKernel, numTilesX, numTilesY, data.viewCount);

                // Update the shadow history buffer
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaUpdateAnalyticHistoryKernel, HDShaderIDs._AnalyticHistoryBufferRW, data.analyticHistoryArray);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaUpdateAnalyticHistoryKernel, numTilesX, numTilesY, data.viewCount);

                // Update the analytic history buffer
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaUpdateShadowHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaUpdateShadowHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, data.shadowHistoryArray);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaUpdateShadowHistoryKernel, numTilesX, numTilesY, data.viewCount);

                // Inject parameters for noise estimation
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, HDShaderIDs._ScramblingTexture, data.scramblingTex);

                // Noise estimation pre-pass
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputShadowTexture);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaEstimateNoiseKernel, numTilesX, numTilesY, data.viewCount);

                // Reinject parameters for denoising
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaFirstDenoiseKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaFirstDenoiseKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                // First denoising pass
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaFirstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, data.outputShadowTexture);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaFirstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBufferRGBA1);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaFirstDenoiseKernel, numTilesX, numTilesY, data.viewCount);

                // Re-inject parameters for denoising
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaSecondDenoiseKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaSecondDenoiseKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                // Second (and final) denoising pass
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaSecondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBufferRGBA1);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaSecondDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputShadowTexture);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaSecondDenoiseKernel, numTilesX, numTilesY, data.viewCount);
            }
            else
            {
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowNoDenoiseKernel, HDShaderIDs._AnalyticProbBuffer, data.intermediateBufferRG0);
                cmd.SetComputeTextureParam(data.screenSpaceShadowsFilterCS, data.areaShadowNoDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputShadowTexture);
                cmd.DispatchCompute(data.screenSpaceShadowsFilterCS, data.areaShadowNoDenoiseKernel, numTilesX, numTilesY, data.viewCount);
            }
        }

        class RTShadowAreaPassData
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
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.numSamples = additionalLightData.numRayTracingSamples;
                passData.lightIndex = lightIndex;
                // We need to build the world to area light matrix
                passData.worldToLocalMatrix.SetColumn(0, lightData.right);
                passData.worldToLocalMatrix.SetColumn(1, lightData.up);
                passData.worldToLocalMatrix.SetColumn(2, lightData.forward);
                // Compensate the  relative rendering if active
                Vector3 lightPositionWS = lightData.positionRWS;
                passData.worldToLocalMatrix.SetColumn(3, lightPositionWS);
                passData.worldToLocalMatrix.m33 = 1.0f;
                passData.worldToLocalMatrix = passData.worldToLocalMatrix.inverse;
                passData.historyValidity = EvaluateHistoryValidity(hdCamera);
                passData.filterTracedShadow = additionalLightData.filterTracedShadow;
                passData.areaShadowSlot = m_GpuLightsBuilder.lights[lightIndex].screenSpaceShadowIndex;
                passData.filterSize = additionalLightData.filterSizeTraced;

                // Kernels
                passData.areaRaytracingShadowPrepassKernel = m_AreaRaytracingShadowPrepassKernel;
                passData.areaRaytracingShadowNewSampleKernel = m_AreaRaytracingShadowNewSampleKernel;
                passData.areaShadowApplyTAAKernel = m_AreaShadowApplyTAAKernel;
                passData.areaUpdateAnalyticHistoryKernel = m_AreaUpdateAnalyticHistoryKernel;
                passData.areaUpdateShadowHistoryKernel = m_AreaUpdateShadowHistoryKernel;
                passData.areaEstimateNoiseKernel = m_AreaEstimateNoiseKernel;
                passData.areaFirstDenoiseKernel = m_AreaFirstDenoiseKernel;
                passData.areaSecondDenoiseKernel = m_AreaSecondDenoiseKernel;
                passData.areaShadowNoDenoiseKernel = m_AreaShadowNoDenoiseKernel;

                // Other parameters
                // Grab the acceleration structure for the target camera
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.screenSpaceShadowsCS = m_ScreenSpaceShadowsCS;
                passData.screenSpaceShadowsRT = m_ScreenSpaceShadowsRT;
                passData.screenSpaceShadowsFilterCS = m_ScreenSpaceShadowsFilterCS;
                passData.scramblingTex = m_Asset.renderPipelineResources.textures.scramblingTex;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

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

                passData.shadowHistoryArray = builder.ReadWriteTexture(renderGraph.ImportTexture(shadowHistoryArray));
                passData.analyticHistoryArray = builder.ReadWriteTexture(renderGraph.ImportTexture(analyticHistoryArray));

                // Intermediate buffers
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });
                passData.rayLengthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Ray Length Buffer" });
                passData.intermediateBufferRGBA1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Buffer RGBA1" }); ;
                passData.intermediateBufferRG0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Buffer RG0" });

                // Debug textures
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);

                // Output buffers
                passData.outputShadowTexture = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Shadow Buffer" }));
                builder.SetRenderFunc(
                    (RTShadowAreaPassData data, RenderGraphContext context) =>
                    {
                        ExecuteSSSAreaRayTrace(context.cmd, data);
                    });
                areaShadow = passData.outputShadowTexture;
            }

            int areaShadowSlot = m_GpuLightsBuilder.lights[lightIndex].screenSpaceShadowIndex;
            WriteScreenSpaceShadow(renderGraph, hdCamera, areaShadow, screenSpaceShadowArray, areaShadowSlot, ScreenSpaceShadowType.Area);

            if (additionalLightData.filterTracedShadow)
            {
                // Do not forget to update the identification of shadow history usage
                hdCamera.PropagateShadowHistory(additionalLightData, areaShadowSlot, GPULightType.Rectangle);
            }
        }
    }
}
