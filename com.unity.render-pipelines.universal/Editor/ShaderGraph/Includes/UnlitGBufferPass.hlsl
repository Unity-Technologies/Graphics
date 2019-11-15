PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

FragmentOutput frag(PackedVaryings packedInput)
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#if _AlphaClip
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
#endif

#ifdef _ALPHAPREMULTIPLY_ON
    surfaceDescription.Color *= surfaceDescription.Alpha;
#endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.alpha = surfaceDescription.Alpha;

    InputData inputData = (InputData)0;
    inputData.normalWS = half3(0, 1, 0); // need some default to avoid division by 0.

    return SurfaceDataToGbuffer(surfaceData, inputData, surfaceDescription.Color, kLightingInvalid);
}
