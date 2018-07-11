using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.unity.visualeffectgraph.EditorTests")]
[assembly: InternalsVisibleTo("com.unity.visualeffectgraph.EditorTests-testable")]

namespace UnityEditor.VFX
{

public static class VisualEffectGraphPackageInfo
{
    static string m_PackagePath;

        public static string fileSystemPackagePath
        {
            get
            {
                if (m_PackagePath == null)
                {
                    foreach (var pkg in UnityEditor.PackageManager.Packages.GetAll())
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
