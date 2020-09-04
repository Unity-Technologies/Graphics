#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
#ifdef DEBUG_DISPLAY
    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
    {
        color = float4(TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, TRANSPARENCY_OVERDRAW_COST, color.a);
    }
#endif
    return color;
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

float4x4 VFXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

float VFXSampleDepth(float4 posSS)
{
    return LoadSceneDepth(uint2(posSS.xy));
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth, _ZBufferParams);
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
