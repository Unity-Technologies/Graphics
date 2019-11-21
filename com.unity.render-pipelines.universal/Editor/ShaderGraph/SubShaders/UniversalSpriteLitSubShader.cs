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
    class UniversalSpriteLitSubShader : ISpriteLitSubShader
    {
#region Passes
        ShaderPass m_LitPass = new ShaderPass
        {
            // Definition
            displayName = "Sprite Lit",
            referenceName = "SHADERPASS_SPRITELIT",
            lightMode = "Universal2D",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteLitPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.MaskSlotId,
            },

            // Required fields
            requiredVaryings = new List<string>()
            {
                "Varyings.color",
                "Varyings.texCoord0",
                "Varyings.screenPosition",
            },
            
            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl",
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
                s_ShapeLightType0Keyword,
                s_ShapeLightType1Keyword,
                s_ShapeLightType2Keyword,
                s_ShapeLightType3Keyword,
            },
        };

        ShaderPass m_NormalPass = new ShaderPass
        {
            // Definition
            displayName = "Sprite Normal",
            referenceName = "SHADERPASS_SPRITENORMAL",
            lightMode = "NormalsRendering",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteNormalPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId
            },

            // Required fields
            requiredVaryings = new List<string>()
            {
                "Varyings.normalWS",
                "Varyings.tangentWS",
                "Varyings.bitangentWS",
            },

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
            },
        };

        ShaderPass m_ForwardPass = new ShaderPass
        {
            // Definition
            displayName = "Sprite Forward",
            referenceName = "SHADERPASS_SPRITEFORWARD",
            lightMode = "UniversalForward",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteForwardPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId
            },

            // Required fields
            requiredVaryings = new List<string>()
            {
                "Varyings.color",
                "Varyings.texCoord0",
            },
            
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

        static KeywordDescriptor s_ShapeLightType0Keyword = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 0",
            referenceName = "USE_SHAPE_LIGHT_TYPE_0",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_ShapeLightType1Keyword = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 1",
            referenceName = "USE_SHAPE_LIGHT_TYPE_1",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_ShapeLightType2Keyword = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 2",
            referenceName = "USE_SHAPE_LIGHT_TYPE_2",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_ShapeLightType3Keyword = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 3",
            referenceName = "USE_SHAPE_LIGHT_TYPE_3",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
#endregion

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(SpriteLitMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(SpriteLitMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(SpriteLitMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(SpriteLitMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            baseActiveFields.Add("SurfaceType.Transparent");
            baseActiveFields.Add("BlendMode.Alpha");

            return activeFields;
        }

        private static bool GenerateShaderPass(SpriteLitMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            UniversalShaderGraphUtilities.SetRenderState(SurfaceType.Transparent, AlphaMode.Alpha, true, ref pass);

            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            // use standard shader pass generation
            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                UniversalShaderGraphResources.s_Dependencies, UniversalShaderGraphResources.s_ResourceClassName, UniversalShaderGraphResources.s_AssemblyName);
        }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // LightWeightSpriteUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("62511ee827d14492a8c78ba0ef167e7f"));
            }

            // Master Node data
            var litMasterNode = masterNode as SpriteLitMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());

                GenerateShaderPass(litMasterNode, m_LitPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(litMasterNode, m_NormalPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(litMasterNode, m_ForwardPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        public UniversalSpriteLitSubShader () { }
    }
}
