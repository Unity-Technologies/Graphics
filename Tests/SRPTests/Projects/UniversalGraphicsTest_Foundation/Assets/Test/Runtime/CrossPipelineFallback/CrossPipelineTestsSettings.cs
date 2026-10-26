using UnityEngine.TestTools.Graphics;

public class CrossPipelineTestsSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public CrossPipelineTestsSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }
}
