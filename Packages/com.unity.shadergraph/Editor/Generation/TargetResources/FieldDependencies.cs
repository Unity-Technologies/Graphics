namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class FieldDependencies
    {
        public static DependencyCollection Varyings = new DependencyCollection
        {
            new FieldDependency(StructFields.Varyings.positionWS,                                                   StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.Varyings.positionPredisplacementWS,                                    StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.Varyings.normalWS,                                                     StructFields.Attributes.normalOS),
            new FieldDependency(StructFields.Varyings.tangentWS,                                                    StructFields.Attributes.tangentOS),
            new FieldDependency(StructFields.Varyings.texCoord0,                                                    StructFields.Attributes.uv0),
            new FieldDependency(StructFields.Varyings.texCoord1,                                                    StructFields.Attributes.uv1),
            new FieldDependency(StructFields.Varyings.texCoord2,                                                    StructFields.Attributes.uv2),
            new FieldDependency(StructFields.Varyings.texCoord3,                                                    StructFields.Attributes.uv3),
            new FieldDependency(StructFields.Varyings.color,                                                        StructFields.Attributes.color),
            new FieldDependency(StructFields.Varyings.instanceID,                                                   StructFields.Attributes.instanceID),
            new FieldDependency(StructFields.Varyings.vertexID,                                                     StructFields.Attributes.vertexID),
        };

        public static DependencyCollection VertexDescription = new DependencyCollection
        {
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceNormal,                             StructFields.Attributes.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceNormal,                              StructFields.Attributes.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceNormal,                               StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,                            StructFields.Attributes.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceTangent,                             StructFields.Attributes.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceTangent,                              StructFields.VertexDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                          StructFields.Attributes.normalOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                          StructFields.Attributes.tangentOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,                           StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,                            StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpacePosition,                           StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpacePosition,                            StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,                    StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpacePosition,                             StructFields.VertexDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpacePositionPredisplacement,             StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,     StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpacePositionPredisplacement,            StructFields.Attributes.positionOS),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpacePositionPredisplacement,              StructFields.VertexDescriptionInputs.WorldSpacePosition),

            new FieldDependency(StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,                       StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection,                      StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,                        StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                     StructFields.VertexDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.VertexDescriptionInputs.ScreenPosition,                                StructFields.VertexDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv0,                                           StructFields.Attributes.uv0),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv1,                                           StructFields.Attributes.uv1),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv2,                                           StructFields.Attributes.uv2),
            new FieldDependency(StructFields.VertexDescriptionInputs.uv3,                                           StructFields.Attributes.uv3),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexColor,                                   StructFields.Attributes.color),

            new FieldDependency(StructFields.VertexDescriptionInputs.BoneWeights,                                   StructFields.Attributes.weights),
            new FieldDependency(StructFields.VertexDescriptionInputs.BoneIndices,                                   StructFields.Attributes.indices),
            new FieldDependency(StructFields.VertexDescriptionInputs.VertexID,                                      StructFields.Attributes.vertexID),
        };

        public static DependencyCollection SurfaceDescription = new DependencyCollection
        {
            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,                             StructFields.Varyings.normalWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,                            StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,                              StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,                            StructFields.Varyings.tangentWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,                            StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,                           StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,                             StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,                          StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,                          StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,                         StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,                           StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpacePosition,                           StructFields.Varyings.positionWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,                   StructFields.Varyings.positionWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,                          StructFields.Varyings.positionWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpacePosition,                            StructFields.Varyings.positionWS),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpacePositionPredisplacement,            StructFields.Varyings.positionPredisplacementWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,    StructFields.Varyings.positionPredisplacementWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement,           StructFields.Varyings.positionPredisplacementWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpacePositionPredisplacement,             StructFields.Varyings.positionPredisplacementWS),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,                      StructFields.Varyings.viewDirectionWS),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,                     StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,                       StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                    StructFields.SurfaceDescriptionInputs.WorldSpaceNormal),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.ScreenPosition,                               StructFields.SurfaceDescriptionInputs.WorldSpacePosition),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv0,                                          StructFields.Varyings.texCoord0),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv1,                                          StructFields.Varyings.texCoord1),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv2,                                          StructFields.Varyings.texCoord2),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.uv3,                                          StructFields.Varyings.texCoord3),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.VertexColor,                                  StructFields.Varyings.color),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.FaceSign,                                     StructFields.Varyings.cullFace),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.VertexID,                                     StructFields.Varyings.vertexID),
        };

        public static DependencyCollection Default = new DependencyCollection
        {
            { Varyings },
            { VertexDescription },
            { SurfaceDescription },
        };
    }
}
