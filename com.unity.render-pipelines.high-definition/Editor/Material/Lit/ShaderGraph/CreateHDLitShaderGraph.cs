using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateHDLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Lit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new HDLitMasterNode());
        }
    }
}
