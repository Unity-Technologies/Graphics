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
                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.FabricPasses.Indirect, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.FabricPasses.Visibility, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.FabricPasses.Forward, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                        GenerationUtils.GenerateShaderPass(masterNode, target, HDRPRaytracingMeshTarget.FabricPasses.GBuffer, mode, subShader, sourceAssetDependencyPaths,
                            HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
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

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.META, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.DepthForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.FabricPasses.FabricForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }
                subShader.Deindent();
                subShader.AddShaderChunk("}", true);
            }
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.FabricGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
