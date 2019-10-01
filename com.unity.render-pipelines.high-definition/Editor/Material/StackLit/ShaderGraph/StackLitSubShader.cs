using System.Collections.Generic;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.StackLitSubShader")]
    class StackLitSubShader : ISubShader
    {
        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // StackLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("9649efe3e0e8e2941a983bb0f3a034ad"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as StackLitMasterNode;

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
                bool distortionActive = !opaque && masterNode.distortion.isOn;

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.META, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.DepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (distortionActive)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.Distortion, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.StackLitPasses.ForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.StackLitGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
