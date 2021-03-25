#ifndef UNITY_HEADER_HD_INCLUDED
#define UNITY_HEADER_HD_INCLUDED

#define SHADERGRAPH_OBJECT_POSITION GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23)

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

#endif // UNITY_HEADER_HD_INCLUDED
