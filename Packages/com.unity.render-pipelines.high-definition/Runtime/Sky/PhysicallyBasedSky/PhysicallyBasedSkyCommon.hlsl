#ifndef UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED
#define UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/ShaderVariablesPhysicallyBasedSky.cs.hlsl"

TEXTURE2D(_GroundIrradianceTexture);

// Emulate a 4D texture with a "deep" 3D texture.
TEXTURE3D(_AirSingleScatteringTexture);
TEXTURE3D(_AerosolSingleScatteringTexture);
TEXTURE3D(_MultipleScatteringTexture);

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
    SAMPLER(s_linear_clamp_sampler);
#endif

#define _PlanetCenterPosition _PlanetCenterRadius.xyz // camera relative
#define _GroundAlbedo _GroundAlbedo_PlanetRadius.xyz
#define _PlanetUp _PlanetUpAltitude.xyz
#define _CameraAltitude _PlanetUpAltitude.w

#ifndef _PlanetaryRadius
#define _PlanetaryRadius _PlanetCenterRadius.w
#endif

// To reduce banding at low sun angles on 32bits, we have to 'pre expose' ms values as they are very small
#define MS_EXPOSURE 100.0f
#define MS_EXPOSURE_INV 0.01f

// Computes (a^2 - b^2) in a numerically stable way.
float DifferenceOfSquares(float a, float b)
{
    return (a - b) * (a + b);
}

float3 AirScatter(float height)
{
    return _AirSeaLevelScattering.rgb * exp(-height * _AirDensityFalloff);
}

float AirPhase(float LdotV)
{
    return RayleighPhaseFunction(-LdotV);
}

float3 AerosolScatter(float height)
{
    return _AerosolSeaLevelScattering.rgb * exp(-height * _AerosolDensityFalloff);
}

float AerosolPhase(float LdotV)
{
    return _AerosolPhasePartConstant * CornetteShanksPhasePartVarying(_AerosolAnisotropy, -LdotV);
}

float OzoneDensity(float height)
{
    return saturate(1 - abs(height * _OzoneScaleOffset.x + _OzoneScaleOffset.y));
}

float3 AtmosphereExtinction(float height)
{
    const float densityMie      = exp(-height * _AerosolDensityFalloff);
    const float densityRayleigh = exp(-height * _AirDensityFalloff);
    const float densityOzone    = OzoneDensity(height);

    float3 extinction = densityMie * _AerosolSeaLevelExtinction
                      + densityRayleigh * _AirSeaLevelExtinction.xyz
                      + densityOzone * _OzoneSeaLevelExtinction.xyz;

    return max(extinction, FLT_MIN);
}

// For multiple scattering.
// Assume that, after multiple bounces, the effect of anisotropy is lost.
float3 AtmospherePhaseScatter(float LdotV, float height)
{
    return AirPhase(LdotV) * (AirScatter(height) + AerosolScatter(height));
}

// Returns the closest hit in X and the farthest hit in Y.
// Returns a negative number if there's no intersection.
// (result.y >= 0) indicates success.
// (result.x < 0) indicates that we are inside the sphere.
float2 IntersectSphere(float sphereRadius, float cosChi,
                       float radialDistance, float rcpRadialDistance)
{
    // r_o = float2(0, r)
    // r_d = float2(sinChi, cosChi)
    // p_s = r_o + t * r_d
    //
    // R^2 = dot(r_o + t * r_d, r_o + t * r_d)
    // R^2 = ((r_o + t * r_d).x)^2 + ((r_o + t * r_d).y)^2
    // R^2 = t^2 + 2 * dot(r_o, r_d) + dot(r_o, r_o)
    //
    // t^2 + 2 * dot(r_o, r_d) + dot(r_o, r_o) - R^2 = 0
    //
    // Solve: t^2 + (2 * b) * t + c = 0, where
    // b = r * cosChi,
    // c = r^2 - R^2.
    //
    // t = (-2 * b + sqrt((2 * b)^2 - 4 * c)) / 2
    // t = -b + sqrt(b^2 - c)
    // t = -b + sqrt((r * cosChi)^2 - (r^2 - R^2))
    // t = -b + r * sqrt((cosChi)^2 - 1 + (R/r)^2)
    // t = -b + r * sqrt(d)
    // t = r * (-cosChi + sqrt(d))
    //
    // Why do we do this? Because it is more numerically robust.

    float d = Sq(sphereRadius * rcpRadialDistance) - saturate(1 - cosChi * cosChi);

    // Return the value of 'd' for debugging purposes.
    return (d < 0) ? d : (radialDistance * float2(-cosChi - sqrt(d),
                                                  -cosChi + sqrt(d)));
}

// TODO: remove.
float2 IntersectSphere(float sphereRadius, float cosChi, float radialDistance)
{
    return IntersectSphere(sphereRadius, cosChi, radialDistance, rcp(radialDistance));
}

// O must be planet-relative.
float2 IntersectAtmosphere(float3 O, float3 V, out float3 N, out float r)
{
    const float A = _AtmosphericRadius;

    float3 P = O;

    N = normalize(P);
    r = length(P);

    float2 t = IntersectSphere(A, dot(N, -V), r);

    if (t.y >= 0) // Success?
    {
        // If we are already inside, do not step back.
        t.x = max(t.x, 0);

        if (t.x > 0)
        {
            P = P + t.x * -V;
            N = normalize(P);
            r = A;
        }
    }

    return t;
}

float2 IntersectRayCylinder(float3 cylAxis, float cylRadius,
                            float  radialDistance, float3 rayDir)
{
    // rayOrigin = {0, 0, r}.
    float r = radialDistance;
    float x = dot(cylAxis, rayDir);

    // Solve: t^2 + 2 * (b / a) * t + (c / a) = 0.
    float a = saturate(1.0 - x * x);
    float b = rcp(a) * (rayDir.z - x * cylAxis.z);
    float c = rcp(a) * (saturate(1 - cylAxis.z * cylAxis.z) - Sq(cylRadius * rcp(r)));
    float d = b * b - c;

    return ((abs(a) < FLT_EPS) || (d < 0)) ? -1 : r * float2(-b - sqrt(d),
                                                             -b + sqrt(d));
}

float MapQuadraticHeight(float height)
{
    // TODO: we should adjust sub-texel coordinates
    // to account for the non-linear height distribution.
    return sqrt(height * _RcpAtmosphericDepth);
}

// Returns the height.
float UnmapQuadraticHeight(float v)
{
    return (v * v) * _AtmosphericDepth;
}

float ComputeCosineOfHorizonAngle(float r)
{
    float R      = _PlanetaryRadius;
    float sinHor = R * rcp(r);
    return -sqrt(saturate(1 - sinHor * sinHor));
}

// We use the parametrization from "Outdoor Light Scattering Sample Update" by E. Yusov.
float2 MapAerialPerspective(float cosChi, float height, float texelSize)
{
    float R      = _PlanetaryRadius;
    float r      = height + R;
    float cosHor = ComputeCosineOfHorizonAngle(r);

    // Above horizon?
    float s = FastSign(cosChi - cosHor);

    // float x = (cosChi - cosHor) * rcp(1 - s * cosHor); // in [-1, 1]
    // float m = pow(abs(x), 0.5);
    float m = sqrt(abs(cosChi - cosHor)) * rsqrt(1 - s * cosHor);

    // Lighting must be discontinuous across the horizon.
    // Thus, we offset by half a texel to avoid interpolation artifacts.
    m = s * max(m, texelSize);

    float u = saturate(m * 0.5 + 0.5);
    float v = MapQuadraticHeight(height);

    return float2(u, v);
}

float2 MapAerialPerspectiveAboveHorizon(float cosChi, float height)
{
    float R      = _PlanetaryRadius;
    float r      = height + R;
    float cosHor = ComputeCosineOfHorizonAngle(r);

    float u = saturate(sqrt(cosChi - cosHor) * rsqrt(1 - cosHor));
    float v = MapQuadraticHeight(height);

    return float2(u, v);
}

// returns {cosChi, height}.
float2 UnmapAerialPerspective(float2 uv)
{
    float height = UnmapQuadraticHeight(uv.y);
    float R      = _PlanetaryRadius;
    float r      = height + R;
    float cosHor = ComputeCosineOfHorizonAngle(r);

    float m = uv.x * 2 - 1;
    float s = FastSign(m);
    float x = s * (m * m);

    float cosChi = x * (1 - s * cosHor) + cosHor;

    cosChi += s * FLT_EPS; // Avoid the (cosChi == cosHor) case due to the FP arithmetic

    return float2(cosChi, height);
}

float2 UnmapAerialPerspectiveAboveHorizon(float2 uv)
{
    float height = UnmapQuadraticHeight(uv.y);
    float R      = _PlanetaryRadius;
    float r      = height + R;
    float cosHor = ComputeCosineOfHorizonAngle(r);

    float x = (uv.x * uv.x);

    float cosChi = x * (1 - cosHor) + cosHor;

    cosChi += FLT_EPS; // Avoid the (cosChi == cosHor) case due to the FP arithmetic

    return float2(cosChi, height);
}

float ChapmanUpperApprox(float z, float cosTheta)
{
    float c = cosTheta;
    float n = 0.761643 * ((1 + 2 * z) - (c * c * z));
    float d = c * z + sqrt(z * (1.47721 + 0.273828 * (c * c * z)));

    return 0.5 * c + (n * rcp(d));
}

float ChapmanHorizontal(float z)
{
    float r = rsqrt(z);
    float s = z * r; // sqrt(z)

    return 0.626657 * (r + 2 * s);
}

// z = (n * r), Z = (n * R).
float RescaledChapmanFunction(float z, float Z, float cosTheta)
{
    float sinTheta = sqrt(saturate(1 - cosTheta * cosTheta));

    // cos(Pi - theta) = -cos(theta).
    float ch = ChapmanUpperApprox(z, abs(cosTheta)) * exp(Z - z); // Rescaling adds 'exp'

    if (cosTheta < 0)
    {
        // z_0 = n * r_0 = (n * r) * sin(theta) = z * sin(theta).
        // Ch(z, theta) = 2 * exp(z - z_0) * Ch(z_0, Pi/2) - Ch(z, Pi - theta).
        float z_0 = z * sinTheta;
        float a = 2 * ChapmanHorizontal(z_0);
        float b = exp(Z - z_0); // Rescaling cancels out 'z' and adds 'Z'
        float ch_2 = a * b;

        ch = ch_2 - ch;
    }

    return ch;
}

// This is a very crude approximation, should be reworked
// It estimates the result by integrating with 4 samples
float ComputeOzoneOpticalDepth(float r, float cosTheta, float distAlongRay)
{
    const float  R = _PlanetaryRadius;

    float2 tInner = IntersectSphere(_OzoneLayerStart, cosTheta, r);
    float2 tOuter = IntersectSphere(_OzoneLayerEnd, cosTheta, r);
    float tEntry, tEntry2, tExit, tExit2;

    if (tInner.x < 0.0 && tInner.y >= 0.0) // Below the lower bound
    {
        // The ray starts at the intersection with the lower bound and ends at the intersection with the outer bound
        tEntry = tInner.y;
        tExit2 = tOuter.y;
        tEntry2 = tExit = (tExit2 - tEntry) * 0.5f;
    }
    else // Inside or above the volume
    {
        // The ray starts at the intersection with the outer bound, or at 0 if we are inside
        // The ray ends at the lower bound if we hit it, at the outer bound otherwise
        tEntry = max(tOuter.x, 0.0f);
        tExit = tInner.x >= 0.0 ? tInner.x : tOuter.y;

        // If we hit the lower bound, we may intersect the volume a second time
        if (tInner.x >= 0.0 && distAlongRay > tInner.y)
        {
            tEntry2 = tInner.y;
            tExit2 = tOuter.y;
        }
        else
        {
            tExit2 = tExit;
            tEntry2 = tExit = (tExit2 - tEntry) * 0.5f;
        }
    }

    tExit = min(tExit, distAlongRay);
    tExit2 = min(tExit2, distAlongRay);

    float ozoneOD = 0.0f;
    const uint count = 2;
    float dt = max(tExit-tEntry, 0) * rcp(count);
    float dt2 = max(tExit2-tEntry2, 0) * rcp(count);

    [unroll]
    for (uint i = 0; i < count; i++)
    {
        float t = lerp(tEntry, tExit, (i+0.5f) * rcp(count));
        float t2 = lerp(tEntry2, tExit2, (i+0.5f) * rcp(count));
        float h = sqrt(r*r + t * (2*r*cosTheta + t)) - R;
        float h2 = sqrt(r*r + t2 * (2*r*cosTheta + t2)) - R;

        ozoneOD += OzoneDensity(h) * dt;
        ozoneOD += OzoneDensity(h2) * dt2;
    }

    return ozoneOD * 0.6f;
}

float3 ComputeAtmosphericOpticalDepth(float r, float cosTheta, bool aboveHorizon)
{
    const float2 n = float2(_AirDensityFalloff, _AerosolDensityFalloff);
    const float2 H = float2(_AirScaleHeight,    _AerosolScaleHeight);
    const float  R = _PlanetaryRadius;

    float2 z = n * r;
    float2 Z = n * R;

    float sinTheta = sqrt(saturate(1 - cosTheta * cosTheta));

    float2 ch;
    ch.x = ChapmanUpperApprox(z.x, abs(cosTheta)) * exp(Z.x - z.x); // Rescaling adds 'exp'
    ch.y = ChapmanUpperApprox(z.y, abs(cosTheta)) * exp(Z.y - z.y); // Rescaling adds 'exp'

    if (!aboveHorizon) // Below horizon, intersect sphere
    {
        float sinGamma = (r / R) * sinTheta;
        float cosGamma = sqrt(saturate(1 - sinGamma * sinGamma));

        float2 ch_2;
        ch_2.x = ChapmanUpperApprox(Z.x, cosGamma); // No need to rescale
        ch_2.y = ChapmanUpperApprox(Z.y, cosGamma); // No need to rescale

        ch = ch_2 - ch;
    }
    else if (cosTheta < 0)   // Above horizon, lower hemisphere
    {
        // z_0 = n * r_0 = (n * r) * sin(theta) = z * sin(theta).
        // Ch(z, theta) = 2 * exp(z - z_0) * Ch(z_0, Pi/2) - Ch(z, Pi - theta).
        float2 z_0  = z * sinTheta;
        float2 b    = exp(Z - z_0); // Rescaling cancels out 'z' and adds 'Z'
        float2 a;
        a.x         = 2 * ChapmanHorizontal(z_0.x);
        a.y         = 2 * ChapmanHorizontal(z_0.y);
        float2 ch_2 = a * b;

        ch = ch_2 - ch;
    }

    float ozone = aboveHorizon ? ComputeOzoneOpticalDepth(r, cosTheta, FLT_MAX) : 0.0f;
    float3 optDepth = float3(ch * H, ozone);

    return optDepth.x * _AirSeaLevelExtinction.xyz
        + optDepth.y * _AerosolSeaLevelExtinction
        + optDepth.z * _OzoneSeaLevelExtinction.xyz;
}

float3 ComputeAtmosphericOpticalDepth1(float r, float cosTheta)
{
    float cosHor = ComputeCosineOfHorizonAngle(r);

    return ComputeAtmosphericOpticalDepth(r, cosTheta, cosTheta >= cosHor);
}

// Assumes the ray starts and ends inside atmosphere
// O is in planet space
float3 ComputeAtmosphericOpticalDepth(float3 O, float3 V, float distAlongRay)
{
    const float  R = _PlanetaryRadius;
    const float2 n = float2(_AirDensityFalloff, _AerosolDensityFalloff);
    const float2 H = float2(_AirScaleHeight,    _AerosolScaleHeight);

    const float  tFrag = distAlongRay;

    float3 N = normalize(O);
    float r = length(O);

    float NdotV  = dot(N, V);
    float cosChi = -NdotV;

    float2 Z = R * n;
    float r0 = r, cosChi0 = cosChi;

    float r1 = 0, cosChi1 = 0;
    float3 N1 = 0;

    {
        float3 P1 = O + tFrag * -V;

        r1      = length(P1);
        N1      = P1 * rcp(r1);
        cosChi1 = dot(P1, -V) * rcp(r1);

        // Potential swap.
        cosChi0 = (cosChi1 >= 0) ? cosChi0 : -cosChi0;
    }

    float2 ch0, ch1 = 0;

    {
        float2 z0 = r0 * n;

        ch0.x = RescaledChapmanFunction(z0.x, Z.x, cosChi0);
        ch0.y = RescaledChapmanFunction(z0.y, Z.y, cosChi0);
    }

    {
        float2 z1 = r1 * n;

        ch1.x = ChapmanUpperApprox(z1.x, abs(cosChi1)) * exp(Z.x - z1.x);
        ch1.y = ChapmanUpperApprox(z1.y, abs(cosChi1)) * exp(Z.y - z1.y);
    }

    // We may have swapped X and Y.
    float2 ch = abs(ch0 - ch1);
    float3 optDepth = float3(ch * H, ComputeOzoneOpticalDepth(r, cosChi, distAlongRay));

    return optDepth.x * _AirSeaLevelExtinction.xyz
        + optDepth.y * _AerosolSeaLevelExtinction
        + optDepth.z * _OzoneSeaLevelExtinction.xyz;
}

// Evaluates transmittance to sun from a point at altitude r
// cosTheta is the zenith angle
float3 EvaluateSunColorAttenuation(float cosTheta, float r)
{
    float cosHoriz = ComputeCosineOfHorizonAngle(r);

    if (cosTheta >= cosHoriz) // Above horizon
    {
        float3 opticalDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
        return TransmittanceFromOpticalDepth(opticalDepth);
    }
    else
    {
       return 0;
    }
}

// This function evaluates the sun color attenuation from the physically based sky
float3 EvaluateSunColorAttenuation(float3 positionPS, float3 sunDirection, bool estimatePenumbra = false)
{
    float r        = length(positionPS);
    float cosTheta = dot(positionPS, sunDirection) * rcp(r); // Normalize

    // Point can be below horizon due to precision issues
    r = max(r, _PlanetaryRadius);
    float cosHoriz = ComputeCosineOfHorizonAngle(r);

    if (cosTheta >= cosHoriz) // Above horizon
    {
        float3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
        float3 opacity = 1 - TransmittanceFromOpticalDepth(oDepth);
        float penumbra = saturate((cosTheta - cosHoriz) / 0.0019f); // very scientific value
        float3 attenuation = 1 - (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
        return estimatePenumbra ? attenuation * penumbra : attenuation;
    }
    else
    {
       return 0;
    }
}

// Map: [cos(120 deg), 1] -> [0, 1].
// Allocate more samples around (Pi/2).
float MapCosineOfZenithAngle(float NdotL)
{
    float x = max(NdotL, -0.5);
    float s = CopySign(sqrt(abs(x)), x);     // [-0.70710678, 1]
    return saturate(0.585786 * s + 0.414214);
}

// Map: [0, 1] -> [-0.1975, 1].
float UnmapCosineOfZenithAngle(float u)
{
    float s = 1.70711 * u - 0.707107;
    return CopySign(s * s, s);
}

float3 SampleGroundIrradianceTexture(float NdotL)
{
    float2 uv = float2(MapCosineOfZenithAngle(NdotL), 0);

    return SAMPLE_TEXTURE2D_LOD(_GroundIrradianceTexture, s_linear_clamp_sampler, uv, 0).rgb;
}

struct TexCoord4D
{
    float u, v, w0, w1, a;
};

TexCoord4D ConvertPositionAndOrientationToTexCoords(float height, float NdotV, float NdotL, float phiL)
{
    const uint zTexSize = PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_Z;
    const uint zTexCnt  = PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_W;

    float cosChi = -NdotV;

    float u = MapAerialPerspective(cosChi, height, rcp(PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_X)).x;
    float v = MapAerialPerspective(cosChi, height, rcp(PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_X)).y;
    float w = (0.5 + (INV_PI * phiL) * (zTexSize - 1)) * rcp(zTexSize); // [0.5 / zts, 1 - 0.5 / zts]
    float k = MapCosineOfZenithAngle(NdotL) * (zTexCnt - 1);            // [0, ztc - 1]

    TexCoord4D texCoord;

    texCoord.u  = u;
    texCoord.v  = v;

    // Emulate a 4D texture with a "deep" 3D texture.
    texCoord.w0 = (floor(k) + w) * rcp(zTexCnt);
    texCoord.w1 = (ceil(k)  + w) * rcp(zTexCnt);
    texCoord.a  = frac(k);

    return texCoord;
}

float3 ExpLerp(float3 A, float3 B, float t, float x, float y)
{
    // Remap t: (exp(10 k t) - 1) / (exp(10 k) - 1) = exp(x t) y - y.
    t = exp(x * t) * y - y;
    // Perform linear interpolation using the new value of t.
    return lerp(A, B, t);
}

void AtmosphereArtisticOverride(float cosHor, float cosChi, inout float3 skyColor, inout float3 skyOpacity, bool precomputedColorDesaturate = false)
{
    if (!precomputedColorDesaturate)
        skyColor = Desaturate(skyColor, _ColorSaturation);
    skyOpacity = Desaturate(skyOpacity, _AlphaSaturation) * _AlphaMultiplier;

    float horAngle = acos(cosHor);
    float chiAngle = acos(cosChi);

    // [start, end] -> [0, 1] : (x - start) / (end - start) = x * rcpLength - (start * rcpLength)
    // TEMPLATE_3_REAL(Remap01, x, rcpLength, startTimesRcpLength, return saturate(x * rcpLength - startTimesRcpLength))
    float start    = horAngle;
    float end      = 0;
    float rcpLen   = rcp(end - start);
    float nrmAngle = Remap01(chiAngle, rcpLen, start * rcpLen);
    // float angle = saturate((0.5 * PI) - acos(cosChi) * rcp(0.5 * PI));

    skyColor *= ExpLerp(_HorizonTint.rgb, _ZenithTint.rgb, nrmAngle, _HorizonZenithShiftPower, _HorizonZenithShiftScale);
}

#endif // UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED
