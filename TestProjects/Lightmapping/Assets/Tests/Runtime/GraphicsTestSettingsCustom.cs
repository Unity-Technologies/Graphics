//This script is copied from UniversalRP TestProject
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/UniversalGraphicsTest/Assets/Test/Runtime/UniversalGraphicsTestSettings.cs

using UnityEngine.TestTools.Graphics;

public class GraphicsTestSettingsCustom : GraphicsTestSettings
{
    public int WaitFrames = 0;

    public GraphicsTestSettingsCustom()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.001f;
    }
}
