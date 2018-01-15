#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// TODO: Caution - For now the tesselation doesn't displace along the normal with Velocity shader as the previous previous position
// conflict with the normal in the semantic. This need to be fix! Also no per pixel displacement is possible either.

// Attributes
#define REQUIRE_TANGENT_TO_WORLD defined(_PIXEL_DISPLACEMENT)
#define REQUIRE_NORMAL defined(TESSELLATION_ON) || REQUIRE_TANGENT_TO_WORLD || defined(_VERTEX_WIND) || defined(_VERTEX_DISPLACEMENT)
#define REQUIRE_VERTEX_COLOR ((defined(_VERTEX_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT)) && defined(LAYERED_LIT_SHADER) && (defined(_LAYER_MASK_VERTEX_COLOR_MUL) || defined(_LAYER_MASK_VERTEX_COLOR_ADD))) || defined(_VERTEX_WIND)

// This first set of define allow to say which attributes will be use by the mesh in the vertex and domain shader (for tesselation)

// Tesselation require normal
#if REQUIRE_NORMAL
#define ATTRIBUTES_NEED_NORMAL
#endif
#if REQUIRE_TANGENT_TO_WORLD
#define ATTRIBUTES_NEED_TANGENT
#endif
#if REQUIRE_VERTEX_COLOR
#define ATTRIBUTES_NEED_COLOR
#endif

// About UV
// If we have a lit shader, only the UV0 is available for opacity or heightmap
// If we have a layered shader, any UV can be use for this. To reduce the number of variant we groupt UV0/UV1 and UV2/UV3 instead of having variant for UV0/UV1/UV2/UV3
// When UVX is present, we assume that UVX - 1 ... UV0 is present

#if defined(_VERTEX_DISPLACEMENT) || REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON) || defined(_TESSELLATION_DISPLACEMENT)
#define ATTRIBUTES_NEED_TEXCOORD0
    #ifdef LAYERED_LIT_SHADER
    #define ATTRIBUTES_NEED_TEXCOORD1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define ATTRIBUTES_NEED_TEXCOORD2
        #endif
        #if defined(_REQUIRE_UV3)
        #define ATTRIBUTES_NEED_TEXCOORD3
        #endif
    #endif
#endif


// Varying - Use for pixel shader
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#define VARYINGS_NEED_POSITION_WS

#if REQUIRE_TANGENT_TO_WORLD
#define VARYINGS_NEED_TANGENT_TO_WORLD
#endif

#if REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
#define VARYINGS_NEED_TEXCOORD0
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
