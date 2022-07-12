using UnityEngine.TestTools.Graphics;

public class GlobalGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public GlobalGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 1920;
        ImageComparisonSettings.TargetHeight = 1080;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.0015f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.00015f;
    }
}
