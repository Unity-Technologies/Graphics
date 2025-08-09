#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

half4 White;

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    output.normalWS = TransformObjectToWorldDir(input.normalOS);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#ifdef UNIVERSAL_USELEGACYSPRITEBLOCKS
    half4 color = surfaceDescription.SpriteColor;
#else
    half4 color = half4(1.0,1.0,1.0, surfaceDescription.Alpha);
#endif

#if ALPHA_CLIP_THRESHOLD
    clip(color.a - surfaceDescription.AlphaClipThreshold);
#endif

    half crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    half3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);

    #if defined(SHADERGRAPH_PREVIEW_MAIN) || defined(SHADERGRAPH_PREVIEW)
        return NormalsRenderingShared(color, surfaceDescription.NormalTS, unpacked.tangentWS.xyz, bitangent, unpacked.normalWS);
    #else
        return White * NormalsRenderingShared(color, surfaceDescription.NormalTS, unpacked.tangentWS.xyz, bitangent, unpacked.normalWS);
    #endif
}
