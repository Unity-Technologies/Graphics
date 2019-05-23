using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    class CreateSpriteUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/2D Renderer/Sprite Unlit Graph (Experimental)", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new SpriteUnlitMasterNode());
        }
    }
}
