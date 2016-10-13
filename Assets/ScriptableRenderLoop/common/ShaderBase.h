#ifndef __SHADERBASE_H__
#define __SHADERBASE_H__


#define __HLSL      1
#define public


#define unistruct   cbuffer
#define hbool       bool

#define _CB_REGSLOT(x)      : register(x)
#define _QALIGN(x)          : packoffset(c0);


float FetchDepth(Texture2D depthTexture, uint2 pixCoord)
{
    return 1 - depthTexture.Load(uint3(pixCoord.xy, 0)).x;
}

float FetchDepthMSAA(Texture2DMS<float> depthTexture, uint2 pixCoord, uint sampleIdx)
{
    return 1 - depthTexture.Load(uint3(pixCoord.xy, 0), sampleIdx).x;
}

#endif
