// Setup a define to say we are an unlit shader
#define SHADER_UNLIT
$EnableShadowMatte: #define _ENABLE_SHADOW_MATTE

// Following Macro are only used by Unlit material
#if defined(_ENABLE_SHADOW_MATTE)
    #if SHADERPASS == SHADERPASS_FORWARD_UNLIT
        #pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
    #elif SHADERPASS == SHADERPASS_PATH_TRACING
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    #endif

// We don't want to have the lightloop defined for the ray tracing passes, but we do for the rasterisation and path tracing shader passes.
#if !defined(SHADER_STAGE_RAY_TRACING) || SHADERPASS == SHADERPASS_PATH_TRACING
    #define HAS_LIGHTLOOP
#endif
#endif
