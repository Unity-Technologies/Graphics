namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class StructFields
    {
        public struct Attributes
        {
            public static string name = "Attributes";
            public static FieldDescriptor positionOS = new FieldDescriptor(Attributes.name, "positionOS", "", ShaderValueType.Float3, "POSITION");
            public static FieldDescriptor normalOS = new FieldDescriptor(Attributes.name, "normalOS", "ATTRIBUTES_NEED_NORMAL", ShaderValueType.Float3,
                "NORMAL", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor tangentOS = new FieldDescriptor(Attributes.name, "tangentOS", "ATTRIBUTES_NEED_TANGENT", ShaderValueType.Float4,
                "TANGENT", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv0 = new FieldDescriptor(Attributes.name, "uv0", "ATTRIBUTES_NEED_TEXCOORD0", ShaderValueType.Float4,
                "TEXCOORD0", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv1 = new FieldDescriptor(Attributes.name, "uv1", "ATTRIBUTES_NEED_TEXCOORD1", ShaderValueType.Float4,
                "TEXCOORD1", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv2 = new FieldDescriptor(Attributes.name, "uv2", "ATTRIBUTES_NEED_TEXCOORD2", ShaderValueType.Float4,
                "TEXCOORD2", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv3 = new FieldDescriptor(Attributes.name, "uv3", "ATTRIBUTES_NEED_TEXCOORD3", ShaderValueType.Float4,
                "TEXCOORD3", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor weights = new FieldDescriptor(Attributes.name, "weights", "ATTRIBUTES_NEED_BLENDWEIGHTS", ShaderValueType.Float4,
                "BLENDWEIGHTS", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor indices = new FieldDescriptor(Attributes.name, "indices", "ATTRIBUTES_NEED_BLENDINDICES", ShaderValueType.Uint4,
                "BLENDINDICES", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(Attributes.name, "color", "ATTRIBUTES_NEED_COLOR", ShaderValueType.Float4,
                "COLOR", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor instanceID = new FieldDescriptor(Attributes.name, "instanceID", "", ShaderValueType.Uint,
                "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
            public static FieldDescriptor vertexID = new FieldDescriptor(Attributes.name, "vertexID", "ATTRIBUTES_NEED_VERTEXID", ShaderValueType.Uint,
                "SV_VertexID", subscriptOptions: StructFieldOptions.Optional);
        }

        public struct Varyings
        {
            public static string name = "Varyings";
            public static FieldDescriptor positionCS = new FieldDescriptor(Varyings.name, "positionCS", "", ShaderValueType.Float4, "SV_POSITION");
            public static FieldDescriptor positionWS = new FieldDescriptor(Varyings.name, "positionWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor normalWS = new FieldDescriptor(Varyings.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor tangentWS = new FieldDescriptor(Varyings.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord0 = new FieldDescriptor(Varyings.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord1 = new FieldDescriptor(Varyings.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord2 = new FieldDescriptor(Varyings.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord3 = new FieldDescriptor(Varyings.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(Varyings.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor viewDirectionWS = new FieldDescriptor(Varyings.name, "viewDirectionWS", "VARYINGS_NEED_VIEWDIRECTION_WS", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor screenPosition = new FieldDescriptor(Varyings.name, "screenPosition", "VARYINGS_NEED_SCREENPOSITION", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor instanceID = new FieldDescriptor(Varyings.name, "instanceID", "", ShaderValueType.Uint,
                "CUSTOM_INSTANCE_ID", "UNITY_ANY_INSTANCING_ENABLED");
            public static FieldDescriptor cullFace = new FieldDescriptor(Varyings.name, "cullFace", "VARYINGS_NEED_CULLFACE", "FRONT_FACE_TYPE",
                "FRONT_FACE_SEMANTIC", "defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)", StructFieldOptions.Generated & StructFieldOptions.Optional);
        }

        public struct VertexDescriptionInputs
        {
            public static string name = "VertexDescriptionInputs";
            public static FieldDescriptor ObjectSpaceNormal = new FieldDescriptor(VertexDescriptionInputs.name, "ObjectSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceNormal = new FieldDescriptor(VertexDescriptionInputs.name, "ViewSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceNormal = new FieldDescriptor(VertexDescriptionInputs.name, "WorldSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceNormal = new FieldDescriptor(VertexDescriptionInputs.name, "TangentSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceTangent = new FieldDescriptor(VertexDescriptionInputs.name, "ObjectSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceTangent = new FieldDescriptor(VertexDescriptionInputs.name, "ViewSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceTangent = new FieldDescriptor(VertexDescriptionInputs.name, "WorldSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceTangent = new FieldDescriptor(VertexDescriptionInputs.name, "TangentSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceBiTangent = new FieldDescriptor(VertexDescriptionInputs.name, "ObjectSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceBiTangent = new FieldDescriptor(VertexDescriptionInputs.name, "ViewSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceBiTangent = new FieldDescriptor(VertexDescriptionInputs.name, "WorldSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceBiTangent = new FieldDescriptor(VertexDescriptionInputs.name, "TangentSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceViewDirection = new FieldDescriptor(VertexDescriptionInputs.name, "ObjectSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceViewDirection = new FieldDescriptor(VertexDescriptionInputs.name, "ViewSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceViewDirection = new FieldDescriptor(VertexDescriptionInputs.name, "WorldSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceViewDirection = new FieldDescriptor(VertexDescriptionInputs.name, "TangentSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpacePosition = new FieldDescriptor(VertexDescriptionInputs.name, "ObjectSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpacePosition = new FieldDescriptor(VertexDescriptionInputs.name, "ViewSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpacePosition = new FieldDescriptor(VertexDescriptionInputs.name, "WorldSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpacePosition = new FieldDescriptor(VertexDescriptionInputs.name, "TangentSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor AbsoluteWorldSpacePosition = new FieldDescriptor(VertexDescriptionInputs.name, "AbsoluteWorldSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ScreenPosition = new FieldDescriptor(VertexDescriptionInputs.name, "ScreenPosition", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv0 = new FieldDescriptor(VertexDescriptionInputs.name, "uv0", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv1 = new FieldDescriptor(VertexDescriptionInputs.name, "uv1", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv2 = new FieldDescriptor(VertexDescriptionInputs.name, "uv2", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv3 = new FieldDescriptor(VertexDescriptionInputs.name, "uv3", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor VertexColor = new FieldDescriptor(VertexDescriptionInputs.name, "VertexColor", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TimeParameters = new FieldDescriptor(VertexDescriptionInputs.name, "TimeParameters", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor BoneWeights = new FieldDescriptor(VertexDescriptionInputs.name, "BoneWeights", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor BoneIndices = new FieldDescriptor(VertexDescriptionInputs.name, "BoneIndices", "", ShaderValueType.Uint4,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor VertexID = new FieldDescriptor(VertexDescriptionInputs.name, "VertexID", "", ShaderValueType.Uint,
                subscriptOptions: StructFieldOptions.Optional);
        }

        public struct SurfaceDescriptionInputs
        {
            public static string name = "SurfaceDescriptionInputs";
            public static FieldDescriptor ObjectSpaceNormal = new FieldDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceNormal = new FieldDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceNormal = new FieldDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceNormal = new FieldDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceNormal", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceBiTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceBiTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceBiTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceBiTangent = new FieldDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceBiTangent", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpaceViewDirection = new FieldDescriptor(SurfaceDescriptionInputs.name, "ObjectSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpaceViewDirection = new FieldDescriptor(SurfaceDescriptionInputs.name, "ViewSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpaceViewDirection = new FieldDescriptor(SurfaceDescriptionInputs.name, "WorldSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpaceViewDirection = new FieldDescriptor(SurfaceDescriptionInputs.name, "TangentSpaceViewDirection", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ObjectSpacePosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "ObjectSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor ViewSpacePosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "ViewSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor WorldSpacePosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "WorldSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TangentSpacePosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "TangentSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor AbsoluteWorldSpacePosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "AbsoluteWorldSpacePosition", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor ScreenPosition = new FieldDescriptor(SurfaceDescriptionInputs.name, "ScreenPosition", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv0 = new FieldDescriptor(SurfaceDescriptionInputs.name, "uv0", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv1 = new FieldDescriptor(SurfaceDescriptionInputs.name, "uv1", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv2 = new FieldDescriptor(SurfaceDescriptionInputs.name, "uv2", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv3 = new FieldDescriptor(SurfaceDescriptionInputs.name, "uv3", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor VertexColor = new FieldDescriptor(SurfaceDescriptionInputs.name, "VertexColor", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor TimeParameters = new FieldDescriptor(SurfaceDescriptionInputs.name, "TimeParameters", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor FaceSign = new FieldDescriptor(SurfaceDescriptionInputs.name, "FaceSign", "", ShaderValueType.Float,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor BoneWeights = new FieldDescriptor(SurfaceDescriptionInputs.name, "BoneWeights", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor BoneIndices = new FieldDescriptor(SurfaceDescriptionInputs.name, "BoneIndices", "", ShaderValueType.Uint4,
                subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor VertexID = new FieldDescriptor(SurfaceDescriptionInputs.name, "VertexID", "", ShaderValueType.Uint,
                subscriptOptions: StructFieldOptions.Optional);
        }
    }
}
