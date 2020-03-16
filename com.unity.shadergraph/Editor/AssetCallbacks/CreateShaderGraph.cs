using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class CreateShaderGraph
    {
        [MenuItem("Assets/Create/Shader/Mesh Shader Graph", false, 208)]
        public static void CreateMeshShaderGraph()
        {
            GraphUtil.CreateNewGraph<MeshTarget>();
        }

        [MenuItem("Assets/Create/Shader/VFX Shader Graph", false, 208)]
        public static void CreateVfxShaderGraph()
        {
            GraphUtil.CreateNewGraph<VFXTarget>();
        }
    }
}
