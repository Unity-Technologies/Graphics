Shader "Hidden/Unity/LightingDeferred" {
	Properties{
	_SrcBlend("", Float) = 1
	_DstBlend("", Float) = 1
}
	SubShader{

		Pass{
		ZWrite Off
		Blend[_SrcBlend][_DstBlend]

		CGPROGRAM
		#pragma target 5.0
		#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

		#pragma vertex VertDeferred
		#pragma fragment FragDeferred

		#define UNITY_SHADERRENDERPASS UNITY_SHADERRENDERPASS_DEFERRED
		// CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_DISNEYGXX
		#define UNITY_MATERIAL_DISNEYGXX // Need to be define before including Material.hlsl
		#include "Lighting/Lighting.hlsl" // This include Material.hlsl
		#include "ShaderVariables.hlsl"

		sampler2D _CameraGBufferTexture0;
		sampler2D _CameraGBufferTexture1;
		sampler2D _CameraGBufferTexture2;
		sampler2D _CameraGBufferTexture3;

		sampler2D_float _CameraDepthTexture;

		float _LightAsQuad;

		struct Attributes
		{
			float3 positionOS : POSITION;
			float3 normalOS : NORMAL;
		};

		struct Varyings
		{
			float4 positionHS : SV_POSITION;
			float4 uv : TEXCOORD0;
			float3 ray : TEXCOORD1;
		};

		Varyings VertDeferred(Attributes input)
		{
			Varyings output;
			output.positionWS = TransformObjectToWorld(input.positionOS);
			output.positionHS = TransformWorldToHClip(output.positionWS);
			output.uv = ComputeScreenPos(o.positionOS);
			output.ray = TransformObjectToView(input.positionOS) * float3(-1, -1, 1);

			// normal contains a ray pointing from the camera to one of near plane's
			// corners in camera space when we are drawing a full screen quad.
			// Otherwise, when rendering 3D shapes, use the ray calculated here.
			output.ray = lerp(output.ray, normalOS, _LightAsQuad);

			return output;
		}

		float4 FragDeferred(Varyings input) : SV_Target
		{
			/*
			input.ray = input.ray * tex2D(_CameraDepthTexture.z / input.ray.z);
			float2 uv = input.uv.xy / input.uv.w;

			// read depth and reconstruct world position
			float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
			depth = Linear01Depth(depth);
			float4 vpos = float4(i.ray * depth, 1);
			float3 wpos = mul(unity_CameraToWorld, vpos).xyz;

			// read depth and reconstruct world position
			float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
			depth = Linear01Depth(depth);
			float4 vpos = float4(i.ray * depth, 1);
			float3 wpos = mul(unity_CameraToWorld, vpos).xyz;

			// unpack Gbuffer
			float4 gbuffer0 = tex2D(_CameraGBufferTexture0, input.uv);
			float4 gbuffer1 = tex2D(_CameraGBufferTexture1, input.uv);
			float4 gbuffer2 = tex2D(_CameraGBufferTexture2, input.uv);
			float4 gbuffer3 = tex2D(_CameraGBufferTexture3, input.uv);

			BSDFData bsdfData = DecodeFromGBuffer(gbuffer0, gbuffer1, gbuffer2);

			return bsdfData.diffuseColor;
			*/

			return float4(1, 0, 0, 0);
		}

		ENDCG
	}

	}
	Fallback Off
}
