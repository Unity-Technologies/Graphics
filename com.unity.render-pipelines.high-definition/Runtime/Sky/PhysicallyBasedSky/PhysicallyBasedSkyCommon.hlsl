#ifndef UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED
#define UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyRenderer.cs.hlsl"

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

float3 AerosolScatter(float height)
{
    return _AerosolSeaLevelScattering * exp(-height * _AerosolDensityFalloff);
}

float AerosolPhase(float LdotV)
{
    return _AerosolPhasePartConstant * CornetteShanksPhasePartVarying(_AerosolAnisotropy, -LdotV);
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

    float2 optDepth = ch * H;

    return optDepth.x * _AirSeaLevelExtinction + optDepth.y * _AerosolSeaLevelExtinction;
}

float3 ComputeAtmosphericOpticalDepth1(float r, float cosTheta)
{
    float cosHor = ComputeCosineOfHorizonAngle(r);

    return ComputeAtmosphericOpticalDepth(r, cosTheta, cosTheta >= cosHor);
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

#endif // UNITY_PHYSICALLY_BASED_SKY_COMMON_INCLUDED
