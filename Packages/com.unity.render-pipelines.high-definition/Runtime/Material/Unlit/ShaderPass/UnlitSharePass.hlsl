#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_NEED_TEXCOORD0

#if defined(DEBUG_DISPLAY) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
// For the meta pass with emissive we require UV1 and/or UV2
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#ifdef EDITOR_VISUALIZATION
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(_ENABLE_FOG_ON_TRANSPARENT) || (SHADERPASS == SHADERPASS_MOTION_VECTORS)
#define VARYINGS_NEED_POSITION_WS
#endif

#define VARYINGS_NEED_TEXCOORD0

#if (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
#ifdef ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD1
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD2
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
#define VARYINGS_NEED_TEXCOORD3
#endif
#endif

// This include will define the various Attributes/Varyings structure
#if (SHADERPASS < SHADERPASS_RAYTRACING) || (SHADERPASS > SHADERPASS_RAY_TRACING_DEBUG)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
#endif
