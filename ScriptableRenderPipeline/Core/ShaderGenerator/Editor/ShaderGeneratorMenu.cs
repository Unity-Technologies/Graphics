using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public class ShaderGeneratorMenu
    {
        [UnityEditor.MenuItem("Edit/Render Pipeline/Tools/Generate Shader Includes", priority = CoreUtils.editMenuPriority1)]
        static void GenerateShaderIncludes()
        {
            CSharpToHLSL.GenerateAll();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
