namespace UnityEngine.Rendering.Universal
{
    internal class UniversalGlobalShaderData : ContextItem
    {
        #region Internal
        internal GlobalShaderVars m_GlobalShaderVars;

        #endregion

        // URP CB TIER 2 WIP
        // #region Public API

        /// <summary>
        /// Determines whether to use persistent constant buffers or fill dynamic constant buffers with loose uniforms APIs (cmd.SetGlobalFloat, ...)
        /// </summary>
        internal bool useConstantBuffers { get; set; }

        // Returns the stored GlobalShaderVars by ref-readonly so callers with in parameter can avoid its copy, no modification possible
        internal ref readonly GlobalShaderVars GetDefaultVars() => ref m_GlobalShaderVars;

        public override void Reset()
        {
            m_GlobalShaderVars = default;
            useConstantBuffers = false;
        }
        //#endregion
    }
}
