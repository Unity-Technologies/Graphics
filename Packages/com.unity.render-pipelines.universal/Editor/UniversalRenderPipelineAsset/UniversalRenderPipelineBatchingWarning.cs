using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    // TODO: Remove this class once Dynamic Batching has been fully removed.
    static class UniversalRenderPipelineBatchingWarning
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            RenderPipelineManager.activeRenderPipelineCreated += WarnIfDynamicBatchingEnabled;
        }

        static void WarnIfDynamicBatchingEnabled()
        {
            RenderPipelineManager.activeRenderPipelineCreated -= WarnIfDynamicBatchingEnabled;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
                return;

            var serializedAsset = new UnityEditor.SerializedObject(urpAsset);
            var supportsDynamicBatching = serializedAsset.FindProperty("m_SupportsDynamicBatching");
            if (supportsDynamicBatching != null && supportsDynamicBatching.boolValue)
            {
                Debug.LogError("Dynamic Batching has been removed and no longer has any effect. Use SRP Batcher or GPU Instancing instead. Disable Dynamic Batching in the Universal Render Pipeline Asset to remove this error.");
            }
        }
    }
}
