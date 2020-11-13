//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DEBUGDISPLAY_CS_HLSL
#define DEBUGDISPLAY_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.FullScreenDebugMode:  static fields
//
#define FULLSCREENDEBUGMODE_NONE (0)
#define FULLSCREENDEBUGMODE_MIN_LIGHTING_FULL_SCREEN_DEBUG (1)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_AMBIENT_OCCLUSION (2)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS (3)
#define FULLSCREENDEBUGMODE_TRANSPARENT_SCREEN_SPACE_REFLECTIONS (4)
#define FULLSCREENDEBUGMODE_CONTACT_SHADOWS (5)
#define FULLSCREENDEBUGMODE_CONTACT_SHADOWS_FADE (6)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_SHADOWS (7)
#define FULLSCREENDEBUGMODE_PRE_REFRACTION_COLOR_PYRAMID (8)
#define FULLSCREENDEBUGMODE_DEPTH_PYRAMID (9)
#define FULLSCREENDEBUGMODE_FINAL_COLOR_PYRAMID (10)
#define FULLSCREENDEBUGMODE_LIGHT_CLUSTER (11)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_GLOBAL_ILLUMINATION (12)
#define FULLSCREENDEBUGMODE_RECURSIVE_RAY_TRACING (13)
#define FULLSCREENDEBUGMODE_RAY_TRACED_SUB_SURFACE (14)
#define FULLSCREENDEBUGMODE_MAX_LIGHTING_FULL_SCREEN_DEBUG (15)
#define FULLSCREENDEBUGMODE_MIN_RENDERING_FULL_SCREEN_DEBUG (16)
#define FULLSCREENDEBUGMODE_MOTION_VECTORS (17)
#define FULLSCREENDEBUGMODE_NAN_TRACKER (18)
#define FULLSCREENDEBUGMODE_COLOR_LOG (19)
#define FULLSCREENDEBUGMODE_DEPTH_OF_FIELD_COC (20)
#define FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW (21)
#define FULLSCREENDEBUGMODE_QUAD_OVERDRAW (22)
#define FULLSCREENDEBUGMODE_VERTEX_DENSITY (23)
#define FULLSCREENDEBUGMODE_REQUESTED_VIRTUAL_TEXTURE_TILES (24)
#define FULLSCREENDEBUGMODE_MAX_RENDERING_FULL_SCREEN_DEBUG (25)
#define FULLSCREENDEBUGMODE_MIN_MATERIAL_FULL_SCREEN_DEBUG (26)
#define FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR (27)
#define FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR (28)
#define FULLSCREENDEBUGMODE_MAX_MATERIAL_FULL_SCREEN_DEBUG (29)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_PREV (30)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_ACCUM (31)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesDebugDisplay
// PackingRules = Exact
CBUFFER_START(ShaderVariablesDebugDisplay)
    float4 _DebugRenderingLayersColors[32];
    uint4 _DebugViewMaterialArray[11];
    int _DebugLightingMode;
    int _DebugLightLayersMask;
    int _DebugShadowMapMode;
    int _DebugMipMapMode;
    int _DebugFullScreenMode;
    float _DebugTransparencyOverdrawWeight;
    int _DebugMipMapModeTerrainTexture;
    int _ColorPickerMode;
    float4 _DebugLightingAlbedo;
    float4 _DebugLightingSmoothness;
    float4 _DebugLightingNormal;
    float4 _DebugLightingAmbientOcclusion;
    float4 _DebugLightingSpecularColor;
    float4 _DebugLightingEmissiveColor;
    float4 _DebugLightingMaterialValidateHighColor;
    float4 _DebugLightingMaterialValidateLowColor;
    float4 _DebugLightingMaterialValidatePureMetalColor;
    float4 _MousePixelCoord;
    float4 _MouseClickPixelCoord;
    int _MatcapMixAlbedo;
    float _MatcapViewScale;
    int _DebugSingleShadowIndex;
    int _DebugProbeVolumeMode;
    float3 _DebugDisplayPad0;
CBUFFER_END


#endif
