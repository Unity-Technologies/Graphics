using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class Renderer2D : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        PixelPerfectBackgroundPass m_PixelPerfectBackgroundPass;
        UpscalePass m_UpscalePass;
        FinalBlitPass m_FinalBlitPass;
        Light2DCullResult m_LightCullResult;

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Create Camera Textures");

        bool m_UseDepthStencilBuffer = true;
        bool m_CreateColorTexture;
        bool m_CreateDepthTexture;

        // We probably should declare these names in the base class,
        // as they must be the same across all ScriptableRenderer types for camera stacking to work.
        RTHandle m_ColorTextureHandle;
        RTHandle m_DepthTextureHandle;

        Material m_BlitMaterial;
        Material m_SamplingMaterial;

        Renderer2DData m_Renderer2DData;

        internal bool createColorTexture => m_CreateColorTexture;
        internal bool createDepthTexture => m_CreateDepthTexture;

        PostProcessPasses m_PostProcessPasses;
        internal ColorGradingLutPass colorGradingLutPass { get => m_PostProcessPasses.colorGradingLutPass; }
        internal PostProcessPass postProcessPass { get => m_PostProcessPasses.postProcessPass; }
        internal PostProcessPass finalPostProcessPass { get => m_PostProcessPasses.finalPostProcessPass; }
        internal RTHandle afterPostProcessColorHandle { get => m_PostProcessPasses.afterPostProcessColor; }
        internal RTHandle colorGradingLutHandle { get => m_PostProcessPasses.colorGradingLut; }

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.samplingShader);

            m_Render2DLightingPass = new Render2DLightingPass(data, m_BlitMaterial, m_SamplingMaterial);
            // we should determine why clearing the camera target is set so late in the events... sounds like it could be earlier
            m_PixelPerfectBackgroundPass = new PixelPerfectBackgroundPass(RenderPassEvent.AfterRenderingTransparents);
            m_UpscalePass = new UpscalePass(RenderPassEvent.AfterRenderingPostProcessing);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);


            m_PostProcessPasses = new PostProcessPasses(data.postProcessData, m_BlitMaterial);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            m_Renderer2DData = data;

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };

            m_LightCullResult = new Light2DCullResult();
            m_Renderer2DData.lightCullResult = m_LightCullResult;
        }

        protected override void Dispose(bool disposing)
        {
            m_PostProcessPasses.Dispose();
            m_ColorTextureHandle?.Release();
            m_DepthTextureHandle?.Release();
            m_UpscalePass.Dispose();
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
            out RTHandle colorTargetHandle,
            out RTHandle depthTargetHandle)
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

                m_CreateDepthTexture = (!cameraData.resolveFinalTarget && m_UseDepthStencilBuffer) || createColorTexture;

                if (m_CreateColorTexture)
                {
                    var colorDescriptor = cameraTargetDescriptor;
                    colorDescriptor.depthBufferBits = 0;
                    RenderingUtils.ReAllocateIfNeeded(ref m_ColorTextureHandle, colorDescriptor, colorTextureFilterMode, wrapMode: TextureWrapMode.Clamp, name: "_CameraColorTexture");
                }

                if (m_CreateDepthTexture)
                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    if (!cameraData.resolveFinalTarget && m_UseDepthStencilBuffer)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                    RenderingUtils.ReAllocateIfNeeded(ref m_DepthTextureHandle, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                }

                colorTargetHandle = m_CreateColorTexture ? m_ColorTextureHandle : k_CameraTarget;
                depthTargetHandle = m_CreateDepthTexture ? m_DepthTextureHandle : k_CameraTarget;
            }
            else    // Overlay camera
            {
                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_CreateColorTexture = true;
                m_CreateDepthTexture = true;

                m_ColorTextureHandle?.Release();
                m_DepthTextureHandle?.Release();
                m_ColorTextureHandle = RTHandles.Alloc("_CameraColorTexture", name: "_CameraColorTexture");
                m_DepthTextureHandle = RTHandles.Alloc("_CameraDepthAttachment", name: "_CameraDepthAttachment");

                colorTargetHandle = m_ColorTextureHandle;
                depthTargetHandle = m_DepthTextureHandle;
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
                UnityEditorInternal.SpriteMaskUtility.EnableDebugMode(DebugHandler.DebugDisplaySettings.materialSettings.materialDebugMode == DebugMaterialMode.SpriteMask);
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

                    colorTextureFilterMode = FilterMode.Point;
                    ppcUpscaleRT = ppc.gridSnapping == PixelPerfectCamera.GridSnapping.UpscaleRenderTexture || ppc.requiresUpscalePass;
                }
            }

            RTHandle colorTargetHandle;
            RTHandle depthTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CreateRenderTextures(ref cameraData, ppcUsesOffscreenRT, colorTextureFilterMode, cmd,
                    out colorTargetHandle, out depthTargetHandle);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(colorTargetHandle, depthTargetHandle);

            // We generate color LUT in the base camera only. This allows us to not break render pass execution for overlay cameras.
            if (stackHasPostProcess && cameraData.renderType == CameraRenderType.Base && m_PostProcessPasses.isCreated)
            {
                colorGradingLutPass.ConfigureDescriptor(in renderingData.postProcessingData, out var desc, out var filterMode);
                RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_ColorGradingLut, desc, filterMode, TextureWrapMode.Clamp, name: "_InternalGradingLut");
                colorGradingLutPass.Setup(colorGradingLutHandle);
                EnqueuePass(colorGradingLutPass);
            }

            var needsDepth = m_CreateDepthTexture || m_UseDepthStencilBuffer;
            m_Render2DLightingPass.Setup(needsDepth);
            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle, depthTargetHandle);
            EnqueuePass(m_Render2DLightingPass);

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            if (stackHasPostProcess && m_PostProcessPasses.isCreated)
            {
                RTHandle postProcessDestHandle;
                if (lastCameraInStack && !ppcUpscaleRT && !requireFinalPostProcessPass)
                {
                    postProcessDestHandle = k_CameraTarget;
                }
                else
                {
                    var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, cameraTargetDescriptor.graphicsFormat, DepthBits.None);
                    RenderingUtils.ReAllocateIfNeeded(ref m_PostProcessPasses.m_AfterPostProcessColor, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_AfterPostProcessTexture");
                    postProcessDestHandle = afterPostProcessColorHandle;
                }

                postProcessPass.Setup(
                    cameraTargetDescriptor,
                    colorTargetHandle,
                    postProcessDestHandle,
                    depthTargetHandle,
                    colorGradingLutHandle,
                    requireFinalPostProcessPass,
                    postProcessDestHandle.nameID == k_CameraTarget.nameID);

                EnqueuePass(postProcessPass);
                colorTargetHandle = postProcessDestHandle;
            }

            RTHandle finalTargetHandle = colorTargetHandle;

            if (ppc != null && ppc.enabled && ppc.cropFrame != PixelPerfectCamera.CropFrame.None)
            {
                m_PixelPerfectBackgroundPass.Setup(savedIsOrthographic, savedOrthographicSize);
                EnqueuePass(m_PixelPerfectBackgroundPass);

                // Queue PixelPerfect UpscalePass. Only used when using the Stretch Fill option
                if (ppc.requiresUpscalePass)
                {
                    int upscaleWidth = ppc.refResolutionX * ppc.pixelRatio;
                    int upscaleHeight = ppc.refResolutionY * ppc.pixelRatio;

                    m_UpscalePass.Setup(colorTargetHandle, upscaleWidth, upscaleHeight, ppc.finalBlitFilterMode, ref renderingData, out finalTargetHandle);
                    EnqueuePass(m_UpscalePass);
                }
            }

            if (requireFinalPostProcessPass && m_PostProcessPasses.isCreated)
            {
                finalPostProcessPass.SetupFinalPass(finalTargetHandle);
                EnqueuePass(finalPostProcessPass);
            }
            else if (lastCameraInStack && finalTargetHandle != k_CameraTarget)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, finalTargetHandle);
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
    }
}
