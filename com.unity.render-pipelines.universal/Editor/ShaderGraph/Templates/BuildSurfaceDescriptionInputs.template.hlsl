SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

	$SurfaceDescriptionInputs.WorldSpaceNormal: // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
	$SurfaceDescriptionInputs.WorldSpaceNormal: float3 unnormalizedNormalWS = input.normalWS;
    $SurfaceDescriptionInputs.WorldSpaceNormal: const float renormFactor = 1.0 / length(unnormalizedNormalWS);

	$SurfaceDescriptionInputs.WorldSpaceBiTangent: // use bitangent on the fly like in hdrp
	$SurfaceDescriptionInputs.WorldSpaceBiTangent: // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    $SurfaceDescriptionInputs.WorldSpaceBiTangent: float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
	$SurfaceDescriptionInputs.WorldSpaceBiTangent: float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

    $SurfaceDescriptionInputs.WorldSpaceNormal:          output.WorldSpaceNormal =            renormFactor*input.normalWS.xyz;		// we want a unit length Normal Vector node in shader graph
    $SurfaceDescriptionInputs.ObjectSpaceNormal:         output.ObjectSpaceNormal =           normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.ViewSpaceNormal:           output.ViewSpaceNormal =             mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:        output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);

	$SurfaceDescriptionInputs.WorldSpaceTangent: // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
	$SurfaceDescriptionInputs.WorldSpaceTangent: // This is explained in section 2.2 in "surface gradient based bump mapping framework"
    $SurfaceDescriptionInputs.WorldSpaceTangent:         output.WorldSpaceTangent =           renormFactor*input.tangentWS.xyz;
	$SurfaceDescriptionInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent =         renormFactor*bitang;

    $SurfaceDescriptionInputs.ObjectSpaceTangent:        output.ObjectSpaceTangent =          TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:          output.ViewSpaceTangent =            TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:       output.TangentSpaceTangent =         float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.ObjectSpaceBiTangent:      output.ObjectSpaceBiTangent =        TransformWorldToObjectDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.ViewSpaceBiTangent:        output.ViewSpaceBiTangent =          TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $SurfaceDescriptionInputs.TangentSpaceBiTangent:     output.TangentSpaceBiTangent =       float3(0.0f, 1.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceViewDirection:   output.WorldSpaceViewDirection =     input.viewDirectionWS; //TODO: by default normalized in HD, but not in universal
    $SurfaceDescriptionInputs.ObjectSpaceViewDirection:  output.ObjectSpaceViewDirection =    TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.ViewSpaceViewDirection:    output.ViewSpaceViewDirection =      TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection: float3x3 tangentSpaceTransform =     float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $SurfaceDescriptionInputs.TangentSpaceViewDirection: output.TangentSpaceViewDirection =   mul(tangentSpaceTransform, output.WorldSpaceViewDirection);
    $SurfaceDescriptionInputs.WorldSpacePosition:        output.WorldSpacePosition =          input.positionWS;
    $SurfaceDescriptionInputs.ObjectSpacePosition:       output.ObjectSpacePosition =         TransformWorldToObject(input.positionWS);
    $SurfaceDescriptionInputs.ViewSpacePosition:         output.ViewSpacePosition =           TransformWorldToView(input.positionWS);
    $SurfaceDescriptionInputs.TangentSpacePosition:      output.TangentSpacePosition =        float3(0.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.AbsoluteWorldSpacePosition:output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(input.positionWS);
    $SurfaceDescriptionInputs.ScreenPosition:            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
    $SurfaceDescriptionInputs.uv0:                       output.uv0 =                         input.texCoord0;
    $SurfaceDescriptionInputs.uv1:                       output.uv1 =                         input.texCoord1;
    $SurfaceDescriptionInputs.uv2:                       output.uv2 =                         input.texCoord2;
    $SurfaceDescriptionInputs.uv3:                       output.uv3 =                         input.texCoord3;
    $SurfaceDescriptionInputs.VertexColor:               output.VertexColor =                 input.color;
    $SurfaceDescriptionInputs.TimeParameters:            output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    $SurfaceDescriptionInputs.FaceSign:                  BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

    return output;
}
