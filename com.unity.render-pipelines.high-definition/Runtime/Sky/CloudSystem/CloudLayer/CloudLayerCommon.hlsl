#ifndef __CLOUDLAYER_COMMON_H__
#define __CLOUDLAYER_COMMON_H__

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalCone.hlsl"

//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VolumetricCloudsUtilities.hlsl"
float HenyeyGreenstein(float cosAngle, float g)
{
    // There is a mistake in the GPU Gem7 Paper, the result should be divided by 1/(4.PI)
    float g2 = g * g;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / PositivePow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
}
float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}
float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

// The number of octaves for the multi-scattering
#define NUM_MULTI_SCATTERING_OCTAVES 2
// Forward eccentricity
#define FORWARD_ECCENTRICITY 0.7
// Forward eccentricity
#define BACKWARD_ECCENTRICITY 0.7

TEXTURE2D_ARRAY(_CloudTexture);
SAMPLER(sampler_CloudTexture);

TEXTURE2D(_FlowmapA);
SAMPLER(sampler_FlowmapA);

TEXTURE2D(_FlowmapB);
SAMPLER(sampler_FlowmapB);

Texture3D<float> _Worley128RGBA;

float4 _FlowmapParam[2];
float4 _ColorFilter[2];
float3 _SunLightColor;
float3 _SunDirection;
float _LowestCloudAltitude;
float _MaxThickness;
float _Rain;

const float _EarthRadius = 6378100.0f;

#define _ScrollDirection(l) _FlowmapParam[l].xy
#define _ScrollFactor(l)    _FlowmapParam[l].z
#define _UpperHemisphere    (_FlowmapParam[0].w != 0.0)
#define _Opacity            _FlowmapParam[1].w

#define _Tint(l)            _ColorFilter[l].xyz
#define _Density(l)         _ColorFilter[l].w

struct CloudLayerData
{
    int index;
    bool distort, use_flowmap;

    TEXTURE2D(flowmap);
    SAMPLER(flowmapSampler);
};


float2 SampleCloudMap(float3 dir, int layer)
{
    float2 coords = GetLatLongCoords(dir, _UpperHemisphere);
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_CloudTexture, sampler_CloudTexture, coords, layer, 0).rg;
}

float3 RotationUp(float3 p, float2 cos_sin)
{
    float3 rotDirX = float3(cos_sin.x, 0, -cos_sin.y);
    float3 rotDirY = float3(cos_sin.y, 0,  cos_sin.x);

    return float3(dot(rotDirX, p), p.y, dot(rotDirY, p));
}

CloudLayerData GetCloudLayer(int index)
{
    CloudLayerData layer;
    layer.index = index;
    layer.distort = false;
    layer.use_flowmap = false;

    if (index == 0)
    {
        #ifdef USE_CLOUD_MOTION
        layer.distort = true;
        #ifdef USE_FLOWMAP
        layer.use_flowmap = true;
        layer.flowmap = _FlowmapA;
        layer.flowmapSampler = sampler_FlowmapA;
        #endif
        #endif
    }
    else
    {
        #ifdef USE_SECOND_CLOUD_MOTION
        layer.distort = true;
        #ifdef USE_SECOND_FLOWMAP
        layer.use_flowmap = true;
        layer.flowmap = _FlowmapB;
        layer.flowmapSampler = sampler_FlowmapB;
        #endif
        #endif
    }

    return layer;
}

void EvaluateSunColorAttenuation(float3 evaluationPointWS, float3 sunDirection, inout float3 sunColor)
{
#ifdef PHYSICALLY_BASED_SUN
    float3 X = evaluationPointWS;
    float3 C = _PlanetCenterPosition.xyz;

    float r        = distance(X, C);
    float cosHoriz = ComputeCosineOfHorizonAngle(r);
    float cosTheta = dot(X - C, sunDirection) * rcp(r); // Normalize

    float3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
    float3 opacity = 1 - TransmittanceFromOpticalDepth(oDepth);
    sunColor *= 1 - (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
#endif
}

int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b*b) - 4.0*a*c;
    result = 0.0;
    int numSolutions = 0;
    if (d >= 0.0)
    {
        // Compute the values required for the solution eval
        float sqrtD = sqrt(d);
        float q = -0.5*(b + FastSign(b) * sqrtD);
        result = float2(c/q, q/a);
        // Remove the solutions we do not want
        numSolutions = 2;
        if (result.x < 0.0)
        {
            numSolutions--;
            result.x = result.y;
        }
        if (result.y < 0.0)
            numSolutions--;
    }
    // Return the number of solutions
    return numSolutions;
}
bool RaySphereIntersection(float3 startWS, float3 dir, float radius)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;
    bool flag = false;
    if (d >= 0.0)
    {
        // Compute the values required for the solution eval
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        float2 result = float2(c/q, q/a);
        flag = result.x > 0.0 || result.y > 0.0;
    }
    return flag;
}
void GetCloudVolumeIntersection(float3 dir, out float rangeStart, out float range)
{
    float _HighestCloudAltitude = _LowestCloudAltitude + _MaxThickness;

    // intersect with all three spheres
    float2 intersectionInter, intersectionOuter;
    int numInterInner = RaySphereIntersection(0, dir, _LowestCloudAltitude + _EarthRadius, intersectionInter);
    int numInterOuter = RaySphereIntersection(0, dir, _HighestCloudAltitude + _EarthRadius, intersectionOuter);

    // The ray starts at the first intersection with the lower bound and goes up to the first intersection with the outer bound
    rangeStart = intersectionInter.x;
    range = intersectionOuter.x - rangeStart;
}
bool GetCloudVolumeIntersection_Light(float3 originWS, float3 dir, out float totalDistance)
{
    float _HighestCloudAltitude = _LowestCloudAltitude + _MaxThickness;

    // Given that this is a light ray, it will always start from inside the volume and is guaranteed to exit
    float2 intersection, intersectionEarth;
    RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersection);
    bool intersectEarth = RaySphereIntersection(originWS, dir, _EarthRadius);
    totalDistance = intersection.x;
    // If the ray intersects the earth, then the sun is occlued by the earth
    return !intersectEarth;
}

float GetDensity(float3 positionWS)
{
    float3 dir = normalize(positionWS);
    float thickness = SampleCloudMap(dir, 0).y;

    float highFrequencyNoise = 1.0 - SAMPLE_TEXTURE3D_LOD(_Worley128RGBA, s_linear_repeat_sampler, positionWS/2000, 0).x;
    thickness = remap(thickness, 0, 1, 0, highFrequencyNoise);

    float rangeStart = _LowestCloudAltitude, range = _MaxThickness;
    GetCloudVolumeIntersection(dir, rangeStart, range);

    float distToCenter = length(dir * (rangeStart + 0.5 * range) - positionWS);
    float density = 1 - saturate(distToCenter / (range * thickness * 0.5));
    density *= _Density(0) * thickness;
    return density;
}

float3 EvaluateSunLuminance(float3 positionWS, float3 sunDirection, float3 sunColor, float density, float powderEffect, float phaseFunction[NUM_MULTI_SCATTERING_OCTAVES], float2 currentCoord)
{
    float _NumLightSteps = 8;
    float3 _ScatteringTint = _Tint(0);
    float sigmaT = lerp(0.04, 0.12, _Rain);
    float _MultiScattering = 0.5f;

    // Compute the Ray to the limits of the cloud volume in the direction of the light
    float totalLightDistance = 2000.0;
    float3 luminance = float3(0.0, 0.0, 0.0);

    if (GetCloudVolumeIntersection_Light(positionWS, sunDirection, totalLightDistance))
    {
        totalLightDistance = clamp(totalLightDistance, 0, 2000);
        // Apply a small bias to compensate for the imprecision in the ray-sphere intersection at world scale.
        totalLightDistance += 5.0f;

        // Initially the transmittance is one for all octaves
        float3 sunLightTransmittance[NUM_MULTI_SCATTERING_OCTAVES];
        int o;
        for (o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
            sunLightTransmittance[o] = 1.0;

        // Compute the size of the current step
        float stepSize = totalLightDistance / (float)_NumLightSteps;

        // Collect total density along light ray.
        //positionWS += sunDirection * stepSize * 0.25;
        for (int j = 0; j < _NumLightSteps; j++)
        {
            // The samples are not linearly distributed along the point-light direction due to their low number. We sample they in a logarithmic way.
            float dist = stepSize * (0.25 + j);

            // Evaluate the current sample point
            float3 currentSamplePointWS = positionWS + sunDirection * dist;

            //// Evaluate the current sample point
            //float pdf;
            //float2 noiseValue = InitRandom(currentCoord * _ScreenSize.zw);
            //SampleSphericalCone(positionWS, stepSize, sunDirection, PI*0.5f,noiseValue.x ,noiseValue.y, 0, 0, positionWS, pdf);
            ////positionWs += dir * stepSize;
            //float3 currentSamplePointWS = positionWS;

            // Get the cloud properties at the sample point
            float density = GetDensity(currentSamplePointWS);

            // Compute the extinction
            const float3 mediaExtinction = max(_ScatteringTint.xyz * density * sigmaT, float3(1e-6, 1e-6, 1e-6));

            // Update the transmittance for every octave
            for (o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
                sunLightTransmittance[o] *= exp(-stepSize * mediaExtinction * PositivePow(_MultiScattering, o));
        }

        // Compute the luminance for each octave
        for (o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
            luminance += sunLightTransmittance[o] * sunColor * powderEffect * phaseFunction[o] * PositivePow(_MultiScattering, o);
    }

    // return the combined luminance
    return luminance;
}

float4 EvaluateCloud(float3 currentPositionWS, float3 dir, float4 scattering_transmittance, float3 lightColor, float stepSize, float phaseFunction[NUM_MULTI_SCATTERING_OCTAVES], float2 positionCS)
{
    //float ambientOcclusion = 0.25f;
    //float3 ambientTermBottom = _AmbientProbeBottom.xyz * GetCurrentExposureMultiplier();
    float _Altitude = _LowestCloudAltitude;
    float _PowderEffectIntensity = 0.25f;
    float3 _ScatteringTint = _Tint(0);
    float sigmaT = lerp(0.04, 0.12, _Rain);
    float cosAngle = dot(dir, _SunDirection);

    float density = GetDensity(currentPositionWS);
    if (density != 0.0f)
    {
        // Apply the extinction
        const float3 mediaExtinction = _ScatteringTint.xyz * density * sigmaT;
        const float currentStepExtinction = exp(-density * sigmaT * stepSize);

        // Compute the powder effect
        float powder_effect = PowderEffect(density, cosAngle, _PowderEffectIntensity);

        // Evaluate the luminance at this sample
        float3 luminance = EvaluateSunLuminance(currentPositionWS, _SunDirection, lightColor, density, powder_effect, phaseFunction, positionCS);
        //luminance += ambientTermBottom * ambientOcclusion;
        luminance *= mediaExtinction;

        // Improved analytical scattering
        const float3 integScatt = (luminance - luminance * currentStepExtinction) / mediaExtinction;
        float3 inScattering = scattering_transmittance.xyz + integScatt * scattering_transmittance.w;
        float transmittance = scattering_transmittance.w * currentStepExtinction;
        scattering_transmittance = float4(inScattering, transmittance);
    }

    return scattering_transmittance;
}

float4 GetCloudLayerColor(float3 dir, int index, float2 positionCS)
{
    float2 color;

    CloudLayerData layer = GetCloudLayer(index);
    if (layer.distort)
    {
        float2 alpha = frac(_ScrollFactor(index) + float2(0.0, 0.5)) - 0.5;
        float3 delta;

        if (layer.use_flowmap)
        {
            float3 tangent = normalize(cross(dir, float3(0.0, 1.0, 0.0)));
            float3 bitangent = cross(tangent, dir);

            float3 windDir = RotationUp(dir, _ScrollDirection(index));
            float2 flow = SAMPLE_TEXTURE2D_LOD(layer.flowmap, layer.flowmapSampler, GetLatLongCoords(windDir, _UpperHemisphere), 0).rg * 2.0 - 1.0;
            delta = flow.x * tangent + flow.y * bitangent;
        }
        else
        {
            float3 windDir = float3(_ScrollDirection(index).x, 0.0f, _ScrollDirection(index).y);
            delta = windDir * sin(dir.y*PI*0.5);
        }

        // Sample twice
        float2 color1 = SampleCloudMap(normalize(dir + alpha.x * delta), index);
        float2 color2 = SampleCloudMap(normalize(dir + alpha.y * delta), index);

        // Blend color samples
        color = lerp(color1, color2, abs(2.0 * alpha.x));
    }
    else
        color = SampleCloudMap(dir, index);

    float3 lightColor = _SunLightColor.xyz;
    float _Altitude = _LowestCloudAltitude;
    float _PowderEffectIntensity = 0.25f;
    float _MultiScattering = 0.5f;
    float3 _ScatteringTint = 1 - 0.75f * float3(0, 0, 0);
    float thickness = color.y;
    float cosAngle = dot(dir, _SunDirection);
    float _NumPrimarySteps = 16;

    float phaseFunction[NUM_MULTI_SCATTERING_OCTAVES];
    for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
    {
        const float forwardP = HenyeyGreenstein(cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, o));
        const float backwardsP = HenyeyGreenstein(cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, o));
        phaseFunction[o] = backwardsP + forwardP;
    }

    float rangeStart, range;
    GetCloudVolumeIntersection(dir, rangeStart, range);
    float3 currentPositionWS = dir * rangeStart;

    EvaluateSunColorAttenuation(currentPositionWS, _SunDirection, lightColor);

    int currentIndex = 0;
    float stepS = range / (float)_NumPrimarySteps;
    float4 scattering_transmittance = float4(0, 0, 0, 1);
    while (currentIndex < _NumPrimarySteps)
    {
        scattering_transmittance = EvaluateCloud(currentPositionWS, dir,
            scattering_transmittance, lightColor, stepS, phaseFunction, positionCS);
        currentPositionWS += dir * stepS;
        currentIndex++;
    }

    //currentPositionWS = dir * rangeStart + (1 - thickness) * range * 0.5;
    //float density = _Density(0);
    //scattering_transmittance.xyz = EvaluateSunLuminance(currentPositionWS, _SunDirection, lightColor, density, 1.0, phaseFunction);
    //scattering_transmittance *= thickness;

    //scattering_transmittance.xyz = lightColor * color.y;
    return float4(scattering_transmittance.xyz, color.y) * _Opacity;
}

float4 RenderClouds(float3 dir, float2 positionCS=0)
{
    float4 clouds = 0;

    if (dir.y >= 0 || !_UpperHemisphere)
    {
#ifndef DISABLE_MAIN_LAYER
        clouds = GetCloudLayerColor(dir, 0, positionCS);
#endif

#ifdef USE_SECOND_CLOUD_LAYER
        float4 cloudsB = GetCloudLayerColor(dir, 1, positionCS);
        // Premultiplied alpha
        clouds = float4(clouds.rgb + (1 - clouds.a) * cloudsB.rgb, clouds.a + cloudsB.a - clouds.a * cloudsB.a);
#endif
    }
    return clouds;
}

// For shadows
float4 RenderClouds(float2 positionCS)
{
    return RenderClouds(-GetSkyViewDirWS(positionCS), positionCS);
}

#endif // __CLOUDLAYER_COMMON_H__
