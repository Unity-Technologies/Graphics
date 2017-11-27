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
            return new[]
            {
                "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/Core",
                Path.GetFullPath("Packages/com.unity.render-pipelines.core")
            };
        }
    }
}
