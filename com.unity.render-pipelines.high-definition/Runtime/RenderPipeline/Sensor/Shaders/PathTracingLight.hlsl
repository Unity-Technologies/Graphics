#ifndef UNITY_SENSOR_LIGHT_INCLUDED
#define UNITY_SENSOR_LIGHT_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"

/*
struct LightData
{
    float3 positionRWS;
    uint lightLayers;
    float lightDimmer;
    float volumetricLightDimmer;
    real angleScale;
    real angleOffset;
    float3 forward;
    int lightType;
    float3 right;
    real range;
    float3 up;
    float rangeAttenuationScale;
    float3 color;
    float rangeAttenuationBias;
    int cookieIndex;
    int tileCookie;
    int shadowIndex;
    int contactShadowMask;
    float3 shadowTint;
    float shadowDimmer;
    float volumetricShadowDimmer;
    int nonLightMappedOnly;
    real minRoughness;
    int screenSpaceShadowIndex;
    real4 shadowMaskSelector;
    real4 size;
    float diffuseDimmer;
    float specularDimmer;
    float isRayTracedContactShadow;
    float penumbraTint;
    float3 padding;
    float boxLightSafeExtent;
};
*/

bool SampleBeam(
    LightData lightData,
    float3 position,
    float3 normal,
    out float3 outgoingDir,
    out float3 value,
    out float pdf,
    out float dist,
    inout PathIntersection payload)
{
    const float MM_TO_M = 1e-3;
    const float M_TO_MM = 1e3;

    float3 lightDirection = payload.beamDirection;
    float3 lightPosition = payload.beamOrigin;

    outgoingDir = position - lightPosition;
    dist = length(outgoingDir);
    outgoingDir /= dist;

    float apertureRadius = lightData.size.x;
    float w0 = lightData.size.y;
    float zr = lightData.size.z;
    float distToWaist = lightData.size.w;

    // get the hit point in the coordinate frame of the laser as a depth(z) and
    // radial measure(r)
    float ctheta = dot(lightDirection, outgoingDir);
    float zFromAperture = ctheta * dist;
    float rSq = Sq(dist) - Sq(zFromAperture);
    float3 radialDirection = dist * outgoingDir - zFromAperture*lightDirection;

    float zFromWaist = abs(zFromAperture * M_TO_MM - distToWaist);
    if (dot(normal, -outgoingDir) < 0.001)
        return false;

    // Total beam power, note: different from the output here which is irradiance.
    float P = lightData.color.x;

    const float zRatio = Sq(zFromWaist / zr);
    const float wz = w0 * sqrt(1 + zRatio) * MM_TO_M;
    const float Eoz = 2 * P;
    const float wzSq = wz*wz;

    float gaussianFactor = exp(-2 * rSq / wzSq) / (PI * wzSq); // 1/m^2
    value = gaussianFactor * Eoz; // W/m^2

    payload.beamRadius = wz;
    payload.beamDepth = zFromAperture;

#if 0 /*Debug values*/
    payload.diffuseColor = float3(ctheta, zFromAperture, rSq);
    payload.fresnel0 = float3(distToWaist, w0, zr);
    payload.transmittance = float3(zFromWaist, zRatio, wzSq);
    payload.tangentWS = float3(Eoz, wzSq, gaussianFactor);
#endif

    // sampling a point in the "virtual" aperture
    // Find the actual point in the beam aperture that corresponds to this point
    float rRatio = apertureRadius / wz;
    float3 pAperture = lightPosition + rRatio * radialDirection; // location of the point in the aperture

    outgoingDir = pAperture - position; // corrected outgoing vector using the assumption below
    dist = length(outgoingDir);
    outgoingDir /= dist;

    // assumption that the interaction point is only illuminated by
    // one point in the laser aperture.
    pdf = 1.0f;

    return any(value);
}


#endif // UNITY_SENSOR_LIGHT_INCLUDED
