#if URP_COMPATIBILITY_MODE
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal sealed partial class Renderer2D : ScriptableRenderer
    {
        CompatibilityMode.Render2DLightingPass m_Render2DLightingPass;
        CompatibilityMode.PixelPerfectBackgroundPass m_PixelPerfectBackgroundPass;

        internal RenderTargetBufferSystem m_ColorBufferSystem;

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Create Camera Textures");

        bool m_UseDepthStencilBuffer = true;

        // We probably should declare these names in the base class,
        // as they must be the same across all ScriptableRenderer types for camera stacking to work.
        internal RTHandle m_ColorTextureHandle;
        internal RTHandle m_DepthTextureHandle;

#if UNITY_EDITOR
        SetEditorTargetPass m_SetEditorTargetPass;
        internal RTHandle m_DefaultWhiteTextureHandle;
#endif

        internal bool createColorTexture => m_CreateColorTexture;
        internal bool createDepthTexture => m_CreateDepthTexture;

        // Compatibility
        CompatibilityMode.PostProcessPasses m_PostProcessPasses;
        internal ColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal CompatibilityMode.PostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal CompatibilityMode.PostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RTHandle afterPostProcessColorHandle { get => m_PostProcessPasses.afterPostProcessColor; }
        internal RTHandle colorGradingLutHandle { get => m_PostProcessPasses.colorGradingLut; }

        void InitializeCompatibilityMode(Renderer2DData data)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var renderer2DResources))
            {
                m_Render2DLightingPass = new CompatibilityMode.Render2DLightingPass(data, m_BlitMaterial, m_SamplingMaterial, renderer2DResources.fallOffLookup);
            }

            m_PixelPerfectBackgroundPass = new CompatibilityMode.PixelPerfectBackgroundPass(RenderPassEvent.AfterRenderingTransparents);

#if UNITY_EDITOR
            m_SetEditorTargetPass = new SetEditorTargetPass(RenderPassEvent.AfterRendering + 9);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_ColorBufferSystem = new RenderTargetBufferSystem("_CameraColorAttachment");

            var ppParams = CompatibilityMode.PostProcessParams.Create();
            ppParams.blitMaterial = m_BlitMaterial;
            ppParams.requestColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;

            m_PostProcessPasses = new CompatibilityMode.PostProcessPasses(data.postProcessData, ref ppParams);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;
        }

        void CleanupCompatibilityModeResources()
        {
            m_Render2DLightingPass?.Dispose();
            m_PostProcessPasses.Dispose();
            m_ColorTextureHandle?.Release();
            m_DepthTextureHandle?.Release();
            ReleaseRenderTargets();
        }

        internal override void ReleaseRenderTargets()
        {
            m_ColorBufferSystem.Dispose();
            m_PostProcessPasses.ReleaseRenderTargets();
        }

        void CreateRenderTextures(
            ref RenderPassInputSummary renderPassInputs,
            CommandBuffer cmd,
            UniversalCameraData cameraData,
            bool forceCreateColorTexture,
            FilterMode colorTextureFilterMode,
            out RTHandle colorTargetHandle,
            out RTHandle depthTargetHandle)
        {
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            var colorDescriptor = cameraTargetDescriptor;
            colorDescriptor.depthStencilFormat = GraphicsFormat.None;
            m_ColorBufferSystem.SetCameraSettings(colorDescriptor, colorTextureFilterMode);

            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_CreateColorTexture = renderPassInputs.requiresColorTexture;
                m_CreateDepthTexture = renderPassInputs.requiresDepthTexture;
                m_CreateColorTexture |= forceCreateColorTexture;

                // RTHandles do not support combining color and depth in the same texture so we create them separately
                m_CreateDepthTexture |= createColorTexture;

                if (createColorTexture)
                {
                    if (m_ColorBufferSystem.PeekBackBuffer() == null || m_ColorBufferSystem.PeekBackBuffer().nameID != BuiltinRenderTextureType.CameraTarget)
                    {
                        m_ColorTextureHandle = m_ColorBufferSystem.GetBackBuffer(cmd);
                        cmd.SetGlobalTexture("_CameraColorTexture", m_ColorTextureHandle.nameID);
                        //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
                        cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ColorTextureHandle.nameID);
                    }

                    m_ColorTextureHandle = m_ColorBufferSystem.PeekBackBuffer();
                }

                if (createDepthTexture)
                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthStencilFormat = CoreUtils.GetDefaultDepthStencilFormat();
                    if (!cameraData.resolveFinalTarget && m_UseDepthStencilBuffer)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                    RenderingUtils.ReAllocateHandleIfNeeded(ref m_DepthTextureHandle, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                }

                colorTargetHandle = createColorTexture ? m_ColorTextureHandle : k_CameraTarget;
                depthTargetHandle = createDepthTexture ? m_DepthTextureHandle : k_CameraTarget;
            }
            else    // Overlay camera
            {
                cameraData.baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraData);
                var baseRenderer = (Renderer2D)baseCameraData.scriptableRenderer;

                if (m_ColorBufferSystem != baseRenderer.m_ColorBufferSystem)
                {
                    m_ColorBufferSystem.Dispose();
                    m_ColorBufferSystem = baseRenderer.m_ColorBufferSystem;
                }

                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_CreateColorTexture = true;
                m_CreateDepthTexture = true;

                m_ColorTextureHandle = baseRenderer.m_ColorTextureHandle;
                m_DepthTextureHandle = baseRenderer.m_DepthTextureHandle;

                colorTargetHandle = m_ColorTextureHandle;
                depthTargetHandle = m_DepthTextureHandle;
            }
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            bool stackHasPostProcess = postProcessingData.isEnabled && m_PostProcessPasses.isCreated;
            bool hasPostProcess = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool lastCameraInStack = cameraData.resolveFinalTarget;
            var colorTextureFilterMode = FilterMode.Bilinear;

            PixelPerfectCamera ppc = null;
            bool ppcUsesOffscreenRT = false;
            bool ppcUpscaleRT = false;

            if (DebugHandler != null)
            {
#if UNITY_EDITOR
                UnityEditorInternal.SpriteMaskUtility.EnableDebugMode(DebugHandler.DebugDisplaySettings.materialSettings.materialDebugMode == DebugMaterialMode.SpriteMask);
#endif
                if (DebugHandler.AreAnySettingsActive)
                {
                    stackHasPostProcess = stackHasPostProcess && DebugHandler.IsPostProcessingAllowed;
                    hasPostProcess = hasPostProcess && DebugHandler.IsPostProcessingAllowed;
                }
                DebugHandler.Setup(universalRenderingData.commandBuffer, cameraData.isPreviewCamera);

                if (DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
                {
                    if (DebugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget))
                    {
                        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                        DebugHandler.ConfigureColorDescriptorForDebugScreen(ref descriptor, cameraData.pixelWidth, cameraData.pixelHeight);
                        RenderingUtils.ReAllocateHandleIfNeeded(ref DebugHandler.DebugScreenColorHandle, descriptor, name: "_DebugScreenColor");

                        RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                        DebugHandler.ConfigureDepthDescriptorForDebugScreen(ref depthDesc, CoreUtils.GetDefaultDepthStencilFormat(), cameraData.pixelWidth, cameraData.pixelHeight);
                        RenderingUtils.ReAllocateHandleIfNeeded(ref DebugHandler.DebugScreenDepthHandle, depthDesc, name: "_DebugScreenDepth");
                    }

                    if (DebugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget))
                    {
                        DebugHandler.hdrDebugViewPass.Setup(cameraData, DebugHandler.DebugDisplaySettings.lightingSettings.hdrDebugMode);
                        EnqueuePass(DebugHandler.hdrDebugViewPass);
                    }
                }
            }

#if UNITY_EDITOR
            // The scene view camera cannot be uninitialized or skybox when using the 2D renderer.
            if (cameraData.cameraType == CameraType.SceneView)
            {
                cameraData.camera.clearFlags = CameraClearFlags.SolidColor;
            }
#endif

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && lastCameraInStack)
            {
                cameraData.camera.TryGetComponent(out ppc);
                if (ppc != null && ppc.enabled)
                {
                    if (ppc.offscreenRTSize != Vector2Int.zero)
                    {
                        ppcUsesOffscreenRT = true;

                        // Pixel Perfect Camera may request a different RT size than camera VP size.
                        // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
                        cameraTargetDescriptor.width = ppc.offscreenRTSize.x;
                        cameraTargetDescriptor.height = ppc.offscreenRTSize.y;

                        // If using FullScreenRenderPass with Pixel Perfect, we need to reallocate the size of the RT used
                        var fullScreenRenderPass = activeRenderPassQueue.Find(x => x is FullScreenPassRendererFeature.FullScreenRenderPass) as FullScreenPassRendererFeature.FullScreenRenderPass;
                        fullScreenRenderPass?.ReAllocate(cameraTargetDescriptor);
                    }

                    colorTextureFilterMode = FilterMode.Point;
                    ppcUpscaleRT = ppc.gridSnapping == PixelPerfectCamera.GridSnapping.UpscaleRenderTexture || ppc.requiresUpscalePass;
                }
            }

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(cameraData);

            RTHandle colorTargetHandle;
            RTHandle depthTargetHandle;

            var cmd = universalRenderingData.commandBuffer;

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CreateRenderTextures(ref renderPassInputs, cmd, cameraData, ppcUsesOffscreenRT, colorTextureFilterMode,
                    out colorTargetHandle, out depthTargetHandle);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            ConfigureCameraTarget(colorTargetHandle, depthTargetHandle);

            if (hasPostProcess)
            {
                colorGradingLutPass.ConfigureDescriptor(in postProcessingData, out var desc, out var filterMode);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_PostProcessPasses.m_ColorGradingLut, desc, filterMode, TextureWrapMode.Clamp, name: "_InternalGradingLut");
                colorGradingLutPass.Setup(colorGradingLutHandle);
                EnqueuePass(colorGradingLutPass);
            }

            m_Render2DLightingPass.Setup(renderPassInputs.requiresDepthTexture || m_UseDepthStencilBuffer);
            // Disable obsolete warning for internal usage
#pragma warning disable CS0618
            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle, depthTargetHandle);
#pragma warning restore CS0618
            EnqueuePass(m_Render2DLightingPass);

            bool shouldRenderUI = cameraData.rendersOverlayUI;
            bool outputToHDR = cameraData.isHDROutputActive;
            if (shouldRenderUI && outputToHDR)
            {
                m_DrawOffscreenUIPass.Setup(cameraData, CoreUtils.GetDefaultDepthStencilFormat());
                EnqueuePass(m_DrawOffscreenUIPass);
            }

            // TODO: Investigate how to make FXAA work with HDR output.
            bool isFXAAEnabled = cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing && !outputToHDR;

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && isFXAAEnabled;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;
            bool needsColorEncoding = DebugHandler == null || !DebugHandler.HDRDebugViewIsActive(cameraData.resolveFinalTarget);

            // Don't resolve during post processing if there are passes after or pixel perfect camera is used
            bool pixelPerfectCameraEnabled = ppc != null && ppc.enabled;
            bool hasCaptureActions = cameraData.captureActions != null && lastCameraInStack;
            bool resolvePostProcessingToCameraTarget = lastCameraInStack && !hasCaptureActions && !hasPassesAfterPostProcessing && !requireFinalPostProcessPass && !pixelPerfectCameraEnabled;
            bool doSRGBEncoding = resolvePostProcessingToCameraTarget && needsColorEncoding;

            if (hasPostProcess)
            {
                var desc = CompatibilityMode.PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_PostProcessPasses.m_AfterPostProcessColor, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_AfterPostProcessTexture");

                postProcessPass.Setup(
                    cameraTargetDescriptor,
                    colorTargetHandle,
                    resolvePostProcessingToCameraTarget,
                    depthTargetHandle,
                    colorGradingLutHandle,
                    null,
                    requireFinalPostProcessPass,
                    doSRGBEncoding);

                EnqueuePass(postProcessPass);
            }

            RTHandle finalTargetHandle = colorTargetHandle;

            if (pixelPerfectCameraEnabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None)
            {
                EnqueuePass(m_PixelPerfectBackgroundPass);

                // Queue PixelPerfect UpscalePass. Only used when using the Stretch Fill option
                if (ppc.requiresUpscalePass)
                {
                    int upscaleWidth = ppc.refResolutionX * ppc.pixelRatio;
                    int upscaleHeight = ppc.refResolutionY * ppc.pixelRatio;

                    m_UpscalePass.Setup(colorTargetHandle, upscaleWidth, upscaleHeight, ppc.finalBlitFilterMode, cameraData.cameraTargetDescriptor, out finalTargetHandle);
                    EnqueuePass(m_UpscalePass);
                }
            }

            if (requireFinalPostProcessPass)
            {
                finalPostProcessPass.SetupFinalPass(finalTargetHandle, hasPassesAfterPostProcessing, needsColorEncoding);
                EnqueuePass(finalPostProcessPass);
            }

            // If post-processing then we already resolved to camera target while doing post.
            // Also only do final blit if camera is not rendering to RT.
            bool cameraTargetResolved =
                   // final PP always blit to camera target
                   requireFinalPostProcessPass ||
                   // no final PP but we have PP stack. In that case it blit unless there are render pass after PP or pixel perfect camera is used
                   (hasPostProcess && !hasPassesAfterPostProcessing && !hasCaptureActions && !pixelPerfectCameraEnabled) ||
                   // offscreen camera rendering to a texture, we don't need a blit pass to resolve to screen
                   colorTargetHandle.nameID == k_CameraTarget.nameID;

            if (!cameraTargetResolved)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, finalTargetHandle);
                EnqueuePass(m_FinalBlitPass);
            }

            // We can explicitely render the overlay UI from URP when HDR output is not enabled.
            // SupportedRenderingFeatures.active.rendersUIOverlay should also be set to true.
            if (shouldRenderUI && cameraData.isLastBaseCamera && !outputToHDR)
            {
                EnqueuePass(m_DrawOverlayUIPass);
            }

            // The editor scene view still relies on some builtin passes (i.e. drawing the scene grid). The builtin
            // passes are not explicitly setting RTs and rely on the last active render target being set.
            // TODO: this will go away once we remove the builtin dependencies and implement the grid in SRP.
#if UNITY_EDITOR
            bool isSceneViewOrPreviewCamera = cameraData.isSceneViewCamera || cameraData.isPreviewCamera;
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
            if (isSceneViewOrPreviewCamera || (isGizmosEnabled && lastCameraInStack))
            {
                EnqueuePass(m_SetEditorTargetPass);
            }
#endif
        }

        internal override void SwapColorBuffer(CommandBuffer cmd)
        {
            m_ColorBufferSystem.Swap();

            // Disable obsolete warning for internal usage
#pragma warning disable CS0618
            //Check if we are using the depth that is attached to color buffer
            if (m_DepthTextureHandle.nameID != BuiltinRenderTextureType.CameraTarget)
                ConfigureCameraTarget(m_ColorBufferSystem.GetBackBuffer(cmd), m_DepthTextureHandle);
            else
                ConfigureCameraColorTarget(m_ColorBufferSystem.GetBackBuffer(cmd));
#pragma warning restore CS0618

            m_ColorTextureHandle = m_ColorBufferSystem.GetBackBuffer(cmd);
            cmd.SetGlobalTexture("_CameraColorTexture", m_ColorTextureHandle.nameID);
            //Set _AfterPostProcessTexture, users might still rely on this although it is now always the cameratarget due to swapbuffer
            cmd.SetGlobalTexture("_AfterPostProcessTexture", m_ColorTextureHandle.nameID);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        internal override RTHandle GetCameraColorFrontBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetFrontBuffer(cmd);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsoleteFrom2023_3)]
        internal override RTHandle GetCameraColorBackBuffer(CommandBuffer cmd)
        {
            return m_ColorBufferSystem.GetBackBuffer(cmd);
        }

        internal override void EnableSwapBufferMSAA(bool enable)
        {
            m_ColorBufferSystem.EnableMSAA(enable);
        }
    }

#if UNITY_EDITOR
    internal class SetEditorTargetPass : ScriptableRenderPass
    {
        public SetEditorTargetPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete + " #from(6000.0)")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            renderingData.commandBuffer.SetRenderTarget(k_CameraTarget,
                       RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, // color
                       RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare); // depth
        }
    }
#endif
}
#endif // URP_COMPATIBILITY_MODE
