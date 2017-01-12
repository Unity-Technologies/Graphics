#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Attributes
#define ATTRIBUTES_TESSELATION_WANT_UV (defined(TESSELLATION_ON) && (defined(_TESSELATION_DISPLACEMENT) || defined(_TESSELATION_DISPLACEMENT_PHONG)))

#if defined(TESSELLATION_ON) || (defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT))
#define ATTRIBUTES_WANT_NORMAL
#endif
#if defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT)
#define ATTRIBUTES_WANT_TANGENT
#endif
// About UV
// If we have a lit shader, only the UV0 is available for opacity or heightmap
// If we have a layered shader, any UV can be use for this. To reduce the number of variant we groupt UV0/UV1 and UV2/UV3 instead of having variant for UV0/UV1/UV2/UV3
// When UVX is present, we assume that UVX - 1 ... UV0 is present
#if ATTRIBUTES_TESSELATION_WANT_UV || (defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT)) || defined(_ALPHATEST_ON)
#define ATTRIBUTES_WANT_UV0
    #ifdef LAYERED_LIT_SHADER
    #define ATTRIBUTES_WANT_UV1
        #ifdef _REQUIRE_UV2_OR_UV3
        #define ATTRIBUTES_WANT_UV2
        #define ATTRIBUTES_WANT_UV3
        #endif
    #endif
#endif

// Varying - Use for pixel shader
#if defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT)
#define VARYING_WANT_TANGENT_TO_WORLD
#endif

#if defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT) || defined(_ALPHATEST_ON)
#define VARYING_WANT_TEXCOORD0
    #ifdef LAYERED_LIT_SHADER
    #define VARYING_WANT_TEXCOORD1
        #ifdef _REQUIRE_UV2_OR_UV3
        #define VARYING_WANT_TEXCOORD2
        #define VARYING_WANT_TEXCOORD3
        #endif
    #endif
#endif

// Varying DS - Use for domain shader, are deduced from the above

// Include structure declaration and packing functions
#include "LitAttributesVarying.hlsl"

#include "LitVertexShare.hlsl"
