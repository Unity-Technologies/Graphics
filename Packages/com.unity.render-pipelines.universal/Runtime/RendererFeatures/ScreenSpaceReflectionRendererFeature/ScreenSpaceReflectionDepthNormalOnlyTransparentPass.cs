using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace UnityEngine.Rendering.Universal
{
    // A separate DepthNormal pass that renders only transparents. Used to support transparency in screen space reflections.
    internal class ScreenSpaceReflectionDepthNormalOnlyTransparentPass : Internal.DepthNormalOnlyPass
    {
        static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSR Depth Normal Only Transparent");

        readonly Internal.CopyDepthPass m_CopyDepthPass;

        public ScreenSpaceReflectionDepthNormalOnlyTransparentPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
            : base(evt, renderQueueRange, layerMask, m_ProfilingSampler)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);

            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRendererResources>(
                    out var universalRendererShaders))
            {
                //Pass event not used because we call the render function directly.
                m_CopyDepthPass = new(RenderPassEvent.AfterRenderingPrePasses, universalRendererShaders.copyDepthPS, false, true, false, "Copy Depth");
            }
            else
            {
                Debug.LogError("Failed to create the copy depth path required for the SSR Depth Normal Only Transparent pass");
            }
        }

        public void UpdateRenderPassEvent(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        /// <inheritdoc />
        protected override SortingCriteria GetSortingCriteria(UniversalCameraData cameraData)
        {
            // Sort for transparent without opaque.
            var sortingCriteria = cameraData.defaultOpaqueSortFlags;
            sortingCriteria = sortingCriteria & ~SortingCriteria.CommonOpaque | SortingCriteria.CommonTransparent;
            return sortingCriteria;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var ssrData = frameData.GetOrCreate<ScreenSpaceReflectionPass.SharedSSRData>();
            if (!ssrData.depthTransparentTexture.IsValid())
            {
                var depthDescriptor = resourceData.cameraDepth.GetDescriptor(renderGraph);
                depthDescriptor.msaaSamples = MSAASamples.None; // Depth-Only pass don't use MSAA
                depthDescriptor.format = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = DepthBits.Depth32;
                ssrData.depthTransparentTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "SSRDepthTransparentTexture", false, Color.clear);
            }
            if (!ssrData.normalTransparentTexture.IsValid())
            {
                var normalDescriptor = resourceData.cameraNormalsTexture.GetDescriptor(renderGraph);
                normalDescriptor.msaaSamples = MSAASamples.None; // Never use MSAA for the normal texture!
                normalDescriptor.format = GetGraphicsFormat();
                normalDescriptor.depthBufferBits = DepthBits.None;
                ssrData.normalTransparentTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, normalDescriptor, "SSRNormalTransparentTexture", true, Color.clear);
            }

            m_CopyDepthPass.Render(renderGraph, frameData, ssrData.depthTransparentTexture, resourceData.cameraDepthTexture);
            renderGraph.AddCopyPass(resourceData.cameraNormalsTexture, ssrData.normalTransparentTexture, "Copy Normal Texture");

            if (enableRenderingLayers)
            {
                Debug.LogError("DepthNormalOnlyPass does not support enableRenderingLayers when called as a reusable pass.");
                return;
            }

            if (ssrData.depthTransparentTexture.IsValid() && ssrData.normalTransparentTexture.IsValid())
            {
                Render(renderGraph, frameData, ssrData.normalTransparentTexture, ssrData.depthTransparentTexture, TextureHandle.nullHandle, uint.MaxValue, false, false, false);
            }
        }
    }
}
