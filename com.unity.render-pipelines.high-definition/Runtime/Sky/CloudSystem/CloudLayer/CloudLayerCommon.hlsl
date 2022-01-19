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
float4 _ColorFilter[2];
float4 _SunDensity[2];
float3 _SunDirection;
float3 _AmbientProbe;

#define _ScrollDirection(l) _FlowmapParam[l].xy
#define _ScrollFactor(l)    _FlowmapParam[l].z
#define _UpperHemisphere    (_FlowmapParam[0].w != 0.0)
#define _Opacity            _FlowmapParam[1].w

#define _SunLightColor(l)   _ColorFilter[l].xyz
#define _Altitude(l)        _ColorFilter[l].w


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

    float3 lightColor = _SunLightColor(index);
    EvaluateSunColorAttenuation(dir*_Altitude(index), _SunDirection, lightColor);

    return float4(lightColor * color.x + _AmbientProbe, color.y) * _Opacity;
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
