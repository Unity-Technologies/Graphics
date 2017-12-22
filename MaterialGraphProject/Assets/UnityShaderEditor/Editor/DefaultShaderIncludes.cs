using System.IO;

namespace UnityEditor
{
    internal static class DefaultShaderIncludes
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                Path.GetFullPath("Packages/com.unity.render-pipelines.core"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.lightweight/Shaders"),
                Path.GetFullPath("Packages/com.unity.render-pipelines.high-quality/Material/Unlit"),
                Path.GetFullPath("Assets/UnityShaderEditor/Editor"),
                Path.GetFullPath("Packages/com.unity.shadergraph/Editor"),
            };
        }
    }
}
