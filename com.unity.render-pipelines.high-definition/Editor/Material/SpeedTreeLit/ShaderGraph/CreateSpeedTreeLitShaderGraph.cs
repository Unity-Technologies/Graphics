using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateSpeedTreeLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/SpeedTree Lit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new SpeedTreeLitMasterNode());
        }
    }
}
