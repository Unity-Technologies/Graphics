SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    $SurfaceDescriptionInputs.WorldSpaceNormal:          output.WorldSpaceNormal =            input.normalWS;
    $SurfaceDescriptionInputs.ObjectSpaceNormal:         output.ObjectSpaceNormal =           mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M);           // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.ViewSpaceNormal:           output.ViewSpaceNormal =             mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    $SurfaceDescriptionInputs.TangentSpaceNormal:        output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);
    $SurfaceDescriptionInputs.WorldSpaceTangent:         output.WorldSpaceTangent =           input.tangentWS.xyz;
    $SurfaceDescriptionInputs.ObjectSpaceTangent:        output.ObjectSpaceTangent =          TransformWorldToObjectDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.ViewSpaceTangent:          output.ViewSpaceTangent =            TransformWorldToViewDir(output.WorldSpaceTangent);
    $SurfaceDescriptionInputs.TangentSpaceTangent:       output.TangentSpaceTangent =         float3(1.0f, 0.0f, 0.0f);
    $SurfaceDescriptionInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent =         input.bitangentWS;
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
