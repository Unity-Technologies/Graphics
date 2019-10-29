#ifndef UNIVERSAL_AO_INCLUDED
#define UNIVERSAL_AO_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

float4 _NoiseTex_TexelSize;

//Common Settings
half _AO_Intensity;
half _AO_Radius;

// SSAO Settings
int _SSAO_Samples;
float _SSAO_Area;

float3 normal_from_depth(float depth, float2 texcoords)
{
  const float2 offset1 = float2(0.0,_ScreenParams.w - 1);
  const float2 offset2 = float2(_ScreenParams.z - 1,0.0);

  float depth1 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, texcoords + offset1).r, _ZBufferParams);
  float depth2 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, texcoords + offset2).r, _ZBufferParams);

  float3 p1 = float3(offset1, depth1 - depth);
  float3 p2 = float3(offset2, depth2 - depth);

  float3 normal = cross(p1, p2);
  normal.z = -normal.z;

  return normalize(normal);
}

float SSAO(float2 coords)
{
    float area = _SSAO_Area;
    const float falloff = 0.05;
    float radius = _AO_Radius;

    const int samples = _SSAO_Samples;
    const float3 sample_sphere[16] = {
        float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
        float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
        float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
        float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
        float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
        float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
        float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
        float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
    };

    float3 random = normalize(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, coords * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw)).rgb);

    float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, coords).r, _ZBufferParams);

    float3 position = float3(coords, depth);
    float3 normal = normal_from_depth(depth, coords);

    float radius_depth = radius;///depth;
    float occlusion = 0.0;

    for(int i=0; i < samples; i++) {

      float3 ray = radius_depth * reflect(sample_sphere[i], random);
      float3 hemi_ray = position + sign(dot(ray,normal)) * ray;

      float occ_depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, saturate(hemi_ray.xy)).r, _ZBufferParams);
      float difference = depth - occ_depth;

      occlusion += step(falloff, difference) * (1.0-smoothstep(falloff, area, difference));
    }
    float ao = 1.0 - _AO_Intensity * occlusion * (1.0 / samples);

    return ao;
}

#endif //UNIVERSAL_AO_INCLUDED
