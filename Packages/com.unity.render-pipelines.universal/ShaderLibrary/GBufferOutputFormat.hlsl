// This file contains functionality for letting the Unity Shader Compiler know what render target formats could be used
// The functionality provided here is intended to be used during in the material GBuffer pass.
#ifndef UNIVERSAL_GBUFFEROUTPUTFORMAT_INCLUDED
#define UNIVERSAL_GBUFFEROUTPUTFORMAT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferCommon.hlsl"

// Keep these formats in sync with the formats returned in DeferredLights.GetGBufferFormat
#pragma rendertarget_format_hint MRT0 R8G8B8A8_SRGB
#pragma rendertarget_format_hint MRT1 R8G8B8A8_SRGB
#pragma rendertarget_format_hint MRT2 R8G8B8A8_SRGB
#pragma rendertarget_format_hint MRT3 R8G8B8A8_SRGB,B10G11R11_UFloatPack32
#if defined(GBUFFER_FEATURE_DEPTH)
    #pragma rendertarget_format_hint MRT4 R32_SFloat
    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
        #pragma rendertarget_format_hint MRT5 R8_UInt, R16_UInt, R32_UInt
        #if defined(GBUFFER_FEATURE_SHADOWMASK)
            #pragma rendertarget_format_hint MRT6 B8G8R8A8_UNorm, R8G8B8A8_UNorm
        #endif
    #else // GBUFFER_FEATURE_RENDERING_LAYERS
        #if defined(GBUFFER_FEATURE_SHADOWMASK)
            #pragma rendertarget_format_hint MRT5 B8G8R8A8_UNorm, R8G8B8A8_UNorm
        #endif
    #endif
#else
    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
        #pragma rendertarget_format_hint MRT4 R8_UInt, R16_UInt, R32_UInt
        #if defined(GBUFFER_FEATURE_SHADOWMASK)
            #pragma rendertarget_format_hint MRT5 B8G8R8A8_UNorm, R8G8B8A8_UNorm
        #endif
    #else // GBUFFER_FEATURE_RENDERING_LAYERS
        #if defined(GBUFFER_FEATURE_SHADOWMASK)
            #pragma rendertarget_format_hint MRT4 B8G8R8A8_UNorm, R8G8B8A8_UNorm
        #endif
    #endif
#endif

#endif // UNIVERSAL_GBUFFEROUTPUTFORMAT_INCLUDED
