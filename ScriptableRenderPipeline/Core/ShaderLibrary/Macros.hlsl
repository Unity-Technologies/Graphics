#ifndef UNITY_MACROS_INCLUDED
#define UNITY_MACROS_INCLUDED

// Some shader compiler don't support to do multiple ## for concatenation inside the same macro, it require an indirection.
// This is the purpose of this macro
#define MERGE_NAME(X, Y) X##Y

// These define are use to abstract the way we sample into a cubemap array.
// Some platform don't support cubemap array so we fallback on 2D latlong
#ifdef  UNITY_NO_CUBEMAP_ARRAY
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURE2D_ARRAY
#define SAMPLERCUBE_ABSTRACT SAMPLER2D
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURE2D_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURE2D_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, DirectionToLatLongCoordinate(coord3), index, lod)
#else
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURECUBE_ARRAY
#define SAMPLERCUBE_ABSTRACT SAMPLERCUBE
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURECUBE_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURECUBE_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)
#endif

#define TEMPLATE_1_FLT(FunctionName, Parameter1, FunctionBody) \
float  FunctionName(float  Parameter1) { FunctionBody; } \
float2 FunctionName(float2 Parameter1) { FunctionBody; } \
float3 FunctionName(float3 Parameter1) { FunctionBody; } \
float4 FunctionName(float4 Parameter1) { FunctionBody; }

#define TEMPLATE_1_INT(FunctionName, Parameter1, FunctionBody) \
int    FunctionName(int    Parameter1) { FunctionBody; } \
int2   FunctionName(int2   Parameter1) { FunctionBody; } \
int3   FunctionName(int3   Parameter1) { FunctionBody; } \
int4   FunctionName(int4   Parameter1) { FunctionBody; } \
uint   FunctionName(uint   Parameter1) { FunctionBody; } \
uint2  FunctionName(uint2  Parameter1) { FunctionBody; } \
uint3  FunctionName(uint3  Parameter1) { FunctionBody; } \
uint4  FunctionName(uint4  Parameter1) { FunctionBody; }

#define TEMPLATE_2_FLT(FunctionName, Parameter1, Parameter2, FunctionBody) \
float  FunctionName(float  Parameter1, float  Parameter2) { FunctionBody; } \
float2 FunctionName(float2 Parameter1, float2 Parameter2) { FunctionBody; } \
float3 FunctionName(float3 Parameter1, float3 Parameter2) { FunctionBody; } \
float4 FunctionName(float4 Parameter1, float4 Parameter2) { FunctionBody; }

#define TEMPLATE_2_INT(FunctionName, Parameter1, Parameter2, FunctionBody) \
int    FunctionName(int    Parameter1, int    Parameter2) { FunctionBody; } \
int2   FunctionName(int2   Parameter1, int2   Parameter2) { FunctionBody; } \
int3   FunctionName(int3   Parameter1, int3   Parameter2) { FunctionBody; } \
int4   FunctionName(int4   Parameter1, int4   Parameter2) { FunctionBody; } \
uint   FunctionName(uint   Parameter1, uint   Parameter2) { FunctionBody; } \
uint2  FunctionName(uint2  Parameter1, uint2  Parameter2) { FunctionBody; } \
uint3  FunctionName(uint3  Parameter1, uint3  Parameter2) { FunctionBody; } \
uint4  FunctionName(uint4  Parameter1, uint4  Parameter2) { FunctionBody; }

#define TEMPLATE_3_FLT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
float  FunctionName(float  Parameter1, float  Parameter2, float  Parameter3) { FunctionBody; } \
float2 FunctionName(float2 Parameter1, float2 Parameter2, float2 Parameter3) { FunctionBody; } \
float3 FunctionName(float3 Parameter1, float3 Parameter2, float3 Parameter3) { FunctionBody; } \
float4 FunctionName(float4 Parameter1, float4 Parameter2, float4 Parameter3) { FunctionBody; }

#define TEMPLATE_3_INT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
int    FunctionName(int    Parameter1, int    Parameter2, int    Parameter3) { FunctionBody; } \
int2   FunctionName(int2   Parameter1, int2   Parameter2, int2   Parameter3) { FunctionBody; } \
int3   FunctionName(int3   Parameter1, int3   Parameter2, int3   Parameter3) { FunctionBody; } \
int4   FunctionName(int4   Parameter1, int4   Parameter2, int4   Parameter3) { FunctionBody; } \
uint   FunctionName(uint   Parameter1, uint   Parameter2, uint   Parameter3) { FunctionBody; } \
uint2  FunctionName(uint2  Parameter1, uint2  Parameter2, uint2  Parameter3) { FunctionBody; } \
uint3  FunctionName(uint3  Parameter1, uint3  Parameter2, uint3  Parameter3) { FunctionBody; } \
uint4  FunctionName(uint4  Parameter1, uint4  Parameter2, uint4  Parameter3) { FunctionBody; }

#endif // UNITY_MACROS_INCLUDED
