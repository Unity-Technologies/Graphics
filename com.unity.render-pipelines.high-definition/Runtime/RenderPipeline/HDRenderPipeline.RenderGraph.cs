using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{

    public partial class HDRenderPipeline
    {
        class TempPassData { };

        // Needed only because of custom pass. See comment at ResolveMSAAColor.
        TextureHandle m_NonMSAAColorBuffer;

        void ExecuteWithRenderGraph(    RenderRequest           renderRequest,
                                        AOVRequestData          aovRequest,
                                        List<RTHandle>          aovBuffers,
                                        ScriptableRenderContext renderContext,
                                        CommandBuffer           commandBuffer)
        {
            var hdCamera = renderRequest.hdCamera;
            var camera = hdCamera.camera;
            var cullingResults = renderRequest.cullingResults.cullingResults;
            var customPassCullingResults = renderRequest.cullingResults.customPassCullingResults ?? cullingResults;
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            var target = renderRequest.target;

#if UNITY_EDITOR
            var showGizmos = camera.cameraType == CameraType.Game
                || camera.cameraType == CameraType.SceneView;
#endif

            TextureHandle backBuffer = m_RenderGraph.ImportBackbuffer(target.id);
            TextureHandle colorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, msaa);
            m_NonMSAAColorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, false);
            TextureHandle currentColorPyramid = m_RenderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain), HDShaderIDs._ColorPyramidTexture);

            LightingBuffers lightingBuffers = new LightingBuffers();
            lightingBuffers.diffuseLightingBuffer = CreateDiffuseLightingBuffer(m_RenderGraph, msaa);
            lightingBuffers.sssBuffer = CreateSSSBuffer(m_RenderGraph, msaa);

            var prepassOutput = RenderPrepass(m_RenderGraph, colorBuffer, lightingBuffers.sssBuffer, cullingResults, customPassCullingResults, hdCamera, aovRequest, aovBuffers);

            // Need this during debug render at the end outside of the main loop scope.
            // Once render graph move is implemented, we can probably remove the branch and this.
            ShadowResult shadowResult = new ShadowResult();
            BuildGPULightListOutput gpuLightListOutput = new BuildGPULightListOutput();

            RenderTransparencyOverdraw(m_RenderGraph, prepassOutput.depthBuffer, cullingResults, hdCamera);

            if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() || m_CurrentDebugDisplaySettings.IsMaterialValidationEnabled() || CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
            {
                using (new XRSinglePassScope(m_RenderGraph, hdCamera))
                {
                    colorBuffer = RenderDebugViewMaterial(m_RenderGraph, cullingResults, hdCamera);
                    colorBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, colorBuffer);
                }
            }
            else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                     hdCamera.volumeStack.GetComponent<PathTracing>().enable.value)
            {
                // TODO RENDERGRAPH
                //// We only request the light cluster if we are gonna use it for debug mode
                //if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                //{
                //    HDRaytracingLightCluster lightCluster = RequestLightCluster();
                //    lightCluster.EvaluateClusterDebugView(cmd, hdCamera);
                //}

                //RenderPathTracing(hdCamera, cmd, m_CameraColorBuffer);
            }
            else
            {
                gpuLightListOutput = BuildGPULightList(m_RenderGraph, hdCamera, m_TileAndClusterData, m_TotalLightCount, ref m_ShaderVariablesLightListCB, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.gbuffer);

                lightingBuffers.ambientOcclusionBuffer = m_AmbientOcclusionSystem.Render(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, prepassOutput.normalBuffer, prepassOutput.motionVectorsBuffer, m_FrameCount);
                // Should probably be inside the AO render function but since it's a separate class it's currently not super clean to do.
                PushFullScreenDebugTexture(m_RenderGraph, lightingBuffers.ambientOcclusionBuffer, FullScreenDebugMode.SSAO);

                // Evaluate the clear coat mask texture based on the lit shader mode
                var clearCoatMask = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? prepassOutput.gbuffer.mrt[2] : m_RenderGraph.defaultResources.blackTextureXR;
                lightingBuffers.ssrLightingBuffer = RenderSSR(m_RenderGraph,
                                                              hdCamera,
                                                              ref prepassOutput,
                                                              clearCoatMask,
                                                              transparent: false);

                lightingBuffers.contactShadowsBuffer = RenderContactShadows(m_RenderGraph, hdCamera, msaa ? prepassOutput.depthValuesMSAA : prepassOutput.depthPyramidTexture, gpuLightListOutput, GetDepthBufferMipChainInfo().mipLevelOffsets[1].y);

                var volumetricDensityBuffer = VolumeVoxelizationPass(m_RenderGraph, hdCamera, m_VisibleVolumeBoundsBuffer, m_VisibleVolumeDataBuffer, gpuLightListOutput.bigTileLightList, m_FrameCount);

                shadowResult = RenderShadows(m_RenderGraph, hdCamera, cullingResults);

                StartXRSinglePass(m_RenderGraph, hdCamera);

                // TODO RENDERGRAPH
                //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                //{
                //    // We only request the light cluster if we are gonna use it for debug mode
                //    if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                //    {
                //        HDRaytracingLightCluster lightCluster = RequestLightCluster();
                //        lightCluster.EvaluateClusterDebugView(cmd, hdCamera);
                //    }

                // TODO: check code, everything have change
                //    bool validIndirectDiffuse = ValidIndirectDiffuseState(hdCamera);
                //    if (validIndirectDiffuse)
                //    {
                //        if (RayTracedIndirectDiffuseState(hdCamera))
                //        {
                //            RenderRayTracedIndirectDiffuse(hdCamera, cmd, renderContext, m_FrameCount);
                //        }
                //        else
                //        {
                //            RenderSSGI(hdCamera, cmd, renderContext, m_FrameCount);
                //            BindIndirectDiffuseTexture(cmd);
                //        }
                //    }
                //    else
                //    {
                //        BindBlackIndirectDiffuseTexture(cmd);
                //    }
                //}
                //else
                //{
                //    bool validIndirectDiffuse = ValidIndirectDiffuseState(hdCamera);
                //    if (validIndirectDiffuse)
                //    {
                //        RenderSSGI(hdCamera, cmd, renderContext, m_FrameCount);
                //        BindIndirectDiffuseTexture(cmd);
                //    }
                //    else
                //    {
                //        BindBlackIndirectDiffuseTexture(cmd);
                //    }
                //}

                // Temporary workaround otherwise the texture is not bound when executing directly with rendergraph
                using (var builder = m_RenderGraph.AddRenderPass<TempPassData>("TempPass", out var passData))
                {
                    builder.AllowPassPruning(false);
                    builder.SetRenderFunc(
                    (TempPassData data, RenderGraphContext context) =>
                    {
                        BindBlackIndirectDiffuseTexture(context.cmd);
                    });
                }


                // TODO RENDERGRAPH
                //RenderScreenSpaceShadows(hdCamera, cmd);

                var volumetricLighting = VolumetricLightingPass(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, volumetricDensityBuffer, gpuLightListOutput.bigTileLightList, shadowResult, m_FrameCount);

                var deferredLightingOutput = RenderDeferredLighting(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, lightingBuffers, prepassOutput.gbuffer, shadowResult, gpuLightListOutput);

                RenderForwardOpaque(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, gpuLightListOutput, prepassOutput.depthBuffer, shadowResult, prepassOutput.dbuffer, cullingResults);

                // TODO RENDERGRAPH : Move this to the end after we do move semantic and graph pruning to avoid doing the rest of the frame for nothing
                aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Normals, hdCamera, prepassOutput.resolvedNormalBuffer, aovBuffers);

                lightingBuffers.diffuseLightingBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.diffuseLightingBuffer);
                lightingBuffers.sssBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.sssBuffer);

                RenderSubsurfaceScattering(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, ref prepassOutput);

                RenderForwardEmissive(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, cullingResults);

                RenderSky(m_RenderGraph, hdCamera, colorBuffer, volumetricLighting, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture);

                // Send all the geometry graphics buffer to client systems if required (must be done after the pyramid and before the transparent depth pre-pass)
                SendGeometryGraphicsBuffers(m_RenderGraph, prepassOutput.normalBuffer, prepassOutput.depthPyramidTexture, hdCamera);

                // TODO RENDERGRAPH
                //m_PostProcessSystem.DoUserAfterOpaqueAndSky(cmd, hdCamera, m_CameraColorBuffer);

                // No need for old stencil values here since from transparent on different features are tagged
                ClearStencilBuffer(m_RenderGraph, colorBuffer, prepassOutput.depthBuffer);

                colorBuffer = RenderTransparency(m_RenderGraph, hdCamera, colorBuffer, currentColorPyramid, gpuLightListOutput, ref prepassOutput, shadowResult, cullingResults, customPassCullingResults, aovRequest, aovBuffers);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector))
                {
                    prepassOutput.motionVectorsBuffer = ResolveMotionVector(m_RenderGraph, hdCamera, prepassOutput.motionVectorsBuffer);
                }

                // We push the motion vector debug texture here as transparent object can overwrite the motion vector texture content.
                if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                    PushFullScreenDebugTexture(m_RenderGraph, prepassOutput.motionVectorsBuffer, FullScreenDebugMode.MotionVectors);

                // TODO RENDERGRAPH : Move this to the end after we do move semantic and graph pruning to avoid doing the rest of the frame for nothing
                // Transparent objects may write to the depth and motion vectors buffers.
                aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.DepthStencil, hdCamera, prepassOutput.resolvedDepthBuffer, aovBuffers);
                if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                    aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.MotionVectors, hdCamera, prepassOutput.resolvedMotionVectorsBuffer, aovBuffers);

                // This final Gaussian pyramid can be reused by SSR, so disable it only if there is no distortion
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion) || hdCamera.IsSSREnabled())
                    GenerateColorPyramid(m_RenderGraph, hdCamera, colorBuffer, currentColorPyramid, false);

                var distortionBuffer = AccumulateDistortion(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, cullingResults);
                RenderDistortion(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, currentColorPyramid, distortionBuffer);

                PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.NanTracker);
                PushFullScreenLightingDebugTexture(m_RenderGraph, colorBuffer);

                if (m_SubFrameManager.isRecording && m_SubFrameManager.subFrameCount > 1)
                {
                    RenderAccumulation(m_RenderGraph, hdCamera, colorBuffer, colorBuffer, false);
                }

                // Render gizmos that should be affected by post processes
                RenderGizmos(m_RenderGraph, hdCamera, colorBuffer, GizmoSubset.PreImageEffects);
            }

            // At this point, the color buffer has been filled by either debug views are regular rendering so we can push it here.
            var colorPickerTexture = PushColorPickerDebugTexture(m_RenderGraph, colorBuffer);

            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.normalBuffer, customPassCullingResults, CustomPassInjectionPoint.BeforePostProcess, aovRequest, aovBuffers);

            aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Color, hdCamera, colorBuffer, aovBuffers);

            TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, colorBuffer, prepassOutput.depthBuffer, backBuffer, cullingResults, hdCamera);

            // TODO RENDERGRAPH
            //// If requested, compute histogram of the very final image
            //if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView)
            //{
            //    m_PostProcessSystem.GenerateDebugImageHistogram(cmd, hdCamera, m_IntermediateAfterPostProcessBuffer);
            //}
            //PushFullScreenExposureDebugTexture(cmd, m_IntermediateAfterPostProcessBuffer);

            RenderCustomPass(m_RenderGraph, hdCamera, postProcessDest, prepassOutput.depthBuffer, prepassOutput.normalBuffer, customPassCullingResults, CustomPassInjectionPoint.AfterPostProcess, aovRequest, aovBuffers);

            // TODO RENDERGRAPH
            //// Copy and rescale depth buffer for XR devices
            //if (hdCamera.xr.enabled && hdCamera.xr.copyDepth)
            //{
            //    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.XRDepthCopy)))
            //    {
            //        var depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            //        var rtScale = depthBuffer.rtHandleProperties.rtHandleScale / DynamicResolutionHandler.instance.GetCurrentScale();

            //        m_CopyDepthPropertyBlock.SetTexture(HDShaderIDs._InputDepth, depthBuffer);
            //        m_CopyDepthPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, rtScale);
            //        m_CopyDepthPropertyBlock.SetInt("_FlipY", 1);

            //        cmd.SetRenderTarget(target.id, 0, CubemapFace.Unknown, -1);
            //        cmd.SetViewport(hdCamera.finalViewport);
            //        CoreUtils.DrawFullScreen(cmd, m_CopyDepth, m_CopyDepthPropertyBlock);
            //    }
            //}

            // In developer build, we always render post process in m_AfterPostProcessBuffer at (0,0) in which we will then render debug.
            // Because of this, we need another blit here to the final render target at the right viewport.
            if (!HDUtils.PostProcessIsFinalPass(hdCamera) || aovRequest.isValid)
            {
                hdCamera.ExecuteCaptureActions(m_RenderGraph, colorBuffer);

                postProcessDest = RenderDebug(  m_RenderGraph,
                                                hdCamera,
                                                postProcessDest,
                                                prepassOutput.depthBuffer,
                                                prepassOutput.depthPyramidTexture,
                                                m_DebugFullScreenTexture,
                                                colorPickerTexture,
                                                gpuLightListOutput,
                                                shadowResult,
                                                cullingResults);

                BlitFinalCameraTexture(m_RenderGraph, hdCamera, postProcessDest, backBuffer, prepassOutput.resolvedMotionVectorsBuffer, prepassOutput.resolvedNormalBuffer);

                if (target.targetDepth != null)
                {
                    BlitFinalCameraTexture(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, m_RenderGraph.ImportTexture(target.targetDepth), prepassOutput.resolvedMotionVectorsBuffer, prepassOutput.resolvedNormalBuffer);
                }

                aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Output, hdCamera, postProcessDest, aovBuffers);
            }

            // XR mirror view and blit do device
            EndCameraXR(m_RenderGraph, hdCamera);

            SendColorGraphicsBuffer(m_RenderGraph, hdCamera);

            SetFinalTarget(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, backBuffer);

            RenderWireOverlay(m_RenderGraph, hdCamera, backBuffer);

            RenderGizmos(m_RenderGraph, hdCamera, colorBuffer, GizmoSubset.PostImageEffects);

            ExecuteRenderGraph(m_RenderGraph, hdCamera, m_MSAASamples, m_FrameCount, renderContext, commandBuffer );

            aovRequest.Execute(commandBuffer, aovBuffers, RenderOutputProperties.From(hdCamera));
        }

        static void ExecuteRenderGraph(RenderGraph renderGraph, HDCamera hdCamera, MSAASamples msaaSample, int frameIndex, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            var renderGraphParams = new RenderGraphExecuteParams()
            {
                renderingWidth = hdCamera.actualWidth,
                renderingHeight = hdCamera.actualHeight,
                msaaSamples = msaaSample,
                currentFrameIndex = frameIndex
            };

            renderGraph.Execute(renderContext, cmd, renderGraphParams);
        }

        class FinalBlitPassData
        {
            public BlitFinalCameraTextureParameters parameters;
            public TextureHandle                    source;
            public TextureHandle                    destination;
        }

        void BlitFinalCameraTexture(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle destination, TextureHandle motionVectors, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<FinalBlitPassData>("Final Blit (Dev Build Only)", out var passData))
            {
                passData.parameters = PrepareFinalBlitParameters(hdCamera, 0); // todo viewIndex
                passData.source = builder.ReadTexture(source);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                (FinalBlitPassData data, RenderGraphContext context) =>
                {
                    var sourceTexture = context.resources.GetTexture(data.source);
                    var destinationTexture = context.resources.GetTexture(data.destination);
                    BlitFinalCameraTexture(data.parameters, context.renderGraphPool.GetTempMaterialPropertyBlock(), sourceTexture, destinationTexture, context.cmd);
                });
            }
        }

        class SetFinalTargetPassData
        {
            public bool             copyDepth;
            public Material         copyDepthMaterial;
            public TextureHandle    finalTarget;
            public Rect             finalViewport;
            public TextureHandle    depthBuffer;
            public bool             flipY;
        }

        void SetFinalTarget(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle finalTarget)
        {
            using (var builder = renderGraph.AddRenderPass<SetFinalTargetPassData>("Set Final Target", out var passData))
            {
                // Due to our RT handle system we don't write into the backbuffer depth buffer (as our depth buffer can be bigger than the one provided)
                // So we need to do a copy of the corresponding part of RT depth buffer in the target depth buffer in various situation:
                // - RenderTexture (camera.targetTexture != null) has a depth buffer (camera.targetTexture.depth != 0)
                // - We are rendering into the main game view (i.e not a RenderTexture camera.cameraType == CameraType.Game && hdCamera.camera.targetTexture == null) in the editor for allowing usage of Debug.DrawLine and Debug.Ray.
                // - We draw Gizmo/Icons in the editor (hdCamera.camera.targetTexture != null && camera.targetTexture.depth != 0 - The Scene view has a targetTexture and a depth texture)
                // TODO: If at some point we get proper render target aliasing, we will be able to use the provided depth texture directly with our RT handle system
                // Note: Debug.DrawLine and Debug.Ray only work in editor, not in player
                passData.copyDepth = hdCamera.camera.targetTexture != null && hdCamera.camera.targetTexture.depth != 0;
#if UNITY_EDITOR
                passData.copyDepth = passData.copyDepth || hdCamera.isMainGameView; // Specific case of Debug.DrawLine and Debug.Ray
#endif
                passData.copyDepthMaterial = m_CopyDepth;
                passData.finalTarget = builder.WriteTexture(finalTarget);
                passData.finalViewport = hdCamera.finalViewport;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.flipY = hdCamera.isMainGameView;

                builder.SetRenderFunc(
                (SetFinalTargetPassData data, RenderGraphContext ctx) =>
                {
                    // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
                    ctx.cmd.SetRenderTarget(ctx.resources.GetTexture(data.finalTarget));
                    ctx.cmd.SetViewport(data.finalViewport);

                    if (data.copyDepth)
                    {
                        using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.CopyDepthInTargetTexture)))
                        {
                            var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpb.SetTexture(HDShaderIDs._InputDepth, ctx.resources.GetTexture(data.depthBuffer));
                            // When we are Main Game View we need to flip the depth buffer ourselves as we are after postprocess / blit that have already flipped the screen
                            mpb.SetInt("_FlipY", data.flipY ? 1 : 0);
                            mpb.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                            CoreUtils.DrawFullScreen(ctx.cmd, data.copyDepthMaterial, mpb);
                        }
                    }
                });
            }
        }

        class ForwardPassData
        {
            public RendererListHandle   rendererList;
            public TextureHandle[]      renderTarget = new TextureHandle[3];
            public int                  renderTargetCount;
            public TextureHandle        depthBuffer;
            public TextureHandle        ssrLlightingBuffer;
            public ComputeBufferHandle  lightListBuffer;
            public ComputeBufferHandle  perVoxelOffset;
            public ComputeBufferHandle  perTileLogBaseTweak;
            public FrameSettings        frameSettings;
            public bool                 decalsEnabled;
            public bool                 renderMotionVecForTransparent;
            public DBufferOutput?       dbuffer;
        }

        void PrepareForwardPassData(RenderGraph                 renderGraph,
                                    RenderGraphBuilder          builder,
                                    ForwardPassData             data,
                                    bool                        opaque,
                                    FrameSettings               frameSettings,
                                    RendererListDesc            rendererListDesc,
                                    in BuildGPULightListOutput  lightLists,
                                    TextureHandle               depthBuffer,
                                    ShadowResult                shadowResult,
                                    DBufferOutput?              dbuffer = null)
        {
            bool useFptl = frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque) && opaque;

            data.frameSettings = frameSettings;
            data.lightListBuffer = builder.ReadComputeBuffer(useFptl ? lightLists.lightList : lightLists.perVoxelLightLists);
            if (!useFptl)
            {
                data.perVoxelOffset = builder.ReadComputeBuffer(lightLists.perVoxelOffset);
                if (lightLists.perTileLogBaseTweak.IsValid())
                    data.perTileLogBaseTweak = builder.ReadComputeBuffer(lightLists.perTileLogBaseTweak);
            }
            data.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
            data.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
            // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
            // decal datas count is 0 if no decals affect transparency
            data.decalsEnabled = (frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
            data.renderMotionVecForTransparent = NeedMotionVectorForTransparent(frameSettings);

            HDShadowManager.ReadShadowResult(shadowResult, builder);
            if (dbuffer != null)
                data.dbuffer = ReadDBuffer(dbuffer.Value, builder);
        }

        // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
        // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
        // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
        // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
        // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
        // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"
        void RenderForwardOpaque(   RenderGraph                 renderGraph,
                                    HDCamera                    hdCamera,
                                    TextureHandle               colorBuffer,
                                    in LightingBuffers          lightingBuffers,
                                    in BuildGPULightListOutput  lightLists,
                                    TextureHandle               depthBuffer,
                                    ShadowResult                shadowResult,
                                    DBufferOutput               dbuffer,
                                    CullingResults              cullResults)
        {
            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>(    debugDisplay ? "Forward Opaque Debug" : "Forward Opaque",
                                                                                out var passData,
                                                                                debugDisplay ? ProfilingSampler.Get(HDProfileId.ForwardOpaqueDebug) : ProfilingSampler.Get(HDProfileId.ForwardOpaque)))
            {
                PrepareForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, PrepareForwardOpaqueRendererList(cullResults, hdCamera), lightLists, depthBuffer, shadowResult, dbuffer);

                // In case of forward SSS we will bind all the required target. It is up to the shader to write into it or not.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                {
                    passData.renderTarget[0] = builder.WriteTexture(colorBuffer); // Store the specular color
                    passData.renderTarget[1] = builder.WriteTexture(lightingBuffers.diffuseLightingBuffer);
                    passData.renderTarget[2] = builder.WriteTexture(lightingBuffers.sssBuffer);
                    passData.renderTargetCount = 3;
                }
                else
                {
                    passData.renderTarget[0] = builder.WriteTexture(colorBuffer);
                    passData.renderTargetCount = 1;
                }

                ReadLightingBuffers(lightingBuffers, builder);

                builder.SetRenderFunc(
                (ForwardPassData data, RenderGraphContext context) =>
                {
                    // TODO RENDERGRAPH: replace with UseColorBuffer when removing old rendering (SetRenderTarget is called inside RenderForwardRendererList because of that).
                    var mrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.renderTargetCount);
                    for (int i = 0; i < data.renderTargetCount; ++i)
                        mrt[i] = context.resources.GetTexture(data.renderTarget[i]);

                    if (data.dbuffer != null)
                        BindDBufferGlobalData(data.dbuffer.Value, context);

                    RenderForwardRendererList(data.frameSettings,
                            context.resources.GetRendererList(data.rendererList),
                            mrt,
                            context.resources.GetTexture(data.depthBuffer),
                            context.resources.GetComputeBuffer(data.lightListBuffer),
                            true, context.renderContext, context.cmd);
                });
            }
        }

        void RenderForwardTransparent(  RenderGraph                 renderGraph,
                                        HDCamera                    hdCamera,
                                        TextureHandle               colorBuffer,
                                        TextureHandle               motionVectorBuffer,
                                        TextureHandle               depthBuffer,
                                        TextureHandle               ssrLighting,
                                        TextureHandle?              colorPyramid,
                                        in BuildGPULightListOutput  lightLists,
                                        in ShadowResult             shadowResult,
                                        CullingResults              cullResults,
                                        bool                        preRefractionPass)
        {
            // If rough refraction are turned off, we render all transparents in the Transparent pass and we skip the PreRefraction one.
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) && preRefractionPass)
                return;

            string passName;
            HDProfileId profilingId;
            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
            if (debugDisplay)
            {
                passName = preRefractionPass ? "Forward PreRefraction Debug" : "Forward Transparent Debug";
                profilingId = preRefractionPass ? HDProfileId.ForwardPreRefractionDebug : HDProfileId.ForwardTransparentDebug;
            }
            else
            {
                passName = preRefractionPass ? "Forward PreRefraction" : "Forward Transparent";
                profilingId = preRefractionPass ? HDProfileId.ForwardPreRefraction : HDProfileId.ForwardTransparent;
            }

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>(passName, out var passData, ProfilingSampler.Get(profilingId)))
            {
                PrepareForwardPassData(renderGraph, builder, passData, false, hdCamera.frameSettings, PrepareForwardTransparentRendererList(cullResults, hdCamera, preRefractionPass), lightLists, depthBuffer, shadowResult);

                passData.ssrLlightingBuffer = builder.ReadTexture(ssrLighting);

                bool renderMotionVecForTransparent = NeedMotionVectorForTransparent(hdCamera.frameSettings);

                passData.renderTargetCount = 2;
                passData.renderTarget[0] = builder.WriteTexture(colorBuffer);

                if (renderMotionVecForTransparent)
                {
                    passData.renderTarget[1] = builder.WriteTexture(motionVectorBuffer);
                }
                else
                {
                    // It doesn't really matter what gets bound here since the color mask state set will prevent this from ever being written to. However, we still need to bind something
                    // to avoid warnings about unbound render targets. The following rendertarget could really be anything if renderVelocitiesForTransparent
                    // Create a new target here should reuse existing already released one
                    passData.renderTarget[1] = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, name = "Transparency Velocity Dummy" });
                }

                if (colorPyramid != null && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) && !preRefractionPass)
                {
                    builder.ReadTexture(colorPyramid.Value);
                }

                builder.SetRenderFunc(
                    (ForwardPassData data, RenderGraphContext context) =>
                {
                    // TODO: replace with UseColorBuffer when removing old rendering.
                    var mrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.renderTargetCount);
                    for (int i = 0; i < data.renderTargetCount; ++i)
                        mrt[i] = context.resources.GetTexture(data.renderTarget[i]);

                    // Bind all global data/parameters for transparent forward pass
                    context.cmd.SetGlobalInt(HDShaderIDs._ColorMaskTransparentVel, data.renderMotionVecForTransparent ? (int)ColorWriteMask.All : 0);
                    if (data.decalsEnabled)
                        DecalSystem.instance.SetAtlas(context.cmd); // for clustered decals

                    context.cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, context.resources.GetComputeBuffer(data.perVoxelOffset));
                    context.cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, context.resources.GetComputeBuffer(data.perTileLogBaseTweak));
                    context.cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, context.resources.GetTexture(data.ssrLlightingBuffer));

                    RenderForwardRendererList(  data.frameSettings,
                                                context.resources.GetRendererList(data.rendererList),
                                                mrt,
                                                context.resources.GetTexture(data.depthBuffer),
                                                context.resources.GetComputeBuffer(data.lightListBuffer),
                                                false, context.renderContext, context.cmd);
                });
            }
        }


        void RenderTransparentDepthPrepass(RenderGraph renderGraph, HDCamera hdCamera, in PrepassOutput prepassOutput, CullingResults cull)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPrepass))
                return;

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>("Transparent Depth Prepass", out var passData, ProfilingSampler.Get(HDProfileId.TransparentDepthPrepass)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                if (hdCamera.IsSSREnabled(transparent: true))
                    BindPrepassColorBuffers(builder, prepassOutput, hdCamera);
                builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);

                passData.renderTargetCount = 0;
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cull, hdCamera.camera, m_TransparentDepthPrepassNames)));

                builder.SetRenderFunc(
                (ForwardPassData data, RenderGraphContext context) =>
                {
                    DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, context.resources.GetRendererList(data.rendererList));
                });
            }
        }

        void RenderTransparentDepthPostpass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, CullingResults cull)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPostpass))
                return;

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>("Transparent Depth Postpass", out var passData, ProfilingSampler.Get(HDProfileId.TransparentDepthPostpass)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);
                passData.renderTargetCount = 0;
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cull, hdCamera.camera, m_TransparentDepthPostpassNames)));

                builder.SetRenderFunc(
                (ForwardPassData data, RenderGraphContext context) =>
                {
                    DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, context.resources.GetRendererList(data.rendererList));
                });
            }
        }

        class DownsampleDepthForLowResPassData
        {
            public Material                    downsampleDepthMaterial;
            public TextureHandle    depthTexture;
            public TextureHandle    downsampledDepthBuffer;
        }

        TextureHandle DownsampleDepthForLowResTransparency(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture)
        {
            using (var builder = renderGraph.AddRenderPass<DownsampleDepthForLowResPassData>("Downsample Depth Buffer for Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.DownsampleDepth)))
            {
                // TODO: Add option to switch modes at runtime
                if (m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer)
                {
                    m_DownsampleDepthMaterial.EnableKeyword("CHECKERBOARD_DOWNSAMPLE");
                }

                passData.downsampleDepthMaterial = m_DownsampleDepthMaterial;
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.downsampledDepthBuffer = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * 0.5f, true, true) { depthBufferBits = DepthBits.Depth32, name = "LowResDepthBuffer" }), DepthAccess.Write);

                builder.SetRenderFunc(
                (DownsampleDepthForLowResPassData data, RenderGraphContext context) =>
                {
                    //CoreUtils.SetRenderTarget(cmd, m_SharedRTManager.GetLowResDepthBuffer());
                    //cmd.SetViewport(new Rect(0, 0, hdCamera.actualWidth * 0.5f, hdCamera.actualHeight * 0.5f));

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.downsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                });

                return passData.downsampledDepthBuffer;
            }
        }

        class RenderLowResTransparentPassData
        {
            public ShaderVariablesGlobal    globalCB;
            public FrameSettings            frameSettings;
            public RendererListHandle       rendererList;
            public TextureHandle            lowResBuffer;
            public TextureHandle            downsampledDepthBuffer;
        }

        TextureHandle RenderLowResTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle downsampledDepth, CullingResults cullingResults)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLowResTransparentPassData>("Low Res Transparent", out var passData, ProfilingSampler.Get(HDProfileId.LowResTransparent)))
            {
                var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;

                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.frameSettings = hdCamera.frameSettings;
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_LowTransparent)));
                passData.downsampledDepthBuffer = builder.UseDepthBuffer(downsampledDepth, DepthAccess.ReadWrite);
                // We need R16G16B16A16_SFloat as we need a proper alpha channel for compositing.
                passData.lowResBuffer = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, clearBuffer = true, clearColor = Color.black, name = "Low res transparent" }), 0);

                builder.SetRenderFunc(
                (RenderLowResTransparentPassData data, RenderGraphContext context) =>
                {
                    UpdateOffscreenRenderingConstants(ref data.globalCB, true, 2u);
                    ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                    DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings,  context.resources.GetRendererList(data.rendererList));

                    UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1u);
                    ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                });

                return passData.lowResBuffer;
            }
        }

        class UpsampleTransparentPassData
        {
            public Material                     upsampleMaterial;
            public TextureHandle    colorBuffer;
            public TextureHandle    lowResTransparentBuffer;
            public TextureHandle    downsampledDepthBuffer;
        }

        void UpsampleTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle lowResTransparentBuffer, TextureHandle downsampledDepthBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpsampleTransparentPassData>("Upsample Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.UpsampleLowResTransparent)))
            {
                var settings = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings;
                if (settings.upsampleType == LowResTransparentUpsample.Bilinear)
                {
                    m_UpsampleTransparency.EnableKeyword("BILINEAR");
                }
                else if (settings.upsampleType == LowResTransparentUpsample.NearestDepth)
                {
                    m_UpsampleTransparency.EnableKeyword("NEAREST_DEPTH");
                }

                passData.upsampleMaterial = m_UpsampleTransparency;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.lowResTransparentBuffer = builder.ReadTexture(lowResTransparentBuffer);
                passData.downsampledDepthBuffer = builder.ReadTexture(downsampledDepthBuffer);

                builder.SetRenderFunc(
                (UpsampleTransparentPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    data.upsampleMaterial.SetTexture(HDShaderIDs._LowResTransparent, res.GetTexture(data.lowResTransparentBuffer));
                    data.upsampleMaterial.SetTexture(HDShaderIDs._LowResDepthTexture, res.GetTexture(data.downsampledDepthBuffer));
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.upsampleMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                });
            }
        }

        TextureHandle RenderTransparency(   RenderGraph                 renderGraph,
                                            HDCamera                    hdCamera,
                                            TextureHandle               colorBuffer,
                                            TextureHandle               currentColorPyramid,
                                            in BuildGPULightListOutput  lightLists,
                                            ref PrepassOutput           prepassOutput,
                                            ShadowResult                shadowResult,
                                            CullingResults              cullingResults,
                                            CullingResults              customPassCullingResults,
                                            AOVRequestData              aovRequest,
                                            List<RTHandle>              aovBuffers)
        {
            RenderTransparentDepthPrepass(renderGraph, hdCamera, prepassOutput, cullingResults);

            var ssrLightingBuffer = RenderSSR(renderGraph, hdCamera, ref prepassOutput, renderGraph.defaultResources.blackTextureXR, transparent: true);

            //RenderRayTracingPrepass(cullingResults, hdCamera, renderContext, cmd, true);
            //RaytracingRecursiveRender(hdCamera, cmd, renderContext, cullingResults);

            // TODO RENDERGRAPH
            //// To allow users to fetch the current color buffer, we temporarily bind the camera color buffer
            //cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, m_CameraColorBuffer);

            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.normalBuffer, customPassCullingResults, CustomPassInjectionPoint.BeforePreRefraction, aovRequest, aovBuffers);

            // Render pre-refraction objects
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, prepassOutput.motionVectorsBuffer, prepassOutput.depthBuffer, ssrLightingBuffer, null, lightLists, shadowResult, cullingResults, true);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
            {
                var resolvedColorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);
                GenerateColorPyramid(renderGraph, hdCamera, resolvedColorBuffer, currentColorPyramid, true);
            }

            // We don't have access to the color pyramid with transparent if rough refraction is disabled
            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.normalBuffer, customPassCullingResults, CustomPassInjectionPoint.BeforeTransparent, aovRequest, aovBuffers);

            // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, prepassOutput.motionVectorsBuffer, prepassOutput.depthBuffer, ssrLightingBuffer, currentColorPyramid, lightLists, shadowResult, cullingResults, false);

            colorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);

            // Render All forward error
            RenderForwardError(renderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, cullingResults);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                var downsampledDepth = DownsampleDepthForLowResTransparency(renderGraph, hdCamera, prepassOutput.depthPyramidTexture);
                var lowResTransparentBuffer = RenderLowResTransparent(renderGraph, hdCamera, downsampledDepth, cullingResults);
                UpsampleTransparent(renderGraph, hdCamera, colorBuffer, lowResTransparentBuffer, downsampledDepth);
            }

            // Fill depth buffer to reduce artifact for transparent object during postprocess
            RenderTransparentDepthPostpass(renderGraph, hdCamera, prepassOutput.depthBuffer, cullingResults);

            return colorBuffer;
        }

        class RenderForwardEmissivePassData
        {
            public bool enableDecals;
            public RendererListHandle rendererList;
        }

        void RenderForwardEmissive( RenderGraph                 renderGraph,
                                    HDCamera                    hdCamera,
                                    TextureHandle   colorBuffer,
                                    TextureHandle   depthStencilBuffer,
                                    CullingResults              cullingResults)
        {
            using (var builder = renderGraph.AddRenderPass<RenderForwardEmissivePassData>("ForwardEmissive", out var passData, ProfilingSampler.Get(HDProfileId.ForwardEmissive)))
            {
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(PrepareForwardEmissiveRendererList(cullingResults, hdCamera)));

                builder.SetRenderFunc(
                    (RenderForwardEmissivePassData data, RenderGraphContext context) =>
                {
                    HDUtils.DrawRendererList(context.renderContext, context.cmd, context.resources.GetRendererList(data.rendererList));
                    if (data.enableDecals)
                    DecalSystem.instance.RenderForwardEmissive(context.cmd);
                });
            }
        }

        // This is use to Display legacy shader with an error shader
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
        void RenderForwardError(RenderGraph                 renderGraph,
                                HDCamera                    hdCamera,
                                TextureHandle   colorBuffer,
                                TextureHandle   depthStencilBuffer,
                                CullingResults              cullResults)
        {
            using (var builder = renderGraph.AddRenderPass<ForwardPassData>("Forward Error", out var passData, ProfilingSampler.Get(HDProfileId.RenderForwardError)))
            {
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);

                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, m_ForwardErrorPassNames, renderQueueRange: RenderQueueRange.all, overrideMaterial: m_ErrorMaterial)));

                builder.SetRenderFunc(
                    (ForwardPassData data, RenderGraphContext context) =>
                    {
                        HDUtils.DrawRendererList(context.renderContext, context.cmd, context.resources.GetRendererList(data.rendererList));
                    });
            }
        }

        class SendGeometryBuffersPassData
        {
            public SendGeometryGraphcisBuffersParameters parameters;
            public TextureHandle normalBuffer;
            public TextureHandle depthBuffer;
        }

        void SendGeometryGraphicsBuffers(RenderGraph renderGraph, TextureHandle normalBuffer, TextureHandle depthBuffer, HDCamera hdCamera)
        {
            var parameters = PrepareSendGeometryBuffersParameters(hdCamera, m_DepthBufferMipChainInfo);

            if (!parameters.NeedSendBuffers())
                return;

            using (var builder = renderGraph.AddRenderPass<SendGeometryBuffersPassData>("Send Geometry Buffers", out var passData))
            {
                builder.AllowPassPruning(false);

                passData.parameters = parameters;
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.depthBuffer = builder.ReadTexture(depthBuffer);

                builder.SetRenderFunc(
                (SendGeometryBuffersPassData data, RenderGraphContext ctx) =>
                {
                    SendGeometryGraphicsBuffers(data.parameters, ctx.resources.GetTexture(data.normalBuffer), ctx.resources.GetTexture(data.depthBuffer), ctx.cmd);
                });
            }
        }

        class SendColorGraphicsBufferPassData
        {
            public HDCamera hdCamera;
        }

        void SendColorGraphicsBuffer(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<SendColorGraphicsBufferPassData>("Send Color Buffers", out var passData))
            {
                builder.AllowPassPruning(false);

                passData.hdCamera = hdCamera;

                builder.SetRenderFunc(
                (SendColorGraphicsBufferPassData data, RenderGraphContext ctx) =>
                {
                    SendColorGraphicsBuffer(ctx.cmd, data.hdCamera);
                });
            }
        }

        class ClearStencilPassData
        {
            public Material clearStencilMaterial;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void ClearStencilBuffer(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<ClearStencilPassData>("Clear Stencil Buffer", out var passData, ProfilingSampler.Get(HDProfileId.ClearStencil)))
            {
                passData.clearStencilMaterial = m_ClearStencilBufferMaterial;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthBuffer = builder.WriteTexture(depthBuffer);

                builder.SetRenderFunc(
                (ClearStencilPassData data, RenderGraphContext ctx) =>
                {
                    data.clearStencilMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.HDRPReservedBits);
                    HDUtils.DrawFullScreen(ctx.cmd, data.clearStencilMaterial, ctx.resources.GetTexture(data.colorBuffer), ctx.resources.GetTexture(data.depthBuffer));
                });
            }
        }

        class PreRenderSkyPassData
        {
            public Light sunLight;
            public HDCamera hdCamera;
            public TextureHandle colorBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public DebugDisplaySettings debugDisplaySettings;
            public SkyManager skyManager;
            public int frameCount;
        }

        void PreRenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthStencilBuffer, TextureHandle normalbuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<PreRenderSkyPassData>("Pre Render Sky", out var passData))
            {
                passData.sunLight = GetCurrentSunLight();
                passData.hdCamera = hdCamera;
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthStencilBuffer = builder.WriteTexture(depthStencilBuffer);
                passData.normalBuffer = builder.WriteTexture(normalbuffer);
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.skyManager = m_SkyManager;
                passData.frameCount = m_FrameCount;

                builder.SetRenderFunc(
                (PreRenderSkyPassData data, RenderGraphContext context) =>
                {
                    var depthBuffer = context.resources.GetTexture(data.depthStencilBuffer);
                    var destination = context.resources.GetTexture(data.colorBuffer);
                    var normalBuffer= context.resources.GetTexture(data.normalBuffer);

                    data.skyManager.PreRenderSky(data.hdCamera, data.sunLight, destination, normalBuffer, depthBuffer, data.debugDisplaySettings, data.frameCount, context.cmd);
                });
            }
        }

        class RenderSkyPassData
        {
            public VisualEnvironment    visualEnvironment;
            public Light                sunLight;
            public HDCamera             hdCamera;
            public TextureHandle        volumetricLighting;
            public TextureHandle        colorBuffer;
            public TextureHandle        depthStencilBuffer;
            public TextureHandle        intermediateBuffer;
            public DebugDisplaySettings debugDisplaySettings;
            public SkyManager           skyManager;
            public int                  frameCount;
        }

        void RenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle volumetricLighting, TextureHandle depthStencilBuffer, TextureHandle depthTexture)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<RenderSkyPassData>("Render Sky And Fog", out var passData))
            {
                passData.visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
                passData.sunLight = GetCurrentSunLight();
                passData.hdCamera = hdCamera;
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthStencilBuffer = builder.WriteTexture(depthStencilBuffer);
                passData.intermediateBuffer = builder.CreateTransientTexture(colorBuffer);
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.skyManager = m_SkyManager;
                passData.frameCount = m_FrameCount;

                builder.ReadTexture(depthTexture);

                builder.SetRenderFunc(
                (RenderSkyPassData data, RenderGraphContext context) =>
                {
                    // Necessary to perform dual-source (polychromatic alpha) blending which is not supported by Unity.
                    // We load from the color buffer, perform blending manually, and store to the atmospheric scattering buffer.
                    // Then we perform a copy from the atmospheric scattering buffer back to the color buffer.
                    var depthBuffer = context.resources.GetTexture(data.depthStencilBuffer);
                    var destination = context.resources.GetTexture(data.colorBuffer);
                    var intermediateBuffer = context.resources.GetTexture(data.intermediateBuffer);
                    var inputVolumetric = context.resources.GetTexture(data.volumetricLighting);

                    data.skyManager.RenderSky(data.hdCamera, data.sunLight, destination, depthBuffer, data.debugDisplaySettings, data.frameCount, context.cmd);

                    if (Fog.IsFogEnabled(data.hdCamera) || Fog.IsPBRFogEnabled(data.hdCamera))
                    {
                        var pixelCoordToViewDirWS = data.hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                        data.skyManager.RenderOpaqueAtmosphericScattering(context.cmd, data.hdCamera, destination, inputVolumetric, intermediateBuffer, depthBuffer, pixelCoordToViewDirWS, data.hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA));
                    }
                });
            }
        }

        class GenerateColorPyramidData
        {
            public TextureHandle colorPyramid;
            public TextureHandle inputColor;
            public MipGenerator mipGenerator;
            public HDCamera hdCamera;
        }

        void GenerateColorPyramid(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputColor, TextureHandle output, bool isPreRefraction)
        {
            // Here we cannot rely on automatic pass pruning if the result is not read
            // because the output texture is imported from outside of render graph (as it is persistent)
            // and in this case the pass is considered as having side effect and cannot be pruned.
            if (isPreRefraction)
            {
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
                    return;
            }
            // This final Gaussian pyramid can be reused by SSR, so disable it only if there is no distortion
            else if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion) && !hdCamera.IsSSREnabled())
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<GenerateColorPyramidData>("Color Gaussian MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.ColorPyramid)))
            {
                passData.colorPyramid = builder.WriteTexture(output);
                passData.inputColor = builder.ReadTexture(inputColor);
                passData.hdCamera = hdCamera;
                passData.mipGenerator = m_MipGenerator;

                builder.SetRenderFunc(
                (GenerateColorPyramidData data, RenderGraphContext context) =>
                {
                    Vector2Int pyramidSize = new Vector2Int(data.hdCamera.actualWidth, data.hdCamera.actualHeight);
                    var colorPyramid = context.resources.GetTexture(data.colorPyramid);
                    var inputTexture = context.resources.GetTexture(data.inputColor);
                    data.hdCamera.colorPyramidHistoryMipCount = data.mipGenerator.RenderColorGaussianPyramid(context.cmd, pyramidSize, inputTexture, colorPyramid);

                    // TODO RENDERGRAPH: We'd like to avoid SetGlobals like this but it's required by custom passes currently.
                    // We will probably be able to remove those once we push custom passes fully to render graph.
                    context.cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, colorPyramid);
                });
            }

            // Note: hdCamera.colorPyramidHistoryMipCount is going to be one frame late here (rendering, which is done later, is updating it)
            // In practice this should not be a big problem as it's only for debug purpose here.
            var scale = new Vector4(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y, 0f, 0f);
            PushFullScreenDebugTextureMip(renderGraph, output, hdCamera.colorPyramidHistoryMipCount, scale, isPreRefraction ? FullScreenDebugMode.PreRefractionColorPyramid : FullScreenDebugMode.FinalColorPyramid);
        }

        class AccumulateDistortionPassData
        {
            public TextureHandle        distortionBuffer;
            public TextureHandle        depthStencilBuffer;
            public RendererListHandle   distortionRendererList;
            public FrameSettings        frameSettings;
        }

        TextureHandle AccumulateDistortion( RenderGraph     renderGraph,
                                            HDCamera        hdCamera,
                                            TextureHandle   depthStencilBuffer,
                                            CullingResults  cullResults)
        {
            using (var builder = renderGraph.AddRenderPass<AccumulateDistortionPassData>("Accumulate Distortion", out var passData, ProfilingSampler.Get(HDProfileId.Distortion)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.distortionBuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetDistortionBufferFormat(), clearBuffer = true, clearColor = Color.clear, name = "Distortion" }), 0);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.distortionRendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_DistortionVectorsName)));

                builder.SetRenderFunc(
                (AccumulateDistortionPassData data, RenderGraphContext context) =>
                {
                    DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, context.resources.GetRendererList(data.distortionRendererList));
                });

                return passData.distortionBuffer;
            }
        }

        class RenderDistortionPassData
        {
            public Material         applyDistortionMaterial;
            public TextureHandle    colorPyramidBuffer;
            public TextureHandle    distortionBuffer;
            public TextureHandle    colorBuffer;
            public TextureHandle    depthStencilBuffer;
            public Vector4          size;
        }

        void RenderDistortion(  RenderGraph     renderGraph,
                                HDCamera        hdCamera,
                                TextureHandle   colorBuffer,
                                TextureHandle   depthStencilBuffer,
                                TextureHandle   colorPyramidBuffer,
                                TextureHandle   distortionBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion))
                return;

            using (var builder = renderGraph.AddRenderPass<RenderDistortionPassData>("Apply Distortion", out var passData, ProfilingSampler.Get(HDProfileId.ApplyDistortion)))
            {
                passData.applyDistortionMaterial = m_ApplyDistortionMaterial;
                passData.colorPyramidBuffer = builder.ReadTexture(colorPyramidBuffer);
                passData.distortionBuffer = builder.ReadTexture(distortionBuffer);
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.size = new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);

                builder.SetRenderFunc(
                (RenderDistortionPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    // TODO: Set stencil stuff via parameters rather than hard-coding it in shader.
                    data.applyDistortionMaterial.SetTexture(HDShaderIDs._DistortionTexture, res.GetTexture(data.distortionBuffer));
                    data.applyDistortionMaterial.SetTexture(HDShaderIDs._ColorPyramidTexture, res.GetTexture(data.colorPyramidBuffer));
                    data.applyDistortionMaterial.SetVector(HDShaderIDs._Size, data.size);
                    data.applyDistortionMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.DistortionVectors);
                    data.applyDistortionMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.DistortionVectors);

                    HDUtils.DrawFullScreen(context.cmd, data.applyDistortionMaterial, res.GetTexture(data.colorBuffer), res.GetTexture(data.depthStencilBuffer), null, 0);
                });
            }
        }

        TextureHandle CreateColorBuffer(RenderGraph renderGraph, HDCamera hdCamera, bool msaa)
        {

#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc colorFastMemDesc;
            colorFastMemDesc.inFastMemory = true;
            colorFastMemDesc.residencyFraction = 1.0f;
            colorFastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            return renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    enableMSAA = msaa,
                    clearBuffer = NeedClearColorBuffer(hdCamera),
                    clearColor = GetColorBufferClearColor(hdCamera),
                    name = msaa ? "CameraColorMSAA" : "CameraColor"
#if UNITY_2020_2_OR_NEWER
                    , fastMemoryDesc = colorFastMemDesc
#endif
                });
        }

        class ResolveColorData
        {
            public TextureHandle    input;
            public TextureHandle    output;
            public Material                     resolveMaterial;
            public int                          passIndex;
        }

        TextureHandle ResolveMSAAColor(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle input)
        {
            var outputDesc = renderGraph.GetTextureDesc(input);
            outputDesc.enableMSAA = false;
            outputDesc.enableRandomWrite = true;
            outputDesc.bindTextureMS = false;
            // Can't do that because there is NO way to concatenate strings without allocating.
            // We're stuck with subpar debug name in the meantime...
            //outputDesc.name = string.Format("{0}Resolved", outputDesc.name);

            var output = renderGraph.CreateTexture(outputDesc);

            return ResolveMSAAColor(renderGraph, hdCamera, input, output);
        }

        // TODO RENDERGRAPH:
        // In theory we should never need to specify the output. The function can create the output texture on its own (see function above).
        // This way when doing an msaa resolve, we can return the right texture regardless of msaa being enabled or not (either the new texture or the input directly).
        // This allows client code to not have to worry about managing the texture at all.
        // Now, because Custom Passes allow to do an MSAA resolve for the main color buffer but are implemented outside of render graph, we need an explicit msaa/nonMsaa separation for the main color buffer.
        // Having this function here allows us to do that by having the main color non msaa texture created outside and passed to both ResolveMSAAColor and the custom passes.
        // When Custom Pass correctly use render graph we'll be able to remove that.
        TextureHandle ResolveMSAAColor(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle input, TextureHandle output)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                using (var builder = renderGraph.AddRenderPass<ResolveColorData>("ResolveColor", out var passData))
                {
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(output, 0);
                    passData.resolveMaterial = m_ColorResolveMaterial;
                    passData.passIndex = SampleCountToPassIndex(m_MSAASamples);

                    builder.SetRenderFunc(
                    (ResolveColorData data, RenderGraphContext context) =>
                    {
                        var res = context.resources;
                        var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetTexture(HDShaderIDs._ColorTextureMS, res.GetTexture(data.input));
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.resolveMaterial, data.passIndex, MeshTopology.Triangles, 3, 1, mpb);
                    });

                    return passData.output;
                }
            }
            else
            {
                return input;
            }
        }

        class ResolveMotionVectorData
        {
            public TextureHandle input;
            public TextureHandle output;
            public Material resolveMaterial;
            public int passIndex;
        }

        TextureHandle ResolveMotionVector(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle input)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                using (var builder = renderGraph.AddRenderPass<ResolveMotionVectorData>("ResolveMotionVector", out var passData))
                {
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(CreateMotionVectorBuffer(renderGraph, false, false), 0);
                    passData.resolveMaterial = m_MotionVectorResolve;
                    passData.passIndex = SampleCountToPassIndex(m_MSAASamples);

                    builder.SetRenderFunc(
                        (ResolveMotionVectorData data, RenderGraphContext context) =>
                        {
                            var res = context.resources;
                            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpb.SetTexture(HDShaderIDs._MotionVectorTextureMS, res.GetTexture(data.input));
                            context.cmd.DrawProcedural(Matrix4x4.identity, data.resolveMaterial, data.passIndex, MeshTopology.Triangles, 3, 1, mpb);
                        });

                    return passData.output;
                }
            }
            else
            {
                return input;
            }
        }

        class RenderAccumulationPassData
        {
            public RenderAccumulationParameters parameters;
            public TextureHandle input;
            public TextureHandle output;
            public TextureHandle history;
        }

        void RenderAccumulation(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputTexture, TextureHandle outputTexture, bool needExposure)
        {
            using (var builder = renderGraph.AddRenderPass<RenderAccumulationPassData>("Render Accumulation", out var passData))
            {
                // Grab the history buffer (hijack the reflections one)
                TextureHandle history = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracing, PathTracingHistoryBufferAllocatorFunction, 1));


                passData.parameters = PrepareRenderAccumulationParameters(hdCamera, needExposure);
                passData.input = builder.ReadTexture(inputTexture);
                passData.output = builder.WriteTexture(inputTexture);
                passData.history = builder.WriteTexture(history);

                builder.SetRenderFunc(
                (RenderAccumulationPassData data, RenderGraphContext ctx) =>
                {
                    RenderAccumulation(data.parameters, ctx.resources.GetTexture(data.input), ctx.resources.GetTexture(data.output), ctx.resources.GetTexture(data.history), ctx.cmd);
                });

            }
        }

#if UNITY_EDITOR
        class RenderGizmosPassData
        {
            public GizmoSubset  gizmoSubset;
            public Camera       camera;
            public Texture      exposureTexture;
        }
#endif

        void RenderGizmos(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos() &&
                (hdCamera.camera.cameraType == CameraType.Game || hdCamera.camera.cameraType == CameraType.SceneView))
            {
                bool renderPrePostprocessGizmos = (gizmoSubset == GizmoSubset.PreImageEffects);
                using (var builder = renderGraph.AddRenderPass<RenderGizmosPassData>(renderPrePostprocessGizmos ? "PrePostprocessGizmos" : "Gizmos", out var passData))
                {
                    bool isMatCapView = m_CurrentDebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.MatcapView;

                    builder.WriteTexture(colorBuffer);
                    passData.gizmoSubset = gizmoSubset;
                    passData.camera = hdCamera.camera;
                    passData.exposureTexture = isMatCapView ? (Texture)Texture2D.blackTexture : m_PostProcessSystem.GetExposureTexture(hdCamera).rt;

                    builder.SetRenderFunc(
                    (RenderGizmosPassData data, RenderGraphContext ctx) =>
                    {
                        Gizmos.exposure = data.exposureTexture;

                        ctx.renderContext.ExecuteCommandBuffer(ctx.cmd);
                        ctx.cmd.Clear();
                        ctx.renderContext.DrawGizmos(data.camera, data.gizmoSubset);
                    });
                }
            }
#endif
        }

        bool RenderCustomPass(  RenderGraph                 renderGraph,
                                HDCamera                    hdCamera,
                                TextureHandle               colorBuffer,
                                TextureHandle               depthBuffer,
                                TextureHandle               normalBuffer,
                                CullingResults              cullingResults,
                                CustomPassInjectionPoint    injectionPoint,
                                AOVRequestData              aovRequest,
                                List<RTHandle>              aovCustomPassBuffers)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                return false;

            bool executed = false;
            CustomPassVolume.GetActivePassVolumes(injectionPoint, m_ActivePassVolumes);
            foreach (var customPass in m_ActivePassVolumes)
            {
                if (customPass == null)
                    return false;

                var customPassTargets = new CustomPass.RenderTargets
                {
                    useRenderGraph = true,

                    // Set to null to make sure we don't use them by mistake.
                    cameraColorMSAABuffer = null,
                    cameraColorBuffer = null,
                    // TODO RENDERGRAPH: we can't replace the Lazy<RTHandle> buffers with RenderGraph resource because they are part of the current public API.
                    // To replace them correctly we need users to actually write render graph passes and explicit whether or not they want to use those buffers.
                    // We'll do it when we switch fully to render graph for custom passes.
                    customColorBuffer = m_CustomPassColorBuffer,
                    customDepthBuffer = m_CustomPassDepthBuffer,

                    // Render Graph Specific textures
                    colorBufferRG = colorBuffer,
                    nonMSAAColorBufferRG = m_NonMSAAColorBuffer,
                    depthBufferRG = depthBuffer,
                    normalBufferRG = normalBuffer,
                };
                executed |= customPass.Execute(renderGraph, hdCamera, cullingResults, customPassTargets);
            }

            // Push the custom pass buffer, in case it was requested in the AOVs
            aovRequest.PushCustomPassTexture(renderGraph, injectionPoint, colorBuffer, m_CustomPassColorBuffer, aovCustomPassBuffers);

            return executed;
        }

        class BindCustomPassBuffersPassData
        {
            public Lazy<RTHandle> customColorTexture;
            public Lazy<RTHandle> customDepthTexture;
        }

        void BindCustomPassBuffers(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
            {
                using (var builder = renderGraph.AddRenderPass("Bind Custom Pass Buffers", out BindCustomPassBuffersPassData passData))
                {
                    passData.customColorTexture = m_CustomPassColorBuffer;
                    passData.customDepthTexture = m_CustomPassDepthBuffer;

                    builder.SetRenderFunc(
                    (BindCustomPassBuffersPassData data, RenderGraphContext ctx) =>
                    {
                        if (data.customColorTexture.IsValueCreated)
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._CustomColorTexture, data.customColorTexture.Value);
                        if (data.customDepthTexture.IsValueCreated)
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._CustomDepthTexture, data.customDepthTexture.Value);
                    });
                }
            }
        }

#if UNITY_EDITOR
        class RenderWireOverlayPassData
        {
            public HDCamera hdCamera;
        }
#endif

        void RenderWireOverlay(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
        {
#if UNITY_EDITOR
            if (hdCamera.camera.cameraType == CameraType.SceneView)
            {
                using (var builder = renderGraph.AddRenderPass<RenderWireOverlayPassData>("Wire Overlay", out var passData))
                {
                    builder.WriteTexture(colorBuffer);
                    passData.hdCamera = hdCamera;

                    builder.SetRenderFunc(
                    (RenderWireOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.renderContext.ExecuteCommandBuffer(ctx.cmd);
                        ctx.cmd.Clear();
                        ctx.renderContext.DrawWireOverlay(data.hdCamera.camera);
                    });
                }
            }
#endif
        }
    }
}
