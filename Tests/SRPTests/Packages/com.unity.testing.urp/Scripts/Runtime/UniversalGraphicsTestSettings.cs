using UnityEngine;
using UnityEngine.TestTools.Graphics;

public class UniversalGraphicsTestSettings : GraphicsTestSettings
{
    public int WaitFrames = 0;
    public bool XRCompatible = true;
    public bool gpuDrivenCompatible = true;
    public bool CheckMemoryAllocation = true;

    [System.Serializable]
    public enum RenderBackendCompatibility
    {
        RenderGraph,
        NonRenderGraph,
        RenderGraphAndNonRenderGraph
    }
    public RenderBackendCompatibility renderBackendCompatibility = RenderBackendCompatibility.RenderGraphAndNonRenderGraph;

    [HideInInspector]
    public bool SetBackBufferResolution = false;

    public UniversalGraphicsTestSettings()
    {
        ImageComparisonSettings.TargetWidth = 512;
        ImageComparisonSettings.TargetHeight = 512;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.005f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.001f;
        ImageComparisonSettings.UseBackBuffer = false;
    }
}
