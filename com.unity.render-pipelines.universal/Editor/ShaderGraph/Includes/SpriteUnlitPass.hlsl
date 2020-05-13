#if ETC1_EXTERNAL_ALPHA
    TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
    half _EnableAlphaTexture;
#endif
    half4 _RendererColor;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#if ETC1_EXTERNAL_ALPHA
    if (_EnableAlphaTexture > 0)
        surfaceDescription.Color.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, unpacked.texCoord0.xy).r;
#endif

    surfaceDescription.Color *= unpacked.color * _RendererColor;

    return surfaceDescription.Color;
}
