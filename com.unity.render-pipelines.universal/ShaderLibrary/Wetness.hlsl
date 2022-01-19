#ifndef UNIVERSAL_WETNESS_INCLUDED
#define UNIVERSAL_WETNESS_INCLUDED

static const float kWaterSpec = 0.02;
static const float kWaterLevelMin = 0.5;
static const float kWaterNormalGradientMult = 25.0;

struct WetnessData
{
    float wetness; // pure wetness amount.
    float waterLevel; // Where water starts from wetness level.
    float waterSaturation; // How much surface is satureted with water: 0 (low porosity materials no saturation at all) to 1 (puddle formation starts).
    float waterAmount; // How much watness is actual water on the surface.
    float3 normalWS; // Water surface world space normal
    float occlusion;
};

float3 PerturbNormal(float3 surf_pos, float3 surf_norm, float height, float2 uv, Texture2D<float4> wetnessBuffer, SamplerState wetnessBufferSampler, float depth)
{
    float3 vSigmaS = ddx_fine(surf_pos);
    float3 vSigmaT = ddy_fine(surf_pos);
    if (!any(vSigmaS) || !any(vSigmaT))
        return 0;
    float3 vN = surf_norm; // normalized
    float3 vR1 = cross(vSigmaT, vN);
    float3 vR2 = cross(vN, vSigmaS);
    float fDet = dot(vSigmaS, vR1);

    float gradScale = max(kWaterNormalGradientMult * (1.0 - saturate(depth * 0.1)), 0.1);

    float2 TexDx = ddx_fine(uv) * gradScale;
    float2 TexDy = ddy_fine(uv) * gradScale;

    float2 STl = uv - TexDx;
    float2 STr = uv + TexDx;
    float2 STu = uv - TexDy;
    float2 STb = uv + TexDy;

    float Hl = wetnessBuffer.Sample(wetnessBufferSampler, STl).g;
    float Hr = wetnessBuffer.Sample(wetnessBufferSampler, STr).g;
    float Hu = wetnessBuffer.Sample(wetnessBufferSampler, STu).g;
    float Hb = wetnessBuffer.Sample(wetnessBufferSampler, STb).g;

    float dBs = (Hr - Hl) / (gradScale * 2.0);
    float dBt = (Hu - Hb) / (gradScale * 2.0);

    float3 vSurfGrad = sign(fDet) * (dBs * vR1 + dBt * vR2);
    return normalize(abs(fDet) * vN - vSurfGrad);
}

bool IsSurfaceWet(float4 gbufferWetness)
{
    return gbufferWetness.r > 0.0;
}

WetnessData GetWetnessData(float4 gbufferWetness, BRDFData brdfData, float3 posWS, float3 normalWS, float2 uv, Texture2D<float4> wetnessBuffer, SamplerState wetnessBufferSampler, float depth)
{
    WetnessData wetnessData;

    wetnessData.wetness = gbufferWetness.r;
    wetnessData.waterLevel = kWaterLevelMin;
    wetnessData.waterSaturation = saturate(wetnessData.wetness / (wetnessData.waterLevel + 0.0001));// This should be using porosity
    wetnessData.waterAmount = saturate(wetnessData.wetness - wetnessData.waterLevel);

    float puddleTransition = saturate(wetnessData.waterAmount * 10.0);
    wetnessData.normalWS = normalize(lerp(normalWS, float3(0.0, 1.0, 0.0), puddleTransition));
    float3 waterNormal = PerturbNormal(posWS, wetnessData.normalWS, gbufferWetness.g, uv, wetnessBuffer, wetnessBufferSampler, depth);
    wetnessData.normalWS = normalize(lerp(wetnessData.normalWS, waterNormal, puddleTransition * wetnessData.waterAmount * 2.0));

    wetnessData.occlusion = 1.0;

    return wetnessData;
}

void AllpyWetnessToBRDF(WetnessData wetnessData, inout BRDFData brdfData)
{
    float3 lum = (brdfData.diffuse.r + brdfData.diffuse.g + brdfData.diffuse.b) / 3.0;
    // Saturation
    brdfData.diffuse = brdfData.diffuse + normalize(brdfData.diffuse - lum) * wetnessData.waterSaturation * 0.2;
    // Darkening
    brdfData.diffuse = lerp(brdfData.diffuse, pow(brdfData.diffuse, 3), sqrt(wetnessData.waterSaturation));
}

float3 Fresnel(float3 reflectance, float cosTheta)
{
    return reflectance + (1.0 - reflectance) * pow(1.0 - cosTheta, 5.0);
}

#endif
