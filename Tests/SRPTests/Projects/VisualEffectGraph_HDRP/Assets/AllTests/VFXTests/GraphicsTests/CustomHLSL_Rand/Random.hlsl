//#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl"

void RandomColor(inout VFXAttributes attributes, in VFXGradient color)
{ 
    attributes.color =SampleGradient(color, VFXRAND).xyz;
}

void RandomPosition(inout VFXAttributes attributes, in float3 positionMinRange, in float3 positionMaxRange)
{
    attributes.position = lerp(positionMinRange, positionMaxRange, VFXRAND3);
}

void RandomSize(inout VFXAttributes attributes, in float2 sizeRange)
{
    attributes.size = lerp(sizeRange.x, sizeRange.y, VFXRAND);
}