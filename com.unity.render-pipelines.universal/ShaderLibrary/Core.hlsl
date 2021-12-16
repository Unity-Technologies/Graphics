#ifndef UNIVERSAL_PIPELINE_CORE_INCLUDED
#define UNIVERSAL_PIPELINE_CORE_INCLUDED

// VT is not supported in URP (for now) this ensures any shaders using the VT
// node work by falling to regular texture sampling.
#define FORCE_VIRTUAL_TEXTURING_OFF 1

#if defined(_CLUSTERED_RENDERING)
#define _ADDITIONAL_LIGHTS 1
#undef _ADDITIONAL_LIGHTS_VERTEX
#define USE_CLUSTERED_LIGHTING 1
#else
#define USE_CLUSTERED_LIGHTING 0
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

#if !defined(SHADER_HINT_NICE_QUALITY)
    #if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
        #define SHADER_HINT_NICE_QUALITY 0
    #else
        #define SHADER_HINT_NICE_QUALITY 1
    #endif
#endif

// Shader Quality Tiers in Universal.
// SRP doesn't use Graphics Settings Quality Tiers.
// We should expose shader quality tiers in the pipeline asset.
// Meanwhile, it's forced to be:
// High Quality: Non-mobile platforms or shader explicit defined SHADER_HINT_NICE_QUALITY
// Medium: Mobile aside from GLES2
// Low: GLES2
#if SHADER_HINT_NICE_QUALITY
    #define SHADER_QUALITY_HIGH
#elif defined(SHADER_API_GLES)
    #define SHADER_QUALITY_LOW
#else
    #define SHADER_QUALITY_MEDIUM
#endif

#ifndef BUMP_SCALE_NOT_SUPPORTED
    #define BUMP_SCALE_NOT_SUPPORTED !SHADER_HINT_NICE_QUALITY
#endif


#if UNITY_REVERSED_Z
    // TODO: workaround. There's a bug where SHADER_API_GL_CORE gets erroneously defined on switch.
    #if (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
        //GL with reversed z => z clip range is [near, -far] -> remapping to [0, far]
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max((coord - _ProjectionParams.y)/(-_ProjectionParams.z-_ProjectionParams.y)*_ProjectionParams.z, 0)
    #else
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningful in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> remapping to [0, far]
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((coord + _ProjectionParams.y)/(_ProjectionParams.z+_ProjectionParams.y))*_ProjectionParams.z, 0)
#endif

// Stereo-related bits
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

    #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex

    #define TEXTURE2D_X(textureName)                                        TEXTURE2D_ARRAY(textureName)
    #define TEXTURE2D_X_PARAM(textureName, samplerName)                     TEXTURE2D_ARRAY_PARAM(textureName, samplerName)
    #define TEXTURE2D_X_ARGS(textureName, samplerName)                      TEXTURE2D_ARRAY_ARGS(textureName, samplerName)
    #define TEXTURE2D_X_HALF(textureName)                                   TEXTURE2D_ARRAY_HALF(textureName)
    #define TEXTURE2D_X_FLOAT(textureName)                                  TEXTURE2D_ARRAY_FLOAT(textureName)

    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))

#else
    #define SLICE_ARRAY_INDEX       0

    #define TEXTURE2D_X(textureName)                                        TEXTURE2D(textureName)
    #define TEXTURE2D_X_PARAM(textureName, samplerName)                     TEXTURE2D_PARAM(textureName, samplerName)
    #define TEXTURE2D_X_ARGS(textureName, samplerName)                      TEXTURE2D_ARGS(textureName, samplerName)
    #define TEXTURE2D_X_HALF(textureName)                                   TEXTURE2D_HALF(textureName)
    #define TEXTURE2D_X_FLOAT(textureName)                                  TEXTURE2D_FLOAT(textureName)

    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D(textureName, unCoord2)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D(textureName, samplerName, coord2)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, coord2)
#endif

///
/// Texture Sampling Macro Overrides for Scaling
///
/// When mip bias is supported by the underlying platform, the following section redefines all 2d texturing operations to support a global mip bias feature.
/// This feature is used to improve rendering quality when image scaling is active. It achieves this by adding a bias value to the standard mip lod calculation
/// which allows us to select the mip level based on the final image resolution rather than the current rendering resolution.

#ifdef PLATFORM_SAMPLE_TEXTURE2D_BIAS
    #ifdef  SAMPLE_TEXTURE2D
        #undef  SAMPLE_TEXTURE2D
        #define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) \
            PLATFORM_SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2,  _GlobalMipBias.x)
    #endif
    #ifdef  SAMPLE_TEXTURE2D_BIAS
        #undef  SAMPLE_TEXTURE2D_BIAS
        #define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias) \
            PLATFORM_SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2,  (bias + _GlobalMipBias.x))
    #endif
#endif

#ifdef PLATFORM_SAMPLE_TEXTURE2D_GRAD
    #ifdef  SAMPLE_TEXTURE2D_GRAD
        #undef  SAMPLE_TEXTURE2D_GRAD
        #define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, dpdx, dpdy) \
            PLATFORM_SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, (dpdx * _GlobalMipBias.y), (dpdy * _GlobalMipBias.y))
    #endif
#endif

#ifdef PLATFORM_SAMPLE_TEXTURE2D_ARRAY_BIAS
    #ifdef  SAMPLE_TEXTURE2D_ARRAY
        #undef  SAMPLE_TEXTURE2D_ARRAY
        #define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) \
            PLATFORM_SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, _GlobalMipBias.x)
    #endif
    #ifdef  SAMPLE_TEXTURE2D_ARRAY_BIAS
        #undef  SAMPLE_TEXTURE2D_ARRAY_BIAS
        #define SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, bias) \
            PLATFORM_SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, (bias + _GlobalMipBias.x))
    #endif
#endif

#ifdef PLATFORM_SAMPLE_TEXTURECUBE_BIAS
    #ifdef  SAMPLE_TEXTURECUBE
        #undef  SAMPLE_TEXTURECUBE
        #define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3) \
            PLATFORM_SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, _GlobalMipBias.x)
    #endif
    #ifdef  SAMPLE_TEXTURECUBE_BIAS
        #undef  SAMPLE_TEXTURECUBE_BIAS
        #define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias) \
            PLATFORM_SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, (bias + _GlobalMipBias.x))
    #endif
#endif

#ifdef PLATFORM_SAMPLE_TEXTURECUBE_ARRAY_BIAS

    #ifdef  SAMPLE_TEXTURECUBE_ARRAY
        #undef  SAMPLE_TEXTURECUBE_ARRAY
        #define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)\
            PLATFORM_SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, _GlobalMipBias.x)
    #endif

    #ifdef  SAMPLE_TEXTURECUBE_ARRAY_BIAS
        #undef  SAMPLE_TEXTURECUBE_ARRAY_BIAS
        #define SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, bias)\
            PLATFORM_SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, (bias + _GlobalMipBias.x))
    #endif
#endif

#define VT_GLOBAL_MIP_BIAS_MULTIPLIER (_GlobalMipBias.y)

// Structs
struct VertexPositionInputs
{
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float4 positionNDC;// Homogeneous normalized device coordinates
};

struct VertexNormalInputs
{
    real3 tangentWS;
    real3 bitangentWS;
    float3 normalWS;
};

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Deprecated.hlsl"

#endif
