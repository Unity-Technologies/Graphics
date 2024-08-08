#ifndef UNITY_PICKING_SPACE_TRANSFORMS_INCLUDED
#define UNITY_PICKING_SPACE_TRANSFORMS_INCLUDED

#if defined(SCENEPICKINGPASS) || defined(SCENESELECTIONPASS)

// The picking pass uses custom matrices defined directly from the c++
// So we have to redefine the space transform functions to overwrite the used matrices
// For the selection pass, we want to use the non jittered projection matrix to avoid object outline flickering

#undef SHADEROPTIONS_CAMERA_RELATIVE_RENDERING

// Define the correct matrices
#if defined(DOTS_INSTANCING_ON)

#undef unity_ObjectToWorld
#undef unity_MatrixPreviousM

#undef UNITY_MATRIX_M
#define UNITY_MATRIX_M UNITY_DOTS_MATRIX_M

#undef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M Inverse(UNITY_DOTS_MATRIX_M)

#undef UNITY_PREV_MATRIX_M
#define UNITY_PREV_MATRIX_M UNITY_DOTS_PREV_MATRIX_M

#undef UNITY_PREV_MATRIX_I_M
#define UNITY_PREV_MATRIX_I_M Inverse(UNITY_DOTS_PREV_MATRIX_M)

#elif !defined(HAVE_VFX_MODIFICATION)

#undef unity_ObjectToWorld
#undef unity_MatrixPreviousM

#undef UNITY_MATRIX_M
#define UNITY_MATRIX_M unity_ObjectToWorld

#undef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M Inverse(unity_ObjectToWorld)

#undef UNITY_PREV_MATRIX_M
#define UNITY_PREV_MATRIX_M unity_MatrixPreviousM

#undef UNITY_PREV_MATRIX_I_M
#define UNITY_PREV_MATRIX_I_M Inverse(unity_MatrixPreviousM)

#endif

#undef unity_MatrixVP
float4x4 unity_MatrixV;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;

#undef UNITY_MATRIX_V
#define UNITY_MATRIX_V unity_MatrixV

#undef UNITY_MATRIX_VP
#define UNITY_MATRIX_VP unity_MatrixVP

#undef UNITY_MATRIX_P
#define UNITY_MATRIX_P glstate_matrix_projection

// Overwrite the SpaceTransforms functions
#define GetObjectToWorldMatrix GetObjectToWorldMatrix_Picking
#define GetWorldToObjectMatrix GetWorldToObjectMatrix_Picking
#define GetPrevObjectToWorldMatrix GetPrevObjectToWorldMatrix_Picking
#define GetPrevWorldToObjectMatrix GetPrevWorldToObjectMatrix_Picking
#define GetWorldToViewMatrix GetWorldToViewMatrix_Picking
#define GetViewToWorldMatrix GetViewToWorldMatrix_Picking
#define GetWorldToHClipMatrix GetWorldToHClipMatrix_Picking
#define GetViewToHClipMatrix GetViewToHClipMatrix_Picking
#define GetAbsolutePositionWS GetAbsolutePositionWS_Picking
#define GetCameraRelativePositionWS GetCameraRelativePositionWS_Picking
#define GetOddNegativeScale GetOddNegativeScale_Picking
#define TransformObjectToWorld TransformObjectToWorld_Picking
#define TransformWorldToObject TransformWorldToObject_Picking
#define TransformWorldToView TransformWorldToView_Picking
#define TransformViewToWorld TransformViewToWorld_Picking
#define TransformObjectToHClip TransformObjectToHClip_Picking
#define TransformWorldToHClip TransformWorldToHClip_Picking
#define TransformWViewToHClip TransformWViewToHClip_Picking
#define TransformObjectToWorldDir TransformObjectToWorldDir_Picking
#define TransformWorldToObjectDir TransformWorldToObjectDir_Picking
#define TransformWorldToViewDir TransformWorldToViewDir_Picking
#define TransformViewToWorldDir TransformViewToWorldDir_Picking
#define TransformWorldToHClipDir TransformWorldToHClipDir_Picking
#define TransformObjectToWorldNormal TransformObjectToWorldNormal_Picking
#define TransformWorldToObjectNormal TransformWorldToObjectNormal_Picking
#define CreateTangentToWorld CreateTangentToWorld_Picking
#define TransformTangentToWorld TransformTangentToWorld_Picking
#define TransformWorldToTangent TransformWorldToTangent_Picking
#define TransformTangentToObject TransformTangentToObject_Picking
#define TransformObjectToTangent TransformObjectToTangent_Picking
#define TransformWorldToViewNormal TransformWorldToViewNormal_Picking
#define TransformViewToWorldNormal TransformViewToWorldNormal_Picking
#define TransformWorldToTangentDir TransformWorldToTangentDir_Picking
#define TransformTangentToWorldDir TransformTangentToWorldDir_Picking

float4x4 ScenePickingGetCameraViewProjMatrix()
{
    float4x4 translationMatrix = {
            { 1.0 ,0.0 , 0.0, -_WorldSpaceCameraPos.x },
            { 0.0 ,1.0 , 0.0, -_WorldSpaceCameraPos.y },
            { 0.0 ,0.0 , 1.0, -_WorldSpaceCameraPos.z },
            { 0.0 ,0.0 , 0.0, 1.0} };

    return mul(_CameraViewProjMatrix, translationMatrix);
}

#define _CameraViewProjMatrix ScenePickingGetCameraViewProjMatrix()


// Redefine the functions using the new macros
#undef UNITY_SPACE_TRANSFORMS_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#if defined(HAVE_VFX_MODIFICATION)
#define VFX_HAS_PICKING_MATRIX_CORRECTION 1
#define GetSGVFXUnityObjectToWorld     GetSGVFXUnityObjectToWorld_Picking
#define GetSGVFXUnityWorldToObject     GetSGVFXUnityWorldToObject_Picking
float4x4 GetSGVFXUnityObjectToWorld()     { return RevertCameraTranslationFromMatrix(GetSGVFXUnityObjectToWorldBackup()); }
float4x4 GetSGVFXUnityWorldToObject()     { return RevertCameraTranslationFromInverseMatrix(GetSGVFXUnityWorldToObjectBackup()); }
#endif

#endif
#endif
