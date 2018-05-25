using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class HDRPShaderStructs
    {
        struct AttributesMesh
        {
            [Semantic("POSITION")]              Vector3 positionOS;
            [Semantic("NORMAL")][Optional]     Vector3 normalOS;
            [Semantic("TANGENT")][Optional]    Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]  Vector2 uv0;
            [Semantic("TEXCOORD1")][Optional]  Vector2 uv1;
            [Semantic("TEXCOORD2")][Optional]  Vector2 uv2;
            [Semantic("TEXCOORD3")][Optional]  Vector2 uv3;
            [Semantic("COLOR")][Optional]      Vector4 color;
        };

        struct VaryingsMeshToPS
        {
            [Semantic("SV_Position")]           Vector4 positionCS;
            [Optional]                          Vector3 positionWS;
            [Optional]                          Vector3 normalWS;
            [Optional]                          Vector4 tangentWS;      // w contain mirror sign
            [Optional]                          Vector2 texCoord0;
            [Optional]                          Vector2 texCoord1;
            [Optional]                          Vector2 texCoord2;
            [Optional]                          Vector2 texCoord3;
            [Optional]                          Vector4 color;
            [Optional][Semantic("FRONT_FACE_SEMANTIC")][OverrideType("FRONT_FACE_TYPE")][PreprocessorIf("SHADER_STAGE_FRAGMENT")]
            bool cullFace;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionWS",       "VaryingsMeshToDS.positionWS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "VaryingsMeshToDS.normalWS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "VaryingsMeshToDS.tangentWS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "VaryingsMeshToDS.texCoord0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "VaryingsMeshToDS.texCoord1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "VaryingsMeshToDS.texCoord2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "VaryingsMeshToDS.texCoord3"),
                new Dependency("VaryingsMeshToPS.color",            "VaryingsMeshToDS.color"),
            };

            public static Dependency[] standardDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionWS",       "AttributesMesh.positionOS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "AttributesMesh.normalOS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "AttributesMesh.tangentOS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "AttributesMesh.uv0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "AttributesMesh.uv1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "AttributesMesh.uv2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "AttributesMesh.uv3"),
                new Dependency("VaryingsMeshToPS.color",            "AttributesMesh.color"),
            };
        };

        struct VaryingsMeshToDS
        {
            Vector3 positionWS;
            Vector3 normalWS;
            [Optional]      Vector4 tangentWS;
            [Optional]      Vector2 texCoord0;
            [Optional]      Vector2 texCoord1;
            [Optional]      Vector2 texCoord2;
            [Optional]      Vector2 texCoord3;
            [Optional]      Vector4 color;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToDS.tangentWS",        "VaryingsMeshToPS.tangentWS"),
                new Dependency("VaryingsMeshToDS.texCoord0",        "VaryingsMeshToPS.texCoord0"),
                new Dependency("VaryingsMeshToDS.texCoord1",        "VaryingsMeshToPS.texCoord1"),
                new Dependency("VaryingsMeshToDS.texCoord2",        "VaryingsMeshToPS.texCoord2"),
                new Dependency("VaryingsMeshToDS.texCoord3",        "VaryingsMeshToPS.texCoord3"),
                new Dependency("VaryingsMeshToDS.color",            "VaryingsMeshToPS.color"),
            };
        };

        struct FragInputs
        {
            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("FragInputs.positionWS",         "VaryingsMeshToPS.positionWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.normalWS"),
                new Dependency("FragInputs.texCoord0",          "VaryingsMeshToPS.texCoord0"),
                new Dependency("FragInputs.texCoord1",          "VaryingsMeshToPS.texCoord1"),
                new Dependency("FragInputs.texCoord2",          "VaryingsMeshToPS.texCoord2"),
                new Dependency("FragInputs.texCoord3",          "VaryingsMeshToPS.texCoord3"),
                new Dependency("FragInputs.color",              "VaryingsMeshToPS.color"),
                new Dependency("FragInputs.isFrontFace",        "VaryingsMeshToPS.cullFace"),
            };
        };

        struct SurfaceDescriptionInputs
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

            [Optional] Vector4 screenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 vertexColor;

            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("SurfaceDescriptionInputs.WorldSpaceNormal",          "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceNormal",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceNormal",           "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceTangent",        "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceTangent",          "SurfaceDescriptionInputs.WorldSpaceTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceBiTangent",      "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceBiTangent",        "SurfaceDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpacePosition",        "FragInputs.positionWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpacePosition",       "FragInputs.positionWS"),
                new Dependency("SurfaceDescriptionInputs.ViewSpacePosition",         "FragInputs.positionWS"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceViewDirection",   "FragInputs.positionWS"),                   // we build WorldSpaceViewDirection using FragInputs.positionWS in GetWorldSpaceNormalizeViewDir()
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceViewDirection",  "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceViewDirection",    "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.screenPosition",            "FragInputs.positionSS"),
                new Dependency("SurfaceDescriptionInputs.uv0",                       "FragInputs.texCoord0"),
                new Dependency("SurfaceDescriptionInputs.uv1",                       "FragInputs.texCoord1"),
                new Dependency("SurfaceDescriptionInputs.uv2",                       "FragInputs.texCoord2"),
                new Dependency("SurfaceDescriptionInputs.uv3",                       "FragInputs.texCoord3"),
                new Dependency("SurfaceDescriptionInputs.vertexColor",               "FragInputs.color"),
            };
        };

        static void AddActiveFieldsFromGraphRequirements(HashSet<string> activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("SurfaceDescriptionInputs.screenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("SurfaceDescriptionInputs.vertexColor");
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
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("SurfaceDescriptionInputs." + channel.GetUVName());
            }
        }

        // TODO : split this function into buildActiveFields and buildHLSLTypeDeclaration functions
        public static void Generate(
            ShaderGenerator codeResult,
            ShaderGenerator graphInputsResult,
            ShaderGraphRequirements graphRequirements,
            List<string> passRequiredFields,            // fields the pass requires
            CoordinateSpace preferedCoordinateSpace,
            HashSet<string> activeFields)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // build initial requirements
            AddActiveFieldsFromGraphRequirements(activeFields, graphRequirements);
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.Add(requiredField);
                }
            }

            // propagate requirements using dependencies
            {
                ShaderSpliceUtil.ApplyDependencies(
                    activeFields,
                    new List<Dependency[]>()
                {
                    FragInputs.dependencies,
                    VaryingsMeshToPS.standardDependencies,
                    SurfaceDescriptionInputs.dependencies,
                });
            }

            // generate code based on requirements
            ShaderSpliceUtil.BuildType(typeof(AttributesMesh), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(SurfaceDescriptionInputs), activeFields, graphInputsResult);
        }
    };

    public struct Pass
    {
        public string Name;
        public string LightMode;
        public string ShaderPassName;
        public List<string> Includes;
        public string TemplateName;
        public List<string> ExtraDefines;
        public List<int> VertexShaderSlots;         // These control what slots are used by the pass vertex shader
        public List<int> PixelShaderSlots;          // These control what slots are used by the pass pixel shader
        public string CullOverride;
        public string BlendOverride;
        public string BlendOpOverride;
        public string ZTestOverride;
        public string ZWriteOverride;
        public string ColorMaskOverride;
        public List<string> StencilOverride;
        public List<string> RequiredFields;         // feeds into the dependency analysis
        public ShaderGraphRequirements requirements;
    };

    public static class HDSubShaderUtilities
    {
        public static void BuildRenderStatesFromPassAndMaterialOptions(
            Pass pass,
            SurfaceMaterialOptions materialOptions,
            ShaderStringBuilder blendCode,
            ShaderStringBuilder cullCode,
            ShaderStringBuilder zTestCode,
            ShaderStringBuilder zWriteCode,
            ShaderStringBuilder stencilCode,
            ShaderStringBuilder colorMaskCode)
        {
            if (pass.BlendOverride != null)
            {
                blendCode.AppendLine(pass.BlendOverride);
            }
            else
            {
                materialOptions.GetBlend(blendCode);
            }

            if (pass.BlendOpOverride != null)
            {
                blendCode.AppendLine(pass.BlendOpOverride);
            }

            if (pass.CullOverride != null)
            {
                cullCode.AppendLine(pass.CullOverride);
            }
            else
            {
                materialOptions.GetCull(cullCode);
            }

            if (pass.ZTestOverride != null)
            {
                zTestCode.AppendLine(pass.ZTestOverride);
            }
            else
            {
                materialOptions.GetDepthTest(zTestCode);
            }

            if (pass.ZWriteOverride != null)
            {
                zWriteCode.AppendLine(pass.ZWriteOverride);
            }
            else
            {
                materialOptions.GetDepthWrite(zWriteCode);
            }

            if (pass.ColorMaskOverride != null)
            {
                colorMaskCode.AppendLine(pass.ColorMaskOverride);
            }
            else
            {
                // material option default is to not declare anything for color mask
            }

            if (pass.StencilOverride != null)
            {
                foreach (var str in pass.StencilOverride)
                {
                    stencilCode.AppendLine(str);
                }
            }
            else
            {
                stencilCode.AppendLine("// Default Stencil");
            }
        }

        public static SurfaceMaterialOptions BuildMaterialOptions(SurfaceType surfaceType, AlphaMode alphaMode, bool twoSided)
        {
            SurfaceMaterialOptions materialOptions = new SurfaceMaterialOptions();
            if (surfaceType == SurfaceType.Opaque)
            {
                materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
            }
            else
            {
                switch (alphaMode)
                {
                    case AlphaMode.Alpha:
                        materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                        materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                        materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                        materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                        materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                        materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                        break;
                    case AlphaMode.Additive:
                        materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                        materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                        materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                        materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                        materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                        materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                        break;
                        // TODO: other blend modes
                }
            }

            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;

            return materialOptions;
        }
    }
}
