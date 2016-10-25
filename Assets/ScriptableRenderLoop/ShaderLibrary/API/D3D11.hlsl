// This file assume SHADER_API_D3D11 is defined

#define UNITY_UV_STARTS_AT_TOP 1
#define UNITY_REVERSED_Z 1
#define UNITY_NEAR_CLIP_VALUE (1.0)
#define FRONT_FACE_SEMATIC SV_IsFrontFace
#define FRONT_FACE_TYPE bool
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL) ? (FRONT) : (BACK))

#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };

// Initialize arbitrary structure with zero values.
// Do not exist on some platform, in this case we need to have a standard name that call a function that will initialize all parameters to 0
#define ZERO_INITIALIZE(type, name) name = (type)0;

// Macros to declare textures and samplers, possibly separately. For platforms
// that have separate samplers & textures (like DX11), and we'd want to conserve
// the samplers.
//  - UNITY_DECLARE_TEX*_NOSAMPLER declares a texture, without a sampler.
//  - UNITY_SAMPLE_TEX*_SAMPLER samples a texture, using sampler from another texture.
//      That another texture must also be actually used in the current shader, otherwise
//      the correct sampler will not be set.

// 2D textures
#define UNITY_DECLARE_TEX2D(tex) Texture2D tex; SamplerState sampler##tex
#define UNITY_DECLARE_TEX2D_NOSAMPLER(tex) Texture2D tex
#define UNITY_SAMPLE_TEX2D(tex,coord) tex.Sample (sampler##tex,coord)
#define UNITY_SAMPLE_TEX2D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

// Cubemaps
#define UNITY_DECLARE_TEXCUBE(tex) TextureCube tex; SamplerState sampler##tex
#define UNITY_ARGS_TEXCUBE(tex) TextureCube tex, SamplerState sampler##tex
#define UNITY_PASS_TEXCUBE(tex) tex, sampler##tex
#define UNITY_PASS_TEXCUBE_SAMPLER(tex,samplertex) tex, sampler##samplertex
#define UNITY_DECLARE_TEXCUBE_NOSAMPLER(tex) TextureCube tex
#define UNITY_SAMPLE_TEXCUBE(tex,coord) tex.Sample (sampler##tex,coord)
#define UNITY_SAMPLE_TEXCUBE_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEXCUBE_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

// 3D textures
#define UNITY_DECLARE_TEX3D(tex) Texture3D tex; SamplerState sampler##tex
#define UNITY_DECLARE_TEX3D_NOSAMPLER(tex) Texture3D tex
#define UNITY_SAMPLE_TEX3D(tex,coord) tex.Sample (sampler##tex,coord)
#define UNITY_SAMPLE_TEX3D_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEX3D_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

// 2D arrays
#define UNITY_DECLARE_TEX2DARRAY(tex) Texture2DArray tex; SamplerState sampler##tex
#define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(tex) Texture2DArray tex
#define UNITY_ARGS_TEX2DARRAY(tex) Texture2DArray tex, SamplerState sampler##tex
#define UNITY_PASS_TEX2DARRAY(tex) tex, sampler##tex
#define UNITY_SAMPLE_TEX2DARRAY(tex,coord) tex.Sample (sampler##tex,coord)
#define UNITY_SAMPLE_TEX2DARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

// Cube arrays
#define UNITY_DECLARE_TEXCUBEARRAY(tex) TextureCubeArray tex; SamplerState sampler##tex
#define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) TextureCubeArray tex
#define UNITY_ARGS_TEXCUBEARRAY(tex) TextureCubeArray tex, SamplerState sampler##tex
#define UNITY_PASS_TEXCUBEARRAY(tex) tex, sampler##tex
#define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) tex.Sample (sampler##tex,coord)
#define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) tex.SampleLevel (sampler##tex,coord, lod)
#define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord) tex.Sample (sampler##samplertex,coord)

// Shadow
#define UNITY_DECLARE_SHADOWMAP(tex) Texture2D tex; SamplerComparisonState sampler##tex
#define UNITY_SAMPLE_SHADOW(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy,(coord).z)
#define UNITY_SAMPLE_SHADOW_PROJ(tex,coord) tex.SampleCmpLevelZero (sampler##tex,(coord).xy/(coord).w,(coord).z/(coord).w)


// Alternative
#define TEXTURE2D(textureName) Texture2D textureName;
#define TEXTURE2D_ARRAY(textureName) Texture2DArray textureName;
#define TEXTURECUBE(textureName) TextureCube textureName;
#define TEXTURECUBE_ARRAY(textureName) TextureCubeArray textureName;
#define TEXTURE3D(textureName) Texture3D textureName;

#define SAMPLER2D(samplerName) SamplerState samplerName;
#define SAMPLERCUBE(samplerName) SamplerState samplerName;
#define SAMPLER3D(samplerName) SamplerState samplerName;
#define SAMPLER2D_SHADOW(samplerName) SamplerComparisonState samplerName
#define SAMPLERCUBE_SHADOW(samplerName) SamplerComparisonState samplerName

#define TEXTURE2D_ARGS(textureName, samplerName) Texture2D textureName, SamplerState samplerName
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName) Texture2DArray textureName, SamplerState samplerName
#define TEXTURECUBE_ARGS(textureName, samplerName) TextureCube textureName, SamplerState samplerName
#define TEXTURECUBE_ARRAY_ARGS(textureName, samplerName) TextureCubeArray textureName, SamplerState samplerName
#define TEXTURE3D_ARGS(textureName, samplerName) Texture3D textureName, SamplerState samplerName
#define TEXTURE2D_SHADOW_ARGS(textureName, samplerName) Texture2D textureName, SamplerComparisonState samplerName
#define TEXTURE2D_ARRAY_SHADOW_ARGS(textureName, samplerName) Texture2DArray textureName, SamplerComparisonState samplerName
#define TEXTURECUBE_SHADOW_ARGS(textureName, samplerName) TextureCube textureName, SamplerComparisonState samplerName

#define TEXTURE2D_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURE2D_ARRAY_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURECUBE_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURECUBE_ARRAY_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURE3D_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURE2D_SHADOW_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURE2D_ARRAY_SHADOW_PASS(textureName, samplerName) textureName, samplerName
#define TEXTURECUBE_SHADOW_PASS(textureName, samplerName) textureName, samplerName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) textureName.Sample(samplerName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) textureName.SampleLevel(samplerName, coord2, lod)
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) textureName.Sample(samplerName, float3((coord2).xy, index))
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod) textureName.SampleLevel(samplerName, float3((coord2).xy, index), lod)
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3) textureName.Sample(samplerName, coord3)
#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3) textureName.SampleLevel(samplerName, coord3, lod)
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3) textureName.Sample(samplerName, float4((coord3).xyz, index))
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, lod) textureName.SampleLevel(samplerName, float4((coord3).xyz, index), lod)
#define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3) textureName.SampleCmpLevelZero(samplerName, (coord3).xy, (coord3).z)
#define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index) textureName.SampleCmpLevelZero(samplerName, float3((coord3).xy, index), (coord3).z)
#define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4) textureName.SampleCmpLevelZero(samplerName, (coord3).xyz, (coord3).w)

