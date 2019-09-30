using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal;
using Data.Util;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.LightWeightSpriteLitSubShader")]
    class UniversalSpriteLitSubShader : ISubShader
    {
        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightSpriteUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("62511ee827d14492a8c78ba0ef167e7f"));
            }

            // Master Node data
            var masterNode = outputNode as SpriteLitMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());

                GenerationUtils.GenerateShaderPass(outputNode, target, UniversalMeshTarget.Passes.SpriteLit, mode, subShader, sourceAssetDependencyPaths,
                    UniversalMeshTarget.fieldDependencies);

                GenerationUtils.GenerateShaderPass(outputNode, target, UniversalMeshTarget.Passes.SpriteNormal, mode, subShader, sourceAssetDependencyPaths,
                    UniversalMeshTarget.fieldDependencies);

                GenerationUtils.GenerateShaderPass(outputNode, target, UniversalMeshTarget.Passes.SpriteForward, mode, subShader, sourceAssetDependencyPaths,
                    UniversalMeshTarget.fieldDependencies);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
