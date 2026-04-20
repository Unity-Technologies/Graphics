using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    // TODO: Remove this class once Dynamic Batching has been fully removed.
    static class UniversalRenderPipelineBatchingWarning
    {
        const string k_SessionKey = "URPDynamicBatchingWarningShown";

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (SessionState.GetBool(k_SessionKey, false))
                return;

            RenderPipelineManager.activeRenderPipelineCreated += WarnIfDynamicBatchingEnabled;
        }

        static void WarnIfDynamicBatchingEnabled()
        {
            RenderPipelineManager.activeRenderPipelineCreated -= WarnIfDynamicBatchingEnabled;

            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
                return;

            SessionState.SetBool(k_SessionKey, true);

#pragma warning disable 618
            if (urpAsset.supportsDynamicBatching)
            {
                Debug.LogWarning("Dynamic Batching is deprecated and will be removed in a future release. Use SRP Batcher or GPU Instancing instead. Disable Dynamic Batching in the Universal Render Pipeline Asset to remove this warning.");
            }
#pragma warning restore 618
        }
    }
}
