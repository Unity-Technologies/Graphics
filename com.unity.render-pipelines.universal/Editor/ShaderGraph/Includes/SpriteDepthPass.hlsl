float _CustomDepth;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);

    output.positionCS.xy /= output.positionCS.w;
    output.positionCS.z = _CustomDepth > 0 ? _CustomDepth : input.uv1.x;
    output.positionCS.w = 1.0f;

    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    return 0;
}
