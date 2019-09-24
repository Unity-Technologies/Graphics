using System;
using System.Collections.Generic;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDPBRSubShader")]
    [FormerName("UnityEditor.ShaderGraph.HDPBRSubShader")]
    class HDPBRSubShader : ISubShader
    {
        private static bool GenerateShaderPassLit(PBRMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;
            
            // Update render state
            if(pass.Equals(HDRPMeshTarget.Passes.PBRGBuffer))
            {
                if (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque &&
                    (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                    masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f))
                {
                    pass.ZTestOverride = "ZTest Equal";
                }
                else
                {
                    pass.ZTestOverride = "ZTest LEqual";
                }
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.PBRSceneSelection))
            {
                HDSubShaderUtilities.GetCullMode(masterNode.twoSided.isOn, ref pass);
                HDSubShaderUtilities.GetZWrite(masterNode.surfaceType, ref pass);
            }
            else if(pass.Equals(HDRPMeshTarget.Passes.PBRForwardOpaque) || pass.Equals(HDRPMeshTarget.Passes.PBRForwardTransparent))
            {
                HDSubShaderUtilities.GetBlendMode(masterNode.surfaceType, masterNode.alphaMode, ref pass);
                if (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque &&
                    (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                    masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f))
                {
                    pass.ZTestOverride = "ZTest Equal";
                }
            }

            // Action Fields
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));

            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // HDPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("8a6369cac4d1faf45b8715adbd364f13"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as PBRMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var renderingPass = masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, 0, true);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDUnlitShader, queue);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == UnityEditor.ShaderGraph.SurfaceType.Opaque);

                GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRSceneSelection, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRDepthOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRGBuffer, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRMotionVectors, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRForwardOpaque, mode, subShader, sourceAssetDependencyPaths);
                }
                else
                {
                    GenerateShaderPassLit(masterNode, target, HDRPMeshTarget.Passes.PBRForwardTransparent, mode, subShader, sourceAssetDependencyPaths);
                }
                
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDPBRLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
