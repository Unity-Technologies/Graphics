
#if 0 // TODO : remove or not?
#define HAVE_MESH_MODIFICATION

#if SHADERPASS == SHADERPASS_GBUFFER && !defined(DEBUG_DISPLAY)
    // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
#endif

#if SHADERPASS == SHADERPASS_FORWARD && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(DEBUG_DISPLAY)
    // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
#endif

#if defined(_ALPHATEST_ON)
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define VARYINGS_NEED_TEXCOORD0
#endif

#if SHADERPASS != SHADERPASS_DEPTH_ONLY || defined(WRITE_NORMAL_BUFFER)
    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define ATTRIBUTES_NEED_TANGENT // will be filled by ApplyMeshModification()
    #if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #ifdef EDITOR_VISUALIZATION
        #define ATTRIBUTES_NEED_TEXCOORD3
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define VARYINGS_NEED_TEXCOORD3
        #endif
    #endif
    // Varying - Use for pixel shader
    // This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
    #define VARYINGS_NEED_POSITION_WS
    #define VARYINGS_NEED_TANGENT_TO_WORLD
    #define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    // With per-pixel normal enabled, tangent space is created in the pixel shader.
    #undef ATTRIBUTES_NEED_NORMAL
    #undef ATTRIBUTES_NEED_TANGENT
    #undef VARYINGS_NEED_TANGENT_TO_WORLD
#endif
#endif

#if SHADERPASS == SHADERPASS_DEPTH_ONLY
    #ifdef WRITE_NORMAL_BUFFER
        #if defined(_NORMALMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Normal0
        #elif defined(_MASKMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
        #endif
    #endif
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
