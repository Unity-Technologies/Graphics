using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateEyeShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Eye Shader Graph", false, 208)]
        public static void CreateEyeGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(EyeSubTarget));

            var blockDescriptors = new [] 
            { 
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS, 
                HDBlockFields.SurfaceDescription.IrisNormalTS, 
                HDBlockFields.SurfaceDescription.BentNormal,
                BlockFields.SurfaceDescription.Smoothness, 
                HDBlockFields.SurfaceDescription.IOR,
                BlockFields.SurfaceDescription.Occlusion,
                HDBlockFields.SurfaceDescription.Mask,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new [] {target}, blockDescriptors);
        }
    }
}
