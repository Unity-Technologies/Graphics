//Helper to disable bounding box compute code
#define USE_DYNAMIC_AABB 1

// Special semantics for VFX blocks
#define RAND Rand(seed)
#define RAND2 float2(RAND,RAND)
#define RAND3 float3(RAND,RAND,RAND)
#define RAND4 float4(RAND,RAND,RAND,RAND)
#define FIXED_RAND(h) FixedRand(particleId ^ asuint(systemSeed) ^ h)
#define FIXED_RAND2(h) float2(FIXED_RAND(h),FIXED_RAND(h))
#define FIXED_RAND3(h) float3(FIXED_RAND(h),FIXED_RAND(h),FIXED_RAND(h))
#define FIXED_RAND4(h) float4(FIXED_RAND(h),FIXED_RAND(h),FIXED_RAND(h),FIXED_RAND(h))
#define KILL {kill = true;}
#define SAMPLE sampleSignal
#define SAMPLE_SPLINE_POSITION(v,u) sampleSpline(v.x,u)
#define SAMPLE_SPLINE_TANGENT(v,u) sampleSpline(v.y,u)
#define INVERSE(m) Inv##m

#define VFX_FLT_MIN 1.175494351e-38
#define VFX_EPSILON 1e-5

#pragma warning(disable : 3557) // disable warning for auto unrolling of single iteration loop

struct VFXSampler2D
{
    Texture2D t;
    SamplerState s;
};

struct VFXSampler2DArray
{
    Texture2DArray t;
    SamplerState s;
};

struct VFXSampler3D
{
    Texture3D t;
    SamplerState s;
};

struct VFXSamplerCube
{
    TextureCube t;
    SamplerState s;
};

struct VFXSamplerCubeArray
{
    TextureCubeArray t;
    SamplerState s;
};

#if !VFX_WORLD_SPACE && !VFX_LOCAL_SPACE
#error VFXCommon.hlsl should be included after space defines
#endif

#if VFX_WORLD_SPACE && VFX_LOCAL_SPACE
#error VFX_WORLD_SPACE & VFX_LOCAL_SPACE are both enabled
#endif

#ifdef VFX_WORLD_SPACE
float3 TransformDirectionVFXToWorld(float3 dir)             { return dir; }
float3 TransformPositionVFXToWorld(float3 pos)              { return pos; }
float3 TransformNormalVFXToWorld(float3 n)                  { return n; }
float3 TransformPositionVFXToView(float3 pos)               { return VFXTransformPositionWorldToView(pos); }
float4 TransformPositionVFXToClip(float3 pos)               { return VFXTransformPositionWorldToClip(pos); }
float4 TransformPositionVFXToPreviousClip(float3 pos)       { return VFXTransformPositionWorldToPreviousClip(pos); }
float4 TransformPositionVFXToNonJitteredClip(float3 pos)    { return VFXTransformPositionWorldToNonJitteredClip(pos); }
float3x3 GetVFXToViewRotMatrix()                            { return VFXGetWorldToViewRotMatrix(); }
float3 GetViewVFXPosition()                                 { return VFXGetViewWorldPosition(); }
#else
float3 TransformDirectionVFXToWorld(float3 dir)             { return mul(VFXGetObjectToWorldMatrix(),float4(dir,0.0f)).xyz; }
float3 TransformPositionVFXToWorld(float3 pos)              { return mul(VFXGetObjectToWorldMatrix(),float4(pos,1.0f)).xyz; }
float3 TransformNormalVFXToWorld(float3 n)                  { return mul(n, (float3x3)GetWorldToObjectMatrix()); }
float3 TransformPositionVFXToView(float3 pos)               { return VFXTransformPositionWorldToView(mul(VFXGetObjectToWorldMatrix(), float4(pos, 1.0f)).xyz); }
float4 TransformPositionVFXToClip(float3 pos)               { return VFXTransformPositionObjectToClip(pos); }
float4 TransformPositionVFXToPreviousClip(float3 pos)       { return VFXTransformPositionObjectToPreviousClip(pos); }
float4 TransformPositionVFXToNonJitteredClip(float3 pos)    { return VFXTransformPositionObjectToNonJitteredClip(pos); }
float3x3 GetVFXToViewRotMatrix()                            { return mul(VFXGetWorldToViewRotMatrix(),(float3x3)VFXGetObjectToWorldMatrix()); }
float3 GetViewVFXPosition()                                 { return mul(VFXGetWorldToObjectMatrix(),float4(VFXGetViewWorldPosition(),1.0f)).xyz; }
#endif

#define VFX_SAMPLER(name) GetVFXSampler(name,sampler##name)

float4 SampleTexture(VFXSampler2D s,float2 coords,float level = 0.0f)
{
    return s.t.SampleLevel(s.s,coords, level);
}

float4 SampleTexture(VFXSampler2DArray s,float2 coords,float slice,float level = 0.0f)
{
    return s.t.SampleLevel(s.s,float3(coords,slice),level);
}

float4 SampleTexture(VFXSampler3D s,float3 coords,float level = 0.0f)
{
    return s.t.SampleLevel(s.s,coords,level);
}

float4 SampleTexture(VFXSamplerCube s,float3 coords,float level = 0.0f)
{
    return s.t.SampleLevel(s.s,coords,level);
}

float4 SampleTexture(VFXSamplerCubeArray s,float3 coords,float slice,float level = 0.0f)
{
    return s.t.SampleLevel(s.s,float4(coords,slice),level);
}

float4 LoadTexture(VFXSampler2D s, int3 pixelCoords)
{
    return s.t.Load(pixelCoords);
}

float4 LoadTexture(VFXSampler2DArray s, int4 pixelCoords)
{
    return s.t.Load(pixelCoords);
}

float4 LoadTexture(VFXSampler3D s, int4 pixelCoords)
{
    return s.t.Load(pixelCoords);
}

float SampleSDF(VFXSampler3D s,float3 coords,float level = 0.0f)
{
    return SampleTexture(s,coords,level).x;
}

float3 SampleSDFDerivativesFast(VFXSampler3D s, float3 coords, float dist, float level = 0.0f)
{
    float3 d;
    // 3 taps
    const float kStep = 0.01f;
    d.x = SampleSDF(s, coords + float3(kStep, 0, 0));
    d.y = SampleSDF(s, coords + float3(0, kStep, 0));
    d.z = SampleSDF(s, coords + float3(0, 0, kStep));
    return d - dist;
}

float3 SampleSDFDerivatives(VFXSampler3D s,float3 coords,float level = 0.0f)
{
    float3 d;
    // 6 taps
    const float kStep = 0.01f;
    d.x = SampleSDF(s, coords + float3(kStep, 0, 0)) - SampleSDF(s, coords - float3(kStep, 0, 0));
    d.y = SampleSDF(s, coords + float3(0, kStep, 0)) - SampleSDF(s, coords - float3(0, kStep, 0));
    d.z = SampleSDF(s, coords + float3(0, 0, kStep)) - SampleSDF(s, coords - float3(0, 0, kStep));
    return d;
}

VFXSampler2D GetVFXSampler(Texture2D t,SamplerState s)
{
    VFXSampler2D vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

VFXSampler2DArray GetVFXSampler(Texture2DArray t,SamplerState s)
{
    VFXSampler2DArray vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

VFXSampler3D GetVFXSampler(Texture3D t,SamplerState s)
{
    VFXSampler3D vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

VFXSamplerCube GetVFXSampler(TextureCube t,SamplerState s)
{
    VFXSamplerCube vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

VFXSamplerCubeArray GetVFXSampler(TextureCubeArray t,SamplerState s)
{
    VFXSamplerCubeArray vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

uint ConvertFloatToSortableUint(float f)
{
    int mask = (-(int)(asuint(f) >> 31)) | 0x80000000;
    return asuint(f) ^ mask;
}

uint3 ConvertFloatToSortableUint(float3 f)
{
    uint3 res;
    res.x = ConvertFloatToSortableUint(f.x);
    res.y = ConvertFloatToSortableUint(f.y);
    res.z = ConvertFloatToSortableUint(f.z);
    return res;
}

/////////////////////////////
// Random number generator //
/////////////////////////////

#define RAND_24BITS 0

uint VFXMul24(uint a,uint b)
{
#ifndef SHADER_API_PSSL
    return (a & 0xffffff) * (b & 0xffffff); // Tmp to ensure correct inputs
#else
    return mul24(a, b);
#endif
}

uint WangHash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed += (seed << 3);
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

uint WangHash2(uint seed) // without mul on integers
{
    seed += ~(seed<<15);
    seed ^=  (seed>>10);
    seed +=  (seed<<3);
    seed ^=  (seed>>6);
    seed += ~(seed<<11);
    seed ^=  (seed>>16);
    return seed;
}

// See https://stackoverflow.com/a/12996028
uint AnotherHash(uint seed)
{
#if RAND_24BITS
    seed = VFXMul24((seed >> 16) ^ seed,0x5d9f3b);
    seed = VFXMul24((seed >> 16) ^ seed,0x5d9f3b);
#else
    seed = ((seed >> 16) ^ seed) * 0x45d9f3b;
    seed = ((seed >> 16) ^ seed) * 0x45d9f3b;
#endif
    seed = (seed >> 16) ^ seed;
    return seed;
}

uint Lcg(uint seed)
{
    const uint multiplier = 0x0019660d;
    const uint increment = 0x3c6ef35f;
#if RAND_24BITS && defined(SHADER_API_PSSL)
    return mad24(multiplier, seed, increment);
#else
    return multiplier * seed + increment;
#endif
}

float ToFloat01(uint u)
{
#if !RAND_24BITS
    return asfloat((u >> 9) | 0x3f800000) - 1.0f;
#else //Using mad24 keeping consitency between platform
    return asfloat((u & 0x007fffff) | 0x3f800000) - 1.0f;
#endif
}

float Rand(inout uint seed)
{
    seed = Lcg(seed);
    return ToFloat01(seed);
}

float FixedRand(uint seed)
{
    return ToFloat01(AnotherHash(seed));
}

///////////////////////////
// Color transformations //
///////////////////////////

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 RGBtoHCV(in float3 RGB)
{
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + 1e-10) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBtoHSV(in float3 RGB)
{
    float3 HCV = RGBtoHCV(RGB);
    float S = HCV.y / (HCV.z + 1e-10);
    return float3(HCV.x, S, HCV.z);
}

float3 HSVtoRGB(in float3 HSV)
{
    return ((HUEtoRGB(HSV.x) - 1) * HSV.y + 1) * HSV.z;
}

///////////////////
// Baked texture //
///////////////////

Texture2D bakedTexture;
SamplerState samplerbakedTexture;

float HalfTexelOffset(float f)
{
    const uint kTextureWidth = 128;
    float a = (kTextureWidth - 1.0f) / kTextureWidth;
    float b = 0.5f / kTextureWidth;
    return (a * f) + b;
}

float4 SampleGradient(float v,float u)
{
    float2 uv = float2(HalfTexelOffset(saturate(u)),v);
    return bakedTexture.SampleLevel(samplerbakedTexture,uv,0);
}

float SampleCurve(float4 curveData,float u)
{
    float uNorm = (u * curveData.x) + curveData.y;

#if defined(SHADER_API_METAL)
    // Workaround metal compiler crash that is caused by switch statement uint byte shift
    switch(asint(curveData.w) >> 2)
#else
    switch(asuint(curveData.w) >> 2)
#endif
    {
        case 1: uNorm = HalfTexelOffset(frac(min(1.0f - 1e-10f,uNorm))); break; // clamp end. Dont clamp at 1 or else the frac will make it 0...
        case 2: uNorm = HalfTexelOffset(frac(max(0.0f,uNorm))); break; // clamp start
        case 3: uNorm = HalfTexelOffset(saturate(uNorm)); break; // clamp both
    }
    return bakedTexture.SampleLevel(samplerbakedTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];
}

///////////
// Utils //
///////////

// Invert 3D transformation matrix (not perspective). Adapted from graphics gems 2.
// Inverts upper left by calculating its determinant and multiplying it to the symmetric
// adjust matrix of each element. Finally deals with the translation by transforming the
// original translation using by the calculated inverse.
//https://github.com/erich666/GraphicsGems/blob/master/gemsii/inverse.c
float4x4 VFXInverseTRSMatrix(float4x4 input)
{
    float4x4 output = (float4x4)0;

    //Fill output with cofactor
    output._m00 = input._m11 * input._m22 - input._m21 * input._m12;
    output._m01 = input._m21 * input._m02 - input._m01 * input._m22;
    output._m02 = input._m01 * input._m12 - input._m11 * input._m02;
    output._m10 = input._m20 * input._m12 - input._m10 * input._m22;
    output._m11 = input._m00 * input._m22 - input._m20 * input._m02;
    output._m12 = input._m10 * input._m02 - input._m00 * input._m12;
    output._m20 = input._m10 * input._m21 - input._m20 * input._m11;
    output._m21 = input._m20 * input._m01 - input._m00 * input._m21;
    output._m22 = input._m00 * input._m11 - input._m10 * input._m01;

    //Multiply by reciprocal determinant
    float det = determinant((float3x3)input);
    output *= rcp(det);

    // Do the translation part
    output._m03_m13_m23 = -mul((float3x3)output, input._m03_m13_m23);
    output._m33 = 1.0f;

    return output;
}

float3x3 GetScaleMatrix(float3 scale)
{
    return float3x3(scale.x,    0,          0,
                    0,          scale.y,    0,
                    0,          0,          scale.z);
}

float3x3 GetRotationMatrix(float3 axis,float angle)
{
    float2 sincosA;
    sincos(angle, sincosA.x, sincosA.y);
    const float c = sincosA.y;
    const float s = sincosA.x;
    const float t = 1.0 - c;
    const float x = axis.x;
    const float y = axis.y;
    const float z = axis.z;

    return float3x3(t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
                    t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
                    t * x * z - s * y,  t * y * z + s * x,  t * z * z + c);
}

float3x3 GetEulerMatrix(float3 angles)
{
    float3 s,c;
    sincos(angles, s, c);

    return float3x3(c.y * c.z + s.x * s.y * s.z,    c.z * s.x * s.y - c.y * s.z,    c.x * s.y,
                    c.x * s.z,                      c.x * c.z,                      -s.x,
                    -c.z * s.y + c.y * s.x * s.z,   c.y * c.z * s.x + s.y * s.z,    c.x * c.y);
}

float4x4 GetTRSMatrix(float3 pos, float3 angles, float3 scale)
{
    float3x3 rotAndScale = GetEulerMatrix(radians(angles));
    rotAndScale = mul(rotAndScale,GetScaleMatrix(scale));
    return float4x4(
        float4(rotAndScale[0],pos.x),
        float4(rotAndScale[1],pos.y),
        float4(rotAndScale[2],pos.z),
        float4(0,0,0,1));
}

float4x4 GetElementToVFXMatrix(float3 axisX,float3 axisY,float3 axisZ,float3x3 rot,float3 pivot,float3 size,float3 pos)
{
    float3x3 rotAndScale = GetScaleMatrix(size);
    rotAndScale = mul(rot,rotAndScale);
    rotAndScale = mul(transpose(float3x3(axisX,axisY,axisZ)),rotAndScale);
    pos -= mul(rotAndScale,pivot);
    return float4x4(
        float4(rotAndScale[0],pos.x),
        float4(rotAndScale[1],pos.y),
        float4(rotAndScale[2],pos.z),
        float4(0,0,0,1));
}

float4x4 GetElementToVFXMatrix(float3 axisX,float3 axisY,float3 axisZ,float3 angles,float3 pivot,float3 size,float3 pos)
{
    float3x3 rot = GetEulerMatrix(radians(angles));
    return GetElementToVFXMatrix(axisX,axisY,axisZ,rot,pivot,size,pos);
}

// VFXToMatrix for normals (with invert size). TODO Should use inverse transpose but it only works for orthonormal basis atm
float3x3 GetElementToVFXMatrixNormal(float3 axisX,float3 axisY,float3 axisZ,float3 angles,float3 size)
{
	return (float3x3)GetElementToVFXMatrix(axisX,axisY,axisZ,angles,float3(0,0,0),rcp(size),float3(0,0,0));
}

float4x4 GetVFXToElementMatrix(float3 axisX,float3 axisY,float3 axisZ,float3 angles,float3 pivot,float3 size,float3 pos)
{
    float3x3 rotAndScale = float3x3(axisX,axisY,axisZ); // Works only for orthonormal basis
    rotAndScale = mul(transpose(GetEulerMatrix(radians(angles))),rotAndScale);
    rotAndScale = mul(GetScaleMatrix(rcp(size)),rotAndScale);
    pos = pivot - mul(rotAndScale,pos);
    return float4x4(
        float4(rotAndScale[0],pos.x),
        float4(rotAndScale[1],pos.y),
        float4(rotAndScale[2],pos.z),
        float4(0,0,0,1));
}

float3 VFXSafeNormalize(float3 v)
{
    float sqrLength = max(VFX_FLT_MIN,dot(v,v));
    return v * rsqrt(sqrLength);
}

/////////////////////
// flipbooks utils //
/////////////////////

struct VFXUVData
{
    float4 uvs;
    float  blend;
    float4 mvs;
};

float4 SampleTexture(VFXSampler2D s, VFXUVData uvData)
{
    float4 s0 = s.t.Sample(s.s, uvData.uvs.xy + uvData.mvs.xy);
    float4 s1 = s.t.Sample(s.s, uvData.uvs.zw + uvData.mvs.zw);
    return lerp(s0, s1, uvData.blend);
}

float3 SampleNormalMap(VFXSampler2D s, VFXUVData uvData)
{
    float4 packedNormal = SampleTexture(s, uvData);
    packedNormal.w *= packedNormal.x;
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

float2 GetSubUV(int flipBookIndex, float2 uv, float2 dim, float2 invDim)
{
    float2 tile = float2(fmod(flipBookIndex, dim.x), dim.y - 1.0 - floor(flipBookIndex * invDim.x));
    return (tile + uv) * invDim;
}

VFXUVData GetUVData(float2 uv) // no flipbooks
{
    VFXUVData data = (VFXUVData)0;
    data.uvs.xy = uv;
    return data;
}

VFXUVData GetUVData(float2 flipBookSize, float2 invFlipBookSize, float2 uv, float texIndex) // with flipbooks
{
    VFXUVData data = (VFXUVData)0;

    float frameBlend = frac(texIndex);
    float frameIndex = texIndex - frameBlend;

    data.uvs.xy = GetSubUV(frameIndex, uv, flipBookSize, invFlipBookSize);
#if USE_FLIPBOOK_INTERPOLATION
    data.uvs.zw = GetSubUV(frameIndex + 1, uv, flipBookSize, invFlipBookSize);
    data.blend = frameBlend;
#endif

    return data;
}

VFXUVData GetUVData(float2 flipBookSize, float2 uv, float texIndex)
{
    return GetUVData(flipBookSize, 1.0f / flipBookSize, uv, texIndex);
}


///////////
// Noise //
///////////

#include "VFXNoise.hlsl"

////////////
// Strips //
////////////

#include "VFXParticleStripCommon.hlsl"
