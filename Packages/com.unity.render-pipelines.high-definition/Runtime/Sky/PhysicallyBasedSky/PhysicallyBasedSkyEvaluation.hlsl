#ifndef UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED
#define UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED

#ifdef OUTPUT_MULTISCATTERING
#define _PlanetaryRadius _GroundAlbedo_PlanetRadius.w
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"

TEXTURE2D(_SkyViewLUT);
SAMPLER(sampler_SkyViewLUT);
TEXTURE2D(_MultiScatteringLUT);
TEXTURE3D(_AtmosphericScatteringLUT);

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

float2 MapSkyView(float cosChi, float3 V)
{
    float2 uv;

    float coord = FastACos(cosChi) / HALF_PI;
    uv.y = sqrt(1.0 - coord); // quadratic transformation to preserve details at horizon

    // Convert view vector to spherical coordinates
    float phi = FastACos(normalize(V.xz).x);
    if (V.z < 0.0f) phi = TWO_PI - phi;
    uv.x = phi / TWO_PI;

    return uv;
}

void UnmapSkyView(uint2 coord, out float3 V)
{
    const float2 res = float2(PBRSKYCONFIG_SKY_VIEW_LUT_WIDTH, PBRSKYCONFIG_SKY_VIEW_LUT_HEIGHT);
    const float2 uv = coord / float2(res.x, res.y - 1);

    float remapped = 1.0 - uv.y * uv.y; // quadratic transformation to preserve details at horizon
    float cosChi = saturate(cos(remapped * HALF_PI));
    float sinChi = SinFromCos(cosChi);
    V = float3(sinChi, cosChi, sinChi);

    float phi = TWO_PI * uv.x;
    V.xz *= float2(cos(phi), sin(phi));
}

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

// Might need better heuristic for non-earth planets
// Should roughly represent the longest line of sight you can have trough the atmosphere
// Higher values increse coverage of each slice which decreases quality
#define ATMOSPHERIC_SCATTERING_MAX_DISTANCE 128000.0f

float3 MapAtmosphericScattering(float2 positionNDC, float t, float cosChi)
{
    const float offset = rcp(2 * PBRSKYCONFIG_ATMOSPHERIC_SCATTERING_LUT_DEPTH);

    float r = _PlanetaryRadius + _CameraAltitude;

    // Move to atmosphere entry point
    t -= max(IntersectSphere(_AtmosphericRadius, cosChi, r).x, 0);
    t /= ATMOSPHERIC_SCATTERING_MAX_DISTANCE;

    float s = sqrt(max(t - offset * offset, 0.0f)) - offset; // inverse of GetSample()

    return float3(positionNDC, s);
}

void UnmapAtmosphericScattering(uint s, inout float3 V, out float3 O, out float t, out float dt)
{
    O = GetCameraPositionWS() - _PlanetCenterPosition;

#ifdef CAMERA_SPACE
    V.y = max(V.y, 0.0f); // we can't see below the horizon
#else
    // Make sure camera is not below the ground (offset by 1 for precision)
    if (_CameraAltitude < 1.0f)
        O -= (_CameraAltitude - 1.0f) * _PlanetUp;
    else
    {
        // Move to atmosphere entry point
        float r = _PlanetaryRadius + _CameraAltitude;
        O += max(IntersectSphere(_AtmosphericRadius, dot(_PlanetUp, V), r).x, 0) * V;
    }
#endif

    GetSample(s, PBRSKYCONFIG_ATMOSPHERIC_SCATTERING_LUT_DEPTH, ATMOSPHERIC_SCATTERING_MAX_DISTANCE, t, dt);
}

// Evaluate using LUTs

void EvaluateDistantAtmosphere(float3 V, inout float3 skyColor, inout float3 skyOpacity)
{
    float cosHor = 0.0f;
    float cosChi = V.y;

    float3 optDepth = ComputeAtmosphericOpticalDepth(_PlanetaryRadius, cosChi, true);
    skyOpacity = 1 - TransmittanceFromOpticalDepth(optDepth);

    float2 uv = MapSkyView(cosChi, V);
    skyColor += SAMPLE_TEXTURE2D_LOD(_SkyViewLUT, sampler_SkyViewLUT, uv, 0).xyz * _CelestialLightExposure;

    AtmosphereArtisticOverride(cosHor, cosChi, skyColor, skyOpacity);
}

float3 EvaluateMultipleScattering(float cosChi, float height)
{
    float2 uv = MapMultipleScattering(cosChi, height);
    return SAMPLE_TEXTURE2D_LOD(_MultiScatteringLUT, s_linear_clamp_sampler, uv, 0).rgb;
}

void EvaluateCameraAtmosphericScattering(float3 V, float2 positionNDC, float tFrag, out float3 skyColor, out float3 skyOpacity)
{
    skyColor = skyOpacity = 0.0f;

    float3 O = GetCameraPositionWS() - _PlanetCenterPosition;
    float3 N = _PlanetUp;
    float r = _PlanetaryRadius + _CameraAltitude;

    float cosChi = dot(N, V);
    float cosHor = ComputeCosineOfHorizonAngle(r);

    float3 uvw = MapAtmosphericScattering(positionNDC, tFrag, cosChi);
    skyColor = SAMPLE_TEXTURE3D_LOD(_AtmosphericScatteringLUT, s_linear_clamp_sampler, uvw, 0).rgb;

    float entryPoint = max(IntersectSphere(_AtmosphericRadius, cosChi, tFrag).x, 0);
    tFrag = min(tFrag, entryPoint + ATMOSPHERIC_SCATTERING_MAX_DISTANCE);

    float3 optDepth = ComputeAtmosphericOpticalDepth(O, -V, tFrag);
    skyOpacity = 1 - TransmittanceFromOpticalDepth(optDepth);

    AtmosphereArtisticOverride(cosHor, cosChi, skyColor, skyOpacity, true);
}

#endif // UNITY_PHYSICALLY_BASED_SKY_EVALUATION_INCLUDED
