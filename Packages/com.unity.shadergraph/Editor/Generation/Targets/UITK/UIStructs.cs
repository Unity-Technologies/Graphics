namespace UnityEditor.ShaderGraph
{
    internal static class UIStructs
    {
        // Per-element opacity carried to the fragment (custom shaders only). The default shader bakes
        // opacity into the color in uie_std_vert; the custom path keeps the tint straight and applies
        // opacity at the end of the fragment, so it needs its own interpolant (packed by the packer).
        internal static readonly FieldDescriptor OpacityVarying = new FieldDescriptor(
            "Varyings", "opacity", "VARYINGS_NEED_OPACITY", ShaderValueType.Float,
            subscriptOptions: StructFieldOptions.Optional);

        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = true,
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
                OpacityVarying,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.vertexID,
                StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
            }
        };

        // Overrides the default Float4 uv4 with Uint4 because UIE packs integer IDs into TEXCOORD4.
        internal static readonly FieldDescriptor PackedIdsAttribute = new FieldDescriptor(
            "Attributes", "uv4", "ATTRIBUTES_NEED_TEXCOORD4", ShaderValueType.Uint4,
            "TEXCOORD4", subscriptOptions: StructFieldOptions.Optional);

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
                StructFields.Attributes.uv5,        // .xy outer | .zw inner | .x text-extra-dilate
                StructFields.Attributes.uv1,        // optional stream-1 ExtraVertexChannels.TexCoord1
                StructFields.Attributes.uv2,        // optional stream-1 ExtraVertexChannels.TexCoord2
                StructFields.Attributes.uv3,        // optional stream-1 ExtraVertexChannels.TexCoord3
                StructFields.Attributes.normalOS,   // optional stream-1 ExtraVertexChannels.Normal
                StructFields.Attributes.tangentOS,  // optional stream-1 ExtraVertexChannels.Tangent
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
                // Texture UV (dynamic-texture-patched), mirrors SurfaceDescriptionInputs.uvClip so the
                // Element Texture UV node reads IN.uvClip.xy in the vertex stage as it does in fragment.
                new FieldDescriptor("VertexDescriptionInputs", "uvClip", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                // Resolved tint (vertexColor * dynamicColor), mirrors SurfaceDescriptionInputs.color so the
                // Element Color node reads IN.color in the vertex stage; the fragment uses the carried tint.
                new FieldDescriptor("VertexDescriptionInputs", "color", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),
                // Per-element opacity, mirrors SurfaceDescriptionInputs.opacity so the Element Color node
                // reads IN.opacity in the vertex stage.
                new FieldDescriptor("VertexDescriptionInputs", "opacity", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Static),
                // Layout-rect UV, mirrors SurfaceDescriptionInputs.layoutUV so the Element Layout UV node
                // reads IN.layoutUV.xy in the vertex stage as it does in fragment.
                new FieldDescriptor("VertexDescriptionInputs", "layoutUV", "", ShaderValueType.Float2, subscriptOptions: StructFieldOptions.Static),
                // Type/texture settings, mirrors SurfaceDescriptionInputs.typeTexSettings so the Render
                // Type node reads IN.typeTexSettings.x (the render type) and the Element Texture Size node
                // reads IN.typeTexSettings.y (the texture slot) in the vertex stage.
                new FieldDescriptor("VertexDescriptionInputs", "typeTexSettings", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Static),

                // optionals
                StructFields.VertexDescriptionInputs.VertexID,
                StructFields.VertexDescriptionInputs.InstanceID,

                // Element-local position read (PositionNode Object space) and the per-element world
                // position (PositionNode World space, built in BuildUIVertexDescriptionInputs).
                // ObjectSpacePosition is also the default of the always-active Position block.
                StructFields.VertexDescriptionInputs.ObjectSpacePosition,
                StructFields.VertexDescriptionInputs.WorldSpacePosition,

                StructFields.VertexDescriptionInputs.ObjectSpaceNormal,
                StructFields.VertexDescriptionInputs.ObjectSpaceTangent,
                StructFields.VertexDescriptionInputs.uv0, // Element Texture UV node reads IN.uv0 in preview
                StructFields.VertexDescriptionInputs.uv1,
                StructFields.VertexDescriptionInputs.uv2,
                StructFields.VertexDescriptionInputs.uv3,
                StructFields.VertexDescriptionInputs.NDCPosition,
                StructFields.VertexDescriptionInputs.PixelPosition,
                StructFields.VertexDescriptionInputs.TimeParameters, // e.g. a Time-driven vertex Position offset

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
                // Per-element opacity carried from the vertex; read by the Element Color node and applied
                // at the end of uie_custom_frag (unless disabled).
                new FieldDescriptor("SurfaceDescriptionInputs", "opacity", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Static),

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
