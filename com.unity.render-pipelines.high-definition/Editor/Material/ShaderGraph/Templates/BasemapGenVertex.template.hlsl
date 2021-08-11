AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
#ifdef TESSELLATION_ON
    , inout VaryingsMeshToDS varyings
#else
    , inout VaryingsMeshToPS varyings
#endif
#endif
#ifdef HAVE_VFX_MODIFICATION
    , AttributesElement element
#endif
)
{
    return input;
}

FragInputs BuildFragInputs(VaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.positionSS = input.positionCS;       // input.positionCS is SV_Position
    output.positionRWS = input.positionRWS;
    output.texCoord0 = input.texCoord0;

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
