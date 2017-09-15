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

void DefineSurface(LightweightVertexOutput i, inout SurfacePBR s);

half3 MetallicSetup(float2 uv, SurfacePBR s, out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
	smoothness = s.Smoothness;// metallicGloss.g;

							  // We'll need oneMinusReflectivity, so
							  //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
							  // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
							  //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
							  //                  = alpha - metallic * alpha
	half oneMinusDielectricSpec = _DieletricSpec.a;
	oneMinusReflectivity = oneMinusDielectricSpec - s.Metallic * oneMinusDielectricSpec;
	specular = lerp(_DieletricSpec.rgb, s.Albedo, s.Metallic);

	return s.Albedo * oneMinusReflectivity;
}

half3 SpecularSetup(float2 uv, SurfacePBR s, out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
	half4 specGloss = float4(s.Specular, s.Smoothness);

#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
	specGloss.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(specGloss.rgb);
#endif

	specular = specGloss.rgb;
	smoothness = specGloss.a;
	oneMinusReflectivity = 1.0h - SpecularReflectivity(specular);
	return s.Albedo * (half3(1, 1, 1) - specular);
}

half4 LightweightFragmentPBR(LightweightVertexOutput i) : SV_Target
{
	SurfacePBR s = InitializeSurfacePBR();
	DefineSurface(i, s);

    float2 uv = i.uv01.xy;
    float2 lightmapUV = i.uv01.zw;

    half3 specColor;
    half smoothness;
    half oneMinusReflectivity;
#ifdef _METALLIC_SETUP
    half3 diffColor = MetallicSetup(uv, s, specColor, smoothness, oneMinusReflectivity);
#else
    half3 diffColor = SpecularSetup(uv, s, specColor, smoothness, oneMinusReflectivity);
#endif

    diffColor = PreMultiplyAlpha(diffColor, s.Alpha, oneMinusReflectivity, /*out*/ s.Alpha);

    // Roughness is (1.0 - smoothness)²
    half perceptualRoughness = 1.0h - smoothness;

	// TODO - Actually handle normal
    half3 normal;
    CalculateNormal(s.Normal, i, normal);

    // TODO: shader keyword for occlusion
    // TODO: Reflection Probe blend support.
    half3 reflectVec = reflect(-i.viewDir.xyz, normal);

    UnityIndirect indirectLight = LightweightGI(lightmapUV, i.fogCoord.yzw, reflectVec, s.Occlusion, perceptualRoughness);

    // PBS
    // grazingTerm = F90
    half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
    half fresnelTerm = Pow4(1.0 - saturate(dot(normal, i.viewDir.xyz)));
    half3 color = LightweightBRDFIndirect(diffColor, specColor, indirectLight, perceptualRoughness * perceptualRoughness, grazingTerm, fresnelTerm);
    half3 lightDirection;

#ifndef _MULTIPLE_LIGHTS
    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);

#ifdef _SHADOWS
    lightAtten *= ComputeShadowAttenuation(i.normal, i.posWS, _ShadowLightDirection.xyz);
#endif

    half NdotL = saturate(dot(normal, lightDirection));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, normal, lightDirection, i.viewDir.xyz) * radiance;
#else

#ifdef _SHADOWS
    half shadowAttenuation = ComputeShadowAttenuation(i.normal, i.posWS, _ShadowLightDirection.xyz);
#endif
    int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        int lightIndex = unity_4LightIndices0[lightIter];
        INITIALIZE_LIGHT(light, lightIndex);
        half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);
#ifdef _SHADOWS
        lightAtten *= max(shadowAttenuation, half(lightIndex != _ShadowData.x));
#endif
        half NdotL = saturate(dot(normal, lightDirection));
        half3 radiance = light.color * (lightAtten * NdotL);

        color += LightweightBDRF(diffColor, specColor, oneMinusReflectivity, perceptualRoughness, normal, lightDirection, i.viewDir.xyz) * radiance;
    }
#endif

	color += s.Emission;
    UNITY_APPLY_FOG(i.fogCoord, color);
    return OutputColor(color, s.Alpha);
}

#endif