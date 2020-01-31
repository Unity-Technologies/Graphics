#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// Helper for the layered shader.
#if (defined(LAYERED_LIT_SHADER) && (defined(_NORMALMAP_TANGENT_SPACE0) || \
	 								 defined(_NORMALMAP_TANGENT_SPACE1) || \
	 								 defined(_NORMALMAP_TANGENT_SPACE2) || \
	 								 defined(_NORMALMAP_TANGENT_SPACE3)))
	#define _NORMALMAP_TANGENT_SPACE
#endif

// Attributes are vertex and domain shader inputs.
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0

#if (defined(_MATERIAL_FEATURE_ANISOTROPY) || defined(_NORMALMAP_TANGENT_SPACE) || defined(DEBUG_DISPLAY))
	// Tangent is used for: anisotropic lighting, tangent space (bent) normal maps.
	// We use the tangent stored as a vertex attribute only for UV0, and for UV1-3 it is
	// generated on the fly. However, it doesn't appear to be possible to know which UV set
	// (bent) normal maps use at compile time, so we must conservatively request the tangent.
	#define ATTRIBUTES_NEED_TANGENT
#endif

#if (defined(_REQUIRE_UV1) || defined(LIGHTMAP_ON) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) || defined(DEBUG_DISPLAY))
	#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if (defined(_REQUIRE_UV2) || defined(DYNAMICLIGHTMAP_ON) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) || defined(DEBUG_DISPLAY))
	#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if (defined(_REQUIRE_UV3) || defined(DEBUG_DISPLAY))
	#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if (defined(_REQUIRE_VERTEX_COLOR) || defined(DEBUG_DISPLAY))
	#define ATTRIBUTES_NEED_COLOR
#endif

// Varyings are pixel shader inputs (vertex and domain shader outputs).
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TEXCOORD0

#ifdef ATTRIBUTES_NEED_TANGENT
	#define VARYINGS_NEED_TANGENT_WS
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

#ifdef _DOUBLESIDED_ON
	#define VARYINGS_NEED_CULLFACE
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
