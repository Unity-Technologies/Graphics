using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
            public int blitAddAndExposeKernel;
            public int accumAndExposeKernel;
            public TextureHandle color;
            public TextureHandle volumetricFogHistory;
            public TextureHandle normalAOV;
            public TextureHandle albedoAOV;
            public TextureHandle motionVectorAOV;
            public TextureHandle outputTexture;
            public TextureHandle denoisedHistory;
            public TextureHandle denoisedVolumetricFogHistory;
            public SubFrameManager subFrameManager;
            public ScriptableRenderContext renderContext;

            public int camID;
            public bool useAOV;
            public bool denoiseVolumetricFog;
            public bool temporal;
            public bool async;

            public int width;
            public int height;
            public int slices;
        }

        void RenderDenoisePass(RenderGraph renderGraph, ScriptableRenderContext renderContext, HDCamera hdCamera, in TextureHandle outputTexture)
        {
#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            // Early exit if there is no denoising
            if (m_PathTracingSettings.denoising.value == HDDenoiserType.None)
            {
                return;
            }

            using (var builder = renderGraph.AddUnsafePass<RenderDenoisePassData>("Denoise Pass", out var passData))
            {
                passData.blitAndExposeCS = runtimeShaders.blitAndExposeCS;
                passData.blitAndExposeKernel = passData.blitAndExposeCS.FindKernel("KMain");
                passData.subFrameManager = m_SubFrameManager;
                // Note: for now we enable AOVs when temporal is enabled, because this seems to work better with Optix.
                passData.useAOV = m_PathTracingSettings.useAOVs.value || m_PathTracingSettings.temporal.value;
                passData.temporal = m_PathTracingSettings.temporal.value && hdCamera.camera.cameraType == CameraType.Game && m_SubFrameManager.isRecording;
                passData.async = m_PathTracingSettings.asyncDenoising.value && hdCamera.camera.cameraType == CameraType.SceneView;
                passData.denoiseVolumetricFog = m_PathTracingSettings.separateVolumetrics.value;
                if (passData.denoiseVolumetricFog)
                {
                    passData.blitAddAndExposeKernel = passData.blitAndExposeCS.FindKernel("KAddMain");
                    passData.accumAndExposeKernel = passData.blitAndExposeCS.FindKernel("KAccumMain");
                }

                // copy camera params
                passData.camID = hdCamera.camera.GetEntityId();
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.slices = hdCamera.viewCount;

                passData.renderContext = renderContext;

                // Grab the history buffer
                TextureHandle ptAccumulation = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingOutput));
                TextureHandle denoisedHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingDenoised)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracingDenoised, PathTracingHistoryBufferAllocatorFunction, 1));

                passData.color = ptAccumulation;
                builder.UseTexture(passData.color, AccessFlags.ReadWrite);
                passData.outputTexture = outputTexture;
                builder.UseTexture(passData.outputTexture, AccessFlags.Write);
                passData.denoisedHistory = denoisedHistory;
                builder.UseTexture(passData.denoisedHistory, AccessFlags.Read);

                if (passData.useAOV)
                {
                    TextureHandle albedoHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingAlbedo));
                    TextureHandle normalHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingNormal));

                    passData.albedoAOV = albedoHistory;
                    builder.UseTexture(passData.albedoAOV, AccessFlags.Read);
                    passData.normalAOV = normalHistory;
                    builder.UseTexture(passData.normalAOV, AccessFlags.Read);
                }

                if (passData.temporal)
                {
                    TextureHandle motionVectorHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingMotionVector));
                    passData.motionVectorAOV = motionVectorHistory;
                    builder.UseTexture(passData.motionVectorAOV, AccessFlags.Read);
                }

                if (passData.denoiseVolumetricFog)
                {
                    TextureHandle volumetricFogHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingVolumetricFog));
                    TextureHandle denoisedVolumetricFogHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracingVolumetricFogDenoised)
                                                                             ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracingVolumetricFogDenoised, PathTracingHistoryBufferAllocatorFunction, 1));
                    passData.volumetricFogHistory = volumetricFogHistory;
                    builder.UseTexture(passData.volumetricFogHistory, AccessFlags.Read);
                    passData.denoisedVolumetricFogHistory = denoisedVolumetricFogHistory;
                    builder.UseTexture(passData.denoisedVolumetricFogHistory, AccessFlags.Read);
                }

                builder.SetRenderFunc(
                (RenderDenoisePassData data, UnsafeGraphContext ctx) =>
                {
                    var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    CameraData camData = data.subFrameManager.GetCameraData(data.camID);
                    bool wasDenoisedResultRendered = false;

                    // If we don't have any pending async denoise and we don't do temporal denoising, dispose any existing denoiser state
                    if (camData.colorDenoiserData.denoiser.type != DenoiserType.None && m_PathTracingSettings.temporal.value != true && camData.colorDenoiserData.activeRequest == false)
                    {
                        camData.colorDenoiserData.Dispose();
                        m_SubFrameManager.SetCameraData(data.camID, camData);
                    }
                    if (camData.volumetricFogDenoiserData.denoiser.type != DenoiserType.None && data.denoiseVolumetricFog != true && camData.volumetricFogDenoiserData.activeRequest == false)
                    {
                        camData.volumetricFogDenoiserData.Dispose();
                        m_SubFrameManager.SetCameraData(data.camID, camData);
                    }

                    camData.colorDenoiserData.denoiser.Init((DenoiserType)m_PathTracingSettings.denoising.value, data.width, data.height);
                    if (data.denoiseVolumetricFog)
                    {
                        camData.volumetricFogDenoiserData.denoiser.Init((DenoiserType)m_PathTracingSettings.denoising.value, data.width, data.height);
                    }

                    if (camData.currentIteration >= (data.subFrameManager.subFrameCount) && camData.colorDenoiserData.denoiser.type != DenoiserType.None)
                    {
                        // Are we ready to launch a denoise request for the color result?
                        if (!camData.colorDenoiserData.validHistory)
                        {
                            if (!camData.colorDenoiserData.activeRequest)
                            {
                                camData.colorDenoiserData.denoiser.DenoiseRequest(natCmd, "color", data.color);

                                if (data.useAOV)
                                {
                                    camData.colorDenoiserData.denoiser.DenoiseRequest(natCmd, "albedo", data.albedoAOV);
                                    camData.colorDenoiserData.denoiser.DenoiseRequest(natCmd, "normal", data.normalAOV);
                                }

                                if (data.temporal)
                                {
                                    camData.colorDenoiserData.denoiser.DenoiseRequest(natCmd, "flow", data.motionVectorAOV);
                                }

                                camData.colorDenoiserData.InitRequest();
                            }

                            if (!data.async)
                            {
                                camData.colorDenoiserData.denoiser.WaitForCompletion(data.renderContext, natCmd);

                                Denoiser.State ret = camData.colorDenoiserData.denoiser.GetResults(natCmd, data.denoisedHistory);
                                camData.colorDenoiserData.EndRequest(ret == Denoiser.State.Success);
                            }
                            else
                            {
                                if (camData.colorDenoiserData.activeRequest && camData.colorDenoiserData.denoiser.QueryCompletion() != Denoiser.State.Executing)
                                {
                                    Denoiser.State ret = camData.colorDenoiserData.denoiser.GetResults(natCmd, data.denoisedHistory);
                                    camData.colorDenoiserData.EndRequest(ret == Denoiser.State.Success && camData.colorDenoiserData.discardRequest == false);
                                }
                            }

                            m_SubFrameManager.SetCameraData(data.camID, camData);
                        }

                        // Are we ready to launch a denoise request for the volumetrics result?
                        if (!camData.volumetricFogDenoiserData.validHistory && data.denoiseVolumetricFog)
                        {
                            if (!camData.volumetricFogDenoiserData.activeRequest)
                            {
                                camData.volumetricFogDenoiserData.denoiser.DenoiseRequest(natCmd, "color", data.volumetricFogHistory);
                                camData.volumetricFogDenoiserData.InitRequest();
                            }

                            if (!data.async)
                            {
                                camData.volumetricFogDenoiserData.denoiser.WaitForCompletion(data.renderContext, natCmd);


                                Denoiser.State ret = camData.volumetricFogDenoiserData.denoiser.GetResults(natCmd, data.denoisedVolumetricFogHistory);
                                camData.volumetricFogDenoiserData.EndRequest(ret == Denoiser.State.Success);
                            }
                            else
                            {
                                if (camData.volumetricFogDenoiserData.activeRequest && camData.volumetricFogDenoiserData.denoiser.QueryCompletion() != Denoiser.State.Executing)
                                {
                                    Denoiser.State ret = camData.volumetricFogDenoiserData.denoiser.GetResults(natCmd, data.denoisedVolumetricFogHistory);
                                    camData.volumetricFogDenoiserData.EndRequest(ret == Denoiser.State.Success && camData.volumetricFogDenoiserData.discardRequest == false);
                                }
                            }

                            m_SubFrameManager.SetCameraData(data.camID, camData);
                        }

                        if (camData.colorDenoiserData.validHistory && (camData.volumetricFogDenoiserData.validHistory || !data.denoiseVolumetricFog))
                        {
                            int kernel = data.blitAndExposeKernel;

                            // Blit the denoised image from the history buffer and apply exposure. If we have denoised volumetrics, add it back as well.
                            if (camData.volumetricFogDenoiserData.validHistory && data.denoiseVolumetricFog)
                            {
                                kernel = data.blitAddAndExposeKernel;
                                natCmd.SetComputeTextureParam(data.blitAndExposeCS, kernel, HDShaderIDs._InputTexture2, data.denoisedVolumetricFogHistory);
                            }
                            natCmd.SetComputeTextureParam(data.blitAndExposeCS, kernel, HDShaderIDs._InputTexture, data.denoisedHistory);
                            natCmd.SetComputeTextureParam(data.blitAndExposeCS, kernel, HDShaderIDs._OutputTexture, data.outputTexture);
                            natCmd.DispatchCompute(data.blitAndExposeCS, kernel, (data.width + 7) / 8, (data.height + 7) / 8, data.slices);
                            wasDenoisedResultRendered = true;
                        }
                    }

                    if (data.denoiseVolumetricFog && !wasDenoisedResultRendered)
                    {
                        // Just add the volumetrics output on top of the color output to show a full picture to the user while the final denoising is ready to display
                        natCmd.SetComputeTextureParam(data.blitAndExposeCS, data.accumAndExposeKernel, HDShaderIDs._InputTexture, data.volumetricFogHistory);
                        natCmd.SetComputeTextureParam(data.blitAndExposeCS, data.accumAndExposeKernel, HDShaderIDs._OutputTexture, data.outputTexture);
                        natCmd.DispatchCompute(data.blitAndExposeCS, data.accumAndExposeKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.slices);
                    }
                });
            }
#endif
        }
    }
}
