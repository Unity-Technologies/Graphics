#ifndef __SHADERBASE_H__
#define __SHADERBASE_H__

half2 DirectionToSphericalTexCoordinate(half3 dir_in)
{
    half3 dir = normalize(dir_in);
    // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    float recipPi = 1.0 / 3.1415926535897932384626433832795;
    return half2(1.0 - 0.5 * recipPi * atan2(dir.x, -dir.z), asin(dir.y) * recipPi + 0.5);
}

#ifdef UNITY_NO_CUBEMAP_ARRAY
    #define UNITY_DECLARE_ABSTRACT_CUBE_ARRAY                       UNITY_DECLARE_TEX2DARRAY
    #define UNITY_PASS_ABSTRACT_CUBE_ARRAY                          UNITY_PASS_TEX2DARRAY
    #define UNITY_ARGS_ABSTRACT_CUBE_ARRAY                          UNITY_ARGS_TEX2DARRAY
    #define UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(tex, coord, lod)     UNITY_SAMPLE_TEX2DARRAY_LOD(tex, float3(DirectionToSphericalTexCoordinate((coord).xyz), (coord).w), lod)
#else
    #define UNITY_DECLARE_ABSTRACT_CUBE_ARRAY                       UNITY_DECLARE_TEXCUBEARRAY
    #define UNITY_PASS_ABSTRACT_CUBE_ARRAY                          UNITY_PASS_TEXCUBEARRAY
    #define UNITY_ARGS_ABSTRACT_CUBE_ARRAY                          UNITY_ARGS_TEXCUBEARRAY
    #define UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(tex, coord, lod)     UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod)
#endif


// can't use UNITY_REVERSED_Z since it's not enabled in compute shaders
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)
    #define REVERSE_ZBUF
#endif

#ifdef SHADER_API_PSSL

#ifndef Texture2DMS
    #define Texture2DMS     MS_Texture2D
#endif

#ifndef SampleCmpLevelZero
    #define SampleCmpLevelZero              SampleCmpLOD0
#endif

#ifndef firstbithigh
    #define firstbithigh        FirstSetBit_Hi
#endif

#endif


#define __HLSL      1
#define public


#define unistruct   cbuffer
#define hbool       bool

#define _CB_REGSLOT(x)      : register(x)
#define _QALIGN(x)          : packoffset(c0);


float FetchDepth(Texture2D depthTexture, uint2 pixCoord)
{
    float zdpth = depthTexture.Load(uint3(pixCoord.xy, 0)).x;
#ifdef REVERSE_ZBUF
    zdpth = 1.0 - zdpth;
#endif
    return zdpth;
}

float FetchDepthMSAA(Texture2DMS<float> depthTexture, uint2 pixCoord, uint sampleIdx)
{
    float zdpth = depthTexture.Load(pixCoord.xy, sampleIdx).x;
#ifdef REVERSE_ZBUF
    zdpth = 1.0 - zdpth;
#endif
    return zdpth;
}

#endif
