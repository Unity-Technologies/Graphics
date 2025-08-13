namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class Structs
    {
        public static StructDescriptor Attributes = new StructDescriptor()
        {
            name = "Attributes",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Attributes.uv3,
                StructFields.Attributes.uv4,
                StructFields.Attributes.uv5,
                StructFields.Attributes.uv6,
                StructFields.Attributes.uv7,
                StructFields.Attributes.color,
                StructFields.Attributes.instanceID,
                StructFields.Attributes.weights,
                StructFields.Attributes.indices,
                StructFields.Attributes.vertexID,
            }
        };

        public static StructDescriptor VertexDescriptionInputs = new StructDescriptor()
        {
            name = "VertexDescriptionInputs",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                StructFields.VertexDescriptionInputs.ObjectSpaceNormal,
                StructFields.VertexDescriptionInputs.ViewSpaceNormal,
                StructFields.VertexDescriptionInputs.WorldSpaceNormal,
                StructFields.VertexDescriptionInputs.TangentSpaceNormal,

                StructFields.VertexDescriptionInputs.ObjectSpaceTangent,
                StructFields.VertexDescriptionInputs.ViewSpaceTangent,
                StructFields.VertexDescriptionInputs.WorldSpaceTangent,
                StructFields.VertexDescriptionInputs.TangentSpaceTangent,

                StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,
                StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,
                StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,
                StructFields.VertexDescriptionInputs.TangentSpaceBiTangent,

                StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection,
                StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,
                StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,
                StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,

                StructFields.VertexDescriptionInputs.ObjectSpacePosition,
                StructFields.VertexDescriptionInputs.ViewSpacePosition,
                StructFields.VertexDescriptionInputs.WorldSpacePosition,
                StructFields.VertexDescriptionInputs.TangentSpacePosition,
                StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,

                StructFields.VertexDescriptionInputs.ObjectSpacePositionPredisplacement,
                StructFields.VertexDescriptionInputs.ViewSpacePositionPredisplacement,
                StructFields.VertexDescriptionInputs.WorldSpacePositionPredisplacement,
                StructFields.VertexDescriptionInputs.TangentSpacePositionPredisplacement,
                StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,

                StructFields.VertexDescriptionInputs.ScreenPosition,
                StructFields.VertexDescriptionInputs.NDCPosition,
                StructFields.VertexDescriptionInputs.PixelPosition,

                StructFields.VertexDescriptionInputs.uv0,
                StructFields.VertexDescriptionInputs.uv1,
                StructFields.VertexDescriptionInputs.uv2,
                StructFields.VertexDescriptionInputs.uv3,
                StructFields.VertexDescriptionInputs.uv4,
                StructFields.VertexDescriptionInputs.uv5,
                StructFields.VertexDescriptionInputs.uv6,
                StructFields.VertexDescriptionInputs.uv7,
                StructFields.VertexDescriptionInputs.VertexColor,
                StructFields.VertexDescriptionInputs.TimeParameters,
                StructFields.VertexDescriptionInputs.BoneWeights,
                StructFields.VertexDescriptionInputs.BoneIndices,
                StructFields.VertexDescriptionInputs.VertexID,
                StructFields.VertexDescriptionInputs.InstanceID,
            }
        };

        public static StructDescriptor SurfaceDescriptionInputs = new StructDescriptor()
        {
            name = "SurfaceDescriptionInputs",
            packFields = false,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,
                StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,
                StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,
                StructFields.SurfaceDescriptionInputs.TangentSpaceNormal,

                StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,
                StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,
                StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,
                StructFields.SurfaceDescriptionInputs.TangentSpaceTangent,

                StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,
                StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,
                StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,
                StructFields.SurfaceDescriptionInputs.TangentSpaceBiTangent,

                StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,
                StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,
                StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,
                StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,

                StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,
                StructFields.SurfaceDescriptionInputs.ViewSpacePosition,
                StructFields.SurfaceDescriptionInputs.WorldSpacePosition,
                StructFields.SurfaceDescriptionInputs.TangentSpacePosition,
                StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,

                StructFields.SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement,
                StructFields.SurfaceDescriptionInputs.ViewSpacePositionPredisplacement,
                StructFields.SurfaceDescriptionInputs.WorldSpacePositionPredisplacement,
                StructFields.SurfaceDescriptionInputs.TangentSpacePositionPredisplacement,
                StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,

                StructFields.SurfaceDescriptionInputs.ScreenPosition,
                StructFields.SurfaceDescriptionInputs.NDCPosition,
                StructFields.SurfaceDescriptionInputs.PixelPosition,

                StructFields.SurfaceDescriptionInputs.uv0,
                StructFields.SurfaceDescriptionInputs.uv1,
                StructFields.SurfaceDescriptionInputs.uv2,
                StructFields.SurfaceDescriptionInputs.uv3,
                StructFields.SurfaceDescriptionInputs.uv4,
                StructFields.SurfaceDescriptionInputs.uv5,
                StructFields.SurfaceDescriptionInputs.uv6,
                StructFields.SurfaceDescriptionInputs.uv7,
                GeneratorDerivativeUtils.uv0Ddx,
                GeneratorDerivativeUtils.uv0Ddy,
                GeneratorDerivativeUtils.uv1Ddx,
                GeneratorDerivativeUtils.uv1Ddy,
                GeneratorDerivativeUtils.uv2Ddx,
                GeneratorDerivativeUtils.uv2Ddy,
                GeneratorDerivativeUtils.uv3Ddx,
                GeneratorDerivativeUtils.uv3Ddy,
                GeneratorDerivativeUtils.uv4Ddx,
                GeneratorDerivativeUtils.uv4Ddy,
                GeneratorDerivativeUtils.uv5Ddx,
                GeneratorDerivativeUtils.uv5Ddy,
                GeneratorDerivativeUtils.uv6Ddx,
                GeneratorDerivativeUtils.uv6Ddy,
                GeneratorDerivativeUtils.uv7Ddx,
                GeneratorDerivativeUtils.uv7Ddy,
                StructFields.SurfaceDescriptionInputs.VertexColor,
                StructFields.SurfaceDescriptionInputs.TimeParameters,
                StructFields.SurfaceDescriptionInputs.FaceSign,
                StructFields.SurfaceDescriptionInputs.BoneWeights,
                StructFields.SurfaceDescriptionInputs.BoneIndices,
                StructFields.SurfaceDescriptionInputs.VertexID,
                StructFields.SurfaceDescriptionInputs.InstanceID,

                StructFields.SurfaceDescriptionInputs.color,
                StructFields.SurfaceDescriptionInputs.uvClip,
                StructFields.SurfaceDescriptionInputs.typeTexSettings,
                StructFields.SurfaceDescriptionInputs.textCoreLoc,
                StructFields.SurfaceDescriptionInputs.layoutUV,
                StructFields.SurfaceDescriptionInputs.circle,
            }
        };
    }
}
