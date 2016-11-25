#ifndef HLSL_SUPPORT_INCLUDED
#define HLSL_SUPPORT_INCLUDED

// Define the underlying compiler being used. Skips this step if the compiler is already specified,
// which may happen during development of new shader compiler for certain platform
#if !defined(UNITY_COMPILER_CG) && !defined(UNITY_COMPILER_HLSL) && !defined(UNITY_COMPILER_HLSL2GLSL) && !defined(UNITY_COMPILER_HLSLCC)
	#if defined(SHADER_TARGET_SURFACE_ANALYSIS)
		// Cg is used for surface shader analysis step
		#define UNITY_COMPILER_CG
	#elif defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN)
		// N.B. For Metal, the correct flags are set during internal shader compiler setup
		#define UNITY_COMPILER_HLSL
		#define UNITY_COMPILER_HLSLCC
	#elif defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_D3D9) || defined(SHADER_API_XBOXONE)
		#define UNITY_COMPILER_HLSL
	#elif defined(SHADER_TARGET_GLSL) || defined(SHADER_API_WIIU)
		#define UNITY_COMPILER_HLSL2GLSL
	#else
		#define UNITY_COMPILER_CG
	#endif
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

#define COLOR VCOLOR
#define COLOR0 VCOLOR0
#define COLOR1 VCOLOR1
#define COLOR2 VCOLOR2
#define COLOR3 VCOLOR3
#endif

// SV_Target[n] / SV_Depth defines, if not defined by compiler already
#if !defined(SV_Target)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Target COLOR
#	endif
#endif
#if !defined(SV_Target0)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Target0 COLOR0
#	endif
#endif
#if !defined(SV_Target1)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Target1 COLOR1
#	endif
#endif
#if !defined(SV_Target2)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Target2 COLOR2
#	endif
#endif
#if !defined(SV_Target3)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Target3 COLOR3
#	endif
#endif
#if !defined(SV_Depth)
#	if !defined(SHADER_API_XBOXONE)
#		define SV_Depth DEPTH
#	endif
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
#if !defined(SHADER_TARGET_GLSL) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !(defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC))
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
#endif

#if defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC))
// GLES3 and later via HLSLcc, use DX11.1 partial precision for translation
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
#endif // defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN)


// Define min16float/min10float to be half/fixed on non-D3D11 platforms.
// This allows people to use min16float and friends in their shader code if they
// really want to (making that will make shaders not load before DX11.1, e.g. on Win7,
// but if they target WSA/WP exclusively that's fine).
#if !defined(SHADER_API_D3D11) && !defined(SHADER_API_D3D11_9X) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_VULKAN) && !(defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC))
#define min16float half
#define min16float2 half2
#define min16float3 half3
#define min16float4 half4
#define min10float fixed
#define min10float2 fixed2
#define min10float3 fixed3
#define min10float4 fixed4
#endif

#if defined(SHADER_API_PSP2)
// The PSP2 cg compiler does not define uint<N>
#define uint2 unsigned int2
#define uint3 unsigned int3
#define uint4 unsigned int4
#endif

// specifically for samplers that are provided as arguments to entry functions
#if defined(SHADER_API_PSSL)
#define SAMPLER_UNIFORM uniform
#define SHADER_UNIFORM 
#else
#define SAMPLER_UNIFORM
#endif

#if defined(SHADER_API_PSSL)
#define CBUFFER_START(name) ConstantBuffer name {
#define CBUFFER_END };
#elif defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X)
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#else
// On specific platforms, like OpenGL and GLES3, constant buffers may still be used for instancing
#define CBUFFER_START(name)
#define CBUFFER_END
#endif


#if defined(SHADER_API_PSP2)
	// For tex2Dproj the PSP2 cg compiler doesn't like casting half3/4 to
	// float3/4 with swizzle (optimizer generates invalid assembly), so declare
	// explicit versions for half3/4
	half4 tex2Dproj(sampler2D s, in half3 t)		{ return tex2D(s, t.xy / t.z); }
	half4 tex2Dproj(sampler2D s, in half4 t)		{ return tex2D(s, t.xy / t.w); }

	// As above but for sampling from single component textures, e.g. depth textures.
	// NOTE that hardware PCF does not work with these versions, currently we have to ensure
	// that tex coords for shadow sampling use float, not half; and for some reason casting half
	// to float and using tex2Dproj also does not work.
	half4 tex2DprojShadow(sampler2D s, in half3 t)		{ return tex2D<float>(s, t.xy / t.z); }
	half4 tex2DprojShadow(sampler2D s, in half4 t)		{ return tex2D<float>(s, t.xy / t.w); }

	// ...and versions of tex2DprojShadow for float uv.
	half4 tex2DprojShadow(sampler2D s, in float3 t)		{ return tex2Dproj<float>(s, t); }
	half4 tex2DprojShadow(sampler2D s, in float4 t)		{ return tex2Dproj<float>(s, t); }
#endif


#if defined(SHADER_API_PSP2)
#define UNITY_BUGGY_TEX2DPROJ4
#define UNITY_PROJ_COORD(a) (a).xyw
#else
#define UNITY_PROJ_COORD(a) a
#endif


// Depth texture sampling helpers.
// On most platforms you can just sample them, but some (e.g. PSP2) need special handling.
//
// SAMPLE_DEPTH_TEXTURE(sampler,uv): returns scalar depth
// SAMPLE_DEPTH_TEXTURE_PROJ(sampler,uv): projected sample
// SAMPLE_DEPTH_TEXTURE_LOD(sampler,uv): sample with LOD level

#if defined(SHADER_API_PSP2) && !defined(SHADER_API_PSM)
#	define SAMPLE_DEPTH_TEXTURE(sampler, uv) (tex2D<float>(sampler, uv))
#	define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2DprojShadow(sampler, uv))
#	define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod<float>(sampler, uv))
#	define SAMPLE_RAW_DEPTH_TEXTURE(sampler, uv) SAMPLE_DEPTH_TEXTURE(sampler, uv)
#	define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv)
#	define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv)
#else
	// Sample depth, just the red component.
#	define SAMPLE_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv).r)
#	define SAMPLE_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv).r)
#	define SAMPLE_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv).r)
	// Sample depth, all components.
#	define SAMPLE_RAW_DEPTH_TEXTURE(sampler, uv) (tex2D(sampler, uv))
#	define SAMPLE_RAW_DEPTH_TEXTURE_PROJ(sampler, uv) (tex2Dproj(sampler, uv))
#	define SAMPLE_RAW_DEPTH_TEXTURE_LOD(sampler, uv) (tex2Dlod(sampler, uv))
#endif

// Deprecated; use SAMPLE_DEPTH_TEXTURE & SAMPLE_DEPTH_TEXTURE_PROJ instead
#if defined(SHADER_API_PSP2)
#	define UNITY_SAMPLE_DEPTH(value) (value).r
#else
#	define UNITY_SAMPLE_DEPTH(value) (value).r
#endif


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

#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(UNITY_COMPILER_HLSLCC)
	// DX11 & hlslcc platforms: built-in PCF
	#if defined(SHADER_API_D3D11_9X)
		// FL9.x has some bug where the runtime really wants resource & sampler to be bound to the same slot,
		// otherwise it is skipping draw calls that use shadowmap sampling. Let's bind to #15
		// and hope all works out.
		#define UNITY_DECLARE_SHADOWMAP(tex) Texture2D tex : register(t15); SamplerComparisonState sampler##tex : register(s15)
	#else
		#define UNITY_DECLARE_SHADOWMAP(tex) Texture2D tex; SamplerComparisonState sampler##tex
	#endif
	#define UNITY_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#elif defined(UNITY_COMPILER_HLSL2GLSL) && defined(SHADOWS_NATIVE)
	// OpenGL-like hlsl2glsl platforms: most of them always have built-in PCF
	#define UNITY_DECLARE_SHADOWMAP(tex) sampler2DShadow tex
	#define UNITY_SAMPLE_SHADOW(tex,coord) shadow2D (tex,(coord).xyz)
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) shadow2Dproj (tex,coord)
#elif defined(SHADER_API_D3D9)
	// D3D9: Native shadow maps FOURCC "driver hack", looks just like a regular
	// texture sample. Have to always do a projected sample
	// so that HLSL compiler doesn't try to be too smart and mess up swizzles
	// (thinking that Z is unused).
	#define UNITY_DECLARE_SHADOWMAP(tex) sampler2D tex
	#define UNITY_SAMPLE_SHADOW(tex,coord) tex2Dproj (tex,float4((coord).xyz,1)).r
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex2Dproj (tex,coord).r
#elif defined(SHADER_API_PSSL)
	// PS4: built-in PCF
	#define UNITY_DECLARE_SHADOWMAP(tex)		Texture2D tex; SamplerComparisonState sampler##tex
	#define UNITY_SAMPLE_SHADOW(tex,coord)		tex.SampleCmpLOD0(sampler##tex,(coord).xy,(coord).z)
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord)	tex.SampleCmpLOD0(sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)
#elif defined(SHADER_API_PSP2)
	// Vita
	#define UNITY_DECLARE_SHADOWMAP(tex) sampler2D tex
	// tex2d shadow comparison on Vita returns 0 instead of 1 when shadowCoord.z >= 1 causing artefacts in some tests.
	// Clamping Z to the range 0.0 <= Z < 1.0 solves this.
	#define UNITY_SAMPLE_SHADOW(tex,coord) tex2D<float>(tex, float3((coord).xy, clamp((coord).z, 0.0, 0.999999)))
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex2DprojShadow(tex, coord)
#else
	// Fallback / No built-in shadowmap comparison sampling: regular texture sample and do manual depth comparison
	#define UNITY_DECLARE_SHADOWMAP(tex) sampler2D_float tex
	#define UNITY_SAMPLE_SHADOW(tex,coord) ((SAMPLE_DEPTH_TEXTURE(tex,(coord).xy) < (coord).z) ? 0.0 : 1.0)
	#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) ((SAMPLE_DEPTH_TEXTURE_PROJ(tex,UNITY_PROJ_COORD(coord)) < ((coord).z/(coord).w)) ? 0.0 : 1.0)
#endif


// Macros to declare textures and samplers, possibly separately. For platforms
// that have separate samplers & textures (like DX11), and we'd want to conserve
// the samplers.
//	- UNITY_DECLARE_TEX*_NOSAMPLER declares a texture, without a sampler.
//	- UNITY_SAMPLE_TEX*_SAMPLER samples a texture, using sampler from another texture.
//		That another texture must also be actually used in the current shader, otherwise
//		the correct sampler will not be set.
#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC)
	// DX11 style HLSL syntax; separate textures and samplers
	// NB for HLSLcc we have special unity-specific syntax to pass sampler precision information

	// 2D textures
	#define UNITY_DECLARE_TEX2D(tex) Texture2D tex; SamplerState sampler##tex
	#define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) Texture2D tex
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

	// 2D arrays
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
	#define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,coord)
	#define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
	#define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
	#define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)

	
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

	// DX9 with SM2.0, and DX11 FL 9.x do not have texture LOD sampling.
	// We will approximate that with mip bias (very poor approximation, but not much we can do)
	#if ((SHADER_TARGET < 25) && defined(SHADER_API_D3D9)) || defined(SHADER_API_D3D11_9X)
	#	define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) texCUBEbias(tex, half4(coord, lod))
	#	define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex,samplertex,coord,lod) UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod)
	#else
	#	define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) texCUBElod (tex, half4(coord, lod))
	#	define UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(tex,samplertex,coord,lod) UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod)
	#endif
	#define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) texCUBE (tex,coord)

	// 3D textures
	#define UNITY_DECLARE_TEX3D(tex) sampler3D tex
	#define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) sampler3D tex
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
	// 2D/Cube array syntax for PS4
	#if defined(SHADER_API_PSSL)
		// 2D arrays
		#define UNITY_DECLARE_TEX2DARRAY(tex) Texture2D_Array tex; SamplerState sampler##tex
		#define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) Texture2D_Array tex
		#define UNITY_ARGS_TEX2DARRAY(tex) Texture2D_Array tex, SamplerState sampler##tex
		#define UNITY_PASS_TEX2DARRAY(tex) tex, sampler##tex
		#define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex.Sample (sampler##tex,coord)
		#define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
		#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)
		#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,coord,lod)		
		// Cube arrays
		#define UNITY_DECLARE_TEXCUBEARRAY(tex) TextureCube_Array tex; SamplerState sampler##tex
		#define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) TextureCube_Array tex
		#define UNITY_ARGS_TEXCUBEARRAY(tex) TextureCube_Array tex, SamplerState sampler##tex
		#define UNITY_PASS_TEXCUBEARRAY(tex) tex, sampler##tex
		// round the layer index to get DX11-like behaviour (otherwise fractional indices result in mixed up cubemap faces)
		#define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,float4((coord).xyz, round((coord).w)))
		#define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,float4((coord).xyz, round((coord).w)), lod)
		#define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,float4((coord).xyz, round((coord).w)))
		#define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) tex.SampleLevel (sampler##samplertex,float4((coord).xyz, round((coord).w)), lod)		

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


#define samplerRECT sampler2D
#define texRECT tex2D
#define texRECTlod tex2Dlod
#define texRECTbias tex2Dbias
#define texRECTproj tex2Dproj

#if defined(SHADER_API_PSSL)
#define VPOS			S_POSITION
#elif defined(UNITY_COMPILER_CG)
// Cg seems to use WPOS instead of VPOS semantic?
#define VPOS WPOS
// Cg does not have tex2Dgrad and friends, but has tex2D overload that
// can take the derivatives
#define tex2Dgrad tex2D
#define texCUBEgrad texCUBE
#define tex3Dgrad tex3D
#endif


// Data type to be used for "screen space position" pixel shader input semantic;
// D3D9 needs it to be float2, unlike all other platforms.
#if defined(SHADER_API_D3D9)
#define UNITY_VPOS_TYPE float2
#else
#define UNITY_VPOS_TYPE float4
#endif



#if defined(UNITY_COMPILER_HLSL) || defined (SHADER_TARGET_GLSL)
#define FOGC FOG
#endif

// Use VFACE pixel shader input semantic in your shaders to get front-facing scalar value.
// Requires shader model 3.0 or higher.
#if defined(UNITY_COMPILER_CG)
#define VFACE FACE
#endif
#if defined(UNITY_COMPILER_HLSL2GLSL)
#define FACE VFACE
#endif
#if defined(SHADER_API_PSSL)
#define VFACE S_FRONT_FACE
#endif
// Is VFACE affected by flipped projection?
#if defined(SHADER_API_D3D9) || defined(SHADER_API_PSSL)
#define UNITY_VFACE_AFFECTED_BY_PROJECTION 1
#endif


#if !defined(SHADER_API_D3D11) && !defined(SHADER_API_D3D11_9X) && !defined(UNITY_COMPILER_HLSLCC) && !defined(SHADER_API_PSSL)
#define SV_POSITION POSITION
#endif


#if defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_PSP2) || defined(SHADER_API_PSSL)
#define UNITY_ATTEN_CHANNEL r
#else
#define UNITY_ATTEN_CHANNEL a
#endif

#if defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_PSP2) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_WIIU) || defined(SHADER_API_VULKAN)
#define UNITY_UV_STARTS_AT_TOP 1
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
#define UNITY_REVERSED_Z 1
#endif

#if defined(UNITY_REVERSED_Z)
#define UNITY_NEAR_CLIP_VALUE (1.0)
#elif defined(SHADER_API_D3D9)  || defined(SHADER_API_WIIU) || defined(SHADER_API_D3D11_9X)
#define UNITY_NEAR_CLIP_VALUE (0.0)
#else
#define UNITY_NEAR_CLIP_VALUE (-1.0)
#endif

// "platform caps" defines that were moved to editor, so they are set automatically when compiling shader
// UNITY_NO_DXT5nm				- no DXT5NM support, so normal maps will encoded in rgb
// UNITY_NO_RGBM				- no RGBM support, so doubleLDR
// UNITY_NO_SCREENSPACE_SHADOWS	- no screenspace cascaded shadowmaps
// UNITY_FRAMEBUFFER_FETCH_AVAILABLE	- framebuffer fetch
// UNITY_ENABLE_REFLECTION_BUFFERS - render reflection probes in deferred way, when using deferred shading


#if defined(SHADER_API_PSP2)
// To get acceptable precision from the SGX interpolators when decoding RGBM type
// textures we have to disable sRGB reads and then convert to gamma space in the shader
// explicitly.
#define UNITY_FORCE_LINEAR_READ_FOR_RGBM
#endif


// On most platforms, use floating point render targets to store depth of point
// light shadowmaps. However, on some others they either have issues, or aren't widely
// supported; in which case fallback to encoding depth into RGBA channels.
// Make sure this define matches GraphicsCaps.useRGBAForPointShadows.
#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_PSP2)
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

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PSSL)
#define UNITY_CAN_COMPILE_TESSELLATION 1
#	define UNITY_domain					domain
#	define UNITY_partitioning			partitioning
#	define UNITY_outputtopology			outputtopology
#	define UNITY_patchconstantfunc		patchconstantfunc
#	define UNITY_outputcontrolpoints	outputcontrolpoints
#endif

// Not really needed anymore, but did ship in Unity 4.0; with D3D11_9X remapping them to .r channel.
// Now that's not used.
#define UNITY_SAMPLE_1CHANNEL(x,y) tex2D(x,y).a
#define UNITY_ALPHA_CHANNEL a


// HLSL attributes
#if defined(UNITY_COMPILER_HLSL)
	#define UNITY_BRANCH	[branch]
	#define UNITY_FLATTEN	[flatten]
	#define UNITY_UNROLL	[unroll]
	#define UNITY_LOOP		[loop]
	#define UNITY_FASTOPT	[fastopt]
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
#define UNITY_SM40_PLUS_PLATFORM (!((SHADER_TARGET < 30) || defined (SHADER_API_MOBILE) || defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11_9X) || defined (SHADER_API_PSP2) || defined(SHADER_API_GLES)))

// Ability to manually set descriptor set and binding numbers (Vulkan only)
#if defined(SHADER_API_VULKAN)
	#define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name##Xhlslcc_set_##Set##_bind_##Binding##X)
	// Sampler / image declaration with set/binding decoration 
	#define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name##hlslcc_set_##Set##_bind_##Binding
#else
	#define CBUFFER_START_WITH_BINDING(Name, Set, Binding) CBUFFER_START(Name)
	#define DECL_WITH_BINDING(Type, Name, Set, Binding) Type Name
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

#if !defined(DIRLIGHTMAP_OFF) && !defined(DIRLIGHTMAP_SEPARATE) && !defined(DIRLIGHTMAP_COMBINED)
#define DIRLIGHTMAP_OFF 1
#endif

#if !defined(LIGHTMAP_OFF) && !defined(LIGHTMAP_ON)
#define LIGHTMAP_OFF 1
#endif

#if !defined(DYNAMICLIGHTMAP_OFF) && !defined(DYNAMICLIGHTMAP_ON)
#define DYNAMICLIGHTMAP_OFF 1
#endif


#if defined (SHADER_API_D3D11) && defined(STEREO_INSTANCING_ON)

	#undef UNITY_DECLARE_DEPTH_TEXTURE
	#define UNITY_DECLARE_DEPTH_TEXTURE(tex) Texture2DArray tex; SamplerState sampler##tex

	#undef SAMPLE_DEPTH_TEXTURE
	#define SAMPLE_DEPTH_TEXTURE(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3(uv.x, uv.y, (float)unity_StereoEyeIndex)).r

	#undef SAMPLE_DEPTH_TEXTURE_PROJ
	#define SAMPLE_DEPTH_TEXTURE_PROJ(tex, uv) UNITY_SAMPLE_TEX2DARRAY(tex, float3(uv.x/uv.w, uv.y/uv.w, (float)unity_StereoEyeIndex)).r

	#define UNITY_DECLARE_SCREENSPACE_SHADOWMAP UNITY_DECLARE_TEX2DARRAY
	#define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) UNITY_SAMPLE_TEX2DARRAY( tex, float3(uv.x/uv.w, uv.y/uv.w, (float)unity_StereoEyeIndex) ).r

#else
	#define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
	#define UNITY_DECLARE_SCREENSPACE_SHADOWMAP(tex) sampler2D tex
	#define UNITY_SAMPLE_SCREEN_SHADOW(tex, uv) tex2Dproj( tex, UNITY_PROJ_COORD(uv) ).r
#endif

#endif // HLSL_SUPPORT_INCLUDED


