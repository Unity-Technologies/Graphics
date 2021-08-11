#ifndef TERRAIN_SG_PROPS
#define TERRAIN_SG_PROPS

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap_Includes.hlsl"
TEXTURE2D(_Control0);
SAMPLER(sampler_Control0);
float4 _Control0_ST;

#ifndef TERRAIN_SPLAT_BASEPASS
// Include splat properties for all non-basepass terrain shaders
#define DECLARE_TERRAIN_LAYER_TEXS(n)   \
    TEXTURE2D(_Splat##n);               \
    TEXTURE2D(_Normal##n);              \
    TEXTURE2D(_Mask##n)

DECLARE_TERRAIN_LAYER_TEXS(0);
DECLARE_TERRAIN_LAYER_TEXS(1);
DECLARE_TERRAIN_LAYER_TEXS(2);
DECLARE_TERRAIN_LAYER_TEXS(3);
#ifdef _TERRAIN_8_LAYERS
DECLARE_TERRAIN_LAYER_TEXS(4);
DECLARE_TERRAIN_LAYER_TEXS(5);
DECLARE_TERRAIN_LAYER_TEXS(6);
DECLARE_TERRAIN_LAYER_TEXS(7);
TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER_TEXS

SAMPLER(sampler_Splat0);
#endif

// Even though terrain only uses these during base pass, we want to enable custom terrain implementations to sample from basemap
// even in non-basemap shaders.
TEXTURE2D(_MainTex);
TEXTURE2D(_MetallicTex);
SAMPLER(sampler_MainTex);

// Non-splatmap property declarations
#ifdef UNITY_INSTANCING_ENABLED
    TEXTURE2D(_TerrainHeightmapTexture);
    TEXTURE2D(_TerrainNormalmapTexture);
    #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
        SAMPLER(sampler_TerrainNormalmapTexture);
    #endif
#endif

CBUFFER_START(UnityTerrain)
    UNITY_TERRAIN_CB_VARS
#ifdef UNITY_INSTANCING_ENABLED
    float4 _TerrainHeightmapRecipSize;  // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale;      // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif
#ifdef DEBUG_DISPLAY
    UNITY_TERRAIN_CB_DEBUG_VARS
#endif
// ShaderGraph already defines these
//#ifdef SCENESELECTIONPASS
//    int _ObjectId;
//    int _PassValue;
//#endif
CBUFFER_END

#ifdef _ALPHATEST_ON
TEXTURE2D(_TerrainHolesTexture);
SAMPLER(sampler_TerrainHolesTexture);
#endif

UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

// Declare distortion variables just to make the code compile with the Debug Menu.
// See LitBuiltinData.hlsl:73.
TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;

#endif
