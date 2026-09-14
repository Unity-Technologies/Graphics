#ifndef SG_SHADOW_PASS_INCLUDED
#define SG_SHADOW_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

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

    #if defined(_ALPHATEST_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    LODFadeCrossFade(unpacked.positionCS);

    return 0;
}

#endif
