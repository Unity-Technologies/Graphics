#ifndef LIGHTWEIGHT_PBR_INCLUDED
#define LIGHTWEIGHT_PBR_INCLUDED

#include "UnityStandardInput.cginc"
#include "LightweightShadows.cginc"
#include "LightweightBRDF.cginc"
#include "LightweightCore.cginc"

struct SurfacePBR
{
	float3 Albedo;      // diffuse color
	float3 Specular;    // specular color
	float Metallic;		// metallic
	float3 Normal;      // tangent space normal, if written
	half3 Emission;
	half Smoothness;    // 0=rough, 1=smooth
	half Occlusion;     // occlusion (default 1)
	float Alpha;        // alpha for transparencies
};

SurfacePBR InitializeSurfacePBR()
{
	SurfacePBR s;
	s.Albedo = float3(0.5, 0.5, 0.5);
	s.Specular = float3(0, 0, 0);
	s.Metallic = 0;
	s.Normal = float3(.5, .5, 1);
	s.Emission = 0;
	s.Smoothness = 0;
	s.Occlusion = 1;
	s.Alpha = 1;
	return s;
}

void DefineSurface(VertOutput i, inout SurfacePBR o);

half3 MetallicSetup(float2 uv, float3 albedo, float metallic, out half3 specular, out half oneMinusReflectivity)
{
	// We'll need oneMinusReflectivity, so
	//   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
	// store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
	//   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
	//                  = alpha - metallic * alpha
	half oneMinusDielectricSpec = _DieletricSpec.a;
	oneMinusReflectivity = oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
	specular = lerp(_DieletricSpec.rgb, albedo, metallic);

	return albedo * oneMinusReflectivity;
}

half3 SpecularSetup(float2 uv, half3 albedo, half3 specColor, out half3 specular, out half oneMinusReflectivity)
{
	specular = specColor;
	oneMinusReflectivity = 1.0h - SpecularReflectivity(specColor);
	return albedo * (half3(1, 1, 1) - specColor);
}

half4 FragmentLightingPBR(float4 uv, float3 posWS, float3 normal, float3 tangent, float3 binormal, float3 viewDir, float4 fogCoord,
	float3 albedo, float metallic, float3 specular, float smoothness, float3 normalMap, float occlusion, float3 emission, float alpha)
{
	float2 lightmapUV = uv.zw;

	half3 specColor;
	half oneMinusReflectivity;
#ifdef _METALLIC_SETUP
	half3 diffColor = MetallicSetup(uv.xy, albedo, metallic, specColor, oneMinusReflectivity);
#else
	half3 diffColor = SpecularSetup(uv.xy, albedo, specular, specColor, oneMinusReflectivity);
#endif

	diffColor = PreMultiplyAlpha(diffColor, alpha, oneMinusReflectivity, /*out*/ alpha);

	// Roughness is (1.0 - smoothness)²
	half perceptualRoughness = 1.0h - smoothness;

	half3 norm;
#if _NORMALMAP
	half3 tangentToWorld0 = half3(tangent.x, binormal.x, normal.x);
	half3 tangentToWorld1 = half3(tangent.y, binormal.y, normal.y);
	half3 tangentToWorld2 = half3(tangent.z, binormal.z, normal.z);
	norm = normalize(half3(dot(normalMap, tangentToWorld0), dot(normalMap, tangentToWorld1), dot(normalMap, tangentToWorld2)));
#else
	norm = normalize(normal);
#endif

	// TODO: shader keyword for occlusion
	// TODO: Reflection Probe blend support.
	half3 reflectVec = reflect(viewDir.xyz, norm);

	UnityIndirect indirectLight = LightweightGI(lightmapUV, fogCoord.yzw, reflectVec, occlusion, perceptualRoughness);

	// PBS
	// grazingTerm = F90
	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	half fresnelTerm = Pow4(1.0 - saturate(dot(norm, viewDir.xyz)));
	half3 color = LightweightBRDFIndirect(diffColor, specColor, indirectLight, perceptualRoughness * perceptualRoughness, grazingTerm, fresnelTerm);
	half3 lightDirection;

#ifdef _SHADOWS
#if _NORMALMAP
	half3 vertexNormal = half3(tangentToWorld0.z, tangentToWorld1.z, tangentToWorld2.z);
#else
	half3 vertexNormal = normal;
#endif
#endif

#ifndef _MULTIPLE_LIGHTS
	LightInput light;
	INITIALIZE_MAIN_LIGHT(light);
	half lightAtten = ComputeLightAttenuation(light, normal, posWS.xyz, lightDirection);

#ifdef _SHADOWS
	lightAtten *= ComputeShadowAttenuation(vertexNormal, posWS, _ShadowLightDirection.xyz);
#endif

	half NdotL = saturate(dot(norm, lightDirection));
	half3 radiance = light.color * (lightAtten * NdotL);
	color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, norm, lightDirection, viewDir.xyz) * radiance;
#else

#ifdef _SHADOWS
	half shadowAttenuation = ComputeShadowAttenuation(vertexNormal, posWS, _ShadowLightDirection.xyz);
#endif
	int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
	for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
	{
		LightInput light;
		int lightIndex = unity_4LightIndices0[lightIter];
		INITIALIZE_LIGHT(light, lightIndex);
		half lightAtten = ComputeLightAttenuation(light, norm, posWS.xyz, lightDirection);
#ifdef _SHADOWS
		lightAtten *= max(shadowAttenuation, half(lightIndex != _ShadowData.x));
#endif
		half NdotL = saturate(dot(norm, lightDirection));
		half3 radiance = light.color * (lightAtten * NdotL);

		color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, norm, lightDirection, viewDir.xyz) * radiance;
	}
#endif

	color += emission;
	UNITY_APPLY_FOG(fogCoord, color);
	return OutputColor(color, alpha);
}

half4 LightweightFragmentPBR(VertOutput i) : SV_Target
{
	SurfacePBR o = InitializeSurfacePBR();
	DefineSurface(i, o);
	return FragmentLightingPBR(i.meshUV0, i.posWS, i.normal, i.tangent, i.binormal, i.viewDir, i.fogCoord, 
							   o.Albedo, o.Metallic, o.Specular, o.Smoothness, o.Normal, o.Occlusion, o.Emission, o.Alpha);
}

#endif