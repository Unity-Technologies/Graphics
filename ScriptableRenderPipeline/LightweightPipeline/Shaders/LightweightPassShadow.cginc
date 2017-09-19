#ifndef LIGHTWEIGHT_PASS_SHADOW_INCLUDED
#define LIGHTWEIGHT_PASS_SHADOW_INCLUDED

float4 ShadowPassVertex(float4 pos : POSITION) : SV_POSITION
{
    float4 clipPos = UnityObjectToClipPos(pos);
#if defined(UNITY_REVERSED_Z)
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return clipPos;
}

half4 ShadowPassFragment() : SV_TARGET
{
    return 0;
}

#endif
