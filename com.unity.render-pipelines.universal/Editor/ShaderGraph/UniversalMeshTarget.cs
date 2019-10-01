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

        public bool IsValid(IMasterNode masterNode)
        {
            if(GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
            {
                if (masterNode is PBRMasterNode ||
                    masterNode is UnlitMasterNode ||
                    masterNode is SpriteLitMasterNode ||
                    masterNode is SpriteUnlitMasterNode)
                {
                    return true;
                }   
            }

            return false;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            switch(context.masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    context.SetupSubShader(SubShaders.PBR);
                    break;
                case UnlitMasterNode unlitMasterNode:
                    context.SetupSubShader(SubShaders.Unlit);
                    break;
                case SpriteLitMasterNode spriteLitMasterNode:
                    context.SetupSubShader(SubShaders.SpriteLit);
                    break;
                case SpriteUnlitMasterNode spriteUnlitMasterNode:
                    context.SetupSubShader(SubShaders.SpriteUnlit);
                    break;
            }
        }

#region SubShaders
        public static class SubShaders
        {
            const string kPipelineTag = "UniversalPipeline";

            public static SubShaderDescriptor PBR = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPass[]
                {
                    Passes.Forward,
                    Passes.ShadowCaster,
                    Passes.DepthOnly,
                    Passes.Meta,
                    Passes._2D,
                },
            };

            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPass[]
                {
                    Passes.Unlit,
                    Passes.ShadowCaster,
                    Passes.DepthOnly,
                },
            };

            public static SubShaderDescriptor SpriteLit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPass[]
                {
                    Passes.SpriteLit,
                    Passes.SpriteNormal,
                    Passes.SpriteForward,
                },
            };

            public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPass[]
                {
                    Passes.SpriteUnlit,
                },
            };
        }
#endregion

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
                vertexPorts = new int[]
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
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

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Attributes.uv1,
                    MeshTarget.ShaderStructs.Varyings.positionWS,
                    MeshTarget.ShaderStructs.Varyings.normalWS,
                    MeshTarget.ShaderStructs.Varyings.tangentWS,
                    MeshTarget.ShaderStructs.Varyings.bitangentWS,
                    MeshTarget.ShaderStructs.Varyings.viewDirectionWS,
                    UniversalMeshTarget.ShaderStructs.Varyings.lightmapUV,
                    UniversalMeshTarget.ShaderStructs.Varyings.sh,
                    UniversalMeshTarget.ShaderStructs.Varyings.fogFactorAndVertexLight,
                    UniversalMeshTarget.ShaderStructs.Varyings.shadowCoord,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Forward,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.Lightmap),
                    new ConditionalKeyword(Keywords.DirectionalLightmapCombined),
                    new ConditionalKeyword(Keywords.MainLightShadows),
                    new ConditionalKeyword(Keywords.MainLightShadowsCascade),
                    new ConditionalKeyword(Keywords.AdditionalLights),
                    new ConditionalKeyword(Keywords.AdditionalLightShadows),
                    new ConditionalKeyword(Keywords.ShadowsSoft),
                    new ConditionalKeyword(Keywords.MixedLightingSubtractive),
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
                vertexPorts = new int[]
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                fieldDependencies = fieldDependencies,

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
                vertexPorts = new int[]
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Attributes.normalOS,
                },
                fieldDependencies = fieldDependencies,

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
                vertexPorts = new int[]
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    PBRMasterNode.AlbedoSlotId,
                    PBRMasterNode.EmissionSlotId,
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Attributes.uv1,
                    MeshTarget.ShaderStructs.Attributes.uv2,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.ShadowCasterMeta,
                pragmas = Pragmas.Default,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.SmoothnessChannel),
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
                vertexPorts = new int[]
                {
                    PBRMasterNode.PositionSlotId,
                    PBRMasterNode.VertNormalSlotId,
                    PBRMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    PBRMasterNode.AlbedoSlotId,
                    PBRMasterNode.AlphaSlotId,
                    PBRMasterNode.AlphaThresholdSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                fieldDependencies = fieldDependencies,

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
                vertexPorts = new int[]
                {
                    UnlitMasterNode.PositionSlotId,
                    UnlitMasterNode.VertNormalSlotId,
                    UnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    UnlitMasterNode.ColorSlotId,
                    UnlitMasterNode.AlphaSlotId,
                    UnlitMasterNode.AlphaThresholdSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Instanced,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.Lightmap),
                    new ConditionalKeyword(Keywords.DirectionalLightmapCombined),
                    new ConditionalKeyword(Keywords.SampleGI),
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
                vertexPorts = new int[]
                {
                    SpriteLitMasterNode.PositionSlotId,
                    SpriteLitMasterNode.VertNormalSlotId,
                    SpriteLitMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    SpriteLitMasterNode.ColorSlotId,
                    SpriteLitMasterNode.MaskSlotId,
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Varyings.color,
                    MeshTarget.ShaderStructs.Varyings.texCoord0,
                    MeshTarget.ShaderStructs.Varyings.screenPosition,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.ETCExternalAlpha),
                    new ConditionalKeyword(Keywords.ShapeLightType0),
                    new ConditionalKeyword(Keywords.ShapeLightType1),
                    new ConditionalKeyword(Keywords.ShapeLightType2),
                    new ConditionalKeyword(Keywords.ShapeLightType3),
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
                vertexPorts = new int[]
                {
                    SpriteLitMasterNode.PositionSlotId,
                    SpriteLitMasterNode.VertNormalSlotId,
                    SpriteLitMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    SpriteLitMasterNode.ColorSlotId,
                    SpriteLitMasterNode.NormalSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Varyings.normalWS,
                    MeshTarget.ShaderStructs.Varyings.tangentWS,
                    MeshTarget.ShaderStructs.Varyings.bitangentWS,
                },
                fieldDependencies = fieldDependencies,                

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
                vertexPorts = new int[]
                {
                    SpriteLitMasterNode.PositionSlotId,
                    SpriteLitMasterNode.VertNormalSlotId,
                    SpriteLitMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    SpriteLitMasterNode.ColorSlotId,
                    SpriteLitMasterNode.NormalSlotId
                },

                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Varyings.color,
                    MeshTarget.ShaderStructs.Varyings.texCoord0,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.ETCExternalAlpha),
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
                vertexPorts = new int[]
                {
                    SpriteUnlitMasterNode.PositionSlotId,
                    SpriteUnlitMasterNode.VertNormalSlotId,
                    SpriteUnlitMasterNode.VertTangentSlotId
                },
                pixelPorts = new int[]
                {
                    SpriteUnlitMasterNode.ColorSlotId,
                },
                
                // Fields
                structs = new StructDescriptor[]
                {
                    UniversalMeshTarget.Attributes,
                    UniversalMeshTarget.Varyings,
                    UniversalMeshTarget.SurfaceDescriptionInputs,
                    UniversalMeshTarget.VertexDescriptionInputs,
                },
                requiredFields = new List<IField>()
                {
                    MeshTarget.ShaderStructs.Attributes.color,
                    MeshTarget.ShaderStructs.Attributes.uv0,
                    MeshTarget.ShaderStructs.Varyings.color,
                    MeshTarget.ShaderStructs.Varyings.texCoord0,
                },
                fieldDependencies = fieldDependencies,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = new ConditionalKeyword[]
                {
                    new ConditionalKeyword(Keywords.ETCExternalAlpha),
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

#region ShaderStructs
        public static class ShaderStructs
        {
            public struct Varyings
            {
                public static string name = "Varyings";
                public static SubscriptDescriptor lightmapUV = new SubscriptDescriptor(Varyings.name, "lightmapUV", "", ShaderValueType.Float2,
                    preprocessor : "defined(LIGHTMAP_ON)", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor sh = new SubscriptDescriptor(Varyings.name, "sh", "", ShaderValueType.Float3,
                    preprocessor : "!defined(LIGHTMAP_ON)", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor fogFactorAndVertexLight = new SubscriptDescriptor(Varyings.name, "fogFactorAndVertexLight", "VARYINGS_NEED_FOG_AND_VERTEX_LIGHT", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor shadowCoord = new SubscriptDescriptor(Varyings.name, "shadowCoord", "VARYINGS_NEED_SHADOWCOORD", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
            }
        }
        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.Attributes.positionOS,
                MeshTarget.ShaderStructs.Attributes.normalOS,
                MeshTarget.ShaderStructs.Attributes.tangentOS,
                MeshTarget.ShaderStructs.Attributes.uv0,
                MeshTarget.ShaderStructs.Attributes.uv1,
                MeshTarget.ShaderStructs.Attributes.uv2,
                MeshTarget.ShaderStructs.Attributes.uv3,
                MeshTarget.ShaderStructs.Attributes.color,
                MeshTarget.ShaderStructs.Attributes.instanceID,
            }
        };
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            interpolatorPack = true,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.Varyings.positionCS,
                MeshTarget.ShaderStructs.Varyings.positionWS,
                MeshTarget.ShaderStructs.Varyings.normalWS,
                MeshTarget.ShaderStructs.Varyings.tangentWS,
                MeshTarget.ShaderStructs.Varyings.texCoord0,
                MeshTarget.ShaderStructs.Varyings.texCoord1,
                MeshTarget.ShaderStructs.Varyings.texCoord2,
                MeshTarget.ShaderStructs.Varyings.texCoord3,
                MeshTarget.ShaderStructs.Varyings.color,
                MeshTarget.ShaderStructs.Varyings.viewDirectionWS,
                MeshTarget.ShaderStructs.Varyings.bitangentWS,
                MeshTarget.ShaderStructs.Varyings.screenPosition,
                ShaderStructs.Varyings.lightmapUV,
                ShaderStructs.Varyings.sh,
                ShaderStructs.Varyings.fogFactorAndVertexLight,
                ShaderStructs.Varyings.shadowCoord,
                MeshTarget.ShaderStructs.Varyings.instanceID,
                MeshTarget.ShaderStructs.Varyings.cullFace,
            }
        };

        public static StructDescriptor VertexDescriptionInputs = new StructDescriptor()
        {
            name = "VertexDescriptionInputs",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceNormal,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceTangent,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceBiTangent,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpacePosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,

                MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,
                MeshTarget.ShaderStructs.VertexDescriptionInputs.TimeParameters,
            }
        };

        public static StructDescriptor SurfaceDescriptionInputs = new StructDescriptor()
        {
            name = "SurfaceDescriptionInputs",
            interpolatorPack = false,
            subscripts = new SubscriptDescriptor[]
            {
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceNormal,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceTangent,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceBiTangent,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpacePosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,

                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TimeParameters,
                MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,
            }
        };
#endregion

#region Dependencies
        public static List<FieldDependency[]> fieldDependencies = new List<FieldDependency[]>()
        {
            //Varying Dependencies
            new FieldDependency[]
            {
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.positionWS,   MeshTarget.ShaderStructs.Attributes.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.normalWS,     MeshTarget.ShaderStructs.Attributes.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.tangentWS,    MeshTarget.ShaderStructs.Attributes.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.bitangentWS,  MeshTarget.ShaderStructs.Attributes.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.bitangentWS,  MeshTarget.ShaderStructs.Attributes.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord0,    MeshTarget.ShaderStructs.Attributes.uv0),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord1,    MeshTarget.ShaderStructs.Attributes.uv1),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord2,    MeshTarget.ShaderStructs.Attributes.uv2),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord3,    MeshTarget.ShaderStructs.Attributes.uv3),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.color,        MeshTarget.ShaderStructs.Attributes.color),
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.instanceID,   MeshTarget.ShaderStructs.Attributes.instanceID),
            },

            //Vertex Description Dependencies
            new FieldDependency[]
            {
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.Attributes.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,             MeshTarget.ShaderStructs.Attributes.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.Attributes.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,            MeshTarget.ShaderStructs.Attributes.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.Attributes.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.Attributes.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,          MeshTarget.ShaderStructs.Attributes.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,           MeshTarget.ShaderStructs.Attributes.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,   MeshTarget.ShaderStructs.Attributes.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                          MeshTarget.ShaderStructs.Attributes.uv0),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                          MeshTarget.ShaderStructs.Attributes.uv1),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                          MeshTarget.ShaderStructs.Attributes.uv2),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                          MeshTarget.ShaderStructs.Attributes.uv3),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,                  MeshTarget.ShaderStructs.Attributes.color),
            },

            //Surface Description Dependencies
            new FieldDependency[]
            {
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,             MeshTarget.ShaderStructs.Varyings.normalWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,            MeshTarget.ShaderStructs.Varyings.tangentWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.Varyings.bitangentWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,           MeshTarget.ShaderStructs.Varyings.positionWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,   MeshTarget.ShaderStructs.Varyings.positionWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,          MeshTarget.ShaderStructs.Varyings.positionWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.Varyings.positionWS),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.Varyings.viewDirectionWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                          MeshTarget.ShaderStructs.Varyings.texCoord0),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                          MeshTarget.ShaderStructs.Varyings.texCoord1),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                          MeshTarget.ShaderStructs.Varyings.texCoord2),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                          MeshTarget.ShaderStructs.Varyings.texCoord3),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,                  MeshTarget.ShaderStructs.Varyings.color),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                     MeshTarget.ShaderStructs.Varyings.cullFace),
            }
        };
#endregion
    }
}
