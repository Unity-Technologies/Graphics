using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = true;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            // If this is a default camera, we want the improved blending, otherwise we don't (in the case of a planar)
            float perceptualBlending = settings.perceptualBlending.value;
            parameters.commonData.cloudsCB._ImprovedTransmittanceBlend = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default ? perceptualBlending : 0.0f;
            parameters.commonData.cloudsCB._CubicTransmittance = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default && hdCamera.msaaEnabled ? perceptualBlending : 0;

            return parameters;
        }

        static void TraceVolumetricClouds_FullResolution(CommandBuffer cmd, VolumetricCloudsParameters_FullResolution parameters, GraphicsBuffer ambientProbeBuffer,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle volumetricLightingTexture,
            RTHandle intermediateCloudsLighting, RTHandle intermediateCloudsDepth,
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
                volumetricLightingTexture, depthPyramid, ambientProbeBuffer,
                intermediateCloudsLighting, intermediateCloudsDepth);

            DoVolumetricCloudsUpscale(cmd, parameters.combineKernel, finalTX, finalTY, parameters.viewCount, in parameters.commonData,
                intermediateCloudsLighting, intermediateCloudsDepth, colorBuffer, depthPyramid,
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
            public TextureHandle volumetricLighting;

            // Intermediate buffers
            public TextureHandle intermediateLightingBuffer;
            public TextureHandle intermediateBufferDepth;

            // Output buffer
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_FullResolution(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle volumetricLighting)
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
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);

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
                            data.colorBuffer, data.depthPyramid, data.volumetricLighting,
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
