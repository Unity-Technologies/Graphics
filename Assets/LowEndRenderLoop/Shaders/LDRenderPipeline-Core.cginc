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

sampler2D _ShadowMap;
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

inline half3 MetallicInput(v2f i, half3 albedo, out half4 specularGloss, out half oneMinusReflectivity)
{
	// TODO:
	specularGloss = half4(1, 1, 1, 1);
	oneMinusReflectivity = 1.0;
	return half3(1, 1, 1);
	//	half2 metalSmooth;
	//#ifdef _METALLICGLOSSMAP
	//	metalSmooth = tex2D(_MetallicGlossMap, i.uv01.xy).ra;
	//#else
	//	metalSmooth.r = _Metallic;
	//	metalSmooth.g = _Glossiness;
	//#endif
	//
	//	half3 diffuse = albedo.rgb * _Color.rgb;
	//	return DiffuseAndSpecularFromMetallic(diffuse, metalSmooth.r, specularGloss.rgb, oneMinusReflectivity);
}

inline half3 SpecularInput(v2f i, half3 albedo, out half4 specularGloss, out half oneMinusReflectivity)
{
	half3 diffuse = albedo.rgb * _Color.rgb;
	specularGloss = SpecularGloss(i.uv01.xy);

	return EnergyConservationBetweenDiffuseAndSpecular(diffuse, specularGloss.rgb, oneMinusReflectivity);
}

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
	half3 specular = specularGloss.rgb * lightColor * pow(NdotH, 64.0f) * specularGloss.a;
	return diffuse + specular;
}

inline half3 EvaluateMainLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float4 posWorld, half3 viewDir)
{
	int cascadeIndex = ComputeCascadeIndex(posWorld.w);
	float4 shadowCoord = mul(_WorldToShadow[cascadeIndex], float4(posWorld.xyz, 1.0));
	shadowCoord.z = saturate(shadowCoord.z);

#ifdef SHADOWS_FILTERING_VSM
	half shadowAttenuation = ShadowVSM(shadowCoord);
#elif defined(SHADOWS_FILTERING_PCF)
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
#if !GLOSSMAP
	o.viewDir.w = GRAZING_TERM;
#endif
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
#if _NORMALMAP
	half3 normalmap = UnpackNormal(tex2D(_BumpMap, i.uv01.xy));

	// glsl compiler will generate underperforming code by using a row-major pre multiplication matrix: mul(normalmap, i.tangentToWorld)
	// i.tangetToWorld was initialized as column-major in vs and here dot'ing individual for better performance. 
	// The code below is similar to post multiply: mul(i.tangentToWorld, normalmap)
	half3 normal = half3(dot(normalmap, i.tangentToWorld[0]), dot(normalmap, i.tangentToWorld[1]), dot(normalmap, i.tangentToWorld[2]));
#else
	half3 normal = normalize(i.normal);
#endif

	half4 diffuseAlbedo = tex2D(_MainTex, i.uv01.xy);
	float3 posWorld = i.posWS.xyz;
	half3 viewDir = i.viewDir.xyz;
	half alpha = diffuseAlbedo.a * _Color.a;

	half oneMinusReflectivity;
	half4 specularGloss;
	half3 diffuse = DIFFUSE_AND_SPECULAR_INPUT(i, diffuseAlbedo.rgb, specularGloss, oneMinusReflectivity);

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
	return half4(color, alpha);
};