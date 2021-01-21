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
        public virtual void PreRenderSky(BuiltinSkyParameters builtinParams) {}


        /// <summary>
        /// Whether the PreRenderSky step is required.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sky.</param>
        /// <returns>True if the PreRenderSky step is required.</returns>
        public virtual bool RequiresPreRenderSky(BuiltinSkyParameters builtinParams) { return false; }


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
            float skyIntensity = 1.0f;

            switch(skySettings.skyIntensityMode.value)
            {
                case SkyIntensityMode.Exposure:
                    // Note: Here we use EV100 of sky as a multiplier, so it is the opposite of when use with a Camera
                    // because for sky/light, higher EV mean brighter, but for camera higher EV mean darker scene
                    skyIntensity *= ColorUtils.ConvertEV100ToExposure(-skySettings.exposure.value);
                    break;
                case SkyIntensityMode.Multiplier:
                    skyIntensity *= skySettings.multiplier.value;
                    break;
                case SkyIntensityMode.Lux:
                    skyIntensity *= skySettings.desiredLuxValue.value / skySettings.upperHemisphereLuxValue.value;
                    break;
            }

            return skyIntensity;
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
                m_LastFrameUpdate = parameters.frameIndex;
                return Update(parameters);
            }

            return false;
        }
    }
}
