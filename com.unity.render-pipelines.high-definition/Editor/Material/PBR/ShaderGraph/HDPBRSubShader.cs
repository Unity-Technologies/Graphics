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

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.META, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                if (opaque)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.DepthOnly, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.GBuffer, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths,
                        HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
                }
                
                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.PBRPasses.Forward, mode, subShader, sourceAssetDependencyPaths,
                    HDRPShaderStructs.s_Dependencies, HDRPShaderStructs.s_ResourceClassName, HDRPShaderStructs.s_AssemblyName);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDPBRLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
