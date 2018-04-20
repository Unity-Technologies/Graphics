#include "UnityCG.cginc"

#define VFX_EPSILON 1e-5

UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return UnityWorldToClipPos(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return UnityObjectToClipPos(posOS);
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return unity_ObjectToWorld;
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return unity_WorldToObject;
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)UNITY_MATRIX_V;
}

float3 VFXGetViewWorldPosition()
{
    // Not using _WorldSpaceCameraPos as it's not what expected for the shadow pass
    // (It remains primary camera position not view position)
    return UNITY_MATRIX_I_V._m03_m13_m23;
}

float4 VFXGetPOSSS(float4 posCS)
{
    return ComputeScreenPos(posCS);
}

float VFXSampleDepth(float4 posSS)
{
    return SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(posSS));
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return UnityApplyLinearShadowBias(posCS);
}
