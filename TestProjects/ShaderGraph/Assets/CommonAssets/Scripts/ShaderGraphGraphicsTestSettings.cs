using UnityEngine.TestTools.Graphics;

public class ShaderGraphGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public ShaderGraphGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }
}
