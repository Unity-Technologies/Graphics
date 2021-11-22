using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct VolumetricCloudsParameters_FullResolution
        {
            // Resolution parameters
            public int finalWidth;
            public int finalHeight;
            public int viewCount;

            // Compute shader and kernels
            public int renderKernel;
            public int combineKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // MSAA support
            public bool needsTemporaryBuffer;
            public bool needExtraColorBufferCopy;
            public Matrix4x4 pixelCoordToViewDirMatrix;
            public Material cloudCombinePass;
        }

        VolumetricCloudsParameters_FullResolution PrepareVolumetricCloudsParameters_FullResolution(HDCamera hdCamera, int width, int height, int viewCount, bool exposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType)
        {
            VolumetricCloudsParameters_FullResolution parameters = new VolumetricCloudsParameters_FullResolution();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(exposureControl, settings, cameraType, in cloudModelData, ref parameters.commonData);

            // If this is a baked reflection, we run everything at full res
            parameters.finalWidth = width;
            parameters.finalHeight = height;
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
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.combineKernel = parameters.needExtraColorBufferCopy ? m_CombineCloudsKernelColorCopy : m_CombineCloudsKernelColorRW;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.finalWidth;
            cameraData.traceHeight = parameters.finalHeight;
            cameraData.intermediateWidth = parameters.finalWidth;
            cameraData.intermediateHeight = parameters.finalHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.viewCount = parameters.viewCount;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = true;
            UpdateShaderVariableslClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            return parameters;
        }

        static void TraceVolumetricClouds_FullResolution(CommandBuffer cmd, VolumetricCloudsParameters_FullResolution parameters,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle volumetricLightingTexture, RTHandle scatteringFallbackTexture, RTHandle maxZMask,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateDepthBuffer0,
            RTHandle intermediateColorBuffer, RTHandle intermediateUpscaleBuffer)
        {
            // Compute the number of tiles to evaluate
            int finalTX = (parameters.finalWidth + (8 - 1)) / 8;
            int finalTY = (parameters.finalHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // Set the multi compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.commonData.localClouds);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                // Ray-march the clouds for this frame
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", parameters.commonData.cloudsCB._PhysicallyBasedSun == 1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._MaxZMaskTexture, maxZMask);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsSourceDepth, depthPyramid);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._Worley128RGBA, parameters.commonData.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._ErosionNoise, parameters.commonData.erosionNoise);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudMapTexture, parameters.commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudLutTexture, parameters.commonData.cloudLutTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, HDShaderIDs._CloudsDepthTextureRW, intermediateDepthBuffer0);
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.renderKernel, finalTX, finalTY, parameters.viewCount);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscaleAndCombine)))
            {
                if (parameters.needExtraColorBufferCopy)
                    HDUtils.BlitCameraTexture(cmd, colorBuffer, intermediateColorBuffer);

                // Define which kernel to use
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsTexture, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._DepthStatusTexture, intermediateDepthBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._CameraColorTexture, intermediateColorBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
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
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Perform the upscale into an intermediate buffer.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, finalTX, finalTY, parameters.viewCount);

                    parameters.cloudCombinePass.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, intermediateUpscaleBuffer);

                    // Composite the clouds into the MSAA target via hardware blending.
                    HDUtils.DrawFullScreen(cmd, parameters.cloudCombinePass, colorBuffer, null, 0);

                    CoreUtils.SetKeyword(cmd, "USE_INTERMEDIATE_BUFFER", false);
                }
                else
                {
                    cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsUpscaleTextureRW, colorBuffer);

                    // Perform the upscale and combine with the color buffer in place.
                    cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, finalTX, finalTY, parameters.viewCount);
                }
            }

            // Reset all the multi-compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);
        }

        class VolumetricCloudsFullResolutionData
        {
            public VolumetricCloudsParameters_FullResolution parameters;

            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle maxZMask;

            public TextureHandle volumetricLighting;
            public TextureHandle scatteringFallbackTexture;

            public TextureHandle intermediateLightingBuffer;
            public TextureHandle intermediateBufferDepth;
            public TextureHandle intermediateBufferUpscale;

            public TextureHandle intermediateColorBufferCopy;
        }

        TextureHandle RenderVolumetricClouds_FullResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting, TextureHandle maxZMask)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsFullResolutionData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                passData.parameters = PrepareVolumetricCloudsParameters_FullResolution(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight, hdCamera.viewCount, hdCamera.exposureControlFS, settings, cameraType);
                passData.colorBuffer = builder.ReadTexture(builder.WriteTexture(colorBuffer));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.maxZMask = settings.localClouds.value ? renderGraph.defaultResources.blackTextureXR : builder.ReadTexture(maxZMask);

                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                passData.intermediateLightingBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 0" });
                passData.intermediateBufferDepth = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 0" });

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
                    (VolumetricCloudsFullResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_FullResolution(ctx.cmd, data.parameters,
                            data.colorBuffer, data.depthPyramid, data.volumetricLighting, data.scatteringFallbackTexture, data.maxZMask,
                            data.intermediateLightingBuffer, data.intermediateBufferDepth,
                            data.intermediateColorBufferCopy, data.intermediateBufferUpscale);
                    });

                // In the case of reflection probes, we don't expect any pass that will need the transmittance mask of the clouds so we return white.
                return renderGraph.defaultResources.whiteTextureXR;
            }
        }
    }
}
