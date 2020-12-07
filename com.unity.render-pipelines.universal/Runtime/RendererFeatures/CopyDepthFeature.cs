using System;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class CopyDepthFeature : ScriptableRendererFeature
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        // Private
        Material m_CopyDepthMaterial = null;
        CopyDepthPass m_CopyDepthPass;
        RenderTargetHandle m_ActiveCameraDepthAttachment;
        RenderTargetHandle m_CameraDepthAttachment;
        RenderTargetHandle m_DepthTexture;

        // Constants
        private const int k_DepthStencilBufferBits = 32;

        /// <inheritdoc/>
        public override void Create()
        {
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            ForwardRendererData data = ScriptableObject.CreateInstance<ForwardRendererData>();
            data.ReloadAllNullProperties();

            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyDepthPS);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
            m_DepthTexture.Init("_CameraDepthTexture");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {

        }

        /// <inheritdoc/>
        public override void Setup(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            bool isPreviewCamera = cameraData.isPreviewCamera;

            bool createDepthTexture = cameraData.requiresDepthTexture;
            createDepthTexture |= (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget);
            // Deferred renderer always need to access depth buffer.
            //createDepthTexture |= this.actualRenderingMode == RenderingMode.Deferred;

            bool createColorTexture = RequiresIntermediateColorTexture(ref cameraData);
            createColorTexture &= !isPreviewCamera;

            #if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
                createDepthTexture |= createColorTexture;
                createColorTexture = createDepthTexture;
            }
            #endif

            m_CopyDepthPass.renderPassEvent = Event;

            if (cameraData.renderType == CameraRenderType.Base)
            {
                RenderTargetHandle cameraTargetHandle = RenderTargetHandle.GetCameraTarget(cameraData.xr);
                m_ActiveCameraDepthAttachment = (createDepthTexture) ? m_CameraDepthAttachment : cameraTargetHandle;

                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                bool intermediateRenderTexture = createColorTexture || createDepthTexture;
                if (intermediateRenderTexture)
                {
                    CreateCameraRenderTarget(context, ref cameraTargetDescriptor);
                }
            }
            else
            {
                m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
            }

            m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
            renderer.EnqueuePass(m_CopyDepthPass);

            // For Base Cameras: Set the depth texture to the far Z if we do not have a depth prepass or copy depth
            // if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
            // {
            //     Shader.SetGlobalTexture(m_DepthTexture.id, SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);
            // }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_CopyDepthMaterial);
        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            //using (new ProfilingScope(cmd, Profiling.createCameraRenderTarget))
            {
                var depthDescriptor = descriptor;
                depthDescriptor.useMipMap = false;
                depthDescriptor.autoGenerateMips = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                // XRTODO: Enabled this line for non-XR pass? URP copy depth pass is already capable of handling MSAA.
                depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
#endif
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = k_DepthStencilBufferBits;
                cmd.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, depthDescriptor, FilterMode.Point);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// Checks if the pipeline needs to create a intermediate render texture.
        /// </summary>
        /// <param name="cameraData">CameraData contains all relevant render target information for the camera.</param>
        /// <seealso cref="CameraData"/>
        /// <returns>Return true if pipeline needs to render to a intermediate render texture.</returns>
        bool RequiresIntermediateColorTexture(ref CameraData cameraData)
        {
            // When rendering a camera stack we always create an intermediate render texture to composite camera results.
            // We create it upon rendering the Base camera.
            if (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget)
                return true;

            // Always force rendering into intermediate color texture if deferred rendering mode is selected.
            // Reason: without intermediate color texture, the target camera texture is y-flipped.
            // However, the target camera texture is bound during gbuffer pass and deferred pass.
            // Gbuffer pass will not be y-flipped because it is MRT (see ScriptableRenderContext implementation),
            // while deferred pass will be y-flipped, which breaks rendering.
            // This incurs an extra blit into at the end of rendering.
            //if (this.actualRenderingMode == RenderingMode.Deferred)
            //    return true;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            int msaaSamples = cameraTargetDescriptor.msaaSamples;
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isCompatibleBackbufferTextureDimension = cameraTargetDescriptor.dimension == TextureDimension.Tex2D;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && PlatformRequiresExplicitMsaaResolve();
            bool isOffscreenRender = cameraData.targetTexture != null && !isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                isCompatibleBackbufferTextureDimension = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
#endif

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve || !cameraData.isDefaultViewport;
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                !isCompatibleBackbufferTextureDimension || isCapturing || cameraData.requireSrgbConversion;
        }

        bool PlatformRequiresExplicitMsaaResolve()
        {
#if UNITY_EDITOR
            // In the editor play-mode we use a Game View Render Texture, with
            // samples count forced to 1 so we always need to do an explicit MSAA resolve.
            return true;
#else
            // On Metal/iOS the MSAA resolve is done implicitly as part of the renderpass, so we do not need an extra intermediate pass for the explicit autoresolve.
            // TODO: should also be valid on Metal MacOS/Editor, but currently not working as expected. Remove the "mobile only" requirement once trunk has a fix.
            return !SystemInfo.supportsMultisampleAutoResolve
                && !(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && Application.isMobilePlatform);
#endif
        }
    }
}

