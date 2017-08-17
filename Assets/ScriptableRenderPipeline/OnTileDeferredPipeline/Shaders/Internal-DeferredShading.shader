Shader "MobileRenderloop/ClassicDeferred" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_LightTextureB0 ("", 2D) = "" {}
	_ShadowMapTexture ("", any) = "" {}

	_SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
    _SrcABlend ("", Float) = 1
    _DstABlend ("", Float) = 1

    _CullMode ("", Float) = 0
    _CompareFunc ("", Float) = 0

}
SubShader {

//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {

	ZWrite Off
	ZTest [_CompareFunc]
	Cull [_CullMode]
	Blend [_SrcBlend] [_DstBlend], [_SrcABlend] [_DstABlend]

CGPROGRAM
#pragma target 4.5
#pragma vertex onchip_vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

#include "LightingTemplate.hlsl"


half4 frag (unity_v2f_deferred i) : SV_TARGET
{
	return CalculateLight(i);
}

ENDCG
}

}
Fallback Off
}
