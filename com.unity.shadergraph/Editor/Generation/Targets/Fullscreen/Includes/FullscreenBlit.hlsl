PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
#if SHADER_API_GLES
    output.positionCS = float4(input.positionOS.xyz, 1);
#else
    output.positionCS = GetBlitVertexPosition(input.vertexID);
#endif
    BuildVaryings(input, output);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

FragOutput frag(PackedVaryings packedInput)
{
    return DefaultFullscreenFragmentShader(packedInput);
}
