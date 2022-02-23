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
        };
        internal RenderGraphFrameResources frameResources = new RenderGraphFrameResources();

        TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph, RenderTextureDescriptor desc, string name, bool clear)
        {
            TextureDesc rgDesc = new TextureDesc();
            rgDesc.dimension = desc.dimension;
            rgDesc.width = desc.width;
            rgDesc.height = desc.height;
            rgDesc.clearBuffer = clear;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.colorFormat = desc.graphicsFormat;
            rgDesc.depthBufferBits = (DepthBits)desc.depthBufferBits;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;

            return renderGraph.CreateTexture(rgDesc);
        }

        void CreateRenderGraphCameraRenderTargets(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            RenderGraph renderGraph = renderingData.renderGraph;

            frameResources.backBufferColor = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
            frameResources.backBufferDepth = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.Depth);

            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);


            if (cameraData.renderType == CameraRenderType.Base)
            {
                // TODO: check if we need intermediate textures
				// bool createColorTexture = false;
				// createColorTexture |= RequiresIntermediateColorTexture(ref renderingData.cameraData);
				// createColorTexture |= renderPassInputs.requiresColorTexture;

                // COLOR

                var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.useMipMap = false;
                cameraTargetDescriptor.autoGenerateMips = false;
                cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

                frameResources.cameraColor = CreateRenderGraphTexture(renderGraph, cameraTargetDescriptor, "_CameraTargetAttachment", cameraData.renderType == CameraRenderType.Base);

                // DEPTH

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
        }

        private void OnMainRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderGraphTestPass.Render(renderingData.renderGraph, this);

            // Draw Opaque

            // RunCustomPasses(RenderPassEvent.AfterOpaque);

            // Draw Transparent
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
                //passData.m_Depth = builder.UseDepthBuffer(renderer.frameResources.cameraDepth, DepthAccess.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.All, Color.red, 0, 0);
                });

                return passData;
            }
        }
    }

}
