#include "CoreRP/ShaderLibrary/common.hlsl"
#include "HDRP/ShaderVariables.hlsl"
#include "HDRP/Sky/AtmosphericScattering/AtmosphericScattering.hlsl"

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    posWS = GetCameraRelativePositionWS(posWS);
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float3 posWS = TransformObjectToWorld(posOS);
    return VFXTransformPositionWorldToClip(posWS);
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return GetObjectToWorldMatrix();
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return GetWorldToObjectMatrix();
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)GetWorldToViewMatrix();
}

float3 VFXGetViewWorldPosition()
{
    return GetAbsolutePositionWS(GetCurrentViewPosition());
}

float4 VFXGetPOSSS(float4 posCS)
{
    float4 posSS = posCS * 0.5f;
    posSS.xy = float2(posSS.x, posSS.y*_ProjectionParams.x) + posSS.w;
    posSS.zw = posCS.zw;
    return posSS;
}

float VFXSampleDepth(float4 posSS)
{
    return SAMPLE_TEXTURE2D(_MainDepthTexture, sampler_MainDepthTexture, posSS.xyz / posSS.w);
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth,_ZBufferParams);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return posCS;
}
