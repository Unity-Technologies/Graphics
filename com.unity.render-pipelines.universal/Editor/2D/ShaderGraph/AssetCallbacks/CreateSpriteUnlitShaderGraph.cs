using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateSpriteUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Sprite Unlit Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateSpriteUnlitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalSpriteUnlitSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }
    }
}
