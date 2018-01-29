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
                Path.GetFullPath("Assets/UnityShaderEditor"),
                Path.GetFullPath("Packages/com.unity.shadergraph"),
            };
        }
    }
}
