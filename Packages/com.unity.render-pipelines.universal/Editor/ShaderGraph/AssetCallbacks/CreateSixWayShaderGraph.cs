using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class CreateSixWayShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Six Way Shader Graph", priority = CoreUtils.Sections.section6 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateLitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalSixWaySubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.MapRightTopBack,
                BlockFields.SurfaceDescription.MapLeftBottomFront,
                BlockFields.SurfaceDescription.AbsorptionStrength,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
