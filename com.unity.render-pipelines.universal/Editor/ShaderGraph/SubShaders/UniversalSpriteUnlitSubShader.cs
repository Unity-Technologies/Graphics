using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal;
using Data.Util;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.LightWeightSpriteUnlitSubShader")]
    class UniversalSpriteUnlitSubShader : ISubShader
    {
        private static bool GenerateShaderPass(SpriteUnlitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            // use standard shader pass generation
            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, result, sourceAssetDependencyPaths,
                UniversalShaderGraphResources.s_Dependencies, UniversalShaderGraphResources.s_ResourceClassName, UniversalShaderGraphResources.s_AssemblyName);
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightSpriteUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("f2df349d00ec920488971bb77440b7bc"));
            }

            // Master Node data
            var unlitMasterNode = outputNode as SpriteUnlitMasterNode;
            var universalMeshTarget = target as UniversalMeshTarget;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());

                GenerateShaderPass(unlitMasterNode, target, UniversalMeshTarget.Passes.SpriteUnlit, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
