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
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));
            
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
