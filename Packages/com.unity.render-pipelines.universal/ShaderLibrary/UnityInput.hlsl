// UNITY_SHADER_NO_UPGRADE

#ifndef UNIVERSAL_SHADER_VARIABLES_INCLUDED
#define UNIVERSAL_SHADER_VARIABLES_INCLUDED
// Unity Engine built-in shader input variables.
// URP package specific shader input variables are defined in .universal/ShaderLibrary/Input.hlsl

#if defined(STEREO_INSTANCING_ON) && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && !defined(UNITY_COMPILER_DXC)))
#define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(STEREO_MULTIVIEW_ON) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN)) && !(defined(SHADER_API_SWITCH))
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif

#if defined(USING_STEREO_MATRICES)
// Current pass transforms.
#define glstate_matrix_projection     unity_StereoMatrixP[unity_StereoEyeIndex] // goes through GL.GetGPUProjectionMatrix()
#define unity_MatrixV                 unity_StereoMatrixV[unity_StereoEyeIndex]
#define unity_MatrixInvV              unity_StereoMatrixInvV[unity_StereoEyeIndex]
#define unity_MatrixInvP              unity_StereoMatrixInvP[unity_StereoEyeIndex]
#define unity_MatrixVP                unity_StereoMatrixVP[unity_StereoEyeIndex]
#define unity_MatrixInvVP             unity_StereoMatrixInvVP[unity_StereoEyeIndex]

// Camera transform (but the same as pass transform for XR).
#define unity_CameraProjection        unity_StereoCameraProjection[unity_StereoEyeIndex] // Does not go through GL.GetGPUProjectionMatrix()
#define unity_CameraInvProjection     unity_StereoCameraInvProjection[unity_StereoEyeIndex]
#define unity_WorldToCamera           unity_StereoMatrixV[unity_StereoEyeIndex] // Should be unity_StereoWorldToCamera but no use-case in XR pass
#define unity_CameraToWorld           unity_StereoMatrixInvV[unity_StereoEyeIndex] // Should be unity_StereoCameraToWorld but no use-case in XR pass
#define _WorldSpaceCameraPos          unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#endif

#define UNITY_LIGHTMODEL_AMBIENT (glstate_lightmodel_ambient * 2)

// ----------------------------------------------------------------------------

// Time values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
float4 _TimeParameters; // t, sin(t), cos(t)
float4 _LastTimeParameters; // t, sin(t), cos(t)

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

// scaleBias.x = flipSign
// scaleBias.y = scale
// scaleBias.z = bias
// scaleBias.w = unused
uniform float4 _ScaleBias;
uniform float4 _ScaleBiasRt;

// { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame
uniform float4 _RTHandleScale;

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

#ifndef DOTS_INSTANCING_ON // UnityPerDraw cbuffer doesn't exist with hybrid renderer

// Block Layout should be respected due to SRP Batcher
CBUFFER_START(UnityPerDraw)
// Space block Feature
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
real4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

// Render Layer block feature
// Only the first channel (x) contains valid data and the float must be reinterpreted using asuint() to extract the original 32 bits values.
float4 unity_RenderingLayer;

// Light Indices block feature
// These are set internally by the engine upon request by RendererConfiguration.
half4 unity_LightData;
half4 unity_LightIndices[2];

float4 unity_ProbesOcclusion;

// Reflection Probe 0 block feature
// HDR environment map decode instructions
real4 unity_SpecCube0_HDR;
real4 unity_SpecCube1_HDR;

float4 unity_SpecCube0_BoxMax;          // w contains the blend distance
float4 unity_SpecCube0_BoxMin;          // w contains the lerp value
float4 unity_SpecCube0_ProbePosition;   // w is set to 1 for box projection
float4 unity_SpecCube1_BoxMax;          // w contains the blend distance
float4 unity_SpecCube1_BoxMin;          // w contains the sign of (SpecCube0.importance - SpecCube1.importance)
float4 unity_SpecCube1_ProbePosition;   // w is set to 1 for box projection

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

// Renderer bounding box.
float4 unity_RendererBounds_Min;
float4 unity_RendererBounds_Max;

// Velocity
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;
//X : Use last frame positions (right now skinned meshes are the only objects that use this
//Y : Force No Motion
//Z : Z bias value
//W : Camera only
float4 unity_MotionVectorsParams;

// Sprite.
float4 unity_SpriteColor;
//X : FlipX
//Y : FlipY
//Z : Reserved for future use.
//W : Reserved for future use.
float4 unity_SpriteProps;
CBUFFER_END

#endif // UNITY_DOTS_INSTANCING_ENABLED

#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityStereoViewBuffer)
float4x4 unity_StereoMatrixP[2];
float4x4 unity_StereoMatrixInvP[2];
float4x4 unity_StereoMatrixV[2];
float4x4 unity_StereoMatrixInvV[2];
float4x4 unity_StereoMatrixVP[2];
float4x4 unity_StereoMatrixInvVP[2];

float4x4 unity_StereoCameraProjection[2];
float4x4 unity_StereoCameraInvProjection[2];

float3   unity_StereoWorldSpaceCameraPos[2];
CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
// OVR_multiview
// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
#if !defined(UNITY_DECLARE_MULTIVIEW)
#define UNITY_DECLARE_MULTIVIEW(number_of_views) CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; CBUFFER_END
#define UNITY_VIEWID gl_ViewID
#endif
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
#define unity_StereoEyeIndex UNITY_VIEWID
UNITY_DECLARE_MULTIVIEW(2);
#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
static uint unity_StereoEyeIndex;
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
float4x4 unity_MatrixInvP;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixInvVP;
float4 unity_StereoScaleOffset;
int unity_StereoEyeIndex;
#endif

real4 unity_ShadowColor;

// ----------------------------------------------------------------------------

// Unity specific
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);
TEXTURECUBE(unity_SpecCube1);
SAMPLER(samplerunity_SpecCube1);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D_ARRAY(unity_Lightmaps);
SAMPLER(samplerunity_Lightmaps);

// Dynamic lightmap
TEXTURE2D(unity_DynamicLightmap);
SAMPLER(samplerunity_DynamicLightmap);
// TODO ENLIGHTEN: Instanced GI

// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);
TEXTURE2D_ARRAY(unity_LightmapsInd);
TEXTURE2D(unity_DynamicDirectionality);
// TODO ENLIGHTEN: Instanced GI
// TEXTURE2D_ARRAY(unity_DynamicDirectionality);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
TEXTURE2D_ARRAY(unity_ShadowMasks);
SAMPLER(samplerunity_ShadowMasks);

// Mipmap Streaming Debug
TEXTURE2D(unity_MipmapStreaming_DebugTex);

// ----------------------------------------------------------------------------

// TODO: all affine matrices should be 3x4.
// TODO: sort these vars by the frequency of use (descending), and put commonly used vars together.
// Note: please use UNITY_MATRIX_X macros instead of referencing matrix variables directly.
#if defined(USING_STEREO_MATRICES)
float4x4 _PrevViewProjMatrixStereo[2];
float4x4 _NonJitteredViewProjMatrixStereo[2];
float4x4 _ViewProjMatrixStereo[2];
#define  _PrevViewProjMatrix  _PrevViewProjMatrixStereo[unity_StereoEyeIndex]
#define  _NonJitteredViewProjMatrix _NonJitteredViewProjMatrixStereo[unity_StereoEyeIndex]
#define  _ViewProjMatrix      _ViewProjMatrixStereo[unity_StereoEyeIndex]
#else
float4x4 _PrevViewProjMatrix;         // non-jittered. Motion vectors.
float4x4 _NonJitteredViewProjMatrix;  // non-jittered.
float4x4 _ViewProjMatrix; // TODO: URP currently uses unity_MatrixVP, see Input.hlsl
#endif
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
