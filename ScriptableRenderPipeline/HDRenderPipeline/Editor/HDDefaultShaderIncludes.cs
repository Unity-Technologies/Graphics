using UnityEditor.Experimental.Rendering.HDPipeline;

namespace UnityEditor
{
    internal static class HDDefaultShaderIncludes
    {
        static string s_RenderPipelinePath
        {
            get { return HDEditorUtils.GetScriptableRenderPipelinePath(); }
        }

        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                s_RenderPipelinePath
            };
        }
    }
}
