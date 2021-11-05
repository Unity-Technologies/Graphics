PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = GetBlitVertexPositionFromPositionOS(input.positionOS);
    BuildVaryingsWithoutInverseProjection(input, output);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

FragOutput frag(PackedVaryings packedInput)
{
    return DefaultFullscreenFragmentShader(packedInput);
}
