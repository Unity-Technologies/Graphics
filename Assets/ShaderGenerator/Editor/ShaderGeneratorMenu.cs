namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class ShaderGeneratorMenu
    {
        [UnityEditor.MenuItem("RenderPipeline/Generate Shader Includes")]
        static void GenerateShaderIncludes()
        {
            CSharpToHLSL.GenerateAll();
        }
    }
}
