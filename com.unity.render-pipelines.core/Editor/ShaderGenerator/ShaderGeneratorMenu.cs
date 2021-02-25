using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ShaderGeneratorMenu
    {
        [MenuItem("Edit/Render Pipeline/Generate Shader Includes", priority = CoreUtils.editMenuPriority1)]
        async static Task GenerateShaderIncludes()
        {
            await CSharpToHLSL.GenerateAll();
            AssetDatabase.Refresh();
        }
    }
}
