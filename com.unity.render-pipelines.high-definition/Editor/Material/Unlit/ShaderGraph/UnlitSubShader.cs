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
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.UnlitSubShader")]
    class UnlitSubShader : ISubShader
    {
        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("a32a2cf536cae8e478ca1bbb7b9c493b"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var renderingPass = masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, 0, true);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.HDUnlitShader, queue);

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == ShaderGraph.SurfaceType.Opaque);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.META, mode, subShader, sourceAssetDependencyPaths);

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.SceneSelection, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.DepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                    
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.MotionVectors, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerationUtils.GenerateShaderPass(masterNode, target, HDRPMeshTarget.UnlitPasses.ForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.UnlitUI""");

            return subShader.GetShaderString(0);
        }
    }
}