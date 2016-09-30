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
float2 PackNormalOctEncode(float3 n)
{
	float l1norm	= abs(n.x) + abs(n.y) + abs(n.z);
	float2 res0		= n.xy * (1.0 / l1norm);

	float2 val		= 1.0 - abs(res0.yx);
	return (n.zz < float2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);
}

float3 UnpackNormalOctEncode(float2 f)
{
	float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));

	float2 val = 1.0 - abs(n.yx);

	return normalize(n);
}

float3 UnpackNormalDXT5nm (float4 packednormal)
{
	float3 normal;
	normal.xy = packednormal.wy * 2.0 - 1.0;
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

// Encode a float in [0..1] and an int in [0..maxi - 1] as a float [0..1] to be store in log2(precision) bit
// maxi must be a power of two and define the number of bit dedicated 0..1 to the int part (log2(maxi))
// Example: precision is 256.0, maxi is 2, i is [0..1] encode on 1 bit. f is [0..1] encode on 7 bit.
// Example: precision is 256.0, maxi is 4, i is [0..3] encode on 2 bit. f is [0..1] encode on 6 bit.
// Example: precision is 256.0, maxi is 8, i is [0..7] encode on 3 bit. f is [0..1] encode on 5 bit.
// ...
// Example: precision is 1024.0, maxi is 8, i is [0..7] encode on 3 bit. f is [0..1] encode on 7 bit.
//...
float PackFloatInt(float f, int i, float maxi, float precision)
{
	// Constant
	float precisionMinusOne = precision - 1.0;
	float t1 = ((precision / maxi) - 1.0) / precisionMinusOne;
	float t2 = (precision / maxi) / precisionMinusOne;

	return t1 * f + t2 * float(i);
}

void UnpackFloatInt(float val, float maxi, float precision, out float f, out int i)
{
	// Constant
	float precisionMinusOne = precision - 1.0;
	float t1 = ((precision / maxi) - 1.0) / precisionMinusOne;
	float t2 = (precision / maxi) / precisionMinusOne;

	// extract integer part
	i = int(val / t2);
	// Now that we have i, solve formula in PackFloatInt for f
	//f = (val - t2 * float(i)) / t1 => convert in mads form
	f = (-t2 * float(i) + val) / t1;
}

// Define various variante for ease of read
float PackFloatInt8bit(float f, int i, float maxi)
{
	return PackFloatInt(f, i, maxi, 255.0);
}

float UnpackFloatInt8bit(float val, float maxi, out float f, out int i)
{
	UnpackFloatInt(val, maxi, 255.0, f, i);
}

float PackFloatInt10bit(float f, int i, float maxi)
{
	return PackFloatInt(f, i, maxi, 1024.0);
}

float UnpackFloatInt10bit(float val, float maxi, out float f, out int i)
{
	UnpackFloatInt(val, maxi, 1024.0, f, i);
}

float PackFloatInt16bit(float f, int i, float maxi)
{
	return PackFloatInt(f, i, maxi, 65536.0);
}

float UnpackFloatInt16bit(float val, float maxi, out float f, out int i)
{
	UnpackFloatInt(val, maxi, 65536.0, f, i);
}

#endif // UNITY_PACKING_INCLUDED
