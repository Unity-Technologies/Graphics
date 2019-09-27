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
                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitIndirect, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitVisibility, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitForward, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                        
                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.Passes.HDUnlitGBuffer, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
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

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitShadowCaster, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitMETA, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitSceneSelection, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitMotionVectors, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    if (distortionActive)
                    {
                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitDistortion, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    }

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.Passes.HDUnlitForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", false);
            }

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDUnlitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
