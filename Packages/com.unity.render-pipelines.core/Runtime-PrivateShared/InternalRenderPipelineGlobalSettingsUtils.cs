using UnityEngine.Rendering;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
#if UNITY_EDITOR
    internal class InternalRenderPipelineGlobalSettingsUtils
    {
        internal static bool TryMigrateRenderingLayersToTagManager<T>(string[] renderingLayerNames)
            where T : RenderPipeline
        {
            return Bridge.RenderPipelineEditorUtilityBridge.TryMigrateRenderingLayersToTagManager<T>(renderingLayerNames);
        }

        internal static void ClearMigratedRenderPipelines()
        {
            Bridge.RenderPipelineEditorUtilityBridge.ClearMigratedRenderPipelines();
        }
    }
#endif
}
