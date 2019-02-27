#define HAVE_MESH_MODIFICATION

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

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

#if SHADERPASS == SHADERPASS_SHADOWS
    #define USE_LEGACY_UNITY_MATRIX_VARIABLES
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#ifdef DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#if SHADERPASS == SHADERPASS_FORWARD
    // The light loop (or lighting architecture) is in charge to:
    // - Define light list
    // - Define the light loop
    // - Setup the constant/data
    // - Do the reflection hierarchy
    // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))
    #define HAS_LIGHTLOOP
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
#endif

#if SHADERPASS != SHADERPASS_DEPTH_ONLY || defined(WRITE_NORMAL_BUFFER)
    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define ATTRIBUTES_NEED_TANGENT // will be filled by ApplyMeshModification()
    #if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
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

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
#include "TerrainLitData.hlsl"

#if SHADERPASS == SHADERPASS_GBUFFER
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"
#elif SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
#elif SHADERPASS == SHADERPASS_SHADOWS || SHADERPASS == SHADERPASS_DEPTH_ONLY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
#elif SHADERPASS == SHADERPASS_FORWARD
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
#endif
