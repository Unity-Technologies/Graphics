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

float ComputeMoonPhase(DirectionalLightData moon, DirectionalLightData sun, float3 V, float2 uv)
{
    float3 N, L;
    if (moon.bodyType == 1)
    {
        float3 M = moon.forward.xyz * moon.distanceFromCamera;

        float radialDistance = moon.distanceFromCamera, rcpRadialDistance = rcp(radialDistance);
        float radius = tan(0.5 * moon.skyAngularDiameter) * radialDistance;
        float2 t = IntersectSphere(radius, dot(moon.forward.xyz, -V), radialDistance, rcpRadialDistance);

        N = normalize(M - t.x * V);
        L = sun.forward.xyz;
    }
    else
    {
        float2 rotDirX = float2(moon.phaseAngleSinCos.x, -moon.phaseAngleSinCos.y);
        float2 rotDirY = float2(moon.phaseAngleSinCos.y,  moon.phaseAngleSinCos.x);
        uv = float2(dot(rotDirX, uv), dot(rotDirY, uv));

        float d = sqrt(1 - uv.y * uv.y - uv.x * uv.x);
        N = float3(d, uv.x, uv.y);
        L = float3(moon.phaseSinCos.y, 0, moon.phaseSinCos.x);
    }

    return saturate(-dot(N, L));
}

float ComputeEarthshine(DirectionalLightData moon, DirectionalLightData sun)
{
    // Approximate earthshine: sun light reflected from earth
    // cf. A Physically-Based Night Sky Model

    // Compute the percentage of earth surface that is illuminated by the sun as seen from the moon
    //float earthPhase = PI - acos(dot(sun.forward.xyz, -light.forward.xyz));
    //float earthshine = 1.0f - sin(0.5f * earthPhase) * tan(0.5f * earthPhase) * log(rcp(tan(0.25f * earthPhase)));

    // Cheaper approximation of the above (https://www.desmos.com/calculator/11ny6d5j1b)
    float sinPhase = sqrt(dot(sun.forward.xyz, -moon.forward.xyz) + 1) * INV_SQRT2;
    float earthshine = 1.0f - sinPhase * sqrt(sinPhase);

    return earthshine * moon.earthshine;
}

float3 RenderSunDisk(inout float tFrag, float tExit, float3 V)
{
    float3 radiance = 0;

    // Intersect and shade emissive celestial bodies.
    // Unfortunately, they don't write depth.
    for (uint i = 0; i < _DirectionalLightCount; i++)
    {
        DirectionalLightData light = _DirectionalLightDatas[i];

        // Use scalar or integer cores (more efficient).
        bool interactsWithSky = asint(light.distanceFromCamera) >= 0;

        // Celestial body must be outside the atmosphere (request from Pierre D).
        float lightDist = max(light.distanceFromCamera, tExit);

        if (interactsWithSky && asint(light.skyAngularDiameter) != 0 && lightDist < tFrag)
        {
            // We may be able to see the celestial body.
            float3 L = -light.forward.xyz;

            float LdotV    = -dot(L, V);
            float rad      = acos(LdotV);
            float radInner = 0.5 * light.skyAngularDiameter;

            if (LdotV >= light.flareCosOuter)
            {
                // Sun flare is visible. Sun disk may or may not be visible.
                // Assume uniform emission.
                float solidAngle = TWO_PI * (1 - light.flareCosInner);
                float3 color = light.color.rgb * rcp(solidAngle);

                if (LdotV >= light.flareCosInner) // Sun disk.
                {
                    tFrag = lightDist;

                    float2 uv = 0;
                    if (light.bodyType == 2 || light.surfaceTextureScaleOffset.x > 0)
                    {
                        // The cookie code de-normalizes the axes.
                        float2 proj   = float2(dot(-V, normalize(light.right)), dot(-V, normalize(light.up)));
                        float2 angles = float2(FastASin(-proj.x), FastASin(proj.y));
                        uv = angles * rcp(radInner);
                    }

                    uint sunIndex = max(_DirectionalShadowIndex, 0);
                    if ((light.bodyType == 1 && sunIndex != i) || light.bodyType == 2)
                    {
                        DirectionalLightData sun = _DirectionalLightDatas[sunIndex];
                        float earthshine = sunIndex != i ? ComputeEarthshine(light, sun) : 0.0f;
                        float3 sunColor = (sunIndex == i ? rcp(solidAngle) : 1.0f) * sun.color.rgb;;
                        color = (ComputeMoonPhase(light, sun, V, uv) * INV_PI + earthshine) * sunColor;
                    }

                    if (light.surfaceTextureScaleOffset.x > 0)
                    {
                        color *= SampleCookie2D(uv * 0.5 + 0.5, light.surfaceTextureScaleOffset);
                    }

                    color *= light.surfaceTint;
                    radiance = color;
                }
                else // Flare region.
                {
                    float r = max(0, rad - radInner);
                    float w = saturate(1 - r * rcp(light.flareSize));

                    color *= light.flareTint;
                    color *= SafePositivePow(w, light.flareFalloff);
                    radiance += color;
                }

            }
        }
    }

    return radiance;
}

#endif // PHYSICALLY_BASED_SKY_RENDERING_HLSL
