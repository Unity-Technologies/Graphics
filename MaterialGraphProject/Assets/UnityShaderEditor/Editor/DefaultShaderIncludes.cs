namespace UnityEditor
{
    internal static class DefaultShaderIncludes
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                "Assets/ScriptableRenderPipeline/",
                "Assets/SRP/ScriptableRenderPipeline/LightweightPipeline/Shaders"
            };
        }
    }
}
