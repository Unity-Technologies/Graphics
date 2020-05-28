using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        void ExecuteWithRenderGraph(    RenderRequest           renderRequest,
                                        AOVRequestData          aovRequest,
                                        List<RTHandle>          aovBuffers,
                                        ScriptableRenderContext renderContext,
                                        CommandBuffer           commandBuffer)
        {
            var hdCamera = renderRequest.hdCamera;
            var camera = hdCamera.camera;
            var cullingResults = renderRequest.cullingResults.cullingResults;
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            var target = renderRequest.target;

#if UNITY_EDITOR
            var showGizmos = camera.cameraType == CameraType.Game
                || camera.cameraType == CameraType.SceneView;
#endif

            TextureHandle backBuffer = m_RenderGraph.ImportBackbuffer(target.id);
            TextureHandle colorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, msaa);
            TextureHandle currentColorPyramid = m_RenderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain), HDShaderIDs._ColorPyramidTexture);

            LightingBuffers lightingBuffers = new LightingBuffers();
            lightingBuffers.diffuseLightingBuffer = CreateDiffuseLightingBuffer(m_RenderGraph, msaa);
            lightingBuffers.sssBuffer = CreateSSSBuffer(m_RenderGraph, msaa);

            var prepassOutput = RenderPrepass(m_RenderGraph, lightingBuffers.sssBuffer, cullingResults, hdCamera);

            // Need this during debug render at the end outside of the main loop scope.
            // Once render graph move is implemented, we can probably remove the branch and this.
            ShadowResult shadowResult = new ShadowResult();

            if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() || m_CurrentDebugDisplaySettings.IsMaterialValidationEnabled() || CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
            {
                using (new XRSinglePassScope(m_RenderGraph, hdCamera))
                {
                    RenderDebugViewMaterial(m_RenderGraph, cullingResults, hdCamera, colorBuffer);
                    colorBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, colorBuffer);
                }
            }
            else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                     hdCamera.volumeStack.GetComponent<PathTracing>().enable.value)
            {
                // TODO RENDERGRAPH
                //// Update the light clusters that we need to update
                //BuildRayTracingLightCluster(cmd, hdCamera);

                //// We only request the light cluster if we are gonna use it for debug mode
                //if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                //{
                //    HDRaytracingLightCluster lightCluster = RequestLightCluster();
                //    lightCluster.EvaluateClusterDebugView(cmd, hdCamera);
                //}

                //RenderPathTracing(hdCamera, cmd, m_CameraColorBuffer, renderContext, m_FrameCount);
            }
            else
            {
                var gpuLightListOutput = BuildGPULightList(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.gbuffer);

                lightingBuffers.ambientOcclusionBuffer = m_AmbientOcclusionSystem.Render(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, prepassOutput.motionVectorsBuffer, m_FrameCount);
                // Should probably be inside the AO render function but since it's a separate class it's currently not super clean to do.
                PushFullScreenDebugTexture(m_RenderGraph, lightingBuffers.ambientOcclusionBuffer, FullScreenDebugMode.SSAO);

                // Evaluate the clear coat mask texture based on the lit shader mode
                var clearCoatMask = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? prepassOutput.gbuffer.mrt[2] : m_RenderGraph.defaultResources.blackTextureXR;
                lightingBuffers.ssrLightingBuffer = RenderSSR(m_RenderGraph,
                                                              hdCamera,
                                                              prepassOutput.resolvedNormalBuffer,
                                                              prepassOutput.resolvedMotionVectorsBuffer,
                                                              prepassOutput.depthBuffer,
                                                              prepassOutput.depthPyramidTexture,
                                                              prepassOutput.stencilBuffer,
                                                              clearCoatMask);

                lightingBuffers.contactShadowsBuffer = RenderContactShadows(m_RenderGraph, hdCamera, msaa ? prepassOutput.depthValuesMSAA : prepassOutput.depthPyramidTexture, gpuLightListOutput, GetDepthBufferMipChainInfo().mipLevelOffsets[1].y);

                var volumetricDensityBuffer = VolumeVoxelizationPass(m_RenderGraph, hdCamera, m_VisibleVolumeBoundsBuffer, m_VisibleVolumeDataBuffer, gpuLightListOutput.bigTileLightList, m_FrameCount);

                shadowResult = RenderShadows(m_RenderGraph, hdCamera, cullingResults);

                // TODO RENDERGRAPH
                //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                //{
                //    // Update the light clusters that we need to update
                //    BuildRayTracingLightCluster(cmd, hdCamera);

                //    // We only request the light cluster if we are gonna use it for debug mode
                //    if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                //    {
                //        HDRaytracingLightCluster lightCluster = RequestLightCluster();
                //        lightCluster.EvaluateClusterDebugView(cmd, hdCamera);
                //    }

                //    bool validIndirectDiffuse = ValidIndirectDiffuseState(hdCamera);
                //    if (validIndirectDiffuse)
                //    {
                //        RenderIndirectDiffuse(hdCamera, cmd, renderContext, m_FrameCount);
                //    }
                //}

                // TODO RENDERGRAPH
                //using (new ProfilingSample(cmd, "Render screen space shadows", CustomSamplerId.ScreenSpaceShadows.GetSampler()))
                //{
                //    hdCamera.xr.StartSinglePass(cmd);
                //    RenderScreenSpaceShadows(hdCamera, cmd);
                //    hdCamera.xr.StopSinglePass(cmd);
                //}

                var volumetricLighting = VolumetricLightingPass(m_RenderGraph, hdCamera, volumetricDensityBuffer, gpuLightListOutput.bigTileLightList, shadowResult, m_FrameCount);

                StartXRSinglePass(m_RenderGraph, hdCamera);

                var deferredLightingOutput = RenderDeferredLighting(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, lightingBuffers, prepassOutput.gbuffer, shadowResult, gpuLightListOutput);

                RenderForwardOpaque(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, prepassOutput.depthBuffer, shadowResult, prepassOutput.dbuffer, cullingResults);

                // TODO RENDERGRAPH : Move this to the end after we do move semantic and graph pruning to avoid doing the rest of the frame for nothing
                aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Normals, hdCamera, prepassOutput.resolvedNormalBuffer, aovBuffers);

                lightingBuffers.diffuseLightingBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.diffuseLightingBuffer);
                lightingBuffers.sssBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, lightingBuffers.sssBuffer);

                RenderSubsurfaceScattering(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, ref prepassOutput);

                RenderForwardEmissive(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, cullingResults);

                RenderSky(m_RenderGraph, hdCamera, colorBuffer, volumetricLighting, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture);

                // TODO RENDERGRAPH
                // Send all the geometry graphics buffer to client systems if required (must be done after the pyramid and before the transparent depth pre-pass)
                //SendGeometryGraphicsBuffers(cmd, hdCamera);

                // TODO RENDERGRAPH
                //m_PostProcessSystem.DoUserAfterOpaqueAndSky(cmd, hdCamera, m_CameraColorBuffer);

                // TODO RENDERGRAPH
                // No need for old stencil values here since from transparent on different features are tagged
                //ClearStencilBuffer(hdCamera, cmd);

                colorBuffer = RenderTransparency(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.motionVectorsBuffer, currentColorPyramid, prepassOutput.depthPyramidTexture, shadowResult, cullingResults);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector))
                {
                    prepassOutput.motionVectorsBuffer = ResolveMotionVector(m_RenderGraph, hdCamera, prepassOutput.motionVectorsBuffer);
                }

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

                // Render gizmos that should be affected by post processes
                RenderGizmos(m_RenderGraph, hdCamera, colorBuffer, GizmoSubset.PreImageEffects);
            }

            // At this point, the color buffer has been filled by either debug views are regular rendering so we can push it here.
            var colorPickerTexture = PushColorPickerDebugTexture(m_RenderGraph, colorBuffer);

            // TODO RENDERGRAPH
            //RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.BeforePostProcess);

            aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Color, hdCamera, colorBuffer, aovBuffers);

            TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, colorBuffer, prepassOutput.depthBuffer, backBuffer, cullingResults, hdCamera);

            // TODO RENDERGRAPH
            // RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.AfterPostProcess);

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
                                                shadowResult,
                                                cullingResults);

                BlitFinalCameraTexture(m_RenderGraph, hdCamera, postProcessDest, backBuffer, prepassOutput.resolvedMotionVectorsBuffer, prepassOutput.resolvedNormalBuffer);

                aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Output, hdCamera, colorBuffer, aovBuffers);
            }

            // XR mirror view and blit do device
            EndCameraXR(m_RenderGraph, hdCamera);

            // TODO RENDERGRAPH
            // Send all the color graphics buffer to client systems if required.
            //SendColorGraphicsBuffer(cmd, hdCamera);

            SetFinalTarget(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, target.id);

            RenderGizmos(m_RenderGraph, hdCamera, colorBuffer, GizmoSubset.PostImageEffects);

            ExecuteRenderGraph(m_RenderGraph, hdCamera, m_MSAASamples, renderContext, commandBuffer );

            aovRequest.Execute(commandBuffer, aovBuffers, RenderOutputProperties.From(hdCamera));
        }

        static void ExecuteRenderGraph(RenderGraph renderGraph, HDCamera hdCamera, MSAASamples msaaSample, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            var renderGraphParams = new RenderGraphExecuteParams()
            {
                renderingWidth = hdCamera.actualWidth,
                renderingHeight = hdCamera.actualHeight,
                msaaSamples = msaaSample
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

                // TODO REMOVE: Dummy read to avoid early release before render graph is full implemented.
                bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                    builder.ReadTexture(motionVectors);
                builder.ReadTexture(normalBuffer);

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
            public bool                     copyDepth;
            public Material                 copyDepthMaterial;
            public RenderTargetIdentifier   finalTarget;
            public Rect                     finalViewport;
            public TextureHandle            depthBuffer;
            public bool                     flipY;
        }

        void SetFinalTarget(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, RenderTargetIdentifier finalTarget)
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
                passData.finalTarget = finalTarget;
                passData.finalViewport = hdCamera.finalViewport;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.flipY = hdCamera.isMainGameView;

                builder.SetRenderFunc(
                (SetFinalTargetPassData data, RenderGraphContext ctx) =>
                {
                    // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
                    ctx.cmd.SetRenderTarget(data.finalTarget);
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
            public int                          renderTargetCount;
            public TextureHandle        depthBuffer;
            public ComputeBuffer                lightListBuffer;
            public FrameSettings                frameSettings;
            public bool                         decalsEnabled;
            public bool                         renderMotionVecForTransparent;
        }

        void PrepareForwardPassData(RenderGraph renderGraph, RenderGraphBuilder builder, ForwardPassData data, bool opaque, FrameSettings frameSettings, RendererListDesc rendererListDesc, TextureHandle depthBuffer, ShadowResult shadowResult, DBufferOutput? dbuffer = null)
        {
            bool useFptl = frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque) && opaque;

            data.frameSettings = frameSettings;
            data.lightListBuffer = useFptl ? m_TileAndClusterData.lightList: m_TileAndClusterData.perVoxelLightLists;
            data.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
            data.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));
            // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
            // decal datas count is 0 if no decals affect transparency
            data.decalsEnabled = (frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
            data.renderMotionVecForTransparent = NeedMotionVectorForTransparent(frameSettings);

            HDShadowManager.ReadShadowResult(shadowResult, builder);
            if (dbuffer != null)
                ReadDBuffer(dbuffer.Value, builder);
        }

        // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
        // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
        // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
        // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
        // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
        // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"
        void RenderForwardOpaque(   RenderGraph                 renderGraph,
                                    HDCamera                    hdCamera,
                                    TextureHandle       colorBuffer,
                                    in LightingBuffers          lightingBuffers,
                                    TextureHandle       depthBuffer,
                                    ShadowResult                shadowResult,
                                    DBufferOutput               dbuffer,
                                    CullingResults              cullResults)
        {
            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>(    debugDisplay ? "Forward Opaque Debug" : "Forward Opaque",
                                                                                out var passData,
                                                                                debugDisplay ? ProfilingSampler.Get(HDProfileId.ForwardOpaqueDebug) : ProfilingSampler.Get(HDProfileId.ForwardOpaque)))
            {
                PrepareForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, PrepareForwardOpaqueRendererList(cullResults, hdCamera), depthBuffer, shadowResult, dbuffer);

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
                    // TODO: replace with UseColorBuffer when removing old rendering (SetRenderTarget is called inside RenderForwardRendererList because of that).
                    var mrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.renderTargetCount);
                    for (int i = 0; i < data.renderTargetCount; ++i)
                        mrt[i] = context.resources.GetTexture(data.renderTarget[i]);

                    RenderForwardRendererList(data.frameSettings,
                            context.resources.GetRendererList(data.rendererList),
                            mrt,
                            context.resources.GetTexture(data.depthBuffer),
                            data.lightListBuffer,
                            true, context.renderContext, context.cmd);
                });
            }
        }

        void RenderForwardTransparent(  RenderGraph                 renderGraph,
                                        HDCamera                    hdCamera,
                                        TextureHandle   colorBuffer,
                                        TextureHandle   motionVectorBuffer,
                                        TextureHandle   depthBuffer,
                                        TextureHandle?  colorPyramid,
                                        ShadowResult                shadowResult,
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
                PrepareForwardPassData(renderGraph, builder, passData, false, hdCamera.frameSettings, PrepareForwardTransparentRendererList(cullResults, hdCamera, preRefractionPass), depthBuffer, shadowResult);

                bool renderMotionVecForTransparent = NeedMotionVectorForTransparent(hdCamera.frameSettings);

                passData.renderTargetCount = 2;
                passData.renderTarget[0] = builder.WriteTexture(colorBuffer);

                if (renderMotionVecForTransparent)
                {
                    passData.renderTarget[1] = builder.WriteTexture(motionVectorBuffer);
                    // TODO RENDERGRAPH
                    // WORKAROUND VELOCITY-MSAA
                    // This is a workaround for velocity with MSAA. Currently motion vector resolve is not implemented with MSAA
                    // It means that the msaa motion vector target is never read and such released as soon as it's created. In such a case, trying to write it here will generate a render graph error.
                    // So, until we implement it correctly, we'll just force a read here to extend lifetime.
                    builder.ReadTexture(motionVectorBuffer);
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

                    context.cmd.SetGlobalInt(HDShaderIDs._ColorMaskTransparentVel, data.renderMotionVecForTransparent ? (int)ColorWriteMask.All : 0);
                    if (data.decalsEnabled)
                        DecalSystem.instance.SetAtlas(context.cmd); // for clustered decals

                    RenderForwardRendererList(  data.frameSettings,
                                                context.resources.GetRendererList(data.rendererList),
                                                mrt,
                                                context.resources.GetTexture(data.depthBuffer),
                                                data.lightListBuffer,
                                                false, context.renderContext, context.cmd);
                });
            }
        }

        void RenderTransparentDepthPrepass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, CullingResults cull)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPrepass))
                return;

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>("Transparent Depth Prepass", out var passData, ProfilingSampler.Get(HDProfileId.TransparentDepthPrepass)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);
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

        TextureHandle RenderTransparency(   RenderGraph     renderGraph,
                                                        HDCamera                    hdCamera,
                                            TextureHandle   colorBuffer,
                                            TextureHandle   depthStencilBuffer,
                                            TextureHandle   motionVectorsBuffer,
                                            TextureHandle   currentColorPyramid,
                                            TextureHandle   depthPyramid,
                                                        ShadowResult                shadowResult,
                                                        CullingResults              cullingResults)
        {
            RenderTransparentDepthPrepass(renderGraph, hdCamera, depthStencilBuffer, cullingResults);

            // TODO RENDERGRAPH
            //RenderSSRTransparent(hdCamera, cmd, renderContext);

            //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            //{
            //    RaytracingRecursiveRender(hdCamera, cmd, renderContext, cullingResults);
            //}

            // TODO RENDERGRAPH
            // To allow users to fetch the current color buffer, we temporarily bind the camera color buffer
            //cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, m_CameraColorBuffer);
            //RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.BeforePreRefraction);

            // Render pre-refraction objects
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, motionVectorsBuffer, depthStencilBuffer, null, shadowResult, cullingResults, true);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
            {
                var resolvedColorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer);
                GenerateColorPyramid(renderGraph, hdCamera, resolvedColorBuffer, currentColorPyramid, true);
            }

            // TODO RENDERGRAPH
            // We don't have access to the color pyramid with transparent if rough refraction is disabled
            //RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.BeforeTransparent);

            // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, motionVectorsBuffer, depthStencilBuffer, currentColorPyramid, shadowResult, cullingResults, false);

            // We push the motion vector debug texture here as transparent object can overwrite the motion vector texture content.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                PushFullScreenDebugTexture(m_RenderGraph, motionVectorsBuffer, FullScreenDebugMode.MotionVectors);

            colorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer);

            // Render All forward error
            RenderForwardError(renderGraph, hdCamera, colorBuffer, depthStencilBuffer, cullingResults);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                var downsampledDepth = DownsampleDepthForLowResTransparency(renderGraph, hdCamera, depthPyramid);
                var lowResTransparentBuffer = RenderLowResTransparent(renderGraph, hdCamera, downsampledDepth, cullingResults);
                UpsampleTransparent(renderGraph, hdCamera, colorBuffer, lowResTransparentBuffer, downsampledDepth);
            }

            // Fill depth buffer to reduce artifact for transparent object during postprocess
            RenderTransparentDepthPostpass(renderGraph, hdCamera, depthStencilBuffer, cullingResults);

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
                });
            }

            // Note: hdCamera.colorPyramidHistoryMipCount is going to be one frame late here (rendering, which is done later, is updating it)
            // In practice this should not be a big problem as it's only for debug purpose here.
            var scale = new Vector4(renderGraph.rtHandleProperties.rtHandleScale.x, renderGraph.rtHandleProperties.rtHandleScale.y, 0f, 0f);
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
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Write);
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
            public Material                     applyDistortionMaterial;
            public TextureHandle    colorPyramidBuffer;
            public TextureHandle    distortionBuffer;
            public TextureHandle    colorBuffer;
            public TextureHandle    depthStencilBuffer;
            public Vector4                      size;
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
            return renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    enableMSAA = msaa,
                    clearBuffer = NeedClearColorBuffer(hdCamera),
                    clearColor = GetColorBufferClearColor(hdCamera),
                    name = string.Format("CameraColor{0}", msaa ? "MSAA" : "")});
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
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                using (var builder = renderGraph.AddRenderPass<ResolveColorData>("ResolveColor", out var passData))
                {
                    var outputDesc = renderGraph.GetTextureDesc(input);
                    outputDesc.enableMSAA = false;
                    outputDesc.enableRandomWrite = true;
                    outputDesc.bindTextureMS = false;
                    outputDesc.name = string.Format("{0}Resolved", outputDesc.name);

                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(outputDesc), 0);
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
                    var outputDesc = renderGraph.GetTextureDesc(input);
                    outputDesc.enableMSAA = false;
                    outputDesc.enableRandomWrite = true;
                    outputDesc.bindTextureMS = false;
                    outputDesc.name = string.Format("{0}Resolved", outputDesc.name);

                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(outputDesc), 0);
                    passData.resolveMaterial = m_MotionVectorResolve;
                    passData.passIndex = SampleCountToPassIndex(m_MSAASamples);

                    builder.SetRenderFunc(
                        (ResolveColorData data, RenderGraphContext context) =>
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
    }
}
