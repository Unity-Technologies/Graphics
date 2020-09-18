// UNITY_SHADER_NO_UPGRADE

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
#define UNITY_SHADER_VARIABLES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"

// CAUTION:
// Currently the shaders compiler always include regualr Unity shaderVariables, so I get a conflict here were UNITY_SHADER_VARIABLES_INCLUDED is already define, this need to be fixed.
// As I haven't change the variables name yet, I simply don't define anything, and I put the transform function at the end of the file outside the guard header.
// This need to be fixed.

#if defined(USING_STEREO_MATRICES)
    #define _WorldSpaceCameraPos            _XRWorldSpaceCameraPos[unity_StereoEyeIndex].xyz
    #define _WorldSpaceCameraPosViewOffset  _XRWorldSpaceCameraPosViewOffset[unity_StereoEyeIndex].xyz
    #define _PrevCamPosRWS                  _XRPrevWorldSpaceCameraPos[unity_StereoEyeIndex].xyz
#endif

#define UNITY_LIGHTMODEL_AMBIENT (glstate_lightmodel_ambient * 2)

// This only defines the ray tracing macro on the platforms that support ray tracing this should be dx12
#if (SHADEROPTIONS_RAYTRACING && (defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12)) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_PSSL))
#define RAYTRACING_ENABLED (1)
#define DirectionalShadowType float3
#else
#define RAYTRACING_ENABLED (0)
#define DirectionalShadowType float
#endif

#if defined(SHADER_STAGE_RAY_TRACING)
// FXC Supports the naïve "recursive" concatenation, while DXC and C do not https://github.com/pfultz2/Cloak/wiki/C-Preprocessor-tricks,-tips,-and-idioms
// However, FXC does not support the proper pattern (the one bellow), so we only override it in the case of ray tracing subshaders for the moment.
// Note that this should be used for all shaders when DX12 used DXC for vert/frag shaders (which it does not for the moment)
#undef MERGE_NAME
#define MERGE_NAME_CONCAT(Name, ...) Name ## __VA_ARGS__
#define MERGE_NAME(X, Y) MERGE_NAME_CONCAT(X, Y)

#define RAY_TRACING_OPTIONAL_PARAMETERS , IntersectionVertex intersectionVertex, RayCone rayCone, out bool alphaTestResult
#define GENERIC_ALPHA_TEST(alphaValue, alphaCutoffValue) DoAlphaTest(alphaValue, alphaCutoffValue, alphaTestResult); if (!alphaTestResult) { return; }
#define RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS alphaTestResult = true;
#else
#define RAY_TRACING_OPTIONAL_PARAMETERS
#define GENERIC_ALPHA_TEST(alphaValue, alphaCutoffValue) DoAlphaTest(alphaValue, alphaCutoffValue);
#define RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
#endif



// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw)

    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms
    float4 unity_RenderingLayer;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // SH lighting environment
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    // x = Disabled(0)/Enabled(1)
    // y = Computation are done in global space(0) or local space(1)
    // z = Texel size on U texture coordinate
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv; // Note: This variable is float4 and not float3 (compare to builtin unity) to be compatible with SRP batcher
    float4 unity_ProbeVolumeMin; // Note: This variable is float4 and not float3 (compare to builtin unity) to be compatible with SRP batcher

    // This contain occlusion factor from 0 to 1 for dynamic objects (no SH here)
    float4 unity_ProbesOcclusion;

    // Velocity
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    //X : Use last frame positions (right now skinned meshes are the only objects that use this
    //Y : Force No Motion
    //Z : Z bias value
    //W : Camera only
    float4 unity_MotionVectorsParams;

CBUFFER_END

CBUFFER_START(UnityPerDrawRare)
    float4x4 glstate_matrix_transpose_modelview0;
CBUFFER_END

// ----------------------------------------------------------------------------

// These are the samplers available in the HDRenderPipeline.
// Avoid declaring extra samplers as they are 4x SGPR each on GCN.
SAMPLER(s_point_clamp_sampler);
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_linear_repeat_sampler);
SAMPLER(s_trilinear_clamp_sampler);
SAMPLER(s_trilinear_repeat_sampler);
SAMPLER_CMP(s_linear_clamp_compare_sampler);

// ----------------------------------------------------------------------------

TEXTURE2D_X(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

// Color pyramid (width, height, lodcount, Unused)
TEXTURE2D_X(_ColorPyramidTexture);

// Custom pass buffer
TEXTURE2D_X(_CustomDepthTexture);
TEXTURE2D_X(_CustomColorTexture);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

// Dynamic GI lightmap
TEXTURE2D(unity_DynamicLightmap);
SAMPLER(samplerunity_DynamicLightmap);

TEXTURE2D(unity_DynamicDirectionality);

// We can have shadowMask only if we have lightmap, so no sampler
TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

// TODO: Change code here so probe volume use only one transform instead of all this parameters!
TEXTURE3D(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

// Exposure texture - 1x1 RG16F (r: exposure mult, g: exposure EV100)
TEXTURE2D(_ExposureTexture);
TEXTURE2D(_PrevExposureTexture);

// ----------------------------------------------------------------------------

// Define that before including all the sub systems ShaderVariablesXXX.hlsl files in order to include constant buffer properties.
#define SHADER_VARIABLES_INCLUDE_CB

// Important: please use macros or functions to access the CBuffer data.
// The member names and data layout can (and will) change!
CBUFFER_START(UnityGlobal)
    // ================================
    //     PER VIEW CONSTANTS
    // ================================
    // TODO: all affine matrices should be 3x4.
    float4x4 _ViewMatrix;
    float4x4 _InvViewMatrix;
    float4x4 _ProjMatrix;
    float4x4 _InvProjMatrix;
    float4x4 _ViewProjMatrix;
    float4x4 _CameraViewProjMatrix;
    float4x4 _InvViewProjMatrix;
    float4x4 _NonJitteredViewProjMatrix;
    float4x4 _PrevViewProjMatrix;       // non-jittered
    float4x4 _PrevInvViewProjMatrix;       // non-jittered

    // TODO: put commonly used vars together (below), and then sort them by the frequency of use (descending).
    // Note: a matrix is 4 * 4 * 4 = 64 bytes (1x cache line), so no need to sort those.
#ifndef USING_STEREO_MATRICES
    float3 _WorldSpaceCameraPos;
    float  _Pad0;
    float3 _PrevCamPosRWS;
    float  _Pad1;
#endif
    float4 _ScreenSize;                 // { w, h, 1 / w, 1 / h }

    // Those two uniforms are specific to the RTHandle system
    float4 _RTHandleScale;        // { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame
    float4 _RTHandleScaleHistory; // Same as above but the RTHandle handle size is that of the history buffer

    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1 - f/n
    // y = f/n
    // z = 1/f - 1/n
    // w = 1/n
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1 + f/n
    // y = 1
    // z = -1/n + -1/f
    // w = 1/f
    float4 _ZBufferParams;

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1/far plane
    float4 _ProjectionParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;

    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    float4 _FrustumPlanes[6];           // { (a, b, c) = N, d = -dot(N, P) } [L, R, T, B, N, F]
    float4 _ShadowFrustumPlanes[6];     // { (a, b, c) = N, d = -dot(N, P) } [L, R, T, B, N, F]

    // TAA Frame Index ranges from 0 to 7.
    float4 _TaaFrameInfo;               // { taaSharpenStrength, unused, taaFrameIndex, taaEnabled ? 1 : 0 }

    // Current jitter strength (0 if TAA is disabled)
    float4 _TaaJitterStrength;          // { x, y, x/width, y/height }

    // t = animateMaterials ? Time.realtimeSinceStartup : 0.
    // We keep all those time value for compatibility with legacy unity but prefer _TimeParameters instead.
    float4 _Time;                       // { t/20, t, t*2, t*3 }
    float4 _SinTime;                    // { sin(t/8), sin(t/4), sin(t/2), sin(t) }
    float4 _CosTime;                    // { cos(t/8), cos(t/4), cos(t/2), cos(t) }
    float4 unity_DeltaTime;             // { dt, 1/dt, smoothdt, 1/smoothdt }
    float4 _TimeParameters;             // { t, sin(t), cos(t) }
    float4 _LastTimeParameters;         // { t, sin(t), cos(t) }

    // Volumetric lighting.
    float4 _AmbientProbeCoeffs[7];      // 3 bands of SH, packed, rescaled and convolved with the phase function

    float3 _HeightFogBaseScattering;
    float  _HeightFogBaseExtinction;

    float2 _HeightFogExponents;         // { 1/H, H }
    float  _HeightFogBaseHeight;
    float  _GlobalFogAnisotropy;

    float4 _VBufferViewportSize;           // { w, h, 1/w, 1/h }
    uint   _VBufferSliceCount;
    float  _VBufferRcpSliceCount;
    float  _VBufferRcpInstancedViewCount;  // Used to remap VBuffer coordinates for XR

    float  _ContactShadowOpacity;
    float4 _VBufferSharedUvScaleAndLimit;  // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system

    float4 _VBufferDistanceEncodingParams; // See the call site for description
    float4 _VBufferDistanceDecodingParams; // See the call site for description

    // TODO: these are only used for reprojection.
    // Once reprojection is performed in a separate pass, we should probably
    // move these to a dedicated CBuffer to avoid polluting the global one.
    float4 _VBufferPrevViewportSize;
    float4 _VBufferHistoryPrevUvScaleAndLimit;
    float4 _VBufferPrevDepthEncodingParams;
    float4 _VBufferPrevDepthDecodingParams;

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/ShaderVariablesLightLoop.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ShaderVariablesScreenSpaceLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/ShaderVariablesAtmosphericScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/ShaderVariablesSubsurfaceScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderVariablesDecal.hlsl"

    #define DEFAULT_LIGHT_LAYERS 0xFF
    uint _EnableLightLayers;
    float _ReplaceDiffuseForIndirect;
    uint _EnableSkyReflection;

    uint _EnableSSRefraction;

    uint _OffScreenRendering;
    uint _OffScreenDownsampleFactor;

    uint _XRViewCount;
    int  _FrameCount;

    float _ProbeExposureScale;
    int  _UseRayTracedReflections;
    int  _RaytracingFrameIndex;

    float4 _CoarseStencilBufferSize;

    int _TransparentCameraOnlyMotionVectors;
    float3 _Pad;

CBUFFER_END

// Custom generated by HDRP, not from Unity Engine (passed in via HDCamera)
#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityXRViewConstants)
    float4x4 _XRViewMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRInvViewMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRInvProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRViewProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRInvViewProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRNonJitteredViewProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRPrevViewProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRPrevInvViewProjMatrix[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRPrevViewProjMatrixNoCameraTrans[SHADEROPTIONS_XR_MAX_VIEWS];
    float4x4 _XRPixelCoordToViewDirWS[SHADEROPTIONS_XR_MAX_VIEWS];

    float4 _XRWorldSpaceCameraPos[SHADEROPTIONS_XR_MAX_VIEWS];
    float4 _XRWorldSpaceCameraPosViewOffset[SHADEROPTIONS_XR_MAX_VIEWS];
    float4 _XRPrevWorldSpaceCameraPos[SHADEROPTIONS_XR_MAX_VIEWS];
CBUFFER_END
#endif

// Note: To sample camera depth in HDRP we provide these utils functions because the way we store the depth mips can change
// Currently it's an atlas and it's layout can be found at ComputePackedMipChainInfo in HDUtils.cs
float LoadCameraDepth(uint2 pixelCoords)
{
    return LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, pixelCoords, 0).r;
}

float SampleCameraDepth(float2 uv)
{
    return LoadCameraDepth(uint2(uv * _ScreenSize.xy));
}

float3 LoadCameraColor(uint2 pixelCoords, uint lod)
{
    return LOAD_TEXTURE2D_X_LOD(_ColorPyramidTexture, pixelCoords, lod).rgb;
}

float3 SampleCameraColor(float2 uv, float lod)
{
    return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScaleHistory.xy, lod).rgb;
}

float3 LoadCameraColor(uint2 pixelCoords)
{
    return LoadCameraColor(pixelCoords, 0);
}

float3 SampleCameraColor(float2 uv)
{
    return SampleCameraColor(uv, 0);
}

float4 SampleCustomColor(float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CustomColorTexture, s_trilinear_clamp_sampler, uv * _RTHandleScale.xy, 0);
}

float4 LoadCustomColor(uint2 pixelCoords)
{
    return LOAD_TEXTURE2D_X_LOD(_CustomColorTexture, pixelCoords, 0);
}

float LoadCustomDepth(uint2 pixelCoords)
{
    return LOAD_TEXTURE2D_X_LOD(_CustomDepthTexture, pixelCoords, 0).r;
}

float SampleCustomDepth(float2 uv)
{
    return LoadCustomDepth(uint2(uv * _ScreenSize.xy));
}

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

// Helper to handle camera relative space

float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    // To handle camera relative rendering we substract the camera position in the model matrix
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    modelMatrix._m03_m13_m23 -= _WorldSpaceCameraPos;
#endif
    return modelMatrix;
}

float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    // To handle camera relative rendering we need to apply translation before converting to object space
    float4x4 translationMatrix = { { 1.0, 0.0, 0.0, _WorldSpaceCameraPos.x },{ 0.0, 1.0, 0.0, _WorldSpaceCameraPos.y },{ 0.0, 0.0, 1.0, _WorldSpaceCameraPos.z },{ 0.0, 0.0, 0.0, 1.0 } };
    return mul(inverseModelMatrix, translationMatrix);
#else
    return inverseModelMatrix;
#endif
}

void ApplyCameraRelativeXR(inout float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0) && defined(USING_STEREO_MATRICES)
    positionWS += _WorldSpaceCameraPosViewOffset;
#endif
}

float GetCurrentExposureMultiplier()
{
#if SHADEROPTIONS_PRE_EXPOSITION
    // _ProbeExposureScale is a scale used to perform range compression to avoid saturation of the content of the probes. It is 1.0 if we are not rendering probes.
    return LOAD_TEXTURE2D(_ExposureTexture, int2(0, 0)).x * _ProbeExposureScale;
#else
    return _ProbeExposureScale;
#endif
}

float GetPreviousExposureMultiplier()
{
#if SHADEROPTIONS_PRE_EXPOSITION
    // _ProbeExposureScale is a scale used to perform range compression to avoid saturation of the content of the probes. It is 1.0 if we are not rendering probes.
    return LOAD_TEXTURE2D(_PrevExposureTexture, int2(0, 0)).x * _ProbeExposureScale;
#else
    return _ProbeExposureScale;
#endif
}

float GetInverseCurrentExposureMultiplier()
{
    float exposure = GetCurrentExposureMultiplier();
    return rcp(exposure + (exposure == 0.0)); // zero-div guard
}

float GetInversePreviousExposureMultiplier()
{
    float exposure = GetPreviousExposureMultiplier();
    return rcp(exposure + (exposure == 0.0)); // zero-div guard
}

// Helper function for indirect control volume
float GetIndirectDiffuseMultiplier(uint renderingLayers)
{
    return (_IndirectDiffuseLightingLayers & renderingLayers) ? _IndirectDiffuseLightingMultiplier : 1.0f;
}

float GetIndirectSpecularMultiplier(uint renderingLayers)
{
    return (_ReflectionLightingLayers & renderingLayers) ? _ReflectionLightingMultiplier : 1.0f;
}

// Functions to clamp UVs to use when RTHandle system is used.

float2 ClampAndScaleUV(float2 UV, float2 texelSize, float numberOfTexels)
{
    float2 maxCoord = 1.0f - numberOfTexels * texelSize;
    return min(UV, maxCoord) * _RTHandleScale.xy;
}

// This is assuming half a texel offset in the clamp.
float2 ClampAndScaleUVForBilinear(float2 UV, float2 texelSize)
{
    return ClampAndScaleUV(UV, texelSize, 0.5f);
}

// This is assuming full screen buffer and .
float2 ClampAndScaleUVForBilinear(float2 UV)
{
    return ClampAndScaleUV(UV, _ScreenSize.zw, 0.5f);
}

float2 ClampAndScaleUVForPoint(float2 UV)
{
    return min(UV, 1.0f) * _RTHandleScale.xy;
}

uint Get1DAddressFromPixelCoord(uint2 pixCoord, uint2 screenSize, uint eye)
{
    // We need to shift the index to look up the right eye info.
    return (pixCoord.y * screenSize.x + pixCoord.x) + eye * (screenSize.x * screenSize.y);
}

uint Get1DAddressFromPixelCoord(uint2 pixCoord, uint2 screenSize)
{
    return Get1DAddressFromPixelCoord(pixCoord, screenSize, 0);
}

// Define Model Matrix Macro
// Note: In order to be able to define our macro to forbid usage of unity_ObjectToWorld/unity_WorldToObject
// We need to declare inline function. Using uniform directly mean they are expand with the macro
float4x4 GetRawUnityObjectToWorld() { return unity_ObjectToWorld; }
float4x4 GetRawUnityWorldToObject() { return unity_WorldToObject; }

#define UNITY_MATRIX_M     ApplyCameraTranslationToMatrix(GetRawUnityObjectToWorld())
#define UNITY_MATRIX_I_M   ApplyCameraTranslationToInverseMatrix(GetRawUnityWorldToObject())

// To get instancing working, we must use UNITY_MATRIX_M / UNITY_MATRIX_I_M as UnityInstancing.hlsl redefine them
#define unity_ObjectToWorld Use_Macro_UNITY_MATRIX_M_instead_of_unity_ObjectToWorld
#define unity_WorldToObject Use_Macro_UNITY_MATRIX_I_M_instead_of_unity_WorldToObject

// Define View/Projection matrix macro
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesMatrixDefsHDCamera.hlsl"

// This define allow to tell to unity instancing that we will use our camera relative functions (ApplyCameraTranslationToMatrix and  ApplyCameraTranslationToInverseMatrix) for the model view matrix
#define MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

// This is located after the include of UnityInstancing.hlsl so it can be used for declaration
// Undef in order to include all textures and buffers declarations
#undef SHADER_VARIABLES_INCLUDE_CB
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/ShaderVariablesLightLoop.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/ShaderVariablesAtmosphericScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ShaderVariablesScreenSpaceLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderVariablesDecal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/ShaderVariablesSubsurfaceScattering.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

#endif // UNITY_SHADER_VARIABLES_INCLUDED
