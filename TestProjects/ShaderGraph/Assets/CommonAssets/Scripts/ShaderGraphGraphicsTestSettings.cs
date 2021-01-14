using UnityEngine.TestTools.Graphics;

public class ShaderGraphGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;
    [UnityEngine.HideInInspector]
    public int TargetWidth;
    [UnityEngine.HideInInspector]
    public int TargetHeight;

    public ShaderGraphGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }

    void Start()
    {
        TargetWidth = ImageComparisonSettings.TargetWidth;
        TargetHeight = ImageComparisonSettings.TargetHeight;
    }
}
