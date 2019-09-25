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
#region Passes
        ShaderPass m_UnlitPass = new ShaderPass
        {
            // Definition
            referenceName = "SHADERPASS_SPRITEUNLIT",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteUnlitPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                SpriteUnlitMasterNode.PositionSlotId,
                SpriteUnlitMasterNode.VertNormalSlotId,
                SpriteUnlitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                SpriteUnlitMasterNode.ColorSlotId,
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.color",
                "Attributes.uv0",
            },
            requiredVaryings = new List<string>()
            {
                "Varyings.color",
                "Varyings.texCoord0",
            },

            // Render State
            renderStateOverrides = UniversalMeshTarget.RenderStates.Default,
            
            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
            },
            keywords = new KeywordDescriptor[]
            {
                s_ETCExternalAlphaKeyword,
            },
        };
#endregion

#region Keywords
        static KeywordDescriptor s_ETCExternalAlphaKeyword = new KeywordDescriptor()
        {
            displayName = "ETC External Alpha",
            referenceName = "ETC1_EXTERNAL_ALPHA",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
#endregion

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
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());

                GenerateShaderPass(unlitMasterNode, target, m_UnlitPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
