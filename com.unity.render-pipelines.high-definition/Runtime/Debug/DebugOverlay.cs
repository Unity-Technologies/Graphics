namespace UnityEngine.Rendering.HighDefinition
{
    public class DebugOverlay
    {
        public int x { get; private set; }
        public int y { get; private set; }
        public int overlaySize { get; private set; }

        int m_InitialPositionX;
        int m_ScreenWidth;

        public void StartOverlay(int initialX, int initialY, int overlaySize, int screenWidth)
        {
            x = initialX;
            y = initialY;
            this.overlaySize = overlaySize;

            m_InitialPositionX = initialX;
            m_ScreenWidth = screenWidth;
        }

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

        public void SetViewport(CommandBuffer cmd)
        {
            cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
        }
    }
}
