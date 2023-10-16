#ifndef EDITOR_SHADER_VARIABLES
#define EDITOR_SHADER_VARIABLES

// ================================
//     PER FRAME CONSTANTS
// ================================
#if defined(USING_STEREO_MATRICES)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    #define glstate_matrix_projection unity_StereoMatrixP[unity_StereoEyeIndex]
    #define unity_MatrixV unity_StereoMatrixV[unity_StereoEyeIndex]
    #define unity_MatrixInvV unity_StereoMatrixInvV[unity_StereoEyeIndex]
    #define unity_MatrixVP unity_StereoMatrixVP[unity_StereoEyeIndex]

    #define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
    #define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
    #define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
    #define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
#else
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4 unity_StereoScaleOffset;
#endif

#endif // EDITOR_SHADER_VARIABLES
