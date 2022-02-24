using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        internal class RenderGraphFrameResources
        {
            // backbuffer
            public TextureHandle backBufferColor;
            public TextureHandle backBufferDepth;

            // intermediate camera targets
            public TextureHandle cameraColor;
            public TextureHandle cameraDepth;

            public TextureHandle mainShadowsTexture;
            public TextureHandle additionalShadowsTexture;

            // camear opauqe/depth/normal
            public TextureHandle cameraOpaqueTexture;
            public TextureHandle cameraDepthTexture;
            public TextureHandle cameraNormalsTexture;
        };
        internal RenderGraphFrameResources frameResources = new RenderGraphFrameResources();

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
            // TODO: check if we need intermediate textures.Enable this code when we actually need the logic. Or can we always create them and RG will allocate only if needed?
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

                frameResources.cameraColor = CreateRenderGraphTexture(renderGraph, cameraTargetDescriptor, "_CameraTargetAttachment", cameraData.renderType == CameraRenderType.Base);
            }

            // if (createDepthTexture)
            {
                var depthDescriptor = cameraData.cameraTargetDescriptor;
                depthDescriptor.useMipMap = false;
                depthDescriptor.autoGenerateMips = false;
                depthDescriptor.bindMS = false;

                bool hasMSAA = depthDescriptor.msaaSamples > 1 && (SystemInfo.supportsMultisampledTextures != 0);

                // if MSAA is enabled and we are not resolving depth, which we only do if the CopyDepthPass is AfterTransparents,
                // then we want to bind the multisampled surface.
                if (hasMSAA)
                {
                    // if depth priming is enabled the copy depth primed pass is meant to do the MSAA resolve, so we want to bind the MS surface
                    if (IsDepthPrimingEnabled())
                        depthDescriptor.bindMS = true;
                    else
                        depthDescriptor.bindMS = !(RenderingUtils.MultisampleDepthResolveSupported() && m_CopyDepthMode == CopyDepthMode.AfterTransparents);
                }

                // binding MS surfaces is not supported by the GLES backend, and it won't be fixed after investigating
                // the high performance impact of potential fixes, which would make it more expensive than depth prepass (fogbugz 1339401 for more info)
                if (IsGLESDevice())
                    depthDescriptor.bindMS = false;

                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;

                frameResources.cameraDepth = CreateRenderGraphTexture(renderGraph, depthDescriptor, "_CameraDepthAttachment", cameraData.clearDepth);
            }
            #endregion
        }

        protected override void RecordRenderGraphBlock(RenderGraphRenderPassBlock renderPassBlock, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            switch (renderPassBlock)
            {
                case RenderGraphRenderPassBlock.BeforeRendering:
                    OnBeforeRendering(context, ref renderingData);

                    break;
                case RenderGraphRenderPassBlock.MainRendering:
                    OnMainRendering(context, ref renderingData);

                    break;
                case RenderGraphRenderPassBlock.AfterRendering:
                    OnAfterRendering(context, ref renderingData);

                    break;
            }
        }

        private void OnBeforeRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CreateRenderGraphCameraRenderTargets(context, ref renderingData);

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
                frameResources.mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderingData.renderGraph, ref renderingData);

            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
                frameResources.additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderingData.renderGraph, ref renderingData);

            // TODO: check require DepthPrepass
            //if (requiresDepthPrepass)
            {
                // TODO: check requires normal
                //if (renderPassInputs.requiresNormalsTexture))
                {
                    m_DepthNormalPrepass.Render(out frameResources.cameraDepthTexture, out frameResources.cameraNormalsTexture, ref renderingData);
                }
                //else
                {
                    m_DepthPrepass.Render(out frameResources.cameraDepthTexture, ref renderingData);
                }
            }
        }

        private void OnMainRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderType == CameraRenderType.Base)
                RenderGraphTestPass.Render(renderingData.renderGraph, this);

            m_RenderOpaqueForwardPass.Render(frameResources.backBufferColor, frameResources.cameraDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);

            // RunCustomPasses(RenderPassEvent.AfterOpaque);

            // TODO: Skybox

            //if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Render(out frameResources.cameraDepthTexture, in frameResources.cameraDepth, ref renderingData);
            }

            m_RenderTransparentForwardPass.Render(frameResources.backBufferColor, frameResources.cameraDepth, frameResources.mainShadowsTexture, frameResources.additionalShadowsTexture, ref renderingData);
        }

        private void OnAfterRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

    }


    class RenderGraphTestPass
    {
        public class PassData
        {
            public TextureHandle m_Albedo;
            public TextureHandle m_Depth;
        }

        static public PassData Render(RenderGraph graph, UniversalRenderer renderer)
        {
            using (var builder = graph.AddRenderPass<PassData>("Test Pass", out var passData, new ProfilingSampler("Test Pass Profiler")))
            {
                TextureHandle backbuffer = renderer.frameResources.backBufferColor; //renderer.frameResources.cameraColor;
                passData.m_Albedo = builder.UseColorBuffer(backbuffer, 0);
                passData.m_Depth = builder.UseDepthBuffer(renderer.frameResources.cameraDepth, DepthAccess.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.All, Color.red, 1, 0);
                });

                return passData;
            }
        }
    }

}
