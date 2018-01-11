#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// 3 interpolators for decal normal map tangent space
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
