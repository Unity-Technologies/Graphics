using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseDenoiser
    {
        // Resources used for the de-noiser
        ComputeShader m_DiffuseDenoiser;
        Texture m_OwenScrambleRGBA;

        HDRenderPipeline m_RenderPipeline;

        // Kernels that may be required
        int m_BilateralFilterSingleKernel;
        int m_BilateralFilterColorKernel;
        int m_GatherSingleKernel;
        int m_GatherColorKernel;
        ComputeBuffer m_PointDistribution;
        static internal float[] pointDistribution = new float[] { 0.647285104f, -0.534139216f, 0.201738372f, 0.260410696f,
                                                                  -0.443308681f, 0.259598345f, 0.0f, 0.0f,
                                                                    0.851900041f, 0.214261428f, 0.0376310274f, -0.406103343f,
                                                                    -0.357411921f, -0.525219262f, -0.00147355383f, 0.239211172f,
                                                                    -0.463947058f, 0.646911025f, -0.0379408896f, -0.291660219f,
                                                                    0.405679494f, -0.473511368f, 0.0473965593f, 0.0411158539f,
                                                                    -0.963973522f, -0.155723229f, -0.444706231f, 0.141471207f,
                                                                    0.0980135575f, 0.687162697f, 0.156328082f, -0.0518609099f};

        public void Init(HDRenderPipelineRuntimeResources rpResources, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_DiffuseDenoiser = rpResources.shaders.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            m_RenderPipeline = renderPipeline;

            // Grab all the kernels we'll eventually need
            m_BilateralFilterSingleKernel = m_DiffuseDenoiser.FindKernel("BilateralFilterSingle");
            m_BilateralFilterColorKernel = m_DiffuseDenoiser.FindKernel("BilateralFilterColor");
            m_GatherSingleKernel = m_DiffuseDenoiser.FindKernel("GatherSingle");
            m_GatherColorKernel = m_DiffuseDenoiser.FindKernel("GatherColor");
            m_PointDistribution = new ComputeBuffer(16 * 2, sizeof(float));
            m_PointDistribution.SetData(pointDistribution);
        }

        public void Release()
        {
            CoreUtils.SafeRelease(m_PointDistribution);
        }

        class DiffuseDenoiserPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Denoising parameters
            public float pixelSpreadTangent;
            public float kernelSize;
            public bool halfResolutionFilter;
            public bool jitterFilter;
            public int frameIndex;
            public bool fullResolutionInput;

            // Kernels
            public int bilateralFilterKernel;
            public int gatherKernel;

            // Other parameters
            public ComputeBufferHandle pointDistribution;
            public ComputeShader diffuseDenoiserCS;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle outputBuffer;
        }

        internal struct DiffuseDenoiserParameters
        {
            public bool singleChannel;
            public float kernelSize;
            public bool halfResolutionFilter;
            public bool jitterFilter;
            public bool fullResolutionInput;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, DiffuseDenoiserParameters denoiserParams,
            TextureHandle noisyBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle outputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DiffuseDenoiserPassData>("DiffuseDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                // Camera parameters
                if (denoiserParams.fullResolutionInput)
                {
                    passData.texWidth = hdCamera.actualWidth;
                    passData.texHeight = hdCamera.actualHeight;
                }
                else
                {
                    passData.texWidth = hdCamera.actualWidth / 2;
                    passData.texHeight = hdCamera.actualHeight / 2;
                }
                passData.viewCount = hdCamera.viewCount;

                // Parameters
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, passData.texWidth, passData.texHeight);
                passData.kernelSize = denoiserParams.kernelSize;
                passData.halfResolutionFilter = denoiserParams.halfResolutionFilter;
                passData.jitterFilter = denoiserParams.jitterFilter;
                passData.frameIndex = m_RenderPipeline.RayTracingFrameIndex(hdCamera);
                passData.fullResolutionInput = denoiserParams.fullResolutionInput;

                // Kernels
                passData.bilateralFilterKernel = denoiserParams.singleChannel ? m_BilateralFilterSingleKernel : m_BilateralFilterColorKernel;
                passData.gatherKernel = denoiserParams.singleChannel ? m_GatherSingleKernel : m_GatherColorKernel;

                // Other parameters
                passData.diffuseDenoiserCS = m_DiffuseDenoiser;

                passData.pointDistribution = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_PointDistribution));
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "DiffuseDenoiserIntermediate" });
                passData.outputBuffer = builder.WriteTexture(outputBuffer);

                builder.SetRenderFunc(
                    (DiffuseDenoiserPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (data.texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (data.texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Request the intermediate buffers that we need
                        ctx.cmd.SetComputeFloatParam(data.diffuseDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.kernelSize);
                        ctx.cmd.SetComputeBufferParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._PointDistribution, data.pointDistribution);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DenoiseOutputTextureRW, data.halfResolutionFilter ? data.intermediateBuffer : data.outputBuffer);
                        ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._HalfResolutionFilter, data.halfResolutionFilter ? 1 : 0);
                        ctx.cmd.SetComputeFloatParam(data.diffuseDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
                        if (data.jitterFilter)
                            ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, (data.frameIndex % 4));
                        else
                            ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, -1);

                        CoreUtils.SetKeyword(ctx.cmd, "FULL_RESOLUTION_INPUT", data.fullResolutionInput);
                        ctx.cmd.DispatchCompute(data.diffuseDenoiserCS, data.bilateralFilterKernel, numTilesX, numTilesY, data.viewCount);

                        if (data.halfResolutionFilter)
                        {
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer);
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);
                            ctx.cmd.DispatchCompute(data.diffuseDenoiserCS, data.gatherKernel, numTilesX, numTilesY, data.viewCount);
                        }
                        CoreUtils.SetKeyword(ctx.cmd, "FULL_RESOLUTION_INPUT", false);
                    });
                return passData.outputBuffer;
            }
        }
    }
}
