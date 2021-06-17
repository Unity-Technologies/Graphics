// Setup a define to say we are an unlit shader
#define SHADER_UNLIT
$EnableShadowMatte: #define _ENABLE_SHADOW_MATTE

// Following Macro are only used by Unlit material
#if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
#define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
#define HAS_LIGHTLOOP
#endif
