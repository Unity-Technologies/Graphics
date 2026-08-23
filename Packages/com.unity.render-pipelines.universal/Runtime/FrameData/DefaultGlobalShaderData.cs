namespace UnityEngine.Rendering.Universal
{
    // This contextItem is NOT used by URP 3D Renderer nor URP 2D Renderer, but only by potential custom ScriptableRenderer instances.
    // - Use UniversalGlobalShaderData instead for URP 3D Renderer.
    // - Use Universal2DGlobalShaderData instead for URP 2D Renderer.
    internal class DefaultGlobalShaderData : ContextItem
    {
        // Owned and created by the renderer, see ScriptableRenderer.InitGlobalShaderVariablesBase.
        private GlobalShaderVariablesUploader m_Uploader;

        internal void Set(GlobalShaderVariablesUploader uploader) => m_Uploader = uploader;

        internal GlobalShaderVariablesUploader Get() => m_Uploader;

        public override void Reset()
        {
            m_Uploader = null;
        }
    }
}
