using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    static class CreateUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/BuiltIn/Unlit Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateUnlitGraph()
        {
            var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            target.TrySetActiveSubTarget(typeof(BuiltInUnlitSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
