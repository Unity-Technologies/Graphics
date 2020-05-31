//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SPHERICALHARMONICS_CS_HLSL
#define SPHERICALHARMONICS_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.SphericalHarmonicsL0
// PackingRules = Exact
struct SphericalHarmonicsL0
{
    float3 shrgb;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.SphericalHarmonicsL0
//
float3 GetShrgb(SphericalHarmonicsL0 value)
{
    return value.shrgb;
}

// Generated from UnityEngine.Rendering.HighDefinition.SphericalHarmonicsL1
// PackingRules = Exact
struct SphericalHarmonicsL1
{
    float4 shAr;
    float4 shAg;
    float4 shAb;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.SphericalHarmonicsL1
//
float4 GetShAr(SphericalHarmonicsL1 value)
{
    return value.shAr;
}
float4 GetShAg(SphericalHarmonicsL1 value)
{
    return value.shAg;
}
float4 GetShAb(SphericalHarmonicsL1 value)
{
    return value.shAb;
}

#endif
