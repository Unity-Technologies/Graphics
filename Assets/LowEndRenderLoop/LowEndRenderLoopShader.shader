// Shader targeted for LowEnd mobile devices. Single Pass Forward Rendering. Shader Model 2
//
// The parameters and inspector of the shader are the same as Standard shader,
// for easier experimentation.
Shader "RenderLoop/LowEnd"
{
	// Properties is just a copy of Standard (Specular Setup).shader. Our example shader does not use all of them,
	// but the inspector UI expects all these to exist.
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0
		[Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}
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

		// Blending state
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
		//UsePass "Standard (Specular setup)/FORWARD"
		//UsePass "Standard (Specular setup)/FORWARD_DELTA"

		Pass
		{
			Name "SINGLE_PASS_FORWARD"
			Tags { "LightMode" = "LowEndForwardBase" }

			// Use same blending / depth states as Standard shader
			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _EMISSION
			#pragma shader_feature LIGHTMAP_ON 

			#pragma multi_compile_fog
			#pragma only_renderers d3d9 d3d11 d3d11_9x glcore gles gles3
			
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
				light.spotDir = globalLightSpotDir[lightIndex];

			#define FRESNEL_TERM(normal, viewDir) Pow4(1.0 - saturate(dot(normal, viewDir)))

			// TODO: Add metallic or specular reflectivity
			#define GRAZING_TERM _Glossiness 

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

			sampler2D g_tShadowBuffer;

			half4x4 _WorldToShadow[MAX_SHADOW_CASCADES];
			half4 _PSSMDistances;

			struct LightInput
			{
				half4 pos;
				half4 color;
				half4 atten;
				half4 spotDir;
			};

			// Evaluate 2nd order spherical harmonics, given normalized world space direction.
			// Similar to ShadeSH9 in UnityCG.cginc
			half3 EvaluateSH(half3 n)
			{
				half3 res;
				half4 normal = half4(n, 1);
				// Linear (L1) + constant (L0) polynomial terms
				res.r = dot(globalSH[0], normal);
				res.g = dot(globalSH[1], normal);
				res.b = dot(globalSH[2], normal);
				// 4 of the quadratic (L2) polynomials
				half4 vB = normal.xyzz * normal.yzzx;
				res.r += dot(globalSH[3], vB);
				res.g += dot(globalSH[4], vB);
				res.b += dot(globalSH[5], vB);
				// Final (5th) quadratic (L2) polynomial
				half vC = normal.x*normal.x - normal.y*normal.y;
				res += globalSH[6].rgb * vC;
				return res;
			}

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
				half3 specular = specularColor * lightColor * pow(NdotH, 128.0f) * _Glossiness;
				return diffuse + specular;
			}

			inline half3 EvaluateMainLight(LightInput lightInput, half3 diffuseColor, half3 specularColor, half3 normal, float4 posWorld, half3 viewDir)
			{
				int cascadeIndex = ComputeCascadeIndex(posWorld.w);
				float3 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorld.xyz, 1.0));
				shadowCoord.z = saturate(shadowCoord.z);

				// TODO: Apply proper bias considering NdotL
				half bias = 0.0005;
				half shadowDepth = tex2D(g_tShadowBuffer, shadowCoord.xy).r;
				half shadowAttenuation = 1.0;
				
#if defined(UNITY_REVERSED_Z)
				shadowAttenuation = step(shadowDepth - bias, shadowCoord.z);
#else
				shadowAttenuation = step(shadowCoord.z - bias, shadowDepth);
#endif

#if DEBUG_CASCADES
				half3 cascadeColors[MAX_SHADOW_CASCADES] = { half3(1.0, 0.0, 0.0), half3(0.0, 1.0, 0.0),  half3(0.0, 0.0, 1.0),  half3(1.0, 0.0, 1.0) };
				return cascadeColors[cascadeIndex] * diffuseColor * max(shadowAttenuation, 0.5); 
#endif

				half3 color = EvaluateOneLight(lightInput, diffuseColor, specularColor, normal, posWorld, viewDir);
				return color * shadowAttenuation;
			}

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
				half4 normalWS : TEXCOORD2;// xyz: normal, w: fresnel term
				half3 tangentWS : TEXCOORD3;
				half3 binormalWS : TEXCOORD4;
				half4 viewDir : TEXCOORD5; // xyz: viewDir, w: grazingTerm;
				UNITY_FOG_COORDS_PACKED(6, half4) // x: fogCoord, yzw: vertexColor
				float4 hpos : SV_POSITION;
			}; 

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
#if !GLOSSMAP
				o.viewDir.w = GRAZING_TERM;
#endif
				o.normalWS.xyz = UnityObjectToWorldNormal(v.normal);
				o.normalWS.w = FRESNEL_TERM(o.normalWS.xyz, o.viewDir.xyz);

#if _NORMALMAP
				half sign = v.tangent.w * unity_WorldTransformParams.w;
				o.tangentWS = UnityObjectToWorldDir(v.tangent);
				o.binormalWS = cross(o.normalWS.xyz, o.tangentWS) * v.tangent.w;
#endif

				half3 diffuseAndSpecularColor = half3(1.0, 1.0, 1.0);
				for (int lightIndex = globalLightCount.x; lightIndex < globalLightCount.y; ++lightIndex)
				{
					LightInput lightInput;
					INITIALIZE_LIGHT(lightInput, lightIndex);
					o.fogCoord.yzw += EvaluateOneLight(lightInput, diffuseAndSpecularColor, diffuseAndSpecularColor, o.normalWS, o.posWS.xyz, o.viewDir.xyz);
				}

				o.fogCoord.x = 1.0;
				UNITY_TRANSFER_FOG(o, o.hpos);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
#if _NORMALMAP
				half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

				// TODO: This will generate unoptimized code from the glsl compiler. Store the transpose matrix and compute dot manually
				half3x3 tangentToWorld = half3x3(i.tangentWS, i.binormalWS, i.normalWS.xyx); 
				half3 normal = mul(normalmap, tangentToWorld);
#else
				half3 normal = normalize(i.normalWS.xyz);
#endif
				float3 posWorld = i.posWS.xyz;
				half3 viewDir = i.viewDir.xyz;

				half4 diffuseAlbedo = tex2D(_MainTex, i.uv01.xy);
				half3 diffuse = diffuseAlbedo.rgb * _Color.rgb;
				half alpha = diffuseAlbedo.a * _Color.a;

				half4 specGloss = SpecularGloss(i.uv01.xy);
				half3 specular = specGloss.rgb;
				half smoothness = specGloss.a;

				half oneMinusReflectivity;
				diffuse = EnergyConservationBetweenDiffuseAndSpecular(diffuse, specular, /*out*/ oneMinusReflectivity);
				
				// Indirect Light Contribution
				UnityIndirect giIndirect;
#ifdef LIGHTMAP_ON
				giIndirect.diffuse = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw));
#else
				giIndirect.diffuse = half3(0, 0, 0);
#endif
				giIndirect.specular = half3(0, 0, 0);
				half3 indirectColor = BRDF3_Indirect(diffuse, specular, giIndirect, i.posWS.w, i.normalWS.w);

				half3 directColor = i.fogCoord.yzw * diffuseAlbedo.rgb;

				// Compute direct contribution from main directional light.
				// Only a single directional shadow caster is supported.
				LightInput mainLight;
				INITIALIZE_LIGHT(mainLight, 0)

#if DEBUG_CASCADES
				return half4(EvaluateMainLight(mainLight, diffuse, specular, normal, i.posWS, viewDir), 1.0);
#endif
				directColor += EvaluateMainLight(mainLight, diffuse, specular, normal, i.posWS, viewDir); 

				// Compute direct contribution from additional lights.
				for (int lightIndex = 1; lightIndex < globalLightCount.x; ++lightIndex)
				{
					LightInput additionalLight;
					INITIALIZE_LIGHT(additionalLight, lightIndex);
					directColor += EvaluateOneLight(additionalLight, diffuse, specular, normal, posWorld, viewDir);
				}

				half3 color = directColor + indirectColor + _EmissionColor;
				UNITY_APPLY_FOG(i.fogCoord, color);
				return half4(color, diffuseAlbedo.a);
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
	Fallback "RenderLoop/Error"
	CustomEditor "StandardShaderGUI"
}
