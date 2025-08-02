SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    $splice(CustomInterpolatorCopyToSDI)

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

    $SurfaceDescriptionInputs.ObjectSpaceTangent:                       output.ObjectSpaceTangent = TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:                         output.ViewSpaceTangent = TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:                      output.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.ObjectSpaceBiTangent:                     output.ObjectSpaceBiTangent = TransformWorldToObjectDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.ViewSpaceBiTangent:                       output.ViewSpaceBiTangent = TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.TangentSpaceBiTangent:                    output.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceViewDirection:                  output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
    $SurfaceDescriptionInputs.ObjectSpaceViewDirection:                 output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.ViewSpaceViewDirection:                   output.ViewSpaceViewDirection = TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                float3x3 tangentSpaceTransform = float3x3(output.WorldSpaceTangent, output.WorldSpaceBiTangent, output.WorldSpaceNormal);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection:                output.TangentSpaceViewDirection = mul(tangentSpaceTransform, output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.WorldSpacePosition:                       output.WorldSpacePosition = input.positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePosition:                      output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);
    $SurfaceDescriptionInputs.ViewSpacePosition:                        output.ViewSpacePosition = TransformWorldToView(input.positionWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:                     output.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:               output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);
    $SurfaceDescriptionInputs.WorldSpacePositionPredisplacement:        output.WorldSpacePositionPredisplacement = input.positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement:       output.ObjectSpacePositionPredisplacement = TransformWorldToObject(input.positionWS);
    $SurfaceDescriptionInputs.ViewSpacePositionPredisplacement:         output.ViewSpacePositionPredisplacement = TransformWorldToView(input.positionWS);
    $SurfaceDescriptionInputs.TangentSpacePositionPredisplacement:      output.TangentSpacePositionPredisplacement = float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement:output.AbsoluteWorldSpacePositionPredisplacement = GetAbsolutePositionWS(input.positionWS);
    $SurfaceDescriptionInputs.ScreenPosition:                           output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

#if UNITY_UV_STARTS_AT_TOP
    $SurfaceDescriptionInputs.PixelPosition:                            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
#else
    $SurfaceDescriptionInputs.PixelPosition:                            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    $SurfaceDescriptionInputs.NDCPosition:                              output.NDCPosition = output.PixelPosition.xy / _ScreenParams.xy;
    $SurfaceDescriptionInputs.NDCPosition:                              output.NDCPosition.y = 1.0f - output.NDCPosition.y;

    $SurfaceDescriptionInputs.uv0:                                      output.uv0 = input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                                      output.uv1 = input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                                      output.uv2 = input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                                      output.uv3 = input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:                              output.VertexColor = input.color;
    $SurfaceDescriptionInputs.TimeParameters:                           output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#ifdef UNIVERSAL_TERRAIN_SPLAT01
    $SurfaceDescriptionInputs.uvSplat01:                                output.uvSplat01 = input.uvSplat01;
#endif
#ifdef UNIVERSAL_TERRAIN_SPLAT23
    $SurfaceDescriptionInputs.uvSplat23:                                output.uvSplat23 = input.uvSplat23;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign = IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    $SurfaceDescriptionInputs.FaceSign:                                 BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

    return output;
}

#if SHADERPASS == SHADERPASS_FORWARD || SHADERPASS == SHADERPASS_GBUFFER

void CalculateTerrainNormalWS(Varyings input, inout SurfaceDescription surfaceDescription, inout InputData inputData)
{
    $SurfaceDescription.NormalTS: half3 normalTS = surfaceDescription.NormalTS;
    $SurfaceDescription.NormalOS: half3 NormalOS = surfaceDescription.NormalOS;
    $SurfaceDescription.NormalWS: half3 normalWS = surfaceDescription.NormalWS;

#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = half3(input.normalViewDir.w, input.tangentViewDir.w, input.bitangentViewDir.w);

    $SurfaceDescription.NormalTS: inputData.tangentToWorld = half3x3(-input.tangentViewDir.xyz, input.bitangentViewDir.xyz, input.normalViewDir.xyz);
    $SurfaceDescription.NormalTS: half3 normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    $SurfaceDescription.NormalTS: half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, input.normalWS);
    $SurfaceDescription.NormalTS: half3 normalWS = TransformTangentToWorld(normalTS, half3x3(-tangentWS, cross(input.normalWS, tangentWS), input.normalWS));
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    $SurfaceDescription.NormalTS: half3 normalWS = input.normalWS;
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#endif

    inputData.normalWS = NormalizeNormalPerPixel(normalWS);
    inputData.viewDirectionWS = viewDirWS;
}

#elif SHADERPASS == SHADERPASS_DEPTHNORMALS

half3 GetTerrainNormalWS(Varyings input, SurfaceDescription surfaceDescription)
{
    $SurfaceDescription.NormalTS: half3 normalTS = surfaceDescription.NormalTS;
    $SurfaceDescription.NormalOS: half3 NormalOS = surfaceDescription.NormalOS;
    $SurfaceDescription.NormalWS: half3 normalWS = surfaceDescription.NormalWS;

#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    $SurfaceDescription.NormalTS: half3x3 tangentToWorld = half3x3(-input.tangentViewDir.xyz, input.bitangentViewDir.xyz, input.normalViewDir.xyz);
    $SurfaceDescription.NormalTS: half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    $SurfaceDescription.NormalTS: half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, input.normalWS);
    $SurfaceDescription.NormalTS: half3 normalWS = TransformTangentToWorld(normalTS, half3x3(-tangentWS, cross(input.normalWS, tangentWS), input.normalWS));
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#else
    $SurfaceDescription.NormalTS: half3 normalWS = input.normalWS;
    $SurfaceDescription.NormalOS: half3 normalWS = TransformObjectToWorldNormal(NormalOS);
#endif

    return normalWS;
}

#endif
