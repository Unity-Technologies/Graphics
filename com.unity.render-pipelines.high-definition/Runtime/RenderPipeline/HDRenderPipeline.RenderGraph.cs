using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.VFX;
using UnityEngine.Rendering.RendererUtils;

// Resove the ambiguity in the RendererList name (pick the in-engine version)
using RendererList = UnityEngine.Rendering.RendererUtils.RendererList;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Needed only because of custom pass. See comment at ResolveMSAAColor.
        TextureHandle m_NonMSAAColorBuffer;

        // Used when the user wants to override the internal rendering format for the AOVs.
        internal bool m_ShouldOverrideColorBufferFormat = false;
        GraphicsFormat m_AOVGraphicsFormat = GraphicsFormat.None;

        // Property used for transparent motion vector color mask - depends on presence of virtual texturing or not
        int colorMaskTransparentVel;
        int colorMaskAdditionalTarget;

        void RecordRenderGraph(RenderRequest renderRequest,
            AOVRequestData aovRequest,
            List<RTHandle> aovBuffers,
            List<RTHandle> aovCustomPassBuffers,
            ScriptableRenderContext renderContext,
            CommandBuffer commandBuffer)
        {
            using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(HDProfileId.RecordRenderGraph)))
            {
                var hdCamera = renderRequest.hdCamera;
                var camera = hdCamera.camera;
                var cullingResults = renderRequest.cullingResults.cullingResults;
                var customPassCullingResults = renderRequest.cullingResults.customPassCullingResults ?? cullingResults;
                bool msaa = hdCamera.msaaEnabled;
                var target = renderRequest.target;

                // We need to initialize the MipChainInfo here, so it will be available to any render graph pass that wants to use it during setup
                // Be careful, ComputePackedMipChainInfo needs the render texture size and not the viewport size. Otherwise it would compute the wrong size.
                hdCamera.depthBufferMipChainInfo.ComputePackedMipChainInfo(RTHandles.rtHandleProperties.currentRenderTargetSize);

#if UNITY_EDITOR
                var showGizmos = camera.cameraType == CameraType.Game
                    || camera.cameraType == CameraType.SceneView;
#endif

                // Set the default color buffer format for full screen debug rendering
                GraphicsFormat fullScreenDebugFormat = GraphicsFormat.R16G16B16A16_SFloat;
                if (aovRequest.isValid && aovRequest.overrideRenderFormat)
                {
                    // If we are going to output AOVs, then override the debug format from the user-provided buffers
                    aovRequest.OverrideBufferFormatForAOVs(ref fullScreenDebugFormat, aovBuffers);

                    // Also override the internal rendering format (this would affect all calls to GetColorBufferFormat)
                    m_ShouldOverrideColorBufferFormat = true;
                    m_AOVGraphicsFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.colorBufferFormat;
                    aovRequest.OverrideBufferFormatForAOVs(ref m_AOVGraphicsFormat, aovBuffers);
                }
                else
                {
                    m_ShouldOverrideColorBufferFormat = false;
                }

                UpdateParentExposure(m_RenderGraph, hdCamera);

                TextureHandle backBuffer = m_RenderGraph.ImportBackbuffer(target.id);
                TextureHandle colorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, msaa);
                m_NonMSAAColorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, false);
                TextureHandle currentColorPyramid = m_RenderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain));
                TextureHandle rayCountTexture = RayCountManager.CreateRayCountTexture(m_RenderGraph);
#if ENABLE_VIRTUALTEXTURES
                TextureHandle vtFeedbackBuffer = VTBufferManager.CreateVTFeedbackBuffer(m_RenderGraph, hdCamera.msaaSamples);
                bool resolveVirtualTextureFeedback = true;
#else
                TextureHandle vtFeedbackBuffer = TextureHandle.nullHandle;
#endif

                LightingBuffers lightingBuffers = new LightingBuffers();
                lightingBuffers.diffuseLightingBuffer = CreateDiffuseLightingBuffer(m_RenderGraph, hdCamera.msaaSamples);
                lightingBuffers.sssBuffer = CreateSSSBuffer(m_RenderGraph, hdCamera, hdCamera.msaaSamples);

                var prepassOutput = RenderPrepass(m_RenderGraph, colorBuffer, lightingBuffers.sssBuffer, vtFeedbackBuffer, cullingResults, customPassCullingResults, hdCamera, aovRequest, aovBuffers);

                // Need this during debug render at the end outside of the main loop scope.
                // Once render graph move is implemented, we can probably remove the branch and this.
                ShadowResult shadowResult = new ShadowResult();
                BuildGPULightListOutput gpuLightListOutput = new BuildGPULightListOutput();
                TextureHandle sunOcclusionTexture = m_RenderGraph.defaultResources.whiteTexture;

                if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() && m_CurrentDebugDisplaySettings.IsFullScreenDebugPassEnabled())
                {
                    // Stop Single Pass is after post process.
                    StartXRSinglePass(m_RenderGraph, hdCamera);

                    RenderFullScreenDebug(m_RenderGraph, colorBuffer, prepassOutput.depthBuffer, cullingResults, hdCamera);

#if ENABLE_VIRTUALTEXTURES
                    resolveVirtualTextureFeedback = false; // Could be handled but not needed for fullscreen debug pass currently
#endif
                }
                else if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() || m_CurrentDebugDisplaySettings.IsMaterialValidationEnabled() || CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
                {
                    gpuLightListOutput = BuildGPULightList(m_RenderGraph, hdCamera, m_TileAndClusterData, m_TotalLightCount, ref m_ShaderVariablesLightListCB, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.gbuffer);

                    // For alpha output in AOVs or debug views, in case we have a shadow matte material, we need to render the shadow maps
                    if (m_CurrentDebugDisplaySettings.data.materialDebugSettings.debugViewMaterialCommonValue == Attributes.MaterialSharedProperty.Alpha)
                        RenderShadows(m_RenderGraph, hdCamera, cullingResults, ref shadowResult);
                    else
                        HDShadowManager.BindDefaultShadowGlobalResources(m_RenderGraph);

                    // Stop Single Pass is after post process.
                    StartXRSinglePass(m_RenderGraph, hdCamera);

                    colorBuffer = RenderDebugViewMaterial(m_RenderGraph, cullingResults, hdCamera, gpuLightListOutput, prepassOutput.dbuffer, prepassOutput.gbuffer, prepassOutput.depthBuffer, vtFeedbackBuffer);
                    colorBuffer = ResolveMSAAColor(m_RenderGraph, hdCamera, colorBuffer);
                }
                else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                         hdCamera.volumeStack.GetComponent<PathTracing>().enable.value &&
                         hdCamera.camera.cameraType != CameraType.Preview)
                {
                    //// We only request the light cluster if we are gonna use it for debug mode
                    //if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                    //{
                    //    HDRaytracingLightCluster lightCluster = RequestLightCluster();
                    //    lightCluster.EvaluateClusterDebugView(cmd, hdCamera);
                    //}

                    if (hdCamera.viewCount == 1)
                    {
                        colorBuffer = RenderPathTracing(m_RenderGraph, hdCamera, colorBuffer);
                    }
                    else
                    {
                        Debug.LogWarning("Path Tracing is not supported with XR single-pass rendering.");
                    }

#if ENABLE_VIRTUALTEXTURES
                    resolveVirtualTextureFeedback = false;
#endif
                }
                else
                {
                    gpuLightListOutput = BuildGPULightList(m_RenderGraph, hdCamera, m_TileAndClusterData, m_TotalLightCount, ref m_ShaderVariablesLightListCB, prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.gbuffer);

                    // Evaluate the history validation buffer that may be required by temporal accumulation based effects
                    TextureHandle historyValidationTexture = EvaluateHistoryValidationBuffer(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.resolvedNormalBuffer, prepassOutput.resolvedMotionVectorsBuffer);

                    lightingBuffers.ambientOcclusionBuffer = RenderAmbientOcclusion(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, prepassOutput.resolvedNormalBuffer, prepassOutput.resolvedMotionVectorsBuffer, historyValidationTexture, hdCamera.depthBufferMipChainInfo, m_ShaderVariablesRayTracingCB, rayCountTexture);
                    lightingBuffers.contactShadowsBuffer = RenderContactShadows(m_RenderGraph, hdCamera, msaa ? prepassOutput.depthValuesMSAA : prepassOutput.depthPyramidTexture, gpuLightListOutput, hdCamera.depthBufferMipChainInfo.mipLevelOffsets[1].y);

                    var volumetricDensityBuffer = VolumeVoxelizationPass(m_RenderGraph, hdCamera, m_VisibleVolumeBoundsBuffer, m_VisibleVolumeDataBuffer, gpuLightListOutput.bigTileLightList);

                    RenderShadows(m_RenderGraph, hdCamera, cullingResults, ref shadowResult);

                    StartXRSinglePass(m_RenderGraph, hdCamera);

                    // Evaluate the clear coat mask texture based on the lit shader mode
                    var clearCoatMask = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? prepassOutput.gbuffer.mrt[2] : m_RenderGraph.defaultResources.blackTextureXR;
                    lightingBuffers.ssrLightingBuffer = RenderSSR(m_RenderGraph, hdCamera, ref prepassOutput, clearCoatMask, rayCountTexture, m_SkyManager.GetSkyReflection(hdCamera), transparent: false);
                    lightingBuffers.ssgiLightingBuffer = RenderScreenSpaceIndirectDiffuse(hdCamera, prepassOutput, rayCountTexture, historyValidationTexture, gpuLightListOutput.lightList);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && GetRayTracingClusterState())
                    {
                        HDRaytracingLightCluster lightCluster = RequestLightCluster();
                        lightCluster.EvaluateClusterDebugView(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture);
                    }

                    lightingBuffers.screenspaceShadowBuffer = RenderScreenSpaceShadows(m_RenderGraph, hdCamera, prepassOutput, prepassOutput.depthBuffer, prepassOutput.normalBuffer, prepassOutput.motionVectorsBuffer, historyValidationTexture, rayCountTexture);

                    var maxZMask = GenerateMaxZPass(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, hdCamera.depthBufferMipChainInfo);

                    var volumetricLighting = VolumetricLightingPass(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, volumetricDensityBuffer, maxZMask, gpuLightListOutput.bigTileLightList, shadowResult);

                    var deferredLightingOutput = RenderDeferredLighting(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, lightingBuffers, prepassOutput.gbuffer, shadowResult, gpuLightListOutput);

                    ApplyCameraMipBias(hdCamera);

                    RenderForwardOpaque(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, gpuLightListOutput, prepassOutput.depthBuffer, vtFeedbackBuffer, shadowResult, prepassOutput.dbuffer, cullingResults);

                    ResetCameraMipBias(hdCamera);

                    if (aovRequest.isValid)
                        aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Normals, hdCamera, prepassOutput.resolvedNormalBuffer, aovBuffers);

                    RenderSubsurfaceScattering(m_RenderGraph, hdCamera, colorBuffer, historyValidationTexture, ref lightingBuffers, ref prepassOutput);

                    RenderSky(m_RenderGraph, hdCamera, colorBuffer, volumetricLighting, prepassOutput.depthBuffer, msaa ? prepassOutput.depthAsColor : prepassOutput.depthPyramidTexture);
                    sunOcclusionTexture = RenderVolumetricClouds(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthPyramidTexture, prepassOutput.motionVectorsBuffer, volumetricLighting, maxZMask);

                    // Send all the geometry graphics buffer to client systems if required (must be done after the pyramid and before the transparent depth pre-pass)
                    SendGeometryGraphicsBuffers(m_RenderGraph, prepassOutput.normalBuffer, prepassOutput.depthPyramidTexture, hdCamera);

                    DoUserAfterOpaqueAndSky(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, prepassOutput.resolvedNormalBuffer, prepassOutput.resolvedMotionVectorsBuffer);

                    // No need for old stencil values here since from transparent on different features are tagged
                    ClearStencilBuffer(m_RenderGraph, hdCamera, prepassOutput.depthBuffer);

                    colorBuffer = RenderTransparency(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedNormalBuffer, vtFeedbackBuffer, currentColorPyramid, volumetricLighting, rayCountTexture, m_SkyManager.GetSkyReflection(hdCamera), gpuLightListOutput, ref prepassOutput, shadowResult, cullingResults, customPassCullingResults, aovRequest, aovCustomPassBuffers);

                    if (NeedMotionVectorForTransparent(hdCamera.frameSettings))
                    {
                        prepassOutput.resolvedMotionVectorsBuffer = ResolveMotionVector(m_RenderGraph, hdCamera, prepassOutput.motionVectorsBuffer);
                    }

                    // We push the motion vector debug texture here as transparent object can overwrite the motion vector texture content.
                    if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                        PushFullScreenDebugTexture(m_RenderGraph, prepassOutput.resolvedMotionVectorsBuffer, FullScreenDebugMode.MotionVectors, fullScreenDebugFormat);

                    // Transparent objects may write to the depth and motion vectors buffers.
                    if (aovRequest.isValid)
                    {
                        aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.DepthStencil, hdCamera, prepassOutput.resolvedDepthBuffer, aovBuffers);
                        if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                            aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.MotionVectors, hdCamera, prepassOutput.resolvedMotionVectorsBuffer, aovBuffers);
                    }

                    var distortionRendererList = m_RenderGraph.CreateRendererList(CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, HDShaderPassNames.s_DistortionVectorsName));

                    // This final Gaussian pyramid can be reused by SSR, so disable it only if there is no distortion
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughDistortion))
                    {
                        TextureHandle distortionColorPyramid = m_RenderGraph.CreateTexture(
                            new TextureDesc(Vector2.one, true, true)
                            {
                                colorFormat = GetColorBufferFormat(),
                                enableRandomWrite = true,
                                useMipMap = true,
                                autoGenerateMips = false,
                                name = "DistortionColorBufferMipChain"
                            });
                        GenerateColorPyramid(m_RenderGraph, hdCamera, colorBuffer, distortionColorPyramid, FullScreenDebugMode.PreRefractionColorPyramid, distortionRendererList);
                        currentColorPyramid = distortionColorPyramid;
                    }

                    using (new RenderGraphProfilingScope(m_RenderGraph, ProfilingSampler.Get(HDProfileId.Distortion)))
                    {
                        var distortionBuffer = AccumulateDistortion(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, distortionRendererList);
                        RenderDistortion(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, currentColorPyramid, distortionBuffer, distortionRendererList);
                    }

                    PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.NanTracker, fullScreenDebugFormat);
                    PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.WorldSpacePosition, fullScreenDebugFormat);
                    PushFullScreenLightingDebugTexture(m_RenderGraph, colorBuffer, fullScreenDebugFormat);

                    bool accumulateInPost = m_PostProcessEnabled && m_DepthOfField.IsActive();
                    if (!accumulateInPost && m_SubFrameManager.isRecording && m_SubFrameManager.subFrameCount > 1)
                    {
                        RenderAccumulation(m_RenderGraph, hdCamera, colorBuffer, colorBuffer, false);
                    }

                    // Render gizmos that should be affected by post processes
                    RenderGizmos(m_RenderGraph, hdCamera, GizmoSubset.PreImageEffects);
                }

#if ENABLE_VIRTUALTEXTURES
                // Note: This pass rely on availability of vtFeedbackBuffer buffer (i.e it need to be write before we read it here)
                // We don't write it when FullScreenDebug mode or path tracer.
                if (resolveVirtualTextureFeedback)
                {
                    hdCamera.ResolveVirtualTextureFeedback(m_RenderGraph, vtFeedbackBuffer);
                    PushFullScreenVTFeedbackDebugTexture(m_RenderGraph, vtFeedbackBuffer, msaa);
                }
#endif

                // At this point, the color buffer has been filled by either debug views are regular rendering so we can push it here.
                var colorPickerTexture = PushColorPickerDebugTexture(m_RenderGraph, colorBuffer);

                RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforePostProcess, aovRequest, aovCustomPassBuffers);

                if (aovRequest.isValid)
                {
                    aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Color, hdCamera, colorBuffer, aovBuffers);
                }

                TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, prepassOutput, colorBuffer, backBuffer, sunOcclusionTexture, cullingResults, hdCamera);

                GenerateDebugImageHistogram(m_RenderGraph, hdCamera, postProcessDest);
                PushFullScreenExposureDebugTexture(m_RenderGraph, postProcessDest, fullScreenDebugFormat);

                ResetCameraSizeForAfterPostProcess(m_RenderGraph, hdCamera, commandBuffer);

                RenderCustomPass(m_RenderGraph, hdCamera, postProcessDest, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.AfterPostProcess, aovRequest, aovCustomPassBuffers);

                CopyXRDepth(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, backBuffer);

                // In developer build, we always render post process in an intermediate buffer at (0,0) in which we will then render debug.
                // Because of this, we need another blit here to the final render target at the right viewport.
                if (!HDUtils.PostProcessIsFinalPass(hdCamera) || aovRequest.isValid)
                {
                    hdCamera.ExecuteCaptureActions(m_RenderGraph, postProcessDest);

                    postProcessDest = RenderDebug(m_RenderGraph,
                        hdCamera,
                        postProcessDest,
                        prepassOutput.resolvedDepthBuffer,
                        prepassOutput.depthPyramidTexture,
                        colorPickerTexture,
                        rayCountTexture,
                        gpuLightListOutput,
                        shadowResult,
                        cullingResults,
                        fullScreenDebugFormat);

                    StopXRSinglePass(m_RenderGraph, hdCamera);

                    for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                    {
                        BlitFinalCameraTexture(m_RenderGraph, hdCamera, postProcessDest, backBuffer, viewIndex);
                    }

                    if (aovRequest.isValid)
                        aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Output, hdCamera, postProcessDest, aovBuffers);
                }

                // This code is only for planar reflections. Given that the depth texture cannot be shared currently with the other depth copy that we do
                // we need to do this separately.
                for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                {
                    if (target.targetDepth != null)
                    {
                        BlitFinalCameraTexture(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, m_RenderGraph.ImportTexture(target.targetDepth), viewIndex);
                    }
                }

                // XR mirror view and blit do device
                EndCameraXR(m_RenderGraph, hdCamera);

                SendColorGraphicsBuffer(m_RenderGraph, hdCamera);

                SetFinalTarget(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, backBuffer);

                RenderWireOverlay(m_RenderGraph, hdCamera, backBuffer);

                RenderGizmos(m_RenderGraph, hdCamera, GizmoSubset.PostImageEffects);
            }
        }

        void ExecuteWithRenderGraph(RenderRequest renderRequest,
            AOVRequestData aovRequest,
            List<RTHandle> aovBuffers,
            List<RTHandle> aovCustomPassBuffers,
            ScriptableRenderContext renderContext,
            CommandBuffer commandBuffer)
        {
            using (m_RenderGraph.RecordAndExecute(new RenderGraphParameters
            {
                executionName = renderRequest.hdCamera.name,
                currentFrameIndex = m_FrameCount,
                rendererListCulling = m_GlobalSettings.rendererListCulling,
                scriptableRenderContext = renderContext,
                commandBuffer = commandBuffer
            }))
            {
                RecordRenderGraph(
                    renderRequest, aovRequest, aovBuffers,
                    aovCustomPassBuffers, renderContext, commandBuffer);
            }


            if (aovRequest.isValid)
            {
                // aovRequest.Execute don't go through render graph for now
                using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(HDProfileId.AOVExecute)))
                {
                    aovRequest.Execute(commandBuffer, aovBuffers, aovCustomPassBuffers, RenderOutputProperties.From(renderRequest.hdCamera));
                }
            }
        }

        class FinalBlitPassData
        {
            public bool flip;
            public int srcTexArraySlice;
            public int dstTexArraySlice;
            public Rect viewport;
            public Material blitMaterial;

            public TextureHandle source;
            public TextureHandle destination;
        }

        void BlitFinalCameraTexture(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle destination, int viewIndex)
        {
            using (var builder = renderGraph.AddRenderPass<FinalBlitPassData>("Final Blit (Dev Build Only)", out var passData))
            {
                if (hdCamera.xr.enabled)
                {
                    passData.viewport = hdCamera.xr.GetViewport(viewIndex);
                    passData.srcTexArraySlice = viewIndex;
                    passData.dstTexArraySlice = hdCamera.xr.GetTextureArraySlice(viewIndex);
                }
                else
                {
                    passData.viewport = hdCamera.finalViewport;
                    passData.srcTexArraySlice = -1;
                    passData.dstTexArraySlice = -1;
                }
                passData.flip = hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView;
                passData.blitMaterial = HDUtils.GetBlitMaterial(TextureXR.useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D, singleSlice: passData.srcTexArraySlice >= 0);
                passData.source = builder.ReadTexture(source);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                    (FinalBlitPassData data, RenderGraphContext context) =>
                    {
                        RTHandle sourceTexture = data.source;
                        // Here we can't use the viewport scale provided in hdCamera. The reason is that this scale is for internal rendering before post process with dynamic resolution factored in.
                        // Here the input texture is already at the viewport size but may be smaller than the RT itself (because of the RTHandle system) so we compute the scale specifically here.
                        var scaleBias = new Vector4((float)data.viewport.width / sourceTexture.rt.width, (float)data.viewport.height / sourceTexture.rt.height, 0.0f, 0.0f);

                        if (data.flip)
                        {
                            scaleBias.w = scaleBias.y;
                            scaleBias.y *= -1;
                        }

                        var propertyBlock = context.renderGraphPool.GetTempMaterialPropertyBlock();
                        propertyBlock.SetTexture(HDShaderIDs._BlitTexture, sourceTexture);
                        propertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
                        propertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                        propertyBlock.SetInt(HDShaderIDs._BlitTexArraySlice, data.srcTexArraySlice);
                        HDUtils.DrawFullScreen(context.cmd, data.viewport, data.blitMaterial, data.destination, propertyBlock, 0, data.dstTexArraySlice);
                    });
            }
        }

        class UpdateParentExposureData
        {
            public HDCamera.ExposureTextures textures;
        }

        void UpdateParentExposure(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var exposures = hdCamera.currentExposureTextures;
            if (exposures.useCurrentCamera)
                return;

            using (var builder = renderGraph.AddRenderPass<UpdateParentExposureData>("UpdateParentExposures", out var passData))
            {
                passData.textures = exposures;

                builder.SetRenderFunc(
                    (UpdateParentExposureData data, RenderGraphContext context) =>
                    {
                        if (data.textures.useFetchedExposure)
                        {
                            Color clearCol = new Color(data.textures.fetchedGpuExposure, ColorUtils.ConvertExposureToEV100(data.textures.fetchedGpuExposure), 0.0f, 0.0f);
                            context.cmd.SetRenderTarget(data.textures.current);
                            context.cmd.ClearRenderTarget(false, true, clearCol);
                        }
                        else
                        {
                            context.cmd.CopyTexture(data.textures.parent, data.textures.current);
                        }
                    });
            }
        }

        void ApplyCameraMipBias(HDCamera hdCamera)
        {
            float globalMaterialMipBias = 0.0f;
            if (m_CurrentDebugDisplaySettings != null && m_CurrentDebugDisplaySettings.data.UseDebugGlobalMipBiasOverride())
            {
                globalMaterialMipBias = m_CurrentDebugDisplaySettings.data.GetDebugGlobalMipBiasOverride();
            }
            else
            {
                globalMaterialMipBias = hdCamera.globalMipBias;
            }
            PushCameraGlobalMipBiasPass(m_RenderGraph, hdCamera, globalMaterialMipBias);
        }

        void ResetCameraMipBias(HDCamera hdCamera) => PushCameraGlobalMipBiasPass(m_RenderGraph, hdCamera, 0.0f);

        class PushCameraGlobalMipBiasData
        {
            public HDCamera hdCamera;
            public float mipBias;
            public ShaderVariablesGlobal globalCB;
            public ShaderVariablesXR xrCB;
        }

        void PushCameraGlobalMipBiasPass(RenderGraph renderGraph, HDCamera hdCamera, float mipBias)
        {
            if (!ShaderConfig.s_GlobalMipBias)
                return;

            using (var builder = renderGraph.AddRenderPass<PushCameraGlobalMipBiasData>("Push Global Camera Mip Bias", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.mipBias = mipBias;
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.xrCB = m_ShaderVariablesXRCB;

                builder.SetRenderFunc(
                    (PushCameraGlobalMipBiasData data, RenderGraphContext context) =>
                    {
                        data.hdCamera.globalMipBias = data.mipBias;
                        data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                        data.hdCamera.UpdateShaderVariablesXRCB(ref data.xrCB);
                        ConstantBuffer.PushGlobal(context.cmd, data.xrCB, HDShaderIDs._ShaderVariablesXR);
                    });
            }
        }

        class SetFinalTargetPassData
        {
            public bool copyDepth;
            public Material copyDepthMaterial;
            public TextureHandle finalTarget;
            public Rect finalViewport;
            public TextureHandle depthBuffer;
            public bool flipY;
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
                passData.copyDepth = passData.copyDepth && !hdCamera.xr.enabled;
                passData.finalTarget = builder.WriteTexture(finalTarget);
                passData.finalViewport = hdCamera.finalViewport;

                if (passData.copyDepth)
                {
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.flipY = hdCamera.isMainGameView;
                    passData.copyDepthMaterial = m_CopyDepth;
                }

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
                                RTHandle depth = data.depthBuffer;
                                // Depth buffer can be invalid if no opaque has been rendered before.
                                if (depth != null)
                                {
                                    mpb.SetTexture(HDShaderIDs._InputDepth, depth);
                                    // When we are Main Game View we need to flip the depth buffer ourselves as we are after postprocess / blit that have already flipped the screen
                                    mpb.SetInt("_FlipY", data.flipY ? 1 : 0);
                                    mpb.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                                    CoreUtils.DrawFullScreen(ctx.cmd, data.copyDepthMaterial, mpb);
                                }
                            }
                        }
                    });
            }
        }

        class CopyXRDepthPassData
        {
            public Material copyDepth;
            public Rect viewport;
            public TextureHandle depthBuffer;
            public TextureHandle output;
            public float dynamicResolutionScale;
        }

        void CopyXRDepth(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle output)
        {
            // Copy and rescale depth buffer for XR devices
            if (hdCamera.xr.enabled && hdCamera.xr.copyDepth)
            {
                using (var builder = renderGraph.AddRenderPass<CopyXRDepthPassData>("Copy XR Depth", out var passData, ProfilingSampler.Get(HDProfileId.XRDepthCopy)))
                {
                    passData.copyDepth = m_CopyDepth;
                    passData.viewport = hdCamera.finalViewport;
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.output = builder.WriteTexture(output);
                    passData.dynamicResolutionScale = DynamicResolutionHandler.instance.GetCurrentScale();

                    builder.SetRenderFunc(
                        (CopyXRDepthPassData data, RenderGraphContext ctx) =>
                        {
                            var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                            mpb.SetTexture(HDShaderIDs._InputDepth, data.depthBuffer);
                            mpb.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(data.dynamicResolutionScale, data.dynamicResolutionScale, 0.0f, 0.0f));
                            mpb.SetInt("_FlipY", 1);

                            ctx.cmd.SetRenderTarget(data.output, 0, CubemapFace.Unknown, -1);
                            ctx.cmd.SetViewport(data.viewport);
                            CoreUtils.DrawFullScreen(ctx.cmd, data.copyDepth, mpb);
                        });
                }
            }
        }

        class ForwardPassData
        {
            public RendererListHandle rendererList;
            public ComputeBufferHandle lightListTile;
            public ComputeBufferHandle lightListCluster;

            public ComputeBufferHandle perVoxelOffset;
            public ComputeBufferHandle perTileLogBaseTweak;
            public FrameSettings frameSettings;
        }

        class ForwardOpaquePassData : ForwardPassData
        {
            public DBufferOutput dbuffer;
            public LightingBuffers lightingBuffers;
            public bool enableDecals;
        }

        class ForwardTransparentPassData : ForwardPassData
        {
            public bool decalsEnabled;
            public bool renderMotionVecForTransparent;
            public int colorMaskTransparentVel;
            public TextureHandle transparentSSRLighting;
            public TextureHandle volumetricLighting;
            public TextureHandle depthPyramidTexture;
            public TextureHandle normalBuffer;
        }

        void PrepareCommonForwardPassData(
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            ForwardPassData data,
            bool opaque,
            FrameSettings frameSettings,
            RendererListDesc rendererListDesc,
            in BuildGPULightListOutput lightLists,
            ShadowResult shadowResult)
        {
            bool useFptl = frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque) && opaque;

            data.frameSettings = frameSettings;
            data.lightListTile = builder.ReadComputeBuffer(lightLists.lightList);
            data.lightListCluster = builder.ReadComputeBuffer(lightLists.perVoxelLightLists);

            if (!useFptl)
            {
                data.perVoxelOffset = builder.ReadComputeBuffer(lightLists.perVoxelOffset);
                if (lightLists.perTileLogBaseTweak.IsValid())
                    data.perTileLogBaseTweak = builder.ReadComputeBuffer(lightLists.perTileLogBaseTweak);
            }
            data.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(rendererListDesc));

            HDShadowManager.ReadShadowResult(shadowResult, builder);
        }

        static void BindGlobalLightListBuffers(ForwardPassData data, RenderGraphContext ctx)
        {
            ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListTile, data.lightListTile);
            ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListCluster, data.lightListCluster);

            // Next two are only for cluster rendering. PerTileLogBaseTweak is only when using depth buffer so can be invalid as well.
            if (data.perVoxelOffset.IsValid())
                ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
            if (data.perTileLogBaseTweak.IsValid())
                ctx.cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, data.perTileLogBaseTweak);
        }

        RendererListDesc PrepareForwardOpaqueRendererList(CullingResults cullResults, HDCamera hdCamera)
        {
            var passNames = hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward
                ? m_ForwardAndForwardOnlyPassNames
                : m_ForwardOnlyPassNames;
            return CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting);
        }

        RendererListDesc PrepareForwardTransparentRendererList(CullingResults cullResults, HDCamera hdCamera, bool preRefraction)
        {
            RenderQueueRange transparentRange;
            if (preRefraction)
            {
                transparentRange = HDRenderQueue.k_RenderQueue_PreRefraction;
            }
            else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                transparentRange = HDRenderQueue.k_RenderQueue_Transparent;
            }
            else // Low res transparent disabled
            {
                transparentRange = HDRenderQueue.k_RenderQueue_TransparentWithLowRes;
            }

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
            {
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
                    transparentRange = HDRenderQueue.k_RenderQueue_AllTransparent;
                else
                    transparentRange = HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
            }

            if (NeedMotionVectorForTransparent(hdCamera.frameSettings))
            {
                m_CurrentRendererConfigurationBakedLighting |= PerObjectData.MotionVectors; // This will enable the flag for low res transparent as well
            }

            var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
            return CreateTransparentRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, transparentRange);
        }

        static void RenderForwardRendererList(FrameSettings frameSettings,
            RendererList rendererList,
            bool opaque,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd)
        {
            // Note: SHADOWS_SHADOWMASK keyword is enabled in HDRenderPipeline.cs ConfigureForShadowMask
            bool useFptl = opaque && frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque);

            // say that we want to use tile/cluster light loop
            CoreUtils.SetKeyword(cmd, "USE_FPTL_LIGHTLIST", useFptl);
            CoreUtils.SetKeyword(cmd, "USE_CLUSTERED_LIGHTLIST", !useFptl);

            if (opaque)
                DrawOpaqueRendererList(renderContext, cmd, frameSettings, rendererList);
            else
                DrawTransparentRendererList(renderContext, cmd, frameSettings, rendererList);
        }

        // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
        // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
        // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
        // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
        // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
        // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"
        void RenderForwardOpaque(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            in LightingBuffers lightingBuffers,
            in BuildGPULightListOutput lightLists,
            TextureHandle depthBuffer,
            TextureHandle vtFeedbackBuffer,
            ShadowResult shadowResult,
            DBufferOutput dbuffer,
            CullingResults cullResults)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>(debugDisplay ? "Forward (+ Emissive) Opaque  Debug" : "Forward (+ Emissive) Opaque",
                out var passData,
                debugDisplay ? ProfilingSampler.Get(HDProfileId.ForwardOpaqueDebug) : ProfilingSampler.Get(HDProfileId.ForwardOpaque)))
            {
                PrepareCommonForwardPassData(renderGraph, builder, passData, true, hdCamera.frameSettings, PrepareForwardOpaqueRendererList(cullResults, hdCamera), lightLists, shadowResult);

                int index = 0;
                builder.UseColorBuffer(colorBuffer, index++);
#if ENABLE_VIRTUALTEXTURES
                builder.UseColorBuffer(vtFeedbackBuffer, index++);
#endif
                // In case of forward SSS we will bind all the required target. It is up to the shader to write into it or not.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                {
                    builder.UseColorBuffer(lightingBuffers.diffuseLightingBuffer, index++);
                    builder.UseColorBuffer(lightingBuffers.sssBuffer, index++);
                }
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
                passData.dbuffer = ReadDBuffer(dbuffer, builder);
                passData.lightingBuffers = ReadLightingBuffers(lightingBuffers, builder);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext context) =>
                    {
                        BindGlobalLightListBuffers(data, context);
                        BindDBufferGlobalData(data.dbuffer, context);
                        BindGlobalLightingBuffers(data.lightingBuffers, context.cmd);

                        RenderForwardRendererList(data.frameSettings, data.rendererList, true, context.renderContext, context.cmd);

                        // TODO : what will happen with render list? maybe we will not be able to skip this pass because of decal emissive projector, in this case
                        // we may need to move this part out?
                        if (data.enableDecals)
                            DecalSystem.instance.RenderForwardEmissive(context.cmd);
                    });
            }
        }

        // This is use to Display legacy shader with an error shader
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
        void RenderForwardError(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle depthStencilBuffer,
            CullingResults cullResults)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects) &&
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                return;
            }

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>("Forward Error", out var passData, ProfilingSampler.Get(HDProfileId.RenderForwardError)))
            {
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);

                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, m_ForwardErrorPassNames, renderQueueRange: RenderQueueRange.all, overrideMaterial: m_ErrorMaterial)));

                builder.SetRenderFunc(
                    (ForwardPassData data, RenderGraphContext context) =>
                    {
                        CoreUtils.DrawRendererList(context.renderContext, context.cmd, data.rendererList);
                    });
            }
        }

        void RenderForwardTransparent(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle normalBuffer,
            in PrepassOutput prepassOutput,
            TextureHandle vtFeedbackBuffer,
            TextureHandle volumetricLighting,
            TextureHandle ssrLighting,
            TextureHandle? colorPyramid,
            in BuildGPULightListOutput lightLists,
            in ShadowResult shadowResult,
            CullingResults cullResults,
            bool preRefractionPass)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects))
                return;

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

            using (var builder = renderGraph.AddRenderPass<ForwardTransparentPassData>(passName, out var passData, ProfilingSampler.Get(profilingId)))
            {
                PrepareCommonForwardPassData(renderGraph, builder, passData, false, hdCamera.frameSettings, PrepareForwardTransparentRendererList(cullResults, hdCamera, preRefractionPass), lightLists, shadowResult);

                // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
                // decal datas count is 0 if no decals affect transparency
                passData.decalsEnabled = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
                passData.renderMotionVecForTransparent = NeedMotionVectorForTransparent(hdCamera.frameSettings);
                passData.colorMaskTransparentVel = colorMaskTransparentVel;
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.transparentSSRLighting = builder.ReadTexture(ssrLighting);
                passData.depthPyramidTexture = builder.ReadTexture(prepassOutput.depthPyramidTexture); // We need to bind this for transparent materials doing stuff like soft particles etc.

                int index = 0;
                builder.UseColorBuffer(colorBuffer, index++);
#if ENABLE_VIRTUALTEXTURES
                builder.UseColorBuffer(vtFeedbackBuffer, index++);
#endif

                if (passData.renderMotionVecForTransparent)
                {
                    builder.UseColorBuffer(prepassOutput.motionVectorsBuffer, index++);
                }
                else
                {
                    bool msaa = hdCamera.msaaEnabled;

                    // It doesn't really matter what gets bound here since the color mask state set will prevent this from ever being written to. However, we still need to bind something
                    // to avoid warnings about unbound render targets. The following rendertarget could really be anything if renderVelocitiesForTransparent
                    // Create a new target here should reuse existing already released one
                    builder.UseColorBuffer(builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, bindTextureMS = msaa, msaaSamples = hdCamera.msaaSamples, name = "Transparency Velocity Dummy" }), index++);
                }
                builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);

                if (colorPyramid != null && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) && !preRefractionPass)
                {
                    builder.ReadTexture(colorPyramid.Value);
                }

                // TODO RENDERGRAPH
                // Since in the old code path we bound this as global, it was available here so we need to bind it as well in order not to break existing projects...
                // This is not good because it will extend its lifetime even when it's not actually used by a shader (we can't have that info).
                // TODO: Make this explicit?
                passData.normalBuffer = builder.ReadTexture(normalBuffer);

                builder.SetRenderFunc(
                    (ForwardTransparentPassData data, RenderGraphContext context) =>
                    {
                        // Bind all global data/parameters for transparent forward pass
                        context.cmd.SetGlobalInt(data.colorMaskTransparentVel, data.renderMotionVecForTransparent ? (int)ColorWriteMask.All : 0);
                        if (data.decalsEnabled)
                            DecalSystem.instance.SetAtlas(context.cmd); // for clustered decals

                        BindGlobalLightListBuffers(data, context);

                        context.cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, data.transparentSSRLighting);
                        context.cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, data.volumetricLighting);
                        context.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramidTexture);
                        context.cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        RenderForwardRendererList(data.frameSettings, data.rendererList, false, context.renderContext, context.cmd);
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
                {
                    int index = 0;
                    if (hdCamera.msaaEnabled)
                        builder.UseColorBuffer(prepassOutput.depthAsColor, index++);
                    builder.UseColorBuffer(prepassOutput.normalBuffer, index++);
                }
                builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cull, hdCamera.camera, m_TransparentDepthPrepassNames)));

                builder.SetRenderFunc(
                    (ForwardPassData data, RenderGraphContext context) =>
                    {
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
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
                builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.ReadWrite);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cull, hdCamera.camera, m_TransparentDepthPostpassNames)));

                builder.SetRenderFunc(
                    (ForwardPassData data, RenderGraphContext context) =>
                    {
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
                    });
            }
        }

        class RenderLowResTransparentPassData
        {
            public float lowResScale;
            public ShaderVariablesGlobal globalCB;
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
        }

        TextureHandle RenderLowResTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle downsampledDepth, CullingResults cullingResults, RendererListHandle rendererList)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLowResTransparentPassData>("Low Res Transparent", out var passData, ProfilingSampler.Get(HDProfileId.LowResTransparent)))
            {
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.lowResScale = hdCamera.lowResScale;
                passData.frameSettings = hdCamera.frameSettings;
                passData.rendererList = builder.UseRendererList(rendererList);
                builder.UseDepthBuffer(downsampledDepth, DepthAccess.ReadWrite);
                // We need R16G16B16A16_SFloat as we need a proper alpha channel for compositing.
                var output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one * hdCamera.lowResScale, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, clearBuffer = true, clearColor = Color.black, name = "Low res transparent" }), 0);

                builder.SetRenderFunc(
                    (RenderLowResTransparentPassData data, RenderGraphContext context) =>
                    {
                        UpdateOffscreenRenderingConstants(ref data.globalCB, true, 1.0f / data.lowResScale);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);

                        UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1.0f);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    });

                return output;
            }
        }

        class UpsampleTransparentPassData
        {
            public Vector4 shaderParams;
            public Material upsampleMaterial;
            public TextureHandle lowResTransparentBuffer;
            public TextureHandle downsampledDepthBuffer;
        }

        void UpsampleTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle lowResTransparentBuffer, TextureHandle downsampledDepthBuffer, RendererListHandle rendererList)
        {
            using (var builder = renderGraph.AddRenderPass<UpsampleTransparentPassData>("Upsample Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.UpsampleLowResTransparent)))
            {
                // This pass depends on the low-res transparency pass
                builder.DependsOn(rendererList);

                var settings = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings;
                if (settings.upsampleType == LowResTransparentUpsample.Bilinear)
                {
                    m_UpsampleTransparency.EnableKeyword("BILINEAR");
                }
                else if (settings.upsampleType == LowResTransparentUpsample.NearestDepth)
                {
                    m_UpsampleTransparency.EnableKeyword("NEAREST_DEPTH");
                }

                passData.shaderParams = new Vector4(hdCamera.lowResScale, 1.0f / hdCamera.lowResScale, 0.0f, 0.0f);
                passData.upsampleMaterial = m_UpsampleTransparency;
                passData.lowResTransparentBuffer = builder.ReadTexture(lowResTransparentBuffer);
                passData.downsampledDepthBuffer = builder.ReadTexture(downsampledDepthBuffer);
                builder.UseColorBuffer(colorBuffer, 0);

                builder.SetRenderFunc(
                    (UpsampleTransparentPassData data, RenderGraphContext context) =>
                    {
                        data.upsampleMaterial.SetVector(HDShaderIDs._Params, data.shaderParams);
                        data.upsampleMaterial.SetTexture(HDShaderIDs._LowResTransparent, data.lowResTransparentBuffer);
                        data.upsampleMaterial.SetTexture(HDShaderIDs._LowResDepthTexture, data.downsampledDepthBuffer);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.upsampleMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                    });
            }
        }

        class SetGlobalColorPassData
        {
            public TextureHandle colorBuffer;
        }

        void SetGlobalColorForCustomPass(RenderGraph renderGraph, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<SetGlobalColorPassData>("SetGlobalColorForCustomPass", out var passData))
            {
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                builder.SetRenderFunc((SetGlobalColorPassData data, RenderGraphContext context) =>
                {
                    RTHandle colorPyramid = data.colorBuffer;
                    if (colorPyramid != null)
                        context.cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, data.colorBuffer);
                });
            }
        }

        class RayTracingFlagMaskPassData
        {
            public FrameSettings frameSettings;
            public TextureHandle depthBuffer;
            public TextureHandle flagMask;
            public RendererListHandle opaqueRenderList;
            public RendererListHandle transparentRenderList;
            public bool clear;
        }

        TextureHandle RenderRayTracingFlagMask(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, TextureHandle depthBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return renderGraph.defaultResources.blackTextureXR;

            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            if (!recursiveSettings.enable.value)
                return renderGraph.defaultResources.blackTextureXR;

            // This pass will fill the flag mask texture. This will only tag pixels for recursive rendering for now.
            // TODO: evaluate the usage of a stencil bit in the stencil buffer to save a render target (But it require various headaches to work correctly).
            using (var builder = renderGraph.AddRenderPass<RayTracingFlagMaskPassData>("RayTracing Flag Mask", out var passData, ProfilingSampler.Get(HDProfileId.RayTracingFlagMask)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.flagMask = builder.UseColorBuffer(CreateFlagMaskTexture(renderGraph), 0);
                passData.opaqueRenderList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_RayTracingPrepassNames, stateBlock: m_DepthStateNoWrite)));
                passData.transparentRenderList = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cull, hdCamera.camera, m_RayTracingPrepassNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes, stateBlock: m_DepthStateNoWrite)));

                builder.SetRenderFunc(
                    (RayTracingFlagMaskPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.opaqueRenderList);
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.transparentRenderList);
                    });

                return passData.flagMask;
            }
        }

        TextureHandle RenderTransparency(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle normalBuffer,
            TextureHandle vtFeedbackBuffer,
            TextureHandle currentColorPyramid,
            TextureHandle volumetricLighting,
            TextureHandle rayCountTexture,
            Texture skyTexture,
            in BuildGPULightListOutput lightLists,
            ref PrepassOutput prepassOutput,
            ShadowResult shadowResult,
            CullingResults cullingResults,
            CullingResults customPassCullingResults,
            AOVRequestData aovRequest,
            List<RTHandle> aovCustomPassBuffers)
        {
            // Transparent (non recursive) objects that are rendered in front of transparent (recursive) require the recursive rendering to be executed for that pixel.
            // This means our flagging process needs to happen before the transparent depth prepass as we use the depth to discriminate pixels that do not need recursive rendering.
            var flagMaskBuffer = RenderRayTracingFlagMask(renderGraph, cullingResults, hdCamera, prepassOutput.depthBuffer);

            RenderTransparentDepthPrepass(renderGraph, hdCamera, prepassOutput, cullingResults);

            var ssrLightingBuffer = RenderSSR(renderGraph, hdCamera, ref prepassOutput, renderGraph.defaultResources.blackTextureXR, rayCountTexture, skyTexture, transparent: true);

            colorBuffer = RaytracingRecursiveRender(renderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, flagMaskBuffer, rayCountTexture);

            // TODO RENDERGRAPH: Remove this when we properly convert custom passes to full render graph with explicit color buffer reads.
            // To allow users to fetch the current color buffer, we temporarily bind the camera color buffer
            SetGlobalColorForCustomPass(renderGraph, colorBuffer);
            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforePreRefraction, aovRequest, aovCustomPassBuffers);
            SetGlobalColorForCustomPass(renderGraph, currentColorPyramid);

            // Render pre-refraction objects
            ApplyCameraMipBias(hdCamera);

            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, normalBuffer, prepassOutput, vtFeedbackBuffer, volumetricLighting, ssrLightingBuffer, null, lightLists, shadowResult, cullingResults, true);

            ResetCameraMipBias(hdCamera);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) || hdCamera.IsSSREnabled() || hdCamera.IsSSGIEnabled())
            {
                var resolvedColorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);
                GenerateColorPyramid(renderGraph, hdCamera, resolvedColorBuffer, currentColorPyramid, FullScreenDebugMode.FinalColorPyramid);
            }

            // We don't have access to the color pyramid with transparent if rough refraction is disabled
            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforeTransparent, aovRequest, aovCustomPassBuffers);

            // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
            ApplyCameraMipBias(hdCamera);
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, normalBuffer, prepassOutput, vtFeedbackBuffer, volumetricLighting, ssrLightingBuffer, currentColorPyramid, lightLists, shadowResult, cullingResults, false);
            ResetCameraMipBias(hdCamera);

            colorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);

            // Render All forward error
            RenderForwardError(renderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, cullingResults);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
                var lowResTranspRendererList = renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_LowTransparent));
                ApplyCameraMipBias(hdCamera);
                var lowResTransparentBuffer = RenderLowResTransparent(renderGraph, hdCamera, prepassOutput.downsampledDepthBuffer, cullingResults, lowResTranspRendererList);
                ResetCameraMipBias(hdCamera);
                UpsampleTransparent(renderGraph, hdCamera, colorBuffer, lowResTransparentBuffer, prepassOutput.downsampledDepthBuffer, lowResTranspRendererList);
            }

            // Fill depth buffer to reduce artifact for transparent object during postprocess
            RenderTransparentDepthPostpass(renderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, cullingResults);

            return colorBuffer;
        }

        struct SendGeometryGraphcisBuffersParameters
        {
            public HDCamera hdCamera;
            public bool needNormalBuffer;
            public bool needDepthBuffer;
            public VFXCameraBufferTypes neededVFXBuffers;
            public HDUtils.PackedMipChainInfo packedMipChainInfo;

            public bool NeedSendBuffers()
            {
                return needNormalBuffer || needDepthBuffer || neededVFXBuffers != VFXCameraBufferTypes.None;
            }
        }

        SendGeometryGraphcisBuffersParameters PrepareSendGeometryBuffersParameters(HDCamera hdCamera)
        {
            SendGeometryGraphcisBuffersParameters parameters = new SendGeometryGraphcisBuffersParameters();

            parameters.hdCamera = hdCamera;
            parameters.needNormalBuffer = false;
            parameters.needDepthBuffer = false;
            parameters.packedMipChainInfo = hdCamera.depthBufferMipChainInfo;

            HDAdditionalCameraData acd = null;
            hdCamera.camera.TryGetComponent(out acd);

            HDAdditionalCameraData.BufferAccessType externalAccess = new HDAdditionalCameraData.BufferAccessType();
            if (acd != null)
                externalAccess = acd.GetBufferAccess();

            // Figure out which client systems need which buffers
            // Only VFX systems for now
            parameters.neededVFXBuffers = VFXManager.IsCameraBufferNeeded(hdCamera.camera);
            parameters.needNormalBuffer |= ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Normal) != 0 || (externalAccess & HDAdditionalCameraData.BufferAccessType.Normal) != 0 || GetIndirectDiffuseMode(hdCamera) == IndirectDiffuseMode.ScreenSpace);
            parameters.needDepthBuffer |= ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Depth) != 0 || (externalAccess & HDAdditionalCameraData.BufferAccessType.Depth) != 0 || GetIndirectDiffuseMode(hdCamera) == IndirectDiffuseMode.ScreenSpace);

            // Raytracing require both normal and depth from previous frame.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && GetRayTracingState())
            {
                parameters.needNormalBuffer = true;
                parameters.needDepthBuffer = true;
            }

            return parameters;
        }

        class SendGeometryBuffersPassData
        {
            public SendGeometryGraphcisBuffersParameters parameters;
            public TextureHandle normalBuffer;
            public TextureHandle depthBuffer;
        }

        void SendGeometryGraphicsBuffers(RenderGraph renderGraph, TextureHandle inputNormalBuffer, TextureHandle inputDepthBuffer, HDCamera camera)
        {
            var parameters = PrepareSendGeometryBuffersParameters(camera);

            if (!parameters.NeedSendBuffers())
                return;

            using (var builder = renderGraph.AddRenderPass<SendGeometryBuffersPassData>("Send Geometry Buffers", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.parameters = parameters;

                if (!camera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                {
                    passData.normalBuffer = renderGraph.defaultResources.blackTextureXR;
                    passData.depthBuffer = renderGraph.defaultResources.blackTextureXR;
                }
                else
                {
                    passData.normalBuffer = builder.ReadTexture(inputNormalBuffer);
                    passData.depthBuffer = builder.ReadTexture(inputDepthBuffer);
                }

                builder.SetRenderFunc(
                    (SendGeometryBuffersPassData data, RenderGraphContext ctx) =>
                    {
                        var hdCamera = data.parameters.hdCamera;

                        RTHandle mainNormalBuffer = data.normalBuffer;
                        RTHandle mainDepthBuffer = data.depthBuffer;

                        Texture normalBuffer = null;
                        Texture depthBuffer = null;
                        Texture depthBuffer1 = null;

                        // Here if needed for this particular camera, we allocate history buffers.
                        // Only one is needed here because the main buffer used for rendering is separate.
                        // Ideally, we should double buffer the main rendering buffer but since we don't know in advance if history is going to be needed, it would be a big waste of memory.
                        if (data.parameters.needNormalBuffer && mainNormalBuffer.rt != null)
                        {
                            // local variable to avoid gcalloc caused by capture.
                            var localNormalBuffer = mainNormalBuffer;
                            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                            {
                                return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: localNormalBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localNormalBuffer.rt.enableRandomWrite, name: $"{id}_Normal History Buffer");
                            }

                            normalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Normal, Allocator, 1);

                            for (int i = 0; i < hdCamera.viewCount; i++)
                                ctx.cmd.CopyTexture(localNormalBuffer, i, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, normalBuffer, i, 0, 0, 0);
                        }

                        if (data.parameters.needDepthBuffer && mainDepthBuffer.rt != null)
                        {
                            // local variable to avoid gcalloc caused by capture.
                            var localDepthBuffer = mainDepthBuffer;
                            RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                            {
                                return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: localDepthBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localDepthBuffer.rt.enableRandomWrite, name: $"{id}_Depth History Buffer");
                            }

                            depthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Depth, Allocator, 1);

                            for (int i = 0; i < hdCamera.viewCount; i++)
                                ctx.cmd.CopyTexture(localDepthBuffer, i, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, depthBuffer, i, 0, 0, 0);

                            RTHandle Allocator1(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                            {
                                return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: localDepthBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localDepthBuffer.rt.enableRandomWrite, name: $"Depth History Buffer Mip 1");
                            }

                            depthBuffer1 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Depth1, Allocator1, 1);
                            for (int i = 0; i < hdCamera.viewCount; i++)
                                ctx.cmd.CopyTexture(localDepthBuffer, i, 0, data.parameters.packedMipChainInfo.mipLevelOffsets[1].x, data.parameters.packedMipChainInfo.mipLevelOffsets[1].y, hdCamera.actualWidth / 2, hdCamera.actualHeight / 2, depthBuffer1, i, 0, 0, 0);
                        }

                        // Send buffers to client.
                        // For now, only VFX systems
                        if ((data.parameters.neededVFXBuffers & VFXCameraBufferTypes.Depth) != 0)
                        {
                            VFXManager.SetCameraBuffer(hdCamera.camera, VFXCameraBufferTypes.Depth, depthBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
                        }

                        if ((data.parameters.neededVFXBuffers & VFXCameraBufferTypes.Normal) != 0)
                        {
                            VFXManager.SetCameraBuffer(hdCamera.camera, VFXCameraBufferTypes.Normal, normalBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
                        }
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
                builder.AllowPassCulling(false);

                passData.hdCamera = hdCamera;

                builder.SetRenderFunc(
                    (SendColorGraphicsBufferPassData data, RenderGraphContext ctx) =>
                    {
                        // Figure out which client systems need which buffers
                        VFXCameraBufferTypes neededVFXBuffers = VFXManager.IsCameraBufferNeeded(data.hdCamera.camera);

                        if ((neededVFXBuffers & VFXCameraBufferTypes.Color) != 0)
                        {
                            var colorBuffer = data.hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                            VFXManager.SetCameraBuffer(data.hdCamera.camera, VFXCameraBufferTypes.Color, colorBuffer, 0, 0, data.hdCamera.actualWidth, data.hdCamera.actualHeight);
                        }
                    });
            }
        }

        class ClearStencilPassData
        {
            public Material clearStencilMaterial;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void ClearStencilBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects)) // If we don't have opaque objects there is no need to clear.
                return;

            using (var builder = renderGraph.AddRenderPass<ClearStencilPassData>("Clear Stencil Buffer", out var passData, ProfilingSampler.Get(HDProfileId.ClearStencil)))
            {
                passData.clearStencilMaterial = m_ClearStencilBufferMaterial;
                //passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthBuffer = builder.WriteTexture(depthBuffer);

                builder.SetRenderFunc(
                    (ClearStencilPassData data, RenderGraphContext ctx) =>
                    {
                        data.clearStencilMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.HDRPReservedBits);
                        HDUtils.DrawFullScreen(ctx.cmd, data.clearStencilMaterial, data.depthBuffer);
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
        }

        void PreRenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthStencilBuffer, TextureHandle normalbuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera) ||
                !m_SkyManager.RequiresPreRenderSky(hdCamera))
                return;

            using (var builder = renderGraph.AddRenderPass<PreRenderSkyPassData>("Pre Render Sky", out var passData))
            {
                passData.sunLight = GetMainLight();
                passData.hdCamera = hdCamera;
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthStencilBuffer = builder.WriteTexture(depthStencilBuffer);
                passData.normalBuffer = builder.WriteTexture(normalbuffer);
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.skyManager = m_SkyManager;

                builder.SetRenderFunc(
                    (PreRenderSkyPassData data, RenderGraphContext context) =>
                    {
                        data.skyManager.PreRenderSky(data.hdCamera, data.sunLight, data.colorBuffer, data.normalBuffer, data.depthStencilBuffer, data.debugDisplaySettings, context.cmd);
                    });
            }
        }

        class RenderSkyPassData
        {
            public VisualEnvironment visualEnvironment;
            public Light sunLight;
            public HDCamera hdCamera;
            public TextureHandle volumetricLighting;
            public TextureHandle colorBuffer;
            public TextureHandle depthTexture;
            public TextureHandle depthStencilBuffer;
            public TextureHandle intermediateBuffer;
            public DebugDisplaySettings debugDisplaySettings;
            public SkyManager skyManager;
        }

        void RenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle volumetricLighting, TextureHandle depthStencilBuffer, TextureHandle depthTexture)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera) || CoreUtils.IsSceneFilteringEnabled())
                return;

            using (var builder = renderGraph.AddRenderPass<RenderSkyPassData>("Render Sky And Fog", out var passData))
            {
                passData.visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
                passData.sunLight = GetMainLight();
                passData.hdCamera = hdCamera;
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.depthStencilBuffer = builder.WriteTexture(depthStencilBuffer);
                if (Fog.IsPBRFogEnabled(hdCamera))
                    passData.intermediateBuffer = builder.CreateTransientTexture(colorBuffer);
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.skyManager = m_SkyManager;

                builder.SetRenderFunc(
                    (RenderSkyPassData data, RenderGraphContext context) =>
                    {
                        // Necessary to perform dual-source (polychromatic alpha) blending which is not supported by Unity.
                        // We load from the color buffer, perform blending manually, and store to the atmospheric scattering buffer.
                        // Then we perform a copy from the atmospheric scattering buffer back to the color buffer.
                        data.skyManager.RenderSky(data.hdCamera, data.sunLight, data.colorBuffer, data.depthStencilBuffer, data.debugDisplaySettings, context.cmd);

                        if (Fog.IsFogEnabled(data.hdCamera) || Fog.IsPBRFogEnabled(data.hdCamera))
                        {
                            var pixelCoordToViewDirWS = data.hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                            data.skyManager.RenderOpaqueAtmosphericScattering(context.cmd, data.hdCamera, data.colorBuffer, data.depthTexture, data.volumetricLighting, data.intermediateBuffer, data.depthStencilBuffer, pixelCoordToViewDirWS, data.hdCamera.msaaEnabled);
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

        void GenerateColorPyramid(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputColor, TextureHandle output, FullScreenDebugMode fsDebugMode, RendererListHandle? depedency = null)
        {
            using (var builder = renderGraph.AddRenderPass<GenerateColorPyramidData>("Color Gaussian MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.ColorPyramid)))
            {
                if (depedency != null)
                {
                    builder.DependsOn(depedency.Value);
                }

                if (!hdCamera.colorPyramidHistoryIsValid)
                {
                    hdCamera.colorPyramidHistoryIsValid = true; // For the next frame...
                    hdCamera.colorPyramidHistoryValidFrames = 0;
                }
                else
                {
                    hdCamera.colorPyramidHistoryValidFrames++;
                }


                passData.colorPyramid = builder.WriteTexture(output);
                passData.inputColor = builder.ReadTexture(inputColor);
                passData.hdCamera = hdCamera;
                passData.mipGenerator = m_MipGenerator;

                builder.SetRenderFunc(
                    (GenerateColorPyramidData data, RenderGraphContext context) =>
                    {
                        Vector2Int pyramidSize = new Vector2Int(data.hdCamera.actualWidth, data.hdCamera.actualHeight);
                        data.hdCamera.colorPyramidHistoryMipCount = data.mipGenerator.RenderColorGaussianPyramid(context.cmd, pyramidSize, data.inputColor, data.colorPyramid);
                        // TODO RENDERGRAPH: We'd like to avoid SetGlobals like this but it's required by custom passes currently.
                        // We will probably be able to remove those once we push custom passes fully to render graph.
                        context.cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                    });
            }

            // Note: hdCamera.colorPyramidHistoryMipCount is going to be one frame late here (rendering, which is done later, is updating it)
            // In practice this should not be a big problem as it's only for debug purpose here.
            var scale = new Vector4(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y, 0f, 0f);
            PushFullScreenDebugTextureMip(renderGraph, output, hdCamera.colorPyramidHistoryMipCount, scale, fsDebugMode);
        }

        class AccumulateDistortionPassData
        {
            public TextureHandle distortionBuffer;
            public TextureHandle depthStencilBuffer;
            public RendererListHandle distortionRendererList;
            public FrameSettings frameSettings;
        }

        TextureHandle AccumulateDistortion(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle depthStencilBuffer,
            RendererListHandle distortionRendererList)
        {
            using (var builder = renderGraph.AddRenderPass<AccumulateDistortionPassData>("Accumulate Distortion", out var passData, ProfilingSampler.Get(HDProfileId.AccumulateDistortion)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.distortionBuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetDistortionBufferFormat(), clearBuffer = true, clearColor = Color.clear, name = "Distortion" }), 0);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.distortionRendererList = builder.UseRendererList(distortionRendererList);

                builder.SetRenderFunc(
                    (AccumulateDistortionPassData data, RenderGraphContext context) =>
                    {
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.distortionRendererList);
                    });

                return passData.distortionBuffer;
            }
        }

        class RenderDistortionPassData
        {
            public Material applyDistortionMaterial;
            public TextureHandle sourceColorBuffer;
            public TextureHandle distortionBuffer;
            public TextureHandle colorBuffer;
            public TextureHandle depthStencilBuffer;
            public Vector4 size;
            public bool roughDistortion;
        }

        void RenderDistortion(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle depthStencilBuffer,
            TextureHandle colorPyramidBuffer,
            TextureHandle distortionBuffer,
            RendererListHandle distortionRendererList)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion))
                return;

            using (var builder = renderGraph.AddRenderPass<RenderDistortionPassData>("Apply Distortion", out var passData, ProfilingSampler.Get(HDProfileId.ApplyDistortion)))
            {
                builder.DependsOn(distortionRendererList);

                passData.applyDistortionMaterial = m_ApplyDistortionMaterial;
                passData.roughDistortion = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughDistortion);
                passData.sourceColorBuffer = passData.roughDistortion ? builder.ReadTexture(colorPyramidBuffer) : builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GetColorBufferFormat(), name = "DistortionIntermediateBuffer" });
                passData.distortionBuffer = builder.ReadTexture(distortionBuffer);
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.size = new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);

                builder.SetRenderFunc(
                    (RenderDistortionPassData data, RenderGraphContext context) =>
                    {
                        if (!data.roughDistortion)
                            HDUtils.BlitCameraTexture(context.cmd, data.colorBuffer, data.sourceColorBuffer);

                        // TODO: Set stencil stuff via parameters rather than hard-coding it in shader.
                        data.applyDistortionMaterial.SetTexture(HDShaderIDs._DistortionTexture, data.distortionBuffer);
                        data.applyDistortionMaterial.SetTexture(HDShaderIDs._ColorPyramidTexture, data.sourceColorBuffer);
                        data.applyDistortionMaterial.SetVector(HDShaderIDs._Size, data.size);
                        data.applyDistortionMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.DistortionVectors);
                        data.applyDistortionMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.DistortionVectors);
                        data.applyDistortionMaterial.SetInt(HDShaderIDs._RoughDistortion, data.roughDistortion ? 1 : 0);

                        HDUtils.DrawFullScreen(context.cmd, data.applyDistortionMaterial, data.colorBuffer, data.depthStencilBuffer, null, 0);
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
                    msaaSamples = msaa ? hdCamera.msaaSamples : MSAASamples.None,
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
            public TextureHandle input;
            public TextureHandle output;
            public Material resolveMaterial;
            public int passIndex;
        }

        TextureHandle ResolveMSAAColor(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle input)
        {
            var outputDesc = renderGraph.GetTextureDesc(input);
            outputDesc.msaaSamples = MSAASamples.None;
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
            if (hdCamera.msaaEnabled)
            {
                using (var builder = renderGraph.AddRenderPass<ResolveColorData>("ResolveColor", out var passData))
                {
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(output, 0);
                    passData.resolveMaterial = m_ColorResolveMaterial;
                    passData.passIndex = SampleCountToPassIndex(hdCamera.msaaSamples);

                    builder.SetRenderFunc(
                        (ResolveColorData data, RenderGraphContext context) =>
                        {
                            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpb.SetTexture(HDShaderIDs._ColorTextureMS, data.input);
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
            if (hdCamera.msaaEnabled)
            {
                using (var builder = renderGraph.AddRenderPass<ResolveMotionVectorData>("ResolveMotionVector", out var passData))
                {
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(CreateMotionVectorBuffer(renderGraph, false, MSAASamples.None), 0);
                    passData.resolveMaterial = m_MotionVectorResolve;
                    passData.passIndex = SampleCountToPassIndex(hdCamera.msaaSamples);

                    builder.SetRenderFunc(
                        (ResolveMotionVectorData data, RenderGraphContext context) =>
                        {
                            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpb.SetTexture(HDShaderIDs._MotionVectorTextureMS, data.input);
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

        void RenderGizmos(RenderGraph renderGraph, HDCamera hdCamera, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos() &&
                (hdCamera.camera.cameraType == CameraType.Game || hdCamera.camera.cameraType == CameraType.SceneView))
            {
                bool renderPrePostprocessGizmos = (gizmoSubset == GizmoSubset.PreImageEffects);
                using (var builder = renderGraph.AddRenderPass<RenderGizmosPassData>(renderPrePostprocessGizmos ? "PrePostprocessGizmos" : "Gizmos", out var passData))
                {
                    bool isMatCapView = m_CurrentDebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.MatcapView;

                    passData.gizmoSubset = gizmoSubset;
                    passData.camera = hdCamera.camera;
                    passData.exposureTexture = isMatCapView ? (Texture)Texture2D.blackTexture : GetExposureTexture(hdCamera).rt;

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

        bool RenderCustomPass(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            in PrepassOutput prepassOutput,
            CullingResults cullingResults,
            CullingResults cameraCullingResults,
            CustomPassInjectionPoint injectionPoint,
            AOVRequestData aovRequest,
            List<RTHandle> aovCustomPassBuffers)
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
                    // TODO RENDERGRAPH: we can't replace the Lazy<RTHandle> buffers with RenderGraph resource because they are part of the current public API.
                    // To replace them correctly we need users to actually write render graph passes and explicit whether or not they want to use those buffers.
                    // We'll do it when we switch fully to render graph for custom passes.
                    customColorBuffer = m_CustomPassColorBuffer,
                    customDepthBuffer = m_CustomPassDepthBuffer,

                    // Render Graph Specific textures
                    colorBufferRG = colorBuffer,
                    nonMSAAColorBufferRG = m_NonMSAAColorBuffer,
                    depthBufferRG = prepassOutput.depthBuffer,
                    normalBufferRG = prepassOutput.resolvedNormalBuffer,
                    motionVectorBufferRG = prepassOutput.resolvedMotionVectorsBuffer
                };
                executed |= customPass.Execute(renderGraph, hdCamera, cullingResults, cameraCullingResults, customPassTargets);
            }

            // Push the custom pass buffer, in case it was requested in the AOVs
            aovRequest.PushCustomPassTexture(renderGraph, injectionPoint, colorBuffer, m_CustomPassColorBuffer, aovCustomPassBuffers);

            return executed;
        }

        private class UpdatePostProcessScreenSizePassData
        {
            public int postProcessWidth;
            public int postProcessHeight;
            public HDCamera hdCamera;
            public ShaderVariablesGlobal shaderVariablesGlobal;
        }

        internal void UpdatePostProcessScreenSize(RenderGraph renderGraph, HDCamera hdCamera, int postProcessWidth, int postProcessHeight)
        {
            using (var builder = renderGraph.AddRenderPass<UpdatePostProcessScreenSizePassData>("Update RT Handle Scales CB", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.shaderVariablesGlobal = m_ShaderVariablesGlobalCB;
                passData.postProcessWidth = postProcessWidth;
                passData.postProcessHeight = postProcessHeight;

                builder.SetRenderFunc(
                    (UpdatePostProcessScreenSizePassData data, RenderGraphContext ctx) =>
                    {
                        data.hdCamera.SetPostProcessScreenSize(data.postProcessWidth, data.postProcessHeight);
                        data.hdCamera.UpdateScalesAndScreenSizesCB(ref data.shaderVariablesGlobal);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesGlobal, HDShaderIDs._ShaderVariablesGlobal);
                    });
            }
        }

        class ResetCameraSizeForAfterPostProcessPassData
        {
            public HDCamera hdCamera;
            public ShaderVariablesGlobal shaderVariablesGlobal;
        }

        void ResetCameraSizeForAfterPostProcess(RenderGraph renderGraph, HDCamera hdCamera, CommandBuffer commandBuffer)
        {
            if (DynamicResolutionHandler.instance.DynamicResolutionEnabled())
            {
                using (var builder = renderGraph.AddRenderPass<ResetCameraSizeForAfterPostProcessPassData>("Reset Camera Size After Post Process", out var passData))
                {
                    passData.hdCamera = hdCamera;
                    passData.shaderVariablesGlobal = m_ShaderVariablesGlobalCB;
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(
                        (ResetCameraSizeForAfterPostProcessPassData data, RenderGraphContext ctx) =>
                        {
                            data.shaderVariablesGlobal._ScreenSize = new Vector4(data.hdCamera.finalViewport.width, data.hdCamera.finalViewport.height, 1.0f / data.hdCamera.finalViewport.width, 1.0f / data.hdCamera.finalViewport.height);
                            data.shaderVariablesGlobal._RTHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
                            ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesGlobal, HDShaderIDs._ShaderVariablesGlobal);
                            RTHandles.SetReferenceSize((int)data.hdCamera.finalViewport.width, (int)data.hdCamera.finalViewport.height);
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

        static void UpdateOffscreenRenderingConstants(ref ShaderVariablesGlobal cb, bool enabled, float factor)
        {
            cb._OffScreenRendering = enabled ? 1u : 0u;
            cb._OffScreenDownsampleFactor = factor;
        }
    }
}
