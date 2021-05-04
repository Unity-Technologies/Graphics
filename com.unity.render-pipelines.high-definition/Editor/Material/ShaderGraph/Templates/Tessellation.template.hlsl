
float GetTessellationFactor(VaryingsMeshToDS input)
{
    //$VertexDescription.TessellationFactor: float3 displacementWS = vertexDescription.TessellationFactor;
    return 1.0;
}

float GetMaxDisplacement()
{
    //$VertexDescription.TessellationMaxDisplacement: float3 displacementWS = vertexDescription.TessellationMaxDisplacement;
    return 0.01;
}

// TODO => must take into account custom interpolator
VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

    return output;
}

#ifdef HAVE_TESSELLATION_MODIFICATION
// tessellationFactors
// x - 1->2 edge
// y - 2->0 edge
// z - 0->1 edge
// w - inside tessellation factor
void ApplyTessellationModification(VaryingsMeshToDS input, float3 normalWS, inout float3 positionRWS)
{
    // TODO: This shouldn't be in SurfaceDescription but in TessellationDescription
   // $VertexDescription.TessellationDisplacement: float3 displacementWS = vertexDescription.TessellationDisplacement;

    return ;
}

#endif
