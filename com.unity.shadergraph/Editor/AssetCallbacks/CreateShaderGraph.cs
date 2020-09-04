using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class CreateShaderGraph
    {
        [MenuItem("Assets/Create/Shader/Blank Shader Graph", false, 208)]
        public static void CreateBlankShaderGraph()
        {
            GraphUtil.CreateNewGraph();
        }
    }
}
