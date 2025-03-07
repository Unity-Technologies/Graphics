#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#if defined(WRITE_DECAL_BUFFER) || (defined(WRITE_RENDERING_LAYER) && !defined(_DISABLE_DECALS))
#define OUTPUT_DECAL_BUFER
#endif

// Attributes
#define REQUIRE_TANGENT_TO_WORLD ((defined(_PIXEL_DISPLACEMENT) && defined(_DEPTHOFFSET_ON)) || (defined(_ALPHATEST_ON) && defined(_MAPPING_TRIPLANAR)))
#define REQUIRE_NORMAL defined(TESSELLATION_ON) || REQUIRE_TANGENT_TO_WORLD || defined(_VERTEX_DISPLACEMENT) || defined(OUTPUT_DECAL_BUFER)
#define REQUIRE_VERTEX_COLOR (defined(_VERTEX_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT) || (defined(LAYERED_LIT_SHADER) && (defined(_LAYER_MASK_VERTEX_COLOR_MUL) || defined(_LAYER_MASK_VERTEX_COLOR_ADD))))

// This first set of define allow to say which attributes will be use by the mesh in the vertex and domain shader (for tesselation)

// Tesselation require normal
#if REQUIRE_NORMAL
#define ATTRIBUTES_NEED_NORMAL
#endif
#if REQUIRE_TANGENT_TO_WORLD || defined(OUTPUT_DECAL_BUFER)
#define ATTRIBUTES_NEED_TANGENT
#endif
#if REQUIRE_VERTEX_COLOR
#define ATTRIBUTES_NEED_COLOR
#endif

// About UV
// When UVX is present, we assume that UVX - 1 ... UV0 is present

#if defined(_VERTEX_DISPLACEMENT) || REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON) || defined(_TESSELLATION_DISPLACEMENT)
    #define ATTRIBUTES_NEED_TEXCOORD0
    #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        // UV1 is technically not used during the depth pass, but it must be available whenever UV2 or UV3 is required due to
        // the UV rules noted above. (UV[X] implies existence of UV[X - 1])
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
    #endif
    #if defined(_REQUIRE_UV3)
        #define ATTRIBUTES_NEED_TEXCOORD3
    #endif
#endif

// Varying - Use for pixel shader
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#if REQUIRE_TANGENT_TO_WORLD || defined(OUTPUT_DECAL_BUFER)
#define VARYINGS_NEED_TANGENT_TO_WORLD
#endif

#if REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
    #define VARYINGS_NEED_POSITION_WS // Required to get view vector and to get planar/triplanar mapping working
    #define VARYINGS_NEED_TEXCOORD0
    #ifdef ATTRIBUTES_NEED_TEXCOORD1
    #define VARYINGS_NEED_TEXCOORD1
    #endif
    #ifdef ATTRIBUTES_NEED_TEXCOORD2
    #define VARYINGS_NEED_TEXCOORD2
    #endif
    #ifdef ATTRIBUTES_NEED_TEXCOORD3
    #define VARYINGS_NEED_TEXCOORD3
    #endif
    #ifdef ATTRIBUTES_NEED_COLOR
    #define VARYINGS_NEED_COLOR
    #endif
#elif defined(LOD_FADE_CROSSFADE)
    #define VARYINGS_NEED_POSITION_WS // Required to get view vector use in cross fade effect
#endif

#ifdef _DOUBLESIDED_ON
#define VARYINGS_NEED_CULLFACE
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
