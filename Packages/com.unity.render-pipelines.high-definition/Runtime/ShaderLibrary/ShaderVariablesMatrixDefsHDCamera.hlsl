#ifdef UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
    #error Mixing HDCamera and legacy Unity matrix definitions
#endif

#ifndef UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
#define UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED

#if defined(USING_STEREO_MATRICES)

#define UNITY_MATRIX_V              _XRViewMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_I_V            _XRInvViewMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_P              OptimizeProjectionMatrix(_XRProjMatrix[unity_StereoEyeIndex])
#define UNITY_MATRIX_I_P            _XRInvProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_VP             _XRViewProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_I_VP           _XRInvViewProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_UNJITTERED_VP  _XRNonJitteredViewProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_UNJITTERED_I_VP  _XRNonJitteredInvViewProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_PREV_VP        _XRPrevViewProjMatrix[unity_StereoEyeIndex]
#define UNITY_MATRIX_PREV_I_VP      _XRPrevInvViewProjMatrix[unity_StereoEyeIndex]

#else

#define UNITY_MATRIX_V     _ViewMatrix
#define UNITY_MATRIX_I_V   _InvViewMatrix
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(_ProjMatrix)
#define UNITY_MATRIX_I_P   _InvProjMatrix
#define UNITY_MATRIX_VP    _ViewProjMatrix
#define UNITY_MATRIX_I_VP  _InvViewProjMatrix
#define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
#define UNITY_MATRIX_UNJITTERED_I_VP _NonJitteredInvViewProjMatrix
#define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix
#define UNITY_MATRIX_PREV_I_VP _PrevInvViewProjMatrix

#endif // USING_STEREO_MATRICES

// Matrix overrides for water system
#if defined(WATER_SURFACE_GBUFFER) || defined(WATER_ONE_BAND) || defined(WATER_TWO_BANDS) || defined(WATER_THREE_BANDS)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

#define _WaterCustomMeshTransform GetRawUnityObjectToWorld()
#define _WaterCustomMeshTransform_Inverse GetRawUnityWorldToObject()

#undef UNITY_MATRIX_M
#define UNITY_MATRIX_M ApplyCameraTranslationToMatrix(_WaterSurfaceTransform)

#undef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M ApplyCameraTranslationToInverseMatrix(_WaterSurfaceTransform_Inverse)

#endif

#endif // UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
