using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateEyeShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Eye Graph (Experimental)", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new EyeMasterNode());
        }
    }
}
