using System;

namespace UnityEngine.Rendering.Universal
{
    internal class UniversalGlobalShaderData : ContextItem
    {
        #region Internal
        internal (GlobalShaderVars bottomLeftUV, GlobalShaderVars topLeftUV) m_GlobalShaderVars;

        #endregion

        // URP CB TIER 2 WIP
        // #region Public API

        /// <summary>
        /// Determines whether to use persistent constant buffers or fill dynamic constant buffers with loose uniforms APIs (cmd.SetGlobalFloat, ...)
        /// </summary>
        internal bool useConstantBuffers { get; set; }

        // Returns the stored GlobalShaderVars by ref-readonly so callers with in parameter can avoid its copy, no modification possible
        internal ref readonly GlobalShaderVars GetDefaultVars(bool useUVBottomLeft = true)
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
            useConstantBuffers = false;
        }
        //#endregion
    }
}
