#ifndef UNITY_TEXTURE_INCLUDED
#define UNITY_TEXTURE_INCLUDED

#ifdef SHADER_API_GLES
    #define UNITY_BARE_SAMPLER(n) GLES2UnsupportedSamplerState n
#else
    #define UNITY_BARE_SAMPLER(n) SAMPLER(n)
#endif

struct GLES2UnsupportedSamplerState
{
};

UNITY_BARE_SAMPLER(default_sampler_Linear_Repeat);

struct UnitySamplerState
{
    UNITY_BARE_SAMPLER(samplerstate);
};

#ifdef SHADER_API_GLES
    #define UnityBuildSamplerStateStruct(n) UnityBuildSamplerStateStructInternal()
#else
    #define UnityBuildSamplerStateStruct(n) UnityBuildSamplerStateStructInternal(n)
#endif

UnitySamplerState UnityBuildSamplerStateStructInternal(SAMPLER(samplerstate))
{
    UnitySamplerState result;
#ifndef SHADER_API_GLES
    result.samplerstate = samplerstate;
#endif
    return result;
}

struct UnityTexture2D
{
    TEXTURE2D(tex);
    UNITY_BARE_SAMPLER(samplerstate);
    float4 texelSize;
    float4 scaleTranslate;

    // these functions allows users to convert code using Texture2D to UnityTexture2D by simply changing the type of the variable
    // the existing texture macros will call these functions, which will forward the call to the texture appropriately
    float4 Sample(UnitySamplerState s, float2 uv)                       { return SAMPLE_TEXTURE2D(tex, s.samplerstate, uv); }
    float4 SampleLevel(UnitySamplerState s, float2 uv, float lod)       { return SAMPLE_TEXTURE2D_LOD(tex, s.samplerstate, uv, lod); }
    float4 SampleBias(UnitySamplerState s, float2 uv, float bias)       { return SAMPLE_TEXTURE2D_BIAS(tex, s.samplerstate, uv, bias); }
    float4 SampleGrad(UnitySamplerState s, float2 uv, float2 dpdx, float2 dpdy) { return SAMPLE_TEXTURE2D_GRAD(tex, s.samplerstate, uv, dpdx, dpdy); }

    float2 GetTransformedUV(float2 uv)                                  { return uv * scaleTranslate.xy + scaleTranslate.zw; }

#ifndef SHADER_API_GLES
    float CalculateLevelOfDetail(UnitySamplerState s, float2 uv)        { return CALCULATE_TEXTURE2D_LOD(tex, s.samplerstate, uv); }

    float4 Sample(SAMPLER(s), float2 uv)                                { return SAMPLE_TEXTURE2D(tex, s, uv); }
    float4 SampleLevel(SAMPLER(s), float2 uv, float lod)                { return SAMPLE_TEXTURE2D_LOD(tex, s, uv, lod); }
    float4 SampleBias(SAMPLER(s), float2 uv, float bias)                { return SAMPLE_TEXTURE2D_BIAS(tex, s, uv, bias); }
    float4 SampleGrad(SAMPLER(s), float2 uv, float2 dpdx, float2 dpdy)  { return SAMPLE_TEXTURE2D_GRAD(tex, s, uv, dpdx, dpdy); }
    float4 SampleCmpLevelZero(SAMPLER_CMP(s), float2 uv, float cmp)     { return SAMPLE_TEXTURE2D_SHADOW(tex, s, float3(uv, cmp)); }
    float4 Load(int3 pixel)                                             { return LOAD_TEXTURE2D_LOD(tex, pixel.xy, pixel.z); }
    float CalculateLevelOfDetail(SAMPLER(s), float2 uv)                 { return CALCULATE_TEXTURE2D_LOD(tex, s, uv); }
#endif

#ifdef PLATFORM_SUPPORT_GATHER
    float4 Gather(UnitySamplerState s, float2 uv)                       { return GATHER_TEXTURE2D(tex, s.samplerstate, uv); }
    float4 GatherRed(UnitySamplerState s, float2 uv)                    { return GATHER_RED_TEXTURE2D(tex, s.samplerstate, uv); }
    float4 GatherGreen(UnitySamplerState s, float2 uv)                  { return GATHER_GREEN_TEXTURE2D(tex, s.samplerstate, uv); }
    float4 GatherBlue(UnitySamplerState s, float2 uv)                   { return GATHER_BLUE_TEXTURE2D(tex, s.samplerstate, uv); }
    float4 GatherAlpha(UnitySamplerState s, float2 uv)                  { return GATHER_ALPHA_TEXTURE2D(tex, s.samplerstate, uv); }

    float4 Gather(SAMPLER(s), float2 uv)                                { return GATHER_TEXTURE2D(tex, s, uv);  }
    float4 GatherRed(SAMPLER(s), float2 uv)                             { return GATHER_RED_TEXTURE2D(tex, s, uv); }
    float4 GatherGreen(SAMPLER(s), float2 uv)                           { return GATHER_GREEN_TEXTURE2D(tex, s, uv); }
    float4 GatherBlue(SAMPLER(s), float2 uv)                            { return GATHER_BLUE_TEXTURE2D(tex, s, uv); }
    float4 GatherAlpha(SAMPLER(s), float2 uv)                           { return GATHER_ALPHA_TEXTURE2D(tex, s, uv); }
#endif
};

float4 tex2D(UnityTexture2D tex, float2 uv)                 { return SAMPLE_TEXTURE2D(tex.tex, tex.samplerstate, uv); }
float4 tex2Dlod(UnityTexture2D tex, float4 uv0l)            { return SAMPLE_TEXTURE2D_LOD(tex.tex, tex.samplerstate, uv0l.xy, uv0l.w); }
float4 tex2Dbias(UnityTexture2D tex, float4 uv0b)           { return SAMPLE_TEXTURE2D_BIAS(tex.tex, tex.samplerstate, uv0b.xy, uv0b.w); }

#define UnityBuildTexture2DStruct(n) UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler##n), n##_TexelSize, n##_ST)
#define UnityBuildTexture2DStructNoScale(n) UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler##n), n##_TexelSize, float4(1, 1, 0, 0))
UnityTexture2D UnityBuildTexture2DStructInternal(TEXTURE2D_PARAM(tex, samplerstate), float4 texelSize, float4 scaleTranslate)
{
    UnityTexture2D result;
    result.tex = tex;
#ifndef SHADER_API_GLES
    result.samplerstate = samplerstate;
#endif
    result.texelSize = texelSize;
    result.scaleTranslate = scaleTranslate;
    return result;
}


struct UnityTexture2DArray
{
    TEXTURE2D_ARRAY(tex);
    UNITY_BARE_SAMPLER(samplerstate);

    // these functions allows users to convert code using Texture2DArray to UnityTexture2DArray by simply changing the type of the variable
    // the existing texture macros will call these functions, which will forward the call to the texture appropriately
#ifndef SHADER_API_GLES
    float4 Sample(UnitySamplerState s, float3 uv)                               { return SAMPLE_TEXTURE2D_ARRAY(tex, s.samplerstate, uv.xy, uv.z); }
    float4 SampleLevel(UnitySamplerState s, float3 uv, float lod)               { return SAMPLE_TEXTURE2D_ARRAY_LOD(tex, s.samplerstate, uv.xy, uv.z, lod); }
    float4 SampleBias(UnitySamplerState s, float3 uv, float bias)               { return SAMPLE_TEXTURE2D_ARRAY_BIAS(tex, s.samplerstate, uv.xy, uv.z, bias); }
    float4 SampleGrad(UnitySamplerState s, float3 uv, float2 dpdx, float2 dpdy) { return SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, s.samplerstate, uv.xy, uv.z, dpdx, dpdy); }

    float4 Sample(SAMPLER(s), float3 uv)                                        { return SAMPLE_TEXTURE2D_ARRAY(tex, s, uv.xy, uv.z); }
    float4 SampleLevel(SAMPLER(s), float3 uv, float lod)                        { return SAMPLE_TEXTURE2D_ARRAY_LOD(tex, s, uv.xy, uv.z, lod); }
    float4 SampleBias(SAMPLER(s), float3 uv, float bias)                        { return SAMPLE_TEXTURE2D_ARRAY_BIAS(tex, s, uv.xy, uv.z, bias); }
    float4 SampleGrad(SAMPLER(s), float3 uv, float2 dpdx, float2 dpdy)          { return SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, s, uv.xy, uv.z, dpdx, dpdy); }
    float4 SampleCmpLevelZero(SAMPLER_CMP(s), float3 uv, float cmp)             { return SAMPLE_TEXTURE2D_ARRAY_SHADOW(tex, s, float3(uv.xy, cmp), uv.z); }
    float4 Load(int4 pixel)                                                     { return LOAD_TEXTURE2D_ARRAY(tex, pixel.xy, pixel.z); }
#endif
};

#define UnityBuildTexture2DArrayStruct(n) UnityBuildTexture2DArrayStructInternal(TEXTURE2D_ARRAY_ARGS(n, sampler##n))
UnityTexture2DArray UnityBuildTexture2DArrayStructInternal(TEXTURE2D_ARRAY_PARAM(tex, samplerstate))
{
    UnityTexture2DArray result;
    result.tex = tex;
#ifndef SHADER_API_GLES
    result.samplerstate = samplerstate;
#endif
    return result;
}


struct UnityTextureCube
{
    TEXTURECUBE(tex);
    UNITY_BARE_SAMPLER(samplerstate);

    // these functions allows users to convert code using TextureCube to UnityTextureCube by simply changing the type of the variable
    // the existing texture macros will call these functions, which will forward the call to the texture appropriately
    float4 Sample(UnitySamplerState s, float3 dir)                      { return SAMPLE_TEXTURECUBE(tex, s.samplerstate, dir); }
    float4 SampleLevel(UnitySamplerState s, float3 dir, float lod)      { return SAMPLE_TEXTURECUBE_LOD(tex, s.samplerstate, dir, lod); }
    float4 SampleBias(UnitySamplerState s, float3 dir, float bias)      { return SAMPLE_TEXTURECUBE_BIAS(tex, s.samplerstate, dir, bias); }

#ifndef SHADER_API_GLES
    float4 Sample(SAMPLER(s), float3 dir)                               { return SAMPLE_TEXTURECUBE(tex, s, dir); }
    float4 SampleLevel(SAMPLER(s), float3 dir, float lod)               { return SAMPLE_TEXTURECUBE_LOD(tex, s, dir, lod); }
    float4 SampleBias(SAMPLER(s), float3 dir, float bias)               { return SAMPLE_TEXTURECUBE_BIAS(tex, s, dir, bias); }
#endif

#ifdef PLATFORM_SUPPORT_GATHER
    float4 Gather(UnitySamplerState s, float3 dir)                      { return GATHER_TEXTURECUBE(tex, s.samplerstate, dir); }
    float4 Gather(SAMPLER(s), float3 dir)                               { return GATHER_TEXTURECUBE(tex, s, dir);  }
#endif
};

float4 texCUBE(UnityTextureCube tex, float3 dir)                        { return SAMPLE_TEXTURECUBE(tex.tex, tex.samplerstate, dir); }
float4 texCUBEbias(UnityTextureCube tex, float4 dirBias)                { return SAMPLE_TEXTURECUBE_BIAS(tex.tex, tex.samplerstate, dirBias.xyz, dirBias.w); }

#define UnityBuildTextureCubeStruct(n) UnityBuildTextureCubeStructInternal(TEXTURECUBE_ARGS(n, sampler##n))
UnityTextureCube UnityBuildTextureCubeStructInternal(TEXTURECUBE_PARAM(tex, samplerstate))
{
    UnityTextureCube result;
    result.tex = tex;
#ifndef SHADER_API_GLES
    result.samplerstate = samplerstate;
#endif
    return result;
}


struct UnityTexture3D
{
    TEXTURE3D(tex);
    UNITY_BARE_SAMPLER(samplerstate);

    // these functions allows users to convert code using Texture3D to UnityTexture3D by simply changing the type of the variable
    // the existing texture macros will call these functions, which will forward the call to the texture appropriately
    float4 Sample(UnitySamplerState s, float3 uvw)                      { return SAMPLE_TEXTURE3D(tex, s.samplerstate, uvw); }

#ifndef SHADER_API_GLES
    float4 SampleLevel(UnitySamplerState s, float3 uvw, float lod)      { return SAMPLE_TEXTURE3D_LOD(tex, s.samplerstate, uvw, lod); }

    float4 Sample(SAMPLER(s), float3 uvw)                               { return SAMPLE_TEXTURE2D(tex, s, uvw); }
    float4 SampleLevel(SAMPLER(s), float3 uvw, float lod)               { return SAMPLE_TEXTURE2D_LOD(tex, s, uvw, lod); }
    float4 Load(int4 pixel)                                             { return LOAD_TEXTURE3D_LOD(tex, pixel.xyz, pixel.w); }
#endif
};

float4 tex3D(UnityTexture3D tex, float3 uvw)                            { return SAMPLE_TEXTURE3D(tex.tex, tex.samplerstate, uvw); }

#define UnityBuildTexture3DStruct(n) UnityBuildTexture3DStructInternal(TEXTURE3D_ARGS(n, sampler##n))
UnityTexture3D UnityBuildTexture3DStructInternal(TEXTURE3D_PARAM(tex, samplerstate))
{
    UnityTexture3D result;
    result.tex = tex;
#ifndef SHADER_API_GLES
    result.samplerstate = samplerstate;
#endif
    return result;
}

#endif // UNITY_TEXTURE_INCLUDED
