using UnityEngine.TestTools.Graphics;

public class SmokeGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public SmokeGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }
}
