namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for debug overlay coordinates.
    /// </summary>
    [System.Obsolete("Please use UnityEngine.Rendering.DebugOverlay")]
    public class DebugOverlay
    {
        /// <summary>Current x coordinate.</summary>
        public int x { get; private set; }
        /// <summary>Current y coordinate.</summary>
        public int y { get; private set; }
        /// <summary>Current overlay size.</summary>
        public int overlaySize { get; private set; }

        int m_InitialPositionX;
        int m_ScreenWidth;

        /// <summary>
        /// Start rendering overlay.
        /// </summary>
        /// <param name="initialX">Initial x position.</param>
        /// <param name="initialY">Initial y position.</param>
        /// <param name="overlaySize">Size of overlays between 0 and 1.</param>
        /// <param name="screenWidth">Width of the screen.</param>
        public void StartOverlay(int initialX, int initialY, int overlaySize, int screenWidth)
        {
            x = initialX;
            y = initialY;
            this.overlaySize = overlaySize;

            m_InitialPositionX = initialX;
            m_ScreenWidth = screenWidth;
        }

        /// <summary>
        /// Increment coordinates to the next overlay and return the current overlay rect.
        /// </summary>
        /// <param name="aspect">Aspect of the current overlay.</param>
        /// <returns>Returns a rect of the current overlay.</returns>
        public Rect Next(float aspect = 1.0f)
        {
            int overlayWidth = (int)(overlaySize * aspect);

            if ((x + overlayWidth) > m_ScreenWidth && x > m_InitialPositionX)
            {
                x = m_InitialPositionX;
                y -= overlaySize;
            }

            Rect rect = new Rect(x, y, overlayWidth, overlaySize);

            x += overlayWidth;

            return rect;
        }

        /// <summary>
        /// Setup the viewport for the current overlay.
        /// </summary>
        /// <param name="cmd">Command buffer used to setup viewport.</param>
        public void SetViewport(CommandBuffer cmd)
        {
            cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
        }
    }
}
