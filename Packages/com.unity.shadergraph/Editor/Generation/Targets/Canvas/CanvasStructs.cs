namespace UnityEditor.ShaderGraph
{
    internal static class CanvasStructs
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
                StructFields.Varyings.positionPredisplacementWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
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
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.color,
                StructFields.Attributes.uv0,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Attributes.uv3,
                StructFields.Attributes.instanceID,
                StructFields.Attributes.vertexID,
            }
        };

        //todo: Delete
        public static StructDescriptor CanvasVertexDescriptionInputs = new StructDescriptor()
        {
            name = "VertexDescriptionInputs",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                //static required
                new FieldDescriptor("VertexDescriptionInputs", "ObjectSpacePosition", "", ShaderValueType.Float3,
                    subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "VertexColor", "", ShaderValueType.Float4,
                    subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "uv0", "", ShaderValueType.Float4,
                    subscriptOptions: StructFieldOptions.Static),
                new FieldDescriptor("VertexDescriptionInputs", "uv1", "", ShaderValueType.Float4,
                    subscriptOptions: StructFieldOptions.Static),
                //optionals
                StructFields.VertexDescriptionInputs.ObjectSpaceNormal,
                StructFields.VertexDescriptionInputs.NDCPosition,
                StructFields.VertexDescriptionInputs.PixelPosition,
                StructFields.VertexDescriptionInputs.uv2,
                StructFields.VertexDescriptionInputs.uv3,
                StructFields.VertexDescriptionInputs.TimeParameters,
                StructFields.VertexDescriptionInputs.VertexID,
                StructFields.VertexDescriptionInputs.InstanceID,
            }
        };
         public static StructDescriptor CanvasSurfaceDescriptionInputs = new StructDescriptor()
        {
            name = "SurfaceDescriptionInputs",
            packFields = false,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                StructFields.SurfaceDescriptionInputs.ScreenPosition,
                StructFields.SurfaceDescriptionInputs.NDCPosition,
                StructFields.SurfaceDescriptionInputs.PixelPosition,

                StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,
                StructFields.SurfaceDescriptionInputs.ViewSpacePosition,
                StructFields.SurfaceDescriptionInputs.WorldSpacePosition,
                StructFields.SurfaceDescriptionInputs.TangentSpacePosition,
                StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,

                StructFields.SurfaceDescriptionInputs.uv0,
                StructFields.SurfaceDescriptionInputs.uv1,
                StructFields.SurfaceDescriptionInputs.uv2,
                StructFields.SurfaceDescriptionInputs.uv3,
                StructFields.SurfaceDescriptionInputs.VertexColor,
                StructFields.SurfaceDescriptionInputs.TimeParameters,
            }
        };
    }
}
