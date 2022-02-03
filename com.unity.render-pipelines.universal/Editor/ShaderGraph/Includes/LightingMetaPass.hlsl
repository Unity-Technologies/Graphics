#ifndef SG_LIT_META_INCLUDED
#define SG_LIT_META_INCLUDED

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = surfaceDescription.BaseColor;
    metaInput.Emission = surfaceDescription.Emission;
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = unpacked.texCoord1.xy;
    metaInput.LightCoord = unpacked.texCoord2;
#endif

    return UnityMetaFragment(metaInput);
}

#endif
