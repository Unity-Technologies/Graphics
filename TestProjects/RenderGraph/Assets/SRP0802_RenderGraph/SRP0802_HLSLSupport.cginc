// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'
// Upgrade NOTE: replaced 'texRECT' with 'tex2D'
// Upgrade NOTE: replaced 'texRECTbias' with 'tex2Dbias'
// Upgrade NOTE: replaced 'texRECTlod' with 'tex2Dlod'
// Upgrade NOTE: replaced 'texRECTproj' with 'tex2Dproj'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef SRP0802_HLSL_SUPPORT_INCLUDED
#define SRP0802_HLSL_SUPPORT_INCLUDED

#if defined(SHADER_TARGET_SURFACE_ANALYSIS)
    // surface shader analysis is complicated, and is done via two compilers:
    // - Mojoshader for source level analysis (to find out structs/functions with their members & parameters).
    //   This step can understand DX9 style HLSL syntax.
    // - HLSL compiler for "what actually got read & written to" (taking dead code etc into account), via a dummy
    //   compilation and reflection of the shader. This step can understand DX9 & DX11 HLSL syntax.
    // Neither of these compilers are "Cg", but we used to use Cg in the past for this; keep the macro
    // name intact in case some user-written shaders depend on it being that.
    #define UNITY_COMPILER_CG
#elif defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || (defined(UNITY_PREFER_HLSLCC) && defined(SHADER_API_GLES))
    #define UNITY_COMPILER_HLSL
    #define UNITY_COMPILER_HLSLCC
#elif defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE)
    #define UNITY_COMPILER_HLSL
#elif defined(SHADER_TARGET_GLSL)
    #define UNITY_COMPILER_HLSL2GLSL
#else
    #define UNITY_COMPILER_CG
#endif

#if defined(STEREO_MULTIVIEW_ON) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)) && !(defined(SHADER_API_SWITCH))
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if (defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)) && defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Is this shader API able to read the current pixel depth value, be it via
// texture fetch of via renderpass inputs, from the depth buffer while it is
// simultaneously bound as a z-buffer?
// On non-renderpass (fallback impl) platforms it's supported on DX11, GL, desktop Metal
// TODO: Check DX12 and consoles, implement read-only depth if possible
// With native renderpasses, Vulkan is fine but iOS GPU doesn't have the wires connected to read the data.
#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && !defined(SHADER_API_MOBILE))
#define UNITY_SUPPORT_DEPTH_FETCH 1
#endif

#if defined(UNITY_FRAMEBUFFER_FETCH_AVAILABLE) && defined(UNITY_FRAMEBUFFER_FETCH_ENABLED) && defined(UNITY_COMPILER_HLSLCC)
// In the fragment shader, setting inout <type> var : SV_Target would result to
// compiler error, unless SV_Target is defined to COLOR semantic for compatibility
// reasons. Unfortunately, we still need to have a clear distinction between
// vertex shader COLOR output and SV_Target, so the following workaround abuses
// the fact that semantic names are case insensitive and preprocessor macros
// are not. The resulting HLSL bytecode has semantics in case preserving form,
// helps code generator to do extra work required for framebuffer fetch

// You should always declare color inouts against SV_Target
#define SV_Target CoLoR
#define SV_Target0 CoLoR0
#define SV_Target1 CoLoR1
#define SV_Target2 CoLoR2
#define SV_Target3 CoLoR3
#define SV_Target4 CoLoR4
#define SV_Target5 CoLoR5
#define SV_Target6 CoLoR6
#define SV_Target7 CoLoR7

#define COLOR VCOLOR
#define COLOR0 VCOLOR0
#define COLOR1 VCOLOR1
#define COLOR2 VCOLOR2
#define COLOR3 VCOLOR3
#define COLOR4 VCOLOR4
#define COLOR5 VCOLOR5
#define COLOR6 VCOLOR6
#define COLOR7 VCOLOR7

#endif

// SV_Target[n] / SV_Depth defines, if not defined by compiler already
#if !defined(SV_Target)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target COLOR
#   endif
#endif
#if !defined(SV_Target0)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target0 COLOR0
#   endif
#endif
#if !defined(SV_Target1)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target1 COLOR1
#   endif
#endif
#if !defined(SV_Target2)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target2 COLOR2
#   endif
#endif
#if !defined(SV_Target3)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Target3 COLOR3
#   endif
#endif
#if !defined(SV_Target4)
#   if defined(SHADER_API_PSSL)
#       define SV_Target4 S_TARGET_OUTPUT4
#   endif
#endif
#if !defined(SV_Target5)
#   if defined(SHADER_API_PSSL)
#       define SV_Target5 S_TARGET_OUTPUT5
#   endif
#endif
#if !defined(SV_Target6)
#   if defined(SHADER_API_PSSL)
#       define SV_Target6 S_TARGET_OUTPUT6
#   endif
#endif
#if !defined(SV_Target7)
#   if defined(SHADER_API_PSSL)
#       define SV_Target7 S_TARGET_OUTPUT7
#   endif
#endif
#if !defined(SV_Depth)
#   if !defined(SHADER_API_XBOXONE)
#       define SV_Depth DEPTH
#   endif
#endif

#if (defined(SHADER_API_GLES3) && !defined(SHADER_API_DESKTOP)) || defined(SHADER_API_GLES) || defined(SHADER_API_N3DS)
    #define UNITY_ALLOWED_MRT_COUNT 4
#else
    #define UNITY_ALLOWED_MRT_COUNT 8
#endif

#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLES) || defined(SHADER_API_N3DS)
    //no fast coherent dynamic branching on these hardware
#else
    #define UNITY_FAST_COHERENT_DYNAMIC_BRANCHING 1
#endif

// Disable warnings we aren't interested in
#if defined(UNITY_COMPILER_HLSL)
#pragma warning (disable : 3205) // conversion of larger type to smaller
#pragma warning (disable : 3568) // unknown pragma ignored
#pragma warning (disable : 3571) // "pow(f,e) will not work for negative f"; however in majority of our calls to pow we know f is not negative
#pragma warning (disable : 3206) // implicit truncation of vector type
#endif


// Define "fixed" precision to be half on non-GLSL platforms,
// and sampler*_prec to be just simple samplers.
#if !defined(SHADER_TARGET_GLSL) && !(defined(SHADER_API_GLES) && defined(UNITY_COMPILER_HLSLCC)) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !defined(SHADER_API_SWITCH)
#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4
#define fixed4x4 half4x4
#define fixed3x3 half3x3
#define fixed2x2 half2x2
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

#if (defined(SHADER_API_GLES) && defined(UNITY_COMPILER_HLSLCC)) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_MOBILE) && defined(SHADER_API_METAL)) || defined(SHADER_API_SWITCH)
// with HLSLcc, use DX11.1 partial precision for translation
// we specifically define fixed to be float16 (same as half) as all new GPUs seems to agree on float16 being minimal precision float
#define fixed min16float
#define fixed2 min16float2
#define fixed3 min16float3
#define fixed4 min16float4
#define fixed4x4 min16float4x4
#define fixed3x3 min16float3x3
#define fixed2x2 min16float2x2
#define half min16float
#define half2 min16float2
#define half3 min16float3
#define half4 min16float4
#define half2x2 min16float2x2
#define half3x3 min16float3x3
#define half4x4 min16float4x4
#endif

#if (!defined(SHADER_API_MOBILE) && defined(SHADER_API_METAL))
#define fixed float
#define fixed2 float2
#define fixed3 float3
#define fixed4 float4
#define fixed4x4 float4x4
#define fixed3x3 float3x3
#define fixed2x2 float2x2
#define half float
#define half2 float2
#define half3 float3
#define half4 float4
#define half2x2 float2x2
#define half3x3 float3x3
#define half4x4 float4x4
#endif

// Define min16float/min10float to be half/fixed on non-D3D11 platforms.
// This allows people to use min16float and friends in their shader code if they
// really want to (making that will make shaders not load before DX11.1, e.g. on Win7,
// but if they target WSA/WP exclusively that's fine).
#if !defined(SHADER_API_D3D11) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !defined(SHADER_API_METAL) && !(defined(SHADER_API_GLES) && defined(UNITY_COMPILER_HLSLCC)) && !defined(SHADER_API_SWITCH)
#define min16float half
#define min16float2 half2
#define min16float3 half3
#define min16float4 half4
#define min10float fixed
#define min10float2 fixed2
#define min10float3 fixed3
#define min10float4 fixed4
#endif

#if (defined(SHADER_API_GLES) && defined(UNITY_COMPILER_HLSLCC))
#define uint int
#define uint1 int1
#define uint2 int2
#define uint3 int3
#define uint4 int4

#define min16uint int
#define min16uint1 int1
#define min16uint2 int2
#define min16uint3 int3
#define min16uint4 int4

#define uint1x1 int1x1
#define uint1x2 int1x2
#define uint1x3 int1x3
#define uint1x4 int1x4
#define uint2x1 int2x1
#define uint2x2 int2x2
#define uint2x3 int2x3
#define uint2x4 int2x4
#define uint3x1 int3x1
#define uint3x2 int3x2
#define uint3x3 int3x3
#define uint3x4 int3x4
#define uint4x1 int4x1
#define uint4x2 int4x2
#define uint4x3 int4x3
#define uint4x4 int4x4

#define asuint(x) asint(x)
#endif

// specifically for samplers that are provided as arguments to entry functions
#if defined(SHADER_API_PSSL)
#define SAMPLER_UNIFORM uniform
#define SHADER_UNIFORM
#else
#define SAMPLER_UNIFORM
#endif

#if defined(SHADER_API_PSSL)
// variable modifiers
#define nointerpolation nointerp
#define noperspective nopersp

#define CBUFFER_START(name) ConstantBuffer name {
#define CBUFFER_END };
#elif defined(SHADER_API_D3D11)
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#else
// On specific platforms, like OpenGL, GLES3 and Metal, constant buffers may still be used for instancing
#define CBUFFER_START(name)
#define CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) || ((defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)) && (defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL)))
    #define GLOBAL_CBUFFER_START(name)    cbuffer name {
    #define GLOBAL_CBUFFER_END            }
#else
    #define GLOBAL_CBUFFER_START(name)    CBUFFER_START(name)
    #define GLOBAL_CBUFFER_END            CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
// OVR_multiview
// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
#define UNITY_DECLARE_MULTIVIEW(number_of_views) GLOBAL_CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; GLOBAL_CBUFFER_END
#define UNITY_VIEWID gl_ViewID
#endif

// Special declaration macro for requiring the extended blend functionality
#if defined(SHADER_API_GLES3)
// Declare the need for the KHR_blend_equation_advanced extension plus the specific blend mode (see the extension spec for list or "all_equations" for all)
#define UNITY_REQUIRE_ADVANCED_BLEND(mode) uint hlslcc_blend_support_##mode
#else
#define UNITY_REQUIRE_ADVANCED_BLEND(mode)
#endif

#define UNITY_PROJ_COORD(a) a

// Depth texture sampling helpers.
// On most platforms you can just sample them, but some (e.g. PSP2) need special handling.
//
// SAMPLE_DEPTH_TEXTURE(sampler,uv): returns scalar depth
// SAMPLE_DEPTH_TEXTURE_PROJ(sampler,uv): projected sample
// SAMPLE_DEPTH_TEXTURE_LOD(sampler,uv): sample with LOD level

    // Sample depth, just the red component.
// #   define SAMPLE_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv).r)
// #   define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv).r)
// #   define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv).r)
    // Sample depth, all components.
// #   define SAMPLE_RAW_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv))
// #   define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv))
// #   define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv))
// #   define SAMPLE_DEPTH_CUBE_TEXTURE(sampler, uv) (texCUBE(sampler, uv).r)

// Deprecated; use SAMPLE_DEPTH_TEXTURE & SAMPLE_DEPTH_TEXTURE_PROJ instead
//#define UNITY_SAMPLE_DEPTH(value) (value).r


// Macros to declare and sample shadow maps.
//
// UNITY_DECLARE_SHADOWMAP declares a shadowmap.
// UNITY_SAMPLE_SHADOW samples with a float3 coordinate (UV in xy, Z in z) and returns 0..1 scalar result.
// UNITY_SAMPLE_SHADOW_PROJ samples with a projected coordinate (UV and Z divided by w).


#if !defined(SHADER_API_GLES)
    // all platforms except GLES2.0 have built-in shadow comparison samplers
    #define SHADOWS_NATIVE
#elif defined(SHADER_API_GLES) && defined(UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS)
    // GLES2.0 also has built-in shadow comparison samplers, but only on platforms where we pass UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS from the editor
    #define SHADOWS_NATIVE
#endif

#if defined(SHADER_API_D3D11) || (defined(UNITY_COMPILER_HLSLCC) && defined(SHADOWS_NATIVE))
    // DX11 & hlslcc platforms: built-in PCF
    #define UNITY_DECLARE_SHADOWMAP(tex) Texture2D tex; SamplerComparisonState sampler##tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) TextureCube tex; SamplerComparisonState sampler##tex
    #define UNITY_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
    #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
        // GLSL does not have textureLod(samplerCubeShadow, ...) support. GLES2 does not have core support for samplerCubeShadow, so we ignore it.
        #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmp (sampler##tex,(coord).xyz,(coord).w)
    #else
       #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xyz,(coord).w)
    #endif
#elif defined(UNITY_COMPILER_HLSL2GLSL) && defined(SHADOWS_NATIVE)
    // OpenGL-like hlsl2glsl platforms: most of them always have built-in PCF
    #define UNITY_DECLARE_SHADOWMAP(tex) sampler2DShadow tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBEShadow tex
    #define UNITY_SAMPLE_SHADOW(tex,coord) shadow2D (tex,(coord).xyz)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) shadow2Dproj (tex,coord)
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) ((texCUBE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#elif defined(SHADER_API_PSSL)
    // PS4: built-in PCF
    #define UNITY_DECLARE_SHADOWMAP(tex)        Texture2D tex; SamplerComparisonState sampler##tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) TextureCube tex; SamplerComparisonState sampler##tex
    #define UNITY_SAMPLE_SHADOW(tex,coord)      tex.SampleCmpLOD0(sampler##tex,(coord).xy,(coord).z)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLOD0(sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) tex.SampleCmpLOD0(sampler##tex,(coord).xyz,(coord).w)
#else
    // Fallback / No built-in shadowmap comparison sampling: regular texture sample and do manual depth comparison
    #define UNITY_DECLARE_SHADOWMAP(tex) sampler2D_float tex
    #define UNITY_DECLARE_TEXCUBE_SHADOWMAP(tex) samplerCUBE_float tex
    #define UNITY_SAMPLE_SHADOW(tex,coord) ((SAMPLE_DEPTH_TEXTURE(tex,(coord).xy) < (coord).z) ? 0.0 : 1.0)
    #define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) ((SAMPLE_DEPTH_TEXTURE_PROJ(tex,UNITY_PROJ_COORD(coord)) < ((coord).z/(coord).w)) ? 0.0 : 1.0)
    #define UNITY_SAMPLE_TEXCUBE_SHADOW(tex,coord) ((SAMPLE_DEPTH_CUBE_TEXTURE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#endif


// Macros to declare textures and samplers, possibly separately. For platforms
// that have separate samplers & textures (like DX11), and we'd want to conserve
// the samplers.
//  - UNITY_DECLARE_TEX*_NOSAMPLER declares a texture, without a sampler.
//  - UNITY_SAMPLE_TEX*_SAMPLER samples a texture, using sampler from another texture.
//      That another texture must also be actually used in the current shader, otherwise
//      the correct sampler will not be set.
#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))
    // DX11 style HLSL syntax; separate textures and samplers
    //
    // Note: for HLSLcc we have special unity-specific syntax to pass sampler precision information.
    //
    // Note: for surface shader analysis, go into DX11 syntax path when non-mojoshader part of analysis is done,
    // this allows surface shaders to use _NOSAMPLER and similar macros, without using up a sampler register.
    // Don't do that for mojoshader part, as that one can only parse DX9 style HLSL.

    // 2D textures
    #define UNITY_DECLARE_TEX2D(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) Texture2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_INT(tex) Texture2D<int4> tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_UINT(tex) Texture2D<uint4> tex
    #define UNITY_SAMPLE_TEX2D(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

#if defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_GLCORE) // GL Core doesn't have the _half mangling, the rest of them do.
    #define UNITY_DECLARE_TEX2D_HALF(tex) Texture2D_half tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) Texture2D_float tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) Texture2D_half tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) Texture2D_float tex
#else
    #define UNITY_DECLARE_TEX2D_HALF(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) Texture2D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) Texture2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) Texture2D tex
#endif

    // Cubemaps
    #define UNITY_DECLARE_TEXCUBE(tex) TextureCube tex; SamplerState sampler##tex
    #define UNITY_ARGS_TEXCUBE(tex) TextureCube tex, SamplerState sampler##tex
    #define UNITY_PASS_TEXCUBE(tex) tex, sampler##tex
    #define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) tex, sampler##samplertex
    #define UNITY_PASS_TEXCUBE_SAMPLER_LOD(tex, samplertex, lod) tex, sampler##samplertex, lod
    #define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) TextureCube tex
    #define UNITY_SAMPLE_TEXCUBE(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex, samplertex, coord, lod) tex.SampleLevel (sampler##samplertex, coord, lod)
    // 3D textures
    #define UNITY_DECLARE_TEX3D(tex) Texture3D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) Texture3D tex
    #define UNITY_SAMPLE_TEX3D(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEX3D_SAMPLER_LOD(tex, samplertex, coord, lod) tex.SampleLevel(sampler##samplertex, coord, lod)

#if defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_GLCORE) // GL Core doesn't have the _half mangling, the rest of them do.
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) Texture3D_float tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) Texture3D_half tex; SamplerState sampler##tex
#else
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) Texture3D tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) Texture3D tex; SamplerState sampler##tex
#endif

    // 2D arrays
    #define UNITY_DECLARE_TEX2DARRAY_MS(tex) Texture2DMSArray<float> tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2DARRAY_MS_NOSAMPLER(tex) Texture2DArray<float> tex
    #define UNITY_DECLARE_TEX2DARRAY(tex) Texture2DArray tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) Texture2DArray tex
    #define UNITY_ARGS_TEX2DARRAY(tex) Texture2DArray tex, SamplerState sampler##tex
    #define UNITY_PASS_TEX2DARRAY(tex) tex, sampler##tex
    #define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex.Sample (sampler##tex,coord)
    #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
    #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
    #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)

    // Cube arrays
    #define UNITY_DECLARE_TEXCUBEARRAY(tex) TextureCubeArray tex; SamplerState sampler##tex
    #define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) TextureCubeArray tex
    #define UNITY_ARGS_TEXCUBEARRAY(tex) TextureCubeArray tex, SamplerState sampler##tex
    #define UNITY_PASS_TEXCUBEARRAY(tex) tex, sampler##tex
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


#else
    // DX9 style HLSL syntax; same object for texture+sampler
    // 2D textures
    #define UNITY_DECLARE_TEX2D(tex) sampler2D tex
    #define UNITY_DECLARE_TEX2D_HALF(tex) sampler2D_half tex
    #define UNITY_DECLARE_TEX2D_FLOAT(tex) sampler2D_float tex

    #define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) sampler2D tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(tex) sampler2D_half tex
    #define UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(tex) sampler2D_float tex

    #define UNITY_SAMPLE_TEX2D(tex,coord) tex2D (tex,coord)
    #define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex2D (tex,coord)
    // Cubemaps
    #define UNITY_DECLARE_TEXCUBE(tex) samplerCUBE tex
    #define UNITY_ARGS_TEXCUBE(tex) samplerCUBE tex
    #define UNITY_PASS_TEXCUBE(tex) tex
    #define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) tex
    #define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) samplerCUBE tex
    #define UNITY_SAMPLE_TEXCUBE(tex,coord) texCUBE (tex,coord)

    #define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) texCUBElod (tex, half4(coord, lod))
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex,samplertex,coord,lod) UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod)
    #define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) texCUBE (tex,coord)

    // 3D textures
    #define UNITY_DECLARE_TEX3D(tex) sampler3D tex
    #define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) sampler3D tex
    #define UNITY_DECLARE_TEX3D_FLOAT(tex) sampler3D_float tex
    #define UNITY_DECLARE_TEX3D_HALF(tex) sampler3D_float tex
    #define UNITY_SAMPLE_TEX3D(tex,coord) tex3D (tex,coord)
    #define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) tex3D (tex,float4(coord,lod))
    #define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) tex3D (tex,coord)
    #define UNITY_SAMPLE_TEX3D_SAMPLER_LOD(tex,samplertex,coord,lod) tex3D (tex,float4(coord,lod))

    // 2D array syntax for hlsl2glsl and surface shader analysis
    #if defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
        #define UNITY_DECLARE_TEX2DARRAY(tex) sampler2DArray tex
        #define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) sampler2DArray tex
        #define UNITY_ARGS_TEX2DARRAY(tex) sampler2DArray tex
        #define UNITY_PASS_TEX2DARRAY(tex) tex
        #define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex2DArray (tex,coord)
        #define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex2DArraylod (tex, float4(coord,lod))
        #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex2DArray (tex,coord)
        #define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex2DArraylod (tex, float4(coord,lod))
    #endif

    // surface shader analysis; just pretend that 2D arrays are cubemaps
    #if defined(SHADER_TARGET_SURFACE_ANALYSIS)
        #define sampler2DArray samplerCUBE
        #define tex2DArray texCUBE
        #define tex2DArraylod texCUBElod
    #endif

#endif

// For backwards compatibility, so we won't accidentally break shaders written by user
#define SampleCubeReflection(env, dir, lod) UNITY_SAMPLE_TEXCUBE_LOD(env, dir, lod)


#define sampler2D sampler2D
#define tex2D tex2D
#define tex2Dlod tex2Dlod
#define tex2Dbias tex2Dbias
#define tex2Dproj tex2Dproj

#if defined(SHADER_API_PSSL)
#define VPOS            S_POSITION
#elif defined(UNITY_COMPILER_CG)
// Cg seems to use WPOS instead of VPOS semantic?
#define VPOS WPOS
// Cg does not have tex2Dgrad and friends, but has tex2D overload that
// can take the derivatives
#define tex2Dgrad tex2D
#define texCUBEgrad texCUBE
#define tex3Dgrad tex3D
#endif


// Data type to be used for "screen space position" pixel shader input semantic; just a float4 now (used to be float2 when on D3D9)
#define UNITY_VPOS_TYPE float4



#if defined(UNITY_COMPILER_HLSL) || defined (SHADER_TARGET_GLSL)
#define FOGC FOG
#endif

// Use VFACE pixel shader input semantic in your shaders to get front-facing scalar value.
// Requires shader model 3.0 or higher.
// Back when D3D9 existed UNITY_VFACE_AFFECTED_BY_PROJECTION macro used to be defined there too.
#if defined(UNITY_COMPILER_CG)
#define VFACE FACE
#endif
#if defined(UNITY_COMPILER_HLSL2GLSL)
#define FACE VFACE
#endif
#if defined(SHADER_API_PSSL)
#define VFACE S_FRONT_FACE
#endif


#if !defined(SHADER_API_D3D11) && !defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_PSSL)
#define SV_POSITION POSITION
#endif


// On D3D reading screen space coordinates from fragment shader requires SM3.0
#define UNITY_POSITION(pos) float4 pos : SV_POSITION

// Kept for backwards-compatibility
#define UNITY_ATTEN_CHANNEL r

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
#define UNITY_UV_STARTS_AT_TOP 1
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
// D3D style platforms where clip space z is [0, 1].
#define UNITY_REVERSED_Z 1
#endif

#if defined(UNITY_REVERSED_Z)
#define UNITY_NEAR_CLIP_VALUE (1.0)
#else
#define UNITY_NEAR_CLIP_VALUE (-1.0)
#endif

// "platform caps" defines that were moved to editor, so they are set automatically when compiling shader
// UNITY_NO_DXT5nm              - no DXT5NM support, so normal maps will encoded in rgb
// UNITY_NO_RGBM                - no RGBM support, so doubleLDR
// UNITY_NO_SCREENSPACE_SHADOWS - no screenspace cascaded shadowmaps
// UNITY_FRAMEBUFFER_FETCH_AVAILABLE    - framebuffer fetch
// UNITY_ENABLE_REFLECTION_BUFFERS - render reflection probes in deferred way, when using deferred shading


// On most platforms, use floating point render targets to store depth of point
// light shadowmaps. However, on some others they either have issues, or aren't widely
// supported; in which case fallback to encoding depth into RGBA channels.
// Make sure this define matches GraphicsCaps.useRGBAForPointShadows.
#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
#define UNITY_USE_RGBA_FOR_POINT_SHADOWS
#endif


// Initialize arbitrary structure with zero values.
// Not supported on some backends (e.g. Cg-based particularly with nested structs).
// hlsl2glsl would almost support it, except with structs that have arrays -- so treat as not supported there either :(
#if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
#define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
#else
#define UNITY_INITIALIZE_OUTPUT(type,name)
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL)
#define UNITY_CAN_COMPILE_TESSELLATION 1
#   define UNITY_domain                 domain
#   define UNITY_partitioning           partitioning
#   define UNITY_outputtopology         outputtopology
#   define UNITY_patchconstantfunc      patchconstantfunc
#   define UNITY_outputcontrolpoints    outputcontrolpoints
#endif

// Not really needed anymore, but did ship in Unity 4.0; with D3D11_9X remapping them to .r channel.
// Now that's not used.
#define UNITY_SAMPLE_1CHANNEL(x,y) tex2D(x,y).a
#define UNITY_ALPHA_CHANNEL a


// HLSL attributes
#if defined(UNITY_COMPILER_HLSL)
    #define UNITY_BRANCH    [branch]
    #define UNITY_FLATTEN   [flatten]
    #define UNITY_UNROLL    [unroll]
    #define UNITY_LOOP      [loop]
    #define UNITY_FASTOPT   [fastopt]
#else
    #define UNITY_BRANCH
    #define UNITY_FLATTEN
    #define UNITY_UNROLL
    #define UNITY_LOOP
    #define UNITY_FASTOPT
#endif


// Unity 4.x shaders used to mostly work if someone used WPOS semantic,
// which was accepted by Cg. The correct semantic to use is "VPOS",
// so define that so that old shaders keep on working.
#if !defined(UNITY_COMPILER_CG)
#define WPOS VPOS
#endif

// define use to identify platform with modern feature like texture 3D with filtering, texture array etc...
#define UNITY_SM40_PLUS_PLATFORM (!((SHADER_TARGET < 30) || defined (SHADER_API_MOBILE) || defined(SHADER_API_GLES)))

// Ability to manually set descriptor set and binding numbers (Vulkan only)
#if defined(SHADER_API_VULKAN)
    #define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name##Xhlslcc_set_##Set##_bind_##Binding##X)
    // Sampler / image declaration with set/binding decoration
    #define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name##hlslcc_set_##Set##_bind_##Binding
#else
    #define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name)
    #define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name
#endif

// TODO: Really need a better define for iOS Metal than the framebuffer fetch one, that's also enabled on android and webgl (???)
#if defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_FRAMEBUFFER_FETCH_AVAILABLE))
// Renderpass inputs: Vulkan/Metal subpass input
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; }
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx) cbuffer hlslcc_SubpassInput_F_##idx { float4 hlslcc_fbinput_##idx[8]; }
// For halfs
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) cbuffer hlslcc_SubpassInput_h_##idx { half4 hlslcc_fbinput_##idx; }
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx) cbuffer hlslcc_SubpassInput_H_##idx { half4 hlslcc_fbinput_##idx[8]; }
// For ints
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx) cbuffer hlslcc_SubpassInput_i_##idx { int4 hlslcc_fbinput_##idx; }
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx) cbuffer hlslcc_SubpassInput_I_##idx { int4 hlslcc_fbinput_##idx[8]; }
// For uints
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx) cbuffer hlslcc_SubpassInput_u_##idx { uint4 hlslcc_fbinput_##idx; }
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx) cbuffer hlslcc_SubpassInput_U_##idx { uint4 hlslcc_fbinput_##idx[8]; }

#define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx
#define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) hlslcc_fbinput_##idx[sampleIdx]


#else
// Renderpass inputs: General fallback path
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_INT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT(idx) UNITY_DECLARE_TEX2D_NOSAMPLER_UINT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize

#define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fvertexname) _UnityFBInput##idx.Load(uint3(v2fvertexname.xy, 0))

// MSAA input framebuffers via tex2dms

#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_INT_MS(idx) Texture2DMS<int4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_UINT_MS(idx) Texture2DMS<uint4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize

#define UNITY_READ_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fvertexname) _UnityFBInput##idx.Load(uint2(v2fvertexname.xy), sampleIdx)

#endif

// ---- Shader keyword backwards compatibility
// We used to have some built-in shader keywords, but they got removed at some point to save on shader keyword count.
// However some existing shader code might be checking for the old names, so define them as regular
// macros based on other criteria -- so that existing code keeps on working.

// Unity 5.0 renamed HDR_LIGHT_PREPASS_ON to UNITY_HDR_ON
#if defined(UNITY_HDR_ON)
#define HDR_LIGHT_PREPASS_ON 1
#endif

// UNITY_NO_LINEAR_COLORSPACE was removed in 5.4 when UNITY_COLORSPACE_GAMMA was introduced as a platform keyword and runtime gamma fallback removed.
#if !defined(UNITY_NO_LINEAR_COLORSPACE) && defined(UNITY_COLORSPACE_GAMMA)
#define UNITY_NO_LINEAR_COLORSPACE 1
#endif

#if !defined(DIRLIGHTMAP_OFF) && !defined(DIRLIGHTMAP_COMBINED)
#define DIRLIGHTMAP_OFF 1
#endif

#if !defined(LIGHTMAP_OFF) && !defined(LIGHTMAP_ON)
#define LIGHTMAP_OFF 1
#endif

#if !defined(DYNAMICLIGHTMAP_OFF) && !defined(DYNAMICLIGHTMAP_ON)
#define DYNAMICLIGHTMAP_OFF 1
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
    #define UNITY_DECLARE_SCREENSPACE_TEXTURE(tex) sampler2D tex;
    #define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) tex2D(tex, uv)
#endif

#endif // SRP0802_HLSL_SUPPORT_INCLUDED
