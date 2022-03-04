using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        private RTHandle m_RenderGraphCameraColorHandle;
        private RTHandle m_RenderGraphCameraDepthHandle;

        internal class RenderGraphFrameResources
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
        };
        internal RenderGraphFrameResources frameResources = new RenderGraphFrameResources();

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

        void CreateRenderGraphCameraRenderTargets(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            RenderGraph renderGraph = renderingData.renderGraph;

            frameResources.backBufferColor = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
            //frameResources.backBufferDepth = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.Depth);

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);

            #region Intermediate Camera Target
            // TODO RENDERGRAPH: check if we need intermediate textures.Enable this code when we actually need the logic. Or can we always create them and RG will allocate only if needed?
            // bool createColorTexture = false;
            // createColorTexture |= RequiresIntermediateColorTexture(ref renderingData.cameraData);
            // createColorTexture |= renderPassInputs.requiresColorTexture;
            // bool createDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || m_DepthPrimingMode == DepthPrimingMode.Forced;

            // if (createColorTexture)
            {
                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderGraphCameraColorHandle, cameraTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CameraTargetAttachment");
                frameResources.cameraColor = renderGraph.ImportTexture(m_RenderGraphCameraColorHandle);
            }

            // if (createDepthTexture)
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
                frameResources.cameraDepth = renderGraph.ImportTexture(m_RenderGraphCameraDepthHandle);
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
                ClearTargetsPass.Render(renderingData.renderGraph, this, clearFlags);

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
            if (this.renderingModeActual == RenderingMode.Deferred)
            {
                m_DeferredLights.Setup(m_AdditionalLightsShadowCasterPass);
                if (m_DeferredLights != null)
                {
                    m_DeferredLights.UseRenderPass = false;
                    m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
                    m_DeferredLights.IsOverlay = renderingData.cameraData.renderType == CameraRenderType.Overlay;
                }

                m_GBufferPass.Render(frameResources.cameraColor, frameResources.cameraDepth, ref renderingData, ref frameResources);
                m_GBufferCopyDepthPass.Render(out frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData);
                m_DeferredPass.Render(frameResources.cameraColor, frameResources.cameraDepth, frameResources.gbuffer, ref renderingData);
                m_RenderOpaqueForwardOnlyPass.Render(frameResources.cameraColor, frameResources.cameraDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }
            else
            {
                m_RenderOpaqueForwardPass.Render(frameResources.cameraColor, frameResources.cameraDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
            }
            // RunCustomPasses(RenderPassEvent.AfterOpaque);

            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                m_DrawSkyboxPass.Render(frameResources.cameraColor, frameResources.cameraDepth, ref renderingData);
            //if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Render(out frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData);
            }

            //if (requiresColorCopyPass)
            {
                Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                m_CopyColorPass.Render(out frameResources.cameraOpaqueTexture, in frameResources.cameraColor, downsamplingMethod, ref renderingData);
            }

            // TODO RENDERGRAPH: bind _CameraOpaqueTexture, _CameraDepthTexture in transparent pass?

            m_RenderTransparentForwardPass.Render(frameResources.cameraColor, frameResources.cameraDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);

            m_OnRenderObjectCallbackPass.Render(frameResources.cameraColor, frameResources.cameraDepth, ref renderingData);
        }

        private void OnAfterRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            if (drawGizmos)
                DrawRenderGraphGizmos(frameResources.cameraColor, frameResources.cameraDepth, GizmoSubset.PreImageEffects, ref renderingData);

            // TODO RENDERGRAPH: postprocessing passes

            if (drawGizmos)
                DrawRenderGraphGizmos(frameResources.cameraColor, frameResources.cameraDepth, GizmoSubset.PostImageEffects, ref renderingData);

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
        }

        static public PassData Render(RenderGraph graph, UniversalRenderer renderer, RTClearFlags clearFlags)
        {
            using (var builder = graph.AddRenderPass<PassData>("Clear Targets Pass", out var passData, s_ClearProfilingSampler))
            {
                TextureHandle color = renderer.frameResources.cameraColor;
                passData.color = builder.UseColorBuffer(color, 0);
                passData.depth = builder.UseDepthBuffer(renderer.frameResources.cameraDepth, DepthAccess.Write);
                passData.clearFlags = clearFlags;
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, Color.black, 1, 0);
                });

                return passData;
            }
        }
    }

}
