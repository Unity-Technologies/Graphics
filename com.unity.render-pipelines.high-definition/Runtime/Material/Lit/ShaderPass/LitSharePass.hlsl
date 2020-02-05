#ifndef SHADERPASS
	#error Undefined_SHADERPASS
#endif

#if ((SHADERPASS == SHADERPASS_DEPTH_ONLY) || (SHADERPASS == SHADERPASS_SHADOWS) || \
	 (SHADERPASS == SHADERPASS_DISTORTION) || (SHADERPASS == SHADERPASS_MOTION_VECTORS))
// {
	#error Wrong_SHADERPASS
// }
#endif

// Add local helper definitions.
#include "LitParamDefsAdd.hlsl"

/* Varyings_PS are pixel shader inputs (vertex or domain shader outputs). */

#define VARYINGS_NEED_POSITION_WS // TODO: remove, compute from depth
#define VARYINGS_NEED_NORMAL_WS   // Always!

#if (defined(_MATERIAL_FEATURE_ANISOTROPY) || defined(NORMAL_MAP_TS) || defined(_PIXEL_DISPLACEMENT) || defined(DEBUG_DISPLAY))
	// We use the tangent stored as a vertex attribute only for UV0, and for UV1-3 it is generated
	// on the fly. However, it is not possible to know which UV set (bent) normal maps and/or
	// per-pixel displacement use at compile time, so we must conservatively request the tangent.
	#define VARYINGS_NEED_TANGENT_WS
#endif

#define VARYINGS_NEED_TEXCOORD0 // Always!

#if (defined(TEX_UV1) || defined(LIGHTMAP_ON) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) || defined(DEBUG_DISPLAY))
	#define VARYINGS_NEED_TEXCOORD1
#endif

#if (defined(TEX_UV2) || defined(DYNAMICLIGHTMAP_ON) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) || defined(DEBUG_DISPLAY))
	#define VARYINGS_NEED_TEXCOORD2
#endif

#if (defined(TEX_UV3) || defined(DEBUG_DISPLAY))
	#define VARYINGS_NEED_TEXCOORD3
#endif

#if (defined(TEX_COL) || defined(DEBUG_DISPLAY))
	#define VARYINGS_NEED_COLOR
#endif

#if (defined(_DOUBLESIDED_ON) && defined(SHADER_STAGE_FRAGMENT))
	#define VARYINGS_NEED_CULLFACE
#endif

// Varyings_DS are domain shader inputs.
// Attributes_VS are vertex shader inputs.
#include "LitVaryingsDsAndAttributes.hlsl"

// Remove local helper definitions.
#include "LitParamDefsRem.hlsl"

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
