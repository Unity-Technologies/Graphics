#ifndef __CLOUDLAYER_COMMON_H__
#define __CLOUDLAYER_COMMON_H__

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"


TEXTURE2D_ARRAY(_CloudTexture);
SAMPLER(sampler_CloudTexture);

TEXTURE2D(_FlowmapA);
SAMPLER(sampler_FlowmapA);

TEXTURE2D(_FlowmapB);
SAMPLER(sampler_FlowmapB);

float4 _FlowmapParam[2];
float4 _Params1[2];
float4 _Params2[2];

#define _ScrollDirection(l) _FlowmapParam[l].xy
#define _ScrollFactor(l)    _FlowmapParam[l].z
#define _UpperHemisphere    (_FlowmapParam[0].w != 0.0)
#define _Coverage           _FlowmapParam[1].w

#define _SunDirection       _Params1[0].xyz
#define _SunLightColor(l)   _Params2[l].xyz
#define _Altitude(l)        _Params2[l].w


struct CloudLayerData
{
    int index;
    bool distort, use_flowmap;

    TEXTURE2D(flowmap);
    SAMPLER(flowmapSampler);
};

float GetCloudVolumeIntersection(int index, float3 dir)
{
    const float _EarthRadius = 6378100.0f;
    return -IntersectSphere(_Altitude(index) + _EarthRadius, -dir.y, _EarthRadius).x;
}

float2 SampleCloudMap(float3 dir, int layer, float coverage)
{
    float2 coords = GetLatLongCoords(dir, _UpperHemisphere);
    float2 cloud = SAMPLE_TEXTURE2D_ARRAY_LOD(_CloudTexture, sampler_CloudTexture, coords, layer, 0).rg;

    cloud.y = saturate((cloud.y - 1) / coverage + 1);

    return cloud;
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

float4 GetCloudLayerColor(float3 dir, int index)
{
    float2 cloud;

    float3 lightColor = _SunLightColor(index);
    EvaluateSunColorAttenuation(dir * _Altitude(index), _SunDirection, lightColor);

    CloudLayerData layer = GetCloudLayer(index);
    if (layer.distort)
    {
        float rangeStart, range;
        float scrollDist = 2 * _Altitude(index); // max horizontal distance clouds can travel, arbitrary but looks good
        float3 position = dir * GetCloudVolumeIntersection(index, dir);

        float2 alpha = frac(_ScrollFactor(index) / scrollDist + float2(0.0, 0.5)) - 0.5;
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
            delta = float3(_ScrollDirection(index).x, 0.0f, _ScrollDirection(index).y);

        float coverage1 = abs(2.0 * alpha.x), coverage2 = 1 - coverage1;
        float2 cloud1 = SampleCloudMap(normalize(position + alpha.x * delta * scrollDist), index, (1-coverage1*coverage1) * _Coverage);
        float2 cloud2 = SampleCloudMap(normalize(position + alpha.y * delta * scrollDist), index, (1-coverage2*coverage2) * _Coverage);

        cloud = cloud1 + cloud2 * (1-cloud1.y); // blend the two samples as if the second is behind the first one
        cloud.x = lerp(cloud1, cloud2, abs(2.0 * alpha.x)).x;
    }
    else
        cloud = SampleCloudMap(dir, index, _Coverage);

    return float4(cloud.x * cloud.y * lightColor, cloud.y);
}

float4 RenderClouds(float3 dir)
{
    float4 clouds = 0;

    if (dir.y >= 0 || !_UpperHemisphere)
    {
#ifndef DISABLE_MAIN_LAYER
        clouds = GetCloudLayerColor(dir, 0);
#endif

#ifdef USE_SECOND_CLOUD_LAYER
        float4 cloudsB = GetCloudLayerColor(dir, 1);
        clouds += cloudsB * (1-clouds.a);
#endif
    }
    return clouds;
}

// For shadows
float4 RenderClouds(float2 positionCS)
{
    return RenderClouds(-GetSkyViewDirWS(positionCS));
}

#endif // __CLOUDLAYER_COMMON_H__
