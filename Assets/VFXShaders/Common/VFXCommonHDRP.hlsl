#include "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/Core/ShaderLibrary/common.hlsl"
#include "Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/HDRenderPipeline/ShaderVariables.hlsl"

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    posWS = GetCameraRelativePositionWS(posWS);
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float posWS = TransformObjectToWorld(posOS)
    return VFXTransformWorldToClipSpace(posWS);
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
