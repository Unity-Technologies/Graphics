#include "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/Core/ShaderLibrary/common.hlsl"
#include "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/HDRenderPipeline/ShaderVariables.hlsl"
#include "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/HDRenderPipeline/Sky/AtmosphericScattering/AtmosphericScattering.hlsl"

#define VFX_DEPTH_TEXTURE _MainDepthTexture

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
    return GetCurrentViewPosition();
}

float4 VFXGetPOSSS(float4 posCS)
{
    float4 posSS = posCS * 0.5f;
    posSS.xy = float2(posSS.x, posSS.y*_ProjectionParams.x) + posSS.w;
    posSS.zw = posCS.zw;
    return posSS;
}

float VFXLinearEyeDepth(float4 posSS)
{
    return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_MainDepthTexture, UNITY_PROJ_COORD(posSS)),_ZBufferParams);
}
