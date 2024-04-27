using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct VolumetricCloudsParameters_LowResolution
        {
            // Resolution parameters
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public int viewCount;
            public bool needExtraColorBufferCopy;

            // Used kernels
            public int depthDownscaleKernel;
            public int renderKernel;
            public int preUpscaleKernel;
            public int upscaleAndCombineKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // MSAA support
            public bool needsTemporaryBuffer;
            public Material cloudCombinePass;
        }

        VolumetricCloudsParameters_LowResolution PrepareVolumetricCloudsParameters_LowResolution(HDCamera hdCamera, int width, int height, int viewCount, bool exposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType)
        {
            VolumetricCloudsParameters_LowResolution parameters = new VolumetricCloudsParameters_LowResolution();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(exposureControl, settings, cameraType, in cloudModelData, ref parameters.commonData);

            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            // Final resolution at which the effect should be exported
            parameters.finalWidth = width;
            parameters.finalHeight = height;
            // Intermediate resolution at which the effect is accumulated
            parameters.intermediateWidth = Mathf.RoundToInt(0.5f * width);
            parameters.intermediateHeight = Mathf.RoundToInt(0.5f * height);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.25f * width);
            parameters.traceHeight = Mathf.RoundToInt(0.25f * height);
            parameters.viewCount = viewCount;

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
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.preUpscaleKernel = m_PreUpscaleCloudsKernel;
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
            cameraData.enableIntegration = false;
            UpdateShaderVariableslClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            return parameters;
        }

        static void TraceVolumetricClouds_LowResolution(CommandBuffer cmd, VolumetricCloudsParameters_LowResolution parameters, ComputeBuffer ambientProbeBuffer,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle volumetricLightingTexture, RTHandle scatteringFallbackTexture, RTHandle maxZMask,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateLightingBuffer1, RTHandle intermediateLightingBuffer2, RTHandle intermediateDepthBuffer0, RTHandle intermediateDepthBuffer1,
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

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);
            if (parameters.commonData.localClouds)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsDepthDownscale)))
                {
                    // Compute the alternative version of the mip 1 of the depth (min instead of max that is required to handle high frequency meshes (vegetation, hair)
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._DepthTexture, depthPyramid);
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, HDShaderIDs._HalfResDepthBufferRW, intermediateDepthBuffer0);
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount);
                }
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
                cmd.SetComputeBufferParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, ambientProbeBuffer);

                // Output buffers
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsDepthTextureRW, intermediateDepthBuffer1);

                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, traceTX, traceTY, parameters.viewCount);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);
            }

            // We only reproject for realtime clouds
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPreUpscale)))
            {
                // Re-project the result from the previous frame
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, HDShaderIDs._CloudsLightingTexture, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, HDShaderIDs._CloudsDepthTexture, intermediateDepthBuffer1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, HDShaderIDs._HalfResDepthBuffer, intermediateDepthBuffer0);

                // History buffers
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, HDShaderIDs._CloudsAdditionalTextureRW, intermediateLightingBuffer2);

                // Re-project from the previous frame
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.preUpscaleKernel, intermediateTX, intermediateTY, parameters.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscaleAndCombine)))
            {
                if (parameters.needExtraColorBufferCopy)
                    HDUtils.BlitCameraTexture(cmd, colorBuffer, intermediateColorBuffer);

                // Define which kernel to use
                int targetKernel = parameters.upscaleAndCombineKernel;
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._VolumetricCloudsTexture, intermediateLightingBuffer1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._DepthStatusTexture, intermediateLightingBuffer2);

                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._CameraColorTexture, intermediateColorBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
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

                    // Provide this second upscaling + combine strategy in case a temporary buffer is requested (i.e. MSAA).
                    // In the case of an MSAA color target, we cannot use the in-place blending of the clouds with the color target.
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, targetKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Perform the upscale into an intermediate buffer.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, targetKernel, finalTX, finalTY, parameters.viewCount);

                    parameters.cloudCombinePass.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Composite the clouds into the MSAA target via hardware blending.
                    HDUtils.DrawFullScreen(cmd, parameters.cloudCombinePass, colorBuffer, null, 0);

                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", false);
                }
                else
                {
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.upscaleAndCombineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, colorBuffer);

                    // Perform the upscale and combine with the color buffer in place.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, targetKernel, finalTX, finalTY, parameters.viewCount);
                }
            }

            // Reset all the multi-compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);
        }

        class VolumetricCloudsLowResolutionData
        {
            public VolumetricCloudsParameters_LowResolution parameters;

            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle maxZMask;
            public ComputeBufferHandle ambientProbeBuffer;

            public TextureHandle volumetricLighting;
            public TextureHandle scatteringFallbackTexture;

            public TextureHandle intermediateLightingBuffer0;
            public TextureHandle intermediateLightingBuffer1;
            public TextureHandle intermediateLightingBuffer2;
            public TextureHandle intermediateBufferDepth0;
            public TextureHandle intermediateBufferDepth1;
            public TextureHandle intermediateBufferUpscale;

            public TextureHandle intermediateColorBufferCopy;
        }

        TextureHandle RenderVolumetricClouds_LowResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting, TextureHandle maxZMask)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsLowResolutionData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                passData.parameters = PrepareVolumetricCloudsParameters_LowResolution(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight, hdCamera.viewCount, hdCamera.exposureControlFS, settings, cameraType);
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.maxZMask = settings.localClouds.value ? renderGraph.defaultResources.blackTextureXR : builder.ReadTexture(maxZMask);
                passData.ambientProbeBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_CloudsDynamicProbeBuffer));

                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                passData.intermediateLightingBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 0" });
                passData.intermediateLightingBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 1 " });
                passData.intermediateLightingBuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 2 " });
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

                builder.SetRenderFunc(
                    (VolumetricCloudsLowResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_LowResolution(ctx.cmd, data.parameters, data.ambientProbeBuffer,
                            data.colorBuffer, data.depthPyramid, data.volumetricLighting, data.scatteringFallbackTexture, data.maxZMask,
                            data.intermediateLightingBuffer0, data.intermediateLightingBuffer1, data.intermediateLightingBuffer2, data.intermediateBufferDepth0, data.intermediateBufferDepth1,
                            data.intermediateColorBufferCopy, data.intermediateBufferUpscale);
                    });

                // In the case of reflection probes, we don't expect any pass that will need the transmittance mask of the clouds so we return white.
                return renderGraph.defaultResources.whiteTextureXR;
            }
        }
    }
}
