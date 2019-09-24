using System.Collections.Generic;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDUnlitSubShader")]
    class HDUnlitSubShader : ISubShader
    {
        private static bool GenerateShaderPassUnlit(HDUnlitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;
            
            // Render state
            if(pass.Equals(HDRPMeshTarget.Passes.HDUnlitDistortion))
            {
                if (masterNode.distortionDepthTest.isOn)
                {
                    pass.ZTestOverride = "ZTest LEqual";
                }
                else
                {
                    pass.ZTestOverride = "ZTest Always";
                }
                if (masterNode.distortionMode == DistortionMode.Add)
                {
                    pass.BlendOverride = "Blend One One, One One";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
                else if (masterNode.distortionMode == DistortionMode.Multiply)
                {
                    pass.BlendOverride = "Blend DstColor Zero, DstAlpha Zero";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
                else // (masterNode.distortionMode == DistortionMode.Replace)
                {
                    pass.BlendOverride = "Blend One Zero, One Zero";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
            }

            // Active Fields
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            
            // Generate
            return GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // HDUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("1c44ec077faa54145a89357de68e5d26"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as HDUnlitMasterNode;
            var subShader = new ShaderGenerator();
            // TODO: For now this SubShader is used for both MDRPMeshTarget and HDRPMeshRaytracingTarget
            if(target is HDRPRaytracingMeshTarget)
            {
                if(mode == GenerationMode.ForReals)
                {
                    subShader.AddShaderChunk("SubShader", false);
                    subShader.AddShaderChunk("{", false);
                    subShader.Indent();
                    {
                        GenerateShaderPassUnlit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitIndirect, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassUnlit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitVisibility, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassUnlit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitForward, mode, subShader, sourceAssetDependencyPaths);
                        GenerateShaderPassUnlit(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitGBuffer, mode, subShader, sourceAssetDependencyPaths);
                    }
                    subShader.Deindent();
                    subShader.AddShaderChunk("}", false);
                }
            }
            else // HDRPMeshTarget
            {
                subShader.AddShaderChunk("SubShader", true);
                subShader.AddShaderChunk("{", true);
                subShader.Indent();
                {
                    // Add tags at the SubShader level
                    int queue = HDRenderQueue.ChangeType(masterNode.renderingPass, masterNode.sortPriority, masterNode.alphaTest.isOn);
                    HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDUnlitShader, queue);

                    // For preview only we generate the passes that are enabled
                    bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                    bool distortionActive = !opaque && masterNode.distortion.isOn;

                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitMETA, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitSceneSelection, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                    if (distortionActive)
                    {
                        GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitDistortion, mode, subShader, sourceAssetDependencyPaths);
                    }

                    GenerateShaderPassUnlit(masterNode, target, HDRPMeshTarget.Passes.HDUnlitForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDUnlitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
