#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PbrSky/PbrSkyRenderer.cs.hlsl"

CBUFFER_START(UnityPbrSky)
    // All the distance-related entries use km and 1/km units.
    float  _PlanetaryRadius;
    float  _RcpPlanetaryRadius;
    float  _AtmosphericDepth;
    float  _RcpAtmosphericDepth;

    float  _AtmosphericRadius;
    float  _AerosolAnisotropy;
    float  _AerosolPhasePartConstant;
    float  _Unused;

    float  _AirDensityFalloff;
    float  _AirScaleHeight;
    float  _AerosolDensityFalloff;
    float  _AerosolScaleHeight;

    float3 _AirSeaLevelExtinction;
    float  _AerosolSeaLevelExtinction;

    float3 _AirSeaLevelScattering;
    float  _AerosolSeaLevelScattering;

    float3 _GroundAlbedo;

    float3 _PlanetCenterPosition; // Not used during the precomputation, but needed to apply the atmospheric effect
CBUFFER_END

TEXTURE2D(_OpticalDepthTexture);
TEXTURE2D(_GroundIrradianceTexture);

// Emulate a 4D texture with a "deep" 3D texture.
TEXTURE3D(_AirSingleScatteringTexture);
TEXTURE3D(_AerosolSingleScatteringTexture);
TEXTURE3D(_MultipleScatteringTexture);

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
    SAMPLER(s_linear_clamp_sampler);
#endif

// Computes (a^2 - b^2) in a numerically stable way.
float DifferenceOfSquares(float a, float b)
{
    return (a - b) * (a + b);
}

float3 AirScatter(float height)
{
    return _AirSeaLevelScattering * exp(-height * _AirDensityFalloff);
}

float AirPhase(float LdotV)
{
    return RayleighPhaseFunction(-LdotV);
}

float AerosolScatter(float height)
{
    return _AerosolSeaLevelScattering * exp(-height * _AerosolDensityFalloff);
}

float AerosolPhase(float LdotV)
{
    return _AerosolPhasePartConstant * CornetteShanksPhasePartVarying(_AerosolAnisotropy, -LdotV);
}

// AerosolPhase / AirPhase.
float AerosolToAirPhaseRatio(float LdotV)
{
    float k = 3 / (16 * PI);
    return _AerosolPhasePartConstant * rcp(k) * CornetteShanksPhasePartAsymmetrical(_AerosolAnisotropy, -LdotV);
}

float3 AtmospherePhaseScatter(float LdotV, float height)
{
    return AirPhase(LdotV) * AirScatter(height) + AerosolPhase(LdotV) * AerosolScatter(height);
}

// Returns the closest hit in X and the farthest hit in Y.
// Returns a negative number if there's no intersection.
float2 IntersectSphere(float sphereRadius, float cosChi, float radialDistance)
{
    float r = radialDistance;

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
    // b = -r * cosChi,
    // c = r^2 - R^2.
    //
    // t = (-2 * b + sqrt((2 * b)^2 - 4 * c)) / 2
    // t = -b + sqrt(b^2 - c)
    // t = -b + sqrt((r * cosChi)^2 + R^2 - r^2)
    // t = -b + r * sqrt((cosChi)^2 + (R/r)^2 - 1)
    // t = -b + r * sqrt(d)
    // t = r * (-cosChi + sqrt(d))
    //
    // Why do we do this? Because it is more numerically robust.

    float d = Sq(sphereRadius * rcp(r)) - saturate(1 - cosChi * cosChi);

    // Return the value of 'd' for debugging purposes.
    return (d < 0) ? d : (r * float2(-cosChi - sqrt(d),
                                     -cosChi + sqrt(d)));
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

float GetCosineOfHorizonZenithAngle(float height)
{
    float R = _PlanetaryRadius;
    float h = height;
    float r = R + h;

    // cos(Pi - x) = -cos(x).
    // Compute -sqrt(r^2 - R^2) / r = -sqrt(1 - (R / r)^2).
    return -sqrt(saturate(1 - Sq(R * rcp(r))));
}

// We use the parametrization from "Outdoor Light Scattering Sample Update" by E. Yusov.
float2 MapAerialPerspective(float cosChi, float height, float texelSize)
{
    float cosHor = GetCosineOfHorizonZenithAngle(height);

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
    float cosHor = GetCosineOfHorizonZenithAngle(height);

    float u = saturate(sqrt(cosChi - cosHor) * rsqrt(1 - cosHor));
    float v = MapQuadraticHeight(height);

    return float2(u, v);
}

// returns {cosChi, height}.
float2 UnmapAerialPerspective(float2 uv)
{
    float height = UnmapQuadraticHeight(uv.y);
    float cosHor = GetCosineOfHorizonZenithAngle(height);

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
    float cosHor = GetCosineOfHorizonZenithAngle(height);

    float x = (uv.x * uv.x);

    float cosChi = x * (1 - cosHor) + cosHor;

    cosChi += FLT_EPS; // Avoid the (cosChi == cosHor) case due to the FP arithmetic

    return float2(cosChi, height);
}

float3 SampleOpticalDepthTexture(float cosChi, float height, bool lookAboveHorizon)
{
    // TODO: pass the sign? Do not recompute?
    float s = lookAboveHorizon ? 1 : -1;

    // From the current position to the atmospheric boundary.
    float2 uv       = MapAerialPerspectiveAboveHorizon(s * cosChi, height).xy;
    float2 optDepth = SAMPLE_TEXTURE2D_LOD(_OpticalDepthTexture, s_linear_clamp_sampler, uv, 0).xy;

    if (!lookAboveHorizon)
    {
        // Direction points below the horizon.
        // What we want to know is transmittance from the sea level to our current position.
        // Therefore, first, we must flip the direction and perform the look-up from the ground.
        // The direction must be parametrized w.r.t. the normal of the intersection point.
        // This value corresponds to transmittance from the sea level to the atmospheric boundary.
        // If we perform a look-up from the current position (using the reversed direction),
        // we can compute transmittance from the current position to the atmospheric boundary.
        // Taking the difference will give us the desired value.

        float R    = _PlanetaryRadius;
        float rcpR = _RcpPlanetaryRadius;
        float h    = height;
        float r    = R + h;

        float cosAlpha = -cosChi;
        float sinAlpha = SinFromCos(cosAlpha);
        float sinTheta = sinAlpha * (r * rcpR);
        float cosTheta = sqrt(saturate(1 - sinTheta * sinTheta));

        // From the sea level to the atmospheric boundary -
        // from the current position to the atmospheric boundary.
        uv       = MapAerialPerspectiveAboveHorizon(cosTheta, 0).xy;
        optDepth = SAMPLE_TEXTURE2D_LOD(_OpticalDepthTexture, s_linear_clamp_sampler, uv, 0).xy
                 - optDepth;
    }

    // Compose the optical depth with extinction at the sea level.
    return optDepth.x * _AirSeaLevelExtinction + optDepth.y * _AerosolSeaLevelExtinction;
}

float3 SampleOpticalDepthTexture(float cosChi, float height)
{
    float cosHor           = GetCosineOfHorizonZenithAngle(height);
    bool  lookAboveHorizon = (cosChi > cosHor);

    return SampleOpticalDepthTexture(cosChi, height, lookAboveHorizon);
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

// O must be planet-relative.
float IntersectAtmosphere(float3 O, float3 V, out float3 N, out float r)
{
    const float A = _AtmosphericRadius;
    const float R = _PlanetaryRadius;

    float3 P = O;

    N = normalize(P);
    r = max(length(P), R); // Must not be inside the planet

    float t;

    if (r <= A)
    {
        // We are inside the atmosphere.
        t = 0;
    }
    else
    {
        // We are observing the planet from space.
        t = IntersectSphere(A, dot(N, -V), r).x; // Min root

        if (t >= 0)
        {
            // It's in the view.
            P = P + t * -V;
            N = normalize(P);
            r = A;
        }
    }

    return t;
}
