using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        private static RTHandle m_RenderGraphCameraColorHandle;
        private static RTHandle m_RenderGraphCameraDepthHandle;

        /// <summary>
        /// Current active color target. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        static public TextureHandle m_ActiveRenderGraphColor;
        /// <summary>
        /// Current active depth target. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        static public TextureHandle m_ActiveRenderGraphDepth;
        /// <summary>
        /// True if the current active target is the backbuffer. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        static public bool m_TargetIsBackbuffer;

        // rendering layers
        private bool m_RequiresRenderingLayer;
        private RenderingLayerUtils.Event m_RenderingLayersEvent;
        private RenderingLayerUtils.MaskSize m_RenderingLayersMaskSize;
        private bool m_RenderingLayerProvidesRenderObjectPass;
        private bool m_RenderingLayerProvidesByDepthNormalPass;
        private string m_RenderingLayersTextureName;

        internal class RenderGraphFrameResources
        {
            // backbuffer
            internal TextureHandle backBufferColor;
            //internal TextureHandle backBufferDepth;

            // intermediate camera targets
            internal TextureHandle cameraColor;
            internal TextureHandle cameraDepth;

            internal TextureHandle mainShadowsTexture;
            internal TextureHandle additionalShadowsTexture;

            // gbuffer targets

            internal TextureHandle[] gbuffer;

            // camear opaque/depth/normal
            internal TextureHandle cameraOpaqueTexture;
            internal TextureHandle cameraDepthTexture;
            internal TextureHandle cameraNormalsTexture;

            // motion vector
            internal TextureHandle motionVectorColor;
            internal TextureHandle motionVectorDepth;

            // postFx
            internal TextureHandle internalColorLut;

            // rendering layers
            internal TextureHandle renderingLayersTexture;
            internal TextureHandle afterPostProcessColor;
        };
        internal RenderGraphFrameResources frameResources = new RenderGraphFrameResources();

        private void CleanupRenderGraphResources()
        {
            m_RenderGraphCameraColorHandle?.Release();
            m_RenderGraphCameraDepthHandle?.Release();
        }

        internal static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear,
            FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
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
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None;
            // TODO RENDERGRAPH: depthStencilFormat handling?

            return renderGraph.CreateTexture(rgDesc);
        }

        void RequiresColorAndDepthTextures(RenderGraph renderGraph, out bool createColorTexture, out bool createDepthTexture, ref RenderingData renderingData, RenderPassInputSummary renderPassInputs)
        {
            bool isPreviewCamera = renderingData.cameraData.isPreviewCamera;
            bool applyPostProcessing = renderingData.cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            bool requiresDepthPrepass = RequireDepthPrepass(ref renderingData, renderPassInputs);

            createColorTexture = (rendererFeatures.Count != 0 && m_IntermediateTextureMode == IntermediateTextureMode.Always) && !isPreviewCamera;
            createColorTexture |= RequiresIntermediateColorTexture(ref renderingData.cameraData);
            createColorTexture &= !isPreviewCamera;

            createDepthTexture = RequireDepthTexture(ref renderingData, renderPassInputs, requiresDepthPrepass);

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
            useDepthPriming = IsDepthPrimingEnabled();
            useDepthPriming &= requiresDepthPrepass && (createDepthTexture || createColorTexture) && m_RenderingMode == RenderingMode.Forward && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth);

            if (useRenderPassEnabled || useDepthPriming)
                createColorTexture |= createDepthTexture;

            //Scene filtering redraws the objects on top of the resulting frame. It has to draw directly to the sceneview buffer.
            bool sceneViewFilterEnabled = renderingData.cameraData.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered;
            bool intermediateRenderTexture = (createColorTexture || createDepthTexture) && !sceneViewFilterEnabled;
            createDepthTexture = intermediateRenderTexture;
        }

        void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;

            RenderTargetIdentifier targetId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                targetId = cameraData.xr.renderTarget;
#endif
            frameResources.backBufferColor = renderGraph.ImportBackbuffer(targetId);
            //frameResources.backBufferDepth = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.Depth);

            #region Intermediate Camera Target
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            // Enable depth normal prepass if it's needed by rendering layers
            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

            // We configure this for the first camera of the stack and overlay camera will reuse create color/depth var
            // to pick the correct target, as if there is an intermediate texture, overlay cam should use them
            if (cameraData.renderType == CameraRenderType.Base)
                 RequiresColorAndDepthTextures(renderGraph, out m_CreateColorTexture, out m_CreateDepthTexture, ref renderingData, renderPassInputs);

            if (m_CreateColorTexture)
            {
                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandle, cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraTargetAttachment");

                m_ActiveRenderGraphColor = frameResources.cameraColor = renderGraph.ImportTexture(m_RenderGraphCameraColorHandle);
                m_TargetIsBackbuffer = false;
            }
            else
            {
                m_ActiveRenderGraphColor = frameResources.backBufferColor;
                m_TargetIsBackbuffer = true;
            }


            if (m_CreateDepthTexture)
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

                m_ActiveRenderGraphDepth = frameResources.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle);
            }
            else
            {
                m_ActiveRenderGraphDepth = frameResources.backBufferColor;
            }
            #endregion

            CreateCameraDepthCopyTexture(renderGraph, cameraData.cameraTargetDescriptor,RequireDepthPrepass(ref renderingData, renderPassInputs) && this.renderingModeActual != RenderingMode.Deferred);

            CreateCameraNormalsTexture(renderGraph, cameraData.cameraTargetDescriptor);

            CreateMotionVectorTextures(renderGraph, cameraData.cameraTargetDescriptor);

            CreateRenderingLayersTexture(renderGraph, cameraData.cameraTargetDescriptor);

            CreateAfterPostProcessTexture(renderGraph, cameraData.cameraTargetDescriptor);
        }

        void SetupRenderingLayers(ref RenderingData renderingData)
        {
            // Gather render pass require rendering layers event and mask size
            m_RequiresRenderingLayer = RenderingLayerUtils.RequireRenderingLayers(this, rendererFeatures, renderingData.cameraData.cameraTargetDescriptor.msaaSamples,
                out m_RenderingLayersEvent, out m_RenderingLayersMaskSize);

            m_RenderingLayerProvidesRenderObjectPass = m_RequiresRenderingLayer && renderingModeActual == RenderingMode.Forward && m_RenderingLayersEvent == RenderingLayerUtils.Event.Opaque;
            m_RenderingLayerProvidesByDepthNormalPass = m_RequiresRenderingLayer && m_RenderingLayersEvent == RenderingLayerUtils.Event.DepthNormalPrePass;

            if (m_DeferredLights != null)
            {
                m_DeferredLights.RenderingLayerMaskSize = m_RenderingLayersMaskSize;
                m_DeferredLights.UseDecalLayers = m_RequiresRenderingLayer;
            }
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            useRenderPassEnabled = false;

            SetupRenderingLayers(ref renderingData);

            CreateRenderGraphCameraRenderTargets(renderGraph, context, ref renderingData);
            SetupRenderGraphCameraProperties(renderGraph, ref renderingData, m_TargetIsBackbuffer);

            DebugHandler?.Setup(context, ref renderingData);

            cameraData.renderer.useDepthPriming = useDepthPriming;

            if (cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth)
            {
                OnOffscreenDepthTextureRendering(renderGraph, context, ref renderingData);
                return;
            }

            OnBeforeRendering(renderGraph, context, ref renderingData);

            OnMainRendering(renderGraph, context, ref renderingData);

            OnAfterRendering(renderGraph, context, ref renderingData);
        }

        internal override void OnFinishRenderGraphRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (this.renderingModeActual == RenderingMode.Deferred)
                m_DeferredPass.OnCameraCleanup(renderingData.commandBuffer);

            m_CopyDepthPass.OnCameraCleanup(renderingData.commandBuffer);
        }

        private static bool m_CreateColorTexture;
        private static bool m_CreateDepthTexture;

        private void OnOffscreenDepthTextureRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRendering, RenderPassEvent.BeforeRenderingOpaques);
            m_RenderOpaqueForwardPass.Render(renderGraph, frameResources.backBufferColor, TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingSkybox);
            m_DrawSkyboxPass.Render(renderGraph, frameResources.backBufferColor, TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingSkybox, RenderPassEvent.BeforeRenderingTransparents);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            m_RenderTransparentForwardPass.Render(renderGraph, frameResources.backBufferColor, TextureHandle.nullHandle, TextureHandle.nullHandle, TextureHandle.nullHandle, ref renderingData);
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingTransparents, RenderPassEvent.AfterRendering);
        }
        private void OnBeforeRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // TODO RENDERGRAPH: we need to discuss and decide if RenderPassEvent.BeforeRendering injected passes should only be called before the first camera in the stack
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRendering);

            m_ForwardLights.ProcessLights(ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRenderingShadows);

            bool renderShadows = false;

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                frameResources.mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, ref renderingData);
            }

            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
            {
                renderShadows = true;
                frameResources.additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, ref renderingData);
            }

            // The camera need to be setup again after the shadows since those passes override some settings
            // TODO RENDERGRAPH: move the setup code into the shadow passes
            if (renderShadows)
                SetupRenderGraphCameraProperties(renderGraph, ref renderingData, m_TargetIsBackbuffer);

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingShadows);
        }

        private void OnMainRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (renderingData.cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            if (clearFlags != RTClearFlags.None)
                ClearTargetsPass.Render(renderGraph, clearFlags, renderingData.cameraData.backgroundColor);

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRenderingPrePasses);

            var cameraData = renderingData.cameraData;
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = applyPostProcessing && cameraData.postProcessingRequiresDepthTexture;

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            if (m_RenderingLayerProvidesByDepthNormalPass)
                renderPassInputs.requiresNormalsTexture = true;

            bool requiresDepthPrepass = RequireDepthPrepass(ref renderingData, renderPassInputs);
            bool requiresDepthCopyPass = !requiresDepthPrepass
                                         && (cameraData.requiresDepthTexture || cameraHasPostProcessingWithDepth || renderPassInputs.requiresDepthTexture)
                                         && m_CreateDepthTexture; // we create both intermediate textures if this is true, so instead of repeating the checks we reuse this
            bool requiresColorCopyPass = renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;
            bool requiredColorGradingLutPass = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;

            bool isDeferred = this.renderingModeActual == RenderingMode.Deferred;

            if (requiresDepthPrepass)
            {
                // TODO RENDERGRAPH: is this always a valid assumption for deferred rendering?
                TextureHandle depthTarget = (renderingModeActual == RenderingMode.Deferred) ? m_ActiveRenderGraphDepth : frameResources.cameraDepthTexture;
                depthTarget = (useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth)) ? m_ActiveRenderGraphDepth : depthTarget;

                if (renderPassInputs.requiresNormalsTexture)
                    DepthNormalPrepassRender(renderGraph, renderPassInputs, depthTarget, ref renderingData);
                else
                    m_DepthPrepass.Render(renderGraph, ref depthTarget, ref renderingData);
            }

            if (useDepthPriming && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan || cameraData.cameraTargetDescriptor.msaaSamples == 1))
                m_PrimedDepthCopyPass.Render(renderGraph, ref frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData);

            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
                RenderGraphUtils.SetGlobalTexture(renderGraph, "_CameraDepthTexture", SystemInfo.usesReversedZBuffer ? renderGraph.defaultResources.blackTexture : renderGraph.defaultResources.whiteTexture, "Set default Camera Depth Texture");

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingPrePasses);

            if (requiredColorGradingLutPass)
                m_PostProcessPasses.colorGradingLutPass.Render(renderGraph, out frameResources.internalColorLut, ref renderingData);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                m_XROcclusionMeshPass.Render(renderGraph, frameResources.cameraDepth, ref renderingData);
#endif

            if (isDeferred)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);
                if (m_DeferredLights != null)
                {
                    m_DeferredLights.UseRenderPass = false;
                    m_DeferredLights.HasNormalPrepass = renderPassInputs.requiresNormalsTexture;
                    m_DeferredLights.HasDepthPrepass = requiresDepthPrepass;
                    m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                    m_DeferredLights.IsOverlay = cameraData.renderType == CameraRenderType.Overlay;
                }

                RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRenderingGbuffer);

                m_GBufferPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData, ref frameResources);
                m_GBufferCopyDepthPass.Render(renderGraph, ref frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData, "GBuffer Depth Copy");

                RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingGbuffer, RenderPassEvent.BeforeRenderingDeferredLights);

                m_DeferredPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.gbuffer, ref renderingData);

                RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingDeferredLights, RenderPassEvent.BeforeRenderingOpaques);

                m_RenderOpaqueForwardOnlyPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }
            else
            {
                RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRenderingOpaques);

                if (m_RenderingLayerProvidesRenderObjectPass)
                {
                    m_RenderOpaqueForwardWithRenderingLayersPass.Render(renderGraph, m_ActiveRenderGraphColor, frameResources.renderingLayersTexture, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, m_RenderingLayersMaskSize, ref renderingData);
                    SetRenderingLayersGlobalTextures(renderGraph);
                }
                else
                {
                    m_RenderOpaqueForwardPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
                }
            }

            if (requiresDepthCopyPass && m_CopyDepthMode != CopyDepthMode.AfterTransparents)
                m_CopyDepthPass.Render(renderGraph, ref frameResources.cameraDepthTexture, in m_ActiveRenderGraphDepth, ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingOpaques, RenderPassEvent.BeforeRenderingSkybox);

            if (cameraData.camera.clearFlags == CameraClearFlags.Skybox && cameraData.renderType != CameraRenderType.Overlay)
            {
                if (RenderSettings.skybox != null || (cameraData.camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null))
                    m_DrawSkyboxPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData);
            }

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingSkybox, RenderPassEvent.BeforeRenderingTransparents);

            if (requiresColorCopyPass)
            {
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                m_CopyColorPass.Render(renderGraph, out frameResources.cameraOpaqueTexture, in m_ActiveRenderGraphColor, downsamplingMethod, ref renderingData);
            }

            // TODO RENDERGRAPH: bind _CameraOpaqueTexture, _CameraDepthTexture in transparent pass?
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                m_RenderTransparentForwardPass.m_ShouldTransparentsReceiveShadows = !m_TransparentSettingsPass.Setup(ref renderingData);
                m_RenderTransparentForwardPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }

            if (requiresDepthCopyPass && m_CopyDepthMode == CopyDepthMode.AfterTransparents)
                m_CopyDepthPass.Render(renderGraph, ref frameResources.cameraDepthTexture, in m_ActiveRenderGraphDepth, ref renderingData);

            // TODO: Postprocess pass should be able configure its render pass inputs per camera per frame (settings) BEFORE building any of the graph
            // TODO: Alternatively we could always build the graph (a potential graph) and cull away unused passes if "record + cull" is fast enough.
            // TODO: Currently we just override "requiresMotionVectors" for TAA in GetRenderPassInputs()
            if (renderPassInputs.requiresMotionVectors)
            {
                // Depends on camera depth
                m_MotionVectorPass.Render(renderGraph, ref frameResources.cameraDepthTexture, in frameResources.motionVectorColor, in frameResources.motionVectorDepth, ref renderingData);
            }

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingTransparents);

            m_OnRenderObjectCallbackPass.Render(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, ref renderingData);
        }

        private void OnAfterRendering(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            bool isGizmosEnabled = UnityEditor.Handles.ShouldRenderGizmos();
#endif
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, m_ActiveRenderGraphColor, m_ActiveRenderGraphDepth, GizmoSubset.PreImageEffects, ref renderingData);

            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.BeforeRenderingPostProcessing);

            bool applyPostProcessing = renderingData.cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            if (applyPostProcessing)
            {
                postProcessPass.RenderPostProcessingRenderGraph(renderGraph, in m_ActiveRenderGraphColor, in frameResources.internalColorLut, in frameResources.afterPostProcessColor, ref renderingData, true);
                postProcessPass.RenderFinalPassRenderGraph(renderGraph, in frameResources.afterPostProcessColor, ref renderingData);
            }
            // TODO RENDERGRAPH: we need to discuss and decide if RenderPassEvent.AfterRendering injected passes should only be called after the last camera in the stack
            RecordCustomRenderGraphPasses(renderGraph, context, ref renderingData, RenderPassEvent.AfterRenderingPostProcessing, RenderPassEvent.AfterRendering);

            if (!m_TargetIsBackbuffer && renderingData.cameraData.resolveFinalTarget && !applyPostProcessing)
            {
                m_FinalBlitPass.Render(renderGraph, ref renderingData, frameResources.cameraColor, frameResources.backBufferColor);
                m_ActiveRenderGraphColor = frameResources.backBufferColor;
                m_ActiveRenderGraphDepth = frameResources.backBufferColor;
                m_TargetIsBackbuffer = true;
            }

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera || (isGizmosEnabled && renderingData.cameraData.resolveFinalTarget))
            {
                m_FinalDepthCopyPass.CopyToDepth = true;
                m_FinalDepthCopyPass.MssaSamples = 0;
                m_FinalDepthCopyPass.Render(renderGraph, ref m_ActiveRenderGraphDepth, in frameResources.cameraDepthTexture, ref renderingData, "Final Depth Copy");
            }
#endif

            if (drawGizmos)
                DrawRenderGraphGizmos(renderGraph, frameResources.backBufferColor, m_ActiveRenderGraphDepth, GizmoSubset.PostImageEffects, ref renderingData);

            // Invalidating the textures so they wouldn't be accessed
            m_ActiveRenderGraphColor = TextureHandle.nullHandle;
            m_ActiveRenderGraphDepth = TextureHandle.nullHandle;
        }

        bool RequireDepthPrepass(ref RenderingData renderingData, RenderPassInputSummary renderPassInputs)
        {
            ref var cameraData = ref renderingData.cameraData;
            bool applyPostProcessing = cameraData.postProcessEnabled && m_PostProcessPasses.isCreated;
            // If Camera's PostProcessing is enabled and if there any enabled PostProcessing requires depth texture as shader read resource (Motion Blur/DoF)
            bool cameraHasPostProcessingWithDepth = applyPostProcessing && cameraData.postProcessingRequiresDepthTexture;

            bool forcePrepass = (m_CopyDepthMode == CopyDepthMode.ForcePrepass);

            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;
            bool requiresDepthPrepass = (requiresDepthTexture || cameraHasPostProcessingWithDepth) && (!CanCopyDepth(ref cameraData) || forcePrepass);
            requiresDepthPrepass |= cameraData.isSceneViewCamera;
            // requiresDepthPrepass |= isGizmosEnabled;
            requiresDepthPrepass |= cameraData.isPreviewCamera;
            requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
            requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;

            // Current aim of depth prepass is to generate a copy of depth buffer, it is NOT to prime depth buffer and reduce overdraw on non-mobile platforms.
            // When deferred renderer is enabled, depth buffer is already accessible so depth prepass is not needed.
            // The only exception is for generating depth-normal textures: SSAO pass needs it and it must run before forward-only geometry.
            // DepthNormal prepass will render:
            // - forward-only geometry when deferred renderer is enabled
            // - all geometry when forward renderer is enabled
            if (requiresDepthPrepass && this.renderingModeActual == RenderingMode.Deferred && !renderPassInputs.requiresNormalsTexture)
                requiresDepthPrepass = false;

            requiresDepthPrepass |= m_DepthPrimingMode == DepthPrimingMode.Forced;
            return requiresDepthPrepass;
        }

        bool RequireDepthTexture(ref RenderingData renderingData, RenderPassInputSummary renderPassInputs, bool requiresDepthPrepass)
        {
            bool requiresDepthTexture = renderingData.cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;

            // TODO RENDERGRAPH: re-enable the cameraHasPostProcessingWithDepth check once post processing is ported
            var createDepthTexture = (requiresDepthTexture/* || cameraHasPostProcessingWithDepth*/) && !requiresDepthPrepass;
            createDepthTexture |= !renderingData.cameraData.resolveFinalTarget;
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= (renderingModeActual == RenderingMode.Deferred && !useRenderPassEnabled);
            // Some render cases (e.g. Material previews) have shown we need to create a depth texture when we're forcing a prepass.
            createDepthTexture |= m_DepthPrimingMode == DepthPrimingMode.Forced;
            // TODO: seems like with mrt depth is not taken from first target. Investigate if this is needed
            createDepthTexture |= m_RenderingLayerProvidesRenderObjectPass;

            return createDepthTexture;
        }

        internal void SetRenderingLayersGlobalTextures(RenderGraph renderGraph)
        {
            RenderGraphUtils.SetGlobalTexture(renderGraph, m_RenderingLayersTextureName , frameResources.renderingLayersTexture, "Set Rendering Layers Texture");

            if (renderingModeActual == RenderingMode.Deferred) // As this is requested by render pass we still want to set it
                RenderGraphUtils.SetGlobalTexture(renderGraph, "_CameraRenderingLayersTexture", frameResources.renderingLayersTexture, "Set Deferred Rendering Layers Texture");
        }

        void CreateCameraDepthCopyTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor, bool isDepthTexture)
        {
            var depthDescriptor = descriptor;
            depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA

            if (isDepthTexture)
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
            }
            else
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
                depthDescriptor.depthStencilFormat = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = 0;
            }

            frameResources.cameraDepthTexture = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthTexture", true);
        }

        void CreateMotionVectorTextures(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var colorDesc = descriptor;
            colorDesc.graphicsFormat = MotionVectorRenderPass.k_TargetFormat; colorDesc.depthBufferBits = (int)DepthBits.None;
            frameResources.motionVectorColor = CreateRenderGraphTexture(renderGraph, colorDesc, "_MotionVectorTexture", true);

            var depthDescriptor = descriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            //TODO RENDERGRAPH: in some cornercases (f.e. rendering to targetTexture) this is needed. maybe this will be unnece
            depthDescriptor.depthBufferBits = depthDescriptor.depthBufferBits != 0 ? depthDescriptor.depthBufferBits : 32;
            frameResources.motionVectorDepth = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_MotionVectorDepthTexture", true);
        }

        void CreateCameraNormalsTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var normalDescriptor = descriptor;
            normalDescriptor.depthBufferBits = 0;
            // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
            normalDescriptor.msaaSamples = useDepthPriming ? descriptor.msaaSamples : 1;// Depth-Only passes don't use MSAA, unless depth priming is enabled
            // Find compatible render-target format for storing normals.
            // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
            // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
            var normalsName = this.renderingModeActual != RenderingMode.Deferred ? "_CameraNormalsTexture" : DeferredLights.k_GBufferNames[m_DeferredLights.GBufferNormalSmoothnessIndex];
            normalDescriptor.graphicsFormat = this.renderingModeActual != RenderingMode.Deferred ? DepthNormalOnlyPass.GetGraphicsFormat() : m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferNormalSmoothnessIndex);
            frameResources.cameraNormalsTexture = CreateRenderGraphTexture(renderGraph, normalDescriptor, normalsName, true);
        }

        void CreateRenderingLayersTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            if (m_RequiresRenderingLayer)
            {
                m_RenderingLayersTextureName = "_CameraRenderingLayersTexture";

                // TODO RENDERGRAPH: deferred optimization
                if (renderingModeActual == RenderingMode.Deferred && m_DeferredLights.UseRenderingLayers)
                {
                    //RTHandle renderingLayersTexture = frameResources.gbuffer[(int)m_DeferredLights.GBufferRenderingLayers];
                    //m_RenderingLayersTextureName = ""; //renderingLayersTexture.name;
                    m_RenderingLayersTextureName = DeferredLights.k_GBufferNames[m_DeferredLights.GBufferRenderingLayers];
                }

                RenderTextureDescriptor renderingLayersDescriptor = descriptor;
                renderingLayersDescriptor.depthBufferBits = 0;
                if (!m_RenderingLayerProvidesRenderObjectPass)
                    renderingLayersDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA

                // Find compatible render-target format for storing normals.
                // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                if (renderingModeActual == RenderingMode.Deferred && m_RequiresRenderingLayer)
                    renderingLayersDescriptor.graphicsFormat = m_DeferredLights.GetGBufferFormat(m_DeferredLights.GBufferRenderingLayers); // the one used by the gbuffer.
                else
                    renderingLayersDescriptor.graphicsFormat = RenderingLayerUtils.GetFormat(m_RenderingLayersMaskSize);

                frameResources.renderingLayersTexture = CreateRenderGraphTexture(renderGraph, renderingLayersDescriptor, m_RenderingLayersTextureName, true);
            }
        }

        void CreateAfterPostProcessTexture(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
        {
            var desc = PostProcessPass.GetCompatibleDescriptor(descriptor, descriptor.width, descriptor.height, UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat(), DepthBits.None);
            frameResources.afterPostProcessColor = CreateRenderGraphTexture(renderGraph, desc, "_AfterPostProcessTexture", true);
        }

        void DepthNormalPrepassRender(RenderGraph renderGraph, RenderPassInputSummary renderPassInputs, TextureHandle depthTarget, ref RenderingData renderingData)
        {
            TextureHandle normalsTexture = frameResources.cameraNormalsTexture;

            if (m_RenderingLayerProvidesByDepthNormalPass)
            {
                m_DepthNormalPrepass.enableRenderingLayers = true;
                m_DepthNormalPrepass.renderingLayersMaskSize = m_RenderingLayersMaskSize;
            }

            if (renderingModeActual == RenderingMode.Deferred)
            {
                // Only render forward-only geometry, as standard geometry will be rendered as normal into the gbuffer.
                if (RenderPassEvent.AfterRenderingGbuffer <= renderPassInputs.requiresDepthNormalAtEvent &&
                    renderPassInputs.requiresDepthNormalAtEvent <= RenderPassEvent.BeforeRenderingOpaques)
                    m_DepthNormalPrepass.shaderTagIds = k_DepthNormalsOnly;

                // TODO RENDERGRAPH: commented this out since would be equivalent to the current behaviour? Double check
                //if (!m_RenderingLayerProvidesByDepthNormalPass)
                // renderingLayersTexture = frameResources.gbuffer[m_DeferredLights.GBufferRenderingLayers]; // GBUffer texture here
            }

            m_DepthNormalPrepass.Render(renderGraph, normalsTexture, depthTarget, frameResources.renderingLayersTexture, ref renderingData);

            if (m_RequiresRenderingLayer)
                SetRenderingLayersGlobalTextures(renderGraph);
        }
    }

    static class RenderGraphUtils
    {
        static private ProfilingSampler s_SetGlobalTextureProfilingSampler = new ProfilingSampler("Set Global Texture");

        private class PassData
        {
            internal TextureHandle texture;
            internal string name;
            internal int nameID;
        }

        internal static void SetGlobalTexture(RenderGraph graph, string name, TextureHandle texture, string passName = "Set Global Texture")
        {
            using (var builder = graph.AddRenderPass<PassData>(passName, out var passData, s_SetGlobalTextureProfilingSampler))
            {
                passData.texture = builder.ReadTexture(texture);
                passData.name = name;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(data.name, data.texture);
                });
            }
        }

        internal static void SetGlobalTexture(RenderGraph graph, int nameID, TextureHandle texture, string passName = "Set Global Texture")
        {
            using (var builder = graph.AddRenderPass<PassData>(passName, out var passData, s_SetGlobalTextureProfilingSampler))
            {
                passData.texture = builder.ReadTexture(texture);
                passData.nameID = nameID;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(data.nameID, data.texture);
                });
            }
        }
    }
    class ClearTargetsPass
    {
        static private ProfilingSampler s_ClearProfilingSampler = new ProfilingSampler("Clear Targets");

        private class PassData
        {
            internal TextureHandle color;
            internal TextureHandle depth;

            internal RTClearFlags clearFlags;
            internal Color clearColor;
        }

        internal static void Render(RenderGraph graph, RTClearFlags clearFlags, Color clearColor)
        {
            Debug.Assert(UniversalRenderer.m_ActiveRenderGraphColor.IsValid() && UniversalRenderer.m_ActiveRenderGraphDepth.IsValid(), "Active RenderGraph Color and Depth targets are invalid");
            Render(graph, UniversalRenderer.m_ActiveRenderGraphColor, UniversalRenderer.m_ActiveRenderGraphDepth, clearFlags, clearColor);
        }
        internal static void Render(RenderGraph graph, CameraData cameraData)
        {
            Debug.Assert(UniversalRenderer.m_ActiveRenderGraphColor.IsValid() && UniversalRenderer.m_ActiveRenderGraphDepth.IsValid(), "Active RenderGraph Color and Depth targets are invalid");
            Render(graph, UniversalRenderer.m_ActiveRenderGraphColor, UniversalRenderer.m_ActiveRenderGraphDepth, cameraData);
        }
        internal static void Render(RenderGraph graph, TextureHandle colorHandle, TextureHandle depthHandle,
            CameraData cameraData)
        {
            RTClearFlags clearFlags = RTClearFlags.None;

            if (cameraData.renderType == CameraRenderType.Base)
                clearFlags = RTClearFlags.All;
            else if (cameraData.clearDepth)
                clearFlags = RTClearFlags.Depth;

            if (clearFlags != RTClearFlags.None)
                Render(graph, colorHandle, depthHandle, clearFlags, cameraData.backgroundColor);
        }
        internal static void Render(RenderGraph graph, TextureHandle colorHandle, TextureHandle depthHandle, RTClearFlags clearFlags, Color clearColor)
        {
            Debug.Assert(colorHandle.IsValid() || depthHandle.IsValid(), "Trying to clear an invalid render target");

            using (var builder = graph.AddRenderPass<PassData>("Clear Targets Pass", out var passData, s_ClearProfilingSampler))
            {
                passData.color = builder.UseColorBuffer(colorHandle, 0);
                passData.depth = builder.UseDepthBuffer(depthHandle, DepthAccess.Write);
                passData.clearFlags = clearFlags;
                passData.clearColor = clearColor;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });
            }
        }
    }

}
