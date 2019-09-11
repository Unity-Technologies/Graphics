using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class CreateShaderGraph
    {
        [MenuItem("Assets/Create/Shader/Unlit Graph", false, 208)]
        public static void CreateUnlitMasterMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new UnlitMasterNode());
        }

        [MenuItem("Assets/Create/Shader/PBR Graph", false, 208)]
        public static void CreatePBRMasterMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new PBRMasterNode());
        }

        [MenuItem("Assets/Create/Shader/VFX Shader Graph", false, 208)]
        public static void CreateVfxShaderGraph()
        {
            GraphUtil.CreateNewGraph(new VfxMasterNode());
        }
    }
}
