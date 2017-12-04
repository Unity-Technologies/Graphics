//Helper to disable bounding box compute code
#define USE_DYNAMIC_AABB 1

// Special semantics for VFX blocks
#define RAND randLcg(seed)
#define RAND2 float2(RAND,RAND)
#define RAND3 float3(RAND,RAND,RAND)
#define RAND4 float4(RAND,RAND,RAND,RAND)
#define FIXED_RAND(s) FixedRand4(particleId ^ s).x
#define FIXED_RAND2(s) FixedRand4(particleId ^ s).xy
#define FIXED_RAND3(s) FixedRand4(particleId ^ s).xyz
#define FIXED_RAND4(s) FixedRand4(particleId ^ s).xyzw
#define KILL {kill = true;}
#define SAMPLE sampleSignal
#define SAMPLE_SPLINE_POSITION(v,u) sampleSpline(v.x,u)
#define SAMPLE_SPLINE_TANGENT(v,u) sampleSpline(v.y,u)
#define INVERSE(m) Inv##m

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

// indices to access to system data
#define VFX_DATA_UPDATE_ARG_GROUP_X     0
#define VFX_DATA_RENDER_ARG_NB_INDEX    4
#define VFX_DATA_RENDER_ARG_NB_INSTANCE 5
#define VFX_DATA_NB_CURRENT             8
#define VFX_DATA_NB_INIT                9
#define VFX_DATA_NB_UPDATE              10
#define VFX_DATA_NB_FREE                11

#ifdef VFX_WORLD_SPACE
float3 TransformPositionVFXToWorld(float3 pos)  { return pos; }
float4 TransformPositionVFXToClip(float3 pos)   { return VFXTransformPositionWorldToClip(pos); }
float3x3 GetVFXToViewRotMatrix()                { return VFXGetWorldToViewRotMatrix(); }
float3 GetViewVFXPosition()                     { return VFXGetViewWorldPosition(); }
#else
float3 TransformPositionVFXToWorld(float3 pos)  { return mul(VFXGetObjectToWorldMatrix(),float4(pos,1.0f)).xyz; }
float4 TransformPositionVFXToClip(float3 pos)   { return VFXTransformPositionObjectToClip(pos); }
float3x3 GetVFXToViewRotMatrix()                { return mul(VFXGetWorldToViewRotMatrix(),(float3x3)VFXGetObjectToWorldMatrix()); }
float3 GetViewVFXPosition()                     { return mul(VFXGetWorldToObjectMatrix(),float4(VFXGetViewWorldPosition(),1.0f)).xyz; }
#endif

#define VFX_SAMPLER(name) GetVFXSampler(##name,sampler##name)

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

uint WangHash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

float randLcg(inout uint seed)
{
    uint multiplier = 0x0019660d;
    uint increment = 0x3c6ef35f;
#if 1
    seed = multiplier * seed + increment;
    return asfloat((seed >> 9) | 0x3f800000) - 1.0f;
#else //Using mad24 keeping consitency between platform
    #if defined(SHADER_API_PSSL)
        seed = mad24(multiplier, seed, increment);
    #else
        seed = multiplier * seed + increment;
    #endif
    //Using >> 9 instead of &0x007fffff seems to lead to a better random, but with this way, the result is the same between PS4 & PC
    //We need to find a LCG considering the mul24 operation instead of mul32
    //possible variant : return float(seed & 0x007fffff) / float(0x007fffff)
    return asfloat((seed & 0x007fffff) | 0x3f800000) - 1.0f;
#endif
}

float4 FixedRand4(uint baseSeed)
{
    uint currentSeed = WangHash(baseSeed);
    float4 r;
    [unroll(4)]
    for (uint i=0; i<4; ++i)
    {
        r[i] = randLcg(currentSeed);
    }
    return r;
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
    switch(asuint(curveData.w) >> 2)
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

float3 TransformInElementSpace(float3 offsets,float3 side,float3 up,float3 front,float3x3 rot,float3 pivot,float2 size)
{
    offsets -= pivot;
    offsets.xy *=  size.xy;
    float3 tOffsets = mul(rot,side * offsets.x + up * offsets.y);
    tOffsets += front * offsets.z;
    return tOffsets;
}

float3 TransformInElementSpace(float3 offsets,float3 side,float3 up,float3 front,float angle,float3 pivot,float2 size)
{
    float3x3 rot = GetRotationMatrix(front,radians(angle));
    return TransformInElementSpace(offsets,side,up,front,rot,pivot,size);
}

float2 GetSubUV(int flipBookIndex,float2 uv,float2 dim,float2 invDim)
{
    float2 tile = float2(fmod(flipBookIndex,dim.x),dim.y - 1.0 - floor(flipBookIndex * invDim.x));
    return (tile + uv) * invDim;
}
