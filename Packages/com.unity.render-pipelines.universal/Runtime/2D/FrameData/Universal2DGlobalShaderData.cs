using System;

namespace UnityEngine.Rendering.Universal
{
    internal class Universal2DGlobalShaderData : ContextItem
    {
        /// <summary>
        /// Determines whether to use persistent constant buffers or fill dynamic constant buffers with loose uniforms APIs (cmd.SetGlobalFloat, ...)
        /// </summary>
        internal bool useConstantBuffers { get; set; }

        // Owned and created by the renderer (so its persistent constant buffer survives across frames); this per-frame
        // ContextItem only references it, set once per frame via Set().
        private GlobalShaderVariablesUploader m_Uploader;

        internal void Set(GlobalShaderVariablesUploader uploader) => m_Uploader = uploader;

        internal IUniversal2DGlobalShaderVariablesUploader Get()
        {
            if (m_Uploader == null)
                throw new InvalidOperationException(
                    "The global shader variables uploader is only available while the render graph executes. " +
                    "Access it from SetRenderFunc().");

            return m_Uploader;
        }

        public override void Reset()
        {
            m_Uploader = null;
            useConstantBuffers = false;
        }
    }
}