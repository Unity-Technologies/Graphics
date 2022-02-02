#ifndef UNIVERSAL_WETNESS_INCLUDED
#define UNIVERSAL_WETNESS_INCLUDED

static const float kWaterSpec = 0.02;
static const float kWaterLevelMin = 0.5;
static const float kWaterNormalGradientMult = 25.0;
static const float3 kWaterAbsorbtionColor = float3(150.0, 500.0, 500.0) * 0.5;
static const float kWaterAmountDepthRange = 0.01; // 1 sm is maximum thickness
static const float kNormalScale = 0.5;
static const float kWaveLeveScale = 0.15;
static const float kRefractionMult = 15.0;
static const float kNormalGradScale = 0.5;
static const float kSurfWaterGradScale = 0.5; // 0.1

struct WetnessData
{
    float wetness; // pure wetness amount.
    float porosity;
    float waveLevel;
    float puddleTransition;
    float waterLevel; // Where water starts from wetness level.
    float waterSaturation; // How much surface is satureted with water: 0 (low porosity materials no saturation at all) to 1 (puddle formation starts).
    float waterAmount; // How much watness is actual water on the surface.
    float3 normalWS; // Water surface world space normal
    float3 color;
    float occlusion;
};

float3 PerturbNormal(float3 surf_pos, float3 surf_norm, float porosity, float2 uv, Texture2D<float4> wetnessBuffer, SamplerState wetnessBufferSampler, float depth)
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

    float2 Hl = wetnessBuffer.Sample(wetnessBufferSampler, STl).rg;
    float2 Hr = wetnessBuffer.Sample(wetnessBufferSampler, STr).rg;
    float2 Hu = wetnessBuffer.Sample(wetnessBufferSampler, STu).rg;
    float2 Hb = wetnessBuffer.Sample(wetnessBufferSampler, STb).rg;

    Hl.r = pow(Hl.r, 2.0);
    Hr.r = pow(Hr.r, 2.0);
    Hu.r = pow(Hu.r, 2.0);
    Hb.r = pow(Hb.r, 2.0);

    float2 dBs = (Hl - Hr) / (gradScale * 2.0);
    float2 dBt = (Hu - Hb) / (gradScale * 2.0);

    float3 vSurfGradWaves = sign(fDet) * (dBs.g * vR1 + dBt.g * vR2) * kNormalGradScale;
    float3 vSurfGradWater = sign(fDet) * (dBs.r * vR1 + dBt.r * vR2) * kSurfWaterGradScale;

    return normalize(abs(fDet) * vN + vSurfGradWaves + vSurfGradWater * saturate(pow((1.0 - porosity), 5.0)));
}

bool IsSurfaceWet(float4 gbufferWetness)
{
    return gbufferWetness.r > 0.0;
}

WetnessData GetWetnessData(float4 gbufferWetness, float smoothness, float3 posWS, float3 normalWS, float2 uv, Texture2D<float4> wetnessBuffer, SamplerState wetnessBufferSampler, float depth)
{
    WetnessData wetnessData;
    wetnessData.wetness = pow(gbufferWetness.r, 2.0);
    wetnessData.porosity = 1.0 - smoothness;
    wetnessData.waveLevel = (gbufferWetness.g * 2.0 - 1.0) * kWaveLeveScale * wetnessData.wetness;
    wetnessData.waterLevel = max(kWaterLevelMin - (1.0 - wetnessData.porosity) * kWaterLevelMin, 0.0);
    wetnessData.waterSaturation = saturate(wetnessData.wetness / (wetnessData.waterLevel + 0.0001));
    wetnessData.waterAmount = saturate(wetnessData.wetness - wetnessData.waterLevel + wetnessData.waveLevel) * gbufferWetness.b;
    wetnessData.puddleTransition = saturate(wetnessData.waterAmount * 7.5);
    wetnessData.normalWS = normalize(lerp(normalWS, float3(0.0, 1.0, 0.0), wetnessData.puddleTransition));
    float3 waterNormal = PerturbNormal(posWS, wetnessData.normalWS, wetnessData.porosity, uv, wetnessBuffer, wetnessBufferSampler, depth);
    wetnessData.normalWS = normalize(lerp(wetnessData.normalWS, waterNormal, wetnessData.puddleTransition * wetnessData.waterAmount * kNormalScale));
    wetnessData.occlusion = 1.0;
    wetnessData.color = kWaterAbsorbtionColor * gbufferWetness.a;
    return wetnessData;
}

void ApplyWetnessToBRDF(WetnessData wetnessData, inout BRDFData brdfData)
{
    if (dot(brdfData.diffuse, brdfData.diffuse) > 0)
    {
        float3 luma = dot(brdfData.diffuse, float3(0.2126729, 0.7151522, 0.0721750));
        float3 saturation = luma + (wetnessData.waterSaturation * 4.0 + 1.0) * (brdfData.diffuse - luma);

        // Saturation
        brdfData.diffuse = lerp(brdfData.diffuse, saturation, wetnessData.porosity);
        // Darkening
        brdfData.diffuse = lerp(brdfData.diffuse, lerp(brdfData.diffuse, pow(brdfData.diffuse, 3), sqrt(wetnessData.waterSaturation)), sqrt(wetnessData.porosity));
    }
}

void ComputeWetnessAbsorbtionAttenuation(WetnessData wetnessData, float3 viewDirectionWS, float3 lightDirWS, out float3 lightAtten, out float3 giAtten)
{
    float3 normal = float3(0.0, 1.0, 0.0);
    float lDotN = max(saturate(dot(lightDirWS, normal)), 0.0001);
    float vDotN = max(saturate(dot(viewDirectionWS, normal)), 0.0001);

    float waterDepth = (wetnessData.waterAmount + wetnessData.waterSaturation) * kWaterAmountDepthRange;

    float totalLightDistance = (waterDepth / lDotN) + (waterDepth / vDotN);
    float totalGiDistance = waterDepth + (waterDepth / vDotN);

    lightAtten = exp(-totalLightDistance * wetnessData.color);
    giAtten = exp(-totalGiDistance * wetnessData.color);
}

float3 Fresnel(float3 reflectance, float cosTheta)
{
    return reflectance + (1.0 - reflectance) * pow(1.0 - cosTheta, 5.0);
}

float FresnelSchlickTIR(float nt, float ni, float3 n, float3 i)
{
    float R0 = (nt - ni) / (nt + ni);
    R0 *= R0;
    float CosX = dot(n, i);
    if (ni > nt)
    {
        float inv_eta = ni / nt;
        float SinT2 = inv_eta * inv_eta * (1.0f - CosX * CosX);
        if (SinT2 > 1.0f)
        {
            return 1.0f; // TIR
        }
        CosX = sqrt(1.0f - SinT2);
    }

    return R0 + (1.0f - R0) * pow(1.0 - CosX, 5.0);
}

float3 Refract(float3 i, float3 n, float inv_eta)
{
    float cosi = dot(-i, n);
    float cost2 = 1.0f - inv_eta * inv_eta * (1.0f - cosi * cosi);
    float3 t = inv_eta * i + ((inv_eta * cosi - sqrt(abs(cost2))) * n);
    return t * (float3)(cost2 > 0);
}

void ComputeRefractedUV(WetnessData wetnessData, float3 posWS, float3 normalWS, float3 viewDirectionWS, float4x4 worldToClip, out float3 posRefrWS, out float2 uvRefr)
{
    float waterDepth = wetnessData.waterAmount * kWaterAmountDepthRange * kRefractionMult;

    float3 posWater = posWS + viewDirectionWS * (waterDepth / max(dot(float3(0.0, 1.0, 0.0), viewDirectionWS), 0.0001));
    float3 viewRefr = normalize(Refract(-viewDirectionWS, wetnessData.normalWS, 1.0 / 1.33));
    posRefrWS = posWater + viewRefr * (waterDepth / max(dot(float3(0.0, -1.0, 0.0), viewRefr), 0.0001));

    float3 screenUV = mul(worldToClip, float4(posRefrWS, 1.0)).xyw;
//#if UNITY_UV_STARTS_AT_TOP
//    screenUV.xy = screenUV.xy * float2(0.5, -0.5) + 0.5 * screenUV.z;
//#else
    screenUV.xy = screenUV.xy * 0.5 + 0.5 * screenUV.z;
//#endif

    uvRefr = screenUV.xy / screenUV.z;
}

#endif
