using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    static class LightweightIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            var paths = new string[2];
            paths[0] = Path.GetFullPath("Packages/com.unity.visualeffectgraph");
            paths[1] = Path.GetFullPath("Packages/com.unity.visualeffectgraph/VisualEffectGraph");
            return paths;
        }
    }
}
