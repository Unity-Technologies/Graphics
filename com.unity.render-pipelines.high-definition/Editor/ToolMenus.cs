using UnityEditor.Rendering.Utilities;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ToolMenus
    {
        [MenuItemForRenderPipeline("Window/Rendering/Look Dev", false, 10001, typeof(HDRenderPipelineAsset))]
        static void OpenLookDev()
            => LookDev.LookDev.Open();

        [MenuItemForRenderPipeline("Window/Analysis/Render Graph Viewer", false, 10006, typeof(HDRenderPipelineAsset))]
        static void OpenRenderGraphViewer()
            => RenderGraphModule.RenderGraphViewer.Open();
    }
}
