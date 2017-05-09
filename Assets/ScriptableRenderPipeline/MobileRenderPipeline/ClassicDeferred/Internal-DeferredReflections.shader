Shader "MobileRenderloop/ClassicDeferredReflections" {
Properties 
{
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
    _SrcABlend ("", Float) = 1
    _DstABlend ("", Float) = 1
    _CullMode ("", Float) = 0
    _CompareFunc ("", Float) = 0
}

SubShader {

// Calculates reflection contribution from a single probe (rendered as cubes) or default reflection (rendered as full screen quad)
//  Finite: Blend DstAlpha One, DstAlpha Zero
//	clipping near plane: Cull Front; ZTest GEqual; Blend DstAlpha One, DstAlpha Zero
//  renderAsQuad: Cull Off; ZTest Always; Blend DstAlpha One, DstAlpha Zero

Pass {
    ZWrite Off
    Cull [_CullMode]
    ZTest [_CompareFunc]
    Blend [_SrcBlend] [_DstBlend], [_SrcABlend] [_DstABlend]

CGPROGRAM
#pragma target 4.5
#pragma vertex onchip_vert_deferred
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityPBSLighting.cginc"

#include "LightingTemplate.hlsl"

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
    return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

#ifdef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
void frag (unity_v2f_deferred i,
	in half4 gbuffer0 : SV_Target0,
	in half4 gbuffer1 : SV_Target1,
	in half4 gbuffer2 : SV_Target2,
	out half4 outEmission : SV_Target3, 
	in float linearDepth : SV_Target4)
#else
half4 frag (unity_v2f_deferred i) : SV_TARGET
#endif
{
    float2 uv;
    float4 viewPos;
    float3 worldPos;

	#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
    	OnChipDeferredFragSetup(i, uv, viewPos, worldPos, 0.0);
	#else
    	OnChipDeferredFragSetup(i, uv, viewPos, worldPos, linearDepth);
	#endif

#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
	// unpack Gbuffer
	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
#endif
    UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

    float3 eyeVec = normalize(worldPos - _WorldSpaceCameraPos);
    half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);

// ---

    half3 worldNormalRefl = reflect(eyeVec, data.normalWorld);

    // Unused member don't need to be initialized
    UnityGIInput d;
    d.worldPos = worldPos;
    d.worldViewDir = -eyeVec;
    d.probeHDR[0] = unity_SpecCube0_HDR;

    float blendDistance = unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    d.probePosition[0]  = unity_SpecCube0_ProbePosition;
    d.boxMin[0].xyz     = unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
    d.boxMin[0].w       = 1;  // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call
    d.boxMax[0].xyz     = unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
    #endif

    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(data.smoothness, d.worldViewDir, data.normalWorld, data.specularColor);

    half3 env0 = UnityGI_IndirectSpecular(d, data.occlusion, g);

    UnityLight light;
    light.color = half3(0, 0, 0);
    light.dir = half3(0, 1, 0);

    UnityIndirect ind;
    ind.diffuse = 0;
    ind.specular = env0;

    half3 rgb = UNITY_BRDF_PBS (0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind).rgb;

    // Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
    // Also this ensures that pixels not located in the reflection probe AABB won't
    // accidentally pick up reflections from this probe.
    half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
    half falloff = saturate(1.0 - length(distance)/blendDistance);

    half4 ret = half4(rgb*falloff, 1-falloff);

    //debug
    //if (all(worldPos>specMin && worldPos<specMax))
    	//ret = half4(frac(0.2*worldPos), 1);

    // UNITY_BRDF_PBS1 writes out alpha 1 to our emission alpha. TODO: Should preclear emission alpha after gbuffer pass in case this ever changes
    #ifdef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
    	outEmission = ret;
    #else
        return ret;
    #endif
}

ENDCG
}

}
Fallback Off
}
