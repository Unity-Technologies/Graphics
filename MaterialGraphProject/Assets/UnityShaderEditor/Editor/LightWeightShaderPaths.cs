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
                Path.GetFullPath("Assets"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.core/ShaderLibrary"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.lightweight/Shaders"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.high-quality/Material/Unlit")
            };
        }
    }
}
