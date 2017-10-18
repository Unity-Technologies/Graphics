#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LightweightLighting.cginc"

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LightweightVertexOutput
{
    float2 uv                       : TEXCOORD0;
    float4 ambientOrLightmapUV      : TEXCOORD1; // xy: lightmapUV, zw: dynamicLightmapUV OR color from SH
    float4 posWS                    : TEXCOORD2;
#if _NORMALMAP
    half3 tangent                   : TEXCOORD3;
    half3 binormal                  : TEXCOORD4;
    half3 normal                    : TEXCOORD5;
#else
    half3 normal                    : TEXCOORD3;
#endif
    half4 viewDir                   : TEXCOORD6; // xyz: viewDir
    half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

inline void InitializeStandardLitSurfaceData(LightweightVertexOutput IN, out SurfaceData outSurfaceData)
{
    float2 uv = IN.uv;
    half4 albedoAlpha = tex2D(_MainTex, uv);

    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha);
    outSurfaceData.albedo = LIGHTWEIGHT_GAMMA_TO_LINEAR(albedoAlpha.rgb) * _Color.rgb;

#if _METALLIC_SETUP
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#else
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normal = Normal(uv);
    outSurfaceData.occlusion = OcclusionLW(uv);
    outSurfaceData.emission = EmissionLW(uv);
    outSurfaceData.emission += IN.fogFactorAndVertexLight.yzw;
    outSurfaceData.alpha = Alpha(albedoAlpha.a);

#if LIGHTMAP_ON
    outSurfaceData.ambient = half4(0.0h, 0.0h, 0.0h, 0.0h);
#else
    outSurfaceData.ambient = half4(IN.ambientOrLightmapUV);
#endif
}

void InitializeSurfaceInput(LightweightVertexOutput IN, out SurfaceInput outSurfaceInput)
{
#if LIGHTMAP_ON
    outSurfaceInput.lightmapUV = float4(IN.ambientOrLightmapUV.xy, 0.0, 0.0);
#else
    outSurfaceInput.lightmapUV = float4(0.0, 0.0, 0.0, 0.0);
#endif

#if _NORMALMAP
    outSurfaceInput.tangentWS = IN.tangent;
    outSurfaceInput.bitangentWS = IN.binormal;
#else
    outSurfaceInput.tangentWS = half3(1.0h, 0.0h, 0.0h);
    outSurfaceInput.bitangentWS = half3(0.0h, 1.0h, 0.0h);
#endif

    outSurfaceInput.normalWS = IN.normal;
    outSurfaceInput.positionWS = IN.posWS;
    outSurfaceInput.viewDirectionWS = IN.viewDir;
    outSurfaceInput.fogFactor = IN.fogFactorAndVertexLight.x;
}

LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.posWS.xyz = worldPos;

    half3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    o.viewDir.xyz = viewDir;

    half3 normal = normalize(UnityObjectToWorldNormal(v.normal));

#if _NORMALMAP
    half sign = v.tangent.w * unity_WorldTransformParams.w;
    o.tangent = normalize(mul((half3x3)unity_ObjectToWorld, v.tangent.xyz));
    o.binormal = cross(normal, o.tangent) * sign;
    o.normal = normal;
#else
    o.normal = normal;
#endif

#ifdef LIGHTMAP_ON
    o.ambientOrLightmapUV.xy = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    // TODO: Dynamic Lightmap
    o.ambientOrLightmapUV.zw = float2(0.0, 0.0);
#else
    o.ambientOrLightmapUV = half4(SHEvalLinearL2(half4(normal, 1.0)), 0.0h);
#endif

    o.fogFactorAndVertexLight.yzw = half3(0.0h, 0.0h, 0.0h);
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
//        o.ambient.yzw += LightingLambert(diffuse, lightDirection, normal, atten);
//    }
//#endif

    float4 clipPos = UnityObjectToClipPos(v.vertex);
    o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
    o.clipPos = clipPos;
    return o;
}

half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN, surfaceData);

    SurfaceInput surfaceInput;
    InitializeSurfaceInput(IN, surfaceInput);

    return LightweightFragmentPBR(surfaceInput.lightmapUV, surfaceInput.positionWS, surfaceInput.normalWS, surfaceInput.tangentWS, surfaceInput.bitangentWS, surfaceInput.viewDirectionWS, surfaceInput.fogFactor, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.normal, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha, surfaceData.ambient);
}

half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    float2 uv = IN.uv;

    half4 diffuseAlpha = tex2D(_MainTex, uv);
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

#if _NORMALMAP
    half3 normalTangent = Normal(uv);
    half3 normalWorld = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
    half3 normalWorld = normalize(IN.normal);
#endif

    half4 specularGloss;
    SpecularGloss(uv, alpha, specularGloss);

    half3 viewDir = IN.viewDir.xyz;
    float3 worldPos = IN.posWS.xyz;

    half3 lightDirection;

#if defined(LIGHTMAP_ON)
    half3 color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.ambientOrLightmapUV.xy)) * diffuse;
#else
    half3 color = (SHEvalLinearL0L1(half4(normalWorld, 1.0)) + IN.ambientOrLightmapUV.xyz) * diffuse;
#endif

#ifdef _MAIN_LIGHT
    LightInput lightInput;
    INITIALIZE_MAIN_LIGHT(lightInput);
    half lightAtten = ComputeMainLightAttenuation(lightInput, normalWorld, worldPos, lightDirection);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(worldPos, normalize(IN.normal), _ShadowLightDirection.xyz);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
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

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
        color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normalWorld, viewDir, lightAtten) * lightData.color;
#else
        color += LightingLambert(diffuse, lightDirection, normalWorld, lightAtten) * lightData.color;
#endif
    }

#endif // _ADDITIONAL_PIXEL_LIGHTS

    color += EmissionLW(uv);
    color += IN.fogFactorAndVertexLight.yzw;

    // Computes Fog Factor per vextex
    ApplyFog(color, IN.fogFactorAndVertexLight.x);
    return OutputColor(color, alpha);
};

#endif
