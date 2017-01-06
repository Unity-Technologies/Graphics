// Shader targeted for LowEnd mobile devices. Single Pass Forward Rendering. Shader Model 2
//
// The parameters and inspector of the shader are the same as Standard shader,
// for easier experimentation.
Shader "RenderLoop/LowEnd"
{
	// Properties is just a copy of Standard.shader. Our example shader does not use all of them,
	// but the inspector UI expects all these to exist.
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0
		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap("Height Map", 2D) = "black" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		_DetailMask("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}
		[Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "PerformanceChecks" = "False" }
		LOD 300

		// Include forward (base + additive) pass from regular Standard shader.
		// They are not used by the scriptable render loop; only here so that
		// if we turn off our example loop, then regular forward rendering kicks in
		// and objects look just like with a Standard shader.
		UsePass "Standard/FORWARD"
		UsePass "Standard/FORWARD_DELTA"

		Pass
		{
			Tags { "LightMode" = "LowEndForwardBase" }

			// Use same blending / depth states as Standard shader
			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _METALLICGLOSSMAP
			#include "UnityCG.cginc"
			#include "UnityStandardBRDF.cginc"
			#include "UnityStandardUtils.cginc"

			#define DEBUG_CASCADES 0
			#define MAX_SHADOW_CASCADES 4
			#define MAX_LIGHTS 8

			// The variables are very similar to built-in unity_LightColor, unity_LightPosition,
			// unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
			// we use world space positions instead of view space.
			half4 globalLightColor[MAX_LIGHTS];
			float4 globalLightPos[MAX_LIGHTS];
			half4 globalLightSpotDir[MAX_LIGHTS];
			half4 globalLightAtten[MAX_LIGHTS];
			
			int4  globalLightCount; // x: pixelLightCount, y = totalLightCount (pixel + vert)

			// Global ambient/SH probe, similar to unity_SH* built-in variables.
			float4 globalSH[7];

			sampler2D _MainTex; half4 _MainTex_ST;
			sampler2D _MetallicGlossMap;
			sampler2D g_tShadowBuffer;
			half _Metallic;
			half _Glossiness;

			half4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
			half4 _PSSMDistances;

			struct LightInput
			{
				half4 pos;
				half4 color;
				half4 atten;
				half4 spotDir;
			};

			inline int ComputeCascadeIndex(half eyeZ)
			{
				// PSSMDistance is set to infinity for non active cascades. This way the comparison for unavailable cascades will always be zero. 
				half3 cascadeCompare = step(_PSSMDistances, half3(eyeZ, eyeZ, eyeZ));
				return dot(cascadeCompare, cascadeCompare);
			}

			inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half3 specularColor, half3 normal, float3 posWorld, half3 viewDir)
			{
				float3 posToLight = lightInput.pos.xyz;
				posToLight -= posWorld * lightInput.pos.w;

				float distanceSqr = max(dot(posToLight, posToLight), 0.001);
				float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

				float3 lightDir = normalize(posToLight); 
				float SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
				lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

				float cutoff = step(distanceSqr, lightInput.atten.w); 
				lightAtten *= cutoff;

				float NdotL = saturate(dot(normal, lightDir));
				
				half3 halfVec = normalize(lightDir + viewDir);
				half NdotH = saturate(dot(normal, halfVec));

				half3 lightColor = lightInput.color.rgb * lightAtten; 
				half3 diffuse = diffuseColor * lightColor * NdotL;
				half3 specular = specularColor * lightColor * pow(NdotH, 128.0f) * _Glossiness;
				return diffuse + specular;
			}

			inline half3 EvaluateOneLightAndShadow(LightInput lightInput, half3 diffuseColor, half3 specularColor, half3 normal, float4 posWorld, half3 viewDir)
			{
				int cascadeIndex = ComputeCascadeIndex(posWorld.w);
				float3 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorld.xyz, 1.0));
				shadowCoord.z = saturate(shadowCoord.z);

				// TODO: Apply proper bias considering NdotL
				half bias = 0.001;
				half shadowDepth = tex2D(g_tShadowBuffer, shadowCoord.xy).r;
				half shadowAttenuation = 1.0;

#if defined(UNITY_REVERSED_Z)
				shadowAttenuation = step(shadowDepth - bias, shadowCoord.z);
#else
				shadowAttenuation = step(shadowCoord.z - bias, shadowDepth);
#endif

#if DEBUG_CASCADES
				half4 cascadeColors[MAX_SHADOW_CASCADES] = { half4(1.0, 0.0, 0.0, 1.0), half4(0.0, 1.0, 0.0, 1.0),  half4(0.0, 0.0, 1.0, 1.0),  half4(1.0, 0.0, 1.0, 1.0) };
				return cascadeColors[cascadeIndex] * diffuseColor * max(shadowAttenuation, 0.5);
#else
				half3 color = EvaluateOneLight(lightInput, diffuseColor, specularColor, normal, posWorld, viewDir);
				return color * shadowAttenuation;
#endif
			}

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float3 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 posWSEyeZ : TEXCOORD1; // xyz: posWorld, w: eyeZ
				half3 normalWS : TEXCOORD2;
				half3 color : TEXCOORD3;
				float4 hpos : SV_POSITION;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.hpos = UnityObjectToClipPos(v.vertex);
				o.posWSEyeZ.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.posWSEyeZ.w = -UnityObjectToViewPos(v.vertex).z;

				o.normalWS = UnityObjectToWorldNormal(v.normal);
				
				half3 diffuseAndSpecularColor = half3(1.0, 1.0, 1.0);
				half3 viewDir = normalize(_WorldSpaceCameraPos - o.posWSEyeZ);
				
				for (int lightIndex = globalLightCount.x; lightIndex < globalLightCount.y; ++lightIndex)
				{
					LightInput lightInput;
					lightInput.pos = globalLightPos[lightIndex];
					lightInput.color = globalLightColor[lightIndex];
					lightInput.atten = globalLightAtten[lightIndex];
					lightInput.spotDir = globalLightSpotDir[lightIndex];
					o.color += EvaluateOneLight(lightInput, diffuseAndSpecularColor, diffuseAndSpecularColor, o.normalWS, o.posWSEyeZ.xyz, viewDir);
				}

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				i.normalWS = normalize(i.normalWS);
				float3 posWorld = i.posWSEyeZ.xyz;
				half3 viewDir = normalize(_WorldSpaceCameraPos - posWorld);

				half4 diffuseAlbedo = tex2D(_MainTex, i.uv);
				half2 metalSmooth;
#ifdef _METALLICGLOSSMAP
				metalSmooth = tex2D(_MetallicGlossMap, i.uv).ra; 
#else
				metalSmooth.r = _Metallic;
				metalSmooth.g = _Glossiness;
#endif

				half3 specColor;
				half oneMinuReflectivity;
				half3 diffuse = DiffuseAndSpecularFromMetallic(diffuseAlbedo.rgb, metalSmooth.x, specColor, oneMinuReflectivity);

				half3 color = i.color * diffuseAlbedo.rgb;

#if DEBUG_CASCADES
				LightInput lightInput;
				lightInput.pos = globalLightPos[0];
				lightInput.color = globalLightColor[0];
				lightInput.atten = globalLightAtten[0];
				lightInput.spotDir = globalLightSpotDir[0];
				color = EvaluateOneLightAndShadow(lightInput, diffuse, specColor, i.normalWS, i.posWSEyeZ, viewDir);
#else
				for (int lightIndex = 0; lightIndex < globalLightCount.x; ++lightIndex)
				{
					LightInput lightInput;
					lightInput.pos = globalLightPos[lightIndex];
					lightInput.color = globalLightColor[lightIndex];
					lightInput.atten = globalLightAtten[lightIndex];
					lightInput.spotDir = globalLightSpotDir[lightIndex];

					if (lightIndex == 0)
						color += EvaluateOneLightAndShadow(lightInput, diffuse, specColor, i.normalWS, i.posWSEyeZ, viewDir);
					else
						color += EvaluateOneLight(lightInput, diffuse, specColor, i.normalWS, posWorld, viewDir);
				}
#endif
				return half4(color, diffuseAlbedo.a);
			}
			ENDCG
		}

		Pass
		{
			Tags { "Lightmode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Front

			CGPROGRAM

			#pragma enable_d3d11_debug_symbols
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			float4 vert(float4 position : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(position);
			}

			half4 frag() : SV_TARGET
			{
				return 0;
			}
			ENDCG
		}
	}
	CustomEditor "StandardShaderGUI"
}
