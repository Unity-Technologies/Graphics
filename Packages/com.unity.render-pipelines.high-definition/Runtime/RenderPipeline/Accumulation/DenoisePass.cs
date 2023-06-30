using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

// Enable the denoising code path only on windows 64
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
using UnityEngine.Rendering.Denoising;
#endif

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
            public TextureHandle denoiseHistory;
            public SubFrameManager subFrameManager;

            public int camID;
            public bool useAOV;
            public bool temporal;
            public bool async;

            public int width;
            public int height;
            public int slices;
        }

        void RenderDenoisePass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle outputTexture)
        {
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            // Early exit if there is no denoising
            if (m_PathTracingSettings.denoising.value == HDDenoiserType.None)
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<RenderDenoisePassData>("Denoise Pass", out var passData))
            {
                passData.blitAndExposeCS = m_Asset.renderPipelineResources.shaders.blitAndExposeCS;
                passData.blitAndExposeKernel = passData.blitAndExposeCS.FindKernel("KMain");
                passData.subFrameManager = m_SubFrameManager;
                // Note: for now we enable AOVs when temporal is enabled, because this seems to work better with Optix.
                passData.useAOV = m_PathTracingSettings.useAOVs.value || m_PathTracingSettings.temporal.value;
                passData.temporal = m_PathTracingSettings.temporal.value && hdCamera.camera.cameraType == CameraType.Game && m_SubFrameManager.isRecording;
                passData.async = m_PathTracingSettings.asyncDenoising.value && hdCamera.camera.cameraType == CameraType.SceneView;

                // copy camera params
                passData.camID = hdCamera.camera.GetInstanceID();
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.slices = hdCamera.viewCount;

                // Grab the history buffer
                TextureHandle ptAccumulation = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing));
                TextureHandle denoiseHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.DenoiseHistory)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.DenoiseHistory, PathTracingHistoryBufferAllocatorFunction, 1));

                passData.color = builder.ReadWriteTexture(ptAccumulation);
                passData.outputTexture = builder.WriteTexture(outputTexture);
                passData.denoiseHistory = builder.ReadTexture(denoiseHistory);

                if (passData.useAOV)
                {
                    TextureHandle albedoHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AlbedoAOV));

                    TextureHandle normalHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.NormalAOV));

                    passData.albedoAOV = builder.ReadTexture(albedoHistory);
                    passData.normalAOV = builder.ReadTexture(normalHistory);
                }

                if (passData.temporal)
                {
                    TextureHandle motionVectorHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.MotionVectorAOV));
                    passData.motionVectorAOV = builder.ReadTexture(motionVectorHistory);
                }

                builder.SetRenderFunc(
                (RenderDenoisePassData data, RenderGraphContext ctx) =>
                {
                    CameraData camData = data.subFrameManager.GetCameraData(data.camID);

                    // If we don't have any pending async denoise and we don't do temporal denoising, dispose any existing denoiser state
                    if (camData.denoiser.type != DenoiserType.None && m_PathTracingSettings.temporal.value != true && camData.activeDenoiseRequest == false)
                    {
                        camData.denoiser.DisposeDenoiser();
                        m_SubFrameManager.SetCameraData(data.camID, camData);
                    }

                    camData.denoiser.Init((DenoiserType)m_PathTracingSettings.denoising.value, data.width, data.height);

                    if (camData.currentIteration >= (data.subFrameManager.subFrameCount) && camData.denoiser.type != DenoiserType.None)
                    {
                        if (!camData.validDenoiseHistory)
                        {
                            if (!camData.activeDenoiseRequest)
                            {
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
                                camData.activeDenoiseRequest = true;
                                camData.discardDenoiseRequest = false;
                            }

                            if (!data.async)
                            {
                                camData.denoiser.WaitForCompletion(ctx.renderContext, ctx.cmd);

                                Denoiser.State ret = camData.denoiser.GetResults(ctx.cmd, data.denoiseHistory);
                                camData.validDenoiseHistory = (ret == Denoiser.State.Success);
                                camData.activeDenoiseRequest = false;
                            }
                            else
                            {
                                if (camData.activeDenoiseRequest && camData.denoiser.QueryCompletion() != Denoiser.State.Executing)
                                {
                                    Denoiser.State ret = camData.denoiser.GetResults(ctx.cmd, data.denoiseHistory);
                                    camData.validDenoiseHistory = (ret == Denoiser.State.Success) && (camData.discardDenoiseRequest == false);
                                    camData.activeDenoiseRequest = false;
                                }
                            }

                            m_SubFrameManager.SetCameraData(data.camID, camData);
                        }

                        if (camData.validDenoiseHistory)
                        {
                            // Blit the denoised image from the history buffer and apply exposure.
                            ctx.cmd.SetComputeTextureParam(data.blitAndExposeCS, data.blitAndExposeKernel, HDShaderIDs._InputTexture, data.denoiseHistory);
                            ctx.cmd.SetComputeTextureParam(data.blitAndExposeCS, data.blitAndExposeKernel, HDShaderIDs._OutputTexture, data.outputTexture);
                            ctx.cmd.DispatchCompute(data.blitAndExposeCS, data.blitAndExposeKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.slices);
                        }
                    }
                });
            }
#endif
        }
    }
}
