using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.Universal
{
    class CreateSpriteLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/2D Renderer/Sprite Lit Graph (Experimental)", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new SpriteLitMasterNode());
        }
    }
}
