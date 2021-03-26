#ifndef HLSL_SUPPORT_SHIM_INCLUDED
#define HLSL_SUPPORT_SHIM_INCLUDED

#define HLSL_SUPPORT_INCLUDED

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

#endif // HLSL_SUPPORT_SHIM_INCLUDED
