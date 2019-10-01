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

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.META, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.DepthForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                if (transparentBackfaceActive)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.TransparentBackface, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }

                if (transparentDepthPrepassActive)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.TransparentDepthPrepass, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.ForwardOnly, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                if (transparentDepthPostpassActive)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.HairPasses.TransparentDepthPostpass, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HairGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
