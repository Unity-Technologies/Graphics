using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static class UniversalShaderGraphResources
    {
        public static string s_ResourceClassName => typeof(UniversalShaderGraphResources).FullName;

        public static string s_AssemblyName => typeof(UniversalShaderGraphResources).Assembly.FullName.ToString();

        struct UInt32_4
        { }

        internal struct Attributes
        {
            [Semantic("POSITION")]
            Vector3 positionOS;
            [Semantic("NORMAL")][Optional]
            Vector3 normalOS;
            [Semantic("TANGENT")][Optional]
            Vector4 tangentOS;
            [Semantic("TEXCOORD0")][Optional]
            Vector4 uv0;
            [Semantic("TEXCOORD1")][Optional]
            Vector4 uv1;
            [Semantic("TEXCOORD2")][Optional]
            Vector4 uv2;
            [Semantic("TEXCOORD3")][Optional]
            Vector4 uv3;
            [Semantic("COLOR")][Optional]
            Vector4 color;
            [Semantic("BLENDWEIGHTS")][Optional]
            Vector4 weights;
            [Semantic("BLENDINDICES")][Optional]
            UInt32_4 indices;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")]
            uint instanceID;
        };

        [InterpolatorPack]
        internal struct Varyings
        {
            [Semantic("SV_POSITION")]
            Vector4 positionCS;
            [Optional]
            Vector3 positionWS;
            [Optional]
            Vector3 normalWS;
            [Optional]
            Vector4 tangentWS;
            [Optional]
            Vector4 texCoord0;
            [Optional]
            Vector4 texCoord1;
            [Optional]
            Vector4 texCoord2;
            [Optional]
            Vector4 texCoord3;
            [Optional]
            Vector4 color;
            [Optional]
            Vector3 viewDirectionWS;
            [Optional]
            Vector4 screenPosition;
            [Optional][PreprocessorIf("defined(LIGHTMAP_ON)")]
            Vector2 lightmapUV;
            [Optional][PreprocessorIf("!defined(LIGHTMAP_ON)")]
            Vector3 sh;
            [Optional]
            Vector4 fogFactorAndVertexLight;
            [Optional]
            Vector4 shadowCoord;
            [Semantic("CUSTOM_INSTANCE_ID")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")]
            uint instanceID;
            [Semantic("SV_RenderTargetArrayIndex")]
            [SystemGenerated]
            [PreprocessorIf("(defined(UNITY_STEREO_INSTANCING_ENABLED))")]
            uint stereoTargetEyeIndexAsRTArrayIdx;
            [Semantic("BLENDINDICES0")]
            [SystemGenerated]
            [PreprocessorIf("(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || " +
                            "(defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))")]
            uint stereoTargetEyeIndexAsBlendIdx0;
            [Semantic("FRONT_FACE_SEMANTIC")]
            [SystemGenerated]
            [OverrideType("FRONT_FACE_TYPE")]
            [PreprocessorIf("defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)")]
            bool cullFace;
        };

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
            [Optional] Vector4 BoneWeights;
            [Optional] UInt32_4 BoneIndices;
        };
        
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
            [Optional] Vector3 TimeParameters;
            [Optional] float FaceSign;
        };

        public static List<Dependency[]> s_Dependencies = new List<Dependency[]>()
        {
            // Varyings
            new Dependency[]
            {
                new Dependency("Varyings.positionWS",                           "Attributes.positionOS"),
                new Dependency("Varyings.normalWS",                             "Attributes.normalOS"),
                new Dependency("Varyings.tangentWS",                            "Attributes.tangentOS"),
                new Dependency("Varyings.texCoord0",                            "Attributes.uv0"),
                new Dependency("Varyings.texCoord1",                            "Attributes.uv1"),
                new Dependency("Varyings.texCoord2",                            "Attributes.uv2"),
                new Dependency("Varyings.texCoord3",                            "Attributes.uv3"),
                new Dependency("Varyings.color",                                "Attributes.color"),
                new Dependency("Varyings.instanceID",                           "Attributes.instanceID"),
                new Dependency("Varyings.stereoTargetEyeIndexAsBlendIdx0",      "Attributes.instanceID"),
                new Dependency("Varyings.stereoTargetEyeIndexAsRTArrayIdx",     "Attributes.instanceID"),
            },
            // Vertex DescriptionInputs
            new Dependency[]
            {
                new Dependency("VertexDescriptionInputs.ObjectSpaceNormal",         "Attributes.normalOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceNormal",          "Attributes.normalOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceNormal",           "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceTangent",        "Attributes.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceTangent",         "Attributes.tangentOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceTangent",          "VertexDescriptionInputs.WorldSpaceTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "Attributes.normalOS"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "Attributes.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceBiTangent",       "VertexDescriptionInputs.ObjectSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.ViewSpaceBiTangent",        "VertexDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpacePosition",       "Attributes.positionOS"),
                new Dependency("VertexDescriptionInputs.WorldSpacePosition",        "Attributes.positionOS"),
                new Dependency("VertexDescriptionInputs.AbsoluteWorldSpacePosition","Attributes.positionOS"),
                new Dependency("VertexDescriptionInputs.ViewSpacePosition",         "VertexDescriptionInputs.WorldSpacePosition"),

                new Dependency("VertexDescriptionInputs.WorldSpaceViewDirection",   "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceViewDirection",  "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.ViewSpaceViewDirection",    "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ScreenPosition",            "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.uv0",                       "Attributes.uv0"),
                new Dependency("VertexDescriptionInputs.uv1",                       "Attributes.uv1"),
                new Dependency("VertexDescriptionInputs.uv2",                       "Attributes.uv2"),
                new Dependency("VertexDescriptionInputs.uv3",                       "Attributes.uv3"),
                new Dependency("VertexDescriptionInputs.VertexColor",               "Attributes.color"),
                new Dependency("VertexDescriptionInputs.BoneWeights",               "Attributes.weights"),
                new Dependency("VertexDescriptionInputs.BoneIndices",               "Attributes.indices")
            },
            // SurfaceDescriptionInputs
            new Dependency[]
            {
                new Dependency("SurfaceDescriptionInputs.WorldSpaceNormal",          "Varyings.normalWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceNormal",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceNormal",           "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "Varyings.tangentWS"),
                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceTangent",        "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceTangent",          "SurfaceDescriptionInputs.WorldSpaceTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceBiTangent",      "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceBiTangent",        "SurfaceDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpacePosition",        "Varyings.positionWS"),
                new Dependency("SurfaceDescriptionInputs.AbsoluteWorldSpacePosition","Varyings.positionWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpacePosition",       "Varyings.positionWS"),
                new Dependency("SurfaceDescriptionInputs.ViewSpacePosition",         "Varyings.positionWS"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceViewDirection",   "Varyings.viewDirectionWS"),                   // we build WorldSpaceViewDirection using Varyings.positionWS in GetWorldSpaceNormalizeViewDir()
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceViewDirection",  "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceViewDirection",    "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.ScreenPosition",            "SurfaceDescriptionInputs.WorldSpacePosition"),
                new Dependency("SurfaceDescriptionInputs.uv0",                       "Varyings.texCoord0"),
                new Dependency("SurfaceDescriptionInputs.uv1",                       "Varyings.texCoord1"),
                new Dependency("SurfaceDescriptionInputs.uv2",                       "Varyings.texCoord2"),
                new Dependency("SurfaceDescriptionInputs.uv3",                       "Varyings.texCoord3"),
                new Dependency("SurfaceDescriptionInputs.VertexColor",               "Varyings.color"),
                new Dependency("SurfaceDescriptionInputs.FaceSign",                  "Varyings.cullFace"),

                new Dependency("DepthOffset", "Varyings.positionWS"),
            },
        };
    };
}
