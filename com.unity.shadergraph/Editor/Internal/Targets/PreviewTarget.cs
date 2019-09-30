using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    class PreviewTarget : ITarget
    {
        public string displayName => "PREVIEW";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return false;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            subShader = null;
            return false;
        }

#region Passes
        public static class Passes
        {
            public static ShaderPass Preview = new ShaderPass()
            {
                // Definition
                referenceName = "SHADERPASS_PREVIEW",
                passInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewPass.hlsl",
                varyingsInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewVaryings.hlsl",
                useInPreview = true,

                // Pass setup
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                },
                defines = new List<string>()
                {
                    "SHADERGRAPH_PREVIEW 1",
                },
                structs = new StructDescriptor[]
                {
                    PreviewTarget.Attributes,
                    PreviewTarget.Varyings,
                    PreviewTarget.SurfaceDescriptionInputs,
                    PreviewTarget.VertexDescriptionInputs,
                }
            };
        }
#endregion

#region ShaderStructs
        public static class ShaderStructs
        {
            public struct Attributes
            {
                public static string name = "Attributes";
                public static SubscriptDescriptor positionOS = new SubscriptDescriptor(Attributes.name, "positionOS", "", ShaderValueType.Float3, "POSITION");
                public static SubscriptDescriptor normalOS = new SubscriptDescriptor(Attributes.name, "normalOS", "ATTRIBUTES_NEED_NORMAL", ShaderValueType.Float3,
                    "NORMAL", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor tangentOS = new SubscriptDescriptor(Attributes.name, "tangentOS", "ATTRIBUTES_NEED_TANGENT", ShaderValueType.Float4,
                    "TANGENT", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv0 = new SubscriptDescriptor(Attributes.name, "uv0", "ATTRIBUTES_NEED_TEXCOORD0", ShaderValueType.Float4,
                    "TEXCOORD0", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv1 = new SubscriptDescriptor(Attributes.name, "uv1", "ATTRIBUTES_NEED_TEXCOORD1", ShaderValueType.Float4,
                    "TEXCOORD1", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv2 = new SubscriptDescriptor(Attributes.name, "uv2", "ATTRIBUTES_NEED_TEXCOORD2", ShaderValueType.Float4,
                    "TEXCOORD2", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv3 = new SubscriptDescriptor(Attributes.name, "uv3", "ATTRIBUTES_NEED_TEXCOORD3", ShaderValueType.Float4,
                    "TEXCOORD3", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(Attributes.name, "color", "ATTRIBUTES_NEED_COLOR", ShaderValueType.Float4,
                    "COLOR", subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor instanceID = new SubscriptDescriptor(Attributes.name, "instanceID", "", ShaderValueType.UnsignedInteger,
                    "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
            }

            public struct Varyings
            {
                public static string name = "Varyings";
                public static SubscriptDescriptor positionCS = new SubscriptDescriptor(Varyings.name, "positionCS", "", ShaderValueType.Float4, "Sv_Position");
                public static SubscriptDescriptor positionWS = new SubscriptDescriptor(Varyings.name, "positionWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor normalWS = new SubscriptDescriptor(Varyings.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor tangentWS = new SubscriptDescriptor(Varyings.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord0 = new SubscriptDescriptor(Varyings.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord1 = new SubscriptDescriptor(Varyings.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord2 = new SubscriptDescriptor(Varyings.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor texCoord3 = new SubscriptDescriptor(Varyings.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor color = new SubscriptDescriptor(Varyings.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor viewDirectionWS = new SubscriptDescriptor(Varyings.name, "viewDirectionWS", "VARYINGS_NEED_VIEWDIRECTION_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor bitangentWS = new SubscriptDescriptor(Varyings.name, "bitengentWS", "VARYINGS_NEED_BITANGENT_WS", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor screenPosition = new SubscriptDescriptor(Varyings.name, "screenPosition", "VARYINGS_NEED_SCREENPOSITION", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor instanceID = new SubscriptDescriptor(Varyings.name, "instanceID", "", ShaderValueType.UnsignedInteger,
                    "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
                public static SubscriptDescriptor cullFace = new SubscriptDescriptor(Varyings.name, "cullFace", "VARYINGS_NEED_CULLFACE", "FRONT_FACE_TYPE",
                    "FRONT_FACE_SEMANTIC", "defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)", SubscriptOptions.Generated & SubscriptOptions.Optional);
            }
            public struct VertexDescriptionInputs
            {
                public static string name = "VertexDescriptionInputs";
                public static SubscriptDescriptor ObjectSpaceNormal = new SubscriptDescriptor(VertexDescriptionInputs.name, "ObjectSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceNormal = new SubscriptDescriptor(VertexDescriptionInputs.name, "ViewSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceNormal = new SubscriptDescriptor(VertexDescriptionInputs.name, "WorldSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceNormal = new SubscriptDescriptor(VertexDescriptionInputs.name, "TangentSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "ObjectSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "ViewSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "WorldSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "TangentSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceBiTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "ObjectSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceBiTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "ViewSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceBiTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "WorldSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceBiTangent = new SubscriptDescriptor(VertexDescriptionInputs.name, "TangentSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceViewDirection = new SubscriptDescriptor(VertexDescriptionInputs.name, "ObjectSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceViewDirection = new SubscriptDescriptor(VertexDescriptionInputs.name, "ViewSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceViewDirection = new SubscriptDescriptor(VertexDescriptionInputs.name, "WorldSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceViewDirection = new SubscriptDescriptor(VertexDescriptionInputs.name, "TangentSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpacePosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "ObjectSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpacePosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "ViewSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpacePosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "WorldSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpacePosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "TangentSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor AbsoluteWorldpacePosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "AbsoluteWorldSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ScreenPosition = new SubscriptDescriptor(VertexDescriptionInputs.name, "ScreenPosition", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv0 = new SubscriptDescriptor(VertexDescriptionInputs.name, "uv0", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv1 = new SubscriptDescriptor(VertexDescriptionInputs.name, "uv1", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv2 = new SubscriptDescriptor(VertexDescriptionInputs.name, "uv2", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv3 = new SubscriptDescriptor(VertexDescriptionInputs.name, "uv3", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor VertexColor = new SubscriptDescriptor(VertexDescriptionInputs.name, "VertexColor", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TimeParameters = new SubscriptDescriptor(VertexDescriptionInputs.name, "TimeParameters", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
            }

            public struct SurfaceDescriptionInputs
            {
                public static string name = "SurfaceDescriptionInputs";
                public static SubscriptDescriptor ObjectSpaceNormal = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceNormal = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceNormal = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceNormal = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceNormal", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceBiTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceBiTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceBiTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceBiTangent = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceBiTangent", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpaceViewDirection = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpaceViewDirection = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpaceViewDirection = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpaceViewDirection = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceViewDirection", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ObjectSpacePosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ObjectSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor ViewSpacePosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ViewSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor WorldpacePosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "WorldSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TangentSpacePosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TangentSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor AbsoluteWorldpacePosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "AbsoluteWorldSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                
                public static SubscriptDescriptor ScreenPosition = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "ScreenPosition", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv0 = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "uv0", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv1 = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "uv1", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv2 = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "uv2", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor uv3 = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "uv3", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor VertexColor = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "VertexColor", "", ShaderValueType.Float4,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor TimeParameters = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "TimeParameters", "", ShaderValueType.Float3,
                    subscriptOptions : SubscriptOptions.Optional);
                public static SubscriptDescriptor FaceSign = new SubscriptDescriptor(SurfaceDescriptionInputs.name, "FaceSign", "", ShaderValueType.Float,
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
                new FieldDependency(MeshTarget.ShaderStructs.Varyings.instanceID,   MeshTarget.ShaderStructs.Attributes.instanceID)
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
