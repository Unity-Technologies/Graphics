#ifndef UNITY_PACKING_INCLUDED
#define UNITY_PACKING_INCLUDED

#include "Common.hlsl"

//-----------------------------------------------------------------------------
// Normal packing
//-----------------------------------------------------------------------------

float3 PackNormalCartesian(float3 n)
{
	return n * 0.5 + 0.5;
}

float3 UnpackNormalCartesian(float3 n)
{
	return normalize(n * 2.0 - 1.0);
}

float3 PackNormalMaxComponent(float3 n)
{
	// TODO: use max3
	return (n / max(abs(n.x), max(abs(n.y), abs(n.z)))) * 0.5 + 0.5;
}

float3 UnpackNormalMaxComponent(float3 n)
{
	return normalize(n * 2.0 - 1.0);
}

// Ref: http://jcgt.org/published/0003/02/01/paper.pdf
// Encode with Oct, this function work with any size of output
// return float between [-1, 1]
float2 PackNormalOctEncode(float3 v)
{
	float l1norm	= abs(v.x) + abs(v.y) + abs(v.z);
	float2 res0		= v.xy * (1.0f / l1norm);

	float2 val		= 1.0f - abs(res0.yx);
	return (v.zz < float2(0.0f, 0.0f) ? (res0 >= 0.0f ? val : -val) : res0);
}

float3 UnpackNormalOctEncode(float x, float y)
{
	float3 v = float3(x, y, 1.0f - abs(x) - abs(y));

	float2 val = 1.0f - abs(v.yx);
	v.xy = (v.zz < float2(0.0f, 0.0f) ? (v.xy >= 0.0f ? val : -val) : v.xy);

	return normalize(v);
}

float3 UnpackNormalDXT5nm (float4 packednormal)
{
	float3 normal;
	normal.xy = packednormal.wy * 2 - 1;
	normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
	return normal;
}

//-----------------------------------------------------------------------------
// Byte packing
//-----------------------------------------------------------------------------

float Pack2Byte(float2 inputs)
{
	float2 temp = inputs * float2(255.0, 255.0);
	temp.x *= 256.0;
	temp = round(temp);
	float combined = temp.x + temp.y;
	return combined * (1.0 / 65535.0);
}

float2 Unpack2Byte(float inputs)
{
	float temp = round(inputs * 65535.0);
	float ipart;
	float fpart = modf(temp / 256.0, ipart);
	float2 result = float2(ipart, round(256.0 * fpart));
	return result * (1.0 / float2(255.0, 255.0));
}

//-----------------------------------------------------------------------------

#endif // UNITY_PACKING_INCLUDED
