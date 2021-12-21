using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class RenderDenoisePassData
        {
            public ComputeShader blitAndExposeCS;
            public int blitAndExposeKernel;
            public TextureHandle color;
            public TextureHandle normalAOV;
            public TextureHandle albedoAOV;
            public TextureHandle motionVectorAOV;
            public TextureHandle outputTexture;
            public SubFrameManager subFrameManager;

            public int camID;
            public bool useAOV;
            public bool temporal;

            public int width;
            public int height;
            public int slices;
        }

        void RenderDenoisePass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle outputTexture)
        {
#if ENABLE_UNITY_DENOISER_PLUGIN
            using (var builder = renderGraph.AddRenderPass<RenderDenoisePassData>("Denoise Pass", out var passData))
            {
                passData.blitAndExposeCS = m_Asset.renderPipelineResources.shaders.blitAndExposeCS;
                passData.blitAndExposeKernel = passData.blitAndExposeCS.FindKernel("KMain");
                passData.subFrameManager = m_SubFrameManager;
                passData.useAOV = m_PathTracingSettings.useAOVs.value;
                passData.temporal = m_PathTracingSettings.temporal.value && hdCamera.camera.cameraType == CameraType.Game;

                // copy camera params
                passData.camID = hdCamera.camera.GetInstanceID();
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.slices = hdCamera.viewCount;

                // Grab the history buffer
                TextureHandle history = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing));

                passData.color = builder.ReadWriteTexture(history);

                passData.outputTexture = builder.WriteTexture(outputTexture);

                if (m_PathTracingSettings.useAOVs.value)
                {
                    TextureHandle albedoHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AlbedoAOV));

                    TextureHandle normalHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.NormalAOV));

                    passData.albedoAOV = builder.ReadTexture(albedoHistory);
                    passData.normalAOV = builder.ReadTexture(normalHistory);
                }

                if (m_PathTracingSettings.temporal.value)
                {
                    TextureHandle motionVectorHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.MotionVectorAOV));
                    passData.motionVectorAOV = builder.ReadTexture(motionVectorHistory);
                }

                builder.SetRenderFunc(
                (RenderDenoisePassData data, RenderGraphContext ctx) =>
                {
                    CameraData camData = data.subFrameManager.GetCameraData(data.camID);

                    camData.denoiser.type = m_PathTracingSettings.denoising.value;

                    if (camData.currentIteration == (data.subFrameManager.subFrameCount) && camData.denoiser.type != DenoiserType.None)
                    {
                        bool useAsync = false;

                        if (!camData.denoiser.denoised)
                        {
                            camData.denoiser.denoised = true;
                            m_SubFrameManager.SetCameraData(data.camID, camData);

                            camData.denoiser.DenoiseRequest(ctx.cmd, "color", data.color);

                            if (data.useAOV)
                            {
                                camData.denoiser.DenoiseRequest(ctx.cmd, "albedo", data.albedoAOV);
                                camData.denoiser.DenoiseRequest(ctx.cmd, "normal", data.normalAOV);
                            }

                            if (data.temporal)
                            {
                                camData.denoiser.DenoiseRequest(ctx.cmd, "flow", data.motionVectorAOV);
                            }

                            if (!useAsync)
                            {
                                camData.denoiser.WaitForCompletion();
                                camData.denoiser.GetResults(ctx.cmd, data.color);

                                // Blit the denoised image from the history buffer and apply exposure.
                                ctx.cmd.SetComputeTextureParam(data.blitAndExposeCS, data.blitAndExposeKernel, HDShaderIDs._InputTexture, data.color);
                                ctx.cmd.SetComputeTextureParam(data.blitAndExposeCS, data.blitAndExposeKernel, HDShaderIDs._OutputTexture, data.outputTexture);
                                ctx.cmd.DispatchCompute(data.blitAndExposeCS, data.blitAndExposeKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.slices);
                            }
                        }

                        // if denoised frame is ready, blit it
                        if (useAsync && camData.denoiser.QueryCompletion())
                        {
                            camData.denoiser.GetResults(ctx.cmd, data.color);
                        }
                    }
                });
            }
#endif
        }
    }
}
