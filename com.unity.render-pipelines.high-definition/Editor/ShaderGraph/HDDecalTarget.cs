using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDDecalTarget : ITargetImplementation
    {
        public Type targetType => typeof(DecalTarget);
        public string displayName => "HDRP";
        public string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph/Templates";
        public string renderTypeTag { get; }
        public string renderQueueTag { get; }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("61d739b0177943f4d858e09ae4b69ea2")); // DecalTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("21bb2072667892445b27f3e9aad497af")); // HDRPDecalTarget

            context.AddSubShader(HDSubShaders.Decal);
        }

        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {

        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            return null;
        }

        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
        }

        public void ProcessPreviewMaterial(Material material)
        {
        }

        public VisualElement GetSettings(Action onChange)
        {
            return null;
        }
    }
}
