SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    $SurfaceDescriptionInputs.WorldSpaceNormal:                             output.WorldSpaceNormal =                           normalize(input.tangentToWorld[2].xyz);
    #if defined(SHADER_STAGE_RAY_TRACING)
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                            output.ObjectSpaceNormal =                          mul(output.WorldSpaceNormal, (float3x3) ObjectToWorld3x4());
    #else
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                            output.ObjectSpaceNormal =                          normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    #endif
    $SurfaceDescriptionInputs.ViewSpaceNormal:                              output.ViewSpaceNormal =                            mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:                           output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $SurfaceDescriptionInputs.WorldSpaceTangent:                            output.WorldSpaceTangent =                          input.tangentToWorld[0].xyz;
    $SurfaceDescriptionInputs.ObjectSpaceTangent:                           output.ObjectSpaceTangent =                         TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:                             output.ViewSpaceTangent =                           TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:                          output.TangentSpaceTangent =                        float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                          output.WorldSpaceBiTangent =                        input.tangentToWorld[1].xyz;
    $SurfaceDescriptionInputs.ObjectSpaceBiTangent:                         output.ObjectSpaceBiTangent =                       TransformWorldToObjectDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.ViewSpaceBiTangent:                           output.ViewSpaceBiTangent =                         TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.TangentSpaceBiTangent:                        output.TangentSpaceBiTangent =                      float3(0.0f, 1.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceViewDirection:                      output.WorldSpaceViewDirection =                    normalize(viewWS);
    $SurfaceDescriptionInputs.ObjectSpaceViewDirection:                     output.ObjectSpaceViewDirection =                   TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.ViewSpaceViewDirection:                       output.ViewSpaceViewDirection =                     TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                    float3x3 tangentSpaceTransform =                    float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                    output.TangentSpaceViewDirection =                  TransformWorldToTangent(output.WorldSpaceViewDirection, tangentSpaceTransform);
    $SurfaceDescriptionInputs.WorldSpacePosition:                           output.WorldSpacePosition =                         input.positionRWS;
#if SHADERPASS != SHADERPASS_FOG_VOLUME_VOXELIZATION
    $SurfaceDescriptionInputs.ObjectSpacePosition:                          output.ObjectSpacePosition =                        TransformWorldToObject(input.positionRWS);
#else
    $SurfaceDescriptionInputs.ObjectSpacePosition:                          output.ObjectSpacePosition =                        TransformWorldToObjectFog(input.positionRWS);
#endif
    $SurfaceDescriptionInputs.ViewSpacePosition:                            output.ViewSpacePosition =                          TransformWorldToView(input.positionRWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:                         output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:                   output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(input.positionRWS);
    $SurfaceDescriptionInputs.WorldSpacePositionPredisplacement:            output.WorldSpacePositionPredisplacement =          input.positionPredisplacementRWS;
#if SHADERPASS != SHADERPASS_FOG_VOLUME_VOXELIZATION
    $SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement:           output.ObjectSpacePositionPredisplacement =         TransformWorldToObject(input.positionPredisplacementRWS);
#else
    $SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement:           output.ObjectSpacePositionPredisplacement =         TransformWorldToObjectFog(input.positionPredisplacementRWS);
#endif
    $SurfaceDescriptionInputs.ViewSpacePositionPredisplacement:             output.ViewSpacePositionPredisplacement =           TransformWorldToView(input.positionPredisplacementRWS);
    $SurfaceDescriptionInputs.TangentSpacePositionPredisplacement:          output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement:    output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(input.positionPredisplacementRWS);
    $SurfaceDescriptionInputs.ScreenPosition:                               output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);

#if UNITY_UV_STARTS_AT_TOP
    $SurfaceDescriptionInputs.PixelPosition:                                output.PixelPosition = float2(input.positionPixel.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionPixel.y) : input.positionPixel.y);
#else
    $SurfaceDescriptionInputs.PixelPosition:                                output.PixelPosition = float2(input.positionPixel.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionPixel.y) : input.positionPixel.y);
#endif

    $SurfaceDescriptionInputs.NDCPosition:                                  output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    $SurfaceDescriptionInputs.NDCPosition:                                  output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    $SurfaceDescriptionInputs.uv0:                                          output.uv0 =                                        input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                                          output.uv1 =                                        input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                                          output.uv2 =                                        input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                                          output.uv3 =                                        input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:                                  output.VertexColor =                                input.color;
    $SurfaceDescriptionInputs.FaceSign:                                     output.FaceSign =                                   input.isFrontFace;
    $SurfaceDescriptionInputs.InstanceID:                                   output.InstanceID =                                 input.instanceID;
    $SurfaceDescriptionInputs.TimeParameters:                               output.TimeParameters =                             _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value

    // splice point to copy frag inputs custom interpolator pack into the SDI
    $splice(CustomInterpolatorCopyToSDI)

    return output;
}
