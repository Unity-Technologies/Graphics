using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ShaderGeneratorMenu
    {
        [MenuItem("Edit/Rendering/Generate Shader Includes", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.editMenuPriority + 1)]
        async static Task GenerateShaderIncludes()
        {
            await CSharpToHLSL.GenerateAll();
            AssetDatabase.Refresh();
        }
    }
}
