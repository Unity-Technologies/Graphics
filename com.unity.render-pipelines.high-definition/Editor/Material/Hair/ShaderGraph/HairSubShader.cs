using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Data.Util;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HairSubShader")]
    class HairSubShader : ISubShader
    {
        private static ActiveFields GetActiveFieldsFromMasterNode(HairMasterNode masterNode, ShaderPass pass)
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
                case HairMasterNode.MaterialType.KajiyaKay:
                    baseActiveFields.Add("Material.KajiyaKay");
                    break;

                default:
                    UnityEngine.Debug.LogError("Unknown material type: " + masterNode.materialType);
                    break;
            }

            if (masterNode.alphaTest.isOn)
            {
                int count = 0;

                // If alpha test shadow is enable, we use it, otherwise we use the regular test
                if (pass.pixelPorts.Contains(HairMasterNode.AlphaClipThresholdShadowSlotId) && masterNode.alphaTestShadow.isOn)
                {
                    baseActiveFields.Add("AlphaTestShadow");
                    ++count;
                }
                else if (pass.pixelPorts.Contains(HairMasterNode.AlphaClipThresholdSlotId))
                {
                    baseActiveFields.Add("AlphaTest");
                    ++count;
                }
                // Other alpha test are suppose to be alone
                else if (pass.pixelPorts.Contains(HairMasterNode.AlphaClipThresholdDepthPrepassSlotId))
                {
                    baseActiveFields.Add("AlphaTestPrepass");
                    ++count;
                }
                else if (pass.pixelPorts.Contains(HairMasterNode.AlphaClipThresholdDepthPostpassSlotId))
                {
                    baseActiveFields.Add("AlphaTestPostpass");
                    ++count;
                }
                UnityEngine.Debug.Assert(count == 1, "Alpha test value not set correctly");
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                if (masterNode.transparencyFog.isOn)
                {
                    baseActiveFields.Add("AlphaFog");
                }

                if (masterNode.transparentWritesMotionVec.isOn)
                {
                    baseActiveFields.Add("TransparentWritesMotionVec");
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

            if (masterNode.specularAA.isOn && pass.pixelPorts.Contains(HairMasterNode.SpecularAAThresholdSlotId) && pass.pixelPorts.Contains(HairMasterNode.SpecularAAScreenSpaceVarianceSlotId))
            {
                baseActiveFields.Add("Specular.AA");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.BentNormalSlotId) && pass.pixelPorts.Contains(HairMasterNode.BentNormalSlotId))
            {
                baseActiveFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.HairStrandDirectionSlotId) && pass.pixelPorts.Contains(HairMasterNode.HairStrandDirectionSlotId))
            {
                baseActiveFields.Add("HairStrandDirection");
            }

            if (masterNode.IsSlotConnected(HairMasterNode.TransmittanceSlotId) && pass.pixelPorts.Contains(HairMasterNode.TransmittanceSlotId))
            {
                baseActiveFields.Add(HairMasterNode.TransmittanceSlotName);
            }

            if (masterNode.IsSlotConnected(HairMasterNode.RimTransmissionIntensitySlotId) && pass.pixelPorts.Contains(HairMasterNode.RimTransmissionIntensitySlotId))
            {
                baseActiveFields.Add(HairMasterNode.RimTransmissionIntensitySlotName);
            }

            if (masterNode.useLightFacingNormal.isOn)
            {
                baseActiveFields.Add("UseLightFacingNormal");
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

            if (pass.pixelPorts.Contains(HairMasterNode.AmbientOcclusionSlotId))
            {
                var occlusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(HairMasterNode.AmbientOcclusionSlotId);

                bool connected = masterNode.IsSlotConnected(HairMasterNode.AmbientOcclusionSlotId);
                if (connected || occlusionSlot.value != occlusionSlot.defaultValue)
                {
                    baseActiveFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(HairMasterNode.LightingSlotId) && pass.pixelPorts.Contains(HairMasterNode.LightingSlotId))
            {
                baseActiveFields.Add("LightingGI");
            }
            if (masterNode.IsSlotConnected(HairMasterNode.BackLightingSlotId) && pass.pixelPorts.Contains(HairMasterNode.LightingSlotId))
            {
                baseActiveFields.Add("BackLightingGI");
            }

            if (masterNode.depthOffset.isOn && pass.pixelPorts.Contains(HairMasterNode.DepthOffsetSlotId))
                baseActiveFields.Add("DepthOffset");

            return activeFields;
        }

        private static bool GenerateShaderPassHair(HairMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;

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
                // HairSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("c3f20efb64673e0488a2c8e986a453fa"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as HairMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                var renderingPass = masterNode.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDLitShader, queue);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                bool transparentBackfaceActive = transparent && masterNode.backThenFrontRendering.isOn;
                bool transparentDepthPrepassActive = transparent && masterNode.alphaTestDepthPrepass.isOn;
                bool transparentDepthPostpassActive = transparent && masterNode.alphaTestDepthPostpass.isOn;

                GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairSceneSelection, mode, subShader, sourceAssetDependencyPaths);

                GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (transparentBackfaceActive)
                {
                    GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairTransparentBackface, mode, subShader, sourceAssetDependencyPaths);
                }

                if (transparentDepthPrepassActive)
                {
                    GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairTransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths);
                }

                // Assign define here based on opaque or transparent to save some variant
                if (opaque)
                {
                    GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairForwardOnlyOpaque, mode, subShader, sourceAssetDependencyPaths);
                }
                else
                {
                    GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairForwardOnlyTransparent, mode, subShader, sourceAssetDependencyPaths);
                }

                if (transparentDepthPostpassActive)
                {
                    GenerateShaderPassHair(masterNode, target, HDRPMeshTarget.Passes.HairTransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HairGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
