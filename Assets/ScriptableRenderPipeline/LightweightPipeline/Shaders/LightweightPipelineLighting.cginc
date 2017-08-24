#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

UnityIndirect LightweightGI(float2 lightmapUV, half3 ambientColor, half3 reflectVec, half occlusion, half roughness)
{
    UnityIndirect o = (UnityIndirect)0;
#ifdef LIGHTMAP_ON
    o.diffuse = (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV)));
#endif

#if defined(_VERTEX_LIGHTS) || defined(_LIGHT_PROBES_ON)
    o.diffuse += ambientColor;
#endif
    o.diffuse *= occlusion;

    // perceptualRoughness
    Unity_GlossyEnvironmentData g;
    g.roughness = roughness;
    g.reflUVW = reflectVec;
    o.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g) * occlusion;

    return o;
}

inline half3 EvaluateDirectionalLight(half3 diffuseColor, half4 specularGloss, half3 normal, half3 lightDir, half3 viewDir)
{
    half NdotL = saturate(dot(normal, lightDir));
    half3 diffuse = diffuseColor * NdotL;

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specular = specularGloss.rgb * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return diffuse + specular;
#else
    return diffuse;
#endif
}

inline half3 EvaluateOneLight(LightInput lightInput, half3 diffuseColor, half4 specularGloss, half3 normal, float3 posWorld, half3 viewDir)
{
    float3 posToLight = lightInput.pos.xyz;
    posToLight -= posWorld * lightInput.pos.w;

    float distanceSqr = max(dot(posToLight, posToLight), 0.001);
    float lightAtten = 1.0 / (1.0 + distanceSqr * lightInput.atten.z);

    float3 lightDir = posToLight * rsqrt(distanceSqr);
    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDir));
    lightAtten *= saturate((SdotL - lightInput.atten.x) / lightInput.atten.y);

    half cutoff = step(distanceSqr, lightInput.atten.w);
    lightAtten *= cutoff;

    half NdotL = saturate(dot(normal, lightDir));

    half3 lightColor = lightInput.color.rgb * lightAtten;
    half3 diffuse = diffuseColor * lightColor * NdotL;

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specular = specularGloss.rgb * lightColor * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return diffuse + specular;
#else
    return diffuse;
#endif
}

#endif
