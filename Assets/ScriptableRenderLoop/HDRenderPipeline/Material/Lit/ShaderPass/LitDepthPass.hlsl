#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Attributes
#define REQUIRE_UV_FOR_TESSELATION (defined(TESSELLATION_ON) && (defined(_TESSELATION_DISPLACEMENT) || defined(_TESSELATION_DISPLACEMENT_PHONG)))
#define REQUIRE_TANGENT_TO_WORLD (defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT))

// Tesselation require normal
#if defined(TESSELLATION_ON) || REQUIRE_TANGENT_TO_WORLD
#define ATTRIBUTES_WANT_NORMAL
#endif
#if REQUIRE_TANGENT_TO_WORLD
#define ATTRIBUTES_WANT_TANGENT
#endif
// About UV
// If we have a lit shader, only the UV0 is available for opacity or heightmap
// If we have a layered shader, any UV can be use for this. To reduce the number of variant we groupt UV0/UV1 and UV2/UV3 instead of having variant for UV0/UV1/UV2/UV3
// When UVX is present, we assume that UVX - 1 ... UV0 is present
#if REQUIRE_UV_FOR_TESSELATION || REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
#define ATTRIBUTES_WANT_UV0
    #ifdef LAYERED_LIT_SHADER
    #define ATTRIBUTES_WANT_UV1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define ATTRIBUTES_WANT_UV2
        #endif
        #if defined(_REQUIRE_UV3)
        #define ATTRIBUTES_WANT_UV3
        #endif
    #endif
#endif

// Varying - Use for pixel shader
#if REQUIRE_TANGENT_TO_WORLD
#define VARYING_WANT_TANGENT_TO_WORLD
#endif

#if REQUIRE_TANGENT_TO_WORLD || defined(_ALPHATEST_ON)
#define VARYING_WANT_TEXCOORD0
    #ifdef LAYERED_LIT_SHADER
    #define VARYING_WANT_TEXCOORD1
        #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
        #define VARYING_WANT_TEXCOORD2
        #endif
        #if defined(_REQUIRE_UV3)
        #define VARYING_WANT_TEXCOORD3
        #endif
    #endif
#endif

// Varying DS - Use for domain shader, are deduced from the above

// Include structure declaration and packing functions
#include "LitAttributesVarying.hlsl"

#include "LitVertexShare.hlsl"
