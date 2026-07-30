using System;

namespace UnityEngine.Rendering.Universal
{
    // This contextItem is NOT used by URP 3D Renderer nor URP 2D Renderer, but only by potential custom ScriptableRenderer instances.
    // - Use UniversalGlobalShaderData instead for URP 3D Renderer.
    // - Use Universal2DGlobalShaderData instead for URP 2D Renderer.
    internal class DefaultGlobalShaderData : ContextItem
    {
        internal (BaseGlobalShaderVars bottomLeftUV, BaseGlobalShaderVars topLeftUV) m_GlobalShaderVars;

        // Returns the stored GlobalShaderVars by ref-readonly so callers with in parameter can avoid its copy, no modification possible
        internal ref readonly BaseGlobalShaderVars GetDefaultVars(bool useUVBottomLeft = true)
        {
            if (useUVBottomLeft)
                return ref m_GlobalShaderVars.bottomLeftUV;
            if (SystemInfo.graphicsUVStartsAtTop)
                return ref m_GlobalShaderVars.topLeftUV;
            throw new ArgumentException("UV always starts from the bottom for this API, use useUVBottomLeft = true");
        }

        public override void Reset()
        {
            m_GlobalShaderVars = (default, default);
        }
    }
}
