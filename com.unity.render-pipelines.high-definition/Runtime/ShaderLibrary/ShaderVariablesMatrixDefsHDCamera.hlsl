#ifdef UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
    #error Mixing HDCamera and legacy Unity matrix definitions
#endif

#ifndef UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
#define UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED

#if defined(USING_STEREO_MATRICES)

#define UNITY_MATRIX_V              _XRViewConstants[unity_StereoEyeIndex].viewMatrix
#define UNITY_MATRIX_I_V            _XRViewConstants[unity_StereoEyeIndex].invViewMatrix
#define UNITY_MATRIX_P              OptimizeProjectionMatrix(_XRViewConstants[unity_StereoEyeIndex].projMatrix)
#define UNITY_MATRIX_I_P            _XRViewConstants[unity_StereoEyeIndex].invProjMatrix
#define UNITY_MATRIX_VP             _XRViewConstants[unity_StereoEyeIndex].viewProjMatrix
#define UNITY_MATRIX_I_VP           _XRViewConstants[unity_StereoEyeIndex].invViewProjMatrix
#define UNITY_MATRIX_UNJITTERED_VP  _XRViewConstants[unity_StereoEyeIndex].nonJitteredViewProjMatrix
#define UNITY_MATRIX_PREV_VP        _XRViewConstants[unity_StereoEyeIndex].prevViewProjMatrix

#else

#define UNITY_MATRIX_V     _ViewMatrix
#define UNITY_MATRIX_I_V   _InvViewMatrix
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(_ProjMatrix)
#define UNITY_MATRIX_I_P   _InvProjMatrix
#define UNITY_MATRIX_VP    _ViewProjMatrix
#define UNITY_MATRIX_I_VP  _InvViewProjMatrix
#define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
#define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix

#endif // USING_STEREO_MATRICES

#endif // UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
