using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalMeshTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is UniversalRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    subShader = new UniversalPBRSubShader();
                    return true;
                case UnlitMasterNode unlitMasterNode:
                    subShader = new UniversalUnlitSubShader();
                    return true;
                case SpriteLitMasterNode spriteLitMasterNode:
                    subShader = new UniversalSpriteLitSubShader();
                    return true;
                case SpriteUnlitMasterNode spriteUnlitMasterNode:
                    subShader = new UniversalSpriteUnlitSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

#region Passes
        public static class Passes
        {
            public static ShaderPass Forward = new ShaderPass
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
                requiredFields = new List<string>()
                {
                    "Attributes.uv1", //needed for meta vertex position
                    "Varyings.positionWS",
                    "Varyings.normalWS",
                    "Varyings.tangentWS", //needed for vertex lighting
                    "Varyings.bitangentWS",
                    "Varyings.viewDirectionWS",
                    "Varyings.lightmapUV",
                    "Varyings.sh",
                    "Varyings.fogFactorAndVertexLight", //fog and vertex lighting, vert input is dependency
                    "Varyings.shadowCoord", //shadow coord, vert input is dependency
                },

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Forward,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.MainLightShadows,
                    Keywords.MainLightShadowsCascade,
                    Keywords.AdditionalLights,
                    Keywords.AdditionalLightShadows,
                    Keywords.ShadowsSoft,
                    Keywords.MixedLightingSubtractive,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl")),
                },
            };

            public static ShaderPass DepthOnly = new ShaderPass()
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

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.DepthOnly,
                pragmas = Pragmas.Instanced,
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                },
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
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
                requiredFields = new List<string>()
                {
                    "Attributes.normalOS",
                },

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.ShadowCasterMeta,
                pragmas = Pragmas.Instanced,
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl")),
                },
            };

            public static ShaderPass Meta = new ShaderPass()
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
                requiredFields = new List<string>()
                {
                    "Attributes.uv1", //needed for meta vertex position
                    "Attributes.uv2", //needed for meta vertex position
                },

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.ShadowCasterMeta,
                pragmas = Pragmas.Default,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.SmoothnessChannel,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl")),
                },
            };

            public static ShaderPass _2D = new ShaderPass()
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

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Instanced,
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                },
            };

            public static ShaderPass Unlit = new ShaderPass
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

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Instanced,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.Lightmap,
                    Keywords.DirectionalLightmapCombined,
                    Keywords.SampleGI,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                },
            };

            public static ShaderPass SpriteLit = new ShaderPass
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
                requiredFields = new List<string>()
                {
                    "Varyings.color",
                    "Varyings.texCoord0",
                    "Varyings.screenPosition",
                },

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.ETCExternalAlpha,
                    Keywords.ShapeLightType0,
                    Keywords.ShapeLightType1,
                    Keywords.ShapeLightType2,
                    Keywords.ShapeLightType3,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl")),
                },
            };

            public static ShaderPass SpriteNormal = new ShaderPass
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
                requiredFields = new List<string>()
                {
                    "Varyings.normalWS",
                    "Varyings.tangentWS",
                    "Varyings.bitangentWS",
                },

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Default,
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl")),
                },
            };

            public static ShaderPass SpriteForward = new ShaderPass
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
                requiredFields = new List<string>()
                {
                    "Varyings.color",
                    "Varyings.texCoord0",
                },

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.ETCExternalAlpha,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                },
            };

            public static ShaderPass SpriteUnlit = new ShaderPass
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
                requiredFields = new List<string>()
                {
                    "Attributes.color",
                    "Attributes.uv0",
                    "Varyings.color",
                    "Varyings.texCoord0",
                },

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new KeywordDescriptor[]
                {
                    Keywords.ETCExternalAlpha,
                },
                includes = new ConditionalInclude[]
                {
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl")),
                    new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl")),
                },
            };
        }
#endregion

#region Keywords
        public static class Keywords
        {
            public static KeywordDescriptor Lightmap = new KeywordDescriptor()
            {
                displayName = "Lightmap",
                referenceName = "LIGHTMAP_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
            {
                displayName = "Directional Lightmap Combined",
                referenceName = "DIRLIGHTMAP_COMBINED",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor SampleGI = new KeywordDescriptor()
            {
                displayName = "Sample GI",
                referenceName = "_SAMPLE_GI",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MainLightShadows = new KeywordDescriptor()
            {
                displayName = "Main Light Shadows",
                referenceName = "_MAIN_LIGHT_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MainLightShadowsCascade = new KeywordDescriptor()
            {
                displayName = "Main Light Shadows Cascade",
                referenceName = "_MAIN_LIGHT_SHADOWCASCADE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor AdditionalLights = new KeywordDescriptor()
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

            public static KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
            {
                displayName = "Additional Light Shadows",
                referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
            {
                displayName = "Shadows Soft",
                referenceName = "_SHADOWS_SOFT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
            {
                displayName = "Mixed Lighting Subtractive",
                referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
            {
                displayName = "Smoothness Channel",
                referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ETCExternalAlpha = new KeywordDescriptor()
            {
                displayName = "ETC External Alpha",
                referenceName = "ETC1_EXTERNAL_ALPHA",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 0",
                referenceName = "USE_SHAPE_LIGHT_TYPE_0",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 1",
                referenceName = "USE_SHAPE_LIGHT_TYPE_1",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 2",
                referenceName = "USE_SHAPE_LIGHT_TYPE_2",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
            {
                displayName = "Shape Light Type 3",
                referenceName = "USE_SHAPE_LIGHT_TYPE_3",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
        }
#endregion

#region RenderStates
        static class RenderStates
        {
            public static readonly ConditionalRenderState[] Default = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true)),
                new ConditionalRenderState(RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false)),
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true)),
            };

            public static readonly ConditionalRenderState[] ShadowCasterMeta = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false)),
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true)),
            };

            public static readonly ConditionalRenderState[] DepthOnly = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.On)),
                new ConditionalRenderState(RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false)),
                new ConditionalRenderState(RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true)),
                new ConditionalRenderState(RenderState.ColorMask("ColorMask 0")),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true)),
                new ConditionalRenderState(RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true)),
            };
        }
#endregion

#region Pragmas
        static class Pragmas
        {
            public static readonly ConditionalPragma[] Default = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(2.0)),
                new ConditionalPragma(Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 })),
                new ConditionalPragma(Pragma.Custom("prefer_hlslcc gles")),
                new ConditionalPragma(Pragma.Vertex("vert")),
                new ConditionalPragma(Pragma.Fragment("frag")),
            };

            public static readonly ConditionalPragma[] Instanced = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(2.0)),
                new ConditionalPragma(Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 })),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
                new ConditionalPragma(Pragma.Custom("prefer_hlslcc gles")),
                new ConditionalPragma(Pragma.Vertex("vert")),
                new ConditionalPragma(Pragma.Fragment("frag")),
            };

            public static readonly ConditionalPragma[] Forward = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(2.0)),
                new ConditionalPragma(Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 })),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
                new ConditionalPragma(Pragma.MultiCompileFog),
                new ConditionalPragma(Pragma.Custom("prefer_hlslcc gles")),
                new ConditionalPragma(Pragma.Vertex("vert")),
                new ConditionalPragma(Pragma.Fragment("frag")),
            };
        }
#endregion
    }
}
