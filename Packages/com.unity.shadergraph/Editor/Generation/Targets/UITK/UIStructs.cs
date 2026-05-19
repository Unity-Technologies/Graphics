namespace UnityEditor.ShaderGraph
{
    internal static class UIStructs
    {
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = false,
            fields = new[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.texCoord4,
                StructFields.Varyings.texCoord5,
                StructFields.Varyings.texCoord6,
                StructFields.Varyings.texCoord7,
                StructFields.Varyings.color,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.vertexID,
                StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
            }
        };

        // Overrides the default Float4 uv1 with Uint4 because UIE packs integer IDs into TEXCOORD1.
        internal static readonly FieldDescriptor PackedIdsAttribute = new FieldDescriptor(
            "Attributes", "uv1", "ATTRIBUTES_NEED_TEXCOORD1", ShaderValueType.Uint4,
            "TEXCOORD1", subscriptOptions: StructFieldOptions.Optional);

        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            packFields = false,

            fields = new FieldDescriptor[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.color,
                StructFields.Attributes.uv0,        // .xy = uv, .zw = layoutUV
                PackedIdsAttribute,                 // .x:[xform|clip] .y:[opacity|textcoreOrGrad] .z:[tex|flags] .w:reserved
                StructFields.Attributes.uv2,        // .xy outer | .zw inner | .x text-extra-dilate
                StructFields.Attributes.instanceID,
                StructFields.Attributes.vertexID,
            }
        };

        public static StructDescriptor UITKVertexDescriptionInputs = new StructDescriptor()
        {
            name = "VertexDescriptionInputs",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                //static required
                new FieldDescriptor("VertexDescriptionInputs", "vertexPosition", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "vertexColor", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "uv", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "packedIds", "", ShaderValueType.Uint4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "circle", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),

                // optionals
                StructFields.VertexDescriptionInputs.VertexID,
                StructFields.VertexDescriptionInputs.InstanceID,

                StructFields.VertexDescriptionInputs.ObjectSpaceNormal,
                StructFields.VertexDescriptionInputs.NDCPosition,
                StructFields.VertexDescriptionInputs.PixelPosition,

            }
        };
        public static StructDescriptor UITKSurfaceDescriptionInputs = new StructDescriptor()
        {
            name = "SurfaceDescriptionInputs",
            packFields = false,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                //static required
                new FieldDescriptor("SurfaceDescriptionInputs", "color", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("SurfaceDescriptionInputs", "typeTexSettings", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("SurfaceDescriptionInputs", "textCoreLoc", "", ShaderValueType.Float2, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("SurfaceDescriptionInputs", "circle", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("SurfaceDescriptionInputs", "uvClip", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("SurfaceDescriptionInputs", "layoutUV", "", ShaderValueType.Float2, subscriptOptions: StructFieldOptions.Static),

                StructFields.SurfaceDescriptionInputs.uv0,
                StructFields.SurfaceDescriptionInputs.uv1,
                StructFields.SurfaceDescriptionInputs.uv2,
                StructFields.SurfaceDescriptionInputs.uv3,
                StructFields.SurfaceDescriptionInputs.uv4,
                StructFields.SurfaceDescriptionInputs.uv5,
                StructFields.SurfaceDescriptionInputs.uv6,
                StructFields.SurfaceDescriptionInputs.uv7,

                StructFields.SurfaceDescriptionInputs.WorldSpacePosition,
                StructFields.SurfaceDescriptionInputs.ScreenPosition,
                StructFields.SurfaceDescriptionInputs.NDCPosition,
                StructFields.SurfaceDescriptionInputs.PixelPosition,

                StructFields.SurfaceDescriptionInputs.TimeParameters,
            }
        };
    }
}
