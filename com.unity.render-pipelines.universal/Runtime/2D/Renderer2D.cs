using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Experimental.Rendering.Universal
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
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);

            m_PostProcessPasses = new PostProcessPasses(data.postProcessData, m_BlitMaterial);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            // We probably should declare these names in the base class,
            // as they must be the same across all ScriptableRenderer types for camera stacking to work.
            m_ColorTextureHandle = RTHandles.Alloc(Shader.PropertyToID("_CameraColorTexture"), "_CameraColorTexture");
            m_DepthTextureHandle = RTHandles.Alloc(Shader.PropertyToID("_CameraDepthAttachment"), "_CameraDepthAttachment");

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
            m_ColorTextureHandle.Release();
            m_DepthTextureHandle.Release();
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

                m_CreateDepthTexture = !cameraData.resolveFinalTarget && m_UseDepthStencilBuffer;

                colorTargetHandle = m_CreateColorTexture ? m_ColorTextureHandle : k_CameraTarget;
                depthTargetHandle = m_CreateDepthTexture ? m_DepthTextureHandle : colorTargetHandle;

                if (m_CreateColorTexture)
                {
                    var colorDescriptor = cameraTargetDescriptor;
                    colorDescriptor.depthBufferBits = m_CreateDepthTexture || !m_UseDepthStencilBuffer ? 0 : 32;
                    cmd.GetTemporaryRT(Shader.PropertyToID(m_ColorTextureHandle.name), colorDescriptor, colorTextureFilterMode);
                }

                if (m_CreateDepthTexture)
                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                    cmd.GetTemporaryRT(Shader.PropertyToID(m_DepthTextureHandle.name), depthDescriptor, FilterMode.Point);
                }
            }
            else    // Overlay camera
            {
                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_CreateColorTexture = true;
                m_CreateDepthTexture = true;

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
                if (ppc != null)
                {
                    if (ppc.offscreenRTSize != Vector2Int.zero)
                    {
                        ppcUsesOffscreenRT = true;

                        // Pixel Perfect Camera may request a different RT size than camera VP size.
                        // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
                        cameraTargetDescriptor.width = ppc.offscreenRTSize.x;
                        cameraTargetDescriptor.height = ppc.offscreenRTSize.y;
                    }

                    colorTextureFilterMode = ppc.finalBlitFilterMode;
                    ppcUpscaleRT = ppc.upscaleRT && ppc.isRunning;
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
            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle, depthTargetHandle);
            EnqueuePass(m_Render2DLightingPass);

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            if (stackHasPostProcess && m_PostProcessPasses.isCreated)
            {
                bool destinationIsInternalRT = lastCameraInStack && !ppcUpscaleRT && !requireFinalPostProcessPass;

                m_PostProcessPasses.Setup(cameraTargetDescriptor);

                var postProcessDestHandle = destinationIsInternalRT ? k_CameraTarget : afterPostProcessColorHandle;

                postProcessPass.Setup(
                    cameraTargetDescriptor,
                    depthTargetHandle,
                    colorGradingLutHandle,
                    requireFinalPostProcessPass,
                    destinationIsInternalRT);

                EnqueuePass(postProcessPass);
                colorTargetHandle = postProcessDestHandle;
            }

            if (ppc != null && ppc.isRunning && (ppc.cropFrameX || ppc.cropFrameY))
                EnqueuePass(m_PixelPerfectBackgroundPass);

            if (requireFinalPostProcessPass && m_PostProcessPasses.isCreated)
            {
                finalPostProcessPass.SetupFinalPass(colorTargetHandle, true);
                EnqueuePass(finalPostProcessPass);
            }
            else if (lastCameraInStack && colorTargetHandle.nameID != BuiltinRenderTextureType.CameraTarget)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, colorTargetHandle, false);
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
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_ColorTextureHandle.name));

            if (m_CreateDepthTexture)
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_DepthTextureHandle.name));
        }

        internal override void SwapColorBuffer()
        {
            throw new System.NotImplementedException();
        }
    }
}
