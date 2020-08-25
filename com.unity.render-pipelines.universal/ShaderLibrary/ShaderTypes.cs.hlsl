//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERTYPES_CS_HLSL
#define SHADERTYPES_CS_HLSL
// Generated from UnityEngine.Rendering.Universal.ShaderInput+LightData
// PackingRules = Exact
struct LightData
{
    float4 position;
    float4 color;
    float4 attenuation;
    float4 spotDirection;
    float4 occlusionProbeChannels;
};

// Generated from UnityEngine.Rendering.Universal.ShaderInput+ShadowData
// PackingRules = Exact
struct ShadowData
{
    float4x4 worldToShadowMatrix;
    float4 shadowParams;
};


#endif
