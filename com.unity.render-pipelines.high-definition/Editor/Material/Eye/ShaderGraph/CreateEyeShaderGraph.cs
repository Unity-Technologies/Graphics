using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateEyeShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Eye Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 3)]
        public static void CreateEyeGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(EyeSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.IOR,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.Mask,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
