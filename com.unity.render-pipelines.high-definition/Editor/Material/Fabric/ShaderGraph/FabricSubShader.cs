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
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            
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
