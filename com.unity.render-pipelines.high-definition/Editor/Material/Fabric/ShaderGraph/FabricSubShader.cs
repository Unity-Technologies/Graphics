using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Data.Util;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.FabricSubShader")]
    class FabricSubShader : ISubShader
    {
        private static ActiveFields GetActiveFieldsFromMasterNode(FabricMasterNode masterNode, ShaderPass pass)
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
                case FabricMasterNode.MaterialType.CottonWool:
                    baseActiveFields.Add("Material.CottonWool");
                    break;
                case FabricMasterNode.MaterialType.Silk:
                    baseActiveFields.Add("Material.Silk");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                if (pass.pixelPorts.Contains(FabricMasterNode.AlphaClipThresholdSlotId))
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
                baseActiveFields.Add("AddPrecomputedVelocity");
            }

            if (masterNode.energyConservingSpecular.isOn)
            {
                baseActiveFields.Add("Specular.EnergyConserving");
            }

            if (masterNode.transmission.isOn)
            {
                baseActiveFields.Add("Material.Transmission");
            }

            if (masterNode.subsurfaceScattering.isOn && masterNode.surfaceType != SurfaceType.Transparent)
            {
                baseActiveFields.Add("Material.SubsurfaceScattering");
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.BentNormalSlotId) && pass.pixelPorts.Contains(FabricMasterNode.BentNormalSlotId))
            {
                baseActiveFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.TangentSlotId) && pass.pixelPorts.Contains(FabricMasterNode.TangentSlotId))
            {
                baseActiveFields.Add("Tangent");
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

            if (pass.pixelPorts.Contains(FabricMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(FabricMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(FabricMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(FabricMasterNode.LightingSlotId) && pass.pixelPorts.Contains(FabricMasterNode.LightingSlotId))
            {
                baseActiveFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(FabricMasterNode.BackLightingSlotId) && pass.pixelPorts.Contains(FabricMasterNode.BackLightingSlotId))
            {
                baseActiveFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.pixelPorts.Contains(FabricMasterNode.DepthOffsetSlotId))
                baseActiveFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassLit(FabricMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;

            else if(pass.Equals(HDRPMeshTarget.Passes.FabricForwardOnlyOpaque) || pass.Equals(HDRPMeshTarget.Passes.FabricForwardOnlyTransparent))
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
                // FabricSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("059cc3132f0336e40886300f3d2d7f12"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as FabricMasterNode;
            var subShader = new ShaderGenerator();

            if(target is HDRPRaytracingMeshTarget)
            {
                if(mode == GenerationMode.ForReals)
                {
                    subShader.AddShaderChunk("SubShader", false);
                    subShader.AddShaderChunk("{", false);
                    subShader.Indent();
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.FabricIndirect, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.FabricVisibility, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.FabricForward, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassLit(masterNode, target, HDRPRaytracingMeshTarget.Passes.FabricGBuffer, mode, subShader, sourceAssetDependencyPaths);
                    }
                    subShader.Deindent();
                    subShader.AddShaderChunk("}", false);
                }
            }
            else
            {
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

                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricMETA, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricSceneSelection, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                    if(opaque)
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricForwardOnlyOpaque, mode, subShader, sourceAssetDependencyPaths);
                    }
                    else
                    {
                        GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.FabricForwardOnlyTransparent, mode, subShader, sourceAssetDependencyPaths);
                    }
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", true);
            }
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.FabricGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
