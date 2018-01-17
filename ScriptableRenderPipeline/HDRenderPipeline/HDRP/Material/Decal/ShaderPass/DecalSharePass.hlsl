#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// need 3 x float3 interpolators...
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
