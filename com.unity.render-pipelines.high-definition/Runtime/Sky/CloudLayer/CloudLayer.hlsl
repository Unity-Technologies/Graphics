#ifndef __CLOUDLAYER_H__
#define __CLOUDLAYER_H__

TEXTURE2D(_CloudMap);
SAMPLER(sampler_CloudMap);
    
TEXTURE2D(_CloudFlowmap);
SAMPLER(sampler_CloudFlowmap);

float4 _CloudParam; // x upper hemisphere only / rotation, y scroll factor, zw scroll direction (cosPhi and sinPhi)
float4 _CloudParam2; // xyz tint, w intensity

#define _CloudUpperHemisphere   _CloudParam.x > 0
#define _CloudRotation          abs(_CloudParam.x)
#define _CloudScrollFactor      _CloudParam.y
#define _CloudScrollDirection   _CloudParam.zw
#define _CloudTint              _CloudParam2.xyz
#define _CloudIntensity         _CloudParam2.w

#define USE_CLOUD_LAYER         defined(USE_CLOUD_MAP) || (!defined(USE_CLOUD_MAP) && defined(USE_CLOUD_MOTION))

float3 sampleCloud(float3 dir, float3 sky)
{
    float2 coords = GetLatLongCoords(dir, _CloudUpperHemisphere);
    coords.x = frac(coords.x + _CloudRotation);
    float4 cloudLayerColor = SAMPLE_TEXTURE2D_LOD(_CloudMap, sampler_CloudMap, coords, 0);
    return lerp(sky, sky + cloudLayerColor.rgb * _CloudTint * _CloudIntensity, cloudLayerColor.a);
}

float3 CloudRotationUp(float3 p, float2 cos_sin)
{
    float3 rotDirX = float3(cos_sin.x, 0, -cos_sin.y);
    float3 rotDirY = float3(cos_sin.y, 0,  cos_sin.x);

    return float3(dot(rotDirX, p), p.y, dot(rotDirY, p));
}

float3 GetDistordedCloudColor(float3 dir, float3 sky)
{
#if USE_CLOUD_MOTION
    if (dir.y >= 0 || !_CloudUpperHemisphere)
    {
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
        float3 color1 = sampleCloud(normalize(dir - alpha.x * dd), sky);
        float3 color2 = sampleCloud(normalize(dir - alpha.y * dd), sky);

        // Blend color samples
        return lerp(color1, color2, abs(2.0 * alpha.x));
    }
#else
    sky = sampleCloud(dir, sky);
#endif

    return sky;
}

float3 ApplyCloudLayer(float3 dir, float3 sky)
{
#if USE_CLOUD_LAYER
    if (dir.y >= 0 || !_CloudUpperHemisphere)
        sky = GetDistordedCloudColor(dir, sky);
#endif

    return sky;
}

#undef _CloudUpperHemisphere
#undef _CloudScrollFactor
#undef _CloudScrollDirection
#undef _CloudTint
#undef _CloudIntensity

#undef USE_CLOUD_LAYER

#endif // __CLOUDLAYER_H__
