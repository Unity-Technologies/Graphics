using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDStructFields
    {
        public struct AttributesMesh
        {
            public static string name = "AttributesMesh";
            public static FieldDescriptor positionOS = new FieldDescriptor(AttributesMesh.name, "positionOS", "", ShaderValueType.Float3, "POSITION");
            public static FieldDescriptor normalOS = new FieldDescriptor(AttributesMesh.name, "normalOS", "ATTRIBUTES_NEED_NORMAL", ShaderValueType.Float3,
                "NORMAL", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor tangentOS = new FieldDescriptor(AttributesMesh.name, "tangentOS", "ATTRIBUTES_NEED_TANGENT", ShaderValueType.Float4,
                "TANGENT", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv0 = new FieldDescriptor(AttributesMesh.name, "uv0", "ATTRIBUTES_NEED_TEXCOORD0", ShaderValueType.Float4,
                "TEXCOORD0", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv1 = new FieldDescriptor(AttributesMesh.name, "uv1", "ATTRIBUTES_NEED_TEXCOORD1", ShaderValueType.Float4,
                "TEXCOORD1", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv2 = new FieldDescriptor(AttributesMesh.name, "uv2", "ATTRIBUTES_NEED_TEXCOORD2", ShaderValueType.Float4,
                "TEXCOORD2", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor uv3 = new FieldDescriptor(AttributesMesh.name, "uv3", "ATTRIBUTES_NEED_TEXCOORD3", ShaderValueType.Float4,
                "TEXCOORD3", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor weights = new FieldDescriptor(AttributesMesh.name, "weights", "ATTRIBUTES_NEED_BLENDWEIGHTS", ShaderValueType.Float4,
                "BLENDWEIGHTS", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor indices = new FieldDescriptor(AttributesMesh.name, "indices", "ATTRIBUTES_NEED_BLENDINDICES", ShaderValueType.Uint4,
                "BLENDINDICES", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(AttributesMesh.name, "color", "ATTRIBUTES_NEED_COLOR", ShaderValueType.Float4,
                "COLOR", subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor instanceID = new FieldDescriptor(AttributesMesh.name, "instanceID", "", ShaderValueType.Uint,
                "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
            public static FieldDescriptor vertexID = new FieldDescriptor(AttributesMesh.name, "vertexID", "ATTRIBUTES_NEED_VERTEXID", ShaderValueType.Uint,
                "SV_VertexID", subscriptOptions: StructFieldOptions.Optional);
        }

        public struct VaryingsMeshToPS
        {
            public static string name = "VaryingsMeshToPS";
            public static FieldDescriptor positionCS = new FieldDescriptor(VaryingsMeshToPS.name, "positionCS", "", ShaderValueType.Float4, "SV_POSITION");
            public static FieldDescriptor positionRWS = new FieldDescriptor(VaryingsMeshToPS.name, "positionRWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor normalWS = new FieldDescriptor(VaryingsMeshToPS.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor tangentWS = new FieldDescriptor(VaryingsMeshToPS.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord0 = new FieldDescriptor(VaryingsMeshToPS.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord1 = new FieldDescriptor(VaryingsMeshToPS.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord2 = new FieldDescriptor(VaryingsMeshToPS.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord3 = new FieldDescriptor(VaryingsMeshToPS.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(VaryingsMeshToPS.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor instanceID = new FieldDescriptor(VaryingsMeshToPS.name, "instanceID", "", ShaderValueType.Uint,
                "CUSTOM_INSTANCE_ID", "UNITY_ANY_INSTANCING_ENABLED");
            // Note: we don't generate cullFace here as it is always present in VertMesh.hlsl
        }

        public struct VaryingsMeshToDS
        {
            public static string name = "VaryingsMeshToDS";
            public static FieldDescriptor positionRWS = new FieldDescriptor(VaryingsMeshToDS.name, "positionWS", "VARYINGS_NEED_POSITION_WS", ShaderValueType.Float3);
            public static FieldDescriptor normalWS = new FieldDescriptor(VaryingsMeshToDS.name, "normalWS", "VARYINGS_NEED_NORMAL_WS", ShaderValueType.Float3);
            public static FieldDescriptor tangentWS = new FieldDescriptor(VaryingsMeshToDS.name, "tangentWS", "VARYINGS_NEED_TANGENT_WS", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord0 = new FieldDescriptor(VaryingsMeshToDS.name, "texCoord0", "VARYINGS_NEED_TEXCOORD0", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord1 = new FieldDescriptor(VaryingsMeshToDS.name, "texCoord1", "VARYINGS_NEED_TEXCOORD1", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord2 = new FieldDescriptor(VaryingsMeshToDS.name, "texCoord2", "VARYINGS_NEED_TEXCOORD2", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord3 = new FieldDescriptor(VaryingsMeshToDS.name, "texCoord3", "VARYINGS_NEED_TEXCOORD3", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(VaryingsMeshToDS.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor instanceID = new FieldDescriptor(VaryingsMeshToDS.name, "instanceID", "", ShaderValueType.Uint,
                "INSTANCEID_SEMANTIC", "UNITY_ANY_INSTANCING_ENABLED");
        }

        public struct FragInputs
        {
            public static string name = "FragInputs";
            public static FieldDescriptor positionRWS = new FieldDescriptor(FragInputs.name, "positionRWS", "", ShaderValueType.Float3,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor tangentToWorld = new FieldDescriptor(FragInputs.name, "tangentToWorld", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord0 = new FieldDescriptor(FragInputs.name, "texCoord0", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord1 = new FieldDescriptor(FragInputs.name, "texCoord1", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord2 = new FieldDescriptor(FragInputs.name, "texCoord2", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor texCoord3 = new FieldDescriptor(FragInputs.name, "texCoord3", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor color = new FieldDescriptor(FragInputs.name, "color", "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor primitiveID = new FieldDescriptor(FragInputs.name, "primitiveID", "", ShaderValueType.Uint,
                subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor IsFrontFace = new FieldDescriptor(FragInputs.name, "isFrontFace", "", ShaderValueType.Boolean,
                subscriptOptions: StructFieldOptions.Optional);
        }
    }
}
