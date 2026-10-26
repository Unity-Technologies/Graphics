#ifndef UNIVERSAL_SHADOW_SAMPLING_DATA_INCLUDED
#define UNIVERSAL_SHADOW_SAMPLING_DATA_INCLUDED

struct ShadowSamplingData
{
    half4 shadowOffset0;
    half4 shadowOffset1;
    float4 shadowmapSize;
    half softShadowQuality;
};

#endif // UNIVERSAL_SHADOW_SAMPLING_DATA_INCLUDED
