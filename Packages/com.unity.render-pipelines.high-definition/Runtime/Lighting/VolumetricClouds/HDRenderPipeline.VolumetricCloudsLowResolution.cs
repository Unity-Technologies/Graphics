using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
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
            parameters.preUpscaleKernel = m_PreUpscaleCloudsKernel;
            parameters.upscaleKernel = m_UpscaleCloudsKernel;

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

        static void TraceVolumetricClouds_LowResolution(CommandBuffer cmd, VolumetricCloudsParameters_LowResolution parameters,
            GraphicsBuffer ambientProbeBuffer, RTHandle colorBuffer, RTHandle depthPyramid,
            RTHandle tracedCloudsLighting, RTHandle tracedCloudsDepth,
            RTHandle intermediateLightingBuffer1, RTHandle intermediateLightingBuffer2,
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

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, traceTX, traceTY, parameters.viewCount, in parameters.commonData,
                ambientProbeBuffer, colorBuffer, depthPyramid,
                tracedCloudsLighting, tracedCloudsDepth);

            // We only reproject for realtime clouds
            DoVolumetricCloudsReproject(cmd, parameters.preUpscaleKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                tracedCloudsLighting, tracedCloudsDepth, depthPyramid,
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

            // Intermediate buffers
            public TextureHandle tracedCloudsLighting;
            public TextureHandle intermediateLightingBuffer1;
            public TextureHandle intermediateLightingBuffer2;
            public TextureHandle tracedCloudsDepth;

            // Output buffer
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_LowResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid)
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

                CreateTracingTextures(renderGraph, builder, settings, 0.25f, out passData.tracedCloudsLighting, out passData.tracedCloudsDepth);
                CreateIntermediateTextures(renderGraph, builder, settings, out passData.intermediateLightingBuffer1, out passData.intermediateLightingBuffer2);
                CreateOutputTextures(renderGraph, builder, settings, out passData.cloudsLighting, out passData.cloudsDepth);

                builder.SetRenderFunc(
                    (VolumetricCloudsLowResolutionData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_LowResolution(ctx.cmd, data.parameters,
                            data.ambientProbeBuffer, data.colorBuffer, data.depthPyramid,
                            data.tracedCloudsLighting, data.tracedCloudsDepth,
                            data.intermediateLightingBuffer1, data.intermediateLightingBuffer2,
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
