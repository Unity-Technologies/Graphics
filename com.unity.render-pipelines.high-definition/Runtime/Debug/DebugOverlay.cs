namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for debug overlay coordinates.
    /// </summary>
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
        /// Increment coordinates to the next overlay.
        /// </summary>
        public void Next()
        {
            x += overlaySize;
            // Go to next line if it goes outside the screen.
            if ((x + overlaySize) > m_ScreenWidth)
            {
                x = m_InitialPositionX;
                y -= overlaySize;
            }
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
