using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static float EvaluateHistoryValidityDirectionalShadow(HDCamera hdCamera, int dirShadowIndex, HDAdditionalLightData additionalLightData)
        {
            // We need to set the history as invalid if the directional light has rotated
            float historyValidity = 1.0f;
            if (hdCamera.shadowHistoryUsage[dirShadowIndex].transform != additionalLightData.transform.localToWorldMatrix
                || !hdCamera.ValidShadowHistory(additionalLightData, dirShadowIndex, GPULightType.Directional))
                historyValidity = 0.0f;

            // We need to check if something invalidated the history buffers
            historyValidity *= hdCamera.ActiveRayTracingAccumulation() ? EvaluateHistoryValidity(hdCamera) : 0.0f;
            return historyValidity;
        }

        TextureHandle DenoiseDirectionalScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            in TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle motionVetorsBuffer, in TextureHandle historyValidityBuffer,
            in TextureHandle noisyBuffer, in TextureHandle velocityBuffer, in TextureHandle distanceBuffer)
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
            HDTemporalFilter.TemporalDenoiserArrayOutputData temporalFilterResult = GetTemporalFilter().DenoiseBuffer(renderGraph, hdCamera,
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

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle directionBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle velocityBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle outputShadowBuffer;
        }

        void RenderRayTracedDirectionalScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            in TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle motionVetorsBuffer, in TextureHandle historyValidityBuffer,
            in TextureHandle rayCountTexture, in TextureHandle screenSpaceShadowArray)
        {
            TextureHandle directionalShadow;
            TextureHandle velocityBuffer;
            TextureHandle distanceBuffer;

            bool softShadows = m_CurrentSunLightAdditionalLightData.angularDiameter > 0.0 ? true : false;

            using (var builder = renderGraph.AddUnsafePass<RTSDirectionalTracePassData>("Directional RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
            {
                RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.softShadow = softShadows;
                // If the surface is infinitively small, we force it to one sample.
                passData.numShadowSamples = passData.softShadow ? m_CurrentSunLightAdditionalLightData.numRayTracingSamples : 1;
                passData.colorShadow = m_CurrentSunLightAdditionalLightData.colorShadow;
                passData.maxShadowLength = rayTracingSettings.directionalShadowRayLength.value;

                // Kernels
                passData.clearShadowKernel = m_ClearShadowTexture;
                passData.directionalShadowSample = m_RaytracingDirectionalShadowSample;

                // Grab the acceleration structure for the target camera
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.screenSpaceShadowCS = m_ScreenSpaceShadowsCS;
                passData.screenSpaceShadowRT = m_ScreenSpaceShadowsRT;
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                // Input Buffer
                passData.depthStencilBuffer = depthBuffer;
                builder.UseTexture(depthBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Direction Buffer" });

                // Debug buffers
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);

                // Output Buffers
                passData.velocityBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8_SNorm, enableRandomWrite = true, clearBuffer = true, name = "Velocity Buffer" });
                builder.UseTexture(passData.velocityBuffer, AccessFlags.ReadWrite);
                passData.distanceBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, clearBuffer = true, name = "Distance Buffer" });
                builder.UseTexture(passData.distanceBuffer, AccessFlags.ReadWrite);
                passData.outputShadowBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, clearBuffer = true, name = "RT Directional Shadow" });
                builder.UseTexture(passData.outputShadowBuffer, AccessFlags.ReadWrite);

                builder.SetRenderFunc(
                    static (RTSDirectionalTracePassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // Clear the integration texture
                        natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                        natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.velocityBuffer);
                        natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.clearShadowKernel, HDShaderIDs._RaytracedShadowIntegration, data.distanceBuffer);
                        natCmd.DispatchCompute(data.screenSpaceShadowCS, data.clearShadowKernel, numTilesX, numTilesY, data.viewCount);

                        // Grab and bind the acceleration structure for the target camera
                        natCmd.SetRayTracingAccelerationStructure(data.screenSpaceShadowRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Make sure the right closest hit/any hit will be triggered by using the right multi compile
                        CoreUtils.SetKeyword(natCmd, "TRANSPARENT_COLOR_SHADOW", data.colorShadow);

                        // Define which ray generation shaders we shall be using
                        string directionaLightShadowShader = data.colorShadow ? m_RayGenDirectionalColorShadowSingleName : m_RayGenDirectionalShadowSingleName;

                        // Loop through the samples of this frame
                        for (int sampleIdx = 0; sampleIdx < data.numShadowSamples; ++sampleIdx)
                        {
                            // Update global Constant Buffer
                            data.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIdx;
                            data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.numShadowSamples;
                            ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                            // Input Buffer
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.directionalShadowSample, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.directionalShadowSample, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.directionalShadowSample, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                            // Output buffer
                            natCmd.SetComputeTextureParam(data.screenSpaceShadowCS, data.directionalShadowSample, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);

                            // Generate a new direction
                            natCmd.DispatchCompute(data.screenSpaceShadowCS, data.directionalShadowSample, numTilesX, numTilesY, data.viewCount);

                            // Define the shader pass to use for the shadow pass
                            natCmd.SetRayTracingShaderPass(data.screenSpaceShadowRT, "VisibilityDXR");

                            // Input Uniforms
                            natCmd.SetRayTracingFloatParam(data.screenSpaceShadowRT, HDShaderIDs._DirectionalMaxRayLength, data.maxShadowLength);

                            // Set ray count texture
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                            // Input buffers
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);

                            // Output buffer
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, data.colorShadow ? HDShaderIDs._RaytracedColorShadowIntegration : HDShaderIDs._RaytracedShadowIntegration, data.outputShadowBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._VelocityBuffer, data.velocityBuffer);
                            natCmd.SetRayTracingTextureParam(data.screenSpaceShadowRT, HDShaderIDs._RaytracingDistanceBufferRW, data.distanceBuffer);

                            // Evaluate the visibility
                            natCmd.DispatchRays(data.screenSpaceShadowRT, directionaLightShadowShader, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount, null);
                        }

                        // Now that we are done with the ray tracing bit, disable the multi compile that was potentially enabled
                        CoreUtils.SetKeyword(natCmd, "TRANSPARENT_COLOR_SHADOW", false);
                    });

                directionalShadow = passData.outputShadowBuffer;
                velocityBuffer = passData.velocityBuffer;
                distanceBuffer = passData.distanceBuffer;
            }

            // If required, denoise the shadow
            if (m_CurrentSunLightAdditionalLightData.filterTracedShadow && softShadows)
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
            public int depthSlice;

            public TextureHandle normalBuffer;
            public TextureHandle screenSpaceShadowArray;
        }

        void RenderDirectionalLightScreenSpaceShadow(RenderGraph renderGraph, HDCamera hdCamera,
            in TextureHandle depthBuffer, in TextureHandle normalBuffer, in TextureHandle motionVectorsBuffer, in TextureHandle historyValidityBuffer,
            in TextureHandle rayCountTexture, in TextureHandle screenSpaceShadowArray)
        {
            // Should we be executing anything really?
            bool screenSpaceShadowRequired = m_CurrentSunLightAdditionalLightData != null && (m_CurrentSunShadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderScreenSpaceShadow) != 0;

            // Render directional screen space shadow if required
            if (screenSpaceShadowRequired)
            {
                bool rayTracedDirectionalRequired = (m_CurrentSunShadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderRayTracedShadow) != 0 && GetRayTracingState();
                // If the shadow is flagged as ray traced, we need to evaluate it completely
                if (rayTracedDirectionalRequired)
                    RenderRayTracedDirectionalScreenSpaceShadow(renderGraph, hdCamera, depthBuffer, normalBuffer, motionVectorsBuffer, historyValidityBuffer, rayCountTexture, screenSpaceShadowArray);
                else
                {
                    using (var builder = renderGraph.AddUnsafePass<SSSDirectionalTracePassData>("Directional RT Shadow", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDirectionalLightShadow)))
                    {
                        passData.depthSlice = m_CurrentSunLightDirectionalLightData.screenSpaceShadowIndex;
                        passData.normalBuffer = normalBuffer;
                        builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                        passData.screenSpaceShadowArray = screenSpaceShadowArray;
                        builder.UseTexture(passData.screenSpaceShadowArray, AccessFlags.ReadWrite);

                        builder.SetRenderFunc(
                            static (SSSDirectionalTracePassData data, UnsafeGraphContext ctx) =>
                            {
                                var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                                // If it is screen space but not ray traced, then we can rely on the shadow map
                                // WARNING: This pattern only works because we can only have one directional and the directional shadow is evaluated first.
                                var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                                CoreUtils.SetRenderTarget(natCmd, data.screenSpaceShadowArray, depthSlice: data.depthSlice);
                                mpb.SetTexture(HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                HDUtils.DrawFullScreen(natCmd, s_ScreenSpaceShadowsMat, data.screenSpaceShadowArray, mpb);
                            });
                    }
                }
            }
        }
    }
}
