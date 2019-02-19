using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Class <c>ScriptableRendererFeature</c> extends <c>ScriptableRenderer</c> with additional features.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    public abstract class ScriptableRendererFeature : ScriptableObject
    {
        /// <summary>
        /// Injects one or multiple <c>ScriptableRenderPass</c> in the renderer.
        /// </summary>
        /// <param name="renderPasses">List of render passes to add to.</param>
        /// <param name="cameraDescriptor">The camera target descriptor. Use this to setup render passes.</param>
        /// <param name="colorAttachmentHandle">The camera color attachment handle. Use this to setup render passes.</param>
        /// <param name="depthAttachmentHandle">The camera depth attachment handle. Use this to setup render passes.</param>
        public abstract void AddRenderPasses(List<ScriptableRenderPass> renderPasses,
            RenderTextureDescriptor cameraDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle);
    }
}
