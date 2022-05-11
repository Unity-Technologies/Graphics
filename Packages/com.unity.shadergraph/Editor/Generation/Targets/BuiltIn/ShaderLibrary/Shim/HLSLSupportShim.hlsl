#ifndef HLSL_SUPPORT_SHIM_INCLUDED
#define HLSL_SUPPORT_SHIM_INCLUDED

// This file acts as the bridge to avoid including HlslSupport.cginc which contains old definitions for types, duplicate symbols, etc...

// Use the include guard to force HlslSupport.cginc to not get included
#define HLSL_SUPPORT_INCLUDED

// Disable warnings we aren't interested in
#if defined(UNITY_COMPILER_HLSL)
#pragma warning (disable : 3205) // conversion of larger type to smaller
#pragma warning (disable : 3568) // unknown pragma ignored
#pragma warning (disable : 3571) // "pow(f,e) will not work for negative f"; however in majority of our calls to pow we know f is not negative
#pragma warning (disable : 3206) // implicit truncation of vector type
#endif

#if !defined(SHADER_API_GLES)
    // all platforms except GLES2.0 have built-in shadow comparison samplers
    #define SHADOWS_NATIVE
#elif defined(SHADER_API_GLES) && defined(UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS)
    // GLES2.0 also has built-in shadow comparison samplers, but only on platforms where we pass UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS from the editor
    #define SHADOWS_NATIVE
#endif

#define fixed real
#define fixed2 real2
#define fixed3 real3
#define fixed4 real4
#define fixed4x4 real4x4
#define fixed3x3 real3x3
#define fixed2x2 real2x2

#define UNITY_INITIALIZE_OUTPUT(type,name) ZERO_INITIALIZE(type, name)


#define UNITY_PROJ_COORD(a) a
#define UNITY_SAMPLE_DEPTH_TEXTURE(tex, coord) SAMPLE_DEPTH_TEXTURE(tex, sampler##tex, coord)

// 2D textures
#define UNITY_DECLARE_TEX2D(tex) TEXTURE2D(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) TEXTURE2D(tex)
// Not used and doesn't seem to be available in SRP shaders without new macros
//#define UNITY_DECLARE_TEX2D_NOSAMPLER_INT(tex) Texture2D<int4> tex
//#define UNITY_DECLARE_TEX2D_NOSAMPLER_UINT(tex) Texture2D<uint4> tex
#define UNITY_SAMPLE_TEX2D(tex,coord) SAMPLE_TEXTURE2D(tex, sampler##tex, coord)
#define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) SAMPLE_TEXTURE2D_LOD(tex, sampler##tex, coord, lod)
#define UNITY_SAMPLE_TEX2D_SAMPLER(tex, samplertex,coord) SAMPLE_TEXTURE2D(tex, sampler##samplertex, coord)
#define UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex, samplertex, coord, lod) SAMPLE_TEXTURE2D_LOD(tex, sampler##samplertex, coord, lod)

#define UNITY_DECLARE_TEX2D_HALF(tex) TEXTURE2D_HALF(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX2D_FLOAT(tex) TEXTURE2D_FLOAT(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) TEXTURE2D_HALF(tex)
#define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) TEXTURE2D_FLOAT(tex)

// Cubemaps
#define UNITY_DECLARE_TEXCUBE(tex) TEXTURECUBE(tex); SAMPLER(sampler##tex)
#define UNITY_ARGS_TEXCUBE(tex) TEXTURECUBE_PARAM(tex, sampler##tex)
#define UNITY_PASS_TEXCUBE(tex) TEXTURECUBE_ARGS(tex, sampler##tex)
#define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) TEXTURECUBE_ARGS(tex, sampler##samplertex)
#define UNITY_PASS_TEXCUBE_SAMPLER_LOD(tex, samplertex, lod) TEXTURECUBE_ARGS(tex, sampler##samplertex), lod
#define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) TEXTURECUBE(tex)
#define UNITY_SAMPLE_TEXCUBE(tex,coord) SAMPLE_TEXTURECUBE(tex, sampler##tex, coord)
#define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) SAMPLE_TEXTURECUBE_LOD(tex, sampler##tex, coord, lod)
#define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) SAMPLE_TEXTURECUBE_LOD(tex, sampler##samplertex, coord)
#define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex, samplertex, coord, lod) SAMPLE_TEXTURECUBE_LOD(tex, sampler##samplertex, coord, lod)

// 3D textures
#define UNITY_DECLARE_TEX3D(tex) TEXTURE3D(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) TEXTURE3D(tex)
#define UNITY_SAMPLE_TEX3D(tex,coord) SAMPLE_TEXTURE3D(tex, sampler##tex, coord)
#define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) SAMPLE_TEXTURE3D_LOD(tex, sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) SAMPLE_TEXTURE3D(tex, sampler##samplertex, coord)
#define UNITY_SAMPLE_TEX3D_SAMPLER_LOD(tex, samplertex, coord, lod) SAMPLE_TEXTURE3D_LOD(tex, sampler##samplertex, coord, lod)
#define UNITY_DECLARE_TEX3D_FLOAT(tex) TEXTURE3D_FLOAT(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX3D_HALF(tex) TEXTURE3D_HALF(tex); SAMPLER(sampler##tex)

// 2D arrays
//#define UNITY_DECLARE_TEX2DARRAY_MS(tex) Texture2DMSArray<float> tex; SamplerState sampler##tex
//#define UNITY_DECLARE_TEX2DARRAY_MS_NOSAMPLER(tex) Texture2DArray<float> tex
#define UNITY_DECLARE_TEX2DARRAY(tex) TEXTURE2D_ARRAY(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) TEXTURE2D_ARRAY(tex)
#define UNITY_ARGS_TEX2DARRAY(tex) TEXTURE2D_ARRAY_PARAM(tex, sampler##tex)
#define UNITY_PASS_TEX2DARRAY(tex) TEXTURE2D_ARRAY_ARGS(tex, sampler##tex)
#define UNITY_SAMPLE_TEX2DARRAY(tex,coord) SAMPLE_TEXTURE2D_ARRAY(tex, sampler##tex, coord)
#define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##tex, coord, lod)
#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) SAMPLE_TEXTURE2D_ARRAY(tex, sampler##samplertex, coord)
#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler##samplertex, coord, lod)

// Cube arrays
#define UNITY_DECLARE_TEXCUBEARRAY(tex) TEXTURECUBE_ARRAY(tex); SAMPLER(sampler##tex)
#define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) TEXTURECUBE_ARRAY(tex)
#define UNITY_ARGS_TEXCUBEARRAY(tex) TEXTURECUBE_ARRAY_PARAM(tex, sampler##tex)
#define UNITY_PASS_TEXCUBEARRAY(tex) TEXTURECUBE_ARRAY_ARGS(tex, sampler##tex)

#if defined(SHADER_API_PSSL)
    // round the layer index to get DX11-like behaviour (otherwise fractional indices result in mixed up cubemap faces)
    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,float4((coord).xyz, round((coord).w)))
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,float4((coord).xyz, round((coord).w)), lod)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,float4((coord).xyz, round((coord).w)))
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,float4((coord).xyz, round((coord).w)), lod)
#else
    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)
#endif


// Macros to declare and sample shadow maps.
//
// UNITY_DECLARE_SHADOWMAP declares a shadowmap.
// UNITY_SAMPLE_SHADOW samples with a float3 coordinate (UV in xy, Z in z) and returns 0..1 scalar result.
// UNITY_SAMPLE_SHADOW_PROJ samples with a projected coordinate (UV and Z divided by w).
#define UNITY_DECLARE_SHADOWMAP(tex) TEXTURE2D_SHADOW(tex); SAMPLER_CMP(sampler##tex)
#define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) TEXTURECUBE_SHADOW(tex); SAMPLER_CMP(sampler##tex)
#define UNITY_SAMPLE_SHADOW(tex,coord) SAMPLE_TEXTURE2D_SHADOW(tex, sampler##tex, coord)
#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) SAMPLE_TEXTURE2D_SHADOW(tex, sampler##tex, (coord.xyz / coord.w))

#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
    // GLSL does not have textureLod(samplerCubeShadow, ...) support. GLES2 does not have core support for samplerCubeShadow, so we ignore it.
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmp (sampler##tex,(coord).xyz,(coord).w)
#else
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) SAMPLE_TEXTURECUBE_SHADOW(tex, sampler##tex, coord)
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

    #undef UNITY_DECLARE_DEPTH_TEXTURE_MS
    #define UNITY_DECLARE_DEPTH_TEXTURE_MS(tex)  UNITY_DECLARE_TEX2DARRAY_MS (tex)

    #undef UNITY_DECLARE_DEPTH_TEXTURE
    #define UNITY_DECLARE_DEPTH_TEXTURE(tex) UNITY_DECLARE_TEX2DARRAY (tex)

    #undef SAMPLE_DEPTH_TEXTURE
    #define SAMPLE_DEPTH_TEXTURE(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x, (uv).y, (float)unity_StereoEyeIndex)).r

    #undef SAMPLE_DEPTH_TEXTURE_PROJ
    #define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex)).r

    #undef SAMPLE_DEPTH_TEXTURE_LOD
    #define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) UNITY_SAMPLE_TEX2DARRAY_LOD(sampler, float3((uv).xy, (float)unity_StereoEyeIndex), (uv).w).r

    #undef SAMPLE_RAW_DEPTH_TEXTURE
    #define SAMPLE_RAW_DEPTH_TEXTURE(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3((uv).xy, (float)unity_StereoEyeIndex))

    #undef SAMPLE_RAW_DEPTH_TEXTURE_PROJ
    #define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) UNITY_SAMPLE_TEX2DARRAY(sampler, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex))

    #undef SAMPLE_RAW_DEPTH_TEXTURE_LOD
    #define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) UNITY_SAMPLE_TEX2DARRAY_LOD(sampler, float3((uv).xy, (float)unity_StereoEyeIndex), (uv).w)

    #define UNITY_DECLARE_SCREENSPACE_SHADOWMAP UNITY_DECLARE_TEX2DARRAY
    #define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) UNITY_SAMPLE_TEX2DARRAY( tex, float3((uv).x/(uv).w, (uv).y/(uv).w, (float)unity_StereoEyeIndex) ).r

    #define UNITY_DECLARE_SCREENSPACE_TEXTURE UNITY_DECLARE_TEX2DARRAY
    #define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3((uv).xy, (float)unity_StereoEyeIndex))
#else
    #define UNITY_DECLARE_DEPTH_TEXTURE_MS(tex)  Texture2DMS<float> tex;
    #define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
    #define UNITY_DECLARE_SCREENSPACE_SHADOWMAP(tex) sampler2D tex
    #define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) tex2Dproj( tex, UNITY_PROJ_COORD(uv) ).r
    #define UNITY_DECLARE_SCREENSPACE_TEXTURE(tex) sampler2D_float tex;
    #define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) tex2D(tex, uv)
#endif

// DXC no longer supports DX9-style HLSL syntax for sampler2D, tex2D and the like.
// These are emulated for backwards compatibility using our own small structs and functions which manually combine samplers and textures.
#if defined(UNITY_COMPILER_DXC) && !defined(DXC_SAMPLER_COMPATIBILITY)
#define DXC_SAMPLER_COMPATIBILITY 1

// On DXC platforms which don't care about explicit sampler precison we want the emulated types to work directly e.g without needing to redefine 'sampler2D' to 'sampler2D_f'
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH)
    #define sampler1D_f sampler1D
    #define sampler2D_f sampler2D
    #define sampler3D_f sampler3D
    #define samplerCUBE_f samplerCUBE
#endif

struct sampler1D_f      { Texture1D<float4> t; SamplerState s; };
struct sampler2D_f      { Texture2D<float4> t; SamplerState s; };
struct sampler3D_f      { Texture3D<float4> t; SamplerState s; };
struct samplerCUBE_f    { TextureCube<float4> t; SamplerState s; };

float4 tex1D(sampler1D_f x, float v)        { return x.t.Sample(x.s, v); }
float4 tex2D(sampler2D_f x, float2 v)       { return x.t.Sample(x.s, v); }
float4 tex3D(sampler3D_f x, float3 v)       { return x.t.Sample(x.s, v); }
float4 texCUBE(samplerCUBE_f x, float3 v)   { return x.t.Sample(x.s, v); }

float4 tex1Dbias(sampler1D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.x, t.w); }
float4 tex2Dbias(sampler2D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.xy, t.w); }
float4 tex3Dbias(sampler3D_f x, in float4 t)        { return x.t.SampleBias(x.s, t.xyz, t.w); }
float4 texCUBEbias(samplerCUBE_f x, in float4 t)    { return x.t.SampleBias(x.s, t.xyz, t.w); }

float4 tex1Dlod(sampler1D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.x, t.w); }
float4 tex2Dlod(sampler2D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.xy, t.w); }
float4 tex3Dlod(sampler3D_f x, in float4 t)     { return x.t.SampleLevel(x.s, t.xyz, t.w); }
float4 texCUBElod(samplerCUBE_f x, in float4 t) { return x.t.SampleLevel(x.s, t.xyz, t.w); }

float4 tex1Dgrad(sampler1D_f x, float t, float dx, float dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2Dgrad(sampler2D_f x, float2 t, float2 dx, float2 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3Dgrad(sampler3D_f x, float3 t, float3 dx, float3 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBEgrad(samplerCUBE_f x, float3 t, float3 dx, float3 dy) { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1D(sampler1D_f x, float t, float dx, float dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2D(sampler2D_f x, float2 t, float2 dx, float2 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3D(sampler3D_f x, float3 t, float3 dx, float3 dy)     { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBE(samplerCUBE_f x, float3 t, float3 dx, float3 dy) { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1Dproj(sampler1D_f s, in float2 t)        { return tex1D(s, t.x / t.y); }
float4 tex1Dproj(sampler1D_f s, in float4 t)        { return tex1D(s, t.x / t.w); }
float4 tex2Dproj(sampler2D_f s, in float3 t)        { return tex2D(s, t.xy / t.z); }
float4 tex2Dproj(sampler2D_f s, in float4 t)        { return tex2D(s, t.xy / t.w); }
float4 tex3Dproj(sampler3D_f s, in float4 t)        { return tex3D(s, t.xyz / t.w); }
float4 texCUBEproj(samplerCUBE_f s, in float4 t)    { return texCUBE(s, t.xyz / t.w); }

// Half precision emulated samplers used instead the sampler.*_half unity types
struct sampler1D_h      { Texture1D<min16float4> t; SamplerState s; };
struct sampler2D_h      { Texture2D<min16float4> t; SamplerState s; };
struct sampler3D_h      { Texture3D<min16float4> t; SamplerState s; };
struct samplerCUBE_h    { TextureCube<min16float4> t; SamplerState s; };

min16float4 tex1D(sampler1D_h x, float v)       { return x.t.Sample(x.s, v); }
min16float4 tex2D(sampler2D_h x, float2 v)      { return x.t.Sample(x.s, v); }
min16float4 tex3D(sampler3D_h x, float3 v)      { return x.t.Sample(x.s, v); }
min16float4 texCUBE(samplerCUBE_h x, float3 v)  { return x.t.Sample(x.s, v); }

min16float4 tex1Dbias(sampler1D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.x, t.w); }
min16float4 tex2Dbias(sampler2D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.xy, t.w); }
min16float4 tex3Dbias(sampler3D_h x, in float4 t)       { return x.t.SampleBias(x.s, t.xyz, t.w); }
min16float4 texCUBEbias(samplerCUBE_h x, in float4 t)   { return x.t.SampleBias(x.s, t.xyz, t.w); }

min16float4 tex1Dlod(sampler1D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.x, t.w); }
min16float4 tex2Dlod(sampler2D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.xy, t.w); }
min16float4 tex3Dlod(sampler3D_h x, in float4 t)        { return x.t.SampleLevel(x.s, t.xyz, t.w); }
min16float4 texCUBElod(samplerCUBE_h x, in float4 t)    { return x.t.SampleLevel(x.s, t.xyz, t.w); }

min16float4 tex1Dgrad(sampler1D_h x, float t, float dx, float dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex2Dgrad(sampler2D_h x, float2 t, float2 dx, float2 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex3Dgrad(sampler3D_h x, float3 t, float3 dx, float3 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 texCUBEgrad(samplerCUBE_h x, float3 t, float3 dx, float3 dy)    { return x.t.SampleGrad(x.s, t, dx, dy); }

min16float4 tex1D(sampler1D_h x, float t, float dx, float dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex2D(sampler2D_h x, float2 t, float2 dx, float2 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 tex3D(sampler3D_h x, float3 t, float3 dx, float3 dy)        { return x.t.SampleGrad(x.s, t, dx, dy); }
min16float4 texCUBE(samplerCUBE_h x, float3 t, float3 dx, float3 dy)    { return x.t.SampleGrad(x.s, t, dx, dy); }

min16float4 tex1Dproj(sampler1D_h s, in float2 t)       { return tex1D(s, t.x / t.y); }
min16float4 tex1Dproj(sampler1D_h s, in float4 t)       { return tex1D(s, t.x / t.w); }
min16float4 tex2Dproj(sampler2D_h s, in float3 t)       { return tex2D(s, t.xy / t.z); }
min16float4 tex2Dproj(sampler2D_h s, in float4 t)       { return tex2D(s, t.xy / t.w); }
min16float4 tex3Dproj(sampler3D_h s, in float4 t)       { return tex3D(s, t.xyz / t.w); }
min16float4 texCUBEproj(samplerCUBE_h s, in float4 t)   { return texCUBE(s, t.xyz / t.w); }
#endif

// Define "fixed" precision to be half on non-GLSL platforms,
// and sampler*_prec to be just simple samplers.
#if !defined(SHADER_API_GLES) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH)
#define UNITY_FIXED_IS_HALF 1
#define sampler1D_half sampler1D
#define sampler1D_float sampler1D
#define sampler2D_half sampler2D
#define sampler2D_float sampler2D
#define samplerCUBE_half samplerCUBE
#define samplerCUBE_float samplerCUBE
#define sampler3D_float sampler3D
#define sampler3D_half sampler3D
#define Texture2D_half Texture2D
#define Texture2D_float Texture2D
#define Texture2DArray_half Texture2DArray
#define Texture2DArray_float Texture2DArray
#define Texture2DMS_half Texture2DMS
#define Texture2DMS_float Texture2DMS
#define TextureCube_half TextureCube
#define TextureCube_float TextureCube
#define TextureCubeArray_half TextureCubeArray
#define TextureCubeArray_float TextureCubeArray
#define Texture3D_float Texture3D
#define Texture3D_half Texture3D
#endif

#endif // HLSL_SUPPORT_SHIM_INCLUDED
