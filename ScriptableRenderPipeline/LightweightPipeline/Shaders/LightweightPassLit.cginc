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
    o.hpos = UnityObjectToClipPos(v.vertex);

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

    UNITY_TRANSFER_FOG(o, o.hpos);
    return o;
}

half4 LitPassFragment(LightweightVertexOutput i) : SV_Target
{
    SurfaceData surfaceData;
    InitializeSurfaceData(i, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    float2 lightmapUV = i.uv01.zw;
    half3 normal = surfaceData.normalWorld;
    half3 reflectVec = reflect(-i.viewDir.xyz, normal);
    half roughness2 = brdfData.roughness * brdfData.roughness;
    UnityIndirect indirectLight = LightweightGI(lightmapUV, i.fogCoord.yzw, normal, reflectVec, surfaceData.ao, brdfData.perceptualRoughness);

    // PBS
    half fresnelTerm = Pow4(1.0 - saturate(dot(normal, i.viewDir.xyz)));
    half3 color = LightweightBRDFIndirect(brdfData, indirectLight, roughness2, fresnelTerm);
    half3 lightDirection;

#ifndef _MULTIPLE_LIGHTS
    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);

#ifdef _SHADOWS
    lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

    half NdotL = saturate(dot(normal, lightDirection));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(brdfData, roughness2, normal, lightDirection, i.viewDir.xyz) * radiance;
#else

#ifdef _SHADOWS
    half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
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
        color += LightweightBDRF(brdfData, roughness2, normal, lightDirection, i.viewDir.xyz) * radiance;
    }
#endif

    color += surfaceData.emission;
    UNITY_APPLY_FOG(i.fogCoord, color);
    return OutputColor(color, surfaceData.alpha);
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

    half3 normal = Normal(i);

    half4 specularGloss;
    SpecularGloss(i.uv01.xy, alpha, specularGloss);

    half3 viewDir = i.viewDir.xyz;
    float3 worldPos = i.posWS.xyz;

    half3 lightDirection;

#ifndef _MULTIPLE_LIGHTS
    LightInput lightInput;
    INITIALIZE_MAIN_LIGHT(lightInput);
    half lightAtten = ComputeLightAttenuation(lightInput, normal, worldPos, lightDirection);
#ifdef _SHADOWS
    lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
    half3 color = LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightInput.color;
#else
    half3 color = LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightInput.color;
#endif

#else
    half3 color = half3(0, 0, 0);

#ifdef _SHADOWS
    half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif
    int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput lightData;
        int lightIndex = unity_4LightIndices0[lightIter];
        INITIALIZE_LIGHT(lightData, lightIndex);
        half lightAtten = ComputeLightAttenuation(lightData, normal, worldPos, lightDirection);
#ifdef _SHADOWS
        lightAtten *= max(shadowAttenuation, half(lightIndex != _ShadowData.x));
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
        color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightData.color;
#else
        color += LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightData.color;
#endif
    }

#endif // _MULTIPLE_LIGHTS

#ifdef _EMISSION
    color += LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, i.uv01.xy).rgb) * _EmissionColor;
#else
    color += _EmissionColor;
#endif

#if defined(LIGHTMAP_ON)
    color += DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) * diffuse;
#else
    color += (SHEvalLinearL0L1(half4(normal, 1.0)) + i.fogCoord.yzw) * diffuse;
#endif

#if _REFLECTION_CUBEMAP
    // TODO: we can use reflect vec to compute specular instead of half when computing cubemap reflection
    half3 reflectVec = reflect(-i.viewDir.xyz, normal);
    color += texCUBE(_Cube, reflectVec).rgb * specularGloss.rgb;
#elif defined(_REFLECTION_PROBE)
    half3 reflectVec = reflect(-i.viewDir.xyz, normal);
    half4 reflectionProbe = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectVec);
    color += reflectionProbe.rgb * (reflectionProbe.a * unity_SpecCube0_HDR.x) * specularGloss.rgb;
#endif

    UNITY_APPLY_FOG(i.fogCoord, color);

    return OutputColor(color, alpha);
};

#endif
