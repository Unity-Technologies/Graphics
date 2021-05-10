using UnityEngine.TestTools.Graphics;

public class UniversalGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;
    public bool XRCompatible = true;

    public UniversalGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.001f;
        ImageComparisonSettings.UseBackBuffer = false;
    }
}
