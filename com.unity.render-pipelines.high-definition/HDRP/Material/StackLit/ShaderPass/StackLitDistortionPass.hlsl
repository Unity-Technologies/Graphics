#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// NEWLITTODO : Handling of TESSELATION, DISPLACEMENT, HEIGHTMAP, WIND

#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_TEXCOORD0

// This include will define the various Attributes/Varyings structure
#include "HDRP/ShaderPass/VaryingMesh.hlsl"
