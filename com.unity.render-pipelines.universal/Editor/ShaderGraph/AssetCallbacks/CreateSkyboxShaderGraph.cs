using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateSkyboxShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Skybox Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2)]
        public static void CreateUnlitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalSkyboxSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.SurfaceDescription.BaseColor,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }
    }
}
