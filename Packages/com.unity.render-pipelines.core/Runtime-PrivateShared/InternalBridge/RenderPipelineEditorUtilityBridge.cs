using UnityEngine.Rendering;

// /!\ Some API shared through this bridge are in Editor assembly.
// Be sure to not forget #if UNITY_EDITOR for them

namespace Unity.RenderPipelines.Core.Runtime.Shared.Bridge
{
#if UNITY_EDITOR
    internal static class RenderPipelineEditorUtilityBridge
    {
        internal static bool TryMigrateRenderingLayersToTagManager<T>(string[] renderingLayerNames)
            where T : RenderPipeline
        {
            return UnityEditor.Rendering.RenderPipelineEditorUtility.TryMigrateRenderingLayersToTagManager<T>(renderingLayerNames);
        }

        internal static void ClearMigratedRenderPipelines()
        {
            UnityEditor.Rendering.RenderPipelineEditorUtility.ClearMigratedRenderPipelines();
        }
    }
#endif
}
