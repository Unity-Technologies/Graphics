using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Data.Util;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    class EyeSubShader : ISubShader
    {
        private static ActiveFields GetActiveFieldsFromMasterNode(EyeMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                if (pass.referenceName != "SHADERPASS_MOTION_VECTORS")  // HACK to get around lack of a good interpolator dependency system
                {                                                       // we need to be able to build interpolators using multiple input structs
                                                                        // also: should only require isFrontFace if Normals are required...
                    // Important: the following is used in SharedCode.template.hlsl for determining the normal flip mode
                    baseActiveFields.Add("FragInputs.isFrontFace");
                }
            }

            switch (masterNode.materialType)
            {
                case EyeMasterNode.MaterialType.Eye:
                    baseActiveFields.Add("Material.Eye");
                    break;
                case EyeMasterNode.MaterialType.EyeCinematic:
                    baseActiveFields.Add("Material.EyeCinematic");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                if (pass.pixelPorts.Contains(EyeMasterNode.AlphaClipThresholdSlotId))
                {
                    baseActiveFields.Add("AlphaTest");
                }
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    baseActiveFields.Add("AlphaFog");
                }

                if (masterNode.blendPreserveSpecular.isOn)
                {
                    baseActiveFields.Add("BlendMode.PreserveSpecular");
                }
            }

            if (!masterNode.receiveDecals.isOn)
            {
                baseActiveFields.Add("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                baseActiveFields.Add("DisableSSR");
            }

            if (masterNode.addPrecomputedVelocity.isOn)
            {
                baseActiveFields.Add("AdditionalVelocityChange");
            }

            if (masterNode.subsurfaceScattering.isOn && masterNode.surfaceType != SurfaceType.Transparent)
            {
                baseActiveFields.Add("Material.SubsurfaceScattering");
            }

            if (masterNode.IsSlotConnected(EyeMasterNode.BentNormalSlotId) && pass.pixelPorts.Contains(EyeMasterNode.BentNormalSlotId))
            {
                baseActiveFields.Add("BentNormal");
            }

            switch (masterNode.specularOcclusionMode)
            {
                case SpecularOcclusionMode.Off:
                    break;
                case SpecularOcclusionMode.FromAO:
                    baseActiveFields.Add("SpecularOcclusionFromAO");
                    break;
                case SpecularOcclusionMode.FromAOAndBentNormal:
                    baseActiveFields.Add("SpecularOcclusionFromAOBentNormal");
                    break;
                case SpecularOcclusionMode.Custom:
                    baseActiveFields.Add("SpecularOcclusionCustom");
                    break;
                default:
                    break;
            }

            if (pass.pixelPorts.Contains(EyeMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(EyeMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(EyeMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(EyeMasterNode.LightingSlotId) && pass.pixelPorts.Contains(EyeMasterNode.LightingSlotId))
            {
                baseActiveFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(EyeMasterNode.BackLightingSlotId) && pass.pixelPorts.Contains(EyeMasterNode.BackLightingSlotId))
            {
                baseActiveFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.pixelPorts.Contains(EyeMasterNode.DepthOffsetSlotId))
                baseActiveFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassEye(EyeMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;

            else if(pass.Equals(HDRPMeshTarget.Passes.EyeForwardOnlyOpaque) || pass.Equals(HDRPMeshTarget.Passes.EyeForwardOnlyTransparent))
            {
                if (masterNode.surfaceType == SurfaceType.Opaque && masterNode.alphaTest.isOn)
                {
                    pass.ZTestOverride = "ZTest Equal";
                }
            }
            
            // Active Fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);
            
            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                //EyeSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("951ab98b405c28447801dbe209dfc34f"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as EyeMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                // Add tags at the SubShader level
                var renderingPass = masterNode.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeSceneSelection, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                // Assign define here based on opaque or transparent to save some variant
                if(opaque)
                {
                    GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeForwardOnlyOpaque, mode, subShader, sourceAssetDependencyPaths);
                }
                else
                {
                    GenerateShaderPassEye(masterNode, target, HDRPMeshTarget.Passes.EyeForwardOnlyTransparent, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.EyeGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
