#ifndef UNITY_DYNAMIC_SCALING_INCLUDED
#define UNITY_DYNAMIC_SCALING_INCLUDED

float2 DynamicScalingApplyScaleBias(float2 xy, float4 dynamicScalingScaleBias)
{
    return dynamicScalingScaleBias.zw + xy * dynamicScalingScaleBias.xy;
}

float2 DynamicScalingRemoveScaleBias(float2 xy, float4 dynamicScalingScaleBias)
{
    return (xy - dynamicScalingScaleBias.zw) / dynamicScalingScaleBias.xy;
}

#define DYNAMIC_SCALING_APPLY_SCALEBIAS(uv)  DynamicScalingApplyScaleBias(uv, _BlitScaleBias)
#define DYNAMIC_SCALING_REMOVE_SCALEBIAS(uv) DynamicScalingRemoveScaleBias(uv, _BlitScaleBias)

#endif // UNITY_DYNAMIC_SCALING_INCLUDED
