#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT // Always present as we require it also in case of anisotropic lighting
#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0

// This include will define the various Attributes/Varyings structure
#include "../../ShaderPass/VaryingMesh.hlsl"
