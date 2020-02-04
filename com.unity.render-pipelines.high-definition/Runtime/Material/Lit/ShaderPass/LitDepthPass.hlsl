#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Helper for displacement.
#if (defined(_VERTEX_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT) || defined(_PIXEL_DISPLACEMENT))
    #define DISPLACEMENT_MAP
#endif

/* Attributes are vertex shader inputs. */

#ifdef DISPLACEMENT_MAP
    #define ATTRIBUTES_NEED_NORMAL
#endif
#ifdef _PIXEL_DISPLACEMENT
    #define ATTRIBUTES_NEED_TANGENT
#endif
#if (defined(_VERTEX_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT) || \
     (defined(LAYERED_LIT_SHADER) && (defined(_LAYER_MASK_VERTEX_COLOR_MUL) || defined(_LAYER_MASK_VERTEX_COLOR_ADD))) \
    )
#define ATTRIBUTES_NEED_COLOR
#endif

#if (defined(DISPLACEMENT_MAP) || defined(_ALPHATEST_ON))
// {
    #define ATTRIBUTES_NEED_TEXCOORD0

#if (defined(_REQUIRE_UV01) || defined(_REQUIRE_UV012) || defined(_REQUIRE_UV0123))
    #define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if (defined(_REQUIRE_UV012) || defined(_REQUIRE_UV0123))
    #define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if (defined(_REQUIRE_UV0123))
    #define ATTRIBUTES_NEED_TEXCOORD3
#endif
// }
#endif // (defined(DISPLACEMENT_MAP) || defined(_ALPHATEST_ON))

/* Varyings are pixel shader inputs (vertex or domain shader outputs). */

#ifdef _PIXEL_DISPLACEMENT
    #define VARYINGS_NEED_NORMAL_WS
    #define VARYINGS_NEED_TANGENT_WS
#endif

#if (defined(_PIXEL_DISPLACEMENT) || defined(_ALPHATEST_ON))
// {
    #define VARYINGS_NEED_POSITION_WS // TODO: compute from depth

#ifdef ATTRIBUTES_NEED_TEXCOORD0
    #define VARYINGS_NEED_TEXCOORD0
#endif

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
// }
#elif (defined(LOD_FADE_CROSSFADE))
    #define VARYINGS_NEED_POSITION_WS // TODO: compute from depth
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
