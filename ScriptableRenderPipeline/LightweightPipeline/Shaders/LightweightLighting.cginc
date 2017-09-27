#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

#define PI 3.14159265359f

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightBDRF(BRDFData brdfData, half roughness2, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = Unity_SafeNormalize(lightDirection + viewDir);

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

half3 LightweightBRDFIndirect(BRDFData brdfData, UnityIndirect indirect, half roughness2, half fresnelTerm)
{
    half3 c = indirect.diffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    c += surfaceReduction * indirect.specular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

UnityIndirect LightweightGI(float2 lightmapUV, half3 ambientColor, half3 normalWorld, half3 reflectVec, half occlusion, half perceptualRoughness)
{
    UnityIndirect o = (UnityIndirect)0;
#ifdef LIGHTMAP_ON
    ambientColor = (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV)));
#else
    ambientColor += SHEvalLinearL0L1(half4(normalWorld, 1.0));
    ambientColor = max(half3(0.0, 0.0, 0.0), ambientColor);
#endif

    o.diffuse = ambientColor * occlusion;

#ifndef _GLOSSYREFLECTIONS_OFF
    Unity_GlossyEnvironmentData g;
    g.roughness = perceptualRoughness;
    g.reflUVW = reflectVec;
    o.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g) * occlusion;
#else
    o.specular = _GlossyEnvironmentColor * occlusion;
#endif

    return o;
}

inline half ComputeLightAttenuationVertex(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos - worldPos;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    //// attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    //// attenuationParams.w = lightRange * lightRange
    //// TODO: we can precompute 1.0 / (attenuationParams.w * 0.64 - attenuationParams.w)
    //// falloff is computed from 80% light range squared
    float lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);

    return half(lightAtten);
}

inline half ComputeLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;

    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

#ifdef _ATTENUATION_TEXTURE
    float u = (distanceSqr * attenuationParams.z) / attenuationParams.w;
    float lightAtten = tex2D(_AttenuationTexture, float2(u, 0.0)).a;
#else
    //// attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    //// attenuationParams.w = lightRange * lightRange
    //// TODO: we can precompute 1.0 / (attenuationParams.w * 0.64 - attenuationParams.w)
    //// falloff is computed from 80% light range squared
    float lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));
    float falloff = saturate((distanceSqr - attenuationParams.w) / (attenuationParams.w * 0.64 - attenuationParams.w));
    lightAtten *= half(falloff);
#endif

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);
    return half(lightAtten);
}

inline half ComputeMainLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
#ifdef _MAIN_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    return 1.0;
#else
    return ComputeLightAttenuation(lightInput, normal, worldPos, lightDirection);
#endif
}

inline half3 LightingLambert(half3 diffuseColor, half3 lightDir, half3 normal, half atten)
{
    half NdotL = saturate(dot(normal, lightDir));
    return diffuseColor * (NdotL * atten);
}

inline half3 LightingBlinnPhong(half3 diffuseColor, half4 specularGloss, half3 lightDir, half3 normal, half3 viewDir, half atten)
{
    half NdotL = saturate(dot(normal, lightDir));
    half3 diffuse = diffuseColor * NdotL;

    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specular = specularGloss.rgb * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return (diffuse + specular) * atten;
}

#endif
