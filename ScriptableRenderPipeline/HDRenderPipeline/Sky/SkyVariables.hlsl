#ifndef UNITY_SKY_VARIABLES_INCLUDED
#define UNITY_SKY_VARIABLES_INCLUDED

TEXTURECUBE(_SkyTexture);
SAMPLERCUBE(sampler_SkyTexture); // NOTE: Sampler could be share here with _EnvTextures. Don't know if the shader compiler will complain...

CBUFFER_START(SkyParameters)
float _SkyTextureMipCount;
CBUFFER_END

float4 SampleSkyTexture(float3 texCoord)
{
    return SAMPLE_TEXTURECUBE(_SkyTexture, sampler_SkyTexture, texCoord);
}

float4 SampleSkyTexture(float3 texCoord, float lod)
{
    return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, sampler_SkyTexture, texCoord, lod);
}

#endif