PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;

#if defined(HAVE_VFX_MODIFICATION)
    AttributesElement element;
    //TODOPAUL : fill AttributesElement magically !!!!!!!
    output = BuildVaryings(input, element);
#else
    output = BuildVaryings(input);
#endif
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
#if defined(HAVE_VFX_MODIFICATION)
    GraphProperties properties;
    ZERO_INITIALIZE(GraphProperties, properties);
    GetElementPixelProperties(surfaceDescriptionInputs, properties);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
#else
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
#endif

    #if _AlphaClip
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

#ifdef _ALPHAPREMULTIPLY_ON
    surfaceDescription.BaseColor *= surfaceDescription.Alpha;
#endif

    return half4(surfaceDescription.BaseColor, alpha);
}
