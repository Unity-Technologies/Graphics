#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    output = BuildVaryings(input);
    output.normalWS = -GetViewForwardDir();
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

    return NormalsRenderingShared(color, surfaceDescription.NormalTS, unpacked.tangentWS.xyz, bitangent, unpacked.normalWS);
}
