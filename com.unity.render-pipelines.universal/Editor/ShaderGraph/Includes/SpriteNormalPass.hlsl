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

    // Fields required by feature blocks are not currently generated
    // unless the corresponding data block is present
    // Therefore we need to predefine all potential data values.
    // Required fields should be tracked properly and generated.
    half3 baseColor = half3(1, 1, 1);
    half alpha = 1;
    half3 normal = half3(0, 0, 1);

    #if defined(FEATURES_GRAPH_PIXEL)
        SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
        SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

        // Data is overriden if the corresponding data block is present.
        // Could use "$Tag.Field: value = surfaceDescription.Field" pattern
        // to avoid preprocessors if this was a template file.
        #ifdef SURFACEDESCRIPTION_BASECOLOR
            baseColor = surfaceDescription.BaseColor;
        #endif
        #ifdef SURFACEDESCRIPTION_ALPHA
            alpha = surfaceDescription.Alpha;
        #endif
        #ifdef SURFACEDESCRIPTION_NORMAL
            normal = surfaceDescription.Normal;
        #endif
    #endif

    float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
    half4 color = half4(baseColor, alpha);
    return NormalsRenderingShared(color, normal, unpacked.tangentWS.xyz, bitangent, unpacked.normalWS);
}
