Shader "Unity/UnityStandard"
{
	Properties
	{
		_DiffuseColor("Diffuse", Color) = (1,1,1,1)
		_DiffuseMap("Diffuse", 2D) = "white" {}

		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecMap("Specular", 2D) = "white" {}

		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SmoothnessMap("Smoothness", 2D) = "white" {}

		_NormalMap("Normal Map", 2D) = "bump" {}
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5		

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
	}

	CGINCLUDE
	
	#include "Material/Material.hlsl"

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "Forward" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

			// Include
			#include "ShaderVariables.hlsl"
			
			
			#pragma vertex MainVS
			#pragma fragment MainPS
			
			
			//-------------------------------------------------------------------------------------
			// Input functions

			struct VSInput
			{
				float4 vertex	: POSITION;
				half3 normal	: NORMAL;
				float2 uv0		: TEXCOORD0;
				//float2 uv1		: TEXCOORD1;
				//#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
				//	float2 uv2		: TEXCOORD2;
				//#endif
				//#ifdef _TANGENT_TO_WORLD
					half4 tangent	: TANGENT;
				//#endif
				//UNITY_INSTANCE_ID
			};
			
			struct VSOutput
			{
				float4 positionWS : SV_POSITION;
				float2 texCoord0 : TEXCOORD0;
				float4 tangentToWorldAndParallax : TANGENT; // [3x3:tangentToWorld | 1x3:viewDirForParallax]
			};				

			VSOutput MainVS( VSInput i )
			{
				// TODO : here we must support anykind of vertex animation
				PSInput o;

				o.positionWS = UnityObjectToClipPos(v.vertex);
	
				float3 positionWS = mul( unity_ObjectToWorld, i.vPositionOs.xyzw ).xyz;
				o.positionWS.xyzw = mul( UNITY_MATRIX_MVP, float4( mul( unity_WorldToObject, float4( vPositionWs.xyz, 1.0 ) ).xyz, 1.0 ) );
				return o;
			}

			float4 MainPS( VSOutput i ) : SV_Target
			{
				return float4( 1.0, 0.0, 0.0, 0.0 );
			}

			ENDCG
		}
	}
}
