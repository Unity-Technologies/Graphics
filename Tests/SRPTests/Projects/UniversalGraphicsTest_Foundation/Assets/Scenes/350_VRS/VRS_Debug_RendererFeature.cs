using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class VRS_Debug_RendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    public int renderPassEventOffset = 0;
    public bool enabled = true;
    public float debugDisplaySize = 0.25f;

    class VRS_Debug_CustomRenderPass : ScriptableRenderPass
    {
        internal bool enabled;
        internal float debugDisplaySize;

        public VRS_Debug_CustomRenderPass()
        {
            profilingSampler = new ProfilingSampler("VRS_Debug_CustomRenderPass");
        }

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            public TextureHandle srDebugColorTextureHandle;
            public Rect dstRect;
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "VRS Debug Pass";

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!enabled)
                return;

            if (cameraData.historyManager == null)
                return;

            VRSHistory vrsHistory = cameraData.historyManager.GetHistoryForRead<VRSHistory>();
            if (vrsHistory == null)
                return;

            var sriTextureRTHandle = vrsHistory.GetSRITexture();
            if (sriTextureRTHandle.rt == null)
                return;

            // Convert
            var sriTextureHandle = vrsHistory.importedSRITextureHandle;
            Debug.Assert(sriTextureHandle.IsValid(), "Debug imported sri is not valid.");

            var colorDesc = cameraData.cameraTargetDescriptor;
            colorDesc.graphicsFormat = GraphicsFormat.B8G8R8A8_UNorm;
            colorDesc.autoGenerateMips = false;
            colorDesc.msaaSamples = 1;
            colorDesc.depthBufferBits = 0;
            colorDesc.depthStencilFormat = GraphicsFormat.None;
            colorDesc.enableRandomWrite = true;
            var srDebugColorTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "Shading Rate Debug Color Texture", false);

            Vrs.ShadingRateImageToColorMaskTexture(renderGraph, sriTextureHandle, srDebugColorTextureHandle);

            // Blit to screen
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.srDebugColorTextureHandle = srDebugColorTextureHandle;

                float dstScale = 1.0f / Mathf.Max(debugDisplaySize, 0.01f);
                int dstWidth = (int)(cameraData.cameraTargetDescriptor.width / dstScale + 0.5f);
                int dstHeight = (int)(cameraData.cameraTargetDescriptor.height / dstScale + 0.5f);
                passData.dstRect = new Rect(cameraData.cameraTargetDescriptor.width - dstWidth,
                    cameraData.cameraTargetDescriptor.height - dstHeight,
                    dstWidth, dstHeight);

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                builder.UseTexture(passData.srDebugColorTextureHandle);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                {
                    context.cmd.SetViewport(passData.dstRect);
                    Blitter.BlitTexture(context.cmd, passData.srDebugColorTextureHandle, new Vector4(1,1,0,0), 0.0f, false);
                });
            }
        }
    }

    VRS_Debug_CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VRS_Debug_CustomRenderPass();
        m_ScriptablePass.renderPassEvent = renderPassEvent + renderPassEventOffset;
        m_ScriptablePass.enabled = enabled;
        m_ScriptablePass.debugDisplaySize = debugDisplaySize;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
    }
}
