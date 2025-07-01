using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenIndirectDiffuseIntegrationName = "RayGenIntegration";

        // Kernels
        int m_RaytracingIndirectDiffuseFullResKernel;
        int m_RaytracingIndirectDiffuseHalfResKernel;
        int m_IndirectDiffuseUpscaleFullResKernel;
        int m_IndirectDiffuseUpscaleHalfResKernel;

        void InitRayTracedIndirectDiffuse()
        {
            ComputeShader indirectDiffuseShaderCS = rayTracingResources.indirectDiffuseRayTracingCS;

            // Grab all the kernels we shall be using
            m_RaytracingIndirectDiffuseFullResKernel = indirectDiffuseShaderCS.FindKernel("RaytracingIndirectDiffuseFullRes");
            m_RaytracingIndirectDiffuseHalfResKernel = indirectDiffuseShaderCS.FindKernel("RaytracingIndirectDiffuseHalfRes");
            m_IndirectDiffuseUpscaleFullResKernel = indirectDiffuseShaderCS.FindKernel("IndirectDiffuseIntegrationUpscaleFullRes");
            m_IndirectDiffuseUpscaleHalfResKernel = indirectDiffuseShaderCS.FindKernel("IndirectDiffuseIntegrationUpscaleHalfRes");
        }

        void ReleaseRayTracedIndirectDiffuse()
        {
        }

        static RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_IndirectDiffuseHistoryBuffer{1}", viewName, frameIndex));
        }

        DeferredLightingRTParameters PrepareIndirectDiffuseDeferredLightingRTParameters(HDCamera hdCamera, bool fullResolution)
        {
            DeferredLightingRTParameters deferredParameters = new DeferredLightingRTParameters();

            // Fetch the GI volume component
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            RayTracingSettings rTSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = true;
            deferredParameters.layerMask.value = (int)RayTracingRendererFlag.GlobalIllumination;
            deferredParameters.diffuseLightingOnly = true;
            deferredParameters.halfResolution = !fullResolution;
            deferredParameters.rayCountType = (int)RayCountValues.DiffuseGI_Deferred;
            deferredParameters.lodBias = settings.textureLodBias.value;
            deferredParameters.rayMiss = (int)settings.rayMiss.value;
            deferredParameters.lastBounceFallbackHierarchy = (int)settings.lastBounceFallbackHierarchy.value;

            // Ray marching
            deferredParameters.mixedTracing = settings.tracing.value == RayCastingMode.Mixed && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred;
            deferredParameters.raySteps = settings.maxMixedRaySteps;
            deferredParameters.nearClipPlane = hdCamera.camera.nearClipPlane;
            deferredParameters.farClipPlane = hdCamera.camera.farClipPlane;
            deferredParameters.transparent = false;

            // Camera data
            deferredParameters.width = hdCamera.actualWidth;
            deferredParameters.height = hdCamera.actualHeight;
            deferredParameters.viewCount = hdCamera.viewCount;

            // Compute buffers
            deferredParameters.rayBinResult = m_RayBinResult;
            deferredParameters.rayBinSizeResult = m_RayBinSizeResult;
            deferredParameters.accelerationStructure = RequestAccelerationStructure(hdCamera);
            deferredParameters.lightCluster = RequestLightCluster();
            deferredParameters.mipChainBuffer = hdCamera.depthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

            // Shaders
            deferredParameters.rayMarchingCS = rayTracingResources.rayMarchingCS;
            deferredParameters.gBufferRaytracingRT = rayTracingResources.gBufferRayTracingRT;
            deferredParameters.deferredRaytracingCS = rayTracingResources.deferredRayTracingCS;
            deferredParameters.rayBinningCS = rayTracingResources.rayBinningCS;

            // Make a copy of the previous values that were defined in the CB
            deferredParameters.raytracingCB = m_ShaderVariablesRayTracingCB;
            // Override the ones we need to
            deferredParameters.raytracingCB._RaytracingRayMaxLength = settings.rayLength;
            deferredParameters.raytracingCB._RayTracingClampingFlag = 1;
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingPreExposition = 1;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 1;
            deferredParameters.raytracingCB._RayTracingAPVRayMiss = 1;
            deferredParameters.raytracingCB._RayTracingRayMissFallbackHierarchy = deferredParameters.rayMiss;
            deferredParameters.raytracingCB._RayTracingRayMissUseAmbientProbeAsSky = 1;
            deferredParameters.raytracingCB._RayTracingLastBounceFallbackHierarchy = deferredParameters.lastBounceFallbackHierarchy;
            deferredParameters.raytracingCB._RayTracingAmbientProbeDimmer = settings.ambientProbeDimmer.value;
            deferredParameters.raytracingCB._RaytracingAPVLayerMask = settings.adaptiveProbeVolumesLayerMask.value;

            return deferredParameters;
        }

        class DirGenRTGIPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public bool fullResolution;

            // Additional resources
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public int dirGenKernel;
            public ComputeShader directionGenCS;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle DirGenRTGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination settings, TextureHandle depthStencilbuffer, TextureHandle normalBuffer, bool fullResolution)
        {
            using (var builder = renderGraph.AddUnsafePass<DirGenRTGIPassData>("Generating the rays for RTGI", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.fullResolution = fullResolution;

                // Grab the right kernel
                passData.directionGenCS = rayTracingResources.indirectDiffuseRayTracingCS;
                passData.dirGenKernel = fullResolution ? m_RaytracingIndirectDiffuseFullResKernel : m_RaytracingIndirectDiffuseHalfResKernel;

                // Grab the additional parameters
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

                passData.depthStencilBuffer = depthStencilbuffer;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.outputBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "GI Ray Directions" });
                builder.UseTexture(passData.outputBuffer, AccessFlags.Write);

                builder.SetRenderFunc(
                    (DirGenRTGIPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Bind all the required textures
                        natCmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        natCmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
                        natCmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Bind the output buffers
                        natCmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, data.outputBuffer);

                        int numTilesXHR, numTilesYHR;
                        if (data.fullResolution)
                        {
                            // Evaluate the dispatch parameters
                            numTilesXHR = (data.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                            numTilesYHR = (data.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        }
                        else
                        {
                            // Evaluate the dispatch parameters
                            numTilesXHR = (data.texWidth / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                            numTilesYHR = (data.texHeight / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        }

                        // Compute the directions
                        natCmd.DispatchCompute(data.directionGenCS, data.dirGenKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        class UpscaleRTGIPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Additional resources
            public Texture2DArray blueNoiseTexture;
            public Texture2D scramblingTexture;
            public int upscaleKernel;
            public ComputeShader upscaleCS;

            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle indirectDiffuseBuffer;
            public TextureHandle directionBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleRTGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination settings,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle indirectDiffuseBuffer, TextureHandle directionBuffer, bool fullResolution)
        {
            using (var builder = renderGraph.AddUnsafePass<UpscaleRTGIPassData>("Upscale the RTGI result", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale)))
            {
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Grab the right kernel
                passData.upscaleCS = rayTracingResources.indirectDiffuseRayTracingCS;
                passData.upscaleKernel = fullResolution ? m_IndirectDiffuseUpscaleFullResKernel : m_IndirectDiffuseUpscaleHalfResKernel;

                // Grab the additional parameters
                passData.blueNoiseTexture = GetBlueNoiseManager().textureArray16RGB;
                passData.scramblingTexture = runtimeTextures.scramblingTex;

                passData.depthBuffer = depthPyramid;
                builder.UseTexture(passData.depthBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.indirectDiffuseBuffer = indirectDiffuseBuffer;
                builder.UseTexture(passData.indirectDiffuseBuffer, AccessFlags.Read);
                passData.directionBuffer = directionBuffer;
                builder.UseTexture(passData.directionBuffer, AccessFlags.Read);
                passData.outputBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Indirect Diffuse" });
                builder.UseTexture(passData.outputBuffer, AccessFlags.Write);

                builder.SetRenderFunc(
                    (UpscaleRTGIPassData data, UnsafeGraphContext ctx) =>
                    {
                        // Inject all the parameters for the compute
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._IndirectDiffuseTexture, data.indirectDiffuseBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._BlueNoiseTexture, data.blueNoiseTexture);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._ScramblingTexture, data.scramblingTexture);

                        // Output buffer
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._UpscaledIndirectDiffuseTextureRW, data.outputBuffer);

                        // Texture dimensions
                        int texWidth = data.texWidth;
                        int texHeight = data.texHeight;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        ctx.cmd.DispatchCompute(data.upscaleCS, data.upscaleKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        static RTHandle RequestRayTracedIndirectDiffuseHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle RenderIndirectDiffusePerformance(RenderGraph renderGraph, HDCamera hdCamera,
            in PrepassOutput prepassOutput, TextureHandle historyValidationTexture,
            TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Pointer to the final result
            TextureHandle rtgiResult;

            // Fetch all the settings
            GlobalIllumination settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Evaluate if the effect runs in full res
            bool fullResolution = settings.fullResolution || !RayTracingHalfResAllowed();

            // Generate the directions for the effect
            TextureHandle directionBuffer = DirGenRTGI(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.normalBuffer, fullResolution);

            // Trace the rays and evaluate the lighting
            DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera, fullResolution);
            RayTracingDefferedLightLoopOutput lightloopOutput = DeferredLightingRT(renderGraph, hdCamera, in deferredParamters, directionBuffer, prepassOutput, skyTexture, rayCountTexture);

            rtgiResult = UpscaleRTGI(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.normalBuffer, lightloopOutput.lightingBuffer, directionBuffer, fullResolution);

            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, prepassOutput.depthBuffer, prepassOutput.normalBuffer, prepassOutput.resolvedMotionVectorsBuffer, historyValidationTexture, fullResolution);

            return rtgiResult;
        }

        class TraceQualityRTGIPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public float rayLength;
            public int sampleCount;
            public float clampValue;
            public int bounceCount;
            public float lodBias;
            public int rayMiss;
            public int lastBounceFallbackHierarchy;
            public float ambientProbeDimmer;
            public UnityEngine.RenderingLayerMask apvLayerMask;

            // Other parameters
            public RayTracingShader indirectDiffuseRT;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public Texture skyTexture;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputBuffer;

            public bool enableDecals;
        }

        TextureHandle QualityRTGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddUnsafePass<TraceQualityRTGIPassData>("Quality RT Indirect Diffuse", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation)))
            {
                var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.rayLength = settings.rayLength;
                passData.sampleCount = settings.sampleCount.value;
                passData.clampValue = settings.clampValue;
                passData.bounceCount = settings.bounceCount.value;
                passData.lodBias = settings.textureLodBias.value;
                passData.rayMiss = (int)settings.rayMiss.value;
                passData.lastBounceFallbackHierarchy = (int)settings.lastBounceFallbackHierarchy.value;
                passData.ambientProbeDimmer = settings.ambientProbeDimmer.value;
                passData.apvLayerMask = settings.adaptiveProbeVolumesLayerMask.value;

                // Grab the additional parameters
                if (apvIsEnabled)
                {
                    if(m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                    {
                        passData.indirectDiffuseRT = rayTracingResources.indirectDiffuseRayTracingL1RT;
                    }
                    else
                    {
                        passData.indirectDiffuseRT = rayTracingResources.indirectDiffuseRaytracingL2RT;
                    }
                }
                else
                {
                    passData.indirectDiffuseRT = rayTracingResources.indirectDiffuseRayTracingOffRT;
                }

                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.lightCluster = RequestLightCluster();
                passData.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                // Copy the constant buffer
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

                // Set the input and output textures
                passData.depthStencilBuffer = depthStencilBuffer;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);
                passData.outputBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Indirect Diffuse" });
                builder.UseTexture(passData.outputBuffer, AccessFlags.Write);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (TraceQualityRTGIPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Define the shader pass to use for the indirect diffuse pass
                        natCmd.SetRayTracingShaderPass(data.indirectDiffuseRT, "IndirectDXR");

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.indirectDiffuseRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Set the data for the ray generation
                        natCmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._IndirectDiffuseTextureRW, data.outputBuffer);
                        natCmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        natCmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.depthStencilBuffer, RenderTextureSubElement.Stencil);
                        natCmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Set ray count texture
                        natCmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // LightLoop data
                        data.lightCluster.BindLightClusterData(natCmd);

                        // Set the data for the ray miss
                        natCmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._SkyTexture, data.skyTexture);

                        // Update global constant buffer
                        data.shaderVariablesRayTracingCB._RayTracingClampingFlag = 1;
                        data.shaderVariablesRayTracingCB._RaytracingIntensityClamp = data.clampValue;
                        data.shaderVariablesRayTracingCB._RaytracingRayMaxLength = data.rayLength;
                        data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.sampleCount;
#if NO_RAY_RECURSION
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = 1;
#else
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = data.bounceCount;
#endif
                        data.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 1;
                        data.shaderVariablesRayTracingCB._RayTracingLodBias = data.lodBias;
                        data.shaderVariablesRayTracingCB._RayTracingRayMissFallbackHierarchy = data.rayMiss;
                        data.shaderVariablesRayTracingCB._RayTracingLastBounceFallbackHierarchy = data.lastBounceFallbackHierarchy;
                        data.shaderVariablesRayTracingCB._RayTracingAmbientProbeDimmer = data.ambientProbeDimmer;
                        data.shaderVariablesRayTracingCB._RaytracingAPVLayerMask = data.apvLayerMask;

                        ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Only use the shader variant that has multi bounce if the bounce count > 1
                        CoreUtils.SetKeyword(natCmd, "MULTI_BOUNCE_INDIRECT", data.bounceCount > 1);

                        if (data.enableDecals)
                            DecalSystem.instance.SetAtlas(natCmd);

                        // Run the computation
                        natCmd.DispatchRays(data.indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount, null);

                        // Disable the keywords we do not need anymore
                        CoreUtils.SetKeyword(natCmd, "MULTI_BOUNCE_INDIRECT", false);
                    });

                return passData.outputBuffer;
            }
        }

        TextureHandle RenderIndirectDiffuseQuality(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationTexture,
            TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Evaluate the signal
            TextureHandle rtgiResult = QualityRTGI(renderGraph, hdCamera, depthStencilBuffer, normalBuffer, rayCountTexture);

            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, depthStencilBuffer, normalBuffer, motionVectors, historyValidationTexture, true);

            return rtgiResult;
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureHF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureLF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle DenoiseRTGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rtGIBuffer, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationTexture, bool fullResolution)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            if (giSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity0 = EvaluateIndirectDiffuseHistoryValidity0(hdCamera, fullResolution, true);

                HDTemporalFilter temporalFilter = GetTemporalFilter();
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();

                // Run the temporal denoiser
                TextureHandle historyBufferHF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureHF(hdCamera));
                HDTemporalFilter.TemporalFilterParameters filterParams;
                filterParams.singleChannel = false;
                filterParams.historyValidity = historyValidity0;
                filterParams.occluderMotionRejection = false;
                filterParams.receiverMotionRejection = giSettings.receiverMotionRejection.value;
                filterParams.exposureControl = true;
                filterParams.resolutionMultiplier = 1.0f;
                filterParams.historyResolutionMultiplier = 1.0f;

                TextureHandle denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, filterParams,
                    rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferHF,
                    depthStencilBuffer, normalBuffer, motionVectorBuffer, historyValidationTexture);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser.DiffuseDenoiserParameters ddParams;
                ddParams.singleChannel = false;
                ddParams.kernelSize = giSettings.denoiserRadius;
                ddParams.halfResolutionFilter = giSettings.halfResolutionDenoiser;
                ddParams.jitterFilter = giSettings.secondDenoiserPass;
                ddParams.resolutionMultiplier = 1.0f;
                rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthStencilBuffer, normalBuffer, rtGIBuffer);

                // If the second pass is requested, do it otherwise blit
                if (giSettings.secondDenoiserPass)
                {
                    float historyValidity1 = EvaluateIndirectDiffuseHistoryValidity1(hdCamera, fullResolution, true);

                    // Run the temporal filter
                    TextureHandle historyBufferLF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureLF(hdCamera));
                    filterParams.singleChannel = false;
                    filterParams.historyValidity = historyValidity1;
                    filterParams.occluderMotionRejection = false;
                    filterParams.receiverMotionRejection = giSettings.receiverMotionRejection.value;
                    filterParams.exposureControl = true;
                    denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, filterParams,
                        rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferLF,
                        depthStencilBuffer, normalBuffer, motionVectorBuffer, historyValidationTexture);

                    // Apply the second diffuse filter
                    ddParams.singleChannel = false;
                    ddParams.kernelSize = giSettings.denoiserRadius * 0.5f;
                    ddParams.halfResolutionFilter = giSettings.halfResolutionDenoiser;
                    ddParams.jitterFilter = false;
                    ddParams.resolutionMultiplier = 1.0f;
                    rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthStencilBuffer, normalBuffer, rtGIBuffer);

                    // Propagate the history validity for the second buffer
                    PropagateIndirectDiffuseHistoryValidity1(hdCamera, fullResolution, true);
                }

                // Propagate the history validity for the first buffer
                PropagateIndirectDiffuseHistoryValidity0(hdCamera, fullResolution, true);

                return rtGIBuffer;
            }
            else
                return rtGIBuffer;
        }

        TextureHandle RenderRayTracedIndirectDiffuse(RenderGraph renderGraph, HDCamera hdCamera,
            in PrepassOutput prepassOutput, TextureHandle historyValidationTexture,
            Texture skyTexture, TextureHandle rayCountTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            TextureHandle rtreflResult;
            bool qualityMode = false;

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                qualityMode = (giSettings.tracing.value == RayCastingMode.RayTracing) && (giSettings.mode.value == RayTracingMode.Quality);
            else
                qualityMode = m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality;


            if (qualityMode)
                rtreflResult = RenderIndirectDiffuseQuality(renderGraph, hdCamera,
                    prepassOutput.depthBuffer, prepassOutput.normalBuffer, prepassOutput.motionVectorsBuffer, historyValidationTexture,
                    rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);
            else
                rtreflResult = RenderIndirectDiffusePerformance(renderGraph, hdCamera,
                    prepassOutput, historyValidationTexture,
                    rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);

            return rtreflResult;
        }
    }
}
