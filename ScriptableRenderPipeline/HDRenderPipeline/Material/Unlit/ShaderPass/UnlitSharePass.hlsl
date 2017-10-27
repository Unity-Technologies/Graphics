#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_NEED_TEXCOORD0

#if defined(_ENABLE_FOG_ON_TRANSPARENT)
#define VARYINGS_NEED_POSITION_WS
#endif

#define VARYINGS_NEED_TEXCOORD0

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
