#ifndef __SHADERBASE_H__
#define __SHADERBASE_H__


#define __HLSL		1
#define public


#define Vec2		float2
#define Vec3		float3
#define Vec4		float4
#define Mat44		float4x4
#define unistruct	cbuffer
#define hbool		bool

#define _CB_REGSLOT(x) 		: register(x)
#define _QALIGN(x)	 		: packoffset(c0);


float FetchDepth(Texture2D depthTexture, uint2 pixCoord)
{
	return 1 - depthTexture.Load(uint3(pixCoord.xy, 0)).x;
}

#endif