#include "UnityCG.cginc"

Texture2D _CameraDepthTexture;

//Additionnal empty wrapper (equivalent to expected functions in com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl)
float3 GetAbsolutePositionWS(float3 positionRWS)
{
    return positionRWS;
}
float3 GetCameraRelativePositionWS(float3 positionWS)
{
    return positionWS;
}

void VFXTransformPSInputs(inout VFX_VARYING_PS_INPUTS input) {}

void VFXEncodeMotionVector(float2 velocity, out float4 outBuffer)
{
	outBuffer = (float4)0.0f; //TODO
}

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return UnityWorldToClipPos(posWS);
}

float4 VFXTransformFinalColor(float4 color)
{
	return color;
}

float4 VFXTransformPositionWorldToNonJitteredClip(float3 posWS)
{
	return VFXTransformPositionWorldToClip(posWS); //TODO
}

float4 VFXTransformPositionWorldToPreviousClip(float3 posWS)
{
	return VFXTransformPositionWorldToClip(posWS); //TODO
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return UnityObjectToClipPos(posOS);
}

float4 VFXTransformPositionObjectToNonJitteredClip(float3 posOS)
{
	return VFXTransformPositionObjectToClip(posOS); //TODO
}

float4 VFXTransformPositionObjectToPreviousClip(float3 posOS)
{
	return VFXTransformPositionObjectToClip(posOS); //TODO
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return mul(UNITY_MATRIX_V, float4(posWS, 1.0f)).xyz;
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

float4x4 VFXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

float VFXSampleDepth(float4 posSS)
{
    return _CameraDepthTexture.Load(int3(posSS.xy, 0)).r;
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return UnityApplyLinearShadowBias(posCS);
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS, float3 normalWS)
{
    posCS = UnityApplyLinearShadowBias(posCS);
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS)
{
    posCS = UnityApplyLinearShadowBias(posCS);
}

float4 VFXApplyFog(float4 color,float4 posSS,float3 posWS)
{
    return color; // TODO
}

float4 VFXApplyPreExposure(float4 color, VFX_VARYING_PS_INPUTS input)
{
    return color;
}
