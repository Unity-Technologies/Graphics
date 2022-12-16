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

            // Compute shader and kernels
            parameters.renderKernel = m_CloudRenderKernel;
            parameters.combineKernel = hdCamera.msaaEnabled ? m_CombineCloudsKernel : m_CombineCloudsPerceptualKernel;

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

            // If this is a default camera, we want the improved blending, otherwise we don't (in the case of a planar)
            float perceptualBlending = settings.perceptualBlending.value;
            parameters.commonData.cloudsCB._ImprovedTransmittanceBlend = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default ? perceptualBlending : 0.0f;
            parameters.commonData.cloudsCB._CubicTransmittance = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default && hdCamera.msaaEnabled ? perceptualBlending : 0;

            return parameters;
        }

        static void TraceVolumetricClouds_FullResolution(CommandBuffer cmd, VolumetricCloudsParameters_FullResolution parameters, GraphicsBuffer ambientProbeBuffer,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle volumetricLightingTexture, RTHandle scatteringFallbackTexture, RTHandle maxZMask,
            RTHandle intermediateLightingBuffer0, RTHandle intermediateDepthBuffer0,
            RTHandle cloudsLighting, RTHandle cloudsDepth)
        {
            // Compute the number of tiles to evaluate
            int finalTX = (parameters.finalWidth + (8 - 1)) / 8;
            int finalTY = (parameters.finalHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // Set the multi compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.commonData.localClouds);
            CoreUtils.SetKeyword(cmd, "CLOUDS_MICRO_EROSION", parameters.commonData.microErosion);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.commonData.volumetricCloudsTraceCS, HDShaderIDs._ShaderVariablesClouds);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
            {
                // Ray-march the clouds for this frame
                CoreUtils.SetKeyword(cmd, "CLOUDS_SIMPLE_PRESET", parameters.commonData.simplePreset);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", parameters.commonData.cloudsCB._PhysicallyBasedSun == 1);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._MaxZMaskTexture, maxZMask);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsSourceDepth, depthPyramid);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._Worley128RGBA, parameters.commonData.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._ErosionNoise, parameters.commonData.erosionNoise);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._CloudMapTexture, parameters.commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._CloudLutTexture, parameters.commonData.cloudLutTexture);
                cmd.SetComputeBufferParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, ambientProbeBuffer);

                // Output buffers
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._CloudsLightingTextureRW, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, HDShaderIDs._CloudsDepthTextureRW, intermediateDepthBuffer0);

                cmd.DispatchCompute(parameters.commonData.volumetricCloudsTraceCS, parameters.renderKernel, finalTX, finalTY, parameters.viewCount);
                CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);
                CoreUtils.SetKeyword(cmd, "CLOUDS_SIMPLE_PRESET", false);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscale)))
            {
                // Define which kernel to use
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsTexture, intermediateLightingBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._DepthStatusTexture, intermediateDepthBuffer0);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._CameraColorTexture, colorBuffer);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._DepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VBufferLighting, volumetricLightingTexture);
                if (parameters.commonData.cloudsCB._PhysicallyBasedSun == 0)
                {
                    // This has to be done in the global space given that the "correct" one happens in the global space.
                    // If we do it in the local space, there are some cases when the previous frames local take precedence over the current frame global one.
                    cmd.SetGlobalTexture(HDShaderIDs._AirSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._AerosolSingleScatteringTexture, scatteringFallbackTexture);
                    cmd.SetGlobalTexture(HDShaderIDs._MultipleScatteringTexture, scatteringFallbackTexture);
                }

                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsLightingTextureRW, cloudsLighting);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, HDShaderIDs._VolumetricCloudsDepthTextureRW, cloudsDepth);
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.combineKernel, finalTX, finalTY, parameters.viewCount);
            }

            // Reset all the multi-compiles
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);
            CoreUtils.SetKeyword(cmd, "CLOUDS_MICRO_EROSION", false);
        }

        class VolumetricCloudsFullResolutionData
        {
            // Parameters
            public VolumetricCloudsParameters_FullResolution parameters;

            // Input buffers
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle maxZMask;
            public BufferHandle ambientProbeBuffer;
            public TextureHandle volumetricLighting;
            public TextureHandle scatteringFallbackTexture;

            // Intermediate buffers
            public TextureHandle intermediateLightingBuffer;
            public TextureHandle intermediateBufferDepth;

            // Output buffer
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_FullResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting, TextureHandle maxZMask)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsFullResolutionData>("Generating the rays for RTR", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                // Parameters
                passData.parameters = PrepareVolumetricCloudsParameters_FullResolution(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight, hdCamera.viewCount, hdCamera.exposureControlFS, settings, cameraType);

                // Input buffers
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.maxZMask = settings.localClouds.value ? renderGraph.defaultResources.blackTextureXR : builder.ReadTexture(maxZMask);
                passData.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_CloudsDynamicProbeBuffer));
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.scatteringFallbackTexture = renderGraph.defaultResources.blackTexture3DXR;

                passData.intermediateLightingBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 0" });
                passData.intermediateBufferDepth = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Temporary Clouds Depth Buffer 0" });

                // Output of the clouds
                passData.cloudsLighting = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Lighting Texture" }));
                passData.cloudsDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Depth Texture" }));

                builder.SetRenderFunc(
                    (VolumetricCloudsFullResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_FullResolution(ctx.cmd, data.parameters, data.ambientProbeBuffer,
                            data.colorBuffer, data.depthPyramid, data.volumetricLighting, data.scatteringFallbackTexture, data.maxZMask,
                            data.intermediateLightingBuffer, data.intermediateBufferDepth,
                            data.cloudsLighting, data.cloudsDepth);
                    });

                // Pack and return
                VolumetricCloudsOutput cloudsData = new VolumetricCloudsOutput();
                cloudsData.lightingBuffer = passData.cloudsLighting;
                cloudsData.depthBuffer = passData.cloudsDepth;
                cloudsData.valid = true;
                return cloudsData;
            }
        }
    }
}
