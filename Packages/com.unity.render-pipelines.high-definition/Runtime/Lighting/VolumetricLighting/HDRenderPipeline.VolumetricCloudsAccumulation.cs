using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static RTHandle VolumetricCloudsHistoryBufferAllocatorFunction(HDCameraFrameHistoryType type, string viewName, int frameIndex, RTHandleSystem rtHandleSystem, bool fullscale)
        {
            int index = type == HDCameraFrameHistoryType.VolumetricClouds0 ? 0 : 1;
            return rtHandleSystem.Alloc(Vector2.one * (fullscale ? 1.0f : 0.5f), TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory{1}Buffer{2}", viewName, index, frameIndex));
        }

        static RTHandle VolumetricClouds0HistoryBufferAllocatorFunctionDownscaled(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
            => VolumetricCloudsHistoryBufferAllocatorFunction(HDCameraFrameHistoryType.VolumetricClouds0, viewName, frameIndex, rtHandleSystem, false);
        static RTHandle VolumetricClouds0HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
            => VolumetricCloudsHistoryBufferAllocatorFunction(HDCameraFrameHistoryType.VolumetricClouds0, viewName, frameIndex, rtHandleSystem, true);

        static RTHandle VolumetricClouds1HistoryBufferAllocatorFunctionDownscaled(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
            => VolumetricCloudsHistoryBufferAllocatorFunction(HDCameraFrameHistoryType.VolumetricClouds1, viewName, frameIndex, rtHandleSystem, false);
        static RTHandle VolumetricClouds1HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
            => VolumetricCloudsHistoryBufferAllocatorFunction(HDCameraFrameHistoryType.VolumetricClouds1, viewName, frameIndex, rtHandleSystem, true);

        static RTHandle RequestVolumetricCloudsHistoryTexture(HDCamera hdCamera, bool current, HDCameraFrameHistoryType type, bool fullscale)
        {
            RTHandle texture = current ? hdCamera.GetCurrentFrameRT((int)type) : hdCamera.GetPreviousFrameRT((int)type);
            if (texture != null)
                return texture;

            // Do that to avoid GC.alloc
            System.Func<string, int, RTHandleSystem, RTHandle> allocator = type == HDCameraFrameHistoryType.VolumetricClouds0 ?
                (fullscale ? VolumetricClouds0HistoryBufferAllocatorFunction : VolumetricClouds0HistoryBufferAllocatorFunctionDownscaled) :
                (fullscale ? VolumetricClouds1HistoryBufferAllocatorFunction : VolumetricClouds1HistoryBufferAllocatorFunctionDownscaled);
            return hdCamera.AllocHistoryFrameRT((int)type, allocator, 2);
        }

        private int CombineVolumetricCLoudsHistoryStateToMask(bool localClouds)
        {
            // Combine the flags to define the current mask (we use the custom bit 0 to track the locality of the clouds.
            return (localClouds ? (int)HDCamera.HistoryEffectFlags.CustomBit0 : 0);
        }

        private bool EvaluateVolumetricCloudsHistoryValidity(HDCamera hdCamera, bool localClouds)
        {
            // Evaluate the history validity
            int flagMask = CombineVolumetricCLoudsHistoryStateToMask(localClouds);
            return hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        private void PropagateVolumetricCloudsHistoryValidity(HDCamera hdCamera, bool localClouds)
        {
            int flagMask = CombineVolumetricCLoudsHistoryStateToMask(localClouds);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        struct VolumetricCloudsParameters_Accumulation
        {
            // Resolution parameters
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public int viewCount;

            public bool downscaleDepth;
            public bool historyValidity;
            public bool needExtraColorBufferCopy;
            public Vector2Int previousViewportSize;

            // Compute shader and kernels
            public int convertObliqueDepthKernel;
            public int depthDownscaleKernel;
            public int renderKernel;
            public int reprojectKernel;
            public int upscaleAndCombineKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // MSAA support
            public bool needsTemporaryBuffer;
            public Material cloudCombinePass;
        }

        VolumetricCloudsParameters_Accumulation PrepareVolumetricCloudsParameters_Accumulation(HDCamera hdCamera, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, bool historyValidity, float downscaling)
        {
            VolumetricCloudsParameters_Accumulation parameters = new VolumetricCloudsParameters_Accumulation();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera.exposureControlFS, settings, cameraType, in cloudModelData, ref parameters.commonData);

            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            // Final resolution at which the effect should be exported
            parameters.finalWidth = hdCamera.actualWidth;
            parameters.finalHeight = hdCamera.actualHeight;
            // Intermediate resolution at which the effect is accumulated
            parameters.intermediateWidth = Mathf.RoundToInt(downscaling * hdCamera.actualWidth);
            parameters.intermediateHeight = Mathf.RoundToInt(downscaling * hdCamera.actualHeight);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.5f * downscaling * hdCamera.actualWidth);
            parameters.traceHeight = Mathf.RoundToInt(0.5f * downscaling * hdCamera.actualHeight);

            parameters.viewCount = hdCamera.viewCount;
            parameters.previousViewportSize = hdCamera.historyRTHandleProperties.previousViewportSize;
            parameters.historyValidity = historyValidity;;
            parameters.downscaleDepth = downscaling == 0.5f;

            float historyScale =  hdCamera.intermediateDownscaling * (hdCamera.volumetricCloudsFullscaleHistory ? 1.0f : 2.0f);
            parameters.previousViewportSize = new Vector2Int(
                x: Mathf.RoundToInt(historyScale * hdCamera.historyRTHandleProperties.previousViewportSize.x),
                y: Mathf.RoundToInt(historyScale * hdCamera.historyRTHandleProperties.previousViewportSize.y)
            );

            // MSAA support
            parameters.needsTemporaryBuffer = hdCamera.msaaEnabled;
            parameters.cloudCombinePass = m_CloudCombinePass;

            parameters.needExtraColorBufferCopy = (GetColorBufferFormat() == GraphicsFormat.B10G11R11_UFloatPack32 &&
                // On PC and Metal, but not on console.
                (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan));

            // In case of MSAA, we no longer require the preliminary copy as there is no longer a need for RW of the color buffer.
            parameters.needExtraColorBufferCopy &= !parameters.needsTemporaryBuffer;

            // Compute shader and kernels
            parameters.convertObliqueDepthKernel = m_ConvertObliqueDepthKernel;
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.reprojectKernel = settings.ghostingReduction.value ? m_ReprojectCloudsRejectionKernel : m_ReprojectCloudsKernel;
            if (downscaling == 0.5f)
                parameters.upscaleAndCombineKernel = parameters.needExtraColorBufferCopy ? m_UpscaleAndCombineCloudsKernelColorCopy : m_UpscaleAndCombineCloudsKernelColorRW;
            else
                parameters.upscaleAndCombineKernel = parameters.needExtraColorBufferCopy ? m_CombineCloudsKernelColorCopy : m_CombineCloudsKernelColorRW;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.traceWidth;
            cameraData.traceHeight = parameters.traceHeight;
            cameraData.intermediateWidth = parameters.intermediateWidth;
            cameraData.intermediateHeight = parameters.intermediateHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.viewCount = parameters.viewCount;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = true;
            UpdateShaderVariableslClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            // If this is a default camera, we want the improved blending, otherwise we don't (in the case of a planar)
            float perceptualBlending = settings.perceptualBlending.value;
            parameters.commonData.cloudsCB._ImprovedTransmittanceBlend = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default ? perceptualBlending : 0.0f;
            parameters.commonData.cloudsCB._CubicTransmittance = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default && hdCamera.msaaEnabled ? perceptualBlending : 0;

            return parameters;
        }

        static void TraceVolumetricClouds_Accumulation(CommandBuffer cmd, VolumetricCloudsParameters_Accumulation parameters, ComputeBuffer ambientProbe,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle motionVectors, RTHandle volumetricLightingTexture, RTHandle scatteringFallbackTexture, RTHandle maxZMask,
            RTHandle currentHistory0Buffer, RTHandle previousHistory0Buffer,
            RTHandle currentHistory1Buffer, RTHandle previousHistory1Buffer,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateLightingBuffer1, RTHandle intermediateDepthBuffer0, RTHandle intermediateDepthBuffer1, RTHandle intermediateDepthBuffer2,
            RTHandle intermediateColorBuffer, RTHandle intermediateUpscaleBuffer)
        {
            // Compute the number of tiles to evaluate
            int traceTX = (parameters.traceWidth + (8 - 1)) / 8;
            int traceTY = (parameters.traceHeight + (8 - 1)) / 8;

            // Compute the number of tiles to evaluate
            int intermediateTX = (parameters.intermediateWidth + (8 - 1)) / 8;
            int intermediateTY = (parameters.intermediateHeight + (8 - 1)) / 8;

            // Compute the number of tiles to evaluate
            int finalTX = (parameters.finalWidth + (8 - 1)) / 8;
            int finalTY = (parameters.finalHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // Set the multi compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.commonData.localClouds);

            // We only need to handle history buffers if this is not a reflection probe
            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            Vector2Int previousViewportSize = previousHistory0Buffer.GetScaledSize(parameters.previousViewportSize);
            parameters.commonData.cloudsCB._HistoryViewportSize = new Vector2(previousViewportSize.x, previousViewportSize.y);
            parameters.commonData.cloudsCB._HistoryBufferSize = new Vector2(previousHistory0Buffer.rt.width, previousHistory0Buffer.rt.height);

            // Bind the constant buffer (global as we need it for the .shader as well)
            ConstantBuffer.PushGlobal(cmd, parameters.commonData.cloudsCB, HDShaderIDs._ShaderVariablesClouds);

            RTHandle currentDepthBuffer = depthPyramid;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsDepthDownscale)))
            {
                if (parameters.commonData.cameraType == TVolumetricCloudsCameraType.PlanarReflection)
                {
                    // In order to be able to work with planar
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthTexture, depthPyramid);
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthBufferRW, intermediateDepthBuffer2);
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, finalTX, finalTY, parameters.viewCount);
                    currentDepthBuffer = intermediateDepthBuffer2;
                }

                if (parameters.downscaleDepth)
                {
                    // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._DepthTexture, currentDepthBuffer);
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._HalfResDepthBufferRW, intermediateDepthBuffer0);
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount);
                }
                else
                    intermediateDepthBuffer0 = currentDepthBuffer;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                // Ray-march the clouds for this frame
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", parameters.commonData.cloudsCB._PhysicallyBasedSun == 1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._MaxZMaskTexture, maxZMask);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsSourceDepth, intermediateDepthBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley128RGBA, parameters.commonData.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._ErosionNoise, parameters.commonData.erosionNoise);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudMapTexture, parameters.commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudLutTexture, parameters.commonData.cloudLutTexture);
                cmd.SetComputeBufferParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, ambientProbe);

                // Output buffers
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsDepthTextureRW, intermediateDepthBuffer1);

                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, traceTX, traceTY, parameters.viewCount);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);
            }

            // We only reproject for realtime clouds
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsReproject)))
            {
                if (!parameters.historyValidity)
                {
                    CoreUtils.SetRenderTarget(cmd, previousHistory0Buffer, clearFlag: ClearFlag.Color, clearColor: Color.black);
                    CoreUtils.SetRenderTarget(cmd, previousHistory1Buffer, clearFlag: ClearFlag.Color, clearColor: Color.black);
                }

                // Re-project the result from the previous frame
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsLightingTexture, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsDepthTexture, intermediateDepthBuffer1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HalfResDepthBuffer, intermediateDepthBuffer0);

                // History buffers
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HistoryVolumetricClouds0Texture, previousHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._HistoryVolumetricClouds1Texture, previousHistory1Buffer);

                // Output textures
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsLightingTextureRW, currentHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, HDShaderIDs._CloudsAdditionalTextureRW, currentHistory1Buffer);

                // Re-project from the previous frame
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.reprojectKernel, intermediateTX, intermediateTY, parameters.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscaleAndCombine)))
            {
                if (parameters.needExtraColorBufferCopy)
                    HDUtils.BlitCameraTexture(cmd, colorBuffer, intermediateColorBuffer);

                // Compute the final resolution parameters
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthStatusTexture, currentHistory1Buffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsTexture, currentHistory0Buffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._DepthTexture, currentDepthBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._CameraColorTexture, intermediateColorBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
                if (parameters.commonData.cloudsCB._PhysicallyBasedSun == 0)
                {
                    // This has to be done in the global space given that the "correct" one happens in the global space.
                    // If we do it in the local space, there are some cases when the previous frames local take precedence over the current frame global one.
                    cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, scatteringFallbackTexture);
                }

                if (parameters.needsTemporaryBuffer)
                {
                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", true);

                    // Provide this second upscaling + combine strategy in case a temporary buffer is requested (ie MSAA).
                    // In the case of an MSAA color target, we cannot use the in-place blending of the clouds with the color target.
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Perform the upscale into an intermediate buffer.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, finalTX, finalTY, parameters.viewCount);

                    parameters.cloudCombinePass.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Composite the clouds into the MSAA target via hardware blending.
                    HDUtils.DrawFullScreen(cmd, parameters.cloudCombinePass, colorBuffer, null, 0);

                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", false);
                }
                else
                {
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, colorBuffer);

                    // Perform the upscale and combine with the color buffer in place.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, finalTX, finalTY, parameters.viewCount);
                }
            }

            // Reset all the multi-compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);
        }

        class VolumetricCloudsAccumulationData
        {
            public VolumetricCloudsParameters_Accumulation parameters;

            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle motionVectors;
            public TextureHandle maxZMask;
            public ComputeBufferHandle ambientProbeBuffer;

            public TextureHandle volumetricLighting;
            public TextureHandle scatteringFallbackTexture;

            public TextureHandle previousHistoryBuffer0;
            public TextureHandle currentHistoryBuffer0;
            public TextureHandle previousHistoryBuffer1;
            public TextureHandle currentHistoryBuffer1;

            public TextureHandle intermediateBuffer0;
            public TextureHandle intermediateBuffer1;
            public TextureHandle intermediateBufferDepth0;
            public TextureHandle intermediateBufferDepth1;
            public TextureHandle intermediateBufferDepth2;
            public TextureHandle intermediateBufferUpscale;

            public TextureHandle intermediateColorBufferCopy;
        }

        TextureHandle RenderVolumetricClouds_Accumulation(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType,
            TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting, TextureHandle maxZMask)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsAccumulationData>("Volumetric Clouds", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
                
                // When DRS scale is lower than threshold, trace in half res instead of quarter res
                bool halfRes = DynamicResolutionHandler.instance.GetCurrentScale() * 100.0f < currentPlatformRenderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold;
                float downscaling = halfRes ? 1.0f : 0.5f;

                bool fullscaleHistory = DynamicResolutionHandler.instance.DynamicResolutionEnabled() ? true : false;
                if (fullscaleHistory != hdCamera.volumetricCloudsFullscaleHistory)
                {
                    // If history size is invalid, release it
                    hdCamera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0);
                    hdCamera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1);
                    hdCamera.volumetricCloudsFullscaleHistory = fullscaleHistory;
                }

                // If history buffers are null, it means they were not allocated yet so they will potentially contain garbage at first frame.
                var historyValidity = EvaluateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value) && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0) != null;

                passData.parameters = PrepareVolumetricCloudsParameters_Accumulation(hdCamera, settings, cameraType, historyValidity, downscaling);
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.maxZMask = settings.localClouds.value ? renderGraph.defaultResources.blackTextureXR : builder.ReadTexture(maxZMask);
                passData.ambientProbeBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CloudsDynamicProbeBuffer));

                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                hdCamera.intermediateDownscaling = downscaling;
                passData.currentHistoryBuffer0 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, true, HDCameraFrameHistoryType.VolumetricClouds0, fullscaleHistory));
                passData.previousHistoryBuffer0 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, false, HDCameraFrameHistoryType.VolumetricClouds0, fullscaleHistory));
                passData.currentHistoryBuffer1 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, true, HDCameraFrameHistoryType.VolumetricClouds1, fullscaleHistory));
                passData.previousHistoryBuffer1 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, false, HDCameraFrameHistoryType.VolumetricClouds1, fullscaleHistory));
                
                if (passData.parameters.downscaleDepth)
                    passData.intermediateBufferDepth0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * downscaling, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Half Res Scene Depth" });

                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * downscaling * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Clouds Lighting" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * downscaling * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Clouds Depth" });

                passData.intermediateBufferDepth1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 1" });

                passData.intermediateColorBufferCopy = passData.parameters.needExtraColorBufferCopy ? builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GetColorBufferFormat(), enableRandomWrite = true, name = "Temporary Color Buffer" }) : renderGraph.defaultResources.blackTextureXR;

                if (passData.parameters.needsTemporaryBuffer)
                {
                    passData.intermediateBufferUpscale = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Upscaling Buffer" });
                }
                else
                {
                    passData.intermediateBufferUpscale = renderGraph.defaultResources.blackTexture;
                }

                if (passData.parameters.commonData.cameraType == TVolumetricCloudsCameraType.PlanarReflection)
                {
                    passData.intermediateBufferDepth2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 2" });
                }
                else
                {
                    passData.intermediateBufferDepth2 = renderGraph.defaultResources.blackTexture;
                }

                builder.SetRenderFunc(
                    (VolumetricCloudsAccumulationData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_Accumulation(ctx.cmd, data.parameters, data.ambientProbeBuffer,
                            data.colorBuffer, data.depthPyramid, data.motionVectors, data.volumetricLighting, data.scatteringFallbackTexture, data.maxZMask,
                            data.currentHistoryBuffer0, data.previousHistoryBuffer0, data.currentHistoryBuffer1, data.previousHistoryBuffer1,
                            data.intermediateBuffer0, data.intermediateBuffer1, data.intermediateBufferDepth0, data.intermediateBufferDepth1, data.intermediateBufferDepth2,
                            data.intermediateColorBufferCopy, data.intermediateBufferUpscale);
                    });

                // Push the texture to the debug menu
                PushFullScreenDebugTexture(m_RenderGraph, passData.currentHistoryBuffer0, FullScreenDebugMode.VolumetricClouds);

                // We return the volumetric clouds buffers rendered at half resolution. We should ideally return the full resolution transmittance, but
                // this should be enough for the initial case (which is the lens flares). The transmittance can be found in the alpha channel.
                return passData.currentHistoryBuffer0;
            }
        }
    }
}
