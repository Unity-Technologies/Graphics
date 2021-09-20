SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    $SurfaceDescriptionInputs.WorldSpaceNormal:                         // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    $SurfaceDescriptionInputs.WorldSpaceNormal:                         float3 unnormalizedNormalWS = input.normalWS;
    $SurfaceDescriptionInputs.WorldSpaceNormal:                         const float renormFactor = 1.0 / length(unnormalizedNormalWS);

    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      // use bitangent on the fly like in hdrp
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    $SurfaceDescriptionInputs.WorldSpaceNormal:                         output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    $SurfaceDescriptionInputs.ObjectSpaceNormal:                        output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.ViewSpaceNormal:                          output.ViewSpaceNormal = mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:                       output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    $SurfaceDescriptionInputs.WorldSpaceTangent:                        // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    $SurfaceDescriptionInputs.WorldSpaceTangent:                        // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    $SurfaceDescriptionInputs.WorldSpaceTangent:                        output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:                      output.WorldSpaceBiTangent = renormFactor * bitang;

    // Calculate world space view direction without the position WS:
    bool ortho = UNITY_MATRIX_P[3][3] == 0;
    float3 viewDirWS;
    if (ortho)
    {
        viewDirWS = -GetWorldToViewMatrix()[2].xyz;
    }
    else
    {
        float4 n = mul(UNITY_MATRIX_I_VP, float4(input.positionCS.xy, 0, 1));
        float4 f = n + UNITY_MATRIX_I_VP[2];
        n.xyz /= n.w;
        f.xyz /= f.w;
        viewDirWS = f.xyz-n.xyz;
    }

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

    // Calculate world space position:
    float2 screenSpaceUV = input.texCoord0.xy;
    float linearDepth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(screenSpaceUV), _ZBufferParams);
    float3 positionWS = viewDirWS * linearDepth;

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
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    $SurfaceDescriptionInputs.FaceSign:                  BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}
