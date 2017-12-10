using System.Linq;
using UnityEngine;
using System.IO;

namespace UnityEditor.Experimental.Rendering
{
    static class LightweightIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                Path.GetFullPath("Packages/com.unity.render-pipelines.lightweight/Shaders"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.high-quality/Material/Unlit")
            };
        }
    }
}
