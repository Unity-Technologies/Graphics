namespace UnityEditor.Experimental.Rendering
{
    public class ShaderGeneratorMenu
    {
        [UnityEditor.MenuItem("RenderPipeline/Generate Shader Includes")]
        static void GenerateShaderIncludes()
        {
            CSharpToHLSL.GenerateAll();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
