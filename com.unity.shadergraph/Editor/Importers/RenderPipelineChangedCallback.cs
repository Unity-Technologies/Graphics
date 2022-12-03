using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    static class RenderPipelineChangedCallback
    {
        internal static readonly string k_CustomDependencyKey = "ShaderGraph/RenderPipelineChanged";

        [InitializeOnLoadMethod]
        private static void RegisterSRPChangeCallback()
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged -= SRPChanged;
            RenderPipelineManager.activeRenderPipelineTypeChanged += SRPChanged;
        }

        static Hash128 ComputeCurrentRenderPipelineHash()
            => Hash128.Compute(GraphicsSettings.currentRenderPipeline?.GetType()?.FullName ?? "");

        static void SRPChanged()
            => AssetDatabase.RegisterCustomDependency(k_CustomDependencyKey,ComputeCurrentRenderPipelineHash());
    }
}
