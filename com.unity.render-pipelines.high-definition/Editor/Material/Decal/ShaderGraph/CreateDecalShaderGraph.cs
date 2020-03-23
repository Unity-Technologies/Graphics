using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateDecalShaderGraph
    {
        // TODO: This should be in the ShaderGraph codebase?
        // TODO: DecalTarget is defined there but URP has no implementation
        [MenuItem("Assets/Create/Shader/HDRP/Decal Shader Graph", false, 208)]
        public static void CreateDecalGraph()
        {
            GraphUtil.CreateNewGraph<DecalTarget>();
        }
    }
}
