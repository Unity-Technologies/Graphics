using UnityEngine.TestTools.Graphics;

public class UniversalSGGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public UniversalSGGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 640;
        ImageComparisonSettings.TargetHeight = 360;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.001f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }
}
