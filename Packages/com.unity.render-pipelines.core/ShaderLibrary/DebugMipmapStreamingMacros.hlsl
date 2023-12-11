#ifndef UNITY_DEBUG_MIPMAP_STREAMING_MACROS_INCLUDED
#define UNITY_DEBUG_MIPMAP_STREAMING_MACROS_INCLUDED

// Beware that this macro is used in constant buffers, so this should not change in size based on conditionals
#define UNITY_TEXTURE_STREAMING_DEBUG_VARS        \
    float4 unity_MipmapStreaming_DebugTex_ST;        \
    float4 unity_MipmapStreaming_DebugTex_TexelSize; \
    float4 unity_MipmapStreaming_DebugTex_MipInfo;   \
    float4 unity_MipmapStreaming_DebugTex_StreamInfo;

// Beware that this macro is used in constant buffers, so this should not change in size based on conditionals
#define UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(tex) \
    float4 tex##_MipInfo;                                  \
    float4 tex##_StreamInfo;

#endif // UNITY_DEBUG_MIPMAP_STREAMING_MACROS_INCLUDED
