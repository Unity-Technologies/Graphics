using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal static class DefaultShaderIncludes
    {
        public static string GetAssetsPackagePath()
        {
            var packageDirectories = Directory.GetDirectories(Application.dataPath, "com.unity.shadergraph", SearchOption.AllDirectories);
            return packageDirectories.Length == 0 ? null : Path.GetFullPath(packageDirectories.First());
        }
    }
}
