// This file assume SHADER_API_GLES3 is defined

#define UNITY_UV_STARTS_AT_TOP 0
#define UNITY_REVERSED_Z 0
#define UNITY_GATHER_SUPPORTED 0
#define UNITY_NEAR_CLIP_VALUE (-1.0)

// This value will not go through any matrix projection convertion
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)
#define FRONT_FACE_SEMATIC VFACE
#define FRONT_FACE_TYPE float
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL > 0.0) ? (FRONT) : (BACK))

#define CBUFFER_START(name)
#define CBUFFER_END

// Initialize arbitrary structure with zero values.
// Do not exist on some platform, in this case we need to have a standard name that call a function that will initialize all parameters to 0
#define ZERO_INITIALIZE(type, name) name = (type)0;
#define ZERO_INITIALIZE_ARRAY(type, name, arraySize) { for (int arrayIndex = 0; arrayIndex < arraySize; arrayIndex++) { name[arrayIndex] = (type)0; } }

// Texture util abstraction

#define CALCULATE_TEXTURE2D_LOD(textureName, samplerName, coord2) // TODO:

// Texture abstraction

#define TEXTURE2D(textureName) sampler2D textureName
#define TEXTURE2D_ARRAY(textureName) sampler2DArray textureName
#define TEXTURECUBE(textureName) samplerCUBE textureName
#define TEXTURECUBE_ARRAY(textureName) samplerCUBEArray textureName
#define TEXTURE3D(textureName) sampler3D textureName
#define TEXTURE2D_SHADOW(textureName) sampler2DShadow textureName
#define RW_TEXTURE2D(type, textureNam)

#define SAMPLER2D(samplerName)
#define SAMPLERCUBE(samplerName)
#define SAMPLER3D(samplerName)
#define SAMPLER2D_SHADOW(samplerName)
#define SAMPLERCUBE_SHADOW(samplerName)

#define TEXTURE2D_ARGS(textureName, samplerName) sampler2D textureName
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName) sampler2DArray textureName
#define TEXTURECUBE_ARGS(textureName, samplerName) samplerCUBE textureName
#define TEXTURECUBE_ARRAY_ARGS(textureName, samplerName) samplerCUBEArray textureName
#define TEXTURE3D_ARGS(textureName, samplerName) sampler3D textureName
#define TEXTURE2D_SHADOW_ARGS(textureName, samplerName) sampler2DShadow textureName
#define TEXTURE2D_ARRAY_SHADOW_ARGS(textureName, samplerName) sampler2DArrayShadow textureName
#define TEXTURECUBE_SHADOW_ARGS(textureName, samplerName) samplerCUBEArrayShadow textureName

#define TEXTURE2D_PARAM(textureName, samplerName) textureName
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName) textureName
#define TEXTURECUBE_PARAM(textureName, samplerName) textureName
#define TEXTURECUBE_ARRAY_PARAM(textureName, samplerName) textureName
#define TEXTURE3D_PARAM(textureName, samplerName) textureName
#define TEXTURE2D_SHADOW_PARAM(textureName, samplerName) textureName
#define TEXTURE2D_ARRAY_SHADOW_PARAM(textureName, samplerName) textureName
#define TEXTURECUBE_SHADOW_PARAM(textureName, samplerName) textureName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) tex2D(textureName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) tex2Dlod(textureName, float4(coord2, 0, lod))
#define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias) tex2Dbias(textureName, float4(coord2, 0, bias))
#define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, ddx, ddy) tex2Dgrad(coord2.x, coord2.y, ddx, ddy)
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) tex2DArray(textureName, float3(coord2, index))
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod) tex2DArraylod(textureName, float4(coord2, index, lod))
#define SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, bias) tex2DArraybias(textureName, float4(coord2, index, bias))
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3) texCUBE(textureName, coord3)
#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod) texCUBElod(textureName, float4(coord3, lod))
#define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias) texCUBEbias(textureName, float4(coord3, bias))
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index) // TODO:
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod) // TODO:
#define SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, bias) // TODO:
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3) tex3D(textureName, coord3)
#define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3) shadow2D(textureName, coord3)
#define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index) // TODO:
#define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4) ((texCUBE(tex,(coord).xyz) < (coord).w) ? 0.0 : 1.0)
#define SAMPLE_TEXTURECUBE_ARRAY_SHADOW(textureName, samplerName, coord4, index) // TODO:

#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2) SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r

#define TEXTURE2D_HALF TEXTURE2D
#define TEXTURE2D_FLOAT TEXTURE2D
#define TEXTURE3D_HALF TEXTURE3D
#define TEXTURE3D_FLOAT TEXTURE3D
#define SAMPLER2D_HALF SAMPLER2D
#define SAMPLER2D_FLOAT SAMPLER2D

#define LOAD_TEXTURE2D(textureName, unCoord2) textureName.Load(int3(unCoord2, 0))
#define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod) textureName.Load(int3(unCoord2, lod))
#define LOAD_TEXTURE2D_MSAA(textureName, unCoord2, sampleIndex) textureName.Load(unCoord2, sampleIndex)
#define LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, index) textureName.Load(int4(unCoord2, index, 0))
#define LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, index, lod) textureName.Load(int4(unCoord2, index, lod))

#define GATHER_TEXTURE2D(textureName, samplerName, coord2)
#define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)
#define GATHER_TEXTURECUBE(textureName, samplerName, coord3)
#define GATHER_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)
