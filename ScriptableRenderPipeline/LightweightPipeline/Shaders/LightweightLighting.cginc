#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

#include "LightweightCore.cginc"
#include "LightweightShadows.cginc"

#define PI 3.14159265359f
#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

#define MAX_VISIBLE_LIGHTS 16

#ifndef UNITY_SPECCUBE_LOD_STEPS
#define UNITY_SPECCUBE_LOD_STEPS 6
#endif

#ifdef NO_ADDITIONAL_LIGHTS
#undef _ADDITIONAL_LIGHTS
#endif

// If lightmap is not defined than we evaluate GI (ambient + probes) from SH
// We might do it fully or partially in vertex to save shader ALU
#if !defined(LIGHTMAP_ON)
    #if SHADER_TARGET < 30
        // Evaluates SH fully in vertex
        #define EVALUATE_SH_VERTEX
    #else
        // Evaluates L2 SH in vertex and L0L1 in pixel
        #define EVALUATE_SH_MIXED
    #endif
#endif

// Main light initialized without indexing
#define INITIALIZE_MAIN_LIGHT(light) \
    light.pos = _MainLightPosition; \
    light.color = _MainLightColor; \
    light.atten = _MainLightAttenuationParams; \
    light.spotDir = _MainLightSpotDir;

// Indexing might have a performance hit for old mobile hardware
#define INITIALIZE_LIGHT(light, i) \
    half4 indices = (i < 4) ? unity_4LightIndices0 : unity_4LightIndices1; \
    int index = (i < 4) ? i : i - 4; \
    int lightIndex = indices[index]; \
    light.pos = _AdditionalLightPosition[lightIndex]; \
    light.color = _AdditionalLightColor[lightIndex]; \
    light.atten = _AdditionalLightAttenuationParams[lightIndex]; \
    light.spotDir = _AdditionalLightSpotDir[lightIndex]

CBUFFER_START(_PerObject)
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;
half4 unity_4LightIndices1;
CBUFFER_END

CBUFFER_START(_PerCamera)
sampler2D _MainLightCookie;
float4 _MainLightPosition;
half4 _MainLightColor;
float4 _MainLightAttenuationParams;
half4 _MainLightSpotDir;
float4x4 _WorldToLight;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
float4 _AdditionalLightAttenuationParams[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
CBUFFER_END

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
sampler2D _AttenuationTexture;
CBUFFER_END

// Must match Lightweigth ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normal;
    half3 emission;
    half  occlusion;
    half  alpha;
};

struct LightInput
{
    float4 pos;
    half4 color;
    float4 atten;
    half4 spotDir;
};

struct BRDFData
{
    half3 diffuse;
    half3 specular;
    half perceptualRoughness;
    half roughness;
    half grazingTerm;
};

half ReflectivitySpecular(half3 specular)
{
#if (SHADER_TARGET < 30)
    // SM2.0: instruction count limitation
    return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
#else
    return max(max(specular.r, specular.g), specular.b);
#endif
}

half OneMinusReflectivityMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDieletricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDieletricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out BRDFData outBRDFData)
{
#ifdef _SPECULAR_SETUP
    half reflectivity = ReflectivitySpecular(specular);
    half oneMinusReflectivity = 1.0 - reflectivity;

    outBRDFData.diffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    outBRDFData.specular = specular;
#else

    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = 1.0 - oneMinusReflectivity;

    outBRDFData.diffuse = albedo * oneMinusReflectivity;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);
#endif

    outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
    outBRDFData.perceptualRoughness = 1.0h - smoothness;
    outBRDFData.roughness = outBRDFData.perceptualRoughness * outBRDFData.perceptualRoughness;

#ifdef _ALPHAPREMULTIPLY_ON
    outBRDFData.diffuse *= alpha;
    alpha = alpha * oneMinusReflectivity + reflectivity;
#endif
}

half3 GlossyEnvironment(half3 reflectVector, half perceptualRoughness)
{
#if !defined(_GLOSSYREFLECTIONS_OFF)
    half roughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
    half mip = roughness * UNITY_SPECCUBE_LOD_STEPS;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVector, mip);
    return DecodeHDR(rgbm, unity_SpecCube0_HDR);
#endif

    return _GlossyEnvironmentColor;
}

half3 LightweightEnvironmentBRDF(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half roughness2, half fresnelTerm)
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    c += surfaceReduction * indirectSpecular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightBDRF(BRDFData brdfData, half roughness2, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(normal, halfDir));
    half LoH = saturate(dot(lightDirection, halfDir));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    half d = NoH * NoH * (roughness2 - 1.h) + 1.00001h;

    half LoH2 = LoH * LoH;
    half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * (brdfData.roughness + 0.5h) * 4);

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4h;
#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
    return color;
#else
    return brdfData.diffuse;
#endif
}

half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
}

half3 LightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half4 specularGloss, half shininess)
{
    half3 halfVec = SafeNormalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specularReflection = specularGloss.rgb * pow(NdotH, shininess) * specularGloss.a;
    return lightColor * specularReflection;
}

half CookieAttenuation(float3 worldPos)
{
#ifdef _MAIN_LIGHT_COOKIE
    #ifdef _MAIN_DIRECTIONAL_LIGHT
        float2 cookieUV = mul(_WorldToLight, float4(worldPos, 1.0)).xy;
        return tex2D(_MainLightCookie, cookieUV).a;
    #elif defined(_MAIN_SPOT_LIGHT)
        float3 projPos = mul(_WorldToLight, float4(worldPos, 1.0)).xyz;
        float2 cookieUV = projPos.xy / projPos.w + 0.5;
        return tex2D(_MainLightCookie, cookieUV).a;
    #endif // POINT LIGHT cookie not supported
#endif

    return 1;
}

half SpotAttenuation(half3 spotDirection, half3 lightDirection, float4 attenuationParams)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);

    // attenuationParams.x = invAngleRange
    // attenuationParams.y = (-cosOuterAngle  invAngleRange)
    return saturate(SdotL * attenuationParams.x + attenuationParams.y);
}

// In per-vertex falloff there's no smooth falloff to light range. A hard cut will be noticed
inline half ComputeVertexLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    // attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    // attenuationParams.w = lightRange * lightRange
    half lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));
    lightAtten *= SpotAttenuation(lightInput.spotDir.xyz, lightDirection, attenuationParams);
    return lightAtten;
}

// In per-pixel falloff attenuation smoothly decreases to light range.
inline half ComputePixelLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    float u = (distanceSqr * attenuationParams.z) / attenuationParams.w;
    half lightAtten = tex2D(_AttenuationTexture, float2(u, 0.0)).a;
    lightAtten *= SpotAttenuation(lightInput.spotDir.xyz, lightDirection, attenuationParams);
    return lightAtten;
}

inline half ComputeMainLightAttenuation(LightInput lightInput, half3 normalWS, float3 positionWS, out half3 lightDirection)
{
#ifdef _MAIN_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    half attenuation = 1.0;
#else
    half attenuation = ComputePixelLightAttenuation(lightInput, normalWS, positionWS, lightDirection);
#endif

    // Cookies and shadows are only computed for main light
    attenuation *= CookieAttenuation(positionWS);
    attenuation *= LIGHTWEIGHT_SHADOW_ATTENUATION(positionWS, normalWS, lightDirection);
    return attenuation;
}

half3 VertexLighting(float positionWS, half3 normalWS)
{
    half3 vertexLightColor = half3(0.0, 0.0, 0.0);

#if defined(_VERTEX_LIGHTS)
    int vertexLightStart = _AdditionalLightCount.x;
    int vertexLightEnd = min(_AdditionalLightCount.y, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);

        half3 lightDirection;
        half atten = ComputeVertexLightAttenuation(light, normalWS, positionWS, lightDirection);
        half lightColor = light.color * atten;
        vertexLightColor += LightingLambert(lightColor, lightDirection, normalWS);
    }
#endif

    return vertexLightColor;
}

half4 LightweightFragmentPBR(float3 positionWS, half3 normalWS, half3 viewDirectionWS, half fogFactor, half3 indirectDiffuse, half3 vertexLighting, half3 albedo, half metallic, half3 specular, half smoothness, half occlusion, half3 emission, half alpha)
{
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    // TODO: When refactoring shadows remove dependency from vertex normal
    half3 vertexNormal = normalWS;
    half3 reflectVec = reflect(-viewDirectionWS, normalWS);
    half roughness2 = brdfData.roughness * brdfData.roughness;
    indirectDiffuse *= occlusion;
    half3 indirectSpecular = GlossyEnvironment(reflectVec, brdfData.perceptualRoughness) * occlusion;

    // PBS
    half fresnelTerm = _Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));
    half3 color = LightweightEnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, roughness2, fresnelTerm);
    half3 lightDirectionWS;

    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeMainLightAttenuation(light, normalWS, positionWS, lightDirectionWS);
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    color += vertexLighting * brdfData.diffuse;

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);
        half lightAtten = ComputePixelLightAttenuation(light, normalWS, positionWS, lightDirectionWS);

        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half3 radiance = light.color * (lightAtten * NdotL);
        color += LightweightBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    }
#endif

    color += emission;

    // Computes fog factor per-vertex
    ApplyFog(color, fogFactor);
    return OutputColor(color, alpha);
}

half4 LightweightFragmentLambert(float3 positionWS, half3 normalWS, half3 viewDirectionWS, half fogFactor, half3 diffuseGI, half3 diffuse, half3 emission, half alpha)
{
    half3 lightDirection;
    half3 diffuseColor = diffuseGI;

    LightInput mainLight;
    INITIALIZE_MAIN_LIGHT(mainLight);
    half lightAtten = ComputeMainLightAttenuation(mainLight, normalWS, positionWS, lightDirection);
    half3 lightColor = mainLight.color * lightAtten;
    diffuseColor += LightingLambert(lightColor, lightDirection, normalWS);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput lightData;
        INITIALIZE_LIGHT(lightData, lightIter);
        lightAtten = ComputePixelLightAttenuation(lightData, normalWS, positionWS, lightDirection);
        lightColor = lightData.color * lightAtten;

        diffuseColor += LightingLambert(lightColor, lightDirection, normalWS);
    }
#endif // _ADDITIONAL_LIGHTS

    half3 finalColor = diffuseColor * diffuse + emission;

    // Computes Fog Factor per vextex
    ApplyFog(finalColor, fogFactor);
    return OutputColor(finalColor, alpha);
}

half4 LightweightFragmentBlinnPhong(float3 positionWS, half3 normalWS, half3 viewDirectionWS, half fogFactor, half3 diffuseGI, half3 diffuse, half4 specularGloss, half shininess, half3 emission, half alpha)
{
    half3 lightDirection;
    half3 diffuseColor = diffuseGI;
    half3 specularColor;

    LightInput mainLight;
    INITIALIZE_MAIN_LIGHT(mainLight);
    half lightAtten = ComputeMainLightAttenuation(mainLight, normalWS, positionWS, lightDirection);

    half3 lightColor = mainLight.color * lightAtten;
    diffuseColor += LightingLambert(lightColor, lightDirection, normalWS);
    specularColor = LightingSpecular(lightColor, lightDirection, normalWS, viewDirectionWS, specularGloss, shininess);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput lightData;
        INITIALIZE_LIGHT(lightData, lightIter);
        lightAtten = ComputePixelLightAttenuation(lightData, normalWS, positionWS, lightDirection);
        lightColor = lightData.color * lightAtten;

        diffuseColor += LightingLambert(lightColor, lightDirection, normalWS);
        specularColor += LightingSpecular(lightColor, lightDirection, normalWS, viewDirectionWS, specularGloss, shininess);
    }
#endif // _ADDITIONAL_LIGHTS

    half3 finalColor = diffuseColor * diffuse + emission;
    finalColor += specularColor;

    // Computes Fog Factor per vextex
    ApplyFog(finalColor, fogFactor);
    return OutputColor(finalColor, alpha);
}
#endif
