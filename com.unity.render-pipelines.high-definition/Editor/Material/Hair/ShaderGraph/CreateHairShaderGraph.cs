using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateHairShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Hair Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 4)]
        public static void CreateHairGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(HairSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.HairStrandDirection,
                HDBlockFields.SurfaceDescription.Transmittance,
                HDBlockFields.SurfaceDescription.RimTransmissionIntensity,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.SpecularTint,
                HDBlockFields.SurfaceDescription.SpecularShift,
                HDBlockFields.SurfaceDescription.SecondarySpecularTint,
                HDBlockFields.SurfaceDescription.SecondarySmoothness,
                HDBlockFields.SurfaceDescription.SecondarySpecularShift,
                BlockFields.SurfaceDescription.Emission,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
