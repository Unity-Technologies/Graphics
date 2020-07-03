// UNITY_SHADER_NO_UPGRADE

#ifndef UNIVERSAL_SHADER_VARIABLES_INCLUDED
#define UNIVERSAL_SHADER_VARIABLES_INCLUDED

#if defined(STEREO_INSTANCING_ON) && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN))
#define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(STEREO_MULTIVIEW_ON) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN)) && !(defined(SHADER_API_SWITCH))
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif

#if defined(USING_STEREO_MATRICES)
#define glstate_matrix_projection unity_StereoMatrixP[unity_StereoEyeIndex]
#define unity_MatrixV unity_StereoMatrixV[unity_StereoEyeIndex]
#define unity_MatrixInvV unity_StereoMatrixInvV[unity_StereoEyeIndex]
#define unity_MatrixVP unity_StereoMatrixVP[unity_StereoEyeIndex]

#define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
#define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
#define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
#define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
#define _WorldSpaceCameraPos unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#endif

#define UNITY_LIGHTMODEL_AMBIENT (glstate_lightmodel_ambient * 2)

// ----------------------------------------------------------------------------

// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
float4 _TimeParameters; // t, sin(t), cos(t)

#if !defined(USING_STEREO_MATRICES)
float3 _WorldSpaceCameraPos;
#endif

// x = 1 or -1 (-1 if projection is flipped)
// y = near plane
// z = far plane
// w = 1/far plane
float4 _ProjectionParams;

// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
float4 _ScreenParams;

// Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
// x = 1-far/near
// y = far/near
// z = x/far
// w = y/far
// or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
// x = -1+far/near
// y = 1
// z = x/far
// w = 1/far
float4 _ZBufferParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;

float4 unity_CameraWorldClipPlanes[6];

#if !defined(USING_STEREO_MATRICES)
// Projection matrices of the camera. Note that this might be different from projection matrix
// that is set right now, e.g. while rendering shadows the matrices below are still the projection
// of original camera.
float4x4 unity_CameraProjection;
float4x4 unity_CameraInvProjection;
float4x4 unity_WorldToCamera;
float4x4 unity_CameraToWorld;
#endif

// ----------------------------------------------------------------------------

// Block Layout should be respected due to SRP Batcher
CBUFFER_START(UnityPerDraw)
// Space block Feature
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
real4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

// Light Indices block feature
// These are set internally by the engine upon request by RendererConfiguration.
real4 unity_LightData;
real4 unity_LightIndices[2];

float4 unity_ProbesOcclusion;

// Reflection Probe 0 block feature
// HDR environment map decode instructions
real4 unity_SpecCube0_HDR;

// Lightmap block feature
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

// SH block feature
real4 unity_SHAr;
real4 unity_SHAg;
real4 unity_SHAb;
real4 unity_SHBr;
real4 unity_SHBg;
real4 unity_SHBb;
real4 unity_SHC;
CBUFFER_END

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) || ((defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)) && (defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)))
    #define GLOBAL_CBUFFER_START(name)    cbuffer name {
    #define GLOBAL_CBUFFER_END            }
#else
    #define GLOBAL_CBUFFER_START(name)    CBUFFER_START(name)
    #define GLOBAL_CBUFFER_END            CBUFFER_END
#endif

#if defined(USING_STEREO_MATRICES)
GLOBAL_CBUFFER_START(UnityStereoGlobals)
float4x4 unity_StereoMatrixP[2];
float4x4 unity_StereoMatrixV[2];
float4x4 unity_StereoMatrixInvV[2];
float4x4 unity_StereoMatrixVP[2];

float4x4 unity_StereoCameraProjection[2];
float4x4 unity_StereoCameraInvProjection[2];
float4x4 unity_StereoWorldToCamera[2];
float4x4 unity_StereoCameraToWorld[2];

float3 unity_StereoWorldSpaceCameraPos[2];
float4 unity_StereoScaleOffset[2];
GLOBAL_CBUFFER_END
#endif

#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_MULTIVIEW_ENABLED)
GLOBAL_CBUFFER_START(UnityStereoEyeIndices)
    float4 unity_StereoEyeIndices[2];
GLOBAL_CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
// OVR_multiview
// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
#if !defined(UNITY_DECLARE_MULTIVIEW)
#define UNITY_DECLARE_MULTIVIEW(number_of_views) GLOBAL_CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; GLOBAL_CBUFFER_END
#define UNITY_VIEWID gl_ViewID
#endif
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
#define unity_StereoEyeIndex UNITY_VIEWID
UNITY_DECLARE_MULTIVIEW(2);
#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
static uint unity_StereoEyeIndex;
#elif defined(UNITY_SINGLE_PASS_STEREO)
GLOBAL_CBUFFER_START(UnityStereoEyeIndex)
int unity_StereoEyeIndex;
GLOBAL_CBUFFER_END
#endif

float4x4 glstate_matrix_transpose_modelview0;

// ----------------------------------------------------------------------------

real4 glstate_lightmodel_ambient;
real4 unity_AmbientSky;
real4 unity_AmbientEquator;
real4 unity_AmbientGround;
real4 unity_IndirectSpecColor;
float4 unity_FogParams;
real4  unity_FogColor;

#if !defined(USING_STEREO_MATRICES)
float4x4 glstate_matrix_projection;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixVP;
float4 unity_StereoScaleOffset;
int unity_StereoEyeIndex;
#endif

real4 unity_ShadowColor;

// ----------------------------------------------------------------------------

// Unity specific
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

// We can have shadowMask only if we have lightmap, so no sampler
TEXTURE2D(unity_ShadowMask);

// ----------------------------------------------------------------------------

// TODO: all affine matrices should be 3x4.
// TODO: sort these vars by the frequency of use (descending), and put commonly used vars together.
// Note: please use UNITY_MATRIX_X macros instead of referencing matrix variables directly.
float4x4 _PrevViewProjMatrix;
float4x4 _ViewProjMatrix;
float4x4 _NonJitteredViewProjMatrix;
float4x4 _ViewMatrix;
float4x4 _ProjMatrix;
float4x4 _InvViewProjMatrix;
float4x4 _InvViewMatrix;
float4x4 _InvProjMatrix;
float4   _InvProjParam;
float4   _ScreenSize;       // {w, h, 1/w, 1/h}
float4   _FrustumPlanes[6]; // {(a, b, c) = N, d = -dot(N, P)} [L, R, T, B, N, F]

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

#endif // UNIVERSAL_SHADER_VARIABLES_INCLUDED
