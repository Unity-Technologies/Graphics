#ifndef UNITY_COMMON_SHADOW_INCLUDED
#define UNITY_COMMON_SHADOW_INCLUDED

// Ref: https://mynameismjp.wordpress.com/2015/02/18/shadow-sample-update/
// Calculates the offset to use for sampling the shadow map, based on the surface normal
float3 GetShadowPosOffset(float NdotL, float3 normalWS, float2 invShadowMapSize)
{
    float texelSize = 2.0 * invShadowMapSize.x;
    float offsetScaleNormalize = saturate(1.0 - NdotL);
    // return texelSize * OffsetScale * offsetScaleNormalize * normalWS;
    return texelSize * offsetScaleNormalize * normalWS;
}

#endif // UNITY_COMMON_SHADOW_INCLUDED
