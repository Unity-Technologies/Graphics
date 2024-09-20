using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Tile of the side used to dispatch the various compute shader kernels
        const int rtReflectionsComputeTileSize = 8;

        // String values
        const string m_RayGenIntegrationName = "RayGenIntegration";
        const string m_RayGenIntegrationTransparentName = "RayGenIntegrationTransparent";

        // Kernels
        int m_RaytracingReflectionsFullResKernel;
        int m_RaytracingReflectionsHalfResKernel;
        int m_RaytracingReflectionsTransparentFullResKernel;
        int m_RaytracingReflectionsTransparentHalfResKernel;
        int m_ReflectionAdjustWeightKernel;
        int m_ReflectionUpscaleKernel;

        void InitRayTracedReflections()
        {
            ComputeShader reflectionShaderCS = rayTracingResources.reflectionRayTracingCS;
            ComputeShader reflectionBilateralFilterCS = rayTracingResources.reflectionBilateralFilterCS;

            // Grab all the kernels we shall be using
            m_RaytracingReflectionsFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsFullRes");
            m_RaytracingReflectionsHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsHalfRes");
            m_RaytracingReflectionsTransparentFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentFullRes");
            m_RaytracingReflectionsTransparentHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentHalfRes");
            m_ReflectionAdjustWeightKernel = reflectionBilateralFilterCS.FindKernel("ReflectionAdjustWeight");
            m_ReflectionUpscaleKernel = reflectionBilateralFilterCS.FindKernel("ReflectionUpscale");
        }

        static RTHandle ReflectionLightingDistanceHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_Reflection_Lighting_Distance_HistoryBuffer{1}", viewName, frameIndex));
        }

        static RTHandle ReflectionAccHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8_UInt, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_Reflection_Accumulation_HistoryBuffer{1}", viewName, frameIndex));
        }

        static RTHandle ReflectionStabilizationHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_Reflection_Stabilization_HistoryBuffer{1}", viewName, frameIndex));
        }

        int CombineRayTracedReflectionsHistoryStateToMask(bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = 0;
            flagMask |= (fullResolution ? (int)HDCamera.HistoryEffectFlags.FullResolution : 0);
            flagMask |= (rayTraced ? (int)HDCamera.HistoryEffectFlags.RayTraced : 0);
            return flagMask;
        }

        private float EvaluateRayTracedReflectionHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Evaluate the history validity
            int flagMask = CombineRayTracedReflectionsHistoryStateToMask(fullResolution, rayTraced);
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, flagMask) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateRayTracedReflectionsHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            int flagMask = CombineRayTracedReflectionsHistoryStateToMask(fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, flagMask);
        }

        ReflectionsMode GetReflectionsMode(HDCamera hdCamera)
        {
            ReflectionsMode mode = ReflectionsMode.Off;

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
            {
                var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
                if (settings.enabled.value)
                {
                    bool allowSSR = hdCamera.colorPyramidHistoryIsValid && !hdCamera.isFirstFrame;

                    // We can use the ray tracing version of the effect if:
                    // - It is enabled in the frame settings
                    // - It is enabled in the volume
                    // - The RTAS has been build validated
                    // - The RTLightCluster has been validated
                    bool raytracing = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                        && settings.tracing.value != RayCastingMode.RayMarching
                        && GetRayTracingState();
                    if (raytracing)
                        mode = settings.tracing.value == RayCastingMode.RayTracing ? ReflectionsMode.RayTraced : ReflectionsMode.Mixed;
                    else
                        mode = allowSSR ? ReflectionsMode.ScreenSpace : ReflectionsMode.Off;
                }
            }
            return mode;
        }

        #region Direction Generation
        class DirGenRTRPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public float minSmoothness;
            public int frameIndex;

            // Additional resources
            public ComputeShader directionGenCS;
            public int dirGenKernel;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;

            public TextureHandle depthBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;
            public TextureHandle outputBuffer;
        }

        TextureHandle DirGenRTR(RenderGraph renderGraph, HDCamera hdCamera, ScreenSpaceReflection settings, TextureHandle depthBuffer, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle clearCoatTexture, bool fullResolution, bool transparent)
        {
            using (var builder = renderGraph.AddRenderPass<DirGenRTRPassData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionDirectionGeneration)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = fullResolution ? hdCamera.actualWidth : hdCamera.actualWidth / 2;
                passData.texHeight = fullResolution ? hdCamera.actualHeight : hdCamera.actualHeight / 2;
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.minSmoothness = settings.minSmoothness;
                passData.frameIndex = RayTracingFrameIndex(hdCamera, 32);

                // Grab the right kernel
                passData.directionGenCS = rayTracingResources.reflectionRayTracingCS;
                if (fullResolution)
                    passData.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentFullResKernel : m_RaytracingReflectionsFullResKernel;
                else
                    passData.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentHalfResKernel : m_RaytracingReflectionsHalfResKernel;

                // Grab the additional parameters
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Directions" }));

                builder.SetRenderFunc(
                    (DirGenRTRPassData data, RenderGraphContext ctx) =>
                    {
                        // TODO: check if this is required, i do not think so
                        CoreUtils.SetRenderTarget(ctx.cmd, data.outputBuffer, ClearFlag.Color, clearColor: Color.black);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Bind all the required scalars to the CB
                        data.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = data.minSmoothness;
                        data.shaderVariablesRayTracingCB._RayTracingReflectionFrameIndex = data.frameIndex;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Bind all the required textures
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskTexture);
                        ctx.cmd.SetComputeIntParam(data.directionGenCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);

                        // Bind the output buffers
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, data.outputBuffer);

                        // Evaluate the dispatch parameters
                        int numTilesX = (data.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        int numTilesY = (data.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        // Compute the directions
                        ctx.cmd.DispatchCompute(data.directionGenCS, data.dirGenKernel, numTilesX, numTilesY, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        #endregion

        #region AdjustWeight

        class AdjustWeightRTRPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            public float minSmoothness;
            public float smoothnessFadeStart;

            // Other parameters
            public ComputeShader reflectionFilterCS;
            public int adjustWeightKernel;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;

            // GBuffer Data
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;

            // Input data
            public TextureHandle lightingTexture;

            // Output data
            public TextureHandle outputTexture;
        }

        TextureHandle AdjustWeightRTR(RenderGraph renderGraph, HDCamera hdCamera, ScreenSpaceReflection settings,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle clearCoatTexture,
            TextureHandle lightingTexture)
        {
            using (var builder = renderGraph.AddRenderPass<AdjustWeightRTRPassData>("Adjust Weight RTR", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionAdjustWeight)))
            {
                builder.EnableAsyncCompute(false);

                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Requires parameters
                passData.minSmoothness = settings.minSmoothness;
                passData.smoothnessFadeStart = settings.smoothnessFadeStart;

                // Other parameters
                passData.reflectionFilterCS = rayTracingResources.reflectionBilateralFilterCS;
                passData.adjustWeightKernel = m_ReflectionAdjustWeightKernel;
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.lightingTexture = builder.ReadTexture(lightingTexture);
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Reflections" }));

                builder.SetRenderFunc(
                    (AdjustWeightRTRPassData data, RenderGraphContext ctx) =>
                    {
                        // Bind all the required scalars to the CB
                        data.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = data.minSmoothness;
                        data.shaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = data.smoothnessFadeStart;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Source input textures
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskTexture);
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                        // Lighting textures
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._SsrLightingTextureRW, data.lightingTexture);

                        // Output texture
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.adjustWeightKernel, HDShaderIDs._RaytracingReflectionTexture, data.outputTexture);

                        // Compute the texture
                        int numTilesXHR = (data.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        int numTilesYHR = (data.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        ctx.cmd.DispatchCompute(data.reflectionFilterCS, data.adjustWeightKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputTexture;
            }
        }

        #endregion

        #region Upscale

        class UpscaleRTRPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Other parameters
            public ComputeShader reflectionFilterCS;
            public int upscaleKernel;

            public TextureHandle depthStencilBuffer;
            public TextureHandle lightingTexture;
            public TextureHandle outputTexture;
        }

        TextureHandle UpscaleRTR(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle lightingTexture)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleRTRPassData>("Upscale RTR", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.reflectionFilterCS = rayTracingResources.reflectionBilateralFilterCS;
                passData.upscaleKernel = m_ReflectionUpscaleKernel;

                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.lightingTexture = builder.ReadTexture(lightingTexture);
                passData.outputTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Reflections" }));

                builder.SetRenderFunc(
                    (UpscaleRTRPassData data, RenderGraphContext ctx) =>
                    {
                        // Input textures
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.upscaleKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.upscaleKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.upscaleKernel, HDShaderIDs._SsrLightingTextureRW, data.lightingTexture);

                        // Output texture
                        ctx.cmd.SetComputeTextureParam(data.reflectionFilterCS, data.upscaleKernel, HDShaderIDs._RaytracingReflectionTexture, data.outputTexture);

                        // Compute the texture
                        int numTilesXHR = (data.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        int numTilesYHR = (data.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        ctx.cmd.DispatchCompute(data.reflectionFilterCS, data.upscaleKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputTexture;
            }
        }

        #endregion

        static RTHandle RequestRayTracedReflectionLightingDistanceHistoryTexture(HDCamera hdCamera, ref bool realloc)
        {
            var currentRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionDistance);
            if (currentRT == null)
            {
                currentRT = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionDistance, ReflectionLightingDistanceHistoryBufferAllocatorFunction, 1);
                realloc = true;
            }
            return currentRT;
        }

        static RTHandle RequestRayTracedReflectionAccumulationHistoryTexture(HDCamera hdCamera, ref bool realloc)
        {
            var currentRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionAccumulation);
            if (currentRT == null)
            {
                currentRT = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionAccumulation, ReflectionAccHistoryBufferAllocatorFunction, 1);
                realloc = true;
            }
            return currentRT;
        }

        static RTHandle RequestRayTracedReflectionStabilizationHistoryTexture(HDCamera hdCamera, ref bool realloc)
        {
            var currentRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionStabilization);
            if (currentRT == null)
            {
                currentRT = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflectionStabilization, ReflectionStabilizationHistoryBufferAllocatorFunction, 1);
                realloc = true;
            }
            return currentRT;
        }

        DeferredLightingRTParameters PrepareReflectionDeferredLightingRTParameters(HDCamera hdCamera, bool fullResolution, bool transparent)
        {
            DeferredLightingRTParameters deferredParameters = new DeferredLightingRTParameters();

            // Fetch the GI volume component
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            RayTracingSettings rTSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = true;
            deferredParameters.layerMask.value = (int)RayTracingRendererFlag.Reflection;
            deferredParameters.diffuseLightingOnly = false;
            deferredParameters.halfResolution = !fullResolution;
            deferredParameters.rayCountType = (int)RayCountValues.ReflectionDeferred;
            deferredParameters.lodBias = settings.textureLodBias.value;
            deferredParameters.rayMiss = (int)(settings.rayMiss.value);
            deferredParameters.lastBounceFallbackHierarchy = (int)(settings.lastBounceFallbackHierarchy.value);

            // Ray Marching parameters
            deferredParameters.mixedTracing = (settings.tracing.value == RayCastingMode.Mixed && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred);
            deferredParameters.raySteps = settings.rayMaxIterationsRT;
            deferredParameters.nearClipPlane = hdCamera.camera.nearClipPlane;
            deferredParameters.farClipPlane = hdCamera.camera.farClipPlane;
            deferredParameters.transparent = transparent;

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
            deferredParameters.raytracingCB._RayTracingClampingFlag = transparent ? 0 : 1;
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingPreExposition = 0;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 0;
            deferredParameters.raytracingCB._RayTracingAPVRayMiss = 0;
            deferredParameters.raytracingCB._RayTracingRayMissFallbackHierarchy = deferredParameters.rayMiss;
            deferredParameters.raytracingCB._RayTracingRayMissUseAmbientProbeAsSky = 0;
            deferredParameters.raytracingCB._RayTracingLastBounceFallbackHierarchy = deferredParameters.lastBounceFallbackHierarchy;
            deferredParameters.raytracingCB._RayTracingAmbientProbeDimmer = settings.ambientProbeDimmer.value;

            return deferredParameters;
        }

        TextureHandle RenderReflectionsPerformance(RenderGraph renderGraph, HDCamera hdCamera,
            in PrepassOutput prepassOutput, TextureHandle rayCountTexture, TextureHandle historyValidation, TextureHandle clearCoatTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            // Fetch all the settings
            ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Evaluate if the effect runs in full res
            bool fullResolution = settings.fullResolution || !RayTracingHalfResAllowed();

            // Generate the directions for the effect
            TextureHandle directionBuffer = DirGenRTR(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.normalBuffer, clearCoatTexture, fullResolution, transparent);

            // Run the deferred lighting pass
            DeferredLightingRTParameters deferredParamters = PrepareReflectionDeferredLightingRTParameters(hdCamera, fullResolution, transparent);
            RayTracingDefferedLightLoopOutput lightloopOutput = DeferredLightingRT(renderGraph, hdCamera, in deferredParamters, directionBuffer, prepassOutput, skyTexture, rayCountTexture);
           
            // Denoise if required
            if (settings.denoise && !transparent)
            {
                lightloopOutput.lightingBuffer = DenoiseReflection(renderGraph, hdCamera, fullResolution, settings.denoiserRadius, settings.denoiserAntiFlickeringStrength,
                    lightloopOutput.lightingBuffer, lightloopOutput.distanceBuffer, prepassOutput, clearCoatTexture, historyValidation);
            }

            // We only need to upscale if the effect was not rendered in full res
            if (!fullResolution && (!settings.denoise || transparent))
                lightloopOutput.lightingBuffer = UpscaleRTR(renderGraph, hdCamera, prepassOutput.depthBuffer, lightloopOutput.lightingBuffer);

           // Adjust the reflection weight
           lightloopOutput.lightingBuffer = AdjustWeightRTR(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.normalBuffer, clearCoatTexture, lightloopOutput.lightingBuffer);

            // Return the result
            return lightloopOutput.lightingBuffer;
        }

#region Quality
        class TraceQualityRTRPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Reflection evaluation parameters
            public float clampValue;
            public int reflectSky;
            public float rayLength;
            public int sampleCount;
            public int bounceCount;
            public bool transparent;
            public float minSmoothness;
            public float smoothnessFadeStart;
            public float lodBias;
            public int rayMissfallbackHierarchy;
            public int lastBouncefallbackHierarchy;
            public float ambientProbeDimmer;
            public int frameIndex;

            // Other parameters
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public Texture skyTexture;
            public RayTracingShader reflectionShader;

            public TextureHandle depthBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskTexture;
            public TextureHandle rayCountTexture;

            // Output textures
            public TextureHandle lightingTexture;
            public TextureHandle distanceTexture;

            public bool enableDecals;
        }
        struct RayTracingReflectionsQualityOutput
        {
            public TextureHandle lightingBuffer;
            public TextureHandle distanceBuffer;
        }

        RayTracingReflectionsQualityOutput QualityRTR(RenderGraph renderGraph, HDCamera hdCamera, ScreenSpaceReflection settings,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle clearCoatTexture, TextureHandle rayCountTexture, bool transparent)
        {
            using (var builder = renderGraph.AddRenderPass<TraceQualityRTRPassData>("Quality RT Reflections", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                builder.EnableAsyncCompute(false);

                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Reflection evaluation parameters
                passData.clampValue = settings.clampValue;
                passData.reflectSky = settings.reflectSky.value ? 1 : 0;
                passData.rayLength = settings.rayLength;
                passData.sampleCount = settings.sampleCount.value;
                passData.bounceCount = settings.bounceCount.value;
                passData.transparent = transparent;
                passData.minSmoothness = settings.minSmoothness;
                passData.smoothnessFadeStart = settings.smoothnessFadeStart;
                passData.lodBias = settings.textureLodBias.value;
                passData.rayMissfallbackHierarchy = (int)settings.rayMiss.value;
                passData.lastBouncefallbackHierarchy = (int)settings.lastBounceFallbackHierarchy.value;
                passData.ambientProbeDimmer = settings.ambientProbeDimmer.value;
                passData.frameIndex = RayTracingFrameIndex(hdCamera, 32);

                // Other parameters
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.lightCluster = RequestLightCluster();
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
                passData.reflectionShader = rayTracingResources.reflectionRayTracingRT;

                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.clearCoatMaskTexture = builder.ReadTexture(clearCoatTexture);
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);

                // Output textures
                passData.lightingTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Reflections" }));
                passData.distanceTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Reflections" }));

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (TraceQualityRTRPassData data, RenderGraphContext ctx) =>
                    {
                        // Define the shader pass to use for the reflection pass
                        ctx.cmd.SetRayTracingShaderPass(data.reflectionShader, "IndirectDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Global reflection parameters
                        data.shaderVariablesRayTracingCB._RayTracingClampingFlag = data.transparent ? 0 : 1;
                        data.shaderVariablesRayTracingCB._RaytracingIntensityClamp = data.clampValue;
                        // Inject the ray generation data
                        data.shaderVariablesRayTracingCB._RaytracingRayMaxLength = data.rayLength;
                        data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.sampleCount;
                        // Set the number of bounces for reflections
#if NO_RAY_RECURSION
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = 1;
#else
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = data.bounceCount;
#endif
                        data.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 0;
                        // Bind all the required scalars to the CB
                        data.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = data.minSmoothness;
                        data.shaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = data.smoothnessFadeStart;
                        data.shaderVariablesRayTracingCB._RayTracingLodBias = data.lodBias;
                        data.shaderVariablesRayTracingCB._RayTracingRayMissFallbackHierarchy = data.rayMissfallbackHierarchy;
                        data.shaderVariablesRayTracingCB._RayTracingLastBounceFallbackHierarchy = data.lastBouncefallbackHierarchy;
                        data.shaderVariablesRayTracingCB._RayTracingAmbientProbeDimmer = data.ambientProbeDimmer;
                        data.shaderVariablesRayTracingCB._RayTracingReflectionFrameIndex = data.frameIndex;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Set the data for the ray generation
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.stencilBuffer, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetRayTracingIntParam(data.reflectionShader, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

                        // Set ray count texture
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // Bind the lightLoop data
                        data.lightCluster.BindLightClusterData(ctx.cmd);

                        // Evaluate the clear coat mask texture based on the lit shader mode
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskTexture);

                        // Set the data for the ray miss
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._SkyTexture, data.skyTexture);

                        // Output textures
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._RayTracingLightingTextureRW, data.lightingTexture);
                        ctx.cmd.SetRayTracingTextureParam(data.reflectionShader, HDShaderIDs._RayTracingDistanceTextureRW, data.distanceTexture);

                        // Only use the shader variant that has multi bounce if the bounce count > 1
                        CoreUtils.SetKeyword(ctx.cmd, "MULTI_BOUNCE_INDIRECT", data.bounceCount > 1);

                        if (data.enableDecals)
                            DecalSystem.instance.SetAtlas(ctx.cmd); // for clustered decals

                        // Run the computation
                        ctx.cmd.DispatchRays(data.reflectionShader, data.transparent ? m_RayGenIntegrationTransparentName : m_RayGenIntegrationName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount);

                        // Disable multi-bounce
                        CoreUtils.SetKeyword(ctx.cmd, "MULTI_BOUNCE_INDIRECT", false);
                    });

                RayTracingReflectionsQualityOutput output = new RayTracingReflectionsQualityOutput();
                output.lightingBuffer = passData.lightingTexture;
                output.distanceBuffer = passData.distanceTexture;
                return output;
            }
        }

        TextureHandle RenderReflectionsQuality(RenderGraph renderGraph, HDCamera hdCamera,
            in PrepassOutput prepassOutput, TextureHandle rayCountTexture, TextureHandle historyValidation, TextureHandle clearCoatTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            TextureHandle rtrResult;

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            RayTracingReflectionsQualityOutput rtResult = QualityRTR(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.normalBuffer, clearCoatTexture, rayCountTexture, transparent);

            // Denoise if required
            if (settings.denoise && !transparent)
            {
                rtrResult = DenoiseReflection(renderGraph, hdCamera, fullResolution: true, settings.denoiserRadius, settings.denoiserAntiFlickeringStrength,
                    rtResult.lightingBuffer, rtResult.distanceBuffer, prepassOutput, clearCoatTexture, historyValidation);
            }

           // Adjust the reflection weight
           rtResult.lightingBuffer = AdjustWeightRTR(renderGraph, hdCamera, settings, prepassOutput.depthBuffer, prepassOutput.normalBuffer, clearCoatTexture, rtResult.lightingBuffer);

            // Return the result
            return rtResult.lightingBuffer;
        }

        #endregion

        bool EnableRayTracedReflections(HDCamera hdCamera, ScreenSpaceReflection settings)
        {
            // We can use the ray tracing version of the effect if:
            // - It is enabled in the frame settings
            // - It is enabled in the volume
            // - The RTAS has been build validated
            // - The RTLightCluster has been validated
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                   && ScreenSpaceReflection.RayTracingActive(settings)
                   && GetRayTracingState() && GetRayTracingClusterState();
        }

        int CombineReflectionsHistoryStateToMask(bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = 0;
            flagMask |= (fullResolution ? (int)HDCamera.HistoryEffectFlags.FullResolution : 0);
            flagMask |= (rayTraced ? (int)HDCamera.HistoryEffectFlags.RayTraced : 0);
            return flagMask;
        }

        private float EvaluateRayTracingHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineReflectionsHistoryStateToMask(fullResolution, rayTraced);
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, flagMask) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateReflectionsHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineReflectionsHistoryStateToMask(fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, flagMask);
        }

        TextureHandle DenoiseReflection(RenderGraph renderGraph, HDCamera hdCamera, bool fullResolution, float denoiserRadius, float antiFlickering,
            TextureHandle lightingBuffer, TextureHandle distanceBuffer,
            in PrepassOutput prepassOutput, TextureHandle clearCoatTexture, TextureHandle historyValidation)
        {
            // Evaluate if the history is usable
            float historyValidity = EvaluateRayTracingHistoryValidity(hdCamera, fullResolution, true);

            // Grab the 3 history buffers
            bool realloc = false;
            RTHandle mainHistory = RequestRayTracedReflectionLightingDistanceHistoryTexture(hdCamera, ref realloc);
            RTHandle accumulationHistory = RequestRayTracedReflectionAccumulationHistoryTexture(hdCamera, ref realloc);
            RTHandle stabilizationHistory = RequestRayTracedReflectionStabilizationHistoryTexture(hdCamera, ref realloc);
            historyValidity *= realloc ? 0.0f : 1.0f;

            // Denoise the input signal
            TextureHandle denoiserBuffer =  GetReBlurDenoiser().DenoiseIndirectSpecular(renderGraph, hdCamera, fullResolution, historyValidity, denoiserRadius, antiFlickering,
                prepassOutput, clearCoatTexture, historyValidation,
                lightingBuffer, distanceBuffer,
                mainHistory, accumulationHistory, stabilizationHistory);

            // Flag the history as valid for the following frame
            PropagateReflectionsHistoryValidity(hdCamera, fullResolution, true);

            // Return the output buffer
            return denoiserBuffer;
        }

        TextureHandle RenderRayTracedReflections(RenderGraph renderGraph, HDCamera hdCamera,
            in PrepassOutput prepassOutput, TextureHandle clearCoatTexture, Texture skyTexture, TextureHandle rayCountTexture, TextureHandle historyValidation,
            ShaderVariablesRaytracing shaderVariablesRaytracing, bool transparent)
        {
            ScreenSpaceReflection reflectionSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            bool qualityMode = false;

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                qualityMode = (reflectionSettings.tracing.value == RayCastingMode.RayTracing) && (reflectionSettings.mode.value == RayTracingMode.Quality);
            else
                qualityMode = m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality;


            if (qualityMode)
                return RenderReflectionsQuality(renderGraph, hdCamera,
                    prepassOutput, rayCountTexture, historyValidation, clearCoatTexture, skyTexture,
                    shaderVariablesRaytracing, transparent);
            else
                return RenderReflectionsPerformance(renderGraph, hdCamera,
                    prepassOutput, rayCountTexture, historyValidation, clearCoatTexture, skyTexture,
                    shaderVariablesRaytracing, transparent);
        }
    }
}
