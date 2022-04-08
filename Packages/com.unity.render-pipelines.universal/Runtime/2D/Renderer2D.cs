using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class Renderer2D : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        PixelPerfectBackgroundPass m_PixelPerfectBackgroundPass;
        FinalBlitPass m_FinalBlitPass;
        Light2DCullResult m_LightCullResult;

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Create Camera Textures");

        bool m_UseDepthStencilBuffer = true;
        bool m_CreateColorTexture;
        bool m_CreateDepthTexture;

        readonly RenderTargetHandle k_ColorTextureHandle;
        readonly RenderTargetHandle k_DepthTextureHandle;

        Material m_BlitMaterial;
        Material m_SamplingMaterial;

        Renderer2DData m_Renderer2DData;

        internal bool createColorTexture => m_CreateColorTexture;
        internal bool createDepthTexture => m_CreateDepthTexture;

        PostProcessPasses m_PostProcessPasses;
        internal ColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal PostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal PostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RenderTargetHandle afterPostProcessColorHandle { get => m_PostProcessPasses.afterPostProcessColor; }
        internal RenderTargetHandle colorGradingLutHandle { get => m_PostProcessPasses.colorGradingLut; }

        /// <inheritdoc/>
        public override int SupportedCameraStackingTypes()
        {
            return 1 << (int)CameraRenderType.Base | 1 << (int)CameraRenderType.Overlay;
        }

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.samplingShader);

            m_Render2DLightingPass = new Render2DLightingPass(data, m_BlitMaterial, m_SamplingMaterial);
            // we should determine why clearing the camera target is set so late in the events... sounds like it could be earlier
            m_PixelPerfectBackgroundPass = new PixelPerfectBackgroundPass(RenderPassEvent.AfterRenderingTransparents);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);


            m_PostProcessPasses = new PostProcessPasses(data.postProcessData, m_BlitMaterial);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            // We probably should declare these names in the base class,
            // as they must be the same across all ScriptableRenderer types for camera stacking to work.
            k_ColorTextureHandle.Init("_CameraColorTexture");
            k_DepthTextureHandle.Init("_CameraDepthAttachment");

            m_Renderer2DData = data;

            supportedRenderingFeatures = new RenderingFeatures();

            m_LightCullResult = new Light2DCullResult();
            m_Renderer2DData.lightCullResult = m_LightCullResult;
        }

        protected override void Dispose(bool disposing)
        {
            m_PostProcessPasses.Dispose();
        }

        public Renderer2DData GetRenderer2DData()
        {
            return m_Renderer2DData;
        }

        void CreateRenderTextures(
            ref CameraData cameraData,
            bool forceCreateColorTexture,
            FilterMode colorTextureFilterMode,
            CommandBuffer cmd,
            out RenderTargetHandle colorTargetHandle,
            out RenderTargetHandle depthTargetHandle)
        {
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_CreateColorTexture = forceCreateColorTexture
                    || cameraData.postProcessEnabled
                    || cameraData.isHdrEnabled
                    || cameraData.isSceneViewCamera
                    || !cameraData.isDefaultViewport
                    || cameraData.requireSrgbConversion
                    || !cameraData.resolveFinalTarget
                    || m_Renderer2DData.useCameraSortingLayerTexture
                    || !Mathf.Approximately(cameraData.renderScale, 1.0f);

                m_CreateDepthTexture = !cameraData.resolveFinalTarget && m_UseDepthStencilBuffer;

                colorTargetHandle = m_CreateColorTexture ? k_ColorTextureHandle : RenderTargetHandle.CameraTarget;
                depthTargetHandle = m_CreateDepthTexture ? k_DepthTextureHandle : colorTargetHandle;

                if (m_CreateColorTexture)
                {
                    var colorDescriptor = cameraTargetDescriptor;
                    colorDescriptor.depthBufferBits = m_CreateDepthTexture || !m_UseDepthStencilBuffer ? 0 : 32;
                    cmd.GetTemporaryRT(k_ColorTextureHandle.id, colorDescriptor, colorTextureFilterMode);
                }

                if (m_CreateDepthTexture)
                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                    cmd.GetTemporaryRT(k_DepthTextureHandle.id, depthDescriptor, FilterMode.Point);
                }
            }
            else    // Overlay camera
            {
                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_CreateColorTexture = true;
                m_CreateDepthTexture = true;

                colorTargetHandle = k_ColorTextureHandle;
                depthTargetHandle = k_DepthTextureHandle;
            }
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            bool stackHasPostProcess = renderingData.postProcessingEnabled;
            bool lastCameraInStack = cameraData.resolveFinalTarget;
            var colorTextureFilterMode = FilterMode.Bilinear;

            PixelPerfectCamera ppc = null;
            bool ppcUsesOffscreenRT = false;
            bool ppcUpscaleRT = false;

            bool savedIsOrthographic = renderingData.cameraData.camera.orthographic;
            float savedOrthographicSize = renderingData.cameraData.camera.orthographicSize;

            if (DebugHandler != null)
            {
#if UNITY_EDITOR
                UnityEditorInternal.SpriteMaskUtility.EnableDebugMode(DebugHandler.DebugDisplaySettings.MaterialSettings.DebugMaterialModeData == DebugMaterialMode.SpriteMask);
#endif
                if (DebugHandler.AreAnySettingsActive)
                {
                    stackHasPostProcess = stackHasPostProcess && DebugHandler.IsPostProcessingAllowed;
                }
                DebugHandler.Setup(context, ref cameraData);
            }

#if UNITY_EDITOR
            // The scene view camera cannot be uninitialized or skybox when using the 2D renderer.
            if (cameraData.cameraType == CameraType.SceneView)
            {
                renderingData.cameraData.camera.clearFlags = CameraClearFlags.SolidColor;
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
                    }

                    renderingData.cameraData.camera.orthographic = true;
                    renderingData.cameraData.camera.orthographicSize = ppc.orthographicSize;

                    colorTextureFilterMode = ppc.finalBlitFilterMode;
                    ppcUpscaleRT = ppc.gridSnapping == PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
                }
            }

            RenderTargetHandle colorTargetHandle;
            RenderTargetHandle depthTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CreateRenderTextures(ref cameraData, ppcUsesOffscreenRT, colorTextureFilterMode, cmd,
                    out colorTargetHandle, out depthTargetHandle);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(colorTargetHandle.Identifier(), depthTargetHandle.Identifier());

            // Add passes from Renderer Features. - NOTE: This should be reexamined in the future. Please see feedback from this PR https://github.com/Unity-Technologies/Graphics/pull/3147/files
            isCameraColorTargetValid = true;    // This is to make it possible to call ScriptableRenderer.cameraColorTarget in the custom passes.
            AddRenderPasses(ref renderingData);
            isCameraColorTargetValid = false;

            // We generate color LUT in the base camera only. This allows us to not break render pass execution for overlay cameras.
            if (stackHasPostProcess && cameraData.renderType == CameraRenderType.Base && m_PostProcessPasses.isCreated)
            {
                colorGradingLutPass.Setup(colorGradingLutHandle);
                EnqueuePass(colorGradingLutPass);
            }

            var needsDepth = m_CreateDepthTexture || (!m_CreateColorTexture && m_UseDepthStencilBuffer);
            m_Render2DLightingPass.Setup(needsDepth);
            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle.Identifier(), depthTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing) != null;

            if (stackHasPostProcess && m_PostProcessPasses.isCreated)
            {
                RenderTargetHandle postProcessDestHandle =
                    lastCameraInStack && !ppcUpscaleRT && !requireFinalPostProcessPass ? RenderTargetHandle.CameraTarget : afterPostProcessColorHandle;

                postProcessPass.Setup(
                    cameraTargetDescriptor,
                    colorTargetHandle,
                    postProcessDestHandle,
                    depthTargetHandle,
                    colorGradingLutHandle,
                    requireFinalPostProcessPass,
                    postProcessDestHandle == RenderTargetHandle.CameraTarget,
                    hasPassesAfterPostProcessing);

                EnqueuePass(postProcessPass);
                colorTargetHandle = postProcessDestHandle;
            }

            if (ppc != null && ppc.enabled && (ppc.cropFrame == PixelPerfectCamera.CropFrame.Pillarbox || ppc.cropFrame == PixelPerfectCamera.CropFrame.Letterbox || ppc.cropFrame == PixelPerfectCamera.CropFrame.Windowbox || ppc.cropFrame == PixelPerfectCamera.CropFrame.StretchFill))
            {
                m_PixelPerfectBackgroundPass.Setup(savedIsOrthographic, savedOrthographicSize);
                EnqueuePass(m_PixelPerfectBackgroundPass);
            }

            if (requireFinalPostProcessPass && m_PostProcessPasses.isCreated)
            {
                finalPostProcessPass.SetupFinalPass(colorTargetHandle, hasPassesAfterPostProcessing);
                EnqueuePass(finalPostProcessPass);
            }
            else if (lastCameraInStack && colorTargetHandle != RenderTargetHandle.CameraTarget)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, colorTargetHandle);
                EnqueuePass(m_FinalBlitPass);
            }
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
            m_LightCullResult.SetupCulling(ref cullingParameters, cameraData.camera);
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_CreateColorTexture)
                cmd.ReleaseTemporaryRT(k_ColorTextureHandle.id);

            if (m_CreateDepthTexture)
                cmd.ReleaseTemporaryRT(k_DepthTextureHandle.id);
        }
    }
}
