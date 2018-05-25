using System.Linq;
using UnityEngine;
using System.IO;

namespace UnityEditor.Experimental.Rendering
{
    static class CoreShaderIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            var paths = new string[1];
            paths[0] = Path.GetFullPath("Packages/com.unity.render-pipelines.core");
            return paths;
        }
    }
}
