#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LightweightCore.cginc"

LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
    o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.posWS.xyz = worldPos;

    half3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    o.viewDir.xyz = viewDir;

    half3 normal = normalize(UnityObjectToWorldNormal(v.normal));

#if _NORMALMAP
    half sign = v.tangent.w * unity_WorldTransformParams.w;
    half3 tangent = UnityObjectToWorldDir(v.tangent);
    half3 binormal = cross(normal, tangent) * sign;

    // Initialize tangetToWorld in column-major to benefit from better glsl matrix multiplication code
    o.tangentToWorld0 = half3(tangent.x, binormal.x, normal.x);
    o.tangentToWorld1 = half3(tangent.y, binormal.y, normal.y);
    o.tangentToWorld2 = half3(tangent.z, binormal.z, normal.z);
#else
    o.normal = normal;
#endif

    // TODO: change to only support point lights per vertex. This will greatly simplify shader ALU
//#if defined(_VERTEX_LIGHTS) && defined(_MULTIPLE_LIGHTS)
//    half3 diffuse = half3(1.0, 1.0, 1.0);
//    // pixel lights shaded = min(pixelLights, perObjectLights)
//    // vertex lights shaded = min(vertexLights, perObjectLights) - pixel lights shaded
//    // Therefore vertexStartIndex = pixelLightCount;  vertexEndIndex = min(vertexLights, perObjectLights)
//    int vertexLightStart = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
//    int vertexLightEnd = min(globalLightCount.y, unity_LightIndicesOffsetAndCount.y);
//    for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
//    {
//        int lightIndex = unity_4LightIndices0[lightIter];
//        LightInput lightInput;
//        INITIALIZE_LIGHT(lightInput, lightIndex);
//
//        half3 lightDirection;
//        half atten = ComputeLightAttenuationVertex(lightInput, normal, worldPos, lightDirection);
//        o.fogCoord.yzw += LightingLambert(diffuse, lightDirection, normal, atten);
//    }
//#endif

#if !defined(LIGHTMAP_ON)
    o.fogCoord.yzw = SHEvalLinearL2(half4(normal, 1.0));
#endif

    o.hpos = UnityObjectToClipPos(v.vertex);
    o.fogCoord.x = ComputeFogFactor(o.hpos.z);
    return o;
}

// NdotV, reflectVec (view tangent space)
// NdotL, (light in tangent space)

half4 LightweightFragmentPBR(LightweightVertexOutput IN, SurfaceData surfaceData)
{
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    // Should we standardize/pass this as input
    float2 lightmapUV = IN.uv01.zw;
    float fogFactor = IN.fogCoord.x;
    float3 SH_L2Coeff = IN.fogCoord.yzw;
    half3 viewDir = IN.viewDir.xyz;
    float3 posWS = IN.posWS.xyz;

    half3 normalWorld = TangentToWorldNormal(surfaceData.normal, IN);

    half3 reflectVec = reflect(-viewDir, normalWorld);
    half roughness2 = brdfData.roughness * brdfData.roughness;
    UnityIndirect indirectLight = LightweightGI(lightmapUV, SH_L2Coeff, normalWorld, reflectVec, surfaceData.occlusion, brdfData.perceptualRoughness);

    // PBS
    half fresnelTerm = Pow4(1.0 - saturate(dot(normalWorld, viewDir)));
    half3 color = LightweightBRDFIndirect(brdfData, indirectLight, roughness2, fresnelTerm);
    half3 lightDirection;

#ifdef _MAIN_LIGHT
    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeMainLightAttenuation(light, normalWorld, posWS, lightDirection);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(IN, _ShadowLightDirection.xyz);

    half NdotL = saturate(dot(normalWorld, lightDirection));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(brdfData, roughness2, normalWorld, lightDirection, viewDir) * radiance;
#endif

#ifdef _ADDITIONAL_PIXEL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);
        half lightAtten = ComputeLightAttenuation(light, normalWorld, posWS, lightDirection);

        half NdotL = saturate(dot(normalWorld, lightDirection));
        half3 radiance = light.color * (lightAtten * NdotL);
        color += LightweightBDRF(brdfData, roughness2, normalWorld, lightDirection, viewDir) * radiance;
    }
#endif

    color += surfaceData.emission;

    // Computes fog factor per-vertex
    ApplyFog(color, fogFactor);
    return OutputColor(color, surfaceData.alpha);
}

half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    SurfaceData surfaceData;
    InitializeSurfaceData(IN, surfaceData);

    return LightweightFragmentPBR(IN, surfaceData);
}

half4 LitPassFragmentSimple(LightweightVertexOutput i) : SV_Target
{
    half4 diffuseAlpha = tex2D(_MainTex, i.uv01.xy);
    half3 diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(diffuseAlpha.rgb) * _Color.rgb;

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
    half alpha = _Color.a;
#else
    half alpha = diffuseAlpha.a * _Color.a;
#endif

    // Keep for compatibility reasons. Shader Inpector throws a warning when using cutoff
    // due overdraw performance impact.
#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif

    half3 normalTangent = Normal(i.uv01.xy);
    half3 normalWorld = TangentToWorldNormal(normalTangent, i);

    half4 specularGloss;
    SpecularGloss(i.uv01.xy, alpha, specularGloss);

    half3 viewDir = i.viewDir.xyz;
    float3 worldPos = i.posWS.xyz;

    half3 lightDirection;

#if defined(LIGHTMAP_ON)
    half3 color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) * diffuse;
#else
    half3 color = (SHEvalLinearL0L1(half4(normalWorld, 1.0)) + i.fogCoord.yzw) * diffuse;
#endif

#ifdef _MAIN_LIGHT
    LightInput lightInput;
    INITIALIZE_MAIN_LIGHT(lightInput);
    half lightAtten = ComputeMainLightAttenuation(lightInput, normalWorld, worldPos, lightDirection);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(i, _ShadowLightDirection.xyz);

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
    color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normalWorld, viewDir, lightAtten) * lightInput.color;
#else
    color += LightingLambert(diffuse, lightDirection, normalWorld, lightAtten) * lightInput.color;
#endif

#endif

#ifdef _ADDITIONAL_PIXEL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput lightData;
        INITIALIZE_LIGHT(lightData, lightIter);
        half lightAtten = ComputeLightAttenuation(lightData, normalWorld, worldPos, lightDirection);

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
        color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normalWorld, viewDir, lightAtten) * lightData.color;
#else
        color += LightingLambert(diffuse, lightDirection, normalWorld, lightAtten) * lightData.color;
#endif
    }

#endif // _ADDITIONAL_PIXEL_LIGHTS

    color += EmissionLW(i.uv01.xy);

    // Computes Fog Factor per vextex
    ApplyFog(color, i.fogCoord.x);
    return OutputColor(color, alpha);
};

#endif
