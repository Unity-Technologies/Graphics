#ifndef PHYSICALLY_BASED_SKY_RENDERING_HLSL
#define PHYSICALLY_BASED_SKY_RENDERING_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

float3 _PBRSkyCameraPosPS;
int _RenderSunDisk;

float ComputeMoonPhase(CelestialBodyData moon, float3 V)
{
    float3 M = moon.forward.xyz * moon.distanceFromCamera;

    float radialDistance = moon.distanceFromCamera, rcpRadialDistance = rcp(radialDistance);
    float2 t = IntersectSphere(moon.radius, dot(moon.forward.xyz, -V), radialDistance, rcpRadialDistance);

    float3 N = normalize(M - t.x * V);

    return saturate(-dot(N, moon.sunDirection));
}

float ComputeEarthshine(CelestialBodyData moon)
{
    // Approximate earthshine: sun light reflected from earth
    // cf. A Physically-Based Night Sky Model

    // Compute the percentage of earth surface that is illuminated by the sun as seen from the moon
    //float earthPhase = PI - acos(dot(sun.forward.xyz, -light.forward.xyz));
    //float earthshine = 1.0f - sin(0.5f * earthPhase) * tan(0.5f * earthPhase) * log(rcp(tan(0.25f * earthPhase)));

    // Cheaper approximation of the above (https://www.desmos.com/calculator/11ny6d5j1b)
    float sinPhase = sqrt(max(1 - dot(moon.sunDirection, moon.forward), 0.0f)) * INV_SQRT2;
    float earthshine = 1.0f - sinPhase * sqrt(sinPhase);

    return earthshine * moon.earthshine;
}

float3 RenderSunDisk(inout float tFrag, float tExit, float3 V)
{
    float3 radiance = 0;

    // Intersect and shade emissive celestial bodies.
    // Unfortunately, they don't write depth.
    for (uint i = 0; i < _CelestialBodyCount; i++)
    {
        CelestialBodyData light = _CelestialBodyDatas[i];

        // Celestial body must be outside the atmosphere (request from Pierre D).
        float lightDist = max(light.distanceFromCamera, tExit);

        if (asint(light.angularRadius) != 0 && lightDist < tFrag)
        {
            // We may be able to see the celestial body.
            float3 L = -light.forward.xyz;

            float LdotV    = -dot(L, V);
            float radInner = light.angularRadius;

            if (LdotV >= light.flareCosInner) // Sun disk.
            {
                tFrag = lightDist;
                float3 color = light.surfaceColor;

                if (light.type != 0)
                    color *= ComputeMoonPhase(light, V) * INV_PI + ComputeEarthshine(light); // Lambertian BRDF

                if (light.surfaceTextureScaleOffset.x > 0)
                {
                    float2 proj   = float2(dot(V, light.right), dot(V, light.up));
                    float2 angles = float2(FastASin(proj.x), FastASin(-proj.y));
                    float2 uv = angles * rcp(radInner) * 0.5 + 0.5;
                    color *= SampleCookie2D(uv, light.surfaceTextureScaleOffset);
                }

                radiance = color;
            }
            else if (LdotV >= light.flareCosOuter) // Flare region.
            {
                float rad = acos(LdotV);
                float r   = max(0, rad - radInner);
                float w   = saturate(1 - r * rcp(light.flareSize));

                float3 color = light.flareColor;
                color *= SafePositivePow(w, light.flareFalloff);
                radiance += color;
            }
        }
    }

    return radiance;
}

#endif // PHYSICALLY_BASED_SKY_RENDERING_HLSL
