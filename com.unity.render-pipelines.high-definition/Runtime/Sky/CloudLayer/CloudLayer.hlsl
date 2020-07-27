#ifndef __CLOUDLAYER_H__
#define __CLOUDLAYER_H__

TEXTURE2D_ARRAY(_CloudTexture);
SAMPLER(sampler_CloudTexture);
    
TEXTURE2D(_CloudFlowmap1);
SAMPLER(sampler_CloudFlowmap1);
    
TEXTURE2D(_CloudFlowmap2);
SAMPLER(sampler_CloudFlowmap2);

float4 _CloudParams1; // x opacity, y upper hemisphere only
float4 _CloudParams2[2]; // For each layer: x intensity, y scroll factor, zw scroll direction (cosPhi and sinPhi)

#define USE_CLOUD_LAYER         (defined(USE_CLOUD_MAP) || defined(USE_CLOUD_MOTION))
#define USE_SECOND_CLOUD_LAYER  (defined(USE_SECOND_CLOUD_MAP) || defined(USE_SECOND_CLOUD_MOTION))

#define _CloudOpacity           _CloudParams1.x
#define _CloudUpperHemisphere   (_CloudParams1.y != 0.0)

#define _CloudIntensity(l)          _CloudParams2[l].x
#define _CloudScrollFactor(l)       _CloudParams2[l].y
#define _CloudScrollDirection(l)    _CloudParams2[l].zw

struct CloudLayerData
{
    int index;
    bool distort, use_flowmap;

    TEXTURE2D(flowmap);
    SAMPLER(flowmapSampler);
};


float4 SampleCloudMap(float3 dir, int layer)
{
    float2 coords = GetLatLongCoords(dir, _CloudUpperHemisphere);
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_CloudTexture, sampler_CloudTexture, coords, layer, 0);
}

float3 CloudRotationUp(float3 p, float2 cos_sin)
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
        #if USE_CLOUD_MOTION
        layer.distort = true;
        #ifdef USE_CLOUD_MAP
        layer.use_flowmap = true;
        layer.flowmap = _CloudFlowmap1;
        layer.flowmapSampler = sampler_CloudFlowmap1;
        #endif
        #endif
    }
    else
    {
        #if USE_SECOND_CLOUD_MOTION
        layer.distort = true;
        #ifdef USE_SECOND_CLOUD_MAP
        layer.use_flowmap = true;
        layer.flowmap = _CloudFlowmap2;
        layer.flowmapSampler = sampler_CloudFlowmap2;
        #endif
        #endif
    }

    return layer;
}

float4 GetCloudLayerColor(float3 dir, int index)
{
    float4 color;

    CloudLayerData layer = GetCloudLayer(index);
    if (layer.distort)
    {
        float2 alpha = frac(_CloudScrollFactor(layer.index) + float2(0.0, 0.5)) - 0.5;
        float3 delta;

        if (layer.use_flowmap)
        {
            float3 tangent = normalize(cross(dir, float3(0.0, 1.0, 0.0)));
            float3 bitangent = cross(tangent, dir);

            float3 windDir = CloudRotationUp(dir, _CloudScrollDirection(layer.index));
            float2 flow = SAMPLE_TEXTURE2D_LOD(layer.flowmap, layer.flowmapSampler, GetLatLongCoords(windDir, _CloudUpperHemisphere), 0).rg * 2.0 - 1.0;
            delta = flow.x * tangent + flow.y * bitangent;
        }
        else
        {
            float3 windDir = CloudRotationUp(float3(0, 0, 1), _CloudScrollDirection(layer.index));
            windDir.x *= -1.0;
            delta = windDir * sin(dir.y*PI*0.5);
        }

        // Sample twice
        float4 color1 = SampleCloudMap(normalize(dir - alpha.x * delta), layer.index);
        float4 color2 = SampleCloudMap(normalize(dir - alpha.y * delta), layer.index);

        // Blend color samples
        color = lerp(color1, color2, abs(2.0 * alpha.x));
    }
    else
        color = SampleCloudMap(dir, layer.index);

    color.rgb *= _CloudIntensity(layer.index);
    color.a *= _CloudOpacity;
    return color;
}

float GetCloudOpacity(float3 dir)
{
    float opacity = 0;

#if USE_CLOUD_LAYER
    if (dir.y >= 0 || !_CloudUpperHemisphere)
    {
        //opacity = GetDistordedCloudColor(dir, 0).a;
#if USE_SECOND_CLOUD_LAYER
            //opacity *= (1.0 - GetDistordedCloudColor(dir, 1).a);
#endif
    }
#endif

    return opacity * _CloudOpacity;
}

float3 ApplyCloudLayer(float3 dir, float3 sky)
{
#if USE_CLOUD_LAYER
    if (dir.y >= 0 || !_CloudUpperHemisphere)
    {
#if USE_SECOND_CLOUD_LAYER
        float4 cloudsB = GetCloudLayerColor(dir, 1);
        sky = lerp(sky, cloudsB.rgb, cloudsB.a);
#endif

        float4 cloudsA = GetCloudLayerColor(dir, 0);
        sky = lerp(sky, cloudsA.rgb, cloudsA.a);
    }
#endif

    return sky;
}

#endif // __CLOUDLAYER_H__
