VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    // The only parameters that can be requested are the position, normal and time parameters
    $VertexDescriptionInputs.ObjectSpacePosition:                       output.ObjectSpacePosition =                        input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:                        output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    $VertexDescriptionInputs.ViewSpacePosition:                         output.ViewSpacePosition =                          TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:                      output.TangentSpacePosition =                       float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.AbsoluteWorldSpacePosition:                output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS).xyz);
    $VertexDescriptionInputs.ObjectSpaceNormal:                         output.ObjectSpaceNormal =                          input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:                          output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ViewSpaceNormal:                           output.ViewSpaceNormal =                            TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:                        output.TangentSpaceNormal =                         float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.VertexColor:                               output.VertexColor =                                input.color;
    $VertexDescriptionInputs.TimeParameters:                            output.TimeParameters =                             _TimeParameters.xyz; // Note: in case of animation this will be overwrite (allow to handle motion vector)

    return output;
}

void PackWaterVertexData(VertexDescription vertexDescription, out float4 uv0, out float4 uv1)
{
    float3 displacement = vertexDescription.Displacement;

    #if defined(SHADER_STAGE_VERTEX) && defined(TESSELLATION_ON)
    uv0 = float4(vertexDescription.Displacement, 1.0);
    uv1 = float4(vertexDescription.Position, 1.0);
    #else
    uv0 = float4(vertexDescription.Position.x, vertexDescription.Position.z, displacement.y, 0.0);
    uv1 = float4(vertexDescription.LowFrequencyHeight, length(float2(displacement.x, displacement.z)), 0.0, 0.0);
    #endif
}

// The water shader graph required these four fields to be fed (not an option)
// Modifications should probably be replicated to ApplyTessellationModification
AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
    #ifdef USE_CUSTOMINTERP_SUBSTRUCT
    #ifdef TESSELLATION_ON
    , inout VaryingsMeshToDS varyings
    #else
    , inout VaryingsMeshToPS varyings
    #endif
    #endif
    )
{
    // build graph inputs
    VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);

    // Override time parameters with used one (This is required to correctly handle motion vectors for vertex animation based on time)
    $VertexDescriptionInputs.TimeParameters: vertexDescriptionInputs.TimeParameters = timeParameters;

    // evaluate vertex graph
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    // Backward compatibility with old graphs
    $VertexDescriptionInputs.uv0: vertexDescription.Displacement = vertexDescription.uv0.xyz;
    $VertexDescriptionInputs.uv1: vertexDescription.LowFrequencyHeight = vertexDescription.uv1.x;

    input.positionOS = vertexDescription.Position + vertexDescription.Displacement;
    input.normalOS = vertexDescription.Normal;
    PackWaterVertexData(vertexDescription, input.uv0, input.uv1);

    $splice(CustomInterpolatorVertMeshCustomInterpolation)

    return input;
}

FragInputs BuildFragInputs(VaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
    // to compute normals which are then passed on elsewhere to compute other values...
    output.tangentToWorld = k_identity3x3;
    output.positionSS = input.positionCS;       // input.positionCS is SV_Position

    $FragInputs.positionRWS:                    output.positionRWS =                input.positionRWS;
    $FragInputs.positionPixel:                  output.positionPixel =              input.positionCS.xy; // NOTE: this is not actually in clip space, it is the VPOS pixel coordinate value
    $FragInputs.positionPredisplacementRWS:     output.positionPredisplacementRWS = input.positionPredisplacementRWS;
    $FragInputs.tangentToWorld:                 output.tangentToWorld =             GetLocalFrame(input.normalWS);
    $FragInputs.texCoord0:                      output.texCoord0 =                  input.texCoord0;
    $FragInputs.texCoord1:                      output.texCoord1 =                  input.texCoord1;

    // splice point to copy custom interpolator fields from varyings to frag inputs
    $splice(CustomInterpolatorVaryingsToFragInputs)

    return output;
}

// existing HDRP code uses the combined function to go directly from packed to frag inputs
FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
    return BuildFragInputs(unpacked);
}
