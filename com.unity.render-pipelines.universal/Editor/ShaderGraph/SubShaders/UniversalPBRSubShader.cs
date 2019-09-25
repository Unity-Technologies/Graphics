using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Data.Util;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LightweightPipeline.LightWeightPBRSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightPBRSubShader")]
    [FormerName("UnityEditor.Rendering.LWRP.LightWeightPBRSubShader")]
    class UniversalPBRSubShader : ISubShader
    {
        bool GenerateShaderPass(PBRMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, result, sourceAssetDependencyPaths,
                UniversalShaderGraphResources.s_Dependencies, UniversalShaderGraphResources.s_ResourceClassName, UniversalShaderGraphResources.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UniversalPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("ca91dbeb78daa054c9bbe15fef76361c"));
            }

            // Master Node data
            var pbrMasterNode = outputNode as PBRMasterNode;
            var universalMeshTarget = target as UniversalMeshTarget;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(pbrMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                GenerateShaderPass(pbrMasterNode, target, UniversalMeshTarget.Passes.Forward, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, target, UniversalMeshTarget.Passes.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, target, UniversalMeshTarget.Passes.DepthOnly, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, target, UniversalMeshTarget.Passes.Meta, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, target, UniversalMeshTarget.Passes._2D, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
