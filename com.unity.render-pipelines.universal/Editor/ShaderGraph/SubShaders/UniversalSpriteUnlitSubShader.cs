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
    class UniversalSpriteUnlitSubShader : ISpriteUnlitSubShader
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

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(SpriteUnlitMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(SpriteUnlitMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(SpriteUnlitMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(SpriteUnlitMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            baseActiveFields.Add("SurfaceType.Transparent");
            baseActiveFields.Add("BlendMode.Alpha");

            return activeFields;
        }

        private static bool GenerateShaderPass(SpriteUnlitMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
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
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("f2df349d00ec920488971bb77440b7bc"));
            }

            // Master Node data
            var unlitMasterNode = masterNode as SpriteUnlitMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Transparent);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());

                GenerateShaderPass(unlitMasterNode, m_UnlitPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        public UniversalSpriteUnlitSubShader() { }
    }
}