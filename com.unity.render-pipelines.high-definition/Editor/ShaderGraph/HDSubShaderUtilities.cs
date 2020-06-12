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

        struct UInt32_4
        {}

        internal struct AttributesMesh
        {
            [Semantic("POSITION")]                  Vector3 positionOS;
            [Semantic("NORMAL")]                    Vector3 normalOS;
            [Semantic("TANGENT")]                   Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]       Vector4 uv0;
            [Semantic("TEXCOORD1")][Optional]       Vector4 uv1;
            [Semantic("TEXCOORD2")][Optional]       Vector4 uv2;
            [Semantic("TEXCOORD3")][Optional]       Vector4 uv3;
            [Semantic("BLENDWEIGHTS")][Optional]    Vector4 weights;
            [Semantic("BLENDINDICES")][Optional]    UInt32_4 indices;
            [Semantic("COLOR")][Optional]           Vector4 color;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("UNITY_ANY_INSTANCING_ENABLED")] uint instanceID;
        };

        [InterpolatorPack]
        internal struct VaryingsMeshToPS
        {
            [Semantic("SV_POSITION")]                                               Vector4 positionCS;
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
            [Optional] Vector4 BoneWeights;
            [Optional] UInt32_4 BoneIndices;

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

                new Dependency("VertexDescriptionInputs.BoneWeights",               "AttributesMesh.weights"),
                new Dependency("VertexDescriptionInputs.BoneIndices",               "AttributesMesh.indices")
            };
        };

        // TODO: move this out of HDRPShaderStructs
        static public void AddActiveFieldsFromVertexGraphRequirements(IActiveFieldsSet activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.AddAll("VertexDescriptionInputs.ScreenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.AddAll("VertexDescriptionInputs.VertexColor");
            }

            if (requirements.requiresNormal != 0)
            {
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.TangentSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0)
                    activeFields.AddAll("VertexDescriptionInputs.AbsoluteWorldSpacePosition");
            }

            if (requirements.requiresVertexSkinning)
            {
                activeFields.AddAll("VertexDescriptionInputs.BoneWeights");
                activeFields.AddAll("VertexDescriptionInputs.BoneIndices");
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.AddAll("VertexDescriptionInputs." + channel.GetUVName());
            }

            if (requirements.requiresTime)
            {
                activeFields.AddAll("VertexDescriptionInputs.TimeParameters");
            }
        }

        // TODO: move this out of HDRPShaderStructs
        static public void AddActiveFieldsFromPixelGraphRequirements(IActiveFields activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("SurfaceDescriptionInputs.ScreenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("SurfaceDescriptionInputs.VertexColor");
            }

            if (requirements.requiresFaceSign)
            {
                activeFields.Add("SurfaceDescriptionInputs.FaceSign");
            }

            if (requirements.requiresNormal != 0)
            {
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.AbsoluteWorldSpacePosition");
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("SurfaceDescriptionInputs." + channel.GetUVName());
            }

            if (requirements.requiresTime)
            {
                activeFields.Add("SurfaceDescriptionInputs.TimeParameters");
            }
        }

        public static void AddRequiredFields(
            List<string> passRequiredFields,            // fields the pass requires
            IActiveFieldsSet activeFields)
        {
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.AddAll(requiredField);
                }
            }
        }
    };

    delegate void OnGeneratePassDelegate(IMasterNode masterNode, ref Pass pass);
    struct Pass
    {
        public string Name;
        public string LightMode;
        public string ShaderPassName;
        public List<string> Includes;
        public string TemplateName;
        public string MaterialName;
        public List<string> ShaderStages;
        public List<string> ExtraInstancingOptions;
        public List<string> ExtraDefines;
        public List<int> VertexShaderSlots;         // These control what slots are used by the pass vertex shader
        public List<int> PixelShaderSlots;          // These control what slots are used by the pass pixel shader
        public string CullOverride;
        public string BlendOverride;
        public string BlendOpOverride;
        public string ZTestOverride;
        public string ZWriteOverride;
        public string ColorMaskOverride;
        public string ZClipOverride;
        public List<string> StencilOverride;
        public List<string> RequiredFields;         // feeds into the dependency analysis
        public bool UseInPreview;

        // All these lists could probably be hashed to aid lookups.
        public bool VertexShaderUsesSlot(int slotId)
        {
            return VertexShaderSlots.Contains(slotId);
        }
        public bool PixelShaderUsesSlot(int slotId)
        {
            return PixelShaderSlots.Contains(slotId);
        }
        public void OnGeneratePass(IMasterNode masterNode)
        {
            if (OnGeneratePassImpl != null)
            {
                OnGeneratePassImpl(masterNode, ref this);
            }
        }
        public OnGeneratePassDelegate OnGeneratePassImpl;
    }
    static class HDSubShaderUtilities
    {

        static List<Dependency[]> k_Dependencies = new List<Dependency[]>()
        {
            HDRPShaderStructs.FragInputs.dependencies,
            HDRPShaderStructs.VaryingsMeshToPS.standardDependencies,
            HDRPShaderStructs.SurfaceDescriptionInputs.dependencies,
            HDRPShaderStructs.VertexDescriptionInputs.dependencies
        };

        public static bool GenerateShaderPass(AbstractMaterialNode masterNode, Pass pass, GenerationMode mode, ActiveFields activeFields, ShaderGenerator result, List<string> sourceAssetDependencyPaths, bool vertexActive, bool isLit = true, bool instancingFlag = true)
        {
            string templatePath = Path.Combine(HDUtils.GetHDRenderPipelinePath(), "Editor/Material");
            string templateLocation = Path.Combine(Path.Combine(Path.Combine(templatePath, pass.MaterialName), "ShaderGraph"), pass.TemplateName);
            if (!File.Exists(templateLocation))
            {
                // TODO: produce error here
                Debug.LogError("Template not found: " + templateLocation);
                return false;
            }

            bool debugOutput = true;

            // grab all of the active nodes (for pixel and vertex graphs)
            var vertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots);

            var pixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            // graph requirements describe what the graph itself requires
            ShaderGraphRequirementsPerKeyword pixelRequirements = new ShaderGraphRequirementsPerKeyword();
            ShaderGraphRequirementsPerKeyword vertexRequirements = new ShaderGraphRequirementsPerKeyword();
            ShaderGraphRequirementsPerKeyword graphRequirements = new ShaderGraphRequirementsPerKeyword();

            // Function Registry tracks functions to remove duplicates, it wraps a string builder that stores the combined function string
            ShaderStringBuilder graphNodeFunctions = new ShaderStringBuilder();
            graphNodeFunctions.IncreaseIndent();
            var functionRegistry = new FunctionRegistry(graphNodeFunctions);

            // TODO: this can be a shared function for all HDRP master nodes -- From here through GraphUtil.GenerateSurfaceDescription(..)

            // Build the list of active slots based on what the pass requires
            var pixelSlots = HDSubShaderUtilities.FindMaterialSlotsOnNode(pass.PixelShaderSlots, masterNode);
            var vertexSlots = HDSubShaderUtilities.FindMaterialSlotsOnNode(pass.VertexShaderSlots, masterNode);

            // properties used by either pixel and vertex shader
            PropertyCollector sharedProperties = new PropertyCollector();
            KeywordCollector sharedKeywords = new KeywordCollector();
            ShaderStringBuilder shaderPropertyUniforms = new ShaderStringBuilder(1);
            ShaderStringBuilder shaderKeywordDeclarations = new ShaderStringBuilder(1);
            ShaderStringBuilder shaderKeywordPermutations = new ShaderStringBuilder(1);

            // build the graph outputs structure to hold the results of each active slots (and fill out activeFields to indicate they are active)
            string pixelGraphInputStructName = "SurfaceDescriptionInputs";
            string pixelGraphOutputStructName = "SurfaceDescription";
            string pixelGraphEvalFunctionName = "SurfaceDescriptionFunction";
            ShaderStringBuilder pixelGraphEvalFunction = new ShaderStringBuilder();
            ShaderStringBuilder pixelGraphOutputs = new ShaderStringBuilder();

            // ----------------------------------------------------- //
            //                         KEYWORDS                      //
            // ----------------------------------------------------- //

            // -------------------------------------
            // Get keyword permutations

            masterNode.owner.CollectShaderKeywords(sharedKeywords, mode);

            // Track permutation indices for all nodes
            List<int>[] keywordPermutationsPerVertexNode = new List<int>[vertexNodes.Count];
            List<int>[] keywordPermutationsPerPixelNode = new List<int>[pixelNodes.Count];

            // -------------------------------------
            // Evaluate all permutations

            if (sharedKeywords.permutations.Count > 0)
            {
                for(int i = 0; i < sharedKeywords.permutations.Count; i++)
                {
                    // Get active nodes for this permutation
                    var localVertexNodes = UnityEngine.Rendering.ListPool<AbstractMaterialNode>.Get();
                    var localPixelNodes = UnityEngine.Rendering.ListPool<AbstractMaterialNode>.Get();
                    NodeUtils.DepthFirstCollectNodesFromNode(localVertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots, sharedKeywords.permutations[i]);
                    NodeUtils.DepthFirstCollectNodesFromNode(localPixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots, sharedKeywords.permutations[i]);

                    // Track each vertex node in this permutation
                    foreach(AbstractMaterialNode vertexNode in localVertexNodes)
                    {
                        int nodeIndex = vertexNodes.IndexOf(vertexNode);

                        if(keywordPermutationsPerVertexNode[nodeIndex] == null)
                            keywordPermutationsPerVertexNode[nodeIndex] = new List<int>();
                        keywordPermutationsPerVertexNode[nodeIndex].Add(i);
                    }

                    // Track each pixel node in this permutation
                    foreach(AbstractMaterialNode pixelNode in localPixelNodes)
                    {
                        int nodeIndex = pixelNodes.IndexOf(pixelNode);

                        if(keywordPermutationsPerPixelNode[nodeIndex] == null)
                            keywordPermutationsPerPixelNode[nodeIndex] = new List<int>();
                        keywordPermutationsPerPixelNode[nodeIndex].Add(i);
                    }

                    // Get active requirements for this permutation
                    var localVertexRequirements = ShaderGraphRequirements.FromNodes(localVertexNodes, ShaderStageCapability.Vertex, false);
                    var localPixelRequirements = ShaderGraphRequirements.FromNodes(localPixelNodes, ShaderStageCapability.Fragment, false);

                    vertexRequirements[i].SetRequirements(localVertexRequirements);
                    pixelRequirements[i].SetRequirements(localPixelRequirements);

                    // build initial requirements
                    HDRPShaderStructs.AddActiveFieldsFromPixelGraphRequirements(activeFields[i], localPixelRequirements);
                    HDRPShaderStructs.AddActiveFieldsFromVertexGraphRequirements(activeFields[i], localVertexRequirements);
                }
            }
            else
            {
                pixelRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false));   // TODO: is ShaderStageCapability.Fragment correct?
                vertexRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false));
                HDRPShaderStructs.AddActiveFieldsFromPixelGraphRequirements(activeFields.baseInstance, pixelRequirements.baseInstance.requirements);
                HDRPShaderStructs.AddActiveFieldsFromVertexGraphRequirements(activeFields.baseInstance, vertexRequirements.baseInstance.requirements);
            }

            graphRequirements.UnionWith(pixelRequirements);
            graphRequirements.UnionWith(vertexRequirements);

            // build the graph outputs structure, and populate activeFields with the fields of that structure
            SubShaderGenerator.GenerateSurfaceDescriptionStruct(pixelGraphOutputs, pixelSlots, pixelGraphOutputStructName, activeFields.baseInstance);

            // Build the graph evaluation code, to evaluate the specified slots
            SubShaderGenerator.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                keywordPermutationsPerPixelNode,
                masterNode,
                masterNode.owner as GraphData,
                pixelGraphEvalFunction,
                functionRegistry,
                sharedProperties,
                sharedKeywords,
                mode,
                pixelGraphEvalFunctionName,
                pixelGraphOutputStructName,
                null,
                pixelSlots,
                pixelGraphInputStructName);

            string vertexGraphInputStructName = "VertexDescriptionInputs";
            string vertexGraphOutputStructName = "VertexDescription";
            string vertexGraphEvalFunctionName = "VertexDescriptionFunction";
            ShaderStringBuilder vertexGraphEvalFunction = new ShaderStringBuilder();
            ShaderStringBuilder vertexGraphOutputs = new ShaderStringBuilder();

            // check for vertex animation -- enables HAVE_VERTEX_MODIFICATION
            if (vertexActive)
            {
                vertexActive = true;
                activeFields.baseInstance.Add("features.modifyMesh");

                // -------------------------------------
                // Generate Output structure for Vertex Description function
                SubShaderGenerator.GenerateVertexDescriptionStruct(vertexGraphOutputs, vertexSlots, vertexGraphOutputStructName, activeFields.baseInstance);

                // -------------------------------------
                // Generate Vertex Description function
                SubShaderGenerator.GenerateVertexDescriptionFunction(
                    masterNode.owner as GraphData,
                    vertexGraphEvalFunction,
                    functionRegistry,
                    sharedProperties,
                    sharedKeywords,
                    mode,
                    masterNode,
                    vertexNodes,
                    keywordPermutationsPerVertexNode,
                    vertexSlots,
                    vertexGraphInputStructName,
                    vertexGraphEvalFunctionName,
                    vertexGraphOutputStructName);
            }

            var blendCode = new ShaderStringBuilder();
            var cullCode = new ShaderStringBuilder();
            var zTestCode = new ShaderStringBuilder();
            var zWriteCode = new ShaderStringBuilder();
            var zClipCode = new ShaderStringBuilder();
            var stencilCode = new ShaderStringBuilder();
            var colorMaskCode = new ShaderStringBuilder();
            var dotsInstancingCode = new ShaderStringBuilder();
            HDSubShaderUtilities.BuildRenderStatesFromPass(pass, blendCode, cullCode, zTestCode, zWriteCode, zClipCode, stencilCode, colorMaskCode);

            HDRPShaderStructs.AddRequiredFields(pass.RequiredFields, activeFields.baseInstance);

            // Get keyword declarations
            sharedKeywords.GetKeywordsDeclaration(shaderKeywordDeclarations, mode);

            // Get property declarations
            sharedProperties.GetPropertiesDeclaration(shaderPropertyUniforms, mode, masterNode.owner.concretePrecision);

            // propagate active field requirements using dependencies
            foreach (var instance in activeFields.all.instances)
                ShaderSpliceUtil.ApplyDependencies(instance, k_Dependencies);

            // debug output all active fields
            var interpolatorDefines = new ShaderGenerator();
            if (debugOutput)
            {
                interpolatorDefines.AddShaderChunk("// ACTIVE FIELDS:");
                foreach (string f in activeFields.baseInstance.fields)
                {
                    interpolatorDefines.AddShaderChunk("//   " + f);
                }
            }

            // build graph inputs structures
            ShaderGenerator pixelGraphInputs = new ShaderGenerator();
            ShaderSpliceUtil.BuildType(typeof(HDRPShaderStructs.SurfaceDescriptionInputs), activeFields, pixelGraphInputs, debugOutput);
            ShaderGenerator vertexGraphInputs = new ShaderGenerator();
            ShaderSpliceUtil.BuildType(typeof(HDRPShaderStructs.VertexDescriptionInputs), activeFields, vertexGraphInputs, debugOutput);

            ShaderGenerator instancingOptions = new ShaderGenerator();

            if (instancingFlag)
            {
                int instancedCount = sharedProperties.GetDotsInstancingPropertiesCount(mode);
                bool isDotsInstancing = masterNode is MasterNode node && node.dotsInstancing.isOn;

                instancingOptions.AddShaderChunk("#pragma multi_compile_instancing", true);

                if (isDotsInstancing)
                {
                    instancingOptions.AddShaderChunk("#define UNITY_DOTS_SHADER");
                }

                if (isLit)
                {
                    if (isDotsInstancing)
                    {
                        instancingOptions.AddShaderChunk("#pragma instancing_options nolightprobe");
                        instancingOptions.AddShaderChunk("#pragma instancing_options nolodfade");
                    }
                    else
                    {
                        instancingOptions.AddShaderChunk("#pragma instancing_options renderinglayer");
                    }
                }

                if (pass.ExtraInstancingOptions != null)
                {
                    foreach (var instancingOption in pass.ExtraInstancingOptions)
                        instancingOptions.AddShaderChunk(instancingOption);
                }

                if (instancedCount > 0)
                {
                    instancingOptions.AddShaderChunk("#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL))");
                    instancingOptions.AddShaderChunk("#define UNITY_SUPPORT_INSTANCING");
                    instancingOptions.AddShaderChunk("#endif");
                    instancingOptions.AddShaderChunk("#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)");
                    instancingOptions.AddShaderChunk("#define UNITY_DOTS_INSTANCING_ENABLED");
                    instancingOptions.AddShaderChunk("#endif");

                    dotsInstancingCode.AppendLine("//-------------------------------------------------------------------------------------");
                    dotsInstancingCode.AppendLine("// Dots Instancing vars");
                    dotsInstancingCode.AppendLine("//-------------------------------------------------------------------------------------");
                    dotsInstancingCode.AppendLine("");

                    dotsInstancingCode.Append(sharedProperties.GetDotsInstancingPropertiesDeclaration(mode));
                }
            }

            ShaderGenerator shaderStages = new ShaderGenerator();
            {
                if (pass.ShaderStages != null)
                {
                    foreach (var shaderStage in pass.ShaderStages)
                        shaderStages.AddShaderChunk(shaderStage);
                }
            }

            ShaderGenerator defines = new ShaderGenerator();
            {
                defines.AddShaderChunk("// Shared Graph Keywords");
                defines.AddShaderChunk(shaderKeywordDeclarations.ToString());
                defines.AddShaderChunk(shaderKeywordPermutations.ToString());

                defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);
                if (pass.ExtraDefines != null)
                {
                    foreach (var define in pass.ExtraDefines)
                        defines.AddShaderChunk(define);
                }

                if (graphRequirements.permutationCount > 0)
                {
                    {
                        var activePermutationIndices = graphRequirements.allPermutations.instances
                            .Where(p => p.requirements.requiresDepthTexture)
                            .Select(p => p.permutationIndex)
                            .ToList();
                        if (activePermutationIndices.Count > 0)
                        {
                            defines.AddShaderChunk(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                            defines.AddShaderChunk("#define REQUIRE_DEPTH_TEXTURE");
                            defines.AddShaderChunk("#endif");
                        }
                    }

                    {
                        var activePermutationIndices = graphRequirements.allPermutations.instances
                            .Where(p => p.requirements.requiresCameraOpaqueTexture)
                            .Select(p => p.permutationIndex)
                            .ToList();
                        if (activePermutationIndices.Count > 0)
                        {
                            defines.AddShaderChunk(KeywordUtil.GetKeywordPermutationSetConditional(activePermutationIndices));
                            defines.AddShaderChunk("#define REQUIRE_OPAQUE_TEXTURE");
                            defines.AddShaderChunk("#endif");
                        }
                    }
                }
                else
                {
                    if (graphRequirements.baseInstance.requirements.requiresDepthTexture)
                        defines.AddShaderChunk("#define REQUIRE_DEPTH_TEXTURE");
                    if (graphRequirements.baseInstance.requirements.requiresCameraOpaqueTexture)
                        defines.AddShaderChunk("#define REQUIRE_OPAQUE_TEXTURE");
                }

                defines.AddGenerator(interpolatorDefines);
            }

            var shaderPassIncludes = new ShaderGenerator();
            if (pass.Includes != null)
            {
                foreach (var include in pass.Includes)
                    shaderPassIncludes.AddShaderChunk(include);
            }

            // build graph code
            var graph = new ShaderGenerator();
            {
                graph.AddShaderChunk("// Shared Graph Properties (uniform inputs)");
                graph.AddShaderChunk(shaderPropertyUniforms.ToString());

                if (vertexActive)
                {
                    graph.AddShaderChunk("// Vertex Graph Inputs");
                    graph.Indent();
                    graph.AddGenerator(vertexGraphInputs);
                    graph.Deindent();
                    graph.AddShaderChunk("// Vertex Graph Outputs");
                    graph.Indent();
                    graph.AddShaderChunk(vertexGraphOutputs.ToString());
                    graph.Deindent();
                }

                graph.AddShaderChunk("// Pixel Graph Inputs");
                graph.Indent();
                graph.AddGenerator(pixelGraphInputs);
                graph.Deindent();
                graph.AddShaderChunk("// Pixel Graph Outputs");
                graph.Indent();
                graph.AddShaderChunk(pixelGraphOutputs.ToString());
                graph.Deindent();

                graph.AddShaderChunk("// Shared Graph Node Functions");
                graph.AddShaderChunk(graphNodeFunctions.ToString());

                if (vertexActive)
                {
                    graph.AddShaderChunk("// Vertex Graph Evaluation");
                    graph.Indent();
                    graph.AddShaderChunk(vertexGraphEvalFunction.ToString());
                    graph.Deindent();
                }

                graph.AddShaderChunk("// Pixel Graph Evaluation");
                graph.Indent();
                graph.AddShaderChunk(pixelGraphEvalFunction.ToString());
                graph.Deindent();
            }

            // build the hash table of all named fragments      TODO: could make this Dictionary<string, ShaderGenerator / string>  ?
            Dictionary<string, string> namedFragments = new Dictionary<string, string>();
            namedFragments.Add("InstancingOptions", instancingOptions.GetShaderString(0, false));
            namedFragments.Add("ShaderStages", shaderStages.GetShaderString(2, false));
            namedFragments.Add("Defines", defines.GetShaderString(2, false));
            namedFragments.Add("Graph", graph.GetShaderString(2, false));
            namedFragments.Add("LightMode", pass.LightMode);
            namedFragments.Add("PassName", pass.Name);
            namedFragments.Add("Includes", shaderPassIncludes.GetShaderString(2, false));
            namedFragments.Add("Blending", blendCode.ToString());
            namedFragments.Add("Culling", cullCode.ToString());
            namedFragments.Add("ZTest", zTestCode.ToString());
            namedFragments.Add("ZWrite", zWriteCode.ToString());
            namedFragments.Add("ZClip", zClipCode.ToString());
            namedFragments.Add("Stencil", stencilCode.ToString());
            namedFragments.Add("ColorMask", colorMaskCode.ToString());
            namedFragments.Add("DotsInstancedVars", dotsInstancingCode.ToString());

            string sharedTemplatePath = Path.Combine(Path.Combine(HDUtils.GetHDRenderPipelinePath(), "Editor"), "ShaderGraph");
            // process the template to generate the shader code for this pass
            ShaderSpliceUtil.TemplatePreprocessor templatePreprocessor =
                new ShaderSpliceUtil.TemplatePreprocessor(activeFields, namedFragments, debugOutput, sharedTemplatePath, sourceAssetDependencyPaths, HDRPShaderStructs.s_AssemblyName, HDRPShaderStructs.s_ResourceClassName);

            templatePreprocessor.ProcessTemplateFile(templateLocation);

            result.AddShaderChunk(templatePreprocessor.GetShaderCode().ToString(), false);

            return true;
        }

        public static List<MaterialSlot> FindMaterialSlotsOnNode(IEnumerable<int> slots, AbstractMaterialNode node)
        {
            var activeSlots = new List<MaterialSlot>();
            if (slots != null)
            {
                foreach (var id in slots)
                {
                    MaterialSlot slot = node.FindSlot<MaterialSlot>(id);
                    if (slot != null)
                    {
                        activeSlots.Add(slot);
                    }
                }
            }
            return activeSlots;
        }

        public static void BuildRenderStatesFromPass(
            Pass pass,
            ShaderStringBuilder blendCode,
            ShaderStringBuilder cullCode,
            ShaderStringBuilder zTestCode,
            ShaderStringBuilder zWriteCode,
            ShaderStringBuilder zClipCode,
            ShaderStringBuilder stencilCode,
            ShaderStringBuilder colorMaskCode)
        {
            if (pass.BlendOverride != null)
                blendCode.AppendLine(pass.BlendOverride);

            if (pass.BlendOpOverride != null)
                blendCode.AppendLine(pass.BlendOpOverride);

            if (pass.CullOverride != null)
                cullCode.AppendLine(pass.CullOverride);

            if (pass.ZTestOverride != null)
                zTestCode.AppendLine(pass.ZTestOverride);

            if (pass.ZWriteOverride != null)
                zWriteCode.AppendLine(pass.ZWriteOverride);

            if (pass.ColorMaskOverride != null)
                colorMaskCode.AppendLine(pass.ColorMaskOverride);

            if (pass.ZClipOverride != null)
                zClipCode.AppendLine(pass.ZClipOverride);

            if (pass.StencilOverride != null)
            {
                foreach (var str in pass.StencilOverride)
                    stencilCode.AppendLine(str);
            }
        }

        // Comment set of define for Forward Opaque pass in HDRP
        public static List<string> s_ExtraDefinesForwardOpaque = new List<string>()
        {
            "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
            "#pragma multi_compile _ DEBUG_DISPLAY",
            "#pragma multi_compile _ LIGHTMAP_ON",
            "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
            "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
            "#pragma multi_compile _ SHADOWS_SHADOWMASK",
            "#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT",
            "#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST",
            "#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH"
        };

        public static List<string> s_ExtraDefinesForwardTransparent = new List<string>()
        {
            "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
            "#pragma multi_compile _ DEBUG_DISPLAY",
            "#pragma multi_compile _ LIGHTMAP_ON",
            "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
            "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
            "#pragma multi_compile _ SHADOWS_SHADOWMASK",
            "#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT",
            "#define USE_CLUSTERED_LIGHTLIST",
            "#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH",
            HDLitSubShader.DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
        };

        public static List<string> s_ExtraDefinesForwardMaterialDepthOrMotion = new List<string>()
        {
            "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
            "#define WRITE_NORMAL_BUFFER",
            "#pragma multi_compile _ WRITE_MSAA_DEPTH",
            HDLitSubShader.DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
        };

        public static List<string> s_ExtraDefinesDepthOrMotion = new List<string>()
        {
            "#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch",
            "#pragma multi_compile _ WRITE_NORMAL_BUFFER",
            "#pragma multi_compile _ WRITE_MSAA_DEPTH",
            HDLitSubShader.DefineRaytracingKeyword(RayTracingNode.RaytracingVariant.High)
        };

        public static List<string> s_ShaderStagesRasterization = new List<string>()
        {
            "#pragma vertex Vert",
            "#pragma fragment Frag",
        };

        public static List<string> s_ShaderStagesRayTracing = new List<string>()
        {
            "#pragma raytracing surface_shader",
        };

        public static void SetStencilStateForDepth(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                "   WriteMask [_StencilWriteMaskDepth]",
                "   Ref [_StencilRefDepth]",
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void SetStencilStateForMotionVector(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                "   WriteMask [_StencilWriteMaskMV]",
                "   Ref [_StencilRefMV]",
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void SetStencilStateForDistortionVector(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                "   WriteMask [_StencilWriteMaskDistortionVec]",
                "   Ref [_StencilRefDistortionVec]",
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void SetStencilStateForForward(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                "   WriteMask [_StencilWriteMask]",
                "   Ref [_StencilRef]",
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static void SetStencilStateForGBuffer(ref Pass pass)
        {
            pass.StencilOverride = new List<string>()
            {
                "// Stencil setup",
                "Stencil",
                "{",
                "   WriteMask [_StencilWriteMaskGBuffer]",
                "   Ref [_StencilRefGBuffer]",
                "   Comp Always",
                "   Pass Replace",
                "}"
            };
        }

        public static readonly string zClipShadowCaster = "ZClip [_ZClip]";
        public static readonly string defaultCullMode = "Cull [_CullMode]";
        public static readonly string cullModeForward = "Cull [_CullModeForward]";
        public static readonly string zTestDepthEqualForOpaque = "ZTest [_ZTestDepthEqualForOpaque]";
        public static readonly string zTestTransparent = "ZTest [_ZTestTransparent]";
        public static readonly string zTestGBuffer = "ZTest [_ZTestGBuffer]";
        public static readonly string zWriteOn = "ZWrite On";
        public static readonly string zWriteOff = "ZWrite Off";
        public static readonly string ZWriteDefault = "ZWrite [_ZWrite]";

        public static void SetBlendModeForTransparentBackface(ref Pass pass) => SetBlendModeForForward(ref pass);
        public static void SetBlendModeForForward(ref Pass pass)
        {
            pass.BlendOverride = "Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]";
        }

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

        public static void AddTags(ShaderGenerator generator, string pipeline)
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine("\"RenderPipeline\"=\"{0}\"", pipeline);
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
            BaseLitGUI.ComputeStencilProperties(receiveSSR, splitLighting, out int stencilRef, out int stencilWriteMask,
                out int stencilRefDepth, out int stencilWriteMaskDepth, out int stencilRefGBuffer, out int stencilWriteMaskGBuffer,
                out int stencilRefMV, out int stencilWriteMaskMV
            );

            // All these properties values will be patched with the material keyword update
            collector.AddIntProperty("_StencilRef", stencilRef);
            collector.AddIntProperty("_StencilWriteMask", stencilWriteMask);
            // Depth prepass
            collector.AddIntProperty("_StencilRefDepth", stencilRefDepth); // Nothing
            collector.AddIntProperty("_StencilWriteMaskDepth", stencilWriteMaskDepth); // StencilUsage.TraceReflectionRay
            // Motion vector pass
            collector.AddIntProperty("_StencilRefMV", stencilRefMV); // StencilUsage.ObjectMotionVector
            collector.AddIntProperty("_StencilWriteMaskMV", stencilWriteMaskMV); // StencilUsage.ObjectMotionVector
            // Distortion vector pass
            collector.AddIntProperty("_StencilRefDistortionVec", (int)StencilUsage.DistortionVectors);
            collector.AddIntProperty("_StencilWriteMaskDistortionVec", (int)StencilUsage.DistortionVectors);
            // Gbuffer
            collector.AddIntProperty("_StencilWriteMaskGBuffer", stencilWriteMaskGBuffer);
            collector.AddIntProperty("_StencilRefGBuffer", stencilRefGBuffer);
            collector.AddIntProperty("_ZTestGBuffer", 4);

            collector.AddToggleProperty(kUseSplitLighting, splitLighting);
            collector.AddToggleProperty(kReceivesSSR, receiveSSR);

        }

        public static void AddBlendingStatesShaderProperties(
            PropertyCollector collector, SurfaceType surface, BlendMode blend, int sortingPriority,
            bool zWrite, TransparentCullMode transparentCullMode, CompareFunction zTest,
            bool backThenFrontRendering, bool fogOnTransparent)
        {
            collector.AddFloatProperty("_SurfaceType", (int)surface);
            collector.AddFloatProperty("_BlendMode", (int)blend);

            // All these properties values will be patched with the material keyword update
            collector.AddFloatProperty("_SrcBlend", 1.0f);
            collector.AddFloatProperty("_DstBlend", 0.0f);
            collector.AddFloatProperty("_AlphaSrcBlend", 1.0f);
            collector.AddFloatProperty("_AlphaDstBlend", 0.0f);
            collector.AddToggleProperty(kZWrite, (surface == SurfaceType.Transparent) ? zWrite : true);
            collector.AddToggleProperty(kTransparentZWrite, zWrite);
            collector.AddFloatProperty("_CullMode", (int)CullMode.Back);
            collector.AddIntProperty(kTransparentSortPriority, sortingPriority);
            collector.AddToggleProperty(kEnableFogOnTransparent, fogOnTransparent);
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
            collector.AddFloatProperty(kTransparentSortPriority, kTransparentSortPriority, 0);
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

        public static void AddPrePostPassProperties(PropertyCollector collector, bool prepass, bool postpass)
        {
            collector.AddToggleProperty(kTransparentDepthPrepassEnable, prepass);
            collector.AddToggleProperty(kTransparentDepthPostpassEnable, postpass);
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

                case HDRenderQueue.RenderQueueType.RaytracingOpaque:
                {
                    if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
                        return "RayTracing";
                    return "None";
                }
                case HDRenderQueue.RenderQueueType.RaytracingTransparent:
                {
                    if ((RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
                        return "RayTracing";
                    return "None";
                }
                default:
                    return "None";
            }
        }

        public static System.Collections.Generic.List<HDRenderQueue.RenderQueueType> GetRenderingPassList(bool opaque, bool needAfterPostProcess)
        {
            // We can't use RenderPipelineManager.currentPipeline here because this is called before HDRP is created by SG window
            bool supportsRayTracing = HDRenderPipeline.currentAsset && HDRenderPipeline.GatherRayTracingSupport(HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings);
            var result = new System.Collections.Generic.List<HDRenderQueue.RenderQueueType>();
            if (opaque)
            {
                result.Add(HDRenderQueue.RenderQueueType.Opaque);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostProcessOpaque);
                if (supportsRayTracing)
                    result.Add(HDRenderQueue.RenderQueueType.RaytracingOpaque);
            }
            else
            {
                result.Add(HDRenderQueue.RenderQueueType.PreRefraction);
                result.Add(HDRenderQueue.RenderQueueType.Transparent);
                result.Add(HDRenderQueue.RenderQueueType.LowTransparent);
                if (needAfterPostProcess)
                    result.Add(HDRenderQueue.RenderQueueType.AfterPostprocessTransparent);
                if (supportsRayTracing)
                    result.Add(HDRenderQueue.RenderQueueType.RaytracingTransparent);
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
