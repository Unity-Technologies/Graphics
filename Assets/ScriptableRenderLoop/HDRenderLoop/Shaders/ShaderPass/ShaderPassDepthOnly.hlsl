#if SHADERPASS != SHADERPASS_DEPTH_ONLY
#error SHADERPASS_is_not_correctly_define
#endif

float4 Frag(PackedVaryings packedInput) : SV_Target
{
    FragInput input = UnpackVaryings(packedInput);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

    // TODO: handle cubemap shadow
    return float4(0.0, 0.0, 0.0, 0.0);
}

