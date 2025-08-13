#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

#define TERRAIN_DEFAULT_TEXTURE
TEXTURE2D(_BlackTex);        SAMPLER(sampler_BlackTex);
TEXTURE2D(_NormalBlank);     SAMPLER(sampler_NormalBlank);
TEXTURE2D(_DefaultWhiteTex);       SAMPLER(sampler_BilinearClamp);
SAMPLER(sampler_BilinearRepeat);

float4 _BlackTex_TexelSize = float4(1,1,1,1);
half4 _BlackTex_ST = half4(1,1,0,0);

#define _Control0 _Control
#define _Control1 _BlackTex

#define _Splat4 _BlackTex
#define _Splat5 _BlackTex
#define _Splat6 _BlackTex
#define _Splat7 _BlackTex
#define sampler_Splat4 sampler_BlackTex
#define sampler_Splat5 sampler_BlackTex
#define sampler_Splat6 sampler_BlackTex
#define sampler_Splat7 sampler_BlackTex

#define _Normal4 _NormalBlank
#define _Normal5 _NormalBlank
#define _Normal6 _NormalBlank
#define _Normal7 _NormalBlank
#define sampler_Normal4 sampler_NormalBlank
#define sampler_Normal5 sampler_NormalBlank
#define sampler_Normal6 sampler_NormalBlank
#define sampler_Normal7 sampler_NormalBlank

#define _Mask4 _BlackTex
#define _Mask5 _BlackTex
#define _Mask6 _BlackTex
#define _Mask7 _BlackTex
#define sampler_Mask4 sampler_BlackTex
#define sampler_Mask5 sampler_BlackTex
#define sampler_Mask6 sampler_BlackTex
#define sampler_Mask7 sampler_BlackTex

#define _Control1 _BlackTex
#define sampler_Control1 sampler_BlackTex

UnityTexture2D TerrainBuildUnityTexture2DStructInternal(Texture2D tex, SamplerState samplerstate, float4 texelSize, float4 scaleTranslate)
{
    UnityTexture2D result;
    result.tex = tex;
    result.samplerstate = samplerstate;
    result.texelSize = texelSize;
    result.scaleTranslate = scaleTranslate;
    return result;
}

#define DEF_TERRAIN_TEXTURE_LOAD(name, defaultName) UnityTexture2D TerrainBuildUnityTexture2DStructInternal##name(int index)     \
{                                                                                                                   \
    switch(index)                                                                                                   \
    {                                                                                                               \
    case 0:                                                                                                         \
        return TerrainBuildUnityTexture2DStructInternal(name##0, sampler_BilinearRepeat, _Splat0_TexelSize, _Splat0_ST);  \
    case 1:                                                                                                         \
        return TerrainBuildUnityTexture2DStructInternal(name##1, sampler_BilinearRepeat, _Splat1_TexelSize, _Splat1_ST);  \
    case 2:                                                                                                         \
        return TerrainBuildUnityTexture2DStructInternal(name##2, sampler_BilinearRepeat, _Splat2_TexelSize, _Splat2_ST);  \
    case 3:                                                                                                         \
        return TerrainBuildUnityTexture2DStructInternal(name##3, sampler_BilinearRepeat, _Splat3_TexelSize, _Splat3_ST);  \
    default:                                                                                                        \
        return TerrainBuildUnityTexture2DStructInternal(defaultName, sampler_BilinearRepeat, _BlackTex_TexelSize, _BlackTex_ST);  \
    }                                                                                                               \
}

#define DEF_TERRAIN_TEXTURE_AVAILABLE(name) float TerrainTextureAvailableInternal##name(int index)     \
{                                                                                                      \
    switch(index)                                                                                      \
    {                                                                                                  \
        case 0:                                                                                        \
        case 1:                                                                                        \
        case 2:                                                                                        \
        case 3:                                                                                        \
            return 1;                                                                                  \
        default:                                                                                       \
            return 0;                                                                                  \
    }                                                                                                  \
}

DEF_TERRAIN_TEXTURE_LOAD(_Splat, _BlackTex)
DEF_TERRAIN_TEXTURE_AVAILABLE(_Splat)
DEF_TERRAIN_TEXTURE_LOAD(_Normal, _NormalBlank)
DEF_TERRAIN_TEXTURE_AVAILABLE(_Normal)
DEF_TERRAIN_TEXTURE_LOAD(_Mask, _BlackTex)

float TerrainTextureAvailableInternal_Mask(int index)
{
    switch(index)
    {
        case 0:
            return _LayerHasMask0;
        case 1:
            return _LayerHasMask1;
        case 2:
            return _LayerHasMask2;
        case 3:
            return _LayerHasMask3;
        default:
            return 0;
    }
}

float TerrainTextureAvailableInternal_Control(int index)
{
    switch(index)
    {
        case 0:
        case 1:
            return 1;
        default:
            return 0;
    }
}

#define Unity_TerrainTextureAvailable(t, n) TerrainTextureAvailableInternal##t(n)

UnityTexture2D TerrainBuildUnityTextureControl(int index)
{
    switch(index)
    {
    case 0:
        return TerrainBuildUnityTexture2DStructInternal(_Control, sampler_Control, _Control_TexelSize, _Control_ST);
    default:
        return TerrainBuildUnityTexture2DStructInternal(_BlackTex, sampler_BlackTex, _BlackTex_TexelSize, _BlackTex_ST);
    }
}

UnityTexture2D TerrainBuildUnityTextureHoles(int index)
{
    #if defined(_ALPHATEST_ON)
    return TerrainBuildUnityTexture2DStructInternal(_TerrainHolesTexture, sampler_TerrainHolesTexture, _Control_TexelSize, _Control_ST);
    #else
    return TerrainBuildUnityTexture2DStructInternal(_DefaultWhiteTex, sampler_BilinearClamp, _BlackTex_TexelSize, _BlackTex_ST);
    #endif
}

#define DEF_TERRAIN_VALUE_LOAD(name, returnType, defaultVal) returnType Unity_Terrain##name(int index) \
{                                                                                                           \
    switch(index)                                                                                           \
    {                                                                                                       \
        case 0:                                                                                             \
            return name##0;                                                                                 \
        case 1:                                                                                             \
            return name##1;                                                                                 \
        case 2:                                                                                             \
            return name##2;                                                                                 \
        case 3:                                                                                             \
            return name##3;                                                                                 \
    }                                                                                                       \
    return defaultVal;                                                                                      \
}

DEF_TERRAIN_VALUE_LOAD(_NormalScale, float, float(1))
DEF_TERRAIN_VALUE_LOAD(_Metallic, float, float(0))
DEF_TERRAIN_VALUE_LOAD(_Smoothness, float, float(0.5))
DEF_TERRAIN_VALUE_LOAD(_DiffuseRemapScale, float4, float4(1,1,1,1))
DEF_TERRAIN_VALUE_LOAD(_MaskMapRemapOffset, float4, float4(0,0,0,0))
DEF_TERRAIN_VALUE_LOAD(_MaskMapRemapScale, float4, float4(1,1,1,1))

#define TerrainBuildUnityTexture2DStruct(name, index) TerrainBuildUnityTexture2DStructInternal##name(index)
#define TerrainBuildUnityTexture2DStructNoIndex(name) TerrainBuildUnityTexture2DStructInternal(name, sampler##name, name##_TexelSize, name##_ST)
