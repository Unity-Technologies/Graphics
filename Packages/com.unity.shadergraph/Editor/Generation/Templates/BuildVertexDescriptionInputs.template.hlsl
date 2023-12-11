VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ViewSpaceNormal:                           output.ViewSpaceNormal =                            TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:                        output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.ObjectSpaceTangent:                        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    $VertexDescriptionInputs.WorldSpaceTangent:                         output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
    $VertexDescriptionInputs.ViewSpaceTangent:                          output.ViewSpaceTangent =                           TransformWorldToViewDir(output.WorldSpaceTangent);
    $VertexDescriptionInputs.TangentSpaceTangent:                       output.TangentSpaceTangent =                        float3(1.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:                      output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
    $VertexDescriptionInputs.WorldSpaceBiTangent:                       output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
    $VertexDescriptionInputs.ViewSpaceBiTangent:                        output.ViewSpaceBiTangent =                         TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $VertexDescriptionInputs.TangentSpaceBiTangent:                     output.TangentSpaceBiTangent =                      float3(0.0f, 1.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.ViewSpacePosition:                         output.ViewSpacePosition =                          TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:                      output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.ObjectSpacePositionPredisplacement:        output.ObjectSpacePositionPredisplacement =         input.positionOS;
    $VertexDescriptionInputs.WorldSpacePositionPredisplacement:         output.WorldSpacePositionPredisplacement =          TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.ViewSpacePositionPredisplacement:          output.ViewSpacePositionPredisplacement =           TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePositionPredisplacement:       output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement: output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.WorldSpaceViewDirection:                   output.WorldSpaceViewDirection =                    GetWorldSpaceNormalizeViewDir(output.WorldSpacePosition);
    $VertexDescriptionInputs.ObjectSpaceViewDirection:                  output.ObjectSpaceViewDirection =                   TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ViewSpaceViewDirection:                    output.ViewSpaceViewDirection =                     TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 float3x3 tangentSpaceTransform =                    float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceViewDirection:                 output.TangentSpaceViewDirection =                  TransformWorldToTangent(output.WorldSpaceViewDirection, tangentSpaceTransform);
    $VertexDescriptionInputs.ScreenPosition:                            output.ScreenPosition =                             ComputeScreenPos(TransformWorldToHClip(output.WorldSpacePosition), _ProjectionParams.x);
    $VertexDescriptionInputs.NDCPosition:                               output.NDCPosition =                                output.ScreenPosition.xy / output.ScreenPosition.w;
    $VertexDescriptionInputs.PixelPosition:                             output.PixelPosition =                              float2(output.NDCPosition.x, 1.0f - output.NDCPosition.y) * GetScaledScreenParams().xy;
    $VertexDescriptionInputs.uv0:                                       output.uv0 =                                        input.uv0;
    $VertexDescriptionInputs.uv1:                                       output.uv1 =                                        input.uv1;
    $VertexDescriptionInputs.uv2:                                       output.uv2 =                                        input.uv2;
    $VertexDescriptionInputs.uv3:                                       output.uv3 =                                        input.uv3;
    $VertexDescriptionInputs.VertexColor:                               output.VertexColor =                                input.color;
    $VertexDescriptionInputs.TimeParameters:                            output.TimeParameters =                             _TimeParameters.xyz;
    $VertexDescriptionInputs.BoneWeights:                               output.BoneWeights =                                input.weights;
    $VertexDescriptionInputs.BoneIndices:                               output.BoneIndices =                                input.indices;
    $VertexDescriptionInputs.VertexID:                                  output.VertexID =                                   input.vertexID;
#if UNITY_ANY_INSTANCING_ENABLED
    $VertexDescriptionInputs.InstanceID:                                output.InstanceID =                                 unity_InstanceID;
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
    $VertexDescriptionInputs.InstanceID:                                output.InstanceID =                                 input.instanceID;
#endif

    return output;
}
