using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateDecalShaderGraph
    {
        [MenuItem("Assets/Create/Shader/Universal Render Pipeline/Decal Shader Graph", false, 50)]
        public static void CreateLitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalDecalSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.MAOSAlpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }
    }
}
