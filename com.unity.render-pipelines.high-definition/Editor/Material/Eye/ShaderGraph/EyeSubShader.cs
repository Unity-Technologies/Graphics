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

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.META, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.DepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.EyePasses.ForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.EyeGUI""");

            return subShader.GetShaderString(0);
        }
    }
}
