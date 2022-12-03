PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = GetBlitVertexPosition(input.vertexID);
    BuildVaryings(input, output);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

FragOutput frag(PackedVaryings packedInput)
{
    return DefaultFullscreenFragmentShader(packedInput);
}
