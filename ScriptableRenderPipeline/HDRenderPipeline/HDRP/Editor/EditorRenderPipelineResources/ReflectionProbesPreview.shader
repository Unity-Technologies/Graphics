// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Debug/ReflectionProbePreview"
{
	Properties
	{
		_Cubemap("_Cubemap", Cube) = "white" {}
		_CameraWorldPosition("_CameraWorldPosition", Vector) = (1,1,1,1)
		_MipLevel("_MipLevel", Range(0.0,7.0)) = 0.0
		_Exposure("_Exposure", Range(-10.0,10.0)) = 0.0

	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Transparent" }
		LOD 100
		ZWrite On
		Cull Back
		LOD 100

		Pass
	{
		Name "ForwardUnlit"
		Tags{ "LightMode" = "Forward" }

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


		struct appdata
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float3 normal : NORMAL;
		float3 worldpos : TEXCOORD0;
	};

	samplerCUBE _Cubemap;
	float3 _CameraWorldPosition;
	float _MipLevel;
	float _Exposure;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.worldpos = mul(unity_ObjectToWorld, v.vertex);
		o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
		return o;
	}

	float4 frag(v2f i) : SV_Target
	{
		//float3 view = normalize(i.worldpos - _CameraWorldPosition);
		float3 view = normalize(i.worldpos - _WorldSpaceCameraPos);
		float3 reflected = reflect(view, i.normal);
		float4 col = texCUBElod(_Cubemap,float4(reflected,_MipLevel));
		col = col*exp2(_Exposure);
		return col;
	}
		ENDCG
	}
	}
}
