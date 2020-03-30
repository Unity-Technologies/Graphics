using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    enum RaytracingMaterialType
    {
        Unlit,
        Lit,
        Fabric,
    }

    class HDRaytracingMeshTarget : ITargetImplementation
    {
        [SerializeField]
        RaytracingMaterialType m_MaterialType;

        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP Raytracing";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph/Templates";
        public string renderTypeTag { get; }
        public string renderQueueTag { get; }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("a3b60b90b9eb3e549adfd57a75e77811")); // HDRPRaytracingMeshTarget

            switch(m_MaterialType)
            {
                case RaytracingMaterialType.Unlit:
                    context.AddSubShader(HDSubShaders.HDUnlitRaytracing);
                    break;
                case RaytracingMaterialType.Lit:
                    context.AddSubShader(HDSubShaders.HDLitRaytracing);
                    break;
                case RaytracingMaterialType.Fabric:
                    context.AddSubShader(HDSubShaders.FabricRaytracing);
                    break;
            }
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
