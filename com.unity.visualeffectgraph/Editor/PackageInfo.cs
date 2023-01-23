using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceEditorTests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceEditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceRuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceRuntimeTests-testable")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor-testable")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor-testable")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace UnityEditor.VFX
{
    static class VisualEffectGraphPackageInfo
    {
        public static string assetPackagePath
        {
            get
            {
                return "Packages/com.unity.visualeffectgraph";
            }
        }
    }
}
