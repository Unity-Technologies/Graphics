using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Let customizable actions inject commands to capture the camera output.
    ///
    /// You can use this pass to inject capture commands into a command buffer
    /// with the goal of having camera capture happening in external code.
    /// </summary>
    internal class CapturePass : ScriptableRenderPass
    {
        RTHandle m_CameraColorHandle;

        public CapturePass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler("Capture Camera output");
            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmdBuf = renderingData.commandBuffer;

            m_CameraColorHandle = renderingData.cameraData.renderer.GetCameraColorBackBuffer(cmdBuf);

            using (new ProfilingScope(cmdBuf, profilingSampler))
            {
                var colorAttachmentIdentifier = m_CameraColorHandle.nameID;
                var captureActions = renderingData.cameraData.captureActions;
                for (captureActions.Reset(); captureActions.MoveNext();)
                    captureActions.Current(colorAttachmentIdentifier, renderingData.commandBuffer);
            }
        }

        private class UnsafePassData
        {
            internal TextureHandle source;
            public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions;
        }

        // This function needs to add an unsafe render pass to Render Graph because a raster render pass, which is typically
        // used for rendering with Render Graph, cannot perform the texture readback operations performed with the command
        // buffer in CameraTextureProvider. Unsafe passes can do certain operations that raster render passes cannot do and
        // have access to the full command buffer API.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddUnsafePass<UnsafePassData>(passName, out var passData, profilingSampler))
            {
                // Setup up the pass data with cameraColor, which has the correct orientation and position in a built player
                passData.source = resourceData.cameraColor;
                passData.captureActions = cameraData.captureActions;

                // Setup up the builder
                builder.AllowPassCulling(false);
                builder.UseTexture(resourceData.cameraColor);
                builder.SetRenderFunc((UnsafePassData data, UnsafeGraphContext unsafeContext) =>
                {
                    var nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(unsafeContext.cmd);
                    var captureActions = data.captureActions;
                    for (data.captureActions.Reset(); data.captureActions.MoveNext();)
                        captureActions.Current(data.source, nativeCommandBuffer);
                });
            }
        }
    }
}
