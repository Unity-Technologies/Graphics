SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    $splice(CustomInterpolatorCopyToSDI)

    $SurfaceDescriptionInputs.WorldSpaceNormal: // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
    $SurfaceDescriptionInputs.WorldSpaceNormal: float3 unnormalizedNormalWS = input.normalWS;
    $SurfaceDescriptionInputs.WorldSpaceNormal: const float renormFactor = 1.0 / length(unnormalizedNormalWS);

    $SurfaceDescriptionInputs.WorldSpaceBiTangent: // use bitangent on the fly like in hdrp
    $SurfaceDescriptionInputs.WorldSpaceBiTangent: // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    $SurfaceDescriptionInputs.WorldSpaceBiTangent: float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
    $SurfaceDescriptionInputs.WorldSpaceBiTangent: float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    $SurfaceDescriptionInputs.WorldSpaceNormal:          output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
    $SurfaceDescriptionInputs.ObjectSpaceNormal:         output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.ViewSpaceNormal:           output.ViewSpaceNormal = mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:        output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    $SurfaceDescriptionInputs.WorldSpaceTangent: // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
    $SurfaceDescriptionInputs.WorldSpaceTangent: // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    $SurfaceDescriptionInputs.WorldSpaceTangent: output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent = renormFactor * bitang;

    $SurfaceDescriptionInputs.ObjectSpaceTangent:        output.ObjectSpaceTangent = TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:          output.ViewSpaceTangent = TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:       output.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.ObjectSpaceBiTangent:      output.ObjectSpaceBiTangent = TransformWorldToObjectDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.ViewSpaceBiTangent:        output.ViewSpaceBiTangent = TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.TangentSpaceBiTangent:     output.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceViewDirection:   output.WorldSpaceViewDirection = normalize(input.viewDirectionWS);
    $SurfaceDescriptionInputs.ObjectSpaceViewDirection:  output.ObjectSpaceViewDirection = TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.ViewSpaceViewDirection:    output.ViewSpaceViewDirection = TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection: float3x3 tangentSpaceTransform = float3x3(output.WorldSpaceTangent, output.WorldSpaceBiTangent, output.WorldSpaceNormal);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection: output.TangentSpaceViewDirection = mul(tangentSpaceTransform, output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.WorldSpacePosition:        output.WorldSpacePosition = input.positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePosition:       output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);
    $SurfaceDescriptionInputs.ViewSpacePosition:         output.ViewSpacePosition = TransformWorldToView(input.positionWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:      output.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);
    $SurfaceDescriptionInputs.ScreenPosition:            output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
    $SurfaceDescriptionInputs.uv0:                       output.uv0 = input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                       output.uv1 = input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                       output.uv2 = input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                       output.uv3 = input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:               output.VertexColor = input.color;
    $SurfaceDescriptionInputs.TimeParameters:            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    $SurfaceDescriptionInputs.FaceSign:                  BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

struct v2f_surf {
  float4 pos;//UNITY_POSITION(pos);
  float3 worldNormal;// : TEXCOORD1;
  float3 worldPos;// : TEXCOORD2;
  float4 lmap;// : TEXCOORD3;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh;// : TEXCOORD3; // SH
  #endif
  float1 fogCoord; //UNITY_FOG_COORDS(4)
  DECLARE_LIGHT_COORDS(4)//unityShadowCoord4 _LightCoord;
  UNITY_SHADOW_COORDS(5)//unityShadowCoord4 _ShadowCoord;
  
  //#ifdef DIRLIGHTMAP_COMBINED
  float3 tSpace0 : TEXCOORD6;
  float3 tSpace1 : TEXCOORD7;
  float3 tSpace2 : TEXCOORD8;
  //#endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
{
    $Attributes.positionOS:      result.vertex    = float4(attributes.positionOS, 1);
    $Attributes.tangentOS:       result.tangent   = attributes.tangentOS;
    $Attributes.normalOS:        result.normal    = attributes.normalOS;
    $Attributes.uv0:             result.texcoord  = attributes.uv0;
    $Attributes.uv1:             result.texcoord1 = attributes.uv1;
    $Attributes.uv2:             result.texcoord2 = attributes.uv2;
    $Attributes.uv3:             result.texcoord3 = attributes.uv3;
    $Attributes.color:           result.color     = attributes.color;
    $Attributes.instanceID:      #if UNITY_ANY_INSTANCING_ENABLED
    $Attributes.instanceID:      result.instanceID = attributes.instanceID;
    $Attributes.instanceID:      #endif
    $VertexDescription.Position: result.vertex    = float4(vertexDescription.Position, 1);
    $VertexDescription.Normal:   result.normal    = vertexDescription.Normal;
    $VertexDescription.Tangent:  result.tangent   = float4(vertexDescription.Tangent, 0);
}

void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
{
    result.pos = varyings.positionCS;
    $Varyings.positionWS:  result.worldPos = varyings.positionWS;
    $Varyings.normalWS:    result.worldNormal = varyings.normalWS;
    // World Tangent isn't an available input on v2f_surf
    $Varyings.shadowCoord: result._ShadowCoord = varyings.shadowCoord;
    $Varyings.sh:          #if UNITY_SHOULD_SAMPLE_SH
    $Varyings.sh:          result.sh = varyings.sh;
    $Varyings.sh:          #endif
    $Varyings.instanceID:  #if UNITY_ANY_INSTANCING_ENABLED
    $Varyings.instanceID:  UNITY_TRANSFER_INSTANCE_ID(varyings, result);
    $Varyings.instanceID:  #endif
    $Varyings.lightmapUV:  #if defined(LIGHTMAP_ON)
    $Varyings.lightmapUV:  result.lmap.xy = varyings.lightmapUV;
    $Varyings.lightmapUV:  #endif
    $Varyings.shadowCoord: result._ShadowCoord = varyings.shadowCoord;

    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    result.fogCoord = varyings.fogFactorAndVertexLight.x;
    COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
}

void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
{
    result.positionCS = surfVertex.pos;
    $Varyings.positionWS:  result.positionWS = surfVertex.worldPos;
    $Varyings.normalWS:    result.normalWS = surfVertex.worldNormal;
    // World Tangent isn't an available input on v2f_surf
    $Varyings.sh:          result.shadowCoord = surfVertex._ShadowCoord;
    $Varyings.sh:          #if UNITY_SHOULD_SAMPLE_SH
    $Varyings.sh:          result.sh = surfVertex.sh;
    $Varyings.sh:          #endif
    $Varyings.instanceID:  #if UNITY_ANY_INSTANCING_ENABLED
    $Varyings.instanceID:  UNITY_TRANSFER_INSTANCE_ID(surfVertex, result);
    $Varyings.instanceID:  #endif
    $Varyings.lightmapUV:  #if defined(LIGHTMAP_ON)
    $Varyings.lightmapUV:  result.lightmapUV = surfVertex.lmap.xy;
    $Varyings.lightmapUV:  #endif
    $Varyings.shadowCoord: result.shadowCoord = surfVertex._ShadowCoord;
    
    #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
    COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
    #endif

    DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
}

