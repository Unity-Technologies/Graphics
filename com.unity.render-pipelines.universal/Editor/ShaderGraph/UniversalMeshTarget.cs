using System;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return (masterNode is PBRMasterNode ||
                    masterNode is UnlitMasterNode ||
                    masterNode is SpriteLitMasterNode ||
                    masterNode is SpriteUnlitMasterNode);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget
            
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
                passes = new ShaderPassCollection
                {
                    { Passes.Forward },
                    { Passes.ShadowCaster },
                    { Passes.DepthOnly },
                    { Passes.Meta },
                    { Passes._2D },
                },
            };

            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPassCollection
                {
                    { Passes.Unlit },
                    { Passes.ShadowCaster },
                    { Passes.DepthOnly },
                },
            };

            public static SubShaderDescriptor SpriteLit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPassCollection
                {
                    { Passes.SpriteLit },
                    { Passes.SpriteNormal },
                    { Passes.SpriteForward },
                },
            };

            public static SubShaderDescriptor SpriteUnlit = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ShaderPassCollection
                {
                    { Passes.SpriteUnlit },
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
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.PBR,
                pixelPorts = PixelPorts.PBR,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.PBRForward,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Forward,
                keywords = Keywords.PBRForward,
                includes = Includes.Forward,
            };

            public static ShaderPass DepthOnly = new ShaderPass()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.PBR,
                pixelPorts = PixelPorts.PBRAlphaOnly,

                // Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.DepthOnly,
                pragmas = Pragmas.Instanced,
                includes = Includes.DepthOnly,
            };

            public static ShaderPass ShadowCaster = new ShaderPass()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWCASTER",
                lightMode = "ShadowCaster",
                
                // Port Mask
                vertexPorts = VertexPorts.PBR,
                pixelPorts = PixelPorts.PBRAlphaOnly,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.PBRShadowCaster,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.ShadowCasterMeta,
                pragmas = Pragmas.Instanced,
                includes = Includes.ShadowCaster,
            };

            public static ShaderPass Meta = new ShaderPass()
            {
                // Definition
                displayName = "Meta",
                referenceName = "SHADERPASS_META",
                lightMode = "Meta",

                // Port Mask
                vertexPorts = VertexPorts.PBR,
                pixelPorts = PixelPorts.PBRMeta,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.PBRMeta,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.ShadowCasterMeta,
                pragmas = Pragmas.Default,
                keywords = Keywords.PBRMeta,
                includes = Includes.Meta,
            };

            public static ShaderPass _2D = new ShaderPass()
            {
                // Definition
                referenceName = "SHADERPASS_2D",
                lightMode = "Universal2D",

                // Port Mask
                vertexPorts = VertexPorts.PBR,
                pixelPorts = PixelPorts.PBR2D,

                // Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Instanced,
                includes = Includes.PBR2D,
            };

            public static ShaderPass Unlit = new ShaderPass
            {
                // Definition
                displayName = "Pass",
                referenceName = "SHADERPASS_UNLIT",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.Unlit,
                pixelPorts = PixelPorts.Unlit,

                // Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Instanced,
                keywords = Keywords.Unlit,
                includes = Includes.Unlit,
            };

            public static ShaderPass SpriteLit = new ShaderPass
            {
                // Definition
                displayName = "Sprite Lit",
                referenceName = "SHADERPASS_SPRITELIT",
                lightMode = "Universal2D",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.SpriteLit,
                pixelPorts = PixelPorts.SpriteLit,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.SpriteLit,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = Keywords.SpriteLit,
                includes = Includes.SpriteLit,
            };

            public static ShaderPass SpriteNormal = new ShaderPass
            {
                // Definition
                displayName = "Sprite Normal",
                referenceName = "SHADERPASS_SPRITENORMAL",
                lightMode = "NormalsRendering",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.SpriteLit,
                pixelPorts = PixelPorts.SpriteNormal,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.SpriteNormal,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = UniversalMeshTarget.RenderStates.Default,
                pragmas = Pragmas.Default,
                includes = Includes.SpriteNormal,
            };

            public static ShaderPass SpriteForward = new ShaderPass
            {
                // Definition
                displayName = "Sprite Forward",
                referenceName = "SHADERPASS_SPRITEFORWARD",
                lightMode = "UniversalForward",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.SpriteLit,
                pixelPorts = PixelPorts.SpriteNormal,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.SpriteForward,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = Keywords.ETCExternalAlpha,
                includes = Includes.SpriteForward,
            };

            public static ShaderPass SpriteUnlit = new ShaderPass
            {
                // Definition
                referenceName = "SHADERPASS_SPRITEUNLIT",
                useInPreview = true,

                // Port Mask
                vertexPorts = VertexPorts.SpriteUnlit,
                pixelPorts = PixelPorts.SpriteUnlit,

                // Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.SpriteUnlit,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Default,
                pragmas = Pragmas.Default,
                keywords = Keywords.ETCExternalAlpha,
                includes = Includes.SpriteUnlit,
            };
        }
#endregion

#region PortMasks
        static class VertexPorts
        {
            public static int[] PBR = new int[]
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            };

            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };

            public static int[] SpriteLit = new int[]
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId,
            };

            public static int[] SpriteUnlit = new int[]
            {
                SpriteUnlitMasterNode.PositionSlotId,
                SpriteUnlitMasterNode.VertNormalSlotId,
                SpriteUnlitMasterNode.VertTangentSlotId
            };
        }

        static class PixelPorts
        {
            public static int[] PBR = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRAlphaOnly = new int[]
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRMeta = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBR2D = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            };

            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] SpriteLit = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.MaskSlotId,
            };

            public static int[] SpriteNormal = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId,
            };

            public static int[] SpriteUnlit = new int[]
            {
                SpriteUnlitMasterNode.ColorSlotId,
            };
        }
#endregion

#region StructDescriptors
        static class StructDescriptors
        {
            public static StructDescriptor[] Default = new StructDescriptor[]
            {
                UniversalMeshTarget.Attributes,
                UniversalMeshTarget.Varyings,
                UniversalMeshTarget.SurfaceDescriptionInputs,
                UniversalMeshTarget.VertexDescriptionInputs,
            };
        }
#endregion

#region RequiredFields
        static class RequiredFields
        {
            public static IField[] PBRForward = new IField[]
            {
                MeshTarget.ShaderStructs.Attributes.uv1,                            // needed for meta vertex position
                MeshTarget.ShaderStructs.Varyings.positionWS,
                MeshTarget.ShaderStructs.Varyings.normalWS,
                MeshTarget.ShaderStructs.Varyings.tangentWS,                        // needed for vertex lighting
                MeshTarget.ShaderStructs.Varyings.bitangentWS,
                MeshTarget.ShaderStructs.Varyings.viewDirectionWS,
                UniversalMeshTarget.ShaderStructs.Varyings.lightmapUV,
                UniversalMeshTarget.ShaderStructs.Varyings.sh,
                UniversalMeshTarget.ShaderStructs.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalMeshTarget.ShaderStructs.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static IField[] PBRShadowCaster = new IField[]
            {
                MeshTarget.ShaderStructs.Attributes.normalOS,
            };

            public static IField[] PBRMeta = new IField[]
            {
                MeshTarget.ShaderStructs.Attributes.uv1,                            // needed for meta vertex position
                MeshTarget.ShaderStructs.Attributes.uv2,                            //needed for meta vertex position
            };

            public static IField[] SpriteLit = new IField[]
            {
                MeshTarget.ShaderStructs.Varyings.color,
                MeshTarget.ShaderStructs.Varyings.texCoord0,
                MeshTarget.ShaderStructs.Varyings.screenPosition,
            };

            public static IField[] SpriteNormal = new IField[]
            {
                MeshTarget.ShaderStructs.Varyings.normalWS,
                MeshTarget.ShaderStructs.Varyings.tangentWS,
                MeshTarget.ShaderStructs.Varyings.bitangentWS,
            };

            public static IField[] SpriteForward = new IField[]
            {
                MeshTarget.ShaderStructs.Varyings.color,
                MeshTarget.ShaderStructs.Varyings.texCoord0,
            };

            public static IField[] SpriteUnlit = new IField[]
            {
                MeshTarget.ShaderStructs.Attributes.color,
                MeshTarget.ShaderStructs.Attributes.uv0,
                MeshTarget.ShaderStructs.Varyings.color,
                MeshTarget.ShaderStructs.Varyings.texCoord0,
            };
        }
#endregion

#region Dependencies
        static class FieldDependencies
        {
            public static FieldDependency[] Default = new FieldDependency[]
            {
                //Varying Dependencies
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

                //Vertex Description Dependencies
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

                //Surface Description Dependencies
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
            };
        } 
#endregion

#region RenderStates
        static class RenderStates
        {
            public static readonly RenderStateCollection Default = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
                { RenderState.ZWrite(ZWrite.Off), new FieldCondition(DefaultFields.SurfaceTransparent, true) },
                { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
                { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
                { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
            };

            public static readonly RenderStateCollection ShadowCasterMeta = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
                { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
                { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
            };

            public static readonly RenderStateCollection DepthOnly = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.Cull(Cull.Back), new FieldCondition(DefaultFields.DoubleSided, false) },
                { RenderState.Cull(Cull.Off), new FieldCondition(DefaultFields.DoubleSided, true) },
                { RenderState.ColorMask("ColorMask 0") },
                { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(DefaultFields.SurfaceOpaque, true) },
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendAlpha, true) },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(DefaultFields.BlendPremultiply, true) },
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(DefaultFields.BlendAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(DefaultFields.BlendMultiply, true) },
            };
        }
#endregion

#region Pragmas
        static class Pragmas
        {
            public static readonly PragmaCollection Default = new PragmaCollection
            {
                { Pragma.Target(2.0) },
                { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
                { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection Instanced = new PragmaCollection
            {
                { Pragma.Target(2.0) },
                { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            public static readonly PragmaCollection Forward = new PragmaCollection
            {
                { Pragma.Target(2.0) },
                { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.MultiCompileFog },
                { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };
        }
#endregion

#region Keywords
        static class Keywords
        {
            public static KeywordCollection PBRForward = new KeywordCollection
            {
                { KeywordDescriptors.Lightmap },
                { KeywordDescriptors.DirectionalLightmapCombined },
                { KeywordDescriptors.MainLightShadows },
                { KeywordDescriptors.MainLightShadowsCascade },
                { KeywordDescriptors.AdditionalLights },
                { KeywordDescriptors.AdditionalLightShadows },
                { KeywordDescriptors.ShadowsSoft },
                { KeywordDescriptors.MixedLightingSubtractive },
            };

            public static KeywordCollection PBRMeta = new KeywordCollection
            {
                { KeywordDescriptors.SmoothnessChannel },
            };

            public static KeywordCollection Unlit = new KeywordCollection
            {
                { KeywordDescriptors.Lightmap },
                { KeywordDescriptors.DirectionalLightmapCombined },
                { KeywordDescriptors.SampleGI },
            };

            public static KeywordCollection SpriteLit = new KeywordCollection
            {
                { KeywordDescriptors.ETCExternalAlpha },
                { KeywordDescriptors.ShapeLightType0 },
                { KeywordDescriptors.ShapeLightType1 },
                { KeywordDescriptors.ShapeLightType2 },
                { KeywordDescriptors.ShapeLightType3 },
            };

            public static KeywordCollection ETCExternalAlpha = new KeywordCollection
            {
                { KeywordDescriptors.ETCExternalAlpha },
            };
        }
#endregion

#region Includes
        static class Includes
        {
            // Pre-graph
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kInstancing = "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl";
            const string kCore = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
            const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";
            const string kInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string k2DLightingUtil = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl";
            const string k2DNormal = "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl";

            // Post-graph
            const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
            const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
            const string kPBRForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
            const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string kPBR2DPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl";
            const string kUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            const string kSpriteLitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteLitPass.hlsl";
            const string kSpriteNormalPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteNormalPass.hlsl";
            const string kSpriteForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteForwardPass.hlsl";
            const string kSpriteUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteUnlitPass.hlsl";

            public static IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kInstancing, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kShadows, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },
                { kInput, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kPBRForwardPass, Include.Location.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kInstancing, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kDepthOnlyPass, Include.Location.Postgraph },
            };

            public static IncludeCollection ShadowCaster = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kInstancing, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kShadowCasterPass, Include.Location.Postgraph },
            };

            public static IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },
                { kMetaInput, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kLightingMetaPass, Include.Location.Postgraph },
            };

            public static IncludeCollection PBR2D = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kInstancing, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kPBR2DPass, Include.Location.Postgraph },
            };

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kUnlitPass, Include.Location.Postgraph },
            };

            public static IncludeCollection SpriteLit = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },
                { k2DLightingUtil, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kSpriteLitPass, Include.Location.Postgraph },
            };

            public static IncludeCollection SpriteNormal = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },
                { k2DNormal, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kSpriteNormalPass, Include.Location.Postgraph },
            };

            public static IncludeCollection SpriteForward = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kSpriteForwardPass, Include.Location.Postgraph },
            };

            public static IncludeCollection SpriteUnlit = new IncludeCollection
            {
                // Pre-graph
                { kColor, Include.Location.Pregraph },
                { kCore, Include.Location.Pregraph },
                { kLighting, Include.Location.Pregraph },
                { kGraphFunctions, Include.Location.Pregraph },

                // Post-graph
                { kShaderPass, Include.Location.Postgraph },
                { kVaryings, Include.Location.Postgraph },
                { kSpriteUnlitPass, Include.Location.Postgraph },
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

#region KeywordDescriptors
        public static class KeywordDescriptors
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
    }
}
