using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    class PreviewSubShader : ISubShader
    {
#region Passes
        ShaderPass m_PreviewPass = new ShaderPass
        {
            // Definition
            referenceName = "SHADERPASS_PREVIEW",
            passInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewPass.hlsl",
            varyingsInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewVaryings.hlsl",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
            },
        };
#endregion
        private static ActiveFields GetActiveFields(ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;
            
            baseActiveFields.Add("features.graphPixel");
            baseActiveFields.Add("features.preview");

            return activeFields;
        }

        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            var subShader = new ShaderStringBuilder();

            subShader.AppendLine("SubShader");
            using(subShader.BlockScope())
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Opaque);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, null);
                subShader.Concat(tagsBuilder);

                var activeFields = GetActiveFields(m_PreviewPass);

                // use standard shader pass generation
                ShaderGenerator result = new ShaderGenerator();
                ShaderGraph.GenerationUtils.GenerateShaderPass(outputNode, target, m_PreviewPass, mode, activeFields, result, sourceAssetDependencyPaths,
                    PreviewSubShaderResources.s_Dependencies, PreviewSubShaderResources.s_ResourceClassName, PreviewSubShaderResources.s_AssemblyName); 
                subShader.AppendLines(result.GetShaderString(0));
            }

            return subShader.ToString();
        }
    }
}
