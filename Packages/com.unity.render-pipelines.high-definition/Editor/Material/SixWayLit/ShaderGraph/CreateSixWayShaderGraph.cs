using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateSixWayShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Six Way Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 8)]
        public static void CreateSixWayGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(HDSixWaySubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.AbsorptionStrength,
                BlockFields.SurfaceDescription.MapRightTopBack,
                BlockFields.SurfaceDescription.MapLeftBottomFront,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
