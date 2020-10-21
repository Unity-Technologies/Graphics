using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor-testable")]

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
