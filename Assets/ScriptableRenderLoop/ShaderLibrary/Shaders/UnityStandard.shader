Shader "Unity Standard"
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
	
	#include "Lighting.hlsl"
	#include "Material.hlsl"

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 5.0

			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 5.0
			#pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

			// Include
			#include "Lighting.hlsl"
			#include "Material.hlsl"

			VertexOutput MainVs( VertexInput i )
			{
				// TODO : here we must support anykind of vertex animation
				VertexOutput o;

				//o.vPositionPs.xyzw = mul( UNITY_MATRIX_MVP, i.vPositionOs.xyzw );

				float3 vNormalWs = UnityObjectToWorldNormal( i.vNormalOs.xyz );
				float3 vPositionWs = mul( unity_ObjectToWorld, i.vPositionOs.xyzw ).xyz;
				float2 vShadowOffsets = GetShadowOffsets( vNormalWs.xyz, g_vLightDirWs.xyz );
				vPositionWs.xyz -= vShadowOffsets.x * vNormalWs.xyz / 100;
				vPositionWs.xyz += vShadowOffsets.y * g_vLightDirWs.xyz / 1000;
				o.vPositionPs.xyzw = mul( UNITY_MATRIX_MVP, float4( mul( unity_WorldToObject, float4( vPositionWs.xyz, 1.0 ) ).xyz, 1.0 ) );
				return o;
			}

			float4 MainPs( VertexOutput i ) : SV_Target
			{
				return float4( 0.0, 0.0, 0.0, 0.0 );
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 5.0
			#pragma exclude_renderers nomrt


			void fragDeferred2 (
				VertexOutputDeferred i,
				out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
				out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
				out half4 outNormal : SV_Target2,			// RT2: normal (rgb), --unused, very low precision-- (a) 
				out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
			)
			{
				#if (SHADER_TARGET < 30)
					outDiffuse = 1;
					outSpecSmoothness = 1;
					outNormal = 0;
					outEmission = 0;
					return;
				#endif

				FRAGMENT_SETUP(s)
			#if UNITY_OPTIMIZE_TEXCUBELOD
				s.reflUVW		= i.reflUVW;
			#endif

				// no analytic lights in this pass
				UnityLight dummyLight = DummyLight (s.normalWorld);
				half atten = 1;

				// only GI
				half occlusion = Occlusion(i.tex.xy);
			#if UNITY_ENABLE_REFLECTION_BUFFERS
				bool sampleReflectionsInDeferred = false;
			#else
				bool sampleReflectionsInDeferred = true;
			#endif

				UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

				half3 color = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
				color += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);

				#ifdef _EMISSION
					color += Emission (i.tex.xy);
				#endif

				#ifndef UNITY_HDR_ON
					color.rgb = exp2(-color.rgb);
				#endif

				outDiffuse =  half4(s.diffColor, occlusion);
				outSpecSmoothness = half4(s.specColor, s.oneMinusRoughness);
				outNormal = half4(s.normalWorld*0.5+0.5,1);
				outEmission = half4(color, 1);
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}

	CustomEditor "StandardShaderGUI"
}
