#ifndef UNITY_PICKING_SPACE_TRANSFORMS_INCLUDED
#define UNITY_PICKING_SPACE_TRANSFORMS_INCLUDED

#ifdef SCENEPICKINGPASS

// The picking pass uses custom matrices defined directly from the c++
// So we have to redefine the space transform functions to overwrite the used matrices

#undef SHADEROPTIONS_CAMERA_RELATIVE_RENDERING

// Define the correct matrices
#undef unity_ObjectToWorld
#undef unity_MatrixVP
float4x4 unity_MatrixV;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;

#undef UNITY_MATRIX_M
#define UNITY_MATRIX_M unity_ObjectToWorld

#undef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M inverse(unity_ObjectToWorld)

#undef UNITY_MATRIX_V
#define UNITY_MATRIX_V unity_MatrixV

#undef UNITY_MATRIX_VP
#define UNITY_MATRIX_VP unity_MatrixVP

#undef UNITY_MATRIX_P
#define UNITY_MATRIX_P glstate_matrix_projection


// Overwrite the SpaceTransforms functions
#define GetObjectToWorldMatrix GetObjectToWorldMatrix_Picking
#define GetWorldToObjectMatrix GetWorldToObjectMatrix_Picking
#define GetWorldToViewMatrix GetWorldToViewMatrix_Picking
#define GetWorldToHClipMatrix GetWorldToHClipMatrix_Picking
#define GetViewToHClipMatrix GetViewToHClipMatrix_Picking
#define GetAbsolutePositionWS GetAbsolutePositionWS_Picking
#define GetCameraRelativePositionWS GetCameraRelativePositionWS_Picking
#define GetOddNegativeScale GetOddNegativeScale_Picking
#define TransformObjectToWorld TransformObjectToWorld_Picking
#define TransformWorldToObject TransformWorldToObject_Picking
#define TransformWorldToView TransformWorldToView_Picking
#define TransformObjectToHClip TransformObjectToHClip_Picking
#define TransformWorldToHClip TransformWorldToHClip_Picking
#define TransformWViewToHClip TransformWViewToHClip_Picking
#define TransformObjectToWorldDir TransformObjectToWorldDir_Picking
#define TransformWorldToObjectDir TransformWorldToObjectDir_Picking
#define TransformWorldToViewDir TransformWorldToViewDir_Picking
#define TransformWorldToHClipDir TransformWorldToHClipDir_Picking
#define TransformObjectToWorldNormal TransformObjectToWorldNormal_Picking
#define TransformWorldToObjectNormal TransformWorldToObjectNormal_Picking
#define CreateTangentToWorld CreateTangentToWorld_Picking
#define TransformTangentToWorld TransformTangentToWorld_Picking
#define TransformWorldToTangent TransformWorldToTangent_Picking
#define TransformTangentToObject TransformTangentToObject_Picking
#define TransformObjectToTangent TransformObjectToTangent_Picking


float4x4 inverse(float4x4 m) {
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

// Redefine the functions using the new macros
#undef UNITY_SPACE_TRANSFORMS_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"



#endif
#endif
