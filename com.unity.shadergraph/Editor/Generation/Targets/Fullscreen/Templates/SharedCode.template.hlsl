SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    float3 normalWS = SHADERGRAPH_SAMPLE_SCENE_NORMAL(input.texCoord0.xy);
    float4 tangentWS = float4(0, 1, 0, 0); // We can't access the tangent in screen space

    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      // use bitangent on the fly like in hdrp
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      float crossSign = (tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      float3 bitang = crossSign * cross(normalWS.xyz, tangentWS.xyz);

    $SurfaceDescriptionInputs.WorldSpaceNormal:                         output.WorldSpaceNormal = normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                        output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.ViewSpaceNormal:                          output.ViewSpaceNormal = mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:                       output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    $SurfaceDescriptionInputs.WorldSpaceTangent:                        // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    $SurfaceDescriptionInputs.WorldSpaceTangent:                        // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    $SurfaceDescriptionInputs.WorldSpaceTangent:                        output.WorldSpaceTangent = tangentWS.xyz;
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      output.WorldSpaceBiTangent = bitang;

    float3 viewDirWS = normalize(input.texCoord1.xyz);
    float linearDepth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(input.texCoord0.xy), _ZBufferParams);
    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float camearDistance = linearDepth / dot(viewDirWS, cameraForward);
    float3 positionWS = viewDirWS * camearDistance + GetCameraPositionWS();

    $SurfaceDescriptionInputs.ObjectSpaceTangent:                       output.ObjectSpaceTangent = TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:                         output.ViewSpaceTangent = TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:                      output.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.ObjectSpaceBiTangent:                     output.ObjectSpaceBiTangent = TransformWorldToObjectDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.ViewSpaceBiTangent:                       output.ViewSpaceBiTangent = TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.TangentSpaceBiTangent:                    output.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceViewDirection:                  output.WorldSpaceViewDirection = normalize(viewDirWS);
    $SurfaceDescriptionInputs.ObjectSpaceViewDirection:                 output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.ViewSpaceViewDirection:                   output.ViewSpaceViewDirection = TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                float3x3 tangentSpaceTransform = float3x3(output.WorldSpaceTangent, output.WorldSpaceBiTangent, output.WorldSpaceNormal);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                output.TangentSpaceViewDirection = mul(tangentSpaceTransform, output.WorldSpaceViewDirection);

    $SurfaceDescriptionInputs.WorldSpacePosition:                       output.WorldSpacePosition = positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePosition:                      output.ObjectSpacePosition = TransformWorldToObject(positionWS);
    $SurfaceDescriptionInputs.ViewSpacePosition:                        output.ViewSpacePosition = TransformWorldToView(positionWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:                     output.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:               output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(positionWS);
    $SurfaceDescriptionInputs.WorldSpacePositionPredisplacement:        output.WorldSpacePositionPredisplacement = positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement:       output.ObjectSpacePositionPredisplacement = TransformWorldToObject(positionWS);
    $SurfaceDescriptionInputs.ViewSpacePositionPredisplacement:         output.ViewSpacePositionPredisplacement = TransformWorldToView(positionWS);
    $SurfaceDescriptionInputs.TangentSpacePositionPredisplacement:      output.TangentSpacePositionPredisplacement = float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement:output.AbsoluteWorldSpacePositionPredisplacement = GetAbsolutePositionWS(positionWS);
    $SurfaceDescriptionInputs.ScreenPosition:                           output.ScreenPosition = float4(input.texCoord0.xy, 0, 1);
    $SurfaceDescriptionInputs.uv0:                                      output.uv0 = input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                                      output.uv1 = input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                                      output.uv2 = input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                                      output.uv3 = input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:                              output.VertexColor = input.color;
    $SurfaceDescriptionInputs.TimeParameters:                           output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
    $SurfaceDescriptionInputs.NDCPosition:                              output.NDCPosition = input.texCoord0.xy;

#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    $SurfaceDescriptionInputs.FaceSign:                  BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}
