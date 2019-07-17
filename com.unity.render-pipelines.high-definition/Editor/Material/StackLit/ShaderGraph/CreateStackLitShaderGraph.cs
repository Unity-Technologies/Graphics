using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateStackLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/StackLit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new StackLitMasterNode());
        }
    }
}
