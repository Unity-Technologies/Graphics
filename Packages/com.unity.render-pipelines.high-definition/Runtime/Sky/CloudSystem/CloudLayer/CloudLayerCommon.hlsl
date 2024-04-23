#ifndef __CLOUDLAYER_COMMON_H__
#define __CLOUDLAYER_COMMON_H__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"


TEXTURE2D_ARRAY(_CloudTexture);
SAMPLER(sampler_CloudTexture);

TEXTURE2D(_FlowmapA);
SAMPLER(sampler_FlowmapA);

TEXTURE2D(_FlowmapB);
SAMPLER(sampler_FlowmapB);

float4 _FlowmapParam[2];
float4 _Params1[2];
float4 _Params2; // zw ununsed
float3 _SunDirection;

StructuredBuffer<float4> _AmbientProbeBuffer;

#define _ScrollDirection(l) _FlowmapParam[l].xy
#define _ScrollFactor(l)    _FlowmapParam[l].z
#define _UpperHemisphere    (_FlowmapParam[0].w != 0.0)
#define _Opacity            _FlowmapParam[1].w

#define _SunLightColor(l)   _Params1[l].xyz
#define _Altitude(l)        _Params1[l].w
#define _AmbientDimmer(l)   _Params2[l]

#define _LowestAltitude(l)  (_Altitude(l) + _PlanetaryRadius)

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

CloudLayerData GetCloudLayer(int index)
{
    CloudLayerData layer;
    layer.index = index;
    layer.distort = false;
    layer.use_flowmap = false;

    if (index == 0)
    {
        #ifndef LAYER1_STATIC
        layer.distort = true;
        #ifdef LAYER1_FLOWMAP
        layer.use_flowmap = true;
        layer.flowmap = _FlowmapA;
        layer.flowmapSampler = sampler_FlowmapA;
        #endif
        #endif
    }
    else
    {
        #ifndef LAYER2_STATIC
        layer.distort = true;
        #ifdef LAYER2_FLOWMAP
        layer.use_flowmap = true;
        layer.flowmap = _FlowmapB;
        layer.flowmapSampler = sampler_FlowmapB;
        #endif
        #endif
    }

    return layer;
}

float4 GetCloudLayerColor(float3 dir, int index)
{
    float2 cloud;
    float3 position = dir * IntersectSphere(_LowestAltitude(index), dir.y, _PlanetaryRadius).y;

    CloudLayerData layer = GetCloudLayer(index);
    if (layer.distort)
    {
        float scrollDist = 2 * _Altitude(index); // max horizontal distance clouds can travel, arbitrary but looks good

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
        float2 cloud1 = SampleCloudMap(normalize(position + alpha.x * delta * scrollDist), index);
        float2 cloud2 = SampleCloudMap(normalize(position + alpha.y * delta * scrollDist), index);
        cloud = lerp(cloud1, cloud2, abs(2.0 * alpha.x));
    }
    else
        cloud = SampleCloudMap(dir, index);

    float3 lightColor = _SunLightColor(index);
    float3 ambient = max(SampleSH9(_AmbientProbeBuffer, float3(0, -1, 0)), 0) * _AmbientDimmer(index);

    #ifdef PHYSICALLY_BASED_SUN
    float3 positionPS = position + float3(0,_PlanetaryRadius,0);
    lightColor *= EvaluateSunColorAttenuation(positionPS, _SunDirection, true);
    #endif

    return float4(cloud.x * lightColor + ambient * cloud.y, cloud.y) * _Opacity;
}

float4 RenderClouds(float3 dir)
{
    float4 clouds = 0;

    if (dir.y >= 0 || !_UpperHemisphere)
    {
#ifndef LAYER1_OFF
        clouds = GetCloudLayerColor(dir, 0);
#endif

#ifndef LAYER2_OFF
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
