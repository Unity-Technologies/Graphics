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
        static string m_PackagePath;

        public static string fileSystemPackagePath
        {
            get
            {
                if (m_PackagePath == null)
                {
                    foreach (var pkg in UnityEditor.PackageManager.PackageInfo.GetAll())
                    {
                        if (pkg.name == "com.unity.visualeffectgraph")
                        {
                            m_PackagePath = pkg.resolvedPath.Replace("\\", "/");
                            break;
                        }
                    }
                }
                return m_PackagePath;
            }
        }
        public static string assetPackagePath
        {
            get
            {
                return "Packages/com.unity.visualeffectgraph";
            }
        }
    }
}
