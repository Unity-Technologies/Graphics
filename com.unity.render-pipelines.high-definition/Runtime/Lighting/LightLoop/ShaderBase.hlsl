#ifndef __SHADERBASE_H__
#define __SHADERBASE_H__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"

#ifdef SHADER_API_PSSL
    #ifndef Texture2DMS
        #define Texture2DMS         MS_Texture2D
    #endif

    #ifndef SampleCmpLevelZero
        #define SampleCmpLevelZero  SampleCmpLOD0
    #endif

    #ifndef firstbithigh
        #define firstbithigh        FirstSetBit_Hi
    #endif
#endif

#ifdef MSAA_ENABLED
    TEXTURE2D_X_MSAA(float, g_depth_tex) : register( t0 );

    float FetchDepthMSAA(uint2 pixCoord, uint sampleIdx)
    {
        float zdpth = LOAD_TEXTURE2D_X_MSAA(g_depth_tex, pixCoord.xy, sampleIdx).x;
    #if UNITY_REVERSED_Z
        zdpth = 1.0 - zdpth;
    #endif
        return zdpth;
    }
#else
    TEXTURE2D_X(g_depth_tex) : register( t0 );

    float FetchDepth(uint2 pixCoord)
    {
        float zdpth = LOAD_TEXTURE2D_X(g_depth_tex, pixCoord.xy).x;
    #if UNITY_REVERSED_Z
            zdpth = 1.0 - zdpth;
    #endif
        return zdpth;
    }
#endif

#endif
