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
#region Passes
        ShaderPass m_UnlitPass = new ShaderPass
        {
            // Definition
            displayName = "Pass",
            referenceName = "SHADERPASS_UNLIT",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
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
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                s_LightmapKeyword,
                s_DirectionalLightmapCombinedKeyword,
                s_SampleGIKeyword,
            },
        };

        ShaderPass m_DepthOnlyPass = new ShaderPass()
        {
            // Definition
            displayName = "DepthOnly",
            referenceName = "SHADERPASS_DEPTHONLY",
            lightMode = "DepthOnly",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },

            // Render State
            renderStateOverrides = UniversalMeshTarget.RenderStates.DepthOnly,

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
            },
        };

        ShaderPass m_ShadowCasterPass = new ShaderPass()
        {
            // Definition
            displayName = "ShadowCaster",
            referenceName = "SHADERPASS_SHADOWCASTER",
            lightMode = "ShadowCaster",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            
            // Port mask
            vertexPorts = new List<int>()
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.normalOS", 
            },

            // Render State
            renderStateOverrides = UniversalMeshTarget.RenderStates.ShadowCasterMeta,

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                s_SmoothnessChannelKeyword,
            },
        };
#endregion
        
#region Keywords
        static KeywordDescriptor s_LightmapKeyword = new KeywordDescriptor()
        {
            displayName = "Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_DirectionalLightmapCombinedKeyword = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_SampleGIKeyword = new KeywordDescriptor()
        {
            displayName = "Sample GI",
            referenceName = "_SAMPLE_GI",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_SmoothnessChannelKeyword = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };
#endregion

        private static bool GenerateShaderPass(UnlitMasterNode masterNode, ITarget target, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            // use standard shader pass generation
            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, target, pass, mode, result, sourceAssetDependencyPaths,
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
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(unlitMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                GenerateShaderPass(unlitMasterNode, target, m_UnlitPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(unlitMasterNode, target, m_ShadowCasterPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(unlitMasterNode, target, m_DepthOnlyPass, mode, subShader, sourceAssetDependencyPaths);   
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
