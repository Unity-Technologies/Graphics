using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
    {
        struct VolumetricCloudsParameters_FullResolution
        {
            // Resolution parameters
            public int finalWidth;
            public int finalHeight;
            public int viewCount;

            // Compute shader and kernels
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
            FillVolumetricCloudsCommonData(hdCamera, exposureControl, settings, cameraType, in cloudModelData, ref parameters.commonData);

            // If this is a baked reflection, we run everything at full res
            parameters.finalWidth = width;
            parameters.finalHeight = height;
            parameters.viewCount = viewCount;

            // Compute shader and kernels
            parameters.combineKernel = parameters.commonData.perceptualBlending ? m_CombineCloudsKernel : m_CombineCloudsPerceptualKernel;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.finalWidth;
            cameraData.traceHeight = parameters.finalHeight;
            cameraData.intermediateWidth = parameters.finalWidth;
            cameraData.intermediateHeight = parameters.finalHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = true;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            return parameters;
        }

        static void TraceVolumetricClouds_FullResolution(CommandBuffer cmd, VolumetricCloudsParameters_FullResolution parameters,
            GraphicsBuffer ambientProbeBuffer, RTHandle colorBuffer, RTHandle depthPyramid,
            RTHandle cloudsLighting, RTHandle cloudsDepth)
        {
            // Compute the number of tiles to evaluate
            int finalTX = HDUtils.DivRoundUp(parameters.finalWidth, 8);
            int finalTY = HDUtils.DivRoundUp(parameters.finalHeight, 8);

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // Bind the constant buffer
            ConstantBuffer.UpdateData(cmd, parameters.commonData.cloudsCB);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsTraceCS, HDShaderIDs._ShaderVariablesClouds);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, finalTX, finalTY, parameters.viewCount, in parameters.commonData,
                ambientProbeBuffer, colorBuffer, depthPyramid,
                cloudsLighting, cloudsDepth);
        }

        class VolumetricCloudsFullResolutionData
        {
            // Parameters
            public VolumetricCloudsParameters_FullResolution parameters;

            // Input buffers
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public BufferHandle ambientProbeBuffer;

            // Output buffer
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_FullResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsFullResolutionData>("Volumetric Clouds Full Resolution", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                // Parameters
                passData.parameters = PrepareVolumetricCloudsParameters_FullResolution(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight, hdCamera.viewCount, hdCamera.exposureControlFS, settings, cameraType);

                // Input buffers
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_CloudsDynamicProbeBuffer));

                CreateOutputTextures(renderGraph, builder, settings, out passData.cloudsLighting, out passData.cloudsDepth);

                builder.SetRenderFunc(
                    (VolumetricCloudsFullResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_FullResolution(ctx.cmd, data.parameters,
                            data.ambientProbeBuffer, data.colorBuffer, data.depthPyramid,
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
