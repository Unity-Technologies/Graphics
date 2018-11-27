namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal partial class HDReflectionProbeUI : HDProbeUI
    {
        internal HDReflectionProbeUI()
        {
            toolBars = new[] { ToolBar.InfluenceShape | ToolBar.Blend | ToolBar.NormalBlend, ToolBar.CapturePosition };
        }
    }
}
