using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        private static RTHandle m_RenderGraphCameraColorHandle;
        private static RTHandle m_RenderGraphCameraDepthHandle;

        static public TextureHandle m_ActiveRenderGraphColor;
        static public TextureHandle m_ActiveRenderGraphDepth;
        public bool m_TargetIsBackbuffer;

        public class RenderGraphFrameResources
        {
            // backbuffer
            public TextureHandle backBufferColor;
            //public TextureHandle backBufferDepth;

            // intermediate camera targets
            public TextureHandle cameraColor;
            public TextureHandle cameraDepth;

            public TextureHandle mainShadowsTexture;
            public TextureHandle additionalShadowsTexture;

            // gbuffer targets

            public TextureHandle[] gbuffer;

            // camear opaque/depth/normal
            public TextureHandle cameraOpaqueTexture;
            public TextureHandle cameraDepthTexture;
            public TextureHandle cameraNormalsTexture;

            // motion vector
            public TextureHandle motionVectorColor;
            public TextureHandle motionVectorDepth;
        };
        public RenderGraphFrameResources frameResources = new RenderGraphFrameResources();

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandle?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
        }

        internal static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear)
        {
            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.dimension = desc.dimension;
            rgDesc.clearBuffer = clear;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.colorFormat = desc.graphicsFormat;
            rgDesc.depthBufferBits = (DepthBits)desc.depthBufferBits;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.name = name;
            rgDesc.enableRandomWrite = false;

            return renderGraph.CreateTexture(rgDesc);
        }

        bool RequiresColorAndDepthTextures(out bool createColorTexture, out bool createDepthTexture, ref RenderingData renderingData, RenderPassInputSummary renderPassInputs )
        {
            bool isPreviewCamera = renderingData.cameraData.isPreviewCamera;
            bool requiresDepthTexture = renderingData.cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;
#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#else
            bool isGizmosEnabled = false;
#endif
            bool requiresDepthPrepass = (requiresDepthTexture/* || cameraHasPostProcessingWithDepth*/) && !CanCopyDepth(ref renderingData.cameraData);
            requiresDepthPrepass |= renderingData.cameraData.isSceneViewCamera;
            requiresDepthPrepass |= isGizmosEnabled;
            requiresDepthPrepass |= isPreviewCamera;
            requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
            requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;

            createColorTexture = (rendererFeatures.Count != 0 && m_IntermediateTextureMode == IntermediateTextureMode.Always) && !isPreviewCamera;
            createColorTexture |= RequiresIntermediateColorTexture(ref renderingData.cameraData);
            createColorTexture &= !isPreviewCamera;

            createDepthTexture = (requiresDepthTexture/* || cameraHasPostProcessingWithDepth*/) && !requiresDepthPrepass;
            createDepthTexture |= !renderingData.cameraData.resolveFinalTarget;
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= (this.renderingModeActual == RenderingMode.Deferred && !useRenderPassEnabled);
            // Some render cases (e.g. Material previews) have shown we need to create a depth texture when we're forcing a prepass.
            createDepthTexture |= m_DepthPrimingMode == DepthPrimingMode.Forced;
#if ENABLE_VR && ENABLE_XR_MODULE
            // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
            if (renderingData.cameraData.xr.enabled)
                createColorTexture |= createDepthTexture;
#endif
#if UNITY_ANDROID || UNITY_WEBGL
            // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
            // in such case we create a color texture for it too.
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
                createColorTexture |= createDepthTexture;
#endif
            bool depthPriming = IsDepthPrimingEnabled();
            depthPriming &= requiresDepthPrepass && (createDepthTexture || createColorTexture) && m_RenderingMode == RenderingMode.Forward && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth);

            if (useRenderPassEnabled || depthPriming)
                createColorTexture |= createDepthTexture;

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
            {
                //Scene filtering redraws the objects on top of the resulting frame. It has to draw directly to the sceneview buffer.
                bool sceneViewFilterEnabled = renderingData.cameraData.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
                bool intermediateRenderTexture = (createColorTexture || createDepthTexture) && !sceneViewFilterEnabled;
                createDepthTexture = intermediateRenderTexture;

            }
            return createColorTexture || createDepthTexture;
        }
        void CreateRenderGraphCameraRenderTargets(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            RenderGraph renderGraph = renderingData.renderGraph;

            RenderTargetIdentifier targetId = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                targetId = cameraData.xr.renderTarget;
#endif
            frameResources.backBufferColor = renderGraph.ImportBackbuffer(targetId);
            //frameResources.backBufferDepth = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.Depth);

            #region Intermediate Camera Target
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            var createColorTexture = false;
            var createDepthTexture = false;
            // We configure this for the first camera of the stack and
            if (cameraData.renderType == CameraRenderType.Base)
                m_UseIntermediateTexture = RequiresColorAndDepthTextures(out createColorTexture, out createDepthTexture, ref renderingData, renderPassInputs);

            if (createColorTexture)
            {
                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandle, cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraTargetAttachment");
            }

            if (createDepthTexture)
            {
                var depthDescriptor = cameraData.cameraTargetDescriptor;
                depthDescriptor.useMipMap = false;
                depthDescriptor.autoGenerateMips = false;
                depthDescriptor.bindMS = false;

                bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);

                if (hasMSAA)
                    depthDescriptor.bindMS = true;

                // binding MS surfaces is not supported by the GLES backend, and it won't be fixed after investigating
                // the high performance impact of potential fixes, which would make it more expensive than depth prepass (fogbugz 1339401 for more info)
                if (IsGLESDevice())
                    depthDescriptor.bindMS = false;

                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraDepthHandle, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
            }

            if (m_UseIntermediateTexture)
            {
                frameResources.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle);
                frameResources.cameraColor = renderGraph.ImportTexture(m_RenderGraphCameraColorHandle);

                if (frameResources.cameraColor.IsValid())
                {
                    m_ActiveRenderGraphColor = frameResources.cameraColor;
                    m_TargetIsBackbuffer = false;
                }

                if (frameResources.cameraDepth.IsValid())
                {
                    m_ActiveRenderGraphDepth = frameResources.cameraDepth;
                    // TODO RENDERGRAPH: investigate how to set m_TargetIsBackbuffer if there is a mismatch with intermediate color + BB depth for example
                }
            }
            else
            {
                m_ActiveRenderGraphColor = frameResources.backBufferColor;
                m_ActiveRenderGraphDepth = frameResources.backBufferColor;
                m_TargetIsBackbuffer = true;
            }
            #endregion

            #region MotionVector Color/Depth
            // TODO RENDERGRAPH: check the condition for create motionvector frame resources
            //if (renderPassInputs.requiresMotionVectors && !cameraData.xr.enabled)
            {
                SupportedRenderingFeatures.active.motionVectors = true; // hack for enabling UI

                var colorDesc = cameraData.cameraTargetDescriptor;
                colorDesc.graphicsFormat = GraphicsFormat.R16G16_SFloat;
                colorDesc.depthBufferBits = (int)DepthBits.None;
                frameResources.motionVectorColor = CreateRenderGraphTexture(renderGraph, colorDesc, "_MotionVectorTexture", true);

                var depthDescriptor = cameraData.cameraTargetDescriptor;
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                RenderingUtils.ReAllocateIfNeeded(ref m_MotionVectorDepth, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_MotionVectorDepthTexture");
                frameResources.motionVectorDepth = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_MotionVectorDepthTexture", true);
            }
            #endregion

        }

        protected override void RecordRenderGraphInternal(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            useRenderPassEnabled = false;

            CreateRenderGraphCameraRenderTargets(context, ref renderingData);

            OnBeforeRendering(context, ref renderingData);

            OnMainRendering(context, ref renderingData);

            OnAfterRendering(context, ref renderingData);
        }

        protected override void FinishRenderGraphRenderingInternal(ScriptableRenderContext context, RenderingData renderingData)
        {
            if (this.renderingModeActual == RenderingMode.Deferred)
                m_DeferredPass.OnCameraCleanup(renderingData.commandBuffer);

            m_CopyDepthPass.OnCameraCleanup(renderingData.commandBuffer);
        }

        // TODO RENDERGRAPH: do this properly
        private static bool m_UseIntermediateTexture = false;

        private void OnBeforeRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            bool renderShadows = false;

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                frameResources.mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderingData.renderGraph, ref renderingData);
            }

            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                frameResources.additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderingData.renderGraph, ref renderingData);
            }

            // The camera need to be setup again after the shadows since those passes override some settings
            // TODO RENDERGRAPH: move the setup code into the shadow passes
            if (renderShadows)
                SetupRenderGraphCameraProperties(ref renderingData);
        }

        private void OnMainRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (renderingData.cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            if (clearFlags != RTClearFlags.None)
                ClearTargetsPass.Render(renderingData.renderGraph, this, clearFlags, renderingData.cameraData.backgroundColor);

            RecordCustomRenderGraphPasses(context, ref renderingData, RenderPassEvent.BeforeRenderingPrePasses);

            // sort out:
            // - cameraData.target texture
            // - offscreen depth camera
            // - current target to figure out if backbuffer or intermediate texture rendering
            // - figure out if intermediate textures should ben created
            // - depth priming?


            // TODO RENDERGRAPH: check require DepthPrepass
            //if (requiresDepthPrepass)
            {
                // TODO RENDERGRAPH: check requires normal
                //if (renderPassInputs.requiresNormalsTexture))
                {
                    m_DepthNormalPrepass.Render(out frameResources.cameraDepthTexture, out frameResources.cameraNormalsTexture, ref renderingData);
                }
                //else
                {
                    m_DepthPrepass.Render(out frameResources.cameraDepthTexture, ref renderingData);
                }
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (renderingData.cameraData.xr.hasValidOcclusionMesh)
            m_XROcclusionMeshPass.Render(frameResources.cameraDepth, ref renderingData);
#endif

            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);
                if (m_DeferredLights != null)
                {
                    m_DeferredLights.UseRenderPass = false;
                    m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                    m_DeferredLights.IsOverlay = renderingData.cameraData.renderType == CameraRenderType.Overlay;
                }

                m_GBufferPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData, ref frameResources);
                m_GBufferCopyDepthPass.Render(out frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData);
                m_DeferredPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.gbuffer, ref renderingData);
                m_RenderOpaqueForwardOnlyPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }
            else
            {
                m_RenderOpaqueForwardPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }
            // RunCustomPasses(RenderPassEvent.AfterOpaque);

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                m_DrawSkyboxPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData);
            //if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Render(out frameResources.cameraDepthTexture, in m_ActiveRenderGraphDepth, ref renderingData);
            }

            //if (requiresColorCopyPass)
            {
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                m_CopyColorPass.Render(out frameResources.cameraOpaqueTexture, in m_ActiveRenderGraphColor, downsamplingMethod, ref renderingData);
            }

            // if (renderPassInputs.requiresMotionVectors && !cameraData.xr.enabled)
            {
                var data = MotionVectorRendering.instance.GetMotionDataForCamera(renderingData.cameraData.camera, renderingData.cameraData);
                m_MotionVectorPass.Render(in frameResources.motionVectorColor, in frameResources.motionVectorDepth, data, ref renderingData);
            }

            // TODO RENDERGRAPH: bind _CameraOpaqueTexture, _CameraDepthTexture in transparent pass?

            m_RenderTransparentForwardPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);

            m_OnRenderObjectCallbackPass.Render(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData);
        }

        private void OnAfterRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, GizmoSubset.PreImageEffects, ref renderingData);

            // TODO RENDERGRAPH: postprocessing passes

            if (drawGizmos)
                DrawRenderGraphGizmos(m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth,GizmoSubset.PostImageEffects, ref renderingData);

            if (!m_TargetIsBackbuffer && renderingData.cameraData.resolveFinalTarget)
                m_FinalBlitPass.Render(ref renderingData, frameResources.cameraColor, frameResources.backBufferColor);
        }

    }


    class ClearTargetsPass
    {
        static private ProfilingSampler s_ClearProfilingSampler = new ProfilingSampler("Clear Targets");

        public class PassData
        {
            public TextureHandle color;
            public TextureHandle depth;

            public RTClearFlags clearFlags;
            public Color clearColor;
        }

        static public PassData Render(RenderGraph graph, UniversalRenderer renderer, RTClearFlags clearFlags, Color clearColor)
        {
            using (var builder = graph.AddRenderPass<PassData>("Clear Targets Pass", out var passData, s_ClearProfilingSampler))
            {
                passData.color = builder.UseColorBuffer(UniversalRenderer.m_ActiveRenderGraphColor, 0);
                passData.depth = builder.UseDepthBuffer(UniversalRenderer.m_ActiveRenderGraphDepth, DepthAccess.Write);
                passData.clearFlags = clearFlags;
                passData.clearColor = clearColor;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });

                return passData;
            }
        }
    }

}
