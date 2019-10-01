using System.Collections.Generic;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DecalSubShader")]
    class DecalSubShader : ISubShader
    {
        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // DecalSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3b523fb79ded88842bb5195be78e0354"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = outputNode as DecalMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                // Add tags at the SubShader level
                int queue = HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, masterNode.drawOrder, false);
                HDSubShaderUtilities.AddTags(subShader, HDRenderPipeline.k_ShaderTagName, HDRenderTypeTags.Opaque, queue);

                // Caution: Order of GenerateShaderPass matter. Only generate required pass
                if (masterNode.affectsAlbedo.isOn || masterNode.affectsNormal.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn || masterNode.affectsSmoothness.isOn)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Projector3RT, mode, subShader, sourceAssetDependencyPaths);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Projector4RT, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsEmission.isOn)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.ProjectorEmissive, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsAlbedo.isOn || masterNode.affectsNormal.isOn || masterNode.affectsMetal.isOn || masterNode.affectsAO.isOn || masterNode.affectsSmoothness.isOn)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Mesh3RT, mode, subShader, sourceAssetDependencyPaths);

                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Mesh4RT, mode, subShader, sourceAssetDependencyPaths);
                }
                if (masterNode.affectsEmission.isOn)
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.MeshEmissive, mode, subShader, sourceAssetDependencyPaths);
                }

                if (mode.IsPreview())
                {
                    GenerationUtils.GenerateShaderPass(masterNode, target, HDRPDecalTarget.Passes.Preview, mode, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.Rendering.HighDefinition.DecalGUI""");
            string s = subShader.GetShaderString(0);
            return s;
        }
    }
}
