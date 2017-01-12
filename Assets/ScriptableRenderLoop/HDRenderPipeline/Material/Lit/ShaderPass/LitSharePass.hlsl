#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Attributes
#define ATTRIBUTES_WANT_NORMAL
#define ATTRIBUTES_WANT_TANGENT // Always present as we require it also in case of anisotropic lighting
#define ATTRIBUTES_WANT_UV0
#define ATTRIBUTES_WANT_UV1
#define ATTRIBUTES_WANT_COLOR

#if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3) || defined(DYNAMICLIGHTMAP_ON) || (SHADERPASS == SHADERPASS_DEBUG_VIEW_MATERIAL) 
#define ATTRIBUTES_WANT_UV2
#endif
#if defined(_REQUIRE_UV3) || (SHADERPASS == SHADERPASS_DEBUG_VIEW_MATERIAL) 
#define ATTRIBUTES_WANT_UV3
#endif

#define VARYING_WANT_POSITION_WS
#define VARYING_WANT_TANGENT_TO_WORLD
#define VARYING_WANT_TEXCOORD0
#define VARYING_WANT_TEXCOORD1
#ifdef ATTRIBUTES_WANT_UV2
#define VARYING_WANT_TEXCOORD2
#endif
#ifdef ATTRIBUTES_WANT_UV3
#define VARYING_WANT_TEXCOORD3
#endif
#define VARYING_WANT_COLOR

#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
#define VARYING_WANT_CULLFACE
#endif

// Varying DS - Use for domain shader, are deduced from the above

// Include structure declaration and packing functions
#include "LitAttributesVarying.hlsl"

#include "LitVertexShare.hlsl"