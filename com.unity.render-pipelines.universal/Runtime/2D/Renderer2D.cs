using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Renderer2D : ScriptableRenderer
    {
        private static readonly RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

        Render2DLightingPass m_Render2DLightingPass;
        PixelPerfectBackgroundPass m_PixelPerfectBackgroundPass;
        FinalBlitPass m_FinalBlitPass;
        Light2DCullResult m_LightCullResult;

        bool m_UseDepthStencilBuffer = true;
        bool m_UseIntermediateTextures;

        RTHandle m_ColorTextureHandle;
        RTHandle m_DepthTextureHandle;

        Material m_BlitMaterial;
        Material m_SamplingMaterial;

        Renderer2DData m_Renderer2DData;

        internal bool useIntermediateTextures => m_UseIntermediateTextures;

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
        }

        public Renderer2DData GetRenderer2DData()
        {
            return m_Renderer2DData;
        }

        void GetRenderTextures(
            ref CameraData cameraData,
            bool forceCreateColorTexture,
            FilterMode colorTextureFilterMode,
            out RTHandle colorTargetHandle,
            out RTHandle depthTargetHandle)
        {
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            // We probably should declare these names in the base class,
            // as they must be the same across all ScriptableRenderer types for camera stacking to work.
            if (m_ColorTextureHandle == null)
            {
                m_ColorTextureHandle = RTHandles.Alloc(Vector2.one,
                    depthBufferBits: DepthBits.None,
                    colorFormat: cameraTargetDescriptor.graphicsFormat,
                    filterMode: colorTextureFilterMode,
                    dimension: TextureDimension.Tex2D,
                    enableRandomWrite: false,
                    useMipMap: false,
                    autoGenerateMips: false,
                    enableMSAA: true,
                    name: "_CameraColorTexture");
            }
            if (m_DepthTextureHandle == null)
            {
                m_DepthTextureHandle = RTHandles.Alloc(Vector2.one,
                    depthBufferBits: DepthBits.Depth32,
                    filterMode: FilterMode.Point,
                    dimension: TextureDimension.Tex2D,
                    useMipMap: false,
                    autoGenerateMips: false,
                    enableMSAA: true,
                    bindTextureMS: cameraTargetDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0),
                    name: "_CameraDepthAttachment");
            }

            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_UseIntermediateTextures = forceCreateColorTexture
                    || cameraData.postProcessEnabled
                    || cameraData.isHdrEnabled
                    || cameraData.isSceneViewCamera
                    || !cameraData.isDefaultViewport
                    || cameraData.requireSrgbConversion
                    || !cameraData.resolveFinalTarget
                    || m_Renderer2DData.useCameraSortingLayerTexture
                    || !Mathf.Approximately(cameraData.renderScale, 1.0f);

                colorTargetHandle = m_UseIntermediateTextures ? m_ColorTextureHandle : k_CameraTarget;
                depthTargetHandle = m_UseIntermediateTextures ? m_DepthTextureHandle : k_CameraTarget;
            }
            else    // Overlay camera
            {
                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_UseIntermediateTextures = true;

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

            GetRenderTextures(ref cameraData, ppcUsesOffscreenRT, colorTextureFilterMode, out colorTargetHandle, out depthTargetHandle);
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

            var needsDepth = m_UseDepthStencilBuffer;
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
                    colorTargetHandle,
                    postProcessDestHandle,
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
                finalPostProcessPass.SetupFinalPass(colorTargetHandle);
                EnqueuePass(finalPostProcessPass);
            }
            else if (lastCameraInStack && colorTargetHandle.nameID != BuiltinRenderTextureType.CameraTarget)
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
    }
}
