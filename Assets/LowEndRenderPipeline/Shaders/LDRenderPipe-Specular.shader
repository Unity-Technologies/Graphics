// Shader targeted for LowEnd mobile devices. Single Pass Forward Rendering. Shader Model 2
Shader "LDRenderPipeline/Specular"
{
	// Keep properties of StandardSpecular shader for upgrade reasons.
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Shininess", Range(0.01, 1.0)) = 0.5
		_GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0
		[Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		_SpecColor("Specular", Color) = (1.0, 1.0, 1.0)
		_SpecGlossMap("Specular", 2D) = "white" {}
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		[HideInInspector] _BumpScale("Scale", Float) = 1.0
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Emission Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

		// Blending state
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "PerformanceChecks" = "False" "RenderPipeline" = "LDRenderPipeline" }
		LOD 300

		Pass
		{
			Name "SINGLE_PASS_FORWARD"
			Tags { "LightMode" = "LDForwardLight" }

			// Use same blending / depth states as Standard shader
			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _NORMALMAP
			
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ SHADOWS_DEPTH
			#pragma multi_compile _ SHADOWS_FILTERING_PCF
			#pragma multi_compile_fog
			#pragma only_renderers d3d9 d3d11 d3d11_9x glcore gles gles3 metal
			#pragma enable_d3d11_debug_symbols
			
			#define DIFFUSE_AND_SPECULAR_INPUT SpecularInput

			#include "UnityCG.cginc"
			#include "UnityStandardBRDF.cginc"
			#include "UnityStandardInput.cginc"
			#include "UnityStandardUtils.cginc"

			#define DEBUG_CASCADES 0
			#define MAX_SHADOW_CASCADES 4
			#define MAX_LIGHTS 8

			#define INITIALIZE_LIGHT(light, lightIndex) \
							light.pos = globalLightPos[lightIndex]; \
							light.color = globalLightColor[lightIndex]; \
							light.atten = globalLightAtten[lightIndex]; \
							light.spotDir = globalLightSpotDir[lightIndex]

			#define FRESNEL_TERM(normal, viewDir) Pow4(1.0 - saturate(dot(normal, viewDir)))
			
			// The variables are very similar to built-in unity_LightColor, unity_LightPosition,
			// unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
			// we use world space positions instead of view space.
			half4 globalLightColor[MAX_LIGHTS];
			float4 globalLightPos[MAX_LIGHTS];
			half4 globalLightSpotDir[MAX_LIGHTS];
			half4 globalLightAtten[MAX_LIGHTS];
			int4  globalLightCount; // x: pixelLightCount, y = totalLightCount (pixel + vert)

			sampler2D_float _ShadowMap;
			float _PCFKernel[8];

			half4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
			half4 _PSSMDistancesAndShadowResolution; // xyz: PSSM Distance for 4 cascades, w: 1 / shadowmap resolution. Used for filtering

			struct LowendVertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float3 texcoord : TEXCOORD0;
				float2 lightmapUV : TEXCOORD1;
			};

			struct v2f
			{
				float4 uv01 : TEXCOORD0; // uv01.xy: uv0, uv01.zw: uv1
				float4 posWS : TEXCOORD1; // xyz: posWorld, w: eyeZ
	#if _NORMALMAP
				half3 tangentToWorld[3] : TEXCOORD2; // tangentToWorld matrix
	#else
				half3 normal : TEXCOORD2;
	#endif
				half4 viewDir : TEXCOORD5; // xyz: viewDir, w: grazingTerm;
				UNITY_FOG_COORDS_PACKED(6, half4) // x: fogCoord, yzw: vertexColor
					float4 hpos : SV_POSITION;
			};

			struct LightInput
			{
				half4 pos;
				half4 color;
				half4 atten;
				half4 spotDir;
			};

			inline half ComputeCascadeIndex(half eyeZ)
			{
				// PSSMDistance is set to infinity for non active cascades. This way the comparison for unavailable cascades will always be zero. 
				half3 cascadeCompare = step(_PSSMDistancesAndShadowResolution.xyz, half3(eyeZ, eyeZ, eyeZ));
				return dot(cascadeCompare, cascadeCompare);
			}

			inline half ShadowAttenuation(half2 shadowCoord, half shadowCoordDepth)
			{
				half depth = tex2D(_ShadowMap, shadowCoord).r;
	#if defined(UNITY_REVERSED_Z)
				return step(depth, shadowCoordDepth);
	#else
				return step(shadowCoordDepth, depth);
	#endif
			}

			inline half ShadowPCF(half4 shadowCoord)
			{
				// GPU Gems 4x4 kernel with 4 taps.
				half2 offset = (float)(frac(shadowCoord.xy * 0.5) > 0.25);  // mod
				offset.y += offset.x;  // y ^= x in floating point
				offset *= _PSSMDistancesAndShadowResolution.w;

				half attenuation = ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[0], _PCFKernel[1]) + offset, shadowCoord.z) +
					ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[2], _PCFKernel[3]) + offset, shadowCoord.z) +
					ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[4], _PCFKernel[5]) + offset, shadowCoord.z) +
					ShadowAttenuation(shadowCoord.xy + half2(_PCFKernel[6], _PCFKernel[7]) + offset, shadowCoord.z);
				return attenuation * 0.25;
			}

			inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float3 posWorld, half3 viewDir)
			{
				float3 posToLight = lightInput.pos.xyz;
				posToLight -= posWorld * lightInput.pos.w;

				float distanceSqr = max(dot(posToLight, posToLight), 0.001);
				float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

				float3 lightDir = posToLight * rsqrt(distanceSqr);
				float SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
				lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

				float cutoff = step(distanceSqr, lightInput.atten.w);
				lightAtten *= cutoff;

				float NdotL = saturate(dot(normal, lightDir));

				half3 halfVec = normalize(lightDir + viewDir);
				half NdotH = saturate(dot(normal, halfVec));

				half3 lightColor = lightInput.color.rgb * lightAtten;
				half3 diffuse = diffuseColor * lightColor * NdotL;
				half3 specular = specularGloss.rgb * lightColor * pow(NdotH, _Glossiness * 128.0) * specularGloss.a;
				return diffuse + specular;
			}

			inline half3 EvaluateMainLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float4 posWorld, half3 viewDir)
			{
				int cascadeIndex = ComputeCascadeIndex(posWorld.w);
				float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorld.xyz, 1.0));
				shadowCoord.z = saturate(shadowCoord.z);

#ifdef SHADOWS_FILTERING_PCF
				half shadowAttenuation = ShadowPCF(shadowCoord);
#else
				half shadowAttenuation = ShadowAttenuation(shadowCoord.xy, shadowCoord.z);
#endif

#if DEBUG_CASCADES
				half3 cascadeColors[MAX_SHADOW_CASCADES] = { half3(1.0, 0.0, 0.0), half3(0.0, 1.0, 0.0),  half3(0.0, 0.0, 1.0),  half3(1.0, 0.0, 1.0) };
				return cascadeColors[cascadeIndex] * diffuseColor * max(shadowAttenuation, 0.5);
#endif

				half3 color = EvaluateOneLight(lightInput, diffuseColor, specularGloss, normal, posWorld, viewDir);

#ifdef SHADOWS_DEPTH
				return color * shadowAttenuation;
#else
				return color;
#endif
			}

			v2f vert(LowendVertexInput v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
				o.hpos = UnityObjectToClipPos(v.vertex);

				o.posWS.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.posWS.w = -UnityObjectToViewPos(v.vertex).z; 

				o.viewDir.xyz = normalize(_WorldSpaceCameraPos - o.posWS.xyz);
				half3 normal = normalize(UnityObjectToWorldNormal(v.normal));
				half fresnelTerm = FRESNEL_TERM(normal, o.viewDir.xyz);

#if _NORMALMAP
				half sign = v.tangent.w * unity_WorldTransformParams.w;
				half3 tangent = normalize(UnityObjectToWorldDir(v.tangent));
				half3 binormal = cross(normal, tangent) * v.tangent.w;

				// Initialize tangetToWorld in column-major to benefit from better glsl matrix multiplication code
				o.tangentToWorld[0] = half3(tangent.x, binormal.x, normal.x);
				o.tangentToWorld[1] = half3(tangent.y, binormal.y, normal.y);
				o.tangentToWorld[2] = half3(tangent.z, binormal.z, normal.z);
#else
				o.normal = normal;
#endif

				half4 diffuseAndSpecular = half4(1.0, 1.0, 1.0, 1.0);
				for (int lightIndex = globalLightCount.x; lightIndex < globalLightCount.y; ++lightIndex)
				{
					LightInput lightInput;
					INITIALIZE_LIGHT(lightInput, lightIndex);
					o.fogCoord.yzw += EvaluateOneLight(lightInput, diffuseAndSpecular.rgb, diffuseAndSpecular, normal, o.posWS.xyz, o.viewDir.xyz);
				}

#ifndef LIGHTMAP_ON
				o.fogCoord.yzw += max(half3(0, 0, 0), ShadeSH9(half4(normal, 1)));
#endif

				o.fogCoord.x = 1.0;
				UNITY_TRANSFER_FOG(o, o.hpos);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				half4 diffuseAlpha = tex2D(_MainTex, i.uv01.xy);
				half3 diffuse = diffuseAlpha.rgb * _Color.rgb;
				half alpha = diffuseAlpha.a * _Color.a;

#ifdef _ALPHATEST_ON
				clip(alpha - _Cutoff);
#endif

#if _NORMALMAP
				half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

				// glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
				// i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance. 
				// The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
				half3 normal = half3(dot(normalmap, i.tangentToWorld[0]), dot(normalmap, i.tangentToWorld[1]), dot(normalmap, i.tangentToWorld[2]));
#else
				half3 normal = normalize(i.normal);
#endif

#ifdef _SPECGLOSSMAP
				half4 specularGloss = tex2D(_SpecGlossMap, i.uv01.xy) * _SpecColor;
#else
				half4 specularGloss = _SpecColor;
#endif
				
				float3 posWorld = i.posWS.xyz;
				half3 viewDir = i.viewDir.xyz;

				// Indirect Light Contribution
				half3 indirectDiffuse;
#ifdef LIGHTMAP_ON
				indirectDiffuse = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) * diffuse;
#else
				indirectDiffuse = i.fogCoord.yzw * diffuse;
#endif
				// Compute direct contribution from main directional light.
				// Only a single directional shadow caster is supported.
				LightInput mainLight;
				INITIALIZE_LIGHT(mainLight, 0);

#if DEBUG_CASCADES
				return half4(EvaluateMainLight(mainLight, diffuse, specularGloss, normal, i.posWS, viewDir), 1.0);
#endif
				half3 directColor = EvaluateMainLight(mainLight, diffuse, specularGloss, normal, i.posWS, viewDir);

				// Compute direct contribution from additional lights.
				for (int lightIndex = 1; lightIndex < globalLightCount.x; ++lightIndex)
				{
					LightInput additionalLight;
					INITIALIZE_LIGHT(additionalLight, lightIndex);
					directColor += EvaluateOneLight(additionalLight, diffuse, specularGloss, normal, posWorld, viewDir);
				}

				half3 color = directColor + indirectDiffuse + _EmissionColor;
				UNITY_APPLY_FOG(i.fogCoord, color); 

#ifdef _ALPHABLEND_ON 
				return half4(color, alpha);
#else
				return half4(color, 1);
#endif
			};
			ENDCG
		}

		Pass
		{
			Name "SHADOW_CASTER"
			Tags { "Lightmode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Front

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			float4 _ShadowBias;

			#include "UnityCG.cginc"
			
			inline void ApplyLinearBias(half4 clipPos)
			{
#if defined(UNITY_REVERSED_Z)
				clipPos.z -= _ShadowBias.x;
#else
				clipPos.z += _ShadowBias.x;
#endif
			}

			float4 vert(float4 position : POSITION) : SV_POSITION
			{
				float4 clipPos = UnityObjectToClipPos(position);
				ApplyLinearBias(clipPos);
				return clipPos;
			}
				
			half4 frag() : SV_TARGET
			{
				return 0;
			}
			ENDCG
		}

		// Temporary Hack to overcome the globalrender pipeline crashing
		//  When baking lightmap due to lack of META pass
		Pass
		{
			NAME "Dummy1"
		}

		Pass
		{
			NAME "Dummy2"
		}
		
		// This pass it not used during regular rendering, only for lightmap baking.
		Pass
		{
			Name "META"
			Tags{ "LightMode" = "Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature EDITOR_VISUALIZATION

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
	Fallback "Standard (Specular setup)"
	CustomEditor "LDRenderPipelineMaterialEditor"
}
