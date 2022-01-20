using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static uint BFRTGIFrameIndex(HDCamera hdCamera)
        {
            // Accumulation is done on 7 frames
            return hdCamera.GetCameraFrameCount() % 7;
        }

        class BFRTGIAdaptativePassData
        {
            public ComputeShader probePlacementComputeShader;
            public int uniformProbePlacementKernel;

            public int width;
            public int height;
            public int viewCount;

            public int frameIndex;

            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;

            public TextureHandle screenProbeDepthTexture;
            public TextureHandle screenProbeNormalWSTexture;
            public TextureHandle screenProbePositionRWSTexture;
        }

        public static readonly int _ScreenProbeDepthTexture = Shader.PropertyToID("_ScreenProbeDepth");
        public static readonly int _ScreenProbeNormalTexture = Shader.PropertyToID("_ScreenProbeNormalWS");
        public static readonly int _ScreenProbePositionTexture = Shader.PropertyToID("_ScreenProbePositionRWS");

        void BFRTGISetupProbePlacement(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput prepassOutput)
        {
            using (var builder = renderGraph.AddRenderPass<BFRTGIAdaptativePassData>("BF RTGI - Adaptative probe placement", out var passData, ProfilingSampler.Get(HDProfileId.BFRTGIAdaptative)))
            {
                int scaleFactor = 16;

                passData.frameIndex = (int)BFRTGIFrameIndex(hdCamera);

                passData.width = HDUtils.DivRoundUp(hdCamera.actualWidth, scaleFactor);
                passData.height = HDUtils.DivRoundUp(hdCamera.actualHeight, scaleFactor);
                passData.viewCount = hdCamera.viewCount;

                passData.probePlacementComputeShader = defaultResources.shaders.BFRTGIAdaptativePlacementCS;
                passData.uniformProbePlacementKernel = passData.probePlacementComputeShader.FindKernel("BFRTGIUniformPlacement");

                passData.depthPyramid = builder.ReadTexture(prepassOutput.depthPyramidTexture);
                passData.normalBuffer = builder.ReadTexture(prepassOutput.resolvedNormalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(prepassOutput.resolvedMotionVectorsBuffer);

                passData.screenProbeDepthTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / scaleFactor, true, false)
                { colorFormat = GraphicsFormat.R16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "Uniform Probe Placement Depth" }));

                passData.screenProbeNormalWSTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / scaleFactor, true, false)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "Uniform Probe Placement NormalWS" }));

                passData.screenProbePositionRWSTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / scaleFactor, true, false)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.clear, enableRandomWrite = true, name = "Uniform Probe Placement PositionRWS" }));

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (BFRTGIAdaptativePassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);

                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, _ScreenProbeDepthTexture, data.screenProbeDepthTexture);
                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, _ScreenProbeNormalTexture, data.screenProbeNormalWSTexture);
                        ctx.cmd.SetComputeTextureParam(data.probePlacementComputeShader, data.uniformProbePlacementKernel, _ScreenProbePositionTexture, data.screenProbePositionRWSTexture);

                        // Group size of 8
                        ctx.cmd.DispatchCompute(data.probePlacementComputeShader, data.uniformProbePlacementKernel, HDUtils.DivRoundUp(data.width, 8), HDUtils.DivRoundUp(data.height, 8), data.viewCount);
                    });
            }
        }
    }
}
