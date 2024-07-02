#ifdef TESSELLATION_ON

// TODO: We should generate this struct like all the other varying struct
VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    // The set of values that need to interpolated is fixed
    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);

    // Pass-Through for custom interpolator
    $splice(CustomInterpolatorInterpolateWithBaryCoordsMeshToDS)

    return output;
}

VertexDescriptionInputs VaryingsMeshToDSToVertexDescriptionInputs(VaryingsMeshToDS input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    // texcoord1 contains the pre displacement object space position
    // normal is marked WS but it's actually in object space :(
    $VertexDescriptionInputs.ObjectSpacePosition:  output.ObjectSpacePosition = input.texCoord1.xyz;
    $VertexDescriptionInputs.WorldSpacePosition:   output.WorldSpacePosition  = TransformObjectToWorld(input.texCoord1.xyz);
    $VertexDescriptionInputs.ObjectSpaceNormal:    output.ObjectSpaceNormal   = input.normalWS;
    $VertexDescriptionInputs.WorldSpaceNormal:     output.WorldSpaceNormal    = TransformObjectToWorldNormal(input.normalWS);

    $VertexDescriptionInputs.VertexColor:          output.VertexColor         = input.color;

    return output;
}

// tessellationFactors
// x - 1->2 edge
// y - 2->0 edge
// z - 0->1 edge
// w - inside tessellation factor
void ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters, inout VaryingsMeshToPS output, out VertexDescription vertexDescription)
{
    // HACK: As there is no specific tessellation stage for now in shadergraph, we reuse the vertex description mechanism.
    // It mean we store TessellationFactor inside vertex description causing extra read on both vertex and hull stage, but unused parameters are optimize out by the shader compiler, so no impact.
    VertexDescriptionInputs vertexDescriptionInputs = VaryingsMeshToDSToVertexDescriptionInputs(input);

    // Override time parameters with used one (This is required to correctly handle motion vector for tessellation animation based on time)
    $VertexDescriptionInputs.TimeParameters: vertexDescriptionInputs.TimeParameters = timeParameters;

    // evaluate vertex graph
    vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    // Backward compatibility with old graphs
    $VertexDescriptionInputs.uv0: vertexDescription.Displacement = vertexDescription.uv0.xyz;
    $VertexDescriptionInputs.uv1: vertexDescription.LowFrequencyHeight = vertexDescription.uv1.x;

    // Custom interpolators
    $splice(CustomInterpolatorVertMeshTesselationCustomInterpolation)
}

#endif // TESSELLATION_ON
