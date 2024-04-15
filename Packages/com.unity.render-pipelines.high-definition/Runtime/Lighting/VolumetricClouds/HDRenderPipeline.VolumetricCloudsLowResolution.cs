using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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

            // Used kernels
            public int depthDownscaleKernel;
            public int preUpscaleKernel;
            public int upscaleKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;
        }

        VolumetricCloudsParameters_LowResolution PrepareVolumetricCloudsParameters_LowResolution(HDCamera hdCamera, int width, int height, int viewCount, bool exposureControl, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType)
        {
            VolumetricCloudsParameters_LowResolution parameters = new VolumetricCloudsParameters_LowResolution();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera, exposureControl, settings, cameraType, in cloudModelData, ref parameters.commonData);

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

            // Compute shader and kernels
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.preUpscaleKernel = m_PreUpscaleCloudsKernel;
            parameters.upscaleKernel = hdCamera.msaaEnabled ? m_UpscaleCloudsKernel : m_UpscaleCloudsPerceptualKernel;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.traceWidth;
            cameraData.traceHeight = parameters.traceHeight;
            cameraData.intermediateWidth = parameters.intermediateWidth;
            cameraData.intermediateHeight = parameters.intermediateHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = false;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            return parameters;
        }

        static void TraceVolumetricClouds_LowResolution(CommandBuffer cmd, VolumetricCloudsParameters_LowResolution parameters, GraphicsBuffer ambientProbeBuffer,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle volumetricLightingTexture,
            RTHandle intermediateCloudsLighting, RTHandle intermediateLightingBuffer1, RTHandle intermediateLightingBuffer2,
            RTHandle halfResDepthBuffer, RTHandle intermediateCloudsDepth,
            RTHandle cloudsLighting, RTHandle cloudsDepth)
        {
            // Compute the number of tiles to evaluate
            int traceTX = HDUtils.DivRoundUp(parameters.traceWidth, 8);
            int traceTY = HDUtils.DivRoundUp(parameters.traceHeight, 8);

            // Compute the number of tiles to evaluate
            int intermediateTX = HDUtils.DivRoundUp(parameters.intermediateWidth, 8);
            int intermediateTY = HDUtils.DivRoundUp(parameters.intermediateHeight, 8);

            // Compute the number of tiles to evaluate
            int finalTX = HDUtils.DivRoundUp(parameters.finalWidth, 8);
            int finalTY = HDUtils.DivRoundUp(parameters.finalHeight, 8);

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // Bind the constant buffer
            ConstantBuffer.UpdateData(cmd, parameters.commonData.cloudsCB);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsTraceCS, HDShaderIDs._ShaderVariablesClouds);

            // Depth downscale
            DoVolumetricCloudsDepthDownscale(cmd, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                depthPyramid, halfResDepthBuffer);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, traceTX, traceTY, parameters.viewCount, in parameters.commonData,
                volumetricLightingTexture, halfResDepthBuffer, ambientProbeBuffer,
                intermediateCloudsLighting, intermediateCloudsDepth);

            // We only reproject for realtime clouds
            DoVolumetricCloudsReproject(cmd, parameters.preUpscaleKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                intermediateCloudsLighting, intermediateCloudsDepth, halfResDepthBuffer,
                false, false, null, null, // no history reprojection
                intermediateLightingBuffer1, intermediateLightingBuffer2);

            DoVolumetricCloudsUpscale(cmd, parameters.upscaleKernel, finalTX, finalTY, parameters.viewCount, in parameters.commonData,
                intermediateLightingBuffer1, intermediateLightingBuffer2, colorBuffer, depthPyramid,
                cloudsLighting, cloudsDepth);
        }

        class VolumetricCloudsLowResolutionData
        {
            public VolumetricCloudsParameters_LowResolution parameters;

            // Input Buffers
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle maxZMask;
            public BufferHandle ambientProbeBuffer;
            public TextureHandle volumetricLighting;

            // Intermediate buffers
            public TextureHandle halfResCloudsLighting;
            public TextureHandle intermediateLightingBuffer1;
            public TextureHandle intermediateLightingBuffer2;
            public TextureHandle halfResDepthBuffer;
            public TextureHandle halfResCloudsDepth;

            // Output buffer
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_LowResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle volumetricLighting)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsLowResolutionData>("Volumetric Clouds Low Resolution", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                // Parameters
                passData.parameters = PrepareVolumetricCloudsParameters_LowResolution(hdCamera, hdCamera.actualWidth, hdCamera.actualHeight, hdCamera.viewCount, hdCamera.exposureControlFS, settings, cameraType);

                // Input buffers
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_CloudsDynamicProbeBuffer));
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);

                // Intermediate buffers
                passData.halfResCloudsLighting = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Half Res Clouds Lighting" });
                passData.intermediateLightingBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 1" });
                passData.intermediateLightingBuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporary Clouds Lighting Buffer 2" });
                passData.halfResDepthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Half Res Scene Depth" });
                passData.halfResCloudsDepth = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Half Res Clouds Depth" });

                // Output of the clouds
                passData.cloudsLighting = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Lighting Texture" }));
                passData.cloudsDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Depth Texture" }));

                builder.SetRenderFunc(
                    (VolumetricCloudsLowResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_LowResolution(ctx.cmd, data.parameters, data.ambientProbeBuffer,
                            data.colorBuffer, data.depthPyramid, data.volumetricLighting,
                            data.halfResCloudsLighting, data.intermediateLightingBuffer1, data.intermediateLightingBuffer2, data.halfResDepthBuffer, data.halfResCloudsDepth,
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
