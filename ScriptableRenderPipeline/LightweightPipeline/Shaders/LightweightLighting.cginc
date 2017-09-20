#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

UnityIndirect LightweightGI(float2 lightmapUV, half3 ambientColor, half3 reflectVec, half occlusion, half roughness)
{
    UnityIndirect o = (UnityIndirect)0;
#ifdef LIGHTMAP_ON
    o.diffuse = (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV)));
#endif

#if defined(_VERTEX_LIGHTS) || !defined(LIGHTMAP_ON)
    o.diffuse += ambientColor;
#endif
    o.diffuse *= occlusion;

#ifndef _GLOSSYREFLECTIONS_OFF
    // perceptualRoughness
    Unity_GlossyEnvironmentData g;
    g.roughness = roughness;
    g.reflUVW = reflectVec;
    o.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g) * occlusion;
#else
    o.specular = half3(0, 0, 0);
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

#if  !(defined(_SINGLE_POINT_LIGHT) || defined(_SINGLE_DIRECTIONAL_LIGHT))
    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);
#endif

    return half(lightAtten);
}

inline half ComputeLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
#ifdef _SINGLE_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    return 1.0;

#else
    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // TODO: Test separating dir lights into diff loop by sorting on the pipe and setting -1 on LightIndexMap.
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

#ifndef _SINGLE_POINT_LIGHT
    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);
#endif
    return half(lightAtten);

#endif // _SINGLE_DIRECTIONAL_LIGHT
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
    half3 specular = specularGloss.rgb * (pow(NdotH, _Shininess * 128.0) * specularGloss.a);
    return (diffuse + specular) * atten;
}

#endif
