using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.VFX;

// Resove the ambiguity in the RendererList name (pick the in-engine version)
using RendererList = UnityEngine.Rendering.RendererList;
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

                //Set resolution group for the entire frame
                SetCurrentResolutionGroup(m_RenderGraph, hdCamera, ResolutionGroup.BeforeDynamicResUpscale);

                // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
                // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                if (!m_CurrentDebugDisplaySettings.IsMatcapViewEnabled(hdCamera))
                    m_SkyManager.UpdateEnvironment(m_RenderGraph, hdCamera, GetMainLight(), m_CurrentDebugDisplaySettings);

                // We need to initialize the MipChainInfo here, so it will be available to any render graph pass that wants to use it during setup
                // Be careful, ComputePackedMipChainInfo needs the render texture size and not the viewport size. Otherwise it would compute the wrong size.
                hdCamera.depthBufferMipChainInfo.ComputePackedMipChainInfo(RTHandles.rtHandleProperties.currentRenderTargetSize, RequiredCheckerboardMipCountInDepthPyramid(hdCamera));

                // Bind the depth pyramid offset info for the HDSceneDepth node in ShaderGraph. This can be used by users in custom passes.
                commandBuffer.SetGlobalBuffer(HDShaderIDs._DepthPyramidMipLevelOffsets, hdCamera.depthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));

#if UNITY_EDITOR
                GPUInlineDebugDrawer.BindProducers(hdCamera, m_RenderGraph);
#endif

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
                TextureHandle colorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, msaa, true);
                m_NonMSAAColorBuffer = CreateColorBuffer(m_RenderGraph, hdCamera, false);
                TextureHandle currentColorPyramid = m_RenderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain));
                TextureHandle rayCountTexture = RayCountManager.CreateRayCountTexture(m_RenderGraph);
#if ENABLE_VIRTUALTEXTURES
                TextureHandle vtFeedbackBuffer = VTBufferManager.CreateVTFeedbackBuffer(m_RenderGraph, hdCamera.msaaSamples);
                bool resolveVirtualTextureFeedback = true;
#else
                TextureHandle vtFeedbackBuffer = TextureHandle.nullHandle;
#endif

                // Evaluate the ray tracing acceleration structure debug views
                EvaluateRTASDebugView(m_RenderGraph, hdCamera);

                LightingBuffers lightingBuffers = new LightingBuffers();
                lightingBuffers.diffuseLightingBuffer = CreateDiffuseLightingBuffer(m_RenderGraph, hdCamera.msaaSamples);
                lightingBuffers.sssBuffer = CreateSSSBuffer(m_RenderGraph, hdCamera, hdCamera.msaaSamples);

                TextureHandle thicknessTexture = CreateThicknessTexture(m_RenderGraph, hdCamera);

                var prepassOutput = RenderPrepass(m_RenderGraph, colorBuffer, lightingBuffers.sssBuffer, thicknessTexture, vtFeedbackBuffer, cullingResults, customPassCullingResults, hdCamera, aovRequest, aovBuffers);

                // Need this during debug render at the end outside of the main loop scope.
                // Once render graph move is implemented, we can probably remove the branch and this.
                ShadowResult shadowResult = new ShadowResult();
                BuildGPULightListOutput gpuLightListOutput = new BuildGPULightListOutput();
                TextureHandle uiBuffer = m_RenderGraph.defaultResources.blackTextureXR;
                TextureHandle opticalFogTransmittance = TextureHandle.nullHandle;

                // Volume components
                PathTracing pathTracing = hdCamera.volumeStack.GetComponent<PathTracing>();

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
                else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && pathTracing.enable.value && hdCamera.camera.cameraType != CameraType.Preview && GetRayTracingState() && GetRayTracingClusterState())
                {
                    // We only request the light cluster if we are gonna use it for debug mode
                    if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode && GetRayTracingClusterState())
                    {
                        HDRaytracingLightCluster lightCluster = RequestLightCluster();
                        lightCluster.EvaluateClusterDebugView(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture);
                    }

                    DecalSystem.instance.UpdateShaderGraphAtlasTextures(m_RenderGraph);

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

                    lightingBuffers.ambientOcclusionBuffer = RenderAmbientOcclusion(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, prepassOutput.resolvedNormalBuffer, prepassOutput.resolvedMotionVectorsBuffer, historyValidationTexture, hdCamera.depthBufferMipChainInfo, m_ShaderVariablesRayTracingCB, rayCountTexture);
                    lightingBuffers.contactShadowsBuffer = RenderContactShadows(m_RenderGraph, hdCamera, msaa ? prepassOutput.depthValuesMSAA : prepassOutput.depthPyramidTexture, gpuLightListOutput, hdCamera.depthBufferMipChainInfo.mipLevelOffsets[1].y);

                    TransparentPrepassOutput transparentPrepass = default;
                    var volumetricDensityBuffer = TextureHandle.nullHandle;
                    if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water))
                        volumetricDensityBuffer = ClearAndHeightFogVoxelizationPass(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, transparentPrepass);

                    // Render Reflections. Due to incorrect resource barriers on some platforms, we have to run SSR before shadow maps to have correct async compute
                    // However, raytraced reflections don't run async have to be executed after shadows maps to get correct shadowing, so the call is duplicated here
                    // Evaluate the clear coat mask texture based on the lit shader mode
                    var clearCoatMask = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? prepassOutput.gbuffer.mrt[2] : m_RenderGraph.defaultResources.blackTextureXR;
                    var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
                    bool rtReflections = EnableRayTracedReflections(hdCamera, settings);
                    if (!rtReflections)
                        lightingBuffers.ssrLightingBuffer = RenderSSR(m_RenderGraph, hdCamera, ref prepassOutput, clearCoatMask, rayCountTexture, historyValidationTexture, m_SkyManager.GetSkyReflection(hdCamera), transparent: false);

                    RenderShadows(m_RenderGraph, hdCamera, cullingResults, ref shadowResult);

                    StartXRSinglePass(m_RenderGraph, hdCamera);

                    if (rtReflections)
                        lightingBuffers.ssrLightingBuffer = RenderSSR(m_RenderGraph, hdCamera, ref prepassOutput, clearCoatMask, rayCountTexture, historyValidationTexture, m_SkyManager.GetSkyReflection(hdCamera), transparent: false);
                    lightingBuffers.ssgiLightingBuffer = RenderScreenSpaceIndirectDiffuse(hdCamera, prepassOutput, rayCountTexture, historyValidationTexture, gpuLightListOutput.lightList);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && GetRayTracingClusterState())
                    {
                        HDRaytracingLightCluster lightCluster = RequestLightCluster();
                        lightCluster.EvaluateClusterDebugView(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture);
                    }

                    lightingBuffers.screenspaceShadowBuffer = RenderScreenSpaceShadows(m_RenderGraph, hdCamera, prepassOutput, prepassOutput.depthBuffer, prepassOutput.normalBuffer, prepassOutput.motionVectorsBuffer, historyValidationTexture, rayCountTexture);

                    var maxZMask = GenerateMaxZPass(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, hdCamera.depthBufferMipChainInfo);

                    var deferredLightingOutput = RenderDeferredLighting(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, lightingBuffers, prepassOutput.gbuffer, shadowResult, gpuLightListOutput);

                    RenderForwardOpaque(m_RenderGraph, hdCamera, colorBuffer, lightingBuffers, gpuLightListOutput, prepassOutput, vtFeedbackBuffer, shadowResult, cullingResults);

                    if (IsComputeThicknessNeeded(hdCamera))
                        // Compute the thickness for All Transparent which can be occluded by opaque written on the DepthBuffer (which includes the Forward Opaques).
                        RenderThickness(m_RenderGraph, cullingResults, thicknessTexture, prepassOutput.depthPyramidTexture, hdCamera, HDRenderQueue.k_RenderQueue_AllTransparent, true);

                    if (aovRequest.isValid)
                        aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.Normals, hdCamera, prepassOutput.resolvedNormalBuffer, aovBuffers);

                    RenderSubsurfaceScattering(m_RenderGraph, hdCamera, colorBuffer, historyValidationTexture, ref lightingBuffers, ref prepassOutput);

                    RenderSky(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer);

                    // Send all the geometry graphics buffer to client systems if required (must be done after the pyramid and before the transparent depth pre-pass)
                    SendGeometryGraphicsBuffers(m_RenderGraph, prepassOutput.normalBuffer, prepassOutput.depthPyramidTexture, hdCamera);

                    RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.AfterOpaqueAndSky, aovRequest, aovCustomPassBuffers);

                    DoUserAfterOpaqueAndSky(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, prepassOutput.resolvedNormalBuffer, prepassOutput.resolvedMotionVectorsBuffer);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                        DecalSystem.instance.UpdateShaderGraphAtlasTextures(m_RenderGraph);

                    // No need for old stencil values here since from transparent on different features are tagged
                    ClearStencilBuffer(m_RenderGraph, hdCamera, prepassOutput.depthBuffer);

                    // Render transparent prepass for refractive object sorting
                    transparentPrepass = RenderTransparentPrepass(m_RenderGraph, cullingResults, hdCamera, currentColorPyramid, gpuLightListOutput, ref prepassOutput);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water))
                        volumetricDensityBuffer = ClearAndHeightFogVoxelizationPass(m_RenderGraph, hdCamera, prepassOutput.depthBuffer, transparentPrepass);

                    volumetricDensityBuffer = FogVolumeAndVFXVoxelizationPass(m_RenderGraph, hdCamera, volumetricDensityBuffer, m_VisibleVolumeBoundsBuffer, cullingResults);
                    var volumetricLighting = VolumetricLightingPass(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, volumetricDensityBuffer, maxZMask, transparentPrepass, prepassOutput.depthBuffer, gpuLightListOutput.bigTileVolumetricLightList, shadowResult);

                    colorBuffer = RenderOpaqueFog(m_RenderGraph, hdCamera, colorBuffer, volumetricLighting, msaa, in prepassOutput, in transparentPrepass, ref opticalFogTransmittance);

                    RenderClouds(m_RenderGraph, hdCamera, colorBuffer, transparentPrepass.depthBufferPreRefraction, in prepassOutput, ref transparentPrepass, ref opticalFogTransmittance);

                    colorBuffer = RenderTransparency(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.resolvedNormalBuffer, vtFeedbackBuffer, currentColorPyramid, volumetricLighting, rayCountTexture, opticalFogTransmittance,
                        m_SkyManager.GetSkyReflection(hdCamera), gpuLightListOutput, transparentPrepass, ref prepassOutput, shadowResult, cullingResults, customPassCullingResults, aovRequest, aovCustomPassBuffers);

                    uiBuffer = RenderTransparentUI(m_RenderGraph, hdCamera);

                    if (NeedMotionVectorForTransparent(hdCamera.frameSettings))
                    {
                        prepassOutput.resolvedMotionVectorsBuffer = ResolveMotionVector(m_RenderGraph, hdCamera, prepassOutput.motionVectorsBuffer);
                    }

                    // We push the motion vector debug texture here as transparent object can overwrite the motion vector texture content.
                    if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                    {
                        PushFullScreenDebugTexture(m_RenderGraph, prepassOutput.resolvedMotionVectorsBuffer, FullScreenDebugMode.MotionVectors, fullScreenDebugFormat);
                        PushFullScreenDebugTexture(m_RenderGraph, prepassOutput.resolvedMotionVectorsBuffer, FullScreenDebugMode.MotionVectorsIntensity, fullScreenDebugFormat);
                    }

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
                                format = GetColorBufferFormat(),
                                enableRandomWrite = true,
                                useMipMap = true,
                                autoGenerateMips = false,
                                name = "DistortionColorBufferMipChain"
                            });
                        GenerateColorPyramid(m_RenderGraph, hdCamera, colorBuffer, distortionColorPyramid, FullScreenDebugMode.PreRefractionColorPyramid, distortionRendererList);
                        currentColorPyramid = distortionColorPyramid;


                        // The color pyramid for distortion is not an history, so it need to be sampled appropriate RT handle scale. Thus we need to update it
                        var newScale = new Vector4(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y, 0, 0);
                        m_ShaderVariablesGlobalCB._ColorPyramidUvScaleAndLimitCurrentFrame = newScale;
                        PushGlobalCameraParams(m_RenderGraph, hdCamera);
                    }

                    using (new RenderGraphProfilingScope(m_RenderGraph, ProfilingSampler.Get(HDProfileId.Distortion)))
                    {
                        var distortionBuffer = AccumulateDistortion(m_RenderGraph, hdCamera, transparentPrepass.resolvedDepthBufferPreRefraction, distortionRendererList);
                        RenderDistortion(m_RenderGraph, hdCamera, colorBuffer, transparentPrepass.resolvedDepthBufferPreRefraction, currentColorPyramid, distortionBuffer, distortionRendererList);
                    }

                    PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.NanTracker, fullScreenDebugFormat);
                    PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.WorldSpacePosition, fullScreenDebugFormat);
                    PushFullScreenLightingDebugTexture(m_RenderGraph, colorBuffer, fullScreenDebugFormat);

                    bool accumulateInPost = m_PostProcessEnabled && m_DepthOfField.IsActive();
                    if (!accumulateInPost && m_SubFrameManager.isRecording && m_SubFrameManager.subFrameCount > 1)
                    {
                        RenderAccumulation(m_RenderGraph, hdCamera, colorBuffer, colorBuffer, null, false);
                    }

                    // Render gizmos that should be affected by post processes
                    RenderGizmos(m_RenderGraph, hdCamera, GizmoSubset.PreImageEffects);
                }

#if ENABLE_VIRTUALTEXTURES
                //Check debug data to see if user disabled streaming.
                if (HDDebugDisplaySettings.Instance != null && HDDebugDisplaySettings.Instance.vtSettings.data.debugDisableResolving)
                    resolveVirtualTextureFeedback = false;

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

                bool postProcessIsFinalPass = HDUtils.PostProcessIsFinalPass(hdCamera, aovRequest);
                TextureHandle afterPostProcessBuffer = RenderAfterPostProcessObjects(m_RenderGraph, hdCamera, pathTracing, cullingResults, prepassOutput);
                var postProcessTargetFace = postProcessIsFinalPass ? target.face : CubemapFace.Unknown;
                TextureHandle postProcessDest = RenderPostProcess(m_RenderGraph, prepassOutput, colorBuffer, backBuffer, uiBuffer, afterPostProcessBuffer, opticalFogTransmittance, cullingResults, hdCamera, postProcessTargetFace, postProcessIsFinalPass);

                var xyMapping = GenerateDebugHDRxyMapping(m_RenderGraph, hdCamera, postProcessDest);
                GenerateDebugImageHistogram(m_RenderGraph, hdCamera, postProcessDest);
                PushFullScreenExposureDebugTexture(m_RenderGraph, postProcessDest, fullScreenDebugFormat);
                PushFullScreenHDRDebugTexture(m_RenderGraph, postProcessDest, fullScreenDebugFormat);
                PushFullScreenDebugTexture(m_RenderGraph, colorBuffer, FullScreenDebugMode.VolumetricFog);

#if UNITY_EDITOR
                GPUInlineDebugDrawer.Draw(m_RenderGraph);
#endif

                if (aovRequest.isValid)
                {
                    aovRequest.PushCameraTexture(m_RenderGraph, AOVBuffers.VolumetricFog, hdCamera, colorBuffer, aovBuffers);
                }

                ResetCameraDataAfterPostProcess(m_RenderGraph, hdCamera, commandBuffer);

                RenderCustomPass(m_RenderGraph, hdCamera, postProcessDest, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.AfterPostProcess, aovRequest, aovCustomPassBuffers);

                // Copy and rescale depth buffer for XR devices
                if (hdCamera.xr.enabled && hdCamera.xr.copyDepth)
                    CopyDepth(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, backBuffer, true);

                if (m_CurrentDebugDisplaySettings.data.historyBuffersView != -1)
                {
                    int historyFrameIndex = Mathf.Min(m_CurrentDebugDisplaySettings.data.historyBufferFrameIndex, hdCamera.GetHistoryFrameCount(m_CurrentDebugDisplaySettings.data.historyBuffersView)-1);
                    RTHandle historyRT = hdCamera.GetFrameRT(m_CurrentDebugDisplaySettings.data.historyBuffersView, historyFrameIndex);
                    TextureHandle historyTexture = m_RenderGraph.defaultResources.blackTextureArrayXR;
                    if (historyRT != null)
                    {
                        historyTexture = m_RenderGraph.ImportTexture(historyRT);
                    }
                    PushFullScreenHistoryBuffer(m_RenderGraph, historyTexture, (HDCameraFrameHistoryType)m_CurrentDebugDisplaySettings.data.historyBuffersView);
                }

                // In developer build, we always render post process in an intermediate buffer at (0,0) in which we will then render debug.
                // Because of this, we need another blit here to the final render target at the right viewport.
                if (!postProcessIsFinalPass)
                {
                    hdCamera.ExecuteCaptureActions(m_RenderGraph, postProcessDest);

                    postProcessDest = RenderDebug(m_RenderGraph,
                        hdCamera,
                        postProcessDest,
                        prepassOutput.resolvedDepthBuffer,
                        prepassOutput.depthPyramidTexture,
                        colorPickerTexture,
                        rayCountTexture,
                        xyMapping,
                        gpuLightListOutput,
                        shadowResult,
                        cullingResults,
                        fullScreenDebugFormat);

                    StopXRSinglePass(m_RenderGraph, hdCamera);

                    for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                    {
                        BlitFinalCameraTexture(m_RenderGraph, hdCamera, postProcessDest, backBuffer, uiBuffer, afterPostProcessBuffer, viewIndex, HDROutputActiveForCameraType(hdCamera), target.face);
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
                        BlitFinalCameraTexture(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, m_RenderGraph.ImportTexture(target.targetDepth), uiBuffer, afterPostProcessBuffer, viewIndex, outputsToHDR: false, cubemapFace: target.face);
                    }
                }

                SendColorGraphicsBuffer(m_RenderGraph, hdCamera);

                SetFinalTarget(m_RenderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, backBuffer, target.face);

                RenderWireOverlay(m_RenderGraph, hdCamera, backBuffer);

                RenderGizmos(m_RenderGraph, hdCamera, GizmoSubset.PostImageEffects);

                // Stop XR single pass before rendering screenspace UI
                StopXRSinglePass(m_RenderGraph, hdCamera);

                RenderScreenSpaceOverlayUI(m_RenderGraph, hdCamera, backBuffer);
            }
        }

        void ExecuteWithRenderGraph(RenderRequest renderRequest,
            AOVRequestData aovRequest,
            List<RTHandle> aovBuffers,
            List<RTHandle> aovCustomPassBuffers,
            ScriptableRenderContext renderContext,
            CommandBuffer commandBuffer)
        {
            var parameters = new RenderGraphParameters
            {
                executionName = renderRequest.hdCamera.name,
                currentFrameIndex = m_FrameCount,
                rendererListCulling = m_RenderGraphSettings.dynamicRenderPassCullingEnabled,
                scriptableRenderContext = renderContext,
                commandBuffer = commandBuffer
            };

            m_RenderGraph.BeginRecording(parameters);
            RecordRenderGraph(
                renderRequest, aovRequest, aovBuffers,
                aovCustomPassBuffers, renderContext, commandBuffer);
            m_RenderGraph.EndRecordingAndExecute();


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
            public Vector4 hdrOutputParmeters;
            public bool applyAfterPP;
            public CubemapFace cubemapFace;

            public TextureHandle uiTexture;
            public TextureHandle afterPostProcessTexture;
            public TextureHandle source;
            public TextureHandle destination;
            public ColorGamut colorGamut;
        }

        void BlitFinalCameraTexture(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle destination, TextureHandle uiTexture, TextureHandle afterPostProcessTexture, int viewIndex, bool outputsToHDR, CubemapFace cubemapFace)
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
                passData.afterPostProcessTexture = builder.ReadTexture(afterPostProcessTexture);
                passData.destination = builder.WriteTexture(destination);
                passData.applyAfterPP = false;
                passData.cubemapFace = cubemapFace;
                passData.colorGamut = outputsToHDR ? HDRDisplayColorGamutForCamera(hdCamera) : ColorGamut.sRGB;

                if (outputsToHDR)
                {
                    // Pick the right material based off XR rendering using texture arrays and if we are dealing with a single slice at the moment or processing all slices automatically.
                    passData.blitMaterial = (TextureXR.useTexArray && passData.srcTexArraySlice >= 0) ? m_FinalBlitWithOETFTexArraySingleSlice : m_FinalBlitWithOETF;
                    GetHDROutputParameters(HDRDisplayInformationForCamera(hdCamera), HDRDisplayColorGamutForCamera(hdCamera), m_Tonemapping, out passData.hdrOutputParmeters, out var unused);
                    passData.uiTexture = builder.ReadTexture(uiTexture);
                    passData.applyAfterPP = hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess) && !NeedHDRDebugMode(m_CurrentDebugDisplaySettings);
                }
                else
                {
                    passData.hdrOutputParmeters = new Vector4(-1.0f, -1.0f, -1.0f, -1.0f);
                }

                builder.SetRenderFunc(
                    (FinalBlitPassData data, RenderGraphContext context) =>
                    {
                        var propertyBlock = context.renderGraphPool.GetTempMaterialPropertyBlock();
                        RTHandle sourceTexture = data.source;

                        // We are in HDR mode so the final blit is different
                        if (data.hdrOutputParmeters.x >= 0)
                        {
                            data.blitMaterial.SetInt(HDShaderIDs._NeedsFlip, data.flip ? 1 : 0);
                            propertyBlock.SetTexture(HDShaderIDs._UITexture, data.uiTexture);
                            propertyBlock.SetTexture(HDShaderIDs._InputTexture, sourceTexture);

                            propertyBlock.SetVector(HDShaderIDs._HDROutputParams, data.hdrOutputParmeters);
                            propertyBlock.SetInt(HDShaderIDs._BlitTexArraySlice, data.srcTexArraySlice);

                            HDROutputUtils.ConfigureHDROutput(data.blitMaterial, data.colorGamut, HDROutputUtils.Operation.ColorEncoding);

                            if (data.applyAfterPP)
                            {
                                data.blitMaterial.EnableKeyword("APPLY_AFTER_POST");
                                data.blitMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, data.afterPostProcessTexture);
                            }
                            else
                            {
                                data.blitMaterial.DisableKeyword("APPLY_AFTER_POST");
                                data.blitMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, TextureXR.GetBlackTexture());
                            }
                        }
                        else
                        {
                            // Here we can't use the viewport scale provided in hdCamera. The reason is that this scale is for internal rendering before post process with dynamic resolution factored in.
                            // Here the input texture is already at the viewport size but may be smaller than the RT itself (because of the RTHandle system) so we compute the scale specifically here.
                            var scaleBias = new Vector4((float)data.viewport.width / sourceTexture.rt.width, (float)data.viewport.height / sourceTexture.rt.height, 0.0f, 0.0f);

                            if (data.flip)
                            {
                                scaleBias.w = scaleBias.y;
                                scaleBias.y *= -1;
                            }

                            propertyBlock.SetTexture(HDShaderIDs._BlitTexture, sourceTexture);
                            propertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
                            propertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                            propertyBlock.SetInt(HDShaderIDs._BlitTexArraySlice, data.srcTexArraySlice);

                        }
                        HDUtils.DrawFullScreen(context.cmd, data.viewport, data.blitMaterial, data.destination, data.cubemapFace, propertyBlock, 0, data.dstTexArraySlice);
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

        float GetGlobalMipBias(HDCamera hdCamera)
        {
            float globalMaterialMipBias;
            if (m_CurrentDebugDisplaySettings != null && m_CurrentDebugDisplaySettings.data.UseDebugGlobalMipBiasOverride())
                globalMaterialMipBias = m_CurrentDebugDisplaySettings.data.GetDebugGlobalMipBiasOverride();
            else
                globalMaterialMipBias = hdCamera.globalMipBias;

            return globalMaterialMipBias;
        }

        class SetFinalTargetPassData
        {
            public bool copyDepth;
            public Material copyDepthMaterial;
            public TextureHandle finalTarget;
            public CubemapFace finalTargetFace;
            public Rect finalViewport;
            public TextureHandle depthBuffer;
            public Vector2 blitScaleBias;
            public bool flipY;
        }

        void SetFinalTarget(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle finalTarget, CubemapFace finalTargetFace)
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
                passData.finalTargetFace = finalTargetFace;
                passData.finalViewport = hdCamera.finalViewport;

                if (passData.copyDepth)
                {
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.blitScaleBias = RTHandles.rtHandleProperties.rtHandleScale;
                    passData.flipY = hdCamera.isMainGameView || hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY;
                    passData.copyDepthMaterial = m_CopyDepth;
                }

                builder.SetRenderFunc(
                    (SetFinalTargetPassData data, RenderGraphContext ctx) =>
                    {
                        // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
                        ctx.cmd.SetRenderTarget(data.finalTarget, 0, data.finalTargetFace);
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
                                    mpb.SetVector(HDShaderIDs._BlitScaleBias, data.blitScaleBias);
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
            public TextureHandle depthBuffer;
            public TextureHandle output;
            public Vector2 blitScaleBias;
            public bool flipY;
        }

        void CopyDepth(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle output, bool copyForXR)
        {
            string name = copyForXR ? "Copy XR Depth" : "Copy Depth";
            var profileID = copyForXR ? HDProfileId.XRDepthCopy : HDProfileId.DuplicateDepthBuffer;
            using (var builder = renderGraph.AddRenderPass<CopyXRDepthPassData>(name, out var passData, ProfilingSampler.Get(profileID)))
            {
                passData.copyDepth = m_CopyDepth;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.output = builder.UseDepthBuffer(output, DepthAccess.Write);
                passData.blitScaleBias = copyForXR ? new Vector2(hdCamera.actualWidth / hdCamera.finalViewport.width, hdCamera.actualHeight / hdCamera.finalViewport.height)
                                                   : RTHandles.rtHandleProperties.rtHandleScale;
                passData.flipY = copyForXR;

                builder.SetRenderFunc(
                    (CopyXRDepthPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        mpb.SetTexture(HDShaderIDs._InputDepth, data.depthBuffer);
                        mpb.SetVector(HDShaderIDs._BlitScaleBias, data.blitScaleBias);
                        mpb.SetInt(HDShaderIDs._FlipY, data.flipY ? 1 : 0);

                        CoreUtils.DrawFullScreen(ctx.cmd, data.copyDepth, mpb);
                    });
            }
        }

        class ForwardPassData
        {
            public RendererListHandle rendererList;
            public BufferHandle lightListTile;
            public BufferHandle lightListCluster;

            public BufferHandle perVoxelOffset;
            public BufferHandle perTileLogBaseTweak;
            public TextureHandle thicknessTextureArray;
            public BufferHandle thicknessReindexMap;
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
            public TextureHandle colorPyramid;
            public TextureHandle transparentSSRLighting;
            public TextureHandle volumetricLighting;
            public TextureHandle depthPyramidTexture;
            public TextureHandle normalBuffer;
            public TextureHandle depthAndStencil;

            public bool preRefractionPass;
            public ShaderVariablesGlobal globalCB;
            public BufferHandle waterLine;
            public BufferHandle cameraHeightBuffer;
            public BufferHandle waterSurfaceProfiles;
            public TextureHandle waterGBuffer3;
        }

        void PrepareCommonForwardPassData(
            RenderGraph renderGraph,
            RenderGraphBuilder builder,
            ForwardPassData data,
            bool opaque,
            HDCamera hdCamera,
            RendererListHandle rendererList,
            in BuildGPULightListOutput lightLists,
            ShadowResult shadowResult)
        {
            FrameSettings frameSettings = hdCamera.frameSettings;
            bool useFptl = frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque) && opaque;

            data.frameSettings = frameSettings;
            data.lightListTile = builder.ReadBuffer(lightLists.lightList);
            data.lightListCluster = builder.ReadBuffer(lightLists.perVoxelLightLists);

            if (!useFptl)
            {
                data.perVoxelOffset = builder.ReadBuffer(lightLists.perVoxelOffset);
                if (lightLists.perTileLogBaseTweak.IsValid())
                    data.perTileLogBaseTweak = builder.ReadBuffer(lightLists.perTileLogBaseTweak);
            }
            else
            {
                data.perVoxelOffset = BufferHandle.nullHandle;
                data.perTileLogBaseTweak = BufferHandle.nullHandle;
            }
            data.rendererList = builder.UseRendererList(rendererList);
            if (IsComputeThicknessNeeded(hdCamera))
            {
                data.thicknessTextureArray = builder.ReadTexture(HDComputeThickness.Instance.GetThicknessTextureArray());
                data.thicknessReindexMap = builder.ReadBuffer(renderGraph.ImportBuffer(HDComputeThickness.Instance.GetReindexMap()));
            }
            else
            {
                data.thicknessTextureArray = builder.ReadTexture(renderGraph.defaultResources.blackTextureArrayXR);
                data.thicknessReindexMap = builder.ReadBuffer(renderGraph.ImportBuffer(m_ComputeThicknessReindexMap));
            }

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

        RenderQueueRange GetTransparentRenderQueueRange(HDCamera hdCamera, bool preRefraction)
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

            return transparentRange;
        }

        RendererListDesc PrepareForwardTransparentRendererList(CullingResults cullResults, HDCamera hdCamera, bool preRefraction)
        {
            RenderQueueRange transparentRange = GetTransparentRenderQueueRange(hdCamera, preRefraction);

            if (NeedMotionVectorForTransparent(hdCamera.frameSettings))
            {
                m_CurrentRendererConfigurationBakedLighting |= PerObjectData.MotionVectors; // This will enable the flag for low res transparent as well
            }

            var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
            return CreateTransparentRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, transparentRange);
        }

        static internal void RenderForwardRendererList(FrameSettings frameSettings,
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
            in PrepassOutput prepassOutput,
            TextureHandle vtFeedbackBuffer,
            ShadowResult shadowResult,
            CullingResults cullResults)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();

            using (var builder = renderGraph.AddRenderPass<ForwardOpaquePassData>(debugDisplay ? "Forward (+ Emissive) Opaque  Debug" : "Forward (+ Emissive) Opaque",
                out var passData,
                debugDisplay ? ProfilingSampler.Get(HDProfileId.ForwardOpaqueDebug) : ProfilingSampler.Get(HDProfileId.ForwardOpaque)))
            {
                var rendererList = renderGraph.CreateRendererList(PrepareForwardOpaqueRendererList(cullResults, hdCamera));
                PrepareCommonForwardPassData(renderGraph, builder, passData, true, hdCamera, rendererList, lightLists, shadowResult);

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
                builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
                builder.AllowRendererListCulling(false);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) && DecalSystem.instance.HasAnyForwardEmissive();
                passData.dbuffer = ReadDBuffer(prepassOutput.dbuffer, builder);
                passData.lightingBuffers = ReadLightingBuffers(lightingBuffers, builder);

                // Texture has been bound globally during prepass, warn rendergraph that we may use it here
                if (passData.enableDecals && hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers))
                    builder.ReadTexture(prepassOutput.renderingLayersBuffer);

                builder.SetRenderFunc(
                    (ForwardOpaquePassData data, RenderGraphContext context) =>
                    {
                        BindGlobalLightListBuffers(data, context);
                        BindDBufferGlobalData(data.dbuffer, context);
                        BindGlobalLightingBuffers(data.lightingBuffers, context.cmd);
                        BindGlobalThicknessBuffers(data.thicknessTextureArray, data.thicknessReindexMap, context.cmd);

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

        class RenderOffscreenUIData
        {
            public Camera camera;
            public FrameSettings frameSettings;
            public Rect viewport;
        }

        TextureHandle CreateOffscreenUIBuffer(RenderGraph renderGraph, MSAASamples msaaSamples, Rect viewport)
        {
            return renderGraph.CreateTexture(new TextureDesc((int)viewport.width, (int)viewport.height, false, true)
                { format = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = false, msaaSamples = msaaSamples, name = "UI Color Buffer" });
        }

        TextureHandle CreateOffscreenUIDepthBuffer(RenderGraph renderGraph, MSAASamples msaaSamples, Rect viewport)
        {
            return renderGraph.CreateTexture(new TextureDesc((int)viewport.width, (int)viewport.height, false, true)
                { format = GraphicsFormat.D32_SFloat_S8_UInt, clearBuffer = true, msaaSamples = msaaSamples, name = "UI Depth Buffer" });
        }


        TextureHandle RenderTransparentUI(RenderGraph renderGraph, HDCamera hdCamera)
        {
            var output = renderGraph.defaultResources.blackTextureXR;
            if (HDROutputActiveForCameraType(hdCamera) && SupportedRenderingFeatures.active.rendersUIOverlay && !NeedHDRDebugMode(m_CurrentDebugDisplaySettings))
            {
                using (var builder = renderGraph.AddRenderPass<RenderOffscreenUIData>("UI Rendering", out var passData, ProfilingSampler.Get(HDProfileId.OffscreenUIRendering)))
                {
                    // We cannot use rendererlist here because of the path tracing denoiser which will make it invalid due to multiple rendering per frame
                    output = builder.UseColorBuffer(CreateOffscreenUIBuffer(renderGraph, hdCamera.msaaSamples, hdCamera.finalViewport), 0);
                    builder.UseDepthBuffer(CreateOffscreenUIDepthBuffer(renderGraph, hdCamera.msaaSamples, hdCamera.finalViewport), DepthAccess.Write);

                    passData.camera = hdCamera.camera;
                    passData.frameSettings = hdCamera.frameSettings;
                    passData.viewport = new Rect(0.0f, 0.0f, hdCamera.finalViewport.width, hdCamera.finalViewport.height);

                    builder.SetRenderFunc((RenderOffscreenUIData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetViewport(data.viewport);
                        context.cmd.ClearRenderTarget(false, true, Color.clear);
                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                        if (data.camera.targetTexture == null)
                            context.renderContext.DrawUIOverlay(data.camera);
                    });
                }
            }

            return output;
        }

        class AfterPostProcessPassData
        {
            public ShaderVariablesGlobal globalCB;
            public HDCamera hdCamera;
            public RendererListHandle opaqueAfterPostprocessRL;
            public RendererListHandle transparentAfterPostprocessRL;
        }

        TextureHandle RenderAfterPostProcessObjects(RenderGraph renderGraph, HDCamera hdCamera, PathTracing pathTracing, CullingResults cullResults, in PrepassOutput prepassOutput)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                return renderGraph.defaultResources.blackTextureXR;

#if UNITY_64 && ENABLE_UNITY_DENOISING_PLUGIN && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            // For now disable this pass when path tracing is ON and denoising is active (the denoiser flushes the command buffer for syncing and invalidates the recorded RendererLists)
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                && GetRayTracingState()
                && GetRayTracingClusterState()
                && pathTracing.enable.value
                && hdCamera.camera.cameraType != CameraType.Preview
                && m_PathTracingSettings != null
                && m_PathTracingSettings.denoising.value != HDDenoiserType.None)
                return renderGraph.defaultResources.blackTextureXR;
#endif

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            using (var builder = renderGraph.AddRenderPass<AfterPostProcessPassData>("After Post-Process Objects", out var passData, ProfilingSampler.Get(HDProfileId.AfterPostProcessingObjects)))
            {
                bool useDepthBuffer = !hdCamera.RequiresCameraJitter() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.ZTestAfterPostProcessTAA);

                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.hdCamera = hdCamera;
                passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque)));
                passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent)));

                var output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = true, clearColor = Color.black, name = "OffScreen AfterPostProcess" }), 0);
                if (useDepthBuffer)
                    builder.UseDepthBuffer(prepassOutput.resolvedDepthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                    (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                    {
                        // Disable camera jitter. See coment in RestoreNonjitteredMatrices
                        if (data.hdCamera.RequiresCameraJitter())
                        {
                            data.hdCamera.UpdateAllViewConstants(false);
                            data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        }

                        UpdateOffscreenRenderingConstants(ref data.globalCB, true, 1.0f);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                        DrawOpaqueRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.opaqueAfterPostprocessRL);
                        // Setup off-screen transparency here
                        DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.transparentAfterPostprocessRL);

                        // Reenable camera jitter for CustomPostProcessBeforeTAA injection point
                        if (data.hdCamera.RequiresCameraJitter())
                        {
                            data.hdCamera.UpdateAllViewConstants(true);
                            data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);
                        }

                        UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1.0f);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    });

                return output;
            }
        }

        void RenderForwardTransparent(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle normalBuffer,
            in PrepassOutput prepassOutput,
            in TransparentPrepassOutput transparentPrepass,
            TextureHandle vtFeedbackBuffer,
            TextureHandle volumetricLighting,
            TextureHandle ssrLighting,
            TextureHandle? colorPyramid,
            in BuildGPULightListOutput lightLists,
            in ShadowResult shadowResult,
            CullingResults cullResults,
            bool preRefractionPass,
            RendererListHandle rendererList)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects))
                return;

            // If rough refraction are turned off, we render all transparents in the Transparent pass and we skip the PreRefraction one.
            bool refractionEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction);
            if (!refractionEnabled && preRefractionPass)
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
                PrepareCommonForwardPassData(renderGraph, builder, passData, false, hdCamera, rendererList, lightLists, shadowResult);

                var usedDepthBuffer = prepassOutput.depthBuffer;

                if (preRefractionPass)
                {
                    usedDepthBuffer = transparentPrepass.depthBufferPreRefraction;

                    passData.depthAndStencil = builder.ReadTexture(prepassOutput.resolvedDepthBuffer);
                }
                else if (!refractionEnabled)
                {
                    // if refraction is disabled, we did not create a copy of the depth buffer, so we need to create a dummy one here.
                    passData.depthAndStencil = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.D32_SFloat_S8_UInt, bindTextureMS = hdCamera.msaaSamples != MSAASamples.None, msaaSamples = hdCamera.msaaSamples, clearBuffer = false, name = "Dummy Depth", disableFallBackToImportedTexture = true, fallBackToBlackTexture = false});
                }
                else
                {
                    passData.depthAndStencil = builder.ReadTexture(transparentPrepass.resolvedDepthBufferPreRefraction);
                }


                // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
                // decal datas count is 0 if no decals affect transparency
                passData.decalsEnabled = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
                passData.renderMotionVecForTransparent = NeedMotionVectorForTransparent(hdCamera.frameSettings);
                passData.colorMaskTransparentVel = colorMaskTransparentVel;
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);
                passData.transparentSSRLighting = builder.ReadTexture(ssrLighting);
                passData.depthPyramidTexture = builder.ReadTexture(prepassOutput.depthPyramidTexture); // We need to bind this for transparent materials doing stuff like soft particles etc.

                // Water absorption buffers
                passData.waterSurfaceProfiles = builder.ReadBuffer(transparentPrepass.waterSurfaceProfiles);
                passData.waterGBuffer3 = builder.ReadTexture(transparentPrepass.waterGBuffer.waterGBuffer3);
                passData.cameraHeightBuffer = builder.ReadBuffer(transparentPrepass.waterGBuffer.cameraHeight);
                passData.waterLine = builder.ReadBuffer(transparentPrepass.waterLine);
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.preRefractionPass = preRefractionPass;

                builder.UseDepthBuffer(usedDepthBuffer, DepthAccess.ReadWrite);

                int index = 0;
                bool msaa = hdCamera.msaaEnabled;
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
                    // It doesn't really matter what gets bound here since the color mask state set will prevent this from ever being written to. However, we still need to bind something
                    // to avoid warnings about unbound render targets. The following rendertarget could really be anything if renderVelocitiesForTransparent
                    // Create a new target here should reuse existing already released one
                    builder.UseColorBuffer(builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { format = GraphicsFormat.R8G8B8A8_SRGB, bindTextureMS = msaa, msaaSamples = hdCamera.msaaSamples, name = "Transparency Velocity Dummy" }), index++);
                }

                if (transparentPrepass.enablePerPixelSorting)
                {
                    builder.UseColorBuffer(transparentPrepass.beforeRefraction, index++);
                    builder.UseColorBuffer(transparentPrepass.beforeRefractionAlpha, index++);
                }
                else
                {
                    builder.UseColorBuffer(builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R8G8B8A8_SRGB, bindTextureMS = msaa, msaaSamples = hdCamera.msaaSamples, name = "Before Water Color Dummy" }), index++);
                    builder.UseColorBuffer(builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R8G8B8A8_SRGB, bindTextureMS = msaa, msaaSamples = hdCamera.msaaSamples, name = "Before Water Alpha Dummy" }), index++);
                }

                if (colorPyramid != null && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) && !preRefractionPass)
                    passData.colorPyramid = builder.ReadTexture(colorPyramid.Value);
                else
                    passData.colorPyramid = renderGraph.defaultResources.blackTextureXR;

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

                        data.globalCB._PreRefractionPass = data.preRefractionPass ? 1 : 0;
                        ConstantBuffer.UpdateData(context.cmd, data.globalCB);

                        BindGlobalLightListBuffers(data, context);
                        BindGlobalThicknessBuffers(data.thicknessTextureArray, data.thicknessReindexMap, context.cmd);

                        context.cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                        context.cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, data.transparentSSRLighting);
                        context.cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, data.volumetricLighting);
                        context.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramidTexture);
                        context.cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        context.cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.depthAndStencil, RenderTextureSubElement.Stencil);
                        context.cmd.SetGlobalTexture(HDShaderIDs._RefractiveDepthBuffer, data.depthAndStencil, RenderTextureSubElement.Depth);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._WaterSurfaceProfiles, data.waterSurfaceProfiles);
                        context.cmd.SetGlobalTexture(HDShaderIDs._WaterGBufferTexture3, data.waterGBuffer3);
                        context.cmd.SetGlobalBuffer(HDShaderIDs._WaterLineBuffer, data.waterLine);

                        RenderForwardRendererList(data.frameSettings, data.rendererList, false, context.renderContext, context.cmd);
                    });
            }
        }

        void RenderTransparentDepthPrepass(RenderGraph renderGraph, HDCamera hdCamera, in PrepassOutput prepassOutput, CullingResults cull, RendererListHandle rendererList, bool preRefractionPass)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPrepass))
                return;

            var passName = preRefractionPass ? "Forward PreRefraction Prepass" : "Forward Transparent Prepass";
            var profilingId = preRefractionPass ? HDProfileId.PreRefractionDepthPrepass : HDProfileId.TransparentDepthPrepass;

            using (var builder = renderGraph.AddRenderPass<ForwardPassData>(passName, out var passData, ProfilingSampler.Get(profilingId)))
            {
                builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
                passData.rendererList = builder.UseRendererList(rendererList);
                passData.frameSettings = hdCamera.frameSettings;

                if (hdCamera.IsSSREnabled(transparent: true))
                {
                    int index = 0;
                    if (hdCamera.msaaEnabled)
                        builder.UseColorBuffer(prepassOutput.depthAsColor, index++);
                    builder.UseColorBuffer(prepassOutput.normalBuffer, index++);
                }

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
            public Rect viewport;
        }

        TextureHandle RenderLowResTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle downsampledDepth, CullingResults cullingResults, RendererListHandle rendererList)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLowResTransparentPassData>("Low Res Transparent", out var passData, ProfilingSampler.Get(HDProfileId.LowResTransparent)))
            {
                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.lowResScale = hdCamera.lowResScale;
                passData.frameSettings = hdCamera.frameSettings;
                passData.rendererList = builder.UseRendererList(rendererList);
                passData.viewport = hdCamera.lowResViewport;
                builder.UseDepthBuffer(downsampledDepth, DepthAccess.ReadWrite);
                // We need R16G16B16A16_SFloat as we need a proper alpha channel for compositing.
                var output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one * hdCamera.lowResScale, true, true)
                    { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, clearBuffer = true, clearColor = Color.black, name = "Low res transparent" }), 0);

                builder.SetRenderFunc(
                    (RenderLowResTransparentPassData data, RenderGraphContext context) =>
                    {
                        UpdateOffscreenRenderingConstants(ref data.globalCB, true, 1.0f / data.lowResScale);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                        context.cmd.SetViewport(data.viewport);
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);

                        UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1.0f);
                        ConstantBuffer.PushGlobal(context.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    });

                return output;
            }
        }

        class CombineTransparentPassData
        {
            public Vector4 shaderParams;
            public Material upsampleMaterial;
            public int passIndex;

            public TextureHandle lowResTransparentBuffer;
            public TextureHandle downsampledDepthBuffer;

            public TextureHandle beforeRefraction;
            public TextureHandle beforeRefractionAlpha;
        }

        void PrepareCombineTransparentData(RenderGraphBuilder builder, in TransparentPrepassOutput refractionOutput, ref CombineTransparentPassData passData)
        {
            passData.passIndex = 0;
            passData.upsampleMaterial = m_UpsampleTransparency;

            passData.beforeRefraction = builder.ReadTexture(refractionOutput.beforeRefraction);
            passData.beforeRefractionAlpha = builder.ReadTexture(refractionOutput.beforeRefractionAlpha);
        }

        void CombineTransparents(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, in TransparentPrepassOutput refractionOutput, RendererListHandle preRefractionList)
        {
            using (var builder = renderGraph.AddRenderPass<CombineTransparentPassData>("Transparents (Combine)", out var passData, ProfilingSampler.Get(HDProfileId.CombineTransparents)))
            {
                // If we have clouds, we must combine even if pre refraction list is empty
                if (!refractionOutput.clouds.valid) builder.DependsOn(preRefractionList);

                passData.passIndex = 0;
                passData.upsampleMaterial = m_UpsampleTransparency;

                passData.beforeRefraction = builder.ReadTexture(refractionOutput.beforeRefraction);
                passData.beforeRefractionAlpha = builder.ReadTexture(refractionOutput.beforeRefractionAlpha);
                builder.UseColorBuffer(colorBuffer, 0);

                builder.SetRenderFunc(
                    (CombineTransparentPassData data, RenderGraphContext context) =>
                    {
                        data.upsampleMaterial.SetTexture(HDShaderIDs._BeforeRefraction, data.beforeRefraction);
                        data.upsampleMaterial.SetTexture(HDShaderIDs._BeforeRefractionAlpha, data.beforeRefractionAlpha);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.upsampleMaterial, data.passIndex, MeshTopology.Triangles, 3, 1, null);
                    });
            }
        }

        void CombineAndUpsampleTransparent(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle lowResTransparentBuffer, TextureHandle downsampledDepthBuffer, in TransparentPrepassOutput refractionOutput, RendererListHandle preRefractionList, RendererListHandle lowResList)
        {
            // Combine & upsample
            using (var builder = renderGraph.AddRenderPass<CombineTransparentPassData>("Transparents (Combine and Upsample)", out var passData, ProfilingSampler.Get(HDProfileId.CombineAndUpsampleTransparent)))
            {
                // We need to execute this if we have prerefraction objects (combine) or low res transparents (upsample)
                // Warning: clouds are prerefraction objects
                if (!refractionOutput.clouds.valid)
                {
                    builder.DependsOn(preRefractionList);
                    builder.DependsOn(lowResList);
                }

                passData.passIndex = 1;
                passData.upsampleMaterial = m_UpsampleTransparency;

                Vector2 lowResDrsFactor = hdCamera.lowResDrsFactor;
                passData.shaderParams = new Vector4(hdCamera.lowResScale, 1.0f / hdCamera.lowResScale, lowResDrsFactor.x, lowResDrsFactor.y);

                passData.lowResTransparentBuffer = builder.ReadTexture(lowResTransparentBuffer);
                passData.downsampledDepthBuffer = builder.ReadTexture(downsampledDepthBuffer);
                builder.UseColorBuffer(colorBuffer, 0);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
                {
                    passData.passIndex = 2;
                    passData.beforeRefraction = builder.ReadTexture(refractionOutput.beforeRefraction);
                    passData.beforeRefractionAlpha = builder.ReadTexture(refractionOutput.beforeRefractionAlpha);
                }

                var settings = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings;
                if (settings.upsampleType == LowResTransparentUpsample.Bilinear)
                {
                    m_UpsampleTransparency.EnableKeyword("BILINEAR");
                }
                else if (settings.upsampleType == LowResTransparentUpsample.NearestDepth)
                {
                    m_UpsampleTransparency.EnableKeyword("NEAREST_DEPTH");
                }

                builder.SetRenderFunc(
                    (CombineTransparentPassData data, RenderGraphContext context) =>
                    {
                        data.upsampleMaterial.SetVector(HDShaderIDs._Params, data.shaderParams);
                        data.upsampleMaterial.SetTexture(HDShaderIDs._LowResTransparent, data.lowResTransparentBuffer);
                        data.upsampleMaterial.SetTexture(HDShaderIDs._LowResDepthTexture, data.downsampledDepthBuffer);
                        if (data.passIndex == 2)
                        {
                            data.upsampleMaterial.SetTexture(HDShaderIDs._BeforeRefraction, data.beforeRefraction);
                            data.upsampleMaterial.SetTexture(HDShaderIDs._BeforeRefractionAlpha, data.beforeRefractionAlpha);
                        }
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.upsampleMaterial, data.passIndex, MeshTopology.Triangles, 3, 1, null);
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
            if (!(recursiveSettings.enable.value && GetRayTracingState() && GetRayTracingClusterState()))
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

        internal struct TransparentPrepassOutput
        {
            public bool enablePerPixelSorting;
            public TextureHandle depthBufferPreRefraction;
            public TextureHandle resolvedDepthBufferPreRefraction;
            public TextureHandle beforeRefraction;
            public TextureHandle beforeRefractionAlpha;
            public TextureHandle flagMaskBuffer;

            // Water
            public WaterSurface underWaterSurface;

            public WaterSystem.WaterGBuffer waterGBuffer;
            public BufferHandle waterLine;
            public BufferHandle waterSurfaceProfiles;

            // Clouds
            public VolumetricCloudsSystem.VolumetricCloudsOutput clouds;
        }

        TransparentPrepassOutput RenderTransparentPrepass(RenderGraph renderGraph, CullingResults cullingResults, HDCamera hdCamera,
            TextureHandle currentColorPyramid, in BuildGPULightListOutput lightLists, ref PrepassOutput prepassOutput)
        {
            TransparentPrepassOutput output = new TransparentPrepassOutput()
            {
                enablePerPixelSorting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction),
                depthBufferPreRefraction = prepassOutput.depthBuffer,
                resolvedDepthBufferPreRefraction = prepassOutput.resolvedDepthBuffer,

                beforeRefraction = renderGraph.defaultResources.blackTextureXR,
                beforeRefractionAlpha = renderGraph.defaultResources.whiteTextureXR,
            };

            m_WaterSystem.InitializeWaterPrepassOutput(renderGraph, ref output);
            prepassOutput.waterLine = output.waterLine; // This buffer is used by custom passes

            var preRefraction = renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, m_TransparentDepthPrepassNames,
                renderQueueRange: GetTransparentRenderQueueRange(hdCamera, true)));

            // Transparent (non recursive) objects that are rendered in front of transparent (recursive) require the recursive rendering to be executed for that pixel.
            // This means our flagging process needs to happen before the transparent depth prepass as we use the depth to discriminate pixels that do not need recursive rendering.
            output.flagMaskBuffer = RenderRayTracingFlagMask(renderGraph, cullingResults, hdCamera, prepassOutput.depthBuffer);

            // Transparent Depth Prepass A (default render queue with TransparentDepthPrepass enabled, output to regular depth buffer)
            RenderTransparentDepthPrepass(renderGraph, hdCamera, prepassOutput, cullingResults, preRefraction, true);

            if (!output.enablePerPixelSorting)
                return output;

            output.depthBufferPreRefraction = CreateDepthBuffer(renderGraph, false, hdCamera.msaaSamples, "CameraDepthStencil PreRefraction", false);

            output.beforeRefraction = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { format = GraphicsFormat.B10G11R11_UFloatPack32, msaaSamples = hdCamera.msaaSamples, clearBuffer = true, name = "Before Refraction" });

            output.beforeRefractionAlpha = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { format = GraphicsFormat.R8_UNorm, msaaSamples = hdCamera.msaaSamples, clearBuffer = true, clearColor = Color.white, name = "Before Refraction Alpha" });

            bool hasWater = WaterSystem.ShouldRenderWater(hdCamera);
            var refraction = renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, m_TransparentDepthPrepassNames,
                renderQueueRange: GetTransparentRenderQueueRange(hdCamera, false)));

            // Copy depth buffer for refraction
            CopyDepth(renderGraph, hdCamera, prepassOutput.resolvedDepthBuffer, output.depthBufferPreRefraction, false);

            if (hasWater)
            {
                // Render the water gbuffer (and prepare for the transparent SSR pass)
                output.waterGBuffer = m_WaterSystem.RenderWaterGBuffer(renderGraph, cullingResults, hdCamera, prepassOutput.depthBuffer, prepassOutput.normalBuffer, currentColorPyramid, prepassOutput.depthPyramidTexture, lightLists);

                // Render Water Line
                m_WaterSystem.RenderWaterLine(renderGraph, hdCamera, prepassOutput.depthBuffer, ref output);
                prepassOutput.waterLine = output.waterLine;
            }

            // Transparent Depth Prepass B (SSR and refractive render queue, output to secondary depth)
            RenderTransparentDepthPrepass(renderGraph, hdCamera, prepassOutput, cullingResults, refraction, false);

            // Resolve depth buffer
            if (hdCamera.msaaSamples != MSAASamples.None)
            {
                using (var builder = renderGraph.AddRenderPass<ResolvePrepassData>("Resolve Transparent Prepass MSAA", out var passData))
                {
                    passData.depthResolveMaterial = m_MSAAResolveMaterialDepthOnly;
                    passData.depthResolvePassIndex = SampleCountToPassIndex(hdCamera.msaaSamples);
                    passData.depthAsColorBufferMSAA = builder.ReadTexture(output.depthBufferPreRefraction);

                    output.resolvedDepthBufferPreRefraction = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, true, MSAASamples.None), DepthAccess.Write);

                    builder.SetRenderFunc(
                        (ResolvePrepassData data, RenderGraphContext context) =>
                        {
                            CoreUtils.SetKeyword(context.cmd, "_HAS_MOTION_VECTORS", false);
                            data.depthResolveMaterial.SetTexture(HDShaderIDs._DepthTextureMS, data.depthAsColorBufferMSAA);
                            context.cmd.DrawProcedural(Matrix4x4.identity, data.depthResolveMaterial, data.depthResolvePassIndex, MeshTopology.Triangles, 3, 1);
                        });
                }
            }
            else
                output.resolvedDepthBufferPreRefraction = output.depthBufferPreRefraction;

            return output;
        }

        TextureHandle RenderTransparency(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle normalBuffer,
            TextureHandle vtFeedbackBuffer,
            TextureHandle currentColorPyramid,
            TextureHandle volumetricLighting,
            TextureHandle rayCountTexture,
            TextureHandle opticalFogTransmittance,
            Texture skyTexture,
            in BuildGPULightListOutput lightLists,
            in TransparentPrepassOutput transparentPrepass,
            ref PrepassOutput prepassOutput,
            ShadowResult shadowResult,
            CullingResults cullingResults,
            CullingResults customPassCullingResults,
            AOVRequestData aovRequest,
            List<RTHandle> aovCustomPassBuffers)
        {
            // this needs to be before transparency
            RenderProbeVolumeDebug(renderGraph, hdCamera, prepassOutput.depthPyramidTexture, normalBuffer);

            // Render the software line raster path.
            RenderLines(m_RenderGraph, prepassOutput.depthPyramidTexture, hdCamera, lightLists);

            // Immediately compose the lines if the user wants lines in the color pyramid (refraction), but with poor TAA ghosting.
            ComposeLines(renderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, prepassOutput.motionVectorsBuffer, (int)LineRendering.CompositionMode.BeforeColorPyramid);

            // Render the transparent SSR lighting
            var ssrLightingBuffer = RenderSSR(renderGraph, hdCamera, ref prepassOutput, renderGraph.defaultResources.blackTextureXR, rayCountTexture, renderGraph.defaultResources.blackTextureXR, skyTexture, transparent: true);

            colorBuffer = RaytracingRecursiveRender(renderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, transparentPrepass.flagMaskBuffer, rayCountTexture);

            // TODO RENDERGRAPH: Remove this when we properly convert custom passes to full render graph with explicit color buffer reads.
            // To allow users to fetch the current color buffer, we temporarily bind the camera color buffer
            SetGlobalColorForCustomPass(renderGraph, colorBuffer);
            RenderCustomPass(m_RenderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforePreRefraction, aovRequest, aovCustomPassBuffers);
            SetGlobalColorForCustomPass(renderGraph, currentColorPyramid);

            // Combine volumetric clouds with prerefraction transparents
            m_VolumetricClouds.CombineVolumetricClouds(renderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, transparentPrepass, ref opticalFogTransmittance);

            // Compose the lines if the user wants lines in the color pyramid (refraction), but after clouds.
            ComposeLines(renderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, prepassOutput.motionVectorsBuffer, (int)LineRendering.CompositionMode.BeforeColorPyramidAfterClouds);

            var preRefractionList = renderGraph.CreateRendererList(PrepareForwardTransparentRendererList(cullingResults, hdCamera, true));
            var refractionList = renderGraph.CreateRendererList(PrepareForwardTransparentRendererList(cullingResults, hdCamera, false));

            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, normalBuffer, prepassOutput, transparentPrepass, vtFeedbackBuffer, volumetricLighting, ssrLightingBuffer, null, lightLists, shadowResult, cullingResults, true, preRefractionList);

            // Render the deferred water lighting
            m_WaterSystem.RenderWaterLighting(renderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, prepassOutput.depthPyramidTexture, volumetricLighting, ssrLightingBuffer, transparentPrepass, lightLists, ref opticalFogTransmittance);

            // If required, render the water debug view
            m_WaterSystem.RenderWaterDebug(renderGraph, hdCamera, colorBuffer, prepassOutput.depthBuffer, transparentPrepass.waterGBuffer);

            bool ssmsEnabled = Fog.IsMultipleScatteringEnabled(hdCamera, out float fogMultipleScatteringIntensity);
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) || hdCamera.IsSSREnabled() || hdCamera.IsSSREnabled(true) || hdCamera.IsSSGIEnabled() || ssmsEnabled)
            {
                // Generate color pyramid
                // - after water lighting to ensure it's present in transparent ssr
                // - before render refractive transparents
                // - will also be used by opaque ssr next frame
                var resolvedColorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);
                GenerateColorPyramid(renderGraph, hdCamera, resolvedColorBuffer, currentColorPyramid, FullScreenDebugMode.FinalColorPyramid);
            }

            // Just after the color pyramid, we apply the fake multi-scattering effect on the fog
            if (ssmsEnabled && opticalFogTransmittance.IsValid())
                ScreenSpaceFogMultipleScattering(renderGraph, hdCamera, colorBuffer, opticalFogTransmittance, currentColorPyramid, fogMultipleScatteringIntensity);

            // We don't have access to the color pyramid with transparent if rough refraction is disabled
            RenderCustomPass(renderGraph, hdCamera, colorBuffer, prepassOutput, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforeTransparent, aovRequest, aovCustomPassBuffers);

            // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
            RenderForwardTransparent(renderGraph, hdCamera, colorBuffer, normalBuffer, prepassOutput, transparentPrepass, vtFeedbackBuffer, volumetricLighting, ssrLightingBuffer, currentColorPyramid, lightLists, shadowResult, cullingResults, false, refractionList);

            colorBuffer = ResolveMSAAColor(renderGraph, hdCamera, colorBuffer, m_NonMSAAColorBuffer);

            // Render All forward error
            RenderForwardError(renderGraph, hdCamera, colorBuffer, prepassOutput.resolvedDepthBuffer, cullingResults);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
                var lowResTranspRendererList = renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullingResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_LowTransparent));
                var lowResTransparentBuffer = RenderLowResTransparent(renderGraph, hdCamera, prepassOutput.downsampledDepthBuffer, cullingResults, lowResTranspRendererList);

                CombineAndUpsampleTransparent(renderGraph, hdCamera, colorBuffer, lowResTransparentBuffer, prepassOutput.downsampledDepthBuffer, transparentPrepass, preRefractionList, lowResTranspRendererList);
            }
            else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
                CombineTransparents(renderGraph, hdCamera, colorBuffer, transparentPrepass, preRefractionList);

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

        void PreRenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle normalbuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            m_SkyManager.PreRenderSky(renderGraph, hdCamera, normalbuffer, depthStencilBuffer);
        }

        void RenderSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthStencilBuffer)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            m_SkyManager.RenderSky(renderGraph, hdCamera, colorBuffer, depthStencilBuffer, "Render Sky", ProfilingSampler.Get(HDProfileId.RenderSky));
        }

        TextureHandle RenderOpaqueFog(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle volumetricLighting, bool msaa, in PrepassOutput prepassOutput, in TransparentPrepassOutput refractionOutput, ref TextureHandle opticalFogTransmittance)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return colorBuffer;

            return m_SkyManager.RenderOpaqueAtmosphericScattering(renderGraph, hdCamera, in refractionOutput, colorBuffer, msaa ? prepassOutput.depthAsColor : prepassOutput.depthPyramidTexture, volumetricLighting, prepassOutput.depthBuffer, prepassOutput.normalBuffer, ref opticalFogTransmittance);
        }

        void RenderClouds(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthStencilBuffer, in PrepassOutput prepassOutput, ref TransparentPrepassOutput transparentPrepass, ref TextureHandle opticalFogTransmittance)
        {
            if (m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                return;

            m_SkyManager.RenderClouds(renderGraph, hdCamera, colorBuffer, depthStencilBuffer, ref opticalFogTransmittance);

            m_VolumetricClouds.RenderVolumetricClouds(m_RenderGraph, hdCamera, colorBuffer, prepassOutput.depthPyramidTexture, ref transparentPrepass, ref opticalFogTransmittance);
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
                    new TextureDesc(Vector2.one, true, true) { format = Builtin.GetDistortionBufferFormat(), clearBuffer = true, clearColor = Color.clear, name = "Distortion" }), 0);
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
                passData.sourceColorBuffer = passData.roughDistortion ? builder.ReadTexture(colorPyramidBuffer) : builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { format = GetColorBufferFormat(), name = "DistortionIntermediateBuffer" });
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

        TextureHandle CreateColorBuffer(RenderGraph renderGraph, HDCamera hdCamera, bool msaa, bool fallbackToBlack = false)
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
                    format = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    msaaSamples = msaa ? hdCamera.msaaSamples : MSAASamples.None,
                    clearBuffer = NeedClearColorBuffer(hdCamera),
                    clearColor = GetColorBufferClearColor(hdCamera),
                    name = msaa ? "CameraColorMSAA" : "CameraColor",
                    fallBackToBlackTexture = fallbackToBlack
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

            TextureHandle renderingLayerMaskBuffer = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer) ? prepassOutput.renderingLayersBuffer : TextureHandle.nullHandle;

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
                motionVectorBufferRG = prepassOutput.resolvedMotionVectorsBuffer,
                renderingLayerMaskRG = renderingLayerMaskBuffer,
                waterLineRG = prepassOutput.waterLine,
            };

            bool executed = CustomPassVolume.ExecuteAllCustomPasses(renderGraph, hdCamera, cullingResults, cameraCullingResults, injectionPoint, customPassTargets);

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

        void ResetCameraDataAfterPostProcess(RenderGraph renderGraph, HDCamera hdCamera, CommandBuffer commandBuffer)
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
                            var screenSize = new Vector4(data.hdCamera.finalViewport.width, data.hdCamera.finalViewport.height, 1.0f / data.hdCamera.finalViewport.width, 1.0f / data.hdCamera.finalViewport.height);
                            data.shaderVariablesGlobal._ScreenSize = screenSize;
                            data.shaderVariablesGlobal._ScreenParams = new Vector4(screenSize.x, screenSize.y, 1 + screenSize.z, 1 + screenSize.w);
                            data.shaderVariablesGlobal._RTHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
                            data.hdCamera.UpdateGlobalMipBiasCB(ref data.shaderVariablesGlobal, 0);
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

        class RenderScreenSpaceOverlayData
        {
            public RendererListHandle rendererList;
        }

        void RenderScreenSpaceOverlayUI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
        {
            if (!HDROutputActiveForCameraType(hdCamera) && SupportedRenderingFeatures.active.rendersUIOverlay && hdCamera.isMainGameView)
            {
                using (var builder = renderGraph.AddRenderPass<RenderScreenSpaceOverlayData>("Screen Space Overlay UI", out var passData))
                {
                    builder.UseColorBuffer(colorBuffer, 0);
                    passData.rendererList = builder.UseRendererList(renderGraph.CreateUIOverlayRendererList(hdCamera.camera, UISubset.All));

                    builder.SetRenderFunc(
                        (RenderScreenSpaceOverlayData data, RenderGraphContext ctx) =>
                        {
                            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, data.rendererList);
                        });
                }
            }
        }

        class ScreenSpaceFogMultipleScatteringData
        {
            public ComputeShader multipleScatteringCompute;
            public TextureHandle colorBuffer;
            public TextureHandle opticalFogTransmittance;
            public TextureHandle colorPyramid;
            public float intensity;
            public int outputWidth;
            public int outputHeight;
            public int channel;
            public int viewCount;
        }

        void ScreenSpaceFogMultipleScattering(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle opticalFogTransmittance, TextureHandle colorPyramid, float intensity)
        {
            using (var builder = renderGraph.AddRenderPass<ScreenSpaceFogMultipleScatteringData>("Screen Space Fog Multiple Scattering", out var passData))
            {
                passData.multipleScatteringCompute = runtimeShaders.screenSpaceMultipleScatteringCS;
                passData.colorBuffer = builder.ReadWriteTexture(colorBuffer);
                passData.colorPyramid = builder.ReadTexture(colorPyramid);
                passData.opticalFogTransmittance = builder.ReadTexture(opticalFogTransmittance);
                passData.outputWidth = hdCamera.actualWidth;
                passData.outputHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.channel = LensFlareCommonSRP.IsCloudLayerOpacityNeeded(hdCamera.camera) ? 1 : 0;
                // Scale the multiplier to have consistent results between resolutions:
                float finalIntensity = intensity * Mathf.Clamp01(hdCamera.actualHeight / 1080f);
                passData.intensity = finalIntensity;
                builder.SetRenderFunc(
                    (ScreenSpaceFogMultipleScatteringData data, RenderGraphContext ctx) =>
                    {
                        // lerp the color buffer and color pyramid using fog opacity factor
                        ctx.cmd.SetComputeTextureParam(data.multipleScatteringCompute, 0, HDShaderIDs._OpticalFogTransmittance, data.opticalFogTransmittance);
                        ctx.cmd.SetComputeTextureParam(data.multipleScatteringCompute, 0, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                        ctx.cmd.SetComputeTextureParam(data.multipleScatteringCompute, 0, HDShaderIDs._Destination, data.colorBuffer);
                        ctx.cmd.SetComputeFloatParam(data.multipleScatteringCompute, HDShaderIDs._MultipleScatteringIntensity, data.intensity);
                        ctx.cmd.SetComputeFloatParam(data.multipleScatteringCompute, HDShaderIDs._OpticalFogTextureChannel, data.channel);
                        ctx.cmd.DispatchCompute(data.multipleScatteringCompute, 0, HDUtils.DivRoundUp(data.outputWidth, 8), HDUtils.DivRoundUp(data.outputHeight, 8), data.viewCount);
                    });
            }
        }

        static void UpdateOffscreenRenderingConstants(ref ShaderVariablesGlobal cb, bool enabled, float factor)
        {
            cb._OffScreenRendering = enabled ? 1u : 0u;
            cb._OffScreenDownsampleFactor = factor;
        }
    }
}
