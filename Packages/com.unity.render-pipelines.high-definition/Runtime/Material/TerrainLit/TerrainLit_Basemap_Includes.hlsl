#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#define UNITY_TERRAIN_CB_DEBUG_VARS \
    float4 _MainTex_TexelSize;      \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);
