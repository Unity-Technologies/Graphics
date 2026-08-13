namespace UnityEngine.Rendering.Universal
{
    // This contextItem is NOT used by URP 3D Renderer nor URP 2D Renderer, but only by potential custom ScriptableRenderer instances.
    // - Use UniversalGlobalShaderData instead for URP 3D Renderer.
    // - Use Universal2DGlobalShaderData instead for URP 2D Renderer.
    internal class DefaultGlobalShaderData : ContextItem
    {
        internal BaseGlobalShaderVars m_GlobalShaderVars;

        // Returns the stored GlobalShaderVars by ref-readonly so callers with in parameter can avoid its copy, no modification possible
        internal ref readonly BaseGlobalShaderVars GetDefaultVars() => ref m_GlobalShaderVars;

        public override void Reset()
        {
            m_GlobalShaderVars = default;
        }
    }
}
