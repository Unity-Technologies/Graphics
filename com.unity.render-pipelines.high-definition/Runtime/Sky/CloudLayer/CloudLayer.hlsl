#ifndef __CLOUDLAYER_H__
#define __CLOUDLAYER_H__

TEXTURE2D_ARRAY(_CloudTexture);
SAMPLER(sampler_CloudTexture);
    
TEXTURE2D(_CloudFlowmap);
SAMPLER(sampler_CloudFlowmap);

float4 _CloudParams1; // x upper hemisphere only / rotation, y scroll factor, zw scroll direction (cosPhi and sinPhi)
float4 _CloudParams2; // xyz tint, w intensity

#define _CloudOpacity           _CloudParams1.x
#define _CloudUpperHemisphere   _CloudParams1.y
#define _CloudDoubleLayer       (_CloudParams1.z != 0.0)
#define _CloudIntensity         _CloudParams2.x
#define _CloudScrollFactor      _CloudParams2.y
#define _CloudScrollDirection   _CloudParams2.zw

#define USE_CLOUD_LAYER         defined(USE_CLOUD_MAP) || (!defined(USE_CLOUD_MAP) && defined(USE_CLOUD_MOTION))

float4 sampleCloud(float3 dir, int layer)
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

float4 GetDistordedCloudColor(float3 dir, int layer)
{
#if USE_CLOUD_MOTION
    float2 alpha = frac(float2(_CloudScrollFactor, _CloudScrollFactor + 0.5)) - 0.5;

#ifdef USE_CLOUD_MAP
    float3 tangent = normalize(cross(dir, float3(0.0, 1.0, 0.0)));
    float3 bitangent = cross(tangent, dir);

    float3 windDir = CloudRotationUp(dir, _CloudScrollDirection);
    float2 flow = SAMPLE_TEXTURE2D_LOD(_CloudFlowmap, sampler_CloudFlowmap, GetLatLongCoords(windDir, _CloudUpperHemisphere), 0).rg * 2.0 - 1.0;

    float3 dd = flow.x * tangent + flow.y * bitangent;
#else
    float3 windDir = CloudRotationUp(float3(0, 0, 1), _CloudScrollDirection);
    windDir.x *= -1.0;
    float3 dd = windDir*sin(dir.y*PI*0.5);
#endif

    // Sample twice
    float4 color1 = sampleCloud(normalize(dir - alpha.x * dd), layer);
    float4 color2 = sampleCloud(normalize(dir - alpha.y * dd), layer);

    // Blend color samples
    return lerp(color1, color2, abs(2.0 * alpha.x));

#else
    return sampleCloud(dir, layer);
#endif
}

float GetCloudOpacity(float3 dir)
{
    float opacity = 0;

#if USE_CLOUD_LAYER
    if (dir.y >= 0 || !_CloudUpperHemisphere)
    {
        opacity = GetDistordedCloudColor(dir, 0).a;
        if (_CloudDoubleLayer)
            opacity *= (1.0 - GetDistordedCloudColor(dir, 1).a);
    }
    else
#endif

    return opacity * _CloudOpacity;
}

float3 ApplyCloudLayer(float3 dir, float3 sky)
{
#if USE_CLOUD_LAYER
    if (dir.y >= 0 || !_CloudUpperHemisphere)
    {
        if (_CloudDoubleLayer)
            sky += GetDistordedCloudColor(dir, 1).rgb * _CloudIntensity * _CloudOpacity;
        sky += GetDistordedCloudColor(dir, 0).rgb * _CloudIntensity * _CloudOpacity;
    }
#endif

    return sky;
}

#undef _CloudUpperHemisphere
#undef _CloudScrollFactor
#undef _CloudScrollDirection
#undef _CloudIntensity

#undef USE_CLOUD_LAYER

#endif // __CLOUDLAYER_H__
