#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

$Material.SubsurfaceScattering:     #define _MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1
$Material.Transmission:             #define _MATERIAL_FEATURE_TRANSMISSION 1
$Material.Anisotropy:               #define _MATERIAL_FEATURE_ANISOTROPY 1
$Material.Iridescence:              #define _MATERIAL_FEATURE_IRIDESCENCE 1
$Material.SpecularColor:            #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
$Material.ClearCoat:                #define _MATERIAL_FEATURE_CLEAR_COAT
$AmbientOcclusion:                  #define _AMBIENT_OCCLUSION 1
$SpecularOcclusionFromAO:           #define _SPECULAR_OCCLUSION_FROM_AO 1
$SpecularOcclusionFromAOBentNormal: #define _SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1
$SpecularOcclusionCustom:           #define _SPECULAR_OCCLUSION_CUSTOM 1
$Specular.EnergyConserving:         #define _ENERGY_CONSERVING_SPECULAR 1
$Specular.AA:                       #define _ENABLE_GEOMETRIC_SPECULAR_AA 1
$RefractionBox:                     #define _REFRACTION_PLANE 1
$RefractionSphere:                  #define _REFRACTION_SPHERE 1
$RefractionThin:                    #define _REFRACTION_THIN 1

// This shader support recursive rendering for raytracing
//#define HAVE_RECURSIVE_RENDERING

#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
#define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#define TERRAIN_DEFAULT_TEXTURE
TEXTURE2D(_BlackTex);        SAMPLER(sampler_BlackTex);
TEXTURE2D(_NormalBlank);     SAMPLER(sampler_NormalBlank);
TEXTURE2D(_DefaultWhiteTex);       SAMPLER(sampler_BilinearClamp);
SAMPLER(sampler_BilinearRepeat);
SAMPLER(sampler_Control1);

float4 _BlackTex_TexelSize = float4(1,1,1,1);
half4 _BlackTex_ST = half4(1,1,0,0);



#if defined(_ALPHATEST_ON) && !defined(_HOLES_TEXTURE_DEF)
#define _HOLES_TEXTURE_DEF
TEXTURE2D(_TerrainHolesTexture);
SAMPLER(sampler_TerrainHolesTexture);
#endif

UnityTexture2D TerrainBuildUnityTexture2DStructInternal(Texture2D tex, SamplerState samplerstate, float4 texelSize, float4 scaleTranslate)
{
    UnityTexture2D result;
    result.tex = tex;
    result.samplerstate = samplerstate;
    result.texelSize = texelSize;
    result.scaleTranslate = scaleTranslate;
    return result;
}


#ifndef _TERRAIN_8_LAYERS
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

#define DEF_TERRAIN_VALUE_LOAD(name, returnType, defaultVal) returnType Unity_Terrain##name(int index)      \
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

#else

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
    case 4:                                                                                                         \
        return TerrainBuildUnityTexture2DStructInternal(name##4, sampler_BilinearRepeat, _Splat4_TexelSize, _Splat4_ST);  \
    case 5:                                                                                                                 \
        return TerrainBuildUnityTexture2DStructInternal(name##5, sampler_BilinearRepeat, _Splat5_TexelSize, _Splat5_ST);                \
    case 6:                                                                                                                     \
        return TerrainBuildUnityTexture2DStructInternal(name##6, sampler_BilinearRepeat, _Splat6_TexelSize, _Splat6_ST);                \
    case 7:                                                                                                                     \
        return TerrainBuildUnityTexture2DStructInternal(name##7, sampler_BilinearRepeat, _Splat7_TexelSize, _Splat7_ST);                \
    default:                                                                                                                    \
        return TerrainBuildUnityTexture2DStructInternal(defaultName, sampler_BilinearRepeat, _BlackTex_TexelSize, _BlackTex_ST);  \
    }                                                                                                                           \
}

#define DEF_TERRAIN_TEXTURE_AVAILABLE(name) float TerrainTextureAvailableInternal##name(int index)     \
{                                                                                                      \
    switch(index)                                                                                      \
    {                                                                                                  \
        case 0:                                                                                        \
        case 1:                                                                                        \
        case 2:                                                                                        \
        case 3:                                                                                        \
        case 4:                                                                                        \
        case 5:                                                                                        \
        case 6:                                                                                        \
        case 7:                                                                                        \
            return 1;                                                                                  \
        default:                                                                                       \
            return 0;                                                                                  \
    }                                                                                                  \
}

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
    case 4:
        return _LayerHasMask4;
    case 5:
        return _LayerHasMask5;
    case 6:
        return _LayerHasMask6;
    case 7:
        return _LayerHasMask7;
    default:
        return 0;
    }
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
        case 4:                                                                                             \
            return name##4;                                                                                 \
        case 5:                                                                                             \
            return name##5;                                                                                 \
        case 6:                                                                                             \
            return name##6;                                                                                 \
        case 7:                                                                                             \
            return name##7;                                                                                 \
    }                                                                                                       \
    return defaultVal;                                                                                      \
}

#endif

DEF_TERRAIN_TEXTURE_LOAD(_Splat, _BlackTex)
DEF_TERRAIN_TEXTURE_AVAILABLE(_Splat)
DEF_TERRAIN_TEXTURE_LOAD(_Normal, _NormalBlank)
DEF_TERRAIN_TEXTURE_AVAILABLE(_Normal)
DEF_TERRAIN_TEXTURE_LOAD(_Mask, _BlackTex)

DEF_TERRAIN_VALUE_LOAD(_NormalScale, float, float(1))
DEF_TERRAIN_VALUE_LOAD(_Metallic, float, float(0))
DEF_TERRAIN_VALUE_LOAD(_Smoothness, float, float(0.5))
DEF_TERRAIN_VALUE_LOAD(_DiffuseRemapScale, float4, float4(1,1,1,1))
DEF_TERRAIN_VALUE_LOAD(_MaskMapRemapOffset, float4, float4(0,0,0,0))
DEF_TERRAIN_VALUE_LOAD(_MaskMapRemapScale, float4, float4(1,1,1,1))

#ifndef _TERRAIN_8_LAYERS
float TerrainTextureAvailableInternal_Control(int index)
{
    return index==0;
}
UnityTexture2D TerrainBuildUnityTextureControl(int index)
{
    switch(index)
    {
    case 0:
        return TerrainBuildUnityTexture2DStructInternal(_Control0, sampler_Control0, _Control0_TexelSize, float4(1,1,0,0));
    default:
        return TerrainBuildUnityTexture2DStructInternal(_BlackTex, sampler_BlackTex, _BlackTex_TexelSize, _BlackTex_ST);
    }
}
#else
float TerrainTextureAvailableInternal_Control(int index)
{
    return index==0||index==1;
}
UnityTexture2D TerrainBuildUnityTextureControl(int index)
{
    switch(index)
    {
    case 0:
        return TerrainBuildUnityTexture2DStructInternal(_Control0, sampler_Control0, _Control0_TexelSize, float4(1,1,0,0));
    case 1:
        return TerrainBuildUnityTexture2DStructInternal(_Control1, sampler_Control1, _Control0_TexelSize, float4(1,1,0,0));
    default:
        return TerrainBuildUnityTexture2DStructInternal(_BlackTex, sampler_BlackTex, _BlackTex_TexelSize, _BlackTex_ST);
    }
}
#endif
#define Unity_TerrainTextureAvailable(t, n) TerrainTextureAvailableInternal##t(n)

UnityTexture2D TerrainBuildUnityTextureHoles(int index)
{
    #if defined(_ALPHATEST_ON)
    return TerrainBuildUnityTexture2DStructInternal(_TerrainHolesTexture, sampler_TerrainHolesTexture, _Control0_TexelSize, float4(1,1,0,0));
    #else
    return TerrainBuildUnityTexture2DStructInternal(_DefaultWhiteTex, sampler_BilinearClamp, _BlackTex_TexelSize, _BlackTex_ST);
    #endif
}

#define TerrainBuildUnityTexture2DStruct(name, index) TerrainBuildUnityTexture2DStructInternal##name(index)
#define TerrainBuildUnityTexture2DStructNoIndex(name) TerrainBuildUnityTexture2DStructInternal(name, sampler##name, name##_TexelSize, name##_ST)

