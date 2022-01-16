#ifndef UNIVERSAL_WETNESS_INCLUDED
#define UNIVERSAL_WETNESS_INCLUDED

static const float kWaterSpec = 0.02;
static const float kWaterLevelMin = 0.5;

struct WetnessData
{
    float wetness; // pure wetness amount.
    float waterLevel; // Where water starts from wetness level.
    float waterSaturation; // How much surface is satureted with water: 0 (low porosity materials no saturation at all) to 1 (puddle formation starts).
    float waterAmount; // How much watness is actual water on the surface.
    float3 normalWS; // Water surface world space normal
    float occlusion;
};

WetnessData GetWetnessData(float4 wetness, BRDFData brdfData, float3 normalWS)
{
    WetnessData wetnessData;

    wetnessData.wetness = wetness.r;
    wetnessData.waterLevel = kWaterLevelMin;
    wetnessData.waterSaturation = saturate(wetnessData.wetness / (wetnessData.waterLevel + 0.0001));
    wetnessData.waterAmount = saturate(wetnessData.wetness - wetnessData.waterLevel);
    float puddleTransition = saturate(wetnessData.waterAmount * 10.0);
    wetnessData.normalWS = normalize(lerp(normalWS, float3(0.0, 1.0, 0.0), puddleTransition));
    wetnessData.occlusion = 1.0;

    return wetnessData;
}

void AllpyWetnessToBRDF(WetnessData wetnessData, inout BRDFData brdfData)
{
    float3 lum = (brdfData.diffuse.r + brdfData.diffuse.g + brdfData.diffuse.b) / 3.0;
    // Saturation
    brdfData.diffuse = brdfData.diffuse + normalize(brdfData.diffuse - lum) * wetnessData.waterSaturation * 0.20;
    // Darkening
    brdfData.diffuse = lerp(brdfData.diffuse, pow(brdfData.diffuse, 3), sqrt(wetnessData.waterSaturation));
}

float3 Fresnel(float3 reflectance, float cosTheta)
{
    return reflectance + (1.0 - reflectance) * pow(1.0 - cosTheta, 5.0);
}

#endif
