using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateWaterShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/HDRP/Water Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        public static void CreateWaterGraph()
        {
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(WaterSubTarget));

            var blockDescriptors = new[]
            {
                // Vertex shader
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                HDBlockFields.VertexDescription.UV0,
                HDBlockFields.VertexDescription.UV1,

                // Fragment shader
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalWS,
                WaterSubTarget.WaterBlocks.LowFrequencyNormalWS,
                BlockFields.SurfaceDescription.Smoothness,
                WaterSubTarget.WaterBlocks.Foam,
                WaterSubTarget.WaterBlocks.Caustics,
                WaterSubTarget.WaterBlocks.TipThickness,
                BlockFields.SurfaceDescription.Alpha,

            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
