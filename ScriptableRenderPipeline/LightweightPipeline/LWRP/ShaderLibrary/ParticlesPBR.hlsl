#ifndef LIGHTWEIGHT_PARTICLES_PBR_INCLUDED
#define LIGHTWEIGHT_PARTICLES_PBR_INCLUDED

#include "Particles.hlsl"

TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);

void InitializeSurfaceData(VertexOutputLit IN, out SurfaceData surfaceData)
{
    half4 albedo = SampleAlbedo(IN, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));

#if defined(_METALLICGLOSSMAP)
    half2 metallicGloss = readTexture(TEXTURE2D_PARAM(_MetallicGlossMap, sampler_MetallicGlossMap), IN).ra * half2(1.0, _Glossiness);
#else
    half2 metallicGloss = half2(_Metallic, _Glossiness);
#endif

    half3 normalTS = SampleNormalTS(IN, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap), _BumpScale);
    half3 emission = SampleEmission(IN, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));

    surfaceData.albedo = albedo.rgb;
    surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    surfaceData.normalTS = normalTS;
    surfaceData.emission = emission;
    surfaceData.metallic = metallicGloss.r;
    surfaceData.smoothness = metallicGloss.g;
    surfaceData.occlusion = 1.0;

    surfaceData.albedo = AlphaModulate(surfaceData.albedo, albedo.a);
    surfaceData.alpha = AlphaBlendAndTest(albedo.a, _Cutoff);
}

#endif // LIGHTWEIGHT_PARTICLES_PBR_INCLUDED
