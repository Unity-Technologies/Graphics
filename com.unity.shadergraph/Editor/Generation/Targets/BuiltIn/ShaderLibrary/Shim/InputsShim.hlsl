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

float4x4 InvertProjectionMatrix(float4x4 proj)
{
    float det = 0;

    // Calculate the determinant of upper left 3x3 sub-matrix and
    // determine if the matrix is singular.
    det += proj[0][0] * proj[1][1] * proj[2][2];
    det += proj[1][0] * proj[2][1] * proj[0][2];
    det += proj[2][0] * proj[0][1] * proj[1][2];
    det -= proj[2][0] * proj[1][1] * proj[0][2];
    det -= proj[1][0] * proj[0][1] * proj[2][2];
    det -= proj[0][0] * proj[2][1] * proj[1][2];

    float4x4 invProj = 0;

    if (det < FLT_EPS)
        return invProj;

    det = 1.0F / det;
    invProj[0][0] = ( (proj[1][1] * proj[2][2] - proj[2][1] * proj[1][2]) * det);
    invProj[0][1] = (-(proj[0][1] * proj[2][2] - proj[2][1] * proj[0][2]) * det);
    invProj[0][2] = ( (proj[0][1] * proj[1][2] - proj[1][1] * proj[0][2]) * det);
    invProj[1][0] = (-(proj[1][0] * proj[2][2] - proj[2][0] * proj[1][2]) * det);
    invProj[1][1] = ( (proj[0][0] * proj[2][2] - proj[2][0] * proj[0][2]) * det);
    invProj[1][2] = (-(proj[0][0] * proj[1][2] - proj[1][0] * proj[0][2]) * det);
    invProj[2][0] = ( (proj[1][0] * proj[2][1] - proj[2][0] * proj[1][1]) * det);
    invProj[2][1] = (-(proj[0][0] * proj[2][1] - proj[2][0] * proj[0][1]) * det);
    invProj[2][2] = ( (proj[0][0] * proj[1][1] - proj[1][0] * proj[0][1]) * det);

    // Do the translation part
    invProj[0][3] = -(proj[0][3] * invProj[0][0] +
        proj[1][3] * invProj[0][1] +
        proj[2][3] * invProj[0][2]);
    invProj[1][3] = -(proj[0][3] * invProj[1][0] +
        proj[1][3] * invProj[1][1] +
        proj[2][3] * invProj[1][2]);
    invProj[2][3] = -(proj[0][3] * invProj[2][0] +
        proj[1][3] * invProj[2][1] +
        proj[2][3] * invProj[2][2]);

    invProj[3][0] = 0.0f;
    invProj[3][1] = 0.0f;
    invProj[3][2] = 0.0f;
    invProj[3][3] = 1.0f;

    return invProj;
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/BuiltInDOTSInstancing.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif // UNITY_INPUTS_SHIM_INCLUDED
