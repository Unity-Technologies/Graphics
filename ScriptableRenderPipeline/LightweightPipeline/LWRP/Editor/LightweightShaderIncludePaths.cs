using System.IO;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    static class LightweightIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                Path.GetFullPath("Packages/com.unity.render-pipelines.lightweight/LWRP/Shaders"),
            };
        }
    }
}
