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
	#include "ShaderVariables.hlsl"

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
			
			#pragma vertex MainVS
			#pragma fragment MainPS
			
			
			//-------------------------------------------------------------------------------------
			// Input functions

			struct VSInput
			{
				float4 positionOS	: POSITION; // TODO: why do we provide w here ? putting constant to 1 will save a float
				half3 normalOS		: NORMAL;
				float2 uv0			: TEXCOORD0;
				half4 tangentOS		: TANGENT;
			};
			
			struct VSOutput
			{
				float4 positionHS : SV_POSITION;
				float2 texCoord0 : TEXCOORD0;
				float4 tangentToWorld[3] : TEXCOORD1; // [3x3:tangentToWorld | 1x3:viewDirForParallax]
			};

			VSOutput MainVS( VSInput i )
			{
				// TODO : here we must support anykind of vertex animation (GPU skinning, morphing) or better do it in compute.
				VSOutput o;

				float3 positionWS = TransformObjectToWorld(i.positionOS.xyz);
				// TODO deal with camera center rendering and instancing (This is the reason why we always perform tow step transform to clip space + instancing matrix)
				o.positionHS = TransformWorldToHClip(positionWS);

				float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
	
				o.texCoord0 = i.uv0;

				#ifdef _TANGENT_TO_WORLD
				float4 tangentWS = float4(TransformObjectToWorldDir(i.tangentOS.xyz), i.tangentOS.w);

				float3x3 tangentToWorld = CreateTangentToWorld(normalWorld, tangentWorld.xyz, tangentWorld.w);
				o.tangentToWorld[0].xyz = tangentToWorld[0];
				o.tangentToWorld[1].xyz = tangentToWorld[1];
				o.tangentToWorld[2].xyz = tangentToWorld[2];
				#else
				o.tangentToWorld[0].xyz = 0;
				o.tangentToWorld[1].xyz = 0;
				o.tangentToWorld[2].xyz = normalWS;
				#endif

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
