using System;

namespace UnityEngine.Rendering.Universal
{
    // Lives in the core Runtime assembly rather than the 2D one (like Universal2DResourceData) because passes shared
    // by both renderers need to reach it, and the 2D assembly is the one referencing this assembly, not the reverse.
    internal class Universal2DGlobalShaderData : ContextItem
    {
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
        }
    }
}