using UnityEngine.Experimental.Rendering;

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
        int m_ReflectionRescaleAndAdjustWeightKernel;
        int m_ReflectionUpscaleKernel;

        void InitRayTracedReflections()
        {
            ComputeShader reflectionShaderCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            ComputeShader reflectionBilateralFilterCS = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;

            // Grab all the kernels we shall be using
            m_RaytracingReflectionsFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsFullRes");
            m_RaytracingReflectionsHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsHalfRes");
            m_RaytracingReflectionsTransparentFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentFullRes");
            m_RaytracingReflectionsTransparentHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentHalfRes");
            m_ReflectionAdjustWeightKernel = reflectionBilateralFilterCS.FindKernel("ReflectionAdjustWeight");
            m_ReflectionUpscaleKernel = reflectionBilateralFilterCS.FindKernel("ReflectionUpscale");
        }

        static RTHandle ReflectionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_ReflectionHistoryBuffer{1}", viewName, frameIndex));
        }

        void ReleaseRayTracedReflections()
        {
        }

        private float EvaluateRayTracedReflectionHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, fullResolution, rayTraced) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateRayTracedReflectionsHistoryValidity(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.RayTracedReflections, fullResolution, rayTraced);
        }

        #region Direction Generation

        struct RTReflectionDirGenParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public float minSmoothness;

            // Additional resources
            public ComputeShader directionGenCS;
            public int dirGenKernel;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
        }

        RTReflectionDirGenParameters PrepareRTReflectionDirGenParameters(HDCamera hdCamera, bool transparent, ScreenSpaceReflection settings)
        {
            RTReflectionDirGenParameters rtrDirGenParams = new RTReflectionDirGenParameters();

            // Set the camera parameters
            rtrDirGenParams.texWidth = settings.fullResolution ? hdCamera.actualWidth : hdCamera.actualWidth / 2;
            rtrDirGenParams.texHeight = settings.fullResolution ? hdCamera.actualHeight : hdCamera.actualHeight / 2;
            rtrDirGenParams.viewCount = hdCamera.viewCount;

            // Set the generation parameters
            rtrDirGenParams.minSmoothness = settings.minSmoothness;

            // Grab the right kernel
            rtrDirGenParams.directionGenCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            if (settings.fullResolution)
                rtrDirGenParams.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentFullResKernel : m_RaytracingReflectionsFullResKernel;
            else
                rtrDirGenParams.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentHalfResKernel : m_RaytracingReflectionsHalfResKernel;

            // Grab the additional parameters
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtrDirGenParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            rtrDirGenParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

            return rtrDirGenParams;
        }

        struct RTReflectionDirGenResources
        {
            // Input buffers
            public RTHandle depthBuffer;
            public RTHandle normalBuffer;
            public RTHandle stencilBuffer;
            public RenderTargetIdentifier clearCoatMaskTexture;

            // Output buffers
            public RTHandle outputBuffer;
        }

        RTReflectionDirGenResources PrepareRTReflectionDirGenResources(HDCamera hdCamera, RTHandle outputBuffer)
        {
            RTReflectionDirGenResources rtrDirGenResources = new RTReflectionDirGenResources();

            // Input buffers
            rtrDirGenResources.depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrDirGenResources.stencilBuffer = m_SharedRTManager.GetStencilBuffer();
            rtrDirGenResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtrDirGenResources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();

            // Output buffer
            rtrDirGenResources.outputBuffer = outputBuffer;
            return rtrDirGenResources;
        }

        static void RTReflectionDirectionGeneration(CommandBuffer cmd, RTReflectionDirGenParameters rtrDirGenParams, RTReflectionDirGenResources rtrDirGenResources)
        {
            // TODO: check if this is required, i do not think so
            CoreUtils.SetRenderTarget(cmd, rtrDirGenResources.outputBuffer, ClearFlag.Color, clearColor: Color.black);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtrDirGenParams.ditheredTextureSet);

            // Bind all the required scalars to the CB
            rtrDirGenParams.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = rtrDirGenParams.minSmoothness;
            ConstantBuffer.PushGlobal(cmd, rtrDirGenParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Bind all the required textures
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._DepthTexture, rtrDirGenResources.depthBuffer);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._NormalBufferTexture, rtrDirGenResources.normalBuffer);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._SsrClearCoatMaskTexture, rtrDirGenResources.clearCoatMaskTexture);
            cmd.SetComputeIntParam(rtrDirGenParams.directionGenCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._StencilTexture, rtrDirGenResources.stencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the output buffers
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, rtrDirGenResources.outputBuffer);

            // Evaluate the dispatch parameters
            int numTilesX = (rtrDirGenParams.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            int numTilesY = (rtrDirGenParams.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            // Compute the directions
            cmd.DispatchCompute(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, numTilesX, numTilesY, rtrDirGenParams.viewCount);
        }
        #endregion

        #region Deferred Lighting

        DeferredLightingRTParameters PrepareReflectionDeferredLightingRTParameters(HDCamera hdCamera)
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
            deferredParameters.halfResolution = !settings.fullResolution;
            deferredParameters.rayCountType = (int)RayCountValues.ReflectionDeferred;

            // Camera data
            deferredParameters.width = hdCamera.actualWidth;
            deferredParameters.height = hdCamera.actualHeight;
            deferredParameters.viewCount = hdCamera.viewCount;

            // Compute buffers
            deferredParameters.rayBinResult = m_RayBinResult;
            deferredParameters.rayBinSizeResult = m_RayBinSizeResult;
            deferredParameters.accelerationStructure = RequestAccelerationStructure();
            deferredParameters.lightCluster = RequestLightCluster();

            // Shaders
            deferredParameters.gBufferRaytracingRT = m_Asset.renderPipelineRayTracingResources.gBufferRaytracingRT;
            deferredParameters.deferredRaytracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;
            deferredParameters.rayBinningCS = m_Asset.renderPipelineRayTracingResources.rayBinningCS;

            // XRTODO: add ray binning support for single-pass
            if (deferredParameters.viewCount > 1 && deferredParameters.rayBinning)
            {
                deferredParameters.rayBinning = false;
            }

            // Make a copy of the previous values that were defined in the CB
            deferredParameters.raytracingCB = m_ShaderVariablesRayTracingCB;
            // Override the ones we need to
            deferredParameters.raytracingCB._RaytracingRayMaxLength = settings.rayLength;
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingIncludeSky = settings.reflectSky.value ? 1 : 0;
            deferredParameters.raytracingCB._RaytracingPreExposition = 0;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 0;

            return deferredParameters;
        }
        #endregion

        #region AdjustWeight

        struct RTRAdjustWeightParameters
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
        }

        RTRAdjustWeightParameters PrepareRTRAdjustWeightParameters(HDCamera hdCamera, ScreenSpaceReflection settings)
        {
            RTRAdjustWeightParameters parameters = new RTRAdjustWeightParameters();
            // Camera parameters
            parameters.texWidth = settings.fullResolution ? hdCamera.actualWidth : hdCamera.actualWidth / 2;
            parameters.texHeight = settings.fullResolution ? hdCamera.actualHeight : hdCamera.actualHeight / 2;
            parameters.viewCount = hdCamera.viewCount;

            // Requires parameters
            parameters.minSmoothness = settings.minSmoothness;
            parameters.smoothnessFadeStart = settings.smoothnessFadeStart;

            // Other parameters
            parameters.reflectionFilterCS = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            parameters.adjustWeightKernel = settings.fullResolution ? m_ReflectionAdjustWeightKernel : m_ReflectionRescaleAndAdjustWeightKernel;
            parameters.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            return parameters;
        }

        struct RTRAdjustWeightResources
        {
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle lightingTexture;
            public RTHandle directionTexture;
            public RTHandle outputTexture;
            public RenderTargetIdentifier clearCoatMaskTexture;
        }

        RTRAdjustWeightResources PrepareRTRAdjustWeightResources(HDCamera hdCamera, RTHandle lightingTexture, RTHandle directionTexture, RTHandle outputTexture)
        {
            RTRAdjustWeightResources resources = new RTRAdjustWeightResources();
            resources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            resources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            resources.lightingTexture = lightingTexture;
            resources.directionTexture = directionTexture;
            resources.outputTexture = outputTexture;
            resources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
            return resources;
        }

        static void AdjustWeightRTReflections(CommandBuffer cmd, RTRAdjustWeightParameters parameters, RTRAdjustWeightResources resources)
        {
            // Bind all the required scalars to the CB
            parameters.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = parameters.minSmoothness;
            parameters.shaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = parameters.smoothnessFadeStart;
            ConstantBuffer.PushGlobal(cmd, parameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Source input textures
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._DepthTexture, resources.depthStencilBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._SsrClearCoatMaskTexture, resources.clearCoatMaskTexture);
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._DirectionPDFTexture, resources.directionTexture);

            // Lighting textures
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._SsrLightingTextureRW, resources.lightingTexture);

            // Output texture
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.adjustWeightKernel, HDShaderIDs._RaytracingReflectionTexture, resources.outputTexture);

            // Compute the texture
            int numTilesXHR = (parameters.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            int numTilesYHR = (parameters.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            cmd.DispatchCompute(parameters.reflectionFilterCS, parameters.adjustWeightKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }
        #endregion

        #region Upscale
        struct RTRUpscaleParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Other parameters
            public ComputeShader reflectionFilterCS;
            public int upscaleKernel;
        }

        RTRUpscaleParameters PrepareRTRUpscaleParameters(HDCamera hdCamera, ScreenSpaceReflection settings)
        {
            RTRUpscaleParameters parameters = new RTRUpscaleParameters();
            // Camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Other parameters
            parameters.reflectionFilterCS = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            parameters.upscaleKernel = m_ReflectionUpscaleKernel;
            return parameters;
        }

        struct RTRUpscaleResources
        {
            public RTHandle depthStencilBuffer;
            public RTHandle lightingTexture;
            public RTHandle outputTexture;
        }

        RTRUpscaleResources PrepareRTRUpscaleResources(HDCamera hdCamera, RTHandle lightingTexture, RTHandle hitPointTexture, RTHandle outputTexture)
        {
            RTRUpscaleResources rtrUpscaleResources = new RTRUpscaleResources();
            rtrUpscaleResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrUpscaleResources.lightingTexture = lightingTexture;
            rtrUpscaleResources.outputTexture = outputTexture;
            return rtrUpscaleResources;
        }

        static void UpscaleRTR(CommandBuffer cmd, RTRUpscaleParameters parameters, RTRUpscaleResources resources)
        {
            // Input textures
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.upscaleKernel, HDShaderIDs._DepthTexture, resources.depthStencilBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.upscaleKernel, HDShaderIDs._SsrLightingTextureRW, resources.lightingTexture);

            // Output texture
            cmd.SetComputeTextureParam(parameters.reflectionFilterCS, parameters.upscaleKernel, HDShaderIDs._RaytracingReflectionTexture, resources.outputTexture);

            // Compute the texture
            int numTilesXHR = (parameters.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            int numTilesYHR = (parameters.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            cmd.DispatchCompute(parameters.reflectionFilterCS, parameters.upscaleKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }
        #endregion

        #region Quality RTR

        struct RTRQualityRenderingParameters
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

            // Other parameters
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public Texture skyTexture;
            public RayTracingShader reflectionShader;
        };

        RTRQualityRenderingParameters PrepareRTRQualityRenderingParameters(HDCamera hdCamera, ScreenSpaceReflection settings, bool transparent)
        {
            RTRQualityRenderingParameters rtrQualityRenderingParameters = new RTRQualityRenderingParameters();

            // Camera parameters
            rtrQualityRenderingParameters.texWidth = hdCamera.actualWidth;
            rtrQualityRenderingParameters.texHeight = hdCamera.actualHeight;
            rtrQualityRenderingParameters.viewCount = hdCamera.viewCount;

            // Reflection evaluation parameters
            rtrQualityRenderingParameters.clampValue = settings.clampValue;
            rtrQualityRenderingParameters.reflectSky = settings.reflectSky.value ? 1 : 0;
            rtrQualityRenderingParameters.rayLength = settings.rayLength;
            rtrQualityRenderingParameters.sampleCount = settings.sampleCount.value;
            rtrQualityRenderingParameters.bounceCount = settings.bounceCount.value;
            rtrQualityRenderingParameters.transparent = transparent;
            rtrQualityRenderingParameters.minSmoothness = settings.minSmoothness;
            rtrQualityRenderingParameters.smoothnessFadeStart = settings.smoothnessFadeStart;

            // Other parameters
            rtrQualityRenderingParameters.accelerationStructure = RequestAccelerationStructure();
            rtrQualityRenderingParameters.lightCluster = RequestLightCluster();
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtrQualityRenderingParameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            rtrQualityRenderingParameters.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            rtrQualityRenderingParameters.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
            rtrQualityRenderingParameters.reflectionShader = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;

            return rtrQualityRenderingParameters;
        }

        struct RTRQualityRenderingResources
        {
            // Input texture
            public RTHandle depthBuffer;
            public RTHandle normalBuffer;
            public RTHandle stencilBuffer;
            public RenderTargetIdentifier clearCoatMaskTexture;

            // Debug texture
            public RTHandle rayCountTexture;

            // Output texture
            public RTHandle outputTexture;
        }

        RTRQualityRenderingResources PrepareRTRQualityRenderingResources(HDCamera hdCamera, RTHandle outputTexture)
        {
            RTRQualityRenderingResources rtrQualityRenderingResources = new RTRQualityRenderingResources();

            // Input texture
            rtrQualityRenderingResources.depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrQualityRenderingResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtrQualityRenderingResources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
            rtrQualityRenderingResources.stencilBuffer = m_SharedRTManager.GetStencilBuffer();

            // Debug texture
            RayCountManager rayCountManager = GetRayCountManager();
            rtrQualityRenderingResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output texture
            rtrQualityRenderingResources.outputTexture = outputTexture;
            return rtrQualityRenderingResources;
        }

        static void RenderQualityRayTracedReflections(CommandBuffer cmd, RTRQualityRenderingParameters rtrQRenderingParameters, RTRQualityRenderingResources rtrQRenderingResources)
        {
            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(rtrQRenderingParameters.reflectionShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(rtrQRenderingParameters.reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, rtrQRenderingParameters.accelerationStructure);

            // Global reflection parameters
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingIntensityClamp = rtrQRenderingParameters.clampValue;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingIncludeSky = rtrQRenderingParameters.reflectSky;
            // Inject the ray generation data
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingRayMaxLength = rtrQRenderingParameters.rayLength;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingNumSamples = rtrQRenderingParameters.sampleCount;
            // Set the number of bounces for reflections
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingMaxRecursion = rtrQRenderingParameters.bounceCount;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 0;
            // Bind all the required scalars to the CB
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = rtrQRenderingParameters.minSmoothness;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = rtrQRenderingParameters.smoothnessFadeStart;
            ConstantBuffer.PushGlobal(cmd, rtrQRenderingParameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtrQRenderingParameters.ditheredTextureSet);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrLightingTextureRW, rtrQRenderingResources.outputTexture);
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._DepthTexture, rtrQRenderingResources.depthBuffer);
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._NormalBufferTexture, rtrQRenderingResources.normalBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, rtrQRenderingResources.stencilBuffer, RenderTextureSubElement.Stencil);
            cmd.SetRayTracingIntParams(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._RayCountTexture, rtrQRenderingResources.rayCountTexture);

            // Bind the lightLoop data
            rtrQRenderingParameters.lightCluster.BindLightClusterData(cmd);

            // Evaluate the clear coat mask texture based on the lit shader mode
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrClearCoatMaskTexture, rtrQRenderingResources.clearCoatMaskTexture);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SkyTexture, rtrQRenderingParameters.skyTexture);

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", rtrQRenderingParameters.bounceCount > 1);

            // Run the computation
            cmd.DispatchRays(rtrQRenderingParameters.reflectionShader, rtrQRenderingParameters.transparent ? m_RayGenIntegrationTransparentName : m_RayGenIntegrationName, (uint)rtrQRenderingParameters.texWidth, (uint)rtrQRenderingParameters.texHeight, (uint)rtrQRenderingParameters.viewCount);

            // Disable multi-bounce
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);
        }
        #endregion

        #region Legacy Pipeline
        void RenderRayTracedReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent = false)
        {
            ScreenSpaceReflection reflectionSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
            {
                switch (reflectionSettings.mode.value)
                {
                    case RayTracingMode.Performance:
                        {
                            RenderReflectionsPerformance(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
                        }
                        break;
                    case RayTracingMode.Quality:
                        {
                            RenderReflectionsQuality(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
                        }
                        break;
                }
            }
            else if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
            {
                RenderReflectionsQuality(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
            }
            else
            {
                RenderReflectionsPerformance(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
            }
        }

        void RenderReflectionsPerformance(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent)
        {
            // Fetch the required resources
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Fetch all the settings
            ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Generate the signal
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionDirectionGeneration)))
            {
                // Prepare the components for the direction generation
                RTReflectionDirGenParameters rtrDirGenParameters = PrepareRTReflectionDirGenParameters(hdCamera, transparent, settings);
                RTReflectionDirGenResources rtrDirGenResousources = PrepareRTReflectionDirGenResources(hdCamera, intermediateBuffer1);
                RTReflectionDirectionGeneration(cmd, rtrDirGenParameters, rtrDirGenResousources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareReflectionDeferredLightingRTParameters(hdCamera);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, intermediateBuffer1, intermediateBuffer0);
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionAdjustWeight)))
            {
                // Prepare the parameters for the upscale pass
                RTRAdjustWeightParameters parameters = PrepareRTRAdjustWeightParameters(hdCamera, settings);
                RTRAdjustWeightResources resources = PrepareRTRAdjustWeightResources(hdCamera, intermediateBuffer0, intermediateBuffer1, outputTexture);
                AdjustWeightRTReflections(cmd, parameters, resources);
            }

            // Denoise if required
            if (settings.denoise && !transparent)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter)))
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    // Prepare the parameters and the resources
                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    float historyValidity = EvaluateRayTracedReflectionHistoryValidity(hdCamera, settings.fullResolution, true);
                    ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, historyValidity, settings.denoiserRadius, settings.fullResolution);
                    ReflectionDenoiserResources reflectionDenoiserResources = reflectionDenoiser.PrepareReflectionDenoiserResources(hdCamera, outputTexture, reflectionHistory,
                                                    intermediateBuffer0, intermediateBuffer1);

                    // Denoise
                    HDReflectionDenoiser.DenoiseBuffer(cmd, reflDenoiserParameters, reflectionDenoiserResources);
                    PropagateRayTracedReflectionsHistoryValidity(hdCamera, settings.fullResolution, true);
                }
            }
        }

        void RenderReflectionsQuality(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent)
        {
            // Request the buffers we shall be using
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();

            // Do the integration
            RTRQualityRenderingParameters rtrQRenderingParameters = PrepareRTRQualityRenderingParameters(hdCamera, settings, transparent);
            RTRQualityRenderingResources rtrQRenderingResources = PrepareRTRQualityRenderingResources(hdCamera, outputTexture);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                RenderQualityRayTracedReflections(cmd, rtrQRenderingParameters, rtrQRenderingResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter)))
            {
                if (settings.denoise && !transparent)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    // Prepare the parameters and the resources
                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    float historyValidity = EvaluateRayTracedReflectionHistoryValidity(hdCamera, true, true);
                    ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, historyValidity, settings.denoiserRadius, true);
                    ReflectionDenoiserResources reflectionDenoiserResources = reflectionDenoiser.PrepareReflectionDenoiserResources(hdCamera, outputTexture, reflectionHistory,
                                                    intermediateBuffer0, intermediateBuffer1);
                    HDReflectionDenoiser.DenoiseBuffer(cmd, reflDenoiserParameters, reflectionDenoiserResources);
                    PropagateRayTracedReflectionsHistoryValidity(hdCamera, true, true);
                }
            }
        }
    }
    #endregion
}
