#ifndef UNITY_PACKING_INCLUDED
#define UNITY_PACKING_INCLUDED

//-----------------------------------------------------------------------------
// Normal packing
//-----------------------------------------------------------------------------

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

    //float2 val = 1.0 - abs(n.yx);
    //n.xy = (n.zz < float2(0.0, 0.0) ? (n.xy >= 0.0 ? val : -val) : n.xy);

    // Optimized version of above code:
    float t = max(-n.z, 0.0);
    n.xy += n.xy >= 0.0 ? -t.xx : t.xx;

    return normalize(n);
}

float2 PackNormalHemiOctEncode(float3 n)
{
    float l1norm = dot(abs(n), 1.0);
    float2 res = n.xy * (1.0 / l1norm);

    return float2(res.x + res.y, res.x - res.y);
}

float3 UnpackNormalHemiOctEncode(float2 f)
{
    float2 val = float2(f.x + f.y, f.x - f.y) * 0.5;
    float3 n = float3(val, 1.0 - dot(abs(val), 1.0));

    return normalize(n);
}

// Tetrahedral encoding - Looks like Tetra encoding 10:10 + 2 is similar to oct 11:11, as oct is cheaper prefer it
// To generate the basisNormal below we use these 4 vertex of a regular tetrahedron
// v0 = float3(1.0, 0.0, -1.0 / sqrt(2.0));
// v1 = float3(-1.0, 0.0, -1.0 / sqrt(2.0));
// v2 = float3(0.0, 1.0, 1.0 / sqrt(2.0));
// v3 = float3(0.0, -1.0, 1.0 / sqrt(2.0));
// Then we normalize the average of each face's vertices
// normalize(v0 + v1 + v2), etc...
static const float3 tetraBasisNormal[4] =
{
    float3(0., 0.816497, -0.57735),
    float3(-0.816497, 0., 0.57735),
    float3(0.816497, 0., 0.57735),
    float3(0., -0.816497, -0.57735)
};

// Then to get the local matrix (with z axis rotate to basisNormal) use GetLocalFrame(basisNormal[xxx])
static const float3x3 tetraBasisArray[4] =
{
    float3x3(-1., 0., 0.,0., 0.57735, 0.816497,0., 0.816497, -0.57735),
    float3x3(0., -1., 0.,0.57735, 0., 0.816497,-0.816497, 0., 0.57735),
    float3x3(0., 1., 0.,-0.57735, 0., 0.816497,0.816497, 0., 0.57735),
    float3x3(1., 0., 0.,0., -0.57735, 0.816497,0., -0.816497, -0.57735)
};

// Return [-1..1] vector2 oriented in plane of the faceIndex of a regular tetrahedron
float2 PackNormalTetraEncode(float3 n, out uint faceIndex)
{
    // Retrieve the tetrahedra's face for the normal direction
    // It is the one with the greatest dot value with face normal
    float dot0 = dot(n, tetraBasisNormal[0]);
    float dot1 = dot(n, tetraBasisNormal[1]);
    float dot2 = dot(n, tetraBasisNormal[2]);
    float dot3 = dot(n, tetraBasisNormal[3]);

    float maxi0 = max(dot0, dot1);
    float maxi1 = max(dot2, dot3);
    float maxi = max(maxi0, maxi1);

    // Get the index from the greatest dot
    if (maxi == dot0)
        faceIndex = 0;
    else if (maxi == dot1)
        faceIndex = 1;
    else if (maxi == dot2)
        faceIndex = 2;
    else //(maxi == dot3)
        faceIndex = 3;

    // Rotate n into this local basis
    n = mul(tetraBasisArray[faceIndex], n);

    // Project n onto the local plane
    return n.xy;
}

// Assume f [-1..1]
float3 UnpackNormalTetraEncode(float2 f, uint faceIndex)
{
    // Recover n from local plane
    float3 n = float3(f.xy, sqrt(1.0 - dot(f.xy, f.xy)));
    // Inverse of transform PackNormalTetraEncode (just swap order in mul as we have a rotation)
    return mul(n, tetraBasisArray[faceIndex]);
}

// Unpack from normal map
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
    packedNormal.w *= packedNormal.x;
    return UnpackNormalAG(packedNormal, scale);
}

//-----------------------------------------------------------------------------
// HDR packing
//-----------------------------------------------------------------------------

// Ref: http://realtimecollisiondetection.net/blog/?p=15
float4 PackToLogLuv(float3 vRGB)
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
    vResult.z = (Le - (floor(vResult.w * 255.0)) / 255.0) / 255.0;
    return vResult;
}

float3 UnpackFromLogLuv(float4 vLogLuv)
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

// The standard 32-bit HDR color format
uint PackToR11G11B10f(float3 rgb)
{
    uint r = (f32tof16(rgb.x) << 17) & 0xFFE00000;
    uint g = (f32tof16(rgb.y) << 6) & 0x001FFC00;
    uint b = (f32tof16(rgb.z) >> 5) & 0x000003FF;
    return r | g | b;
}

float3 UnpackFromR11G11B10f(uint rgb)
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
    packedQuat.xyz = quat.xyz * FastSign(quat.w) * sqrt(0.5) + 0.5;
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
// Integer packing
//-----------------------------------------------------------------------------

// Packs an integer stored using at most 'numBits' into a [0..1] float.
float PackInt(uint i, uint numBits)
{
    uint maxInt = UINT_MAX >> (32u - numBits);
    return saturate(i * rcp(maxInt));
}

// Unpacks a [0..1] float into an integer of size 'numBits'.
uint UnpackInt(float f, uint numBits)
{
    uint maxInt = UINT_MAX >> (32u - numBits);
    return (uint)(f * maxInt + 0.5); // Round instead of truncating
}

// Packs a [0..255] integer into a [0..1] float.
float PackByte(uint i)
{
    return PackInt(i, 8);
}

// Unpacks a [0..1] float into a [0..255] integer.
uint UnpackByte(float f)
{
    return UnpackInt(f, 8);
}

// Packs a [0..65535] integer into a [0..1] float.
float PackShort(uint i)
{
    return PackInt(i, 16);
}

// Unpacks a [0..1] float into a [0..65535] integer.
uint UnpackShort(float f)
{
    return UnpackInt(f, 16);
}

// Packs 8 lowermost bits of a [0..65535] integer into a [0..1] float.
float PackShortLo(uint i)
{
    uint lo = BitFieldExtract(i, 8u, 0u);
    return PackInt(lo, 8);
}

// Packs 8 uppermost bits of a [0..65535] integer into a [0..1] float.
float PackShortHi(uint i)
{
    uint hi = BitFieldExtract(i, 8u, 8u);
    return PackInt(hi, 8);
}

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
    i = int((val / t2) + rcp(precisionMinusOne)); // + rcp(precisionMinusOne) to deal with precision issue (can't use round() as val contain the floating number
    // Now that we have i, solve formula in PackFloatInt for f
    //f = (val - t2 * float(i)) / t1 => convert in mads form
    f = saturate((-t2 * float(i) + val) / t1); // Saturate in case of precision issue
}

// Define various variante for ease of read
float PackFloatInt8bit(float f, int i, float maxi)
{
    return PackFloatInt(f, i, maxi, 256.0);
}

void UnpackFloatInt8bit(float val, float maxi, out float f, out int i)
{
    UnpackFloatInt(val, maxi, 256.0, f, i);
}

float PackFloatInt10bit(float f, int i, float maxi)
{
    return PackFloatInt(f, i, maxi, 1024.0);
}

void UnpackFloatInt10bit(float val, float maxi, out float f, out int i)
{
    UnpackFloatInt(val, maxi, 1024.0, f, i);
}

float PackFloatInt16bit(float f, int i, float maxi)
{
    return PackFloatInt(f, i, maxi, 65536.0);
}

void UnpackFloatInt16bit(float val, float maxi, out float f, out int i)
{
    UnpackFloatInt(val, maxi, 65536.0, f, i);
}

//-----------------------------------------------------------------------------
// Float packing
//-----------------------------------------------------------------------------

// src must be between 0.0 and 1.0
uint PackFloatToUInt(float src, uint numBits, uint offset)
{
    return UnpackInt(src, numBits) << offset;
}

float UnpackUIntToFloat(uint src, uint numBits, uint offset)
{
    uint maxInt = UINT_MAX >> (32u - numBits);
    return float(BitFieldExtract(src, numBits, offset)) * rcp(maxInt);
}

uint PackToR10G10B10A2(float4 rgba)
{
    return (PackFloatToUInt(rgba.x, 10, 0) | PackFloatToUInt(rgba.y, 10, 10) | PackFloatToUInt(rgba.z, 10, 20) | PackFloatToUInt(rgba.w, 2, 30));
}

float4 UnpackFromR10G10B10A2(uint rgba)
{
    float4 ouput;
    ouput.x = UnpackUIntToFloat(rgba, 10, 0);
    ouput.y = UnpackUIntToFloat(rgba, 10, 10);
    ouput.z = UnpackUIntToFloat(rgba, 10, 20);
    ouput.w = UnpackUIntToFloat(rgba, 2, 30);
    return ouput;
}

// Both the input and the output are in the [0, 1] range.
float2 PackFloatToR8G8(float f)
{
    uint i = UnpackShort(f);
    return float2(PackShortLo(i), PackShortHi(i));
}

// Both the input and the output are in the [0, 1] range.
float UnpackFloatFromR8G8(float2 f)
{
    uint lo = UnpackByte(f.x);
    uint hi = UnpackByte(f.y);
    uint cb = (hi << 8) + lo;
    return PackShort(cb);
}

#endif // UNITY_PACKING_INCLUDED
