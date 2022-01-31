#ifdef TESSELLATION_ON

// TODO: We should generate this struct like all the other varying struct
VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    // The set of values that need to interlopated is fixed
    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
    output.positionPredisplacementRWS = output.positionRWS;

    // Pass-Through for custom interpolator
    $splice(CustomInterpolatorInterpolateWithBaryCoordsMeshToDS)

    return output;
}

VertexDescriptionInputs VaryingsMeshToDSToVertexDescriptionInputs(VaryingsMeshToDS input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);
    // The only variable that needs to propagated from vertex to is the camera relative world position
    output.WorldSpacePosition = input.positionRWS;
    return output;
}

// tessellationFactors
// x - 1->2 edge
// y - 2->0 edge
// z - 0->1 edge
// w - inside tessellation factor
// The water shader graph required these four fields to be fed (not an option)
void ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters, out float3 positionOS, out float3 normalOS, out float4 uv0, out float4 uv1)
{
    // HACK: As there is no specific tessellation stage for now in shadergraph, we reuse the vertex description mechanism.
    // It mean we store TessellationFactor inside vertex description causing extra read on both vertex and hull stage, but unusued paramater are optimize out by the shader compiler, so no impact.
    VertexDescriptionInputs vertexDescriptionInputs = VaryingsMeshToDSToVertexDescriptionInputs(input);

    // Override time paramters with used one (This is required to correctly handle motion vector for tessellation animation based on time)
    $VertexDescriptionInputs.TimeParameters: vertexDescriptionInputs.TimeParameters = timeParameters;

    // evaluate vertex graph
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    // The set of vertex description outputs that are used are fixed
    positionOS = vertexDescription.Position;
    normalOS = vertexDescription.Normal;
    uv0 = vertexDescription.uv0;
    uv1 = vertexDescription.uv1;

    $splice(CustomInterpolatorVertMeshCustomInterpolation)
}

#endif // TESSELLATION_ON
