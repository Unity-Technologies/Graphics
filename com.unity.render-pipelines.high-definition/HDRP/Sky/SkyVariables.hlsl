#ifndef UNITY_SKY_VARIABLES_INCLUDED
#define UNITY_SKY_VARIABLES_INCLUDED

TEXTURECUBE(_SkyTexture);

CBUFFER_START(SkyParameters)
float _SkyTextureMipCount;
CBUFFER_END

float4 SampleSkyTexture(float3 texCoord)
{
    return SAMPLE_TEXTURECUBE(_SkyTexture, s_trilinear_clamp_sampler, texCoord);
}

float4 SampleSkyTexture(float3 texCoord, float lod)
{
    return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, s_trilinear_clamp_sampler, texCoord, lod);
}

#endif