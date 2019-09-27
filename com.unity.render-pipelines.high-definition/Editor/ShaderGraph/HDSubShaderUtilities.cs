using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Util;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    internal enum HDRenderTypeTags
    {
        HDLitShader,    // For Lit, LayeredLit, LitTesselation, LayeredLitTesselation
        HDUnlitShader,  // Unlit
        Opaque,         // Used by Terrain
    }

    static class HDRPShaderStructs
    {
        public static string s_ResourceClassName => typeof(HDRPShaderStructs).FullName;

        public static string s_AssemblyName => typeof(HDRPShaderStructs).Assembly.FullName.ToString();

        internal struct AttributesMesh
        {
            [Semantic("POSITION")]                  Vector3 positionOS;
            [Semantic("NORMAL")][Optional]          Vector3 normalOS;
            [Semantic("TANGENT")][Optional]         Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]       Vector4 uv0;
            [Semantic("TEXCOORD1")][Optional]       Vector4 uv1;
            [Semantic("TEXCOORD2")][Optional]       Vector4 uv2;
            [Semantic("TEXCOORD3")][Optional]       Vector4 uv3;
            [Semantic("COLOR")][Optional]           Vector4 color;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")] uint instanceID;
        };

        [InterpolatorPack]
        internal struct VaryingsMeshToPS
        {
            [Semantic("SV_Position")]                                               Vector4 positionCS;
            [Optional]                                                              Vector3 positionRWS;
            [Optional]                                                              Vector3 normalWS;
            [Optional]                                                              Vector4 tangentWS;      // w contain mirror sign
            [Optional]                                                              Vector4 texCoord0;
            [Optional]                                                              Vector4 texCoord1;
            [Optional]                                                              Vector4 texCoord2;
            [Optional]                                                              Vector4 texCoord3;
            [Optional]                                                              Vector4 color;
            [Semantic("CUSTOM_INSTANCE_ID")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")] uint instanceID;
            [Semantic("FRONT_FACE_SEMANTIC")][SystemGenerated][OverrideType("FRONT_FACE_TYPE")][PreprocessorIf("defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)")] bool cullFace;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionRWS",       "VaryingsMeshToDS.positionRWS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "VaryingsMeshToDS.normalWS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "VaryingsMeshToDS.tangentWS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "VaryingsMeshToDS.texCoord0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "VaryingsMeshToDS.texCoord1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "VaryingsMeshToDS.texCoord2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "VaryingsMeshToDS.texCoord3"),
                new Dependency("VaryingsMeshToPS.color",            "VaryingsMeshToDS.color"),
                new Dependency("VaryingsMeshToPS.instanceID",       "VaryingsMeshToDS.instanceID"),
            };

            public static Dependency[] standardDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionRWS",       "AttributesMesh.positionOS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "AttributesMesh.normalOS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "AttributesMesh.tangentOS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "AttributesMesh.uv0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "AttributesMesh.uv1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "AttributesMesh.uv2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "AttributesMesh.uv3"),
                new Dependency("VaryingsMeshToPS.color",            "AttributesMesh.color"),
                new Dependency("VaryingsMeshToPS.instanceID",       "AttributesMesh.instanceID"),
            };
        };

        [InterpolatorPack]
        internal struct VaryingsMeshToDS
        {
            Vector3 positionRWS;
            Vector3 normalWS;
            [Optional]      Vector4 tangentWS;
            [Optional]      Vector4 texCoord0;
            [Optional]      Vector4 texCoord1;
            [Optional]      Vector4 texCoord2;
            [Optional]      Vector4 texCoord3;
            [Optional]      Vector4 color;
            [Semantic("CUSTOM_INSTANCE_ID")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")] uint instanceID;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToDS.tangentWS",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("VaryingsMeshToDS.texCoord0",     "VaryingsMeshToPS.texCoord0"),
                new Dependency("VaryingsMeshToDS.texCoord1",     "VaryingsMeshToPS.texCoord1"),
                new Dependency("VaryingsMeshToDS.texCoord2",     "VaryingsMeshToPS.texCoord2"),
                new Dependency("VaryingsMeshToDS.texCoord3",     "VaryingsMeshToPS.texCoord3"),
                new Dependency("VaryingsMeshToDS.color",         "VaryingsMeshToPS.color"),
                new Dependency("VaryingsMeshToDS.instanceID",    "VaryingsMeshToPS.instanceID"),
            };
        };

        internal struct FragInputs
        {
            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("FragInputs.positionRWS",        "VaryingsMeshToPS.positionRWS"),
                new Dependency("FragInputs.tangentToWorld",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("FragInputs.tangentToWorld",     "VaryingsMeshToPS.normalWS"),
                new Dependency("FragInputs.texCoord0",          "VaryingsMeshToPS.texCoord0"),
                new Dependency("FragInputs.texCoord1",          "VaryingsMeshToPS.texCoord1"),
                new Dependency("FragInputs.texCoord2",          "VaryingsMeshToPS.texCoord2"),
                new Dependency("FragInputs.texCoord3",          "VaryingsMeshToPS.texCoord3"),
                new Dependency("FragInputs.color",              "VaryingsMeshToPS.color"),
                new Dependency("FragInputs.isFrontFace",        "VaryingsMeshToPS.cullFace"),
            };
        };

        // this describes the input to the pixel shader graph eval
        internal struct SurfaceDescriptionInputs
        {
            [Optional] Vector3 ObjectSpaceNormal;
            [Optional] Vector3 ViewSpaceNormal;
            [Optional] Vector3 WorldSpaceNormal;
            [Optional] Vector3 TangentSpaceNormal;

            [Optional] Vector3 ObjectSpaceTangent;
            [Optional] Vector3 ViewSpaceTangent;
            [Optional] Vector3 WorldSpaceTangent;
            [Optional] Vector3 TangentSpaceTangent;

            [Optional] Vector3 ObjectSpaceBiTangent;
            [Optional] Vector3 ViewSpaceBiTangent;
            [Optional] Vector3 WorldSpaceBiTangent;
            [Optional] Vector3 TangentSpaceBiTangent;

            [Optional] Vector3 ObjectSpaceViewDirection;
            [Optional] Vector3 ViewSpaceViewDirection;
            [Optional] Vector3 WorldSpaceViewDirection;
            [Optional] Vector3 TangentSpaceViewDirection;

            [Optional] Vector3 ObjectSpacePosition;
            [Optional] Vector3 ViewSpacePosition;
            [Optional] Vector3 WorldSpacePosition;
            [Optional] Vector3 TangentSpacePosition;
            [Optional] Vector3 AbsoluteWorldSpacePosition;

            [Optional] Vector4 ScreenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 VertexColor;
            [Optional] float FaceSign;
            [Optional] Vector3 TimeParameters;

            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("SurfaceDescriptionInputs.WorldSpaceNormal",          "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceNormal",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceNormal",           "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceTangent",        "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceTangent",          "SurfaceDescriptionInputs.WorldSpaceTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceBiTangent",      "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceBiTangent",        "SurfaceDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpacePosition",        "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.AbsoluteWorldSpacePosition","FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpacePosition",       "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ViewSpacePosition",         "FragInputs.positionRWS"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceViewDirection",   "FragInputs.positionRWS"),                   // we build WorldSpaceViewDirection using FragInputs.positionRWS in GetWorldSpaceNormalizeViewDir()
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceViewDirection",  "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceViewDirection",    "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.ScreenPosition",            "SurfaceDescriptionInputs.WorldSpacePosition"),
                new Dependency("SurfaceDescriptionInputs.uv0",                       "FragInputs.texCoord0"),
                new Dependency("SurfaceDescriptionInputs.uv1",                       "FragInputs.texCoord1"),
                new Dependency("SurfaceDescriptionInputs.uv2",                       "FragInputs.texCoord2"),
                new Dependency("SurfaceDescriptionInputs.uv3",                       "FragInputs.texCoord3"),
                new Dependency("SurfaceDescriptionInputs.VertexColor",               "FragInputs.color"),
                new Dependency("SurfaceDescriptionInputs.FaceSign",                  "FragInputs.isFrontFace"),

                new Dependency("DepthOffset", "FragInputs.positionRWS"),
            };
        };

        // this describes the input to the pixel shader graph eval
        internal struct VertexDescriptionInputs
        {
            [Optional] Vector3 ObjectSpaceNormal;
            [Optional] Vector3 ViewSpaceNormal;
            [Optional] Vector3 WorldSpaceNormal;
            [Optional] Vector3 TangentSpaceNormal;

            [Optional] Vector3 ObjectSpaceTangent;
            [Optional] Vector3 ViewSpaceTangent;
            [Optional] Vector3 WorldSpaceTangent;
            [Optional] Vector3 TangentSpaceTangent;

            [Optional] Vector3 ObjectSpaceBiTangent;
            [Optional] Vector3 ViewSpaceBiTangent;
            [Optional] Vector3 WorldSpaceBiTangent;
            [Optional] Vector3 TangentSpaceBiTangent;

            [Optional] Vector3 ObjectSpaceViewDirection;
            [Optional] Vector3 ViewSpaceViewDirection;
            [Optional] Vector3 WorldSpaceViewDirection;
            [Optional] Vector3 TangentSpaceViewDirection;

            [Optional] Vector3 ObjectSpacePosition;
            [Optional] Vector3 ViewSpacePosition;
            [Optional] Vector3 WorldSpacePosition;
            [Optional] Vector3 TangentSpacePosition;
            [Optional] Vector3 AbsoluteWorldSpacePosition;

            [Optional] Vector4 ScreenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 VertexColor;
            [Optional] Vector3 TimeParameters;

            public static Dependency[] dependencies = new Dependency[]
            {                                                                       // TODO: NOCHECKIN: these dependencies are not correct for vertex pass
                new Dependency("VertexDescriptionInputs.ObjectSpaceNormal",         "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceNormal",          "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceNormal",           "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceTangent",        "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceTangent",         "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceTangent",          "VertexDescriptionInputs.WorldSpaceTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceBiTangent",       "VertexDescriptionInputs.ObjectSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.ViewSpaceBiTangent",        "VertexDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpacePosition",       "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.WorldSpacePosition",        "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.AbsoluteWorldSpacePosition","AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.ViewSpacePosition",         "VertexDescriptionInputs.WorldSpacePosition"),

                new Dependency("VertexDescriptionInputs.WorldSpaceViewDirection",   "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceViewDirection",  "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.ViewSpaceViewDirection",    "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ScreenPosition",            "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.uv0",                       "AttributesMesh.uv0"),
                new Dependency("VertexDescriptionInputs.uv1",                       "AttributesMesh.uv1"),
                new Dependency("VertexDescriptionInputs.uv2",                       "AttributesMesh.uv2"),
                new Dependency("VertexDescriptionInputs.uv3",                       "AttributesMesh.uv3"),
                new Dependency("VertexDescriptionInputs.VertexColor",               "AttributesMesh.color"),
            };
        };

        public static List<Dependency[]> s_Dependencies = new List<Dependency[]>()
        {
            // VaryingsMeshToPS
            new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionRWS",      "AttributesMesh.positionOS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "AttributesMesh.normalOS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "AttributesMesh.tangentOS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "AttributesMesh.uv0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "AttributesMesh.uv1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "AttributesMesh.uv2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "AttributesMesh.uv3"),
                new Dependency("VaryingsMeshToPS.color",            "AttributesMesh.color"),
                new Dependency("VaryingsMeshToPS.instanceID",       "AttributesMesh.instanceID"),
            },
            // FragInputs
            new Dependency[]
            {
                new Dependency("FragInputs.positionRWS",            "VaryingsMeshToPS.positionRWS"),
                new Dependency("FragInputs.tangentToWorld",         "VaryingsMeshToPS.normalWS"),
                new Dependency("FragInputs.tangentToWorld",         "VaryingsMeshToPS.tangentWS"),
                new Dependency("FragInputs.texCoord0",              "VaryingsMeshToPS.texCoord0"),
                new Dependency("FragInputs.texCoord1",              "VaryingsMeshToPS.texCoord1"),
                new Dependency("FragInputs.texCoord2",              "VaryingsMeshToPS.texCoord2"),
                new Dependency("FragInputs.texCoord3",              "VaryingsMeshToPS.texCoord3"),
                new Dependency("FragInputs.color",                  "VaryingsMeshToPS.color"),
                new Dependency("FragInputs.isFrontFace",            "VaryingsMeshToPS.cullFace"),
            },
            // Vertex DescriptionInputs
            new Dependency[]
            {
                new Dependency("VertexDescriptionInputs.ObjectSpaceNormal",         "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceNormal",          "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceNormal",           "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceTangent",        "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceTangent",         "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceTangent",          "VertexDescriptionInputs.WorldSpaceTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceBiTangent",       "VertexDescriptionInputs.ObjectSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.ViewSpaceBiTangent",        "VertexDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpacePosition",       "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.WorldSpacePosition",        "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.AbsoluteWorldSpacePosition","AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.ViewSpacePosition",         "VertexDescriptionInputs.WorldSpacePosition"),

                new Dependency("VertexDescriptionInputs.WorldSpaceViewDirection",   "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceViewDirection",  "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.ViewSpaceViewDirection",    "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ScreenPosition",            "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.uv0",                       "AttributesMesh.uv0"),
                new Dependency("VertexDescriptionInputs.uv1",                       "AttributesMesh.uv1"),
                new Dependency("VertexDescriptionInputs.uv2",                       "AttributesMesh.uv2"),
                new Dependency("VertexDescriptionInputs.uv3",                       "AttributesMesh.uv3"),
                new Dependency("VertexDescriptionInputs.VertexColor",               "AttributesMesh.color"),
            },
            // SurfaceDescriptionInputs
            new Dependency[]
            {
                new Dependency("SurfaceDescriptionInputs.WorldSpaceNormal",          "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceNormal",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceNormal",           "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceTangent",        "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceTangent",          "SurfaceDescriptionInputs.WorldSpaceTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "FragInputs.tangentToWorld"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceBiTangent",      "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceBiTangent",        "SurfaceDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpacePosition",        "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.AbsoluteWorldSpacePosition","FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpacePosition",       "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ViewSpacePosition",         "FragInputs.positionRWS"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceViewDirection",   "FragInputs.positionRWS"),                   // we build WorldSpaceViewDirection using Varyings.positionWS in GetWorldSpaceNormalizeViewDir()
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceViewDirection",  "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceViewDirection",    "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.ScreenPosition",            "SurfaceDescriptionInputs.WorldSpacePosition"),
                new Dependency("SurfaceDescriptionInputs.uv0",                       "FragInputs.texCoord0"),
                new Dependency("SurfaceDescriptionInputs.uv1",                       "FragInputs.texCoord1"),
                new Dependency("SurfaceDescriptionInputs.uv2",                       "FragInputs.texCoord2"),
                new Dependency("SurfaceDescriptionInputs.uv3",                       "FragInputs.texCoord3"),
                new Dependency("SurfaceDescriptionInputs.VertexColor",               "FragInputs.color"),
                new Dependency("SurfaceDescriptionInputs.FaceSign",                  "FragInputs.isFrontFace"),

                new Dependency("DepthOffset", "FragInputs.positionWS"),
            },
        };
    };

    static class HDSubShaderUtilities
    {
        public static void AddTags(ShaderGenerator generator, string pipeline, HDRenderTypeTags renderType, int queue)
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine("\"RenderPipeline\"=\"{0}\"", pipeline);
                builder.AppendLine("\"RenderType\"=\"{0}\"", renderType);
                builder.AppendLine("\"Queue\" = \"{0}\"", HDRenderQueue.GetShaderTagValue(queue));
            }

            generator.AddShaderChunk(builder.ToString());
        }

        // Utils property to add properties to the collector, all hidden because we use a custom UI to display them
        static void AddIntProperty(this PropertyCollector collector, string referenceName, int defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Integer,
                value = defaultValue,
                hidden = true,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Default,
                hidden = true,
                value = defaultValue,
                overrideReferenceName = referenceName,
            });
        }

        static void AddFloatProperty(this PropertyCollector collector, string referenceName, string displayName, float defaultValue)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty{
                floatType = FloatType.Default,
                value = defaultValue,
                overrideReferenceName = referenceName,
                hidden = true,
                displayName = displayName,
            });
        }

        static void AddToggleProperty(this PropertyCollector collector, string referenceName, bool defaultValue)
        {
            collector.AddShaderProperty(new BooleanShaderProperty{
                value = defaultValue,
                hidden = true,
                overrideReferenceName = referenceName,
            });
        }

        public static void AddStencilShaderProperties(PropertyCollector collector, bool splitLighting, bool receiveSSR)
        {
            // All these properties values will be patched with the material keyword update
            collector.AddIntProperty("_StencilRef", 0); // StencilLightingUsage.NoLighting
            collector.AddIntProperty("_StencilWriteMask", 3); // StencilMask.Lighting
            // Depth prepass
            collector.AddIntProperty("_StencilRefDepth", 0); // Nothing
            collector.AddIntProperty("_StencilWriteMaskDepth", 32); // DoesntReceiveSSR
            // Motion vector pass
            collector.AddIntProperty("_StencilRefMV", 128); // StencilBitMask.ObjectMotionVectors
            collector.AddIntProperty("_StencilWriteMaskMV", 128); // StencilBitMask.ObjectMotionVectors
            // Distortion vector pass
            collector.AddIntProperty("_StencilRefDistortionVec", 64); // StencilBitMask.DistortionVectors
            collector.AddIntProperty("_StencilWriteMaskDistortionVec", 64); // StencilBitMask.DistortionVectors
            // Gbuffer
            collector.AddIntProperty("_StencilWriteMaskGBuffer", 3); // StencilMask.Lighting
            collector.AddIntProperty("_StencilRefGBuffer", 2); // StencilLightingUsage.RegularLighting
            collector.AddIntProperty("_ZTestGBuffer", 4);

            collector.AddToggleProperty(kUseSplitLighting, splitLighting);
            collector.AddToggleProperty(kReceivesSSR, receiveSSR);

        }

        public static void AddBlendingStatesShaderProperties(
            PropertyCollector collector, SurfaceType surface, BlendMode blend, int sortingPriority,
            bool zWrite, TransparentCullMode transparentCullMode, CompareFunction zTest, bool backThenFrontRendering)
        {
            collector.AddFloatProperty("_SurfaceType", (int)surface);
            collector.AddFloatProperty("_BlendMode", (int)blend);

            // All these properties values will be patched with the material keyword update
            collector.AddFloatProperty("_SrcBlend", 1.0f);
            collector.AddFloatProperty("_DstBlend", 0.0f);
            collector.AddFloatProperty("_AlphaSrcBlend", 1.0f);
            collector.AddFloatProperty("_AlphaDstBlend", 0.0f);
            collector.AddToggleProperty("_ZWrite", zWrite);
            collector.AddFloatProperty("_CullMode", (int)CullMode.Back);
            collector.AddIntProperty("_TransparentSortPriority", sortingPriority);
            collector.AddFloatProperty("_CullModeForward", (int)CullMode.Back);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = kTransparentCullMode,
                floatType = FloatType.Enum,
                value = (int)transparentCullMode,
                enumNames = {"Front", "Back"},
                enumValues = {(int)TransparentCullMode.Front, (int)TransparentCullMode.Back},
                hidden = true,
            });

            // Add ZTest properties:
            collector.AddIntProperty("_ZTestDepthEqualForOpaque", (int)CompareFunction.LessEqual);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = kZTestTransparent,
                floatType = FloatType.Enum,
                value = (int)zTest,
                enumType = EnumType.CSharpEnum,
                cSharpEnumType = typeof(CompareFunction),
                hidden = true,
            });

            collector.AddToggleProperty(kTransparentBackfaceEnable, backThenFrontRendering);
        }

        public static void AddAlphaCutoffShaderProperties(PropertyCollector collector, bool alphaCutoff, bool shadowThreshold)
        {
            collector.AddToggleProperty("_AlphaCutoffEnable", alphaCutoff);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                overrideReferenceName = "_AlphaCutoff",
                displayName = "Alpha Cutoff",
                floatType = FloatType.Slider,
                rangeValues = new Vector2(0, 1),
                hidden = true,
                value = 0.5f
            });
            collector.AddFloatProperty("_TransparentSortPriority", "_TransparentSortPriority", 0);
            collector.AddToggleProperty("_UseShadowThreshold", shadowThreshold);
        }

        public static void AddDoubleSidedProperty(PropertyCollector collector, DoubleSidedMode mode = DoubleSidedMode.Enabled)
        {
            var normalMode = ConvertDoubleSidedModeToDoubleSidedNormalMode(mode);
            collector.AddToggleProperty("_DoubleSidedEnable", mode != DoubleSidedMode.Disabled);
            collector.AddShaderProperty(new Vector1ShaderProperty{
                enumNames = {"Flip", "Mirror", "None"}, // values will be 0, 1 and 2
                floatType = FloatType.Enum,
                overrideReferenceName = "_DoubleSidedNormalMode",
                hidden = true,
                value = (int)normalMode
            });
            collector.AddShaderProperty(new Vector4ShaderProperty{
                overrideReferenceName = "_DoubleSidedConstants",
                hidden = true,
                value = new Vector4(1, 1, -1, 0)
            });
        }

        public static string RenderQueueName(HDRenderQueue.RenderQueueType value)
        {
            switch (value)
            {
                case HDRenderQueue.RenderQueueType.Opaque:
                    return "Default";
                case HDRenderQueue.RenderQueueType.AfterPostProcessOpaque:
                    return "After Post-process";
                case HDRenderQueue.RenderQueueType.PreRefraction:
                    return "Before Refraction";
                case HDRenderQueue.RenderQueueType.Transparent:
                    return "Default";
                case HDRenderQueue.RenderQueueType.LowTransparent:
                    return "Low Resolution";
                case HDRenderQueue.RenderQueueType.AfterPostprocessTransparent:
                    return "After Post-process";

#if ENABLE_RAYTRACING
                case HDRenderQueue.RenderQueueType.RaytracingOpaque: return "Raytracing";
                case HDRenderQueue.RenderQueueType.RaytracingTransparent: return "Raytracing";
#endif
                default:
                    return "None";
            }
        }

        public static System.Collections.Generic.List<HDRenderQueue.RenderQueueType> GetRenderingPassList(bool opaque, bool needAfterPostProcess)
        {
            var result = new System.Collections.Generic.List<HDRenderQueue.RenderQueueType>();
            if (opaque)
            {
                result.Add(HDRenderQueue.RenderQueueType.Opaque);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostProcessOpaque);
#if ENABLE_RAYTRACING
                result.Add(HDRenderQueue.RenderQueueType.RaytracingOpaque);
#endif
            }
            else
            {
                result.Add(HDRenderQueue.RenderQueueType.PreRefraction);
                result.Add(HDRenderQueue.RenderQueueType.Transparent);
                result.Add(HDRenderQueue.RenderQueueType.LowTransparent);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostprocessTransparent);
#if ENABLE_RAYTRACING
                result.Add(HDRenderQueue.RenderQueueType.RaytracingTransparent);
#endif
            }

            return result;
        }

        public static BlendMode ConvertAlphaModeToBlendMode(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Additive:
                    return BlendMode.Additive;
                case AlphaMode.Alpha:
                    return BlendMode.Alpha;
                case AlphaMode.Premultiply:
                    return BlendMode.Premultiply;
                case AlphaMode.Multiply: // In case of multiply we fall back to alpha
                    return BlendMode.Alpha;
                default:
                    throw new System.Exception("Unknown AlphaMode: " + alphaMode + ": can't convert to BlendMode.");
            }
        }

        public static DoubleSidedNormalMode ConvertDoubleSidedModeToDoubleSidedNormalMode(DoubleSidedMode shaderGraphMode)
        {
            switch (shaderGraphMode)
            {
                case DoubleSidedMode.FlippedNormals:
                    return DoubleSidedNormalMode.Flip;
                case DoubleSidedMode.MirroredNormals:
                    return DoubleSidedNormalMode.Mirror;
                case DoubleSidedMode.Enabled:
                case DoubleSidedMode.Disabled:
                default:
                    return DoubleSidedNormalMode.None;
            }
        }
    }
}
