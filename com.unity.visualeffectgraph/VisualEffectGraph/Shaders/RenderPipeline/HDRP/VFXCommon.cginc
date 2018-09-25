#include "CoreRP/ShaderLibrary/common.hlsl"
#include "HDRP/ShaderVariables.hlsl"
#include "HDRP/ShaderPass/ShaderPass.cs.hlsl"
#include "HDRP/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

#if IS_TRANSPARENT_PARTICLE
#define USE_FOG 1
#endif

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
#if VFX_WORLD_SPACE
    posWS = GetCameraRelativePositionWS(posWS);
#endif
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float3 posWS = TransformObjectToWorld(posOS);
    return VFXTransformPositionWorldToClip(posWS);
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
#if VFX_WORLD_SPACE
    posWS = GetCameraRelativePositionWS(posWS);
#endif
    return TransformWorldToView(posWS);
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
    float3 pos = GetCurrentViewPosition();
#if VFX_WORLD_SPACE
    pos = GetAbsolutePositionWS(pos);
#endif
    return pos;
}

float4x4 VFXGetViewToWorldMatrix()
{
    float4x4 viewToWorld = UNITY_MATRIX_I_V;
    viewToWorld._14_24_34 = VFXGetViewWorldPosition();
    return viewToWorld;
}

float VFXSampleDepth(float4 posSS)
{
    return LOAD_TEXTURE2D(_CameraDepthTexture, posSS.xy).r;
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth,_ZBufferParams);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return posCS;
}

float4 VFXApplyFog(float4 color,float4 posCS,float3 posWS)
{
#if IS_TRANSPARENT_PARTICLE
#if VFX_WORLD_SPACE
	posWS = GetCameraRelativePositionWS(posWS);
#endif 
	PositionInputs posInput = GetPositionInput(posCS.xy, _ScreenSize.zw, posCS.z, posCS.w, posWS, uint2(0,0));
	float4 fog = EvaluateAtmosphericScattering(posInput);
	color.rgb *= fog.rgb;
#endif
	return color;
}
