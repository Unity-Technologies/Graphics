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

        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            packFields = false,

            fields = new FieldDescriptor[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.color,
                StructFields.Attributes.uv0,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Attributes.uv3,
                StructFields.Attributes.uv4,
                StructFields.Attributes.uv5,
                StructFields.Attributes.uv6,
                StructFields.Attributes.uv7,
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
                new FieldDescriptor("VertexDescriptionInputs", "xformClipPages", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "ids", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "flags", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "opacityColorPages", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "settingIndex", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
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
