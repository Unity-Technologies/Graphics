#ifndef UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED
#define UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"

TEXTURE2D(_MultiScatteringLUT);

// Integration utilities

float3 IntegrateOverSegment(float3 S, float3 transmittanceOverSegment, float3 transmittance, float3 sigmaE)
{
    // https://www.shadertoy.com/view/XlBSRz

    // See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite
    // Assumes homogeneous medium along the interval

    float3 Sint = (S - S * transmittanceOverSegment) / sigmaE;    // integrate along the current step segment
    return transmittance * Sint; // accumulate and also take into account the transmittance from previous steps
}

void GetSample(uint s, uint sampleCount, float tExit, out float t, out float dt)
{
    //dt = tMax / sampleCount;
    //t += dt;

    float t0 = (s) / (float)sampleCount;
    float t1 = (s + 1.0f) / (float)sampleCount;

    // Non linear distribution of sample within the range.
    t0 = t0 * t0 * tExit;
    t1 = t1 * t1 * tExit;

    t = lerp(t0, t1, 0.5f); // 0.5 gives the closest result to reference
    dt = t1 - t0;
}

// LUT uv convertion utilities

float2 MapMultipleScattering(float cosChi, float height)
{
    return saturate(float2(cosChi*0.5f + 0.5f, height / _AtmosphericDepth));
}

void UnmapMultipleScattering(uint2 coord, out float cosChi, out float height)
{
    const float2 res = float2(PBRSKYCONFIG_MULTI_SCATTERING_LUT_WIDTH, PBRSKYCONFIG_MULTI_SCATTERING_LUT_HEIGHT);
    const float2 uv = coord / (res - 1);

    cosChi = uv.x * 2.0 - 1.0;
    height = lerp(_PlanetaryRadius, _AtmosphericRadius, uv.y);
}

// Evaluate using LUTs

float3 EvaluateMultipleScattering(float cosChi, float height)
{
    float2 uv = MapMultipleScattering(cosChi, height);
    return SAMPLE_TEXTURE2D_LOD(_MultiScatteringLUT, s_linear_clamp_sampler, uv, 0).rgb;
}

#endif // UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED
