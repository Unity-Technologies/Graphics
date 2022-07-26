using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Allocation of the first history buffer
        static RTHandle VolumetricClouds0HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory0Buffer{1}", viewName, frameIndex));
        }

        // Allocation of the second history buffer
        static RTHandle VolumetricClouds1HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory1Buffer{1}", viewName, frameIndex));
        }

        // Functions to request the history buffers
        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
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

        VolumetricCloudsParameters_Accumulation PrepareVolumetricCloudsParameters_Accumulation(HDCamera hdCamera, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, bool historyValidity)
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
            parameters.intermediateWidth = Mathf.RoundToInt(0.5f * hdCamera.actualWidth);
            parameters.intermediateHeight = Mathf.RoundToInt(0.5f * hdCamera.actualHeight);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.25f * hdCamera.actualWidth);
            parameters.traceHeight = Mathf.RoundToInt(0.25f * hdCamera.actualHeight);

            parameters.viewCount = hdCamera.viewCount;
            parameters.previousViewportSize = hdCamera.historyRTHandleProperties.previousViewportSize;
            parameters.historyValidity = historyValidity;

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
            parameters.upscaleAndCombineKernel = parameters.needExtraColorBufferCopy ? m_UpscaleAndCombineCloudsKernelColorCopy : m_UpscaleAndCombineCloudsKernelColorRW;

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

        static void TraceVolumetricClouds_Accumulation(CommandBuffer cmd, VolumetricCloudsParameters_Accumulation parameters,
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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPrepare)))
            {
                if (parameters.commonData.cameraType == TVolumetricCloudsCameraType.PlanarReflection)
                {
                    // In order to be able to work with planar
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthTexture, depthPyramid);
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, HDShaderIDs._DepthBufferRW, intermediateDepthBuffer2);
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.convertObliqueDepthKernel, finalTX, finalTY, parameters.viewCount);
                    currentDepthBuffer = intermediateDepthBuffer2;
                }

                // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._DepthTexture, currentDepthBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._HalfResDepthBufferRW, intermediateDepthBuffer0);
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount);
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

                // If history buffers are null, it means they were not allocated yet so they will potentially contain garbage at first frame.
                var historyValidity = EvaluateVolumetricCloudsHistoryValidity(hdCamera, settings.localClouds.value) && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0) != null;

                passData.parameters = PrepareVolumetricCloudsParameters_Accumulation(hdCamera, settings, cameraType, historyValidity);
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.maxZMask = settings.localClouds.value ? renderGraph.defaultResources.blackTextureXR : builder.ReadTexture(maxZMask);

                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                passData.currentHistoryBuffer0 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture0(hdCamera));
                passData.previousHistoryBuffer0 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture0(hdCamera));
                passData.currentHistoryBuffer1 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture1(hdCamera));
                passData.previousHistoryBuffer1 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture1(hdCamera));

                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 1 " });
                passData.intermediateBufferDepth0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 0" });
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
                        TraceVolumetricClouds_Accumulation(ctx.cmd, data.parameters,
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
