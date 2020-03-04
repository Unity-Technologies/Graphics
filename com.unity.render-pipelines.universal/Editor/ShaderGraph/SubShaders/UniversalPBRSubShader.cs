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
    class UniversalPBRSubShader : IPBRSubShader
    {
#region Passes
        ShaderPass m_ForwardPass = new ShaderPass
        {
            // Definition
            displayName = "Universal Forward",
            referenceName = "SHADERPASS_FORWARD",
            lightMode = "UniversalForward",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.uv1", //needed for meta vertex position
            },

            // Required fields
            requiredVaryings = new List<string>()
            {
                "Varyings.positionWS",
                "Varyings.normalWS",
                "Varyings.tangentWS", //needed for vertex lighting
                "Varyings.viewDirectionWS",
                "Varyings.lightmapUV",
                "Varyings.sh",
                "Varyings.fogFactorAndVertexLight", //fog and vertex lighting, vert input is dependency
                "Varyings.shadowCoord", //shadow coord, vert input is dependency
            },

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_fog",
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                s_LightmapKeyword,
                s_DirectionalLightmapCombinedKeyword,
                s_MainLightShadowsKeyword,
                s_MainLightShadowsCascadeKeyword,
                s_AdditionalLightsKeyword,
                s_AdditionalLightShadowsKeyword,
                s_ShadowsSoftKeyword,
                s_MixedLightingSubtractiveKeyword,
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
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
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
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.normalOS",
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ZTestOverride = "ZTest LEqual",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
            },
        };
        ShaderPass m_LitMetaPass = new ShaderPass()
        {
            // Definition
            displayName = "Meta",
            referenceName = "SHADERPASS_META",
            lightMode = "Meta",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.uv1", //needed for meta vertex position
                "Attributes.uv2", //needed for meta vertex position
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ZTestOverride = "ZTest LEqual",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
            },
            keywords = new KeywordDescriptor[]
            {
                s_SmoothnessChannelKeyword,
            },
        };

        ShaderPass m_2DPass = new ShaderPass()
        {
            // Definition
            referenceName = "SHADERPASS_2D",
            lightMode = "Universal2D",
            passInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl",
            varyingsInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
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

        static KeywordDescriptor s_MainLightShadowsKeyword = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows",
            referenceName = "_MAIN_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_MainLightShadowsCascadeKeyword = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows Cascade",
            referenceName = "_MAIN_LIGHT_SHADOWS_CASCADE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_AdditionalLightsKeyword = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "_ADDITIONAL",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
            }
        };

        static KeywordDescriptor s_AdditionalLightShadowsKeyword = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_ShadowsSoftKeyword = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_MixedLightingSubtractiveKeyword = new KeywordDescriptor()
        {
            displayName = "Mixed Lighting Subtractive",
            referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
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

        public int GetPreviewPassIndex() { return 0; }

        ActiveFields GetActiveFieldsFromMasterNode(PBRMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                baseActiveFields.Add("AlphaClip");
            }
            
            if (masterNode.model == PBRMasterNode.Model.Specular)
                baseActiveFields.Add("SpecularSetup");

            if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
            {
                baseActiveFields.Add("Normal");
            }
            switch(masterNode.normalDropOffSpace)
            {
                case NormalDropOffSpace.Tangent:
                    baseActiveFields.AddAll("features.NormalDropOffTS");
                    break;
                case NormalDropOffSpace.Object:
                    baseActiveFields.AddAll("features.NormalDropOffOS");
                    break;
                case NormalDropOffSpace.World:
                    baseActiveFields.AddAll("features.NormalDropOffWS");
                    break;
                default:
                    UnityEngine.Debug.LogError("Unknown normal drop off space: " + masterNode.normalDropOffSpace);
                    break;
            }

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != ShaderGraph.SurfaceType.Opaque)
            {
                // transparent-only defines
                baseActiveFields.Add("SurfaceType.Transparent");

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }
                else if (masterNode.alphaMode == AlphaMode.Premultiply)
                {
                    baseActiveFields.Add("BlendMode.Premultiply");
                }
            }

            return activeFields;
        }

        bool GenerateShaderPass(PBRMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            UniversalShaderGraphUtilities.SetRenderState(masterNode.surfaceType, masterNode.alphaMode, masterNode.twoSided.isOn, ref pass);

            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            return ShaderGraph.GenerationUtils.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                UniversalShaderGraphResources.s_Dependencies, UniversalShaderGraphResources.s_ResourceClassName, UniversalShaderGraphResources.s_AssemblyName);
        }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UniversalPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("ca91dbeb78daa054c9bbe15fef76361c"));
            }

            // Master Node data
            var pbrMasterNode = masterNode as PBRMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(pbrMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                GenerateShaderPass(pbrMasterNode, m_ForwardPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_ShadowCasterPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_DepthOnlyPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_LitMetaPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_2DPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.ShaderGraph.PBRMasterGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        public UniversalPBRSubShader() { }
    }
}
