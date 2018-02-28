#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Attributes
#define REQUIRE_UV_FOR_TESSELATION (defined(TESSELLATION_ON) && defined(_TESSELLATION_DISPLACEMENT))
#define REQUIRE_VERTEX_COLOR_FOR_TESSELATION REQUIRE_UV_FOR_TESSELATION
#define REQUIRE_TANGENT_TO_WORLD (defined(_HEIGHTMAP) && defined(_PIXEL_DISPLACEMENT))

// This first set of define allow to say which attributes will be use by the mesh in the vertex and domain shader (for tesselation)

// Tesselation require normal
#if defined(TESSELLATION_ON) || REQUIRE_TANGENT_TO_WORLD || defined(_VERTEX_WIND)
#define ATTRIBUTES_NEED_NORMAL
#endif
#if REQUIRE_TANGENT_TO_WORLD
#define ATTRIBUTES_NEED_TANGENT
#endif

#ifdef _VERTEX_WIND
#define ATTRIBUTES_NEED_COLOR
#endif

// About UV
// When UVX is present, we assume that UVX - 1 ... UV0 is present

#define ATTRIBUTES_NEED_TEXCOORD0

#if REQUIRE_UV_FOR_TESSELATION || REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
    #define ATTRIBUTES_NEED_TEXCOORD1
    #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define ATTRIBUTES_NEED_TEXCOORD2
    #endif
    #if defined(_REQUIRE_UV3)
        #define ATTRIBUTES_NEED_TEXCOORD3
    #endif
#endif

#if REQUIRE_VERTEX_COLOR_FOR_TESSELATION
#define ATTRIBUTES_NEED_COLOR
#endif

// Varying - Use for pixel shader
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#if REQUIRE_TANGENT_TO_WORLD
#define VARYINGS_NEED_POSITION_WS // Required to get view vector
#define VARYINGS_NEED_TANGENT_TO_WORLD
#endif

#define VARYINGS_NEED_TEXCOORD0

#if REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
    #ifdef LAYERED_LIT_SHADER
    #define VARYINGS_NEED_TEXCOORD1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define VARYINGS_NEED_TEXCOORD2
        #endif
        #if defined(_REQUIRE_UV3)
        #define VARYINGS_NEED_TEXCOORD3
        #endif
    #endif
#endif

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
