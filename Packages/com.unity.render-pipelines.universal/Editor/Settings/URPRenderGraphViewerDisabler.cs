using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class URPRenderGraphViewerDisabler
    {
        // Disable the Render Graph Viewer menu item if URP is active and RenderGraph is disabled.
        [MenuItem("Window/Analysis/Render Graph Viewer", true)]
        static bool ValidateRenderGraphViewer()
        {
            if (RenderPipelineManager.currentPipeline is not UniversalRenderPipeline)
                return true;

            var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            return settings != null && !settings.enableRenderCompatibilityMode;
        }
    }
}
