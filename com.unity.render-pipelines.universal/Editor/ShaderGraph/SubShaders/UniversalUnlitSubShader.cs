using System;
using System.Collections.Generic;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEngine.Experimental.Rendering.LightweightPipeline.LightWeightUnlitSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightUnlitSubShader")]
    [FormerName("UnityEditor.Rendering.LWRP.LightWeightUnlitSubShader")]
    [FormerName("UnityEngine.Rendering.LWRP.LightWeightUnlitSubShader")]
    class UniversalUnlitSubShader : ISubShader
    {
        private static bool GenerateShaderPass(UnlitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            UniversalShaderGraphUtilities.SetRenderState(masterNode.surfaceType, masterNode.alphaMode, masterNode.twoSided.isOn, ref pass);

            // apply master node options to active fields
            var activeFields = GenerationUtils.GetActiveFieldsFromConditionals(masterNode.GetConditionalFields(pass));

            // use standard shader pass generation
            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                UniversalShaderGraphResources.s_Dependencies, UniversalShaderGraphResources.s_ResourceClassName, UniversalShaderGraphResources.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("3ef30c5c1d5fc412f88511ef5818b654"));
            }

            // Master Node data
            var unlitMasterNode = outputNode as UnlitMasterNode;
            var universalMeshTarget = target as UniversalMeshTarget;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(unlitMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                GenerateShaderPass(unlitMasterNode, target, UniversalMeshTarget.Passes.Unlit, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(unlitMasterNode, target, UniversalMeshTarget.Passes.ShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(unlitMasterNode, target, UniversalMeshTarget.Passes.DepthOnly, mode, subShader, sourceAssetDependencyPaths);   
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
