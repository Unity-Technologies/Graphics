using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateStackLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HD Render Pipeline/StackLit Shader Graph", false, 208)]
        public static void CreateStackLitGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(StackLitSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.TangentTS,
                BlockFields.SurfaceDescription.Metallic,
                HDBlockFields.SurfaceDescription.DielectricIor,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }
    }
}
