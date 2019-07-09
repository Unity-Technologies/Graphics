using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateFabricShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Fabric Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new FabricMasterNode());
        }
    }
}
