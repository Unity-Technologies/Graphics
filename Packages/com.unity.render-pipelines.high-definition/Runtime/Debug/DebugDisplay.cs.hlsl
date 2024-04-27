//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
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
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_PREV (5)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_ACCUM (6)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTION_SPEED_REJECTION (7)
#define FULLSCREENDEBUGMODE_CONTACT_SHADOWS (8)
#define FULLSCREENDEBUGMODE_CONTACT_SHADOWS_FADE (9)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_SHADOWS (10)
#define FULLSCREENDEBUGMODE_PRE_REFRACTION_COLOR_PYRAMID (11)
#define FULLSCREENDEBUGMODE_DEPTH_PYRAMID (12)
#define FULLSCREENDEBUGMODE_FINAL_COLOR_PYRAMID (13)
#define FULLSCREENDEBUGMODE_LIGHT_CLUSTER (14)
#define FULLSCREENDEBUGMODE_SCREEN_SPACE_GLOBAL_ILLUMINATION (15)
#define FULLSCREENDEBUGMODE_RECURSIVE_RAY_TRACING (16)
#define FULLSCREENDEBUGMODE_RAY_TRACED_SUB_SURFACE (17)
#define FULLSCREENDEBUGMODE_VOLUMETRIC_CLOUDS (18)
#define FULLSCREENDEBUGMODE_VOLUMETRIC_CLOUDS_SHADOW (19)
#define FULLSCREENDEBUGMODE_RAY_TRACING_ACCELERATION_STRUCTURE (20)
#define FULLSCREENDEBUGMODE_MAX_LIGHTING_FULL_SCREEN_DEBUG (21)
#define FULLSCREENDEBUGMODE_MIN_RENDERING_FULL_SCREEN_DEBUG (22)
#define FULLSCREENDEBUGMODE_MOTION_VECTORS (23)
#define FULLSCREENDEBUGMODE_WORLD_SPACE_POSITION (24)
#define FULLSCREENDEBUGMODE_NAN_TRACKER (25)
#define FULLSCREENDEBUGMODE_COLOR_LOG (26)
#define FULLSCREENDEBUGMODE_DEPTH_OF_FIELD_COC (27)
#define FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW (28)
#define FULLSCREENDEBUGMODE_QUAD_OVERDRAW (29)
#define FULLSCREENDEBUGMODE_LOCAL_VOLUMETRIC_FOG_OVERDRAW (30)
#define FULLSCREENDEBUGMODE_VERTEX_DENSITY (31)
#define FULLSCREENDEBUGMODE_REQUESTED_VIRTUAL_TEXTURE_TILES (32)
#define FULLSCREENDEBUGMODE_LENS_FLARE_DATA_DRIVEN (33)
#define FULLSCREENDEBUGMODE_MAX_RENDERING_FULL_SCREEN_DEBUG (34)
#define FULLSCREENDEBUGMODE_MIN_MATERIAL_FULL_SCREEN_DEBUG (35)
#define FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR (36)
#define FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR (37)
#define FULLSCREENDEBUGMODE_MAX_MATERIAL_FULL_SCREEN_DEBUG (38)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesDebugDisplay
// PackingRules = Exact
CBUFFER_START(ShaderVariablesDebugDisplay)
    float4 _DebugRenderingLayersColors[32];
    uint4 _DebugViewMaterialArray[11];
    float4 _DebugAPVSubdivColors[7];
    int _DebugLightingMode;
    int _DebugLightLayersMask;
    int _DebugShadowMapMode;
    int _DebugMipMapMode;
    int _DebugFullScreenMode;
    float _DebugTransparencyOverdrawWeight;
    int _DebugMipMapModeTerrainTexture;
    int _ColorPickerMode;
    float4 _DebugViewportSize;
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
    int _DebugIsLitShaderModeDeferred;
    int _DebugAOVOutput;
    float _ShaderVariablesDebugDisplayPad0;
    float _ShaderVariablesDebugDisplayPad1;
    float _ShaderVariablesDebugDisplayPad2;
CBUFFER_END


#endif
