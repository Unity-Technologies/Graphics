#ifndef UNITY_INPUTS_SHIM_INCLUDED
#define UNITY_INPUTS_SHIM_INCLUDED

// The built-in pipeline is not able to include UnityInput.hlsl due to it defining a bunch of
// inputs/constants/variables that are also defined in built-in. This file defines the few extra
// functions missing from there that are also needed.

// Use the include guard to force UnityInput.hlsl to not get included
#define BUILTIN_SHADER_VARIABLES_INCLUDED

#include "UnityShaderVariables.cginc"

// scaleBias.x = flipSign
// scaleBias.y = scale
// scaleBias.z = bias
// scaleBias.w = unused
//uniform float4 _ScaleBias;
uniform float4 _ScaleBiasRt;

// _TimeParameters is not defined in built-in, but is expected to be defined as [t, sin(t), cos(t), 0].
// This can be dynamically constructed from other variables knowing that:
// float4 _Time; // (t/20, t, t*2, t*3)
// float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
// float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 GetTimeParameters()
{
    return float4(_Time.y, _SinTime.w, _CosTime.w, 0);
}
#define _TimeParameters GetTimeParameters()

float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif // UNITY_INPUTS_SHIM_INCLUDED
