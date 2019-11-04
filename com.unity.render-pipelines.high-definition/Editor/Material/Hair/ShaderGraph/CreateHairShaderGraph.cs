using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateHairShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Hair Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new HairMasterNode());
        }
    }
}
