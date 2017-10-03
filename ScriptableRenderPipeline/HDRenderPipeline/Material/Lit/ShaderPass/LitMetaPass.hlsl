#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2

#ifdef _VERTEX_WIND
#define ATTRIBUTES_NEED_COLOR
#define ATTRIBUTES_NEED_NORMAL
#endif

#define REQUIRE_VERTEX_COLOR defined(LAYERED_LIT_SHADER) && (defined(_LAYER_MASK_VERTEX_COLOR_MUL) || defined(_LAYER_MASK_VERTEX_COLOR_ADD))

#if REQUIRE_VERTEX_COLOR
#define ATTRIBUTES_NEED_COLOR
#endif

#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1

#if REQUIRE_VERTEX_COLOR
#define VARYINGS_NEED_COLOR
#endif

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
