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

#if _AlphaClip
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
#endif

    half alpha = 1;
    #if _SURFACE_TYPE_TRANSPARENT
        alpha = surfaceDescription.Alpha;
    #endif

#ifdef _ALPHAPREMULTIPLY_ON
    surfaceDescription.BaseColor *= surfaceDescription.Alpha;
#endif

    return half4(surfaceDescription.BaseColor, alpha);
}
