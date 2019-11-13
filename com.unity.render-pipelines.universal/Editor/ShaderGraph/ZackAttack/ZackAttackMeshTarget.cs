using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class ZackAttackMeshTarget : ITargetImplementation
    {

        public Type targetType => typeof(MeshTarget);
        public string displayName => "ZackAttack";

        // TODO:
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return (masterNode is UnlitMasterNode);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            // TODO: can't we automate this?
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("8e07e91ae21b01d4498b0eb040daeee0")); // ZackAttackMeshTarget

            switch(context.masterNode)
            {
                case UnlitMasterNode unlitMasterNode:
                    context.SetupSubShader(ZackAttackSubShaders.Unlit);
                    break;
            }
        }

#region SubShaders
        public static class ZackAttackSubShaders
        {
            public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
            {
                // pipelineTag = "ZackAttackPipeline",
                passes = new PassCollection
                {
                    { ZackAttackPasses.Unlit }
                },
            };
        }
#endregion

#region Passes
        public static class ZackAttackPasses
        {
            public static PassDescriptor Unlit = new PassDescriptor
            {
                // Definition
                displayName = "Pass",
                referenceName = "SHADERPASS_UNLIT",
                lightMode = "BasicPass",
                useInPreview = true,

                // Port Mask
                vertexPorts = ZackAttackVertexPorts.Unlit,
                pixelPorts = ZackAttackPixelPorts.Unlit,

                // Fields
                structs = ZackAttackStructCollections.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = ZackAttackRenderStates.Default,
                pragmas = ZackAttackPragmas.Instanced,
                keywords = ZackAttackKeywordDescriptors.Unlit,
                includes = ZackAttackIncludes.Unlit,
            };
        }
#endregion

#region PortMasks
        // TODO: should be automated
        static class ZackAttackVertexPorts
        {
            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };
        }

        static class ZackAttackPixelPorts
        {
            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };
        }
#endregion

#region StructDescriptors
        static class ZackAttackStructCollections
        {
            public static StructCollection Default = new StructCollection
            {
                { Structs.Attributes },
                { ZackAttackStructs.Varyings },
                { Structs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs }
            };
        }
#endregion

#region RequiredFields
        static class ZackAttackRequiredFields
        {
            // TODO: ???
        }
#endregion

// TODO: Targets
// #region Dependencies
//        static class ZackAttackFieldDependencies
//        {
//            public static FieldDependency[] Default = new FieldDependency[]
//            {
//                // Varying Dependencies
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.positionWS,   MeshTarget.ShaderStructs.Attributes.positionOS),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.normalWS,     MeshTarget.ShaderStructs.Attributes.normalOS),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.tangentWS,    MeshTarget.ShaderStructs.Attributes.tangentOS),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.bitangentWS,  MeshTarget.ShaderStructs.Attributes.normalOS),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.bitangentWS,  MeshTarget.ShaderStructs.Attributes.tangentOS),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord0,    MeshTarget.ShaderStructs.Attributes.uv0),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord1,    MeshTarget.ShaderStructs.Attributes.uv1),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord2,    MeshTarget.ShaderStructs.Attributes.uv2),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.texCoord3,    MeshTarget.ShaderStructs.Attributes.uv3),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.color,        MeshTarget.ShaderStructs.Attributes.color),
//                new FieldDependency(MeshTarget.ShaderStructs.Varyings.instanceID,   MeshTarget.ShaderStructs.Attributes.instanceID),
//
//                // Vertex Description Dependencies
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.Attributes.normalOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,             MeshTarget.ShaderStructs.Attributes.normalOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),
//
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.Attributes.tangentOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,            MeshTarget.ShaderStructs.Attributes.tangentOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
//
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.Attributes.normalOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.Attributes.tangentOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
//
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,          MeshTarget.ShaderStructs.Attributes.positionOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,           MeshTarget.ShaderStructs.Attributes.positionOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,   MeshTarget.ShaderStructs.Attributes.positionOS),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
//
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),
//
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                          MeshTarget.ShaderStructs.Attributes.uv0),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                          MeshTarget.ShaderStructs.Attributes.uv1),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                          MeshTarget.ShaderStructs.Attributes.uv2),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                          MeshTarget.ShaderStructs.Attributes.uv3),
//                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,                  MeshTarget.ShaderStructs.Attributes.color),
//
//                // Surface Description Dependencies
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,             MeshTarget.ShaderStructs.Varyings.normalWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
//
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,            MeshTarget.ShaderStructs.Varyings.tangentWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
//
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.Varyings.bitangentWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
//
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,           MeshTarget.ShaderStructs.Varyings.positionWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,   MeshTarget.ShaderStructs.Varyings.positionWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,          MeshTarget.ShaderStructs.Varyings.positionWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.Varyings.positionWS),
//
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.Varyings.viewDirectionWS),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
//
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                          MeshTarget.ShaderStructs.Varyings.texCoord0),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                          MeshTarget.ShaderStructs.Varyings.texCoord1),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                          MeshTarget.ShaderStructs.Varyings.texCoord2),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                          MeshTarget.ShaderStructs.Varyings.texCoord3),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,                  MeshTarget.ShaderStructs.Varyings.color),
//                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                     MeshTarget.ShaderStructs.Varyings.cullFace),
//            };
//        }
// #endregion

#region RenderStates
        static class ZackAttackRenderStates
        {
            public static readonly RenderStateCollection Default = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
                { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
                { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
                { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
                { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
                { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) }
            };
        }
#endregion

#region Pragmas
        static class ZackAttackPragmas
        {
            public static readonly PragmaCollection Default = new PragmaCollection
            {
                { Pragma.Target(2.0) },
                { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
                { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };

            // TODO:
            public static readonly PragmaCollection Instanced = new PragmaCollection
            {
                { Pragma.Target(2.0) },
                { Pragma.ExcludeRenderers(new Platform[]{ Platform.D3D9 }) },
                { Pragma.MultiCompileInstancing },
                { Pragma.PreferHlslCC(new Platform[]{ Platform.GLES }) },
                { Pragma.Vertex("vert") },
                { Pragma.Fragment("frag") },
            };
        }
#endregion

#region Keywords
        static class ZackAttackKeywordDescriptors
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

            public static KeywordCollection Unlit = new KeywordCollection
            {
                { Lightmap },
                { DirectionalLightmapCombined },
                { SampleGI }
            };
        }
        #endregion

#region Includes
        static class ZackAttackIncludes
        {
            // Pre-graph
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kCore = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
        const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";

        // Post-graph
        const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";

        public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { kColor, IncludeLocation.Pregraph },
                { kCore, IncludeLocation.Pregraph },
                { kLighting, IncludeLocation.Pregraph },
                { kGraphFunctions, IncludeLocation.Pregraph },

                // Post-graph
                { kShaderPass, IncludeLocation.Postgraph },
                { kVaryings, IncludeLocation.Postgraph },
                { kUnlitPass, IncludeLocation.Postgraph },
            };
        }

        // TODO:
//        static class PostGraphIncludes
//        {
//            private static ConditionalInclude varyingsInclude = new ConditionalInclude(
//                Include.File("Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"));
//
//            private static ConditionalInclude shaderPassInclude = new ConditionalInclude(
//                Include.File("Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"));
//
//            public static ConditionalInclude[] Unlit = new ConditionalInclude[]
//            {
//                PostGraphIncludes.shaderPassInclude,
//                PostGraphIncludes.varyingsInclude,
//                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl")),
//            };
//        }
#endregion

#region ShaderStructs
    static class ZackAttackStructs
    {
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                StructFields.Varyings.viewDirectionWS,
                StructFields.Varyings.bitangentWS,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.cullFace,

                // TODO: kinda confusing
//                ZackAttackStructFields.Varyings.lightmapUV,
//                ZackAttackStructFields.Varyings.sh,
//                ZackAttackStructFields.Varyings.fogFactorAndVertexLight,
//                ZackAttackStructFields.Varyings.shadowCoord,
//
//                public static FieldDescriptor lightmapUV = new FieldDescriptor(Varyings.name, "lightmapUV", "", ShaderValueType.Float2, preprocessor : "defined(LIGHTMAP_ON)", subscriptOptions : StructFieldOptions.Optional);
//                public static FieldDescriptor sh = new FieldDescriptor(Varyings.name, "sh", "", ShaderValueType.Float3, preprocessor : "!defined(LIGHTMAP_ON)", subscriptOptions : StructFieldOptions.Optional);
//                public static FieldDescriptor fogFactorAndVertexLight = new FieldDescriptor(Varyings.name, "fogFactorAndVertexLight", "VARYINGS_NEED_FOG_AND_VERTEX_LIGHT", ShaderValueType.Float4, subscriptOptions : StructFieldOptions.Optional);
//                public static FieldDescriptor shadowCoord = new FieldDescriptor(Varyings.name, "shadowCoord", "VARYINGS_NEED_SHADOWCOORD", ShaderValueType.Float4, subscriptOptions : StructFieldOptions.Optional);
            }
        };
    }
#endregion
    }
}
