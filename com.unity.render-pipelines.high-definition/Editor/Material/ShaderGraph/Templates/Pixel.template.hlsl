SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef TERRAIN_ENABLED
    // impacts uv0-uv2, input.tangentToWorld, output.WorldSpaceNormal, output.WorldSpaceTangent
#ifdef UNITY_INSTANCING_ENABLED
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
#ifdef TERRAIN_PERPIXEL_NORMAL_OVERRIDE
    float3 normalWS = normalize(input.tangentToWorld[2].xyz);
#else
    float2 terrainNormalMapUV = (input.texCoord0.xy + 0.5f) * _TerrainHeightmapRecipSize.xy;
    float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, terrainNormalMapUV).rgb * 2 - 1;
    float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
#endif // TERRAIN_PERPIXEL_NORMAL_OVERRIDE

    input.texCoord0.xy *= _TerrainHeightmapRecipSize.zw;
    float4 tangentWS = ConstructTerrainTangent(normalWS, GetObjectToWorldMatrix()._13_23_33);
    input.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
#else
#endif // ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif // UNITY_INSTANCING_ENABLED
    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;
    $SurfaceDescriptionInputs.WorldSpaceNormal:                             output.WorldSpaceNormal =                           input.tangentToWorld[2].xyz;
    $SurfaceDescriptionInputs.WorldSpaceTangent:                            output.WorldSpaceTangent =                          normalize(input.tangentToWorld[0].xyz);

#else
    $SurfaceDescriptionInputs.WorldSpaceNormal:                             output.WorldSpaceNormal =                           normalize(input.tangentToWorld[2].xyz);
    $SurfaceDescriptionInputs.WorldSpaceTangent:                            output.WorldSpaceTangent =                          input.tangentToWorld[0].xyz;
#endif // TERRAIN_ENABLED

#if defined(SHADER_STAGE_RAY_TRACING)
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                            output.ObjectSpaceNormal = mul(output.WorldSpaceNormal, (float3x3) ObjectToWorld3x4());
#else
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                            output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
#endif
    $SurfaceDescriptionInputs.ViewSpaceNormal:                              output.ViewSpaceNormal =                            mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:                           output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
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
    $SurfaceDescriptionInputs.ObjectSpacePosition:                          output.ObjectSpacePosition =                        TransformWorldToObject(input.positionRWS);
    $SurfaceDescriptionInputs.ViewSpacePosition:                            output.ViewSpacePosition =                          TransformWorldToView(input.positionRWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:                         output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:                   output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(input.positionRWS);
    $SurfaceDescriptionInputs.WorldSpacePositionPredisplacement:            output.WorldSpacePositionPredisplacement =          input.positionPredisplacementRWS;
    $SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement:           output.ObjectSpacePositionPredisplacement =         TransformWorldToObject(input.positionPredisplacementRWS);
    $SurfaceDescriptionInputs.ViewSpacePositionPredisplacement:             output.ViewSpacePositionPredisplacement =           TransformWorldToView(input.positionPredisplacementRWS);
    $SurfaceDescriptionInputs.TangentSpacePositionPredisplacement:          output.TangentSpacePositionPredisplacement =        float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement:    output.AbsoluteWorldSpacePositionPredisplacement =  GetAbsolutePositionWS(input.positionPredisplacementRWS);
    $SurfaceDescriptionInputs.PixelPosition:                                output.PixelPosition =                              input.positionPixel.xy;
    $SurfaceDescriptionInputs.NDCPosition:                                  output.NDCPosition =                                output.PixelPosition.xy / _ScreenParams.xy;
    $SurfaceDescriptionInputs.ScreenPosition:                               output.ScreenPosition =                             ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
    $SurfaceDescriptionInputs.uv0:                                          output.uv0 =                                        input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                                          output.uv1 =                                        input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                                          output.uv2 =                                        input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                                          output.uv3 =                                        input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:                                  output.VertexColor =                                input.color;
    $SurfaceDescriptionInputs.FaceSign:                                     output.FaceSign =                                   input.isFrontFace;
    $SurfaceDescriptionInputs.TimeParameters:                               output.TimeParameters =                             _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value

    // splice point to copy frag inputs custom interpolator pack into the SDI
    $splice(CustomInterpolatorCopyToSDI)

    return output;
}
