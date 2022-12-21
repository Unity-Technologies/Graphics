namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Base class for sky rendering.
    /// </summary>
    public abstract class SkyRenderer
    {
        int m_LastFrameUpdate = -1;

        /// <summary>Determines if the sky should be rendered when the sun light changes.</summary>
        public bool SupportDynamicSunLight = true;

        /// <summary>
        /// Called on startup. Create resources used by the renderer (shaders, materials, etc).
        /// </summary>
        public abstract void Build();

        /// <summary>
        /// Called on cleanup. Release resources used by the renderer.
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// HDRP calls this function once every frame. Implement it if your SkyRenderer needs to iterate independently of the user defined update frequency (see SkySettings UpdateMode).
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to update the sky.</param>
        /// <returns>True if the update determines that sky lighting needs to be re-rendered. False otherwise.</returns>
        protected virtual bool Update(BuiltinSkyParameters builtinParams) { return false; }

        /// <summary>
        /// Preprocess for rendering the sky. Called before the DepthPrePass operations
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        /// <param name="renderForCubemap">Pass in true if you want to render the sky into a cubemap for lighting. This is useful when the sky renderer needs a different implementation in this case.</param>
        /// <param name="renderSunDisk">If the sky renderer supports the rendering of a sun disk, it must not render it if this is set to false.</param>
        [System.Obsolete("Please override PreRenderSky(BuiltinSkyParameters) instead.")]
        public virtual void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            PreRenderSky(builtinParams);
        }

        /// <summary>
        /// Preprocess for rendering the sky. Called before the DepthPrePass operations
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        public virtual void PreRenderSky(BuiltinSkyParameters builtinParams) { }


        /// <summary>
        /// Whether the PreRenderSky step is required.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        /// <returns>True if the PreRenderSky step is required.</returns>
        [System.Obsolete("Please implement RequiresPreRender instead")]
        public virtual bool RequiresPreRenderSky(BuiltinSkyParameters builtinParams) { return false; }

        /// <summary>
        /// Whether the PreRenderSky step is required or not.
        /// </summary>
        /// <param name="skySettings">Sky setting for the current sky.</param>
        /// <returns>True if the sky needs a pre-render pass.</returns>
        public virtual bool RequiresPreRender(SkySettings skySettings) { return false; }


        /// <summary>
        /// Implements actual rendering of the sky. HDRP calls this when rendering the sky into a cubemap (for lighting) and also during main frame rendering.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        /// <param name="renderForCubemap">Pass in true if you want to render the sky into a cubemap for lighting. This is useful when the sky renderer needs a different implementation in this case.</param>
        /// <param name="renderSunDisk">If the sky renderer supports the rendering of a sun disk, it must not render it if this is set to false.</param>
        public abstract void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk);

        /// <summary>
        /// Returns exposure setting for the provided SkySettings.
        /// </summary>
        /// <param name="skySettings">SkySettings for which exposure is required.</param>
        /// <param name="debugSettings">Current debug display settings</param>
        /// <returns>Returns SkySetting exposure.</returns>
        protected static float GetSkyIntensity(SkySettings skySettings, DebugDisplaySettings debugSettings)
        {
            return skySettings.GetIntensityFromSettings();
        }

        /// <summary>
        /// Setup global parameters for the sky renderer.
        /// </summary>
        /// <param name="cmd">Command buffer provided to setup shader constants.</param>
        /// <param name="builtinParams">Sky system builtin parameters.</param>
        public virtual void SetGlobalSkyData(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
        }

        internal bool DoUpdate(BuiltinSkyParameters parameters)
        {
            if (m_LastFrameUpdate < parameters.frameIndex)
            {
                // Here we need a temporary command buffer to be executed because this is called during render graph construction.
                // This means that we don't have a proper command buffer to provide unless in a render graph pass.
                // Besides, we need this function to be executed immediately to retrieve the return value so it cannot be executed later as a proper render graph pass.
                var previousCommandBuffer = parameters.commandBuffer;
                var commandBuffer = CommandBufferPool.Get("SkyUpdate");
                parameters.commandBuffer = commandBuffer;
                m_LastFrameUpdate = parameters.frameIndex;
                var result = Update(parameters);
                Graphics.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
                parameters.commandBuffer = previousCommandBuffer;
                return result;
            }

            return false;
        }

        internal void Reset()
        {
            m_LastFrameUpdate = -1;
        }
    }
}
