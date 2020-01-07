using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ShaderGeneratorMenu
    {
        [MenuItem("Edit/Render Pipeline/Generate Shader Includes", priority = CoreUtils.editMenuPriority1)]
        static void GenerateShaderIncludes()
        {
            CSharpToHLSL.GenerateAll();
            AssetDatabase.Refresh();
        }
    }
}
