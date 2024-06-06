using System;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class CreateWaterShaderGraph
    {
        // [MenuItem("Assets/Create/Shader Graph/HDRP/Water Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
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
                WaterSubTarget.WaterBlocks.RefractedPositionWS,
            }; 

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }

        class DoCreateNewWaterShaderGraph : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var shader = GraphicsSettings.GetRenderPipelineSettings<WaterSystemRuntimeResources>().waterPS;
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(shader), pathName);
            }
        }

        [MenuItem("Assets/Create/Shader Graph/HDRP/Water Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 6)]
        static void CreateWaterGraphCopy()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewWaterShaderGraph>(), "Water Shader Graph.shadergraph", null, null);
        }
    }
}
