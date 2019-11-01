#ifdef UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
#error Mixing Universal and legacy Unity matrix definitions
#endif

#ifndef UNITY_SHADER_VARIABLES_MATRIX_DEFS_URP_INCLUDED
#define UNITY_SHADER_VARIABLES_MATRIX_DEFS_URP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesURPBuildinVarsDecl.hlsl"

// Block Layout should be respected due to SRP Batcher
CBUFFER_START(UnityPerDraw)
// Space block feature
float4x4 BUILDIN_unity_ObjectToWorld;
float4x4 BUILDIN_unity_WorldToObject;
float4   BUILDIN_unity_LODFade;
real4    BUILDIN_unity_WorldTransformParams;
// LightMap block feature
// These are set internally by the engine upon request by RendererConfiguration.
float4   BUILDIN_unity_LightmapST;
float4   BUILDIN_unity_DynamicLightmapST;
// SH block feature
real4    BUILDIN_unity_SHAr;
real4    BUILDIN_unity_SHAg;
real4    BUILDIN_unity_SHAb;
real4    BUILDIN_unity_SHBr;
real4    BUILDIN_unity_SHBg;
real4    BUILDIN_unity_SHBb;
real4    BUILDIN_unity_SHC;
// Probe Occlusion block feature
float4   BUILDIN_unity_ProbesOcclusion;
// Light Indices block feature
real4    BUILDIN_unity_LightData;
real4    BUILDIN_unity_LightIndices[2];
// Reflection Probe 0 block feature
// HDR environment map decode instructions
real4    BUILDIN_unity_SpecCube0_HDR;
CBUFFER_END

CBUFFER_START(UnityPerCamera)
float4x4 _InvCameraViewProj;
float4 _ScaledScreenParams;
// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
float4 _ScreenParams;
float3 _WorldSpaceCameraPos;
float4 _ProjectionParams;
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
CBUFFER_END

CBUFFER_START(UnityPerFrame)
half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;
// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 BUILDIN_unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
float4 _TimeParameters; // t, sin(t), cos(t)
float4 BUILDIN_unity_FogParams;
CBUFFER_END

CBUFFER_START(UnityGlobal)
float4 _MainLightPosition;
half4 _MainLightColor;

half4 _AdditionalLightsCount;
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
#endif
real4 BUILDIN_unity_FogColor;
CBUFFER_END

// Descriptors
TEXTURE2D(BUILDIN_unity_Lightmap);
SAMPLER(BUILDIN_samplerunity_Lightmap);
TEXTURECUBE(BUILDIN_unity_SpecCube0);
SAMPLER(BUILDIN_samplerunity_SpecCube0);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesURPBuildinVarsImpl.hlsl"

#if defined(USING_PUREXRSDK_STEREO_MATRICES)
#error TODO: Pure XR - add SPI matrices
//#define UNITY_MATRIX_V              _XRViewConstants[unity_StereoEyeIndex].viewMatrix
//#define UNITY_MATRIX_I_V            _XRViewConstants[unity_StereoEyeIndex].invViewMatrix
//#define UNITY_MATRIX_P              OptimizeProjectionMatrix(_XRViewConstants[unity_StereoEyeIndex].projMatrix)
//#define UNITY_MATRIX_I_P            _XRViewConstants[unity_StereoEyeIndex].invProjMatrix
//#define UNITY_MATRIX_VP             _XRViewConstants[unity_StereoEyeIndex].viewProjMatrix
//#define UNITY_MATRIX_I_VP           _XRViewConstants[unity_StereoEyeIndex].invViewProjMatrix
//#define UNITY_MATRIX_UNJITTERED_VP  _XRViewConstants[unity_StereoEyeIndex].nonJitteredViewProjMatrix
//#define UNITY_MATRIX_PREV_VP        _XRViewConstants[unity_StereoEyeIndex].prevViewProjMatrix
//#define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
//#define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
//#define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
//#define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
//#define _WorldSpaceCameraPos unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#else

//// Note: please use UNITY_MATRIX_X macros instead of referencing matrix variables directly.
float4x4 _ViewProjMatrix;
float4x4 _ViewMatrix;
float4x4 _ProjMatrix;
float4x4 _InvViewProjMatrix;
float4x4 _InvViewMatrix;
float4x4 _InvProjMatrix;
float4x4 BUILDIN_unity_CameraInvProjection;
float4x4 BUILDIN_unity_CameraToWorld;
int      BUILDIN_unity_StereoEyeIndex;

#define UNITY_MATRIX_M     GetRawUnityObjectToWorld()
#define UNITY_MATRIX_I_M   GetRawUnityWorldToObject()
#define UNITY_MATRIX_V     _ViewMatrix
#define UNITY_MATRIX_I_V   _InvViewMatrix
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(_ProjMatrix)
#define UNITY_MATRIX_I_P   _InvProjMatrix
#define UNITY_MATRIX_VP    _ViewProjMatrix
#define UNITY_MATRIX_I_VP  _InvViewProjMatrix
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#endif // USING_STEREO_MATRICES

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

#endif // UNITY_SHADER_VARIABLES_MATRIX_DEFS_URP_INCLUDED
