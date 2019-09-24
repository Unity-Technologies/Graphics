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
        private static bool GenerateShaderPassHair(HairMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if(mode == GenerationMode.Preview && !pass.useInPreview)
                return false;

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
