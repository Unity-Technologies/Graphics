#ifndef UNITY_PACKING_INCLUDED
#define UNITY_PACKING_INCLUDED

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
    float l1norm    = dot(abs(n), 1.0);
    float2 res0     = n.xy * (1.0 / l1norm);

    float2 val      = 1.0 - abs(res0.yx);
    return (n.zz < float2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);
}

float3 UnpackNormalOctEncode(float2 f)
{
    float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));

    float2 val = 1.0 - abs(n.yx);
    n.xy = (n.zz < float2(0.0, 0.0) ? (n.xy >= 0.0 ? val : -val) : n.xy);

    return normalize(n);
}

float3 UnpackNormalRGB(float4 packedNormal, float scale = 1.0)
{
    float3 normal;
    normal.xyz = packedNormal.rgb * 2.0 - 1.0;
    normal.xy *= scale;
    return normalize(normal);
}

float3 UnpackNormalAG(float4 packedNormal, float scale = 1.0)
{
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.xy *= scale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

// Unpack normal as DXT5nm (1, y, 0, x) or BC5 (x, y, 0, 1)
float3 UnpackNormalmapRGorAG(float4 packedNormal, float scale = 1.0)
{
    // This do the trick
    packedNormal.x *= packedNormal.w;

    float3 normal;
    normal.xy = packedNormal.xy * 2.0 - 1.0;
    normal.xy *= scale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

//-----------------------------------------------------------------------------
// HDR packing
//-----------------------------------------------------------------------------

// Ref: http://realtimecollisiondetection.net/blog/?p=15
float4 PackLogLuv(float3 vRGB)
{
    // M matrix, for encoding
    const float3x3 M = float3x3(
        0.2209, 0.3390, 0.4184,
        0.1138, 0.6780, 0.7319,
        0.0102, 0.1130, 0.2969);

    float4 vResult;
    float3 Xp_Y_XYZp = mul(vRGB, M);
    Xp_Y_XYZp = max(Xp_Y_XYZp, float3(1e-6, 1e-6, 1e-6));
    vResult.xy = Xp_Y_XYZp.xy / Xp_Y_XYZp.z;
    float Le = 2.0 * log2(Xp_Y_XYZp.y) + 127.0;
    vResult.w = frac(Le);
    vResult.z = (Le - (floor(vResult.w*255.0f)) / 255.0f) / 255.0f;
    return vResult;
}

float3 UnpackLogLuv(float4 vLogLuv)
{
    // Inverse M matrix, for decoding
    const float3x3 InverseM = float3x3(
        6.0014, -2.7008, -1.7996,
        -1.3320, 3.1029, -5.7721,
        0.3008, -1.0882, 5.6268);

    float Le = vLogLuv.z * 255.0 + vLogLuv.w;
    float3 Xp_Y_XYZp;
    Xp_Y_XYZp.y = exp2((Le - 127.0) / 2.0);
    Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
    Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
    float3 vRGB = mul(Xp_Y_XYZp, InverseM);
    return max(vRGB, float3(0.0, 0.0, 0.0));
}

// TODO: This function is used with the LightTransport pass to encode lightmap or emissive
float4 PackRGBM(float3 rgb, float maxRGBM)
{
    float kOneOverRGBMMaxRange = 1.0 / maxRGBM;
    const float kMinMultiplier = 2.0 * 1e-2;

    float4 rgbm = float4(rgb * kOneOverRGBMMaxRange, 1.0);
    rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    rgbm.a = max(rgbm.a, kMinMultiplier);

    rgbm.rgb /= rgbm.a;
    return rgbm;
}

// Alternative...
#define RGBMRANGE (8.0)
float4 PackRGBM(float3 color)
{
    float4 rgbm;
    color *= (1.0 / RGBMRANGE);
    rgbm.a = saturate(max(max(color.r, color.g), max(color.b, 1e-6)));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;
    rgbm.rgb = color / rgbm.a;
    return rgbm;
}

float3 UnpackRGBM(float4 rgbm)
{
    return RGBMRANGE * rgbm.rgb * rgbm.a;
}

// The standard 32-bit HDR color format
uint PackR11G11B10f(float3 rgb)
{
    uint r = (f32tof16(rgb.x) << 17) & 0xFFE00000;
    uint g = (f32tof16(rgb.y) << 6) & 0x001FFC00;
    uint b = (f32tof16(rgb.z) >> 5) & 0x000003FF;
    return r | g | b;
}

float3 UnpackR11G11B10f(uint rgb)
{
    float r = f16tof32((rgb >> 17) & 0x7FF0);
    float g = f16tof32((rgb >> 6) & 0x7FF0);
    float b = f16tof32((rgb << 5) & 0x7FE0);
    return float3(r, g, b);
}

//-----------------------------------------------------------------------------
// Quaternion packing
//-----------------------------------------------------------------------------

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4

/*
// This is GCN intrinsic
uint FindBiggestComponent(float4 q)
{
    uint xyzIndex = CubeMapFaceID(q.x, q.y, q.z) * 0.5f;
    uint wIndex = 3;

    bool wBiggest = abs(q.w) > max3(abs(q.x), qbs(q.y), qbs(q.z));

    return wBiggest ? wIndex : xyzIndex;
}

// Pack a quaternion into a 10:10:10:2
float4  PackQuat(float4 quat)
{
    uint index = FindBiggestComponent(quat);

    if (index == 0) quat = quat.yzwx;
    if (index == 1) quat = quat.xzwy;
    if (index == 2) quat = quat.xywz;

    float4 packedQuat;
    packedQuat.xyz = quat.xyz * sign(quat.w) * sqrt(0.5) + 0.5;
    packedQuat.w = index / 3.0;

    return packedQuat;
}
*/

// Unpack a quaternion from a 10:10:10:2
float4 UnpackQuat(float4 packedQuat)
{
    uint index = (uint)(packedQuat.w * 3.0);

    float4 quat;
    quat.xyz = packedQuat.xyz * sqrt(2.0) - (1.0 / sqrt(2.0));
    quat.w = sqrt(1.0 - saturate(dot(quat.xyz, quat.xyz)));

    if (index == 0) quat = quat.wxyz;
    if (index == 1) quat = quat.xwyz;
    if (index == 2) quat = quat.xywz;

    return quat;
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

//-----------------------------------------------------------------------------
// float packing to sint/uint
//-----------------------------------------------------------------------------

// src must be between 0.0 and 1.0
uint PackFloatToUInt(float src, uint size, uint offset)
{
    const float maxValue = float((1u << size) - 1u) + 0.5; // Shader compiler should be able to remove this
    return uint(src * maxValue) << offset;
}

float UnpackUIntToFloat(uint src, uint size, uint offset)
{
    const float invMaxValue = 1.0 / float((1 << size) - 1);

    return float(BitFieldExtract(src, size, offset)) * invMaxValue;
}

uint PackR10G10B10A2(float4 rgba)
{
    return (PackFloatToUInt(rgba.x, 10, 0) | PackFloatToUInt(rgba.y, 10, 10) | PackFloatToUInt(rgba.z, 10, 20) | PackFloatToUInt(rgba.w, 2, 30));
}

float4 UnpackR10G10B10A2(uint rgba)
{
    float4 ouput;
    ouput.x = UnpackUIntToFloat(rgba, 10, 0);
    ouput.y = UnpackUIntToFloat(rgba, 10, 10);
    ouput.z = UnpackUIntToFloat(rgba, 10, 20);
    ouput.w = UnpackUIntToFloat(rgba, 2, 30);
    return ouput;
}


#endif // UNITY_PACKING_INCLUDED
