#ifndef FOUNDRY_PIPELINE_CORE_INCLUDED
#define FOUNDRY_PIPELINE_CORE_INCLUDED

// VT is not supported in URP (for now) this ensures any shaders using the VT
// node work by falling to regular texture sampling.
#define FORCE_VIRTUAL_TEXTURING_OFF 1

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"

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

#include "Assets/CommonAssets/Editor/Targets/Shaders/Input.hlsl"

#endif
