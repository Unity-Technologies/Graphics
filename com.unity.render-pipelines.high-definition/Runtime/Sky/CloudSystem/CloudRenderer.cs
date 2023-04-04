namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Parameters to override sun light cookie.
    /// </summary>
    public struct CookieParameters
    {
        /// <summary>The 2D cookie texture to use.</summary>
        public Texture texture;
        /// <summary>The size of the projected cookie texture in pixels.</summary>
        public Vector2 size;
        /// <summary>The world space position to use as projection origin.</summary>
        public Vector3 position;
    }

    /// <summary>
    /// Base class for cloud rendering.
    /// </summary>
    public abstract class CloudRenderer
    {
        int m_LastFrameUpdate = -1;

        /// <summary>Determines if the clouds should be rendered when the sun light changes.</summary>
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
        /// Get the parameters for overriding the main directional light cookie for one frame.
        /// </summary>
        /// <param name="settings">Current cloud settings.</param>
        /// <param name="cookieParams">Overriden values for cookie parameters.</param>
        /// <returns>True if the cookie should be overriden and RenderSunLightCookie should be called.</returns>
        public virtual bool GetSunLightCookieParameters(CloudSettings settings, ref CookieParameters cookieParams) { return false; }

        /// <summary>
        /// HDRP calls this function once every frame where GetSunLightCookieParameters returns true.
        /// Implement it if your CloudRenderer needs to render a texture to use for the light cookie (for example for cloud shadow rendering).
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the sun light cookie.</param>
        public virtual void RenderSunLightCookie(BuiltinSunCookieParameters builtinParams) { }

        /// <summary>
        /// HDRP calls this function once every frame. Implement it if your CloudRenderer needs to iterate independently of the user defined update frequency (see CloudSettings UpdateMode).
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to update the clouds.</param>
        /// <returns>True if the update determines that cloud lighting needs to be re-rendered. False otherwise.</returns>
        protected virtual bool Update(BuiltinSkyParameters builtinParams) { return false; }

        /// <summary>
        /// Preprocess for rendering the clouds. Called before the DepthPrePass operations
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the clouds.</param>
        /// <param name="renderForCubemap">Pass in true if you want to render the clouds into a cubemap for lighting. This is useful when the cloud renderer needs a different implementation in this case.</param>
        public virtual void PreRenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap) { }

        /// <summary>
        /// Whether the PreRenderClouds step is required.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the clouds.</param>
        /// <returns>True if the PreRenderClouds step is required.</returns>
        public virtual bool RequiresPreRenderClouds(BuiltinSkyParameters builtinParams) { return false; }

        /// <summary>
        /// Implements actual rendering of the clouds. HDRP calls this when rendering the clouds into a cubemap (for lighting) and also during main frame rendering.
        /// </summary>
        /// <param name="builtinParams">Engine parameters that you can use to render the clouds.</param>
        /// <param name="renderForCubemap">Pass in true if you want to render the clouds into a cubemap for lighting. This is useful when the cloud renderer needs a different implementation in this case.</param>
        public abstract void RenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap);

        internal bool DoUpdate(BuiltinSkyParameters parameters)
        {
            if (m_LastFrameUpdate < parameters.frameIndex)
            {
                m_LastFrameUpdate = parameters.frameIndex;
                return Update(parameters);
            }

            return false;
        }

        internal void Reset()
        {
            m_LastFrameUpdate = -1;
        }

    }
}
