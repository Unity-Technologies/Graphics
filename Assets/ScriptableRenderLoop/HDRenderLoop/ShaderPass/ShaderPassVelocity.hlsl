#if SHADERPASS != SHADERPASS_VELOCITY
#error SHADERPASS_is_not_correctly_define
#endif

float4 Frag(PackedVaryings packedInput) : SV_Target
{
    FragInput input = UnpackVaryings(packedInput);

    // Perform alpha testing + get velocity
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

    float4 outBuffer;
    EncodeVelocity(builtinData.velocity, outBuffer);
    return outBuffer;
}

