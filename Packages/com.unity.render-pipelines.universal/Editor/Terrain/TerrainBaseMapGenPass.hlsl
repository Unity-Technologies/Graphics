Varyings Vert(Attributes IN)
{
    Varyings output = (Varyings) 0;

    output.positionCS = TransformWorldToHClip(IN.positionOS.xyz);

    // NOTE : This is basically coming from the vertex shader in TerrainLitPasses
    // There are other plenty of other values that the original version computes, but for this
    // pass, we are only interested in a few, so I'm just skipping the rest.
    output.texCoord0.xy = IN.uv0; // uvMainAndLM

    return output;
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = Vert(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#if SHADERPASS == SHADERPASS_MAINTEX
    return half4(surfaceDescription.BaseColor, surfaceDescription.Smoothness);
#elif SHADERPASS == SHADERPASS_METALLICTEX
    return surfaceDescription.Metallic;
#endif
}
