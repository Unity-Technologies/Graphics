using UnityEngine.TestTools.Graphics;


public class ShaderGraphIndividualTestSetting : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public ShaderGraphIndividualTestSetting()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.005f;
    }
}
