using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// Let customizable actions inject commands to capture the camera output.
    ///
    /// You can use this pass to inject capture commands into a command buffer
    /// with the goal of having camera capture happening in external code.
    /// </summary>
    internal class CapturePass : ScriptableRenderPass
    {
        const string k_CaptureTag = "Capture Pass";

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private IEnumerator<Action<RenderTargetIdentifier, CommandBuffer> > captureActions { get; set; }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="actions"></param>
        public bool Setup(RenderTargetHandle colorAttachmentHandle, IEnumerator<Action<RenderTargetIdentifier, CommandBuffer> > actions)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            captureActions = actions;
            return captureActions != null;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            CommandBuffer cmdBuf = CommandBufferPool.Get(k_CaptureTag);
            var colorAttachmentIdentifier = colorAttachmentHandle.Identifier();
            for (captureActions.Reset(); captureActions.MoveNext();)
                captureActions.Current(colorAttachmentIdentifier, cmdBuf);

            context.ExecuteCommandBuffer(cmdBuf);
            CommandBufferPool.Release(cmdBuf);
        }
    }
}
