#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

float3 _LightDirection;

#ifdef VFX_VARYING_PS_INPUTS
void VFXTransformPSInputs(inout VFX_VARYING_PS_INPUTS input) {}

float4 VFXApplyPreExposure(float4 color, float exposureWeight)
{
    return color;
}

float4 VFXApplyPreExposure(float4 color, VFX_VARYING_PS_INPUTS input)
{
    return color;
}
#endif

float4 VFXTransformFinalColor(float4 color)
{
    return color;
}

float2 VFXGetNormalizedScreenSpaceUV(float4 clipPos)
{
    return GetNormalizedScreenSpaceUV(clipPos);
}

void VFXEncodeMotionVector(float2 velocity, out float4 outBuffer)
{
    //TODO : LWRP doesn't support motion vector & TAA yet
    outBuffer = (float4)0.0f;
}

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionWorldToNonJitteredClip(float3 posWS)
{
    //TODO : LWRP doesn't support motion vector & TAA yet
    return VFXTransformPositionWorldToClip(posWS);
}

float4 VFXTransformPositionWorldToPreviousClip(float3 posWS)
{
    //TODO : LWRP doesn't support motion vector & TAA yet
    return VFXTransformPositionWorldToClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float3 posWS = TransformObjectToWorld(posOS);
    return VFXTransformPositionWorldToClip(posWS);
}

float4 VFXTransformPositionObjectToNonJitteredClip(float3 posOS)
{
    //TODO : LWRP doesn't support motion vector & TAA yet
    return VFXTransformPositionObjectToClip(posOS);
}

float4 VFXTransformPositionObjectToPreviousClip(float3 posOS)
{
    //TODO : LWRP doesn't support motion vector & TAA yet
    return VFXTransformPositionObjectToClip(posOS);
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return TransformWorldToView(posWS);
}

float3 VFXTransformPositionWorldToCameraRelative(float3 posWS)
{
#if SHADEROPTIONS_CAMERA_RELATIVE_RENDERING
#error VFX Camera Relative rendering isn't supported in URP.
#endif
    return posWS;
}

//Compatibility functions for the common ShaderGraph integration
float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    return modelMatrix;
}
float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
    return inverseModelMatrix;
}
//End of compatibility functions

float4x4 VFXGetObjectToWorldMatrix()
{
    // NOTE: If using the new generation path, explicitly call the object matrix (since the particle matrix is now baked into UNITY_MATRIX_M)
#if defined(HAVE_VFX_MODIFICATION) && !defined(SHADER_STAGE_COMPUTE)
    return GetSGVFXUnityObjectToWorld();
#else
    return GetObjectToWorldMatrix();
#endif
}

float4x4 VFXGetWorldToObjectMatrix()
{
    // NOTE: If using the new generation path, explicitly call the object matrix (since the particle matrix is now baked into UNITY_MATRIX_I_M)
#if defined(HAVE_VFX_MODIFICATION) && !defined(SHADER_STAGE_COMPUTE)
    return GetSGVFXUnityWorldToObject();
#else
    return GetWorldToObjectMatrix();
#endif
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)GetWorldToViewMatrix();
}

float3 VFXGetViewWorldPosition()
{
    return GetCurrentViewPosition();
}

float4x4 VFXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

#ifdef USING_STEREO_MATRICES
float3 GetWorldStereoOffset()
{
    return float3(0.0f, 0.0f, 0.0f);
}
#endif

float VFXSampleDepth(float4 posSS)
{
    float2 screenUV = GetNormalizedScreenSpaceUV(posSS.xy);

    // In URP, the depth texture is optional and could be 4x4 white texture, Load isn't appropriate in that case.
    //float depth = LoadSceneDepth(screenUV * _ScreenParams.xy);
    float depth = SampleSceneDepth(screenUV);

    return depth;
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS, float3 normalWS)
{
    posWS = ApplyShadowBias(posWS, normalWS, _LightDirection);
    posCS = VFXTransformPositionWorldToClip(posWS);
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS)
{
    posWS = ApplyShadowBias(posWS, _LightDirection, _LightDirection);
    posCS = VFXTransformPositionWorldToClip(posWS);
}

float4 VFXApplyAO(float4 color, float4 posCS)
{
#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
    float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(posCS);
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
    color.rgb *= aoFactor.directAmbientOcclusion;
#endif

    return color;
}

float4 VFXApplyFog(float4 color,float4 posCS,float3 posWS)
{
   float4 fog = (float4)0;
   fog.rgb = unity_FogColor.rgb;

   float fogFactor = ComputeFogFactor(posCS.z * posCS.w);
   fog.a = ComputeFogIntensity(fogFactor);

#if VFX_BLENDMODE_ALPHA || IS_OPAQUE_PARTICLE
   color.rgb = lerp(fog.rgb, color.rgb, fog.a);
#elif VFX_BLENDMODE_ADD
   color.rgb *= fog.a;
#elif VFX_BLENDMODE_PREMULTIPLY
   color.rgb = lerp(fog.rgb * color.a, color.rgb, fog.a);
#endif
   return color;
}

float3 VFXGetCameraWorldDirection()
{
    return unity_CameraToWorld._m02_m12_m22;
}

#if defined(_GBUFFER_NORMALS_OCT)
#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);         /*values between [-1, +1], must use fp32 on some platforms*/ \
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5); /*values between [ 0,  1]*/ \
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);    /*values between [ 0,  1]*/ \
	outNormalBuffer = float4(packedNormalWS, 0.0); \
}
#else
#define VFXComputePixelOutputToNormalBuffer(i,normalWS,uvData,outNormalBuffer) \
{ \
    outNormalBuffer = float4(normalWS, 0.0); \
}
#endif
