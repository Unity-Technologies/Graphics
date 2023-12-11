#ifndef UNIVERSAL_SPEEDTREE7COMMON_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE7COMMON_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "SpeedTreeUtility.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct SpeedTreeVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    half4 color         : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SpeedTreeVertexOutput
{
    #ifdef VERTEX_COLOR
        half4 color                 : COLOR;
    #endif

    half3 uvHueVariation            : TEXCOORD0;

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        half3 detail                : TEXCOORD1;
    #endif

    half4 fogFactorAndVertexLight   : TEXCOORD2;    // x: fogFactor, yzw: vertex light

    #ifdef EFFECT_BUMP
        half4 normalWS              : TEXCOORD3;    // xyz: normal, w: viewDir.x
        half4 tangentWS             : TEXCOORD4;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS           : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normalWS              : TEXCOORD3;
        half3 viewDirWS             : TEXCOORD4;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord          : TEXCOORD6;
    #endif

    float3 positionWS               : TEXCOORD7;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 8);
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthOutput
{
    half3 uvHueVariation            : TEXCOORD0;
    half3 viewDirWS                 : TEXCOORD1;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthNormalOutput
{
    half3 uvHueVariation            : TEXCOORD0;
    float4 clipPos                  : SV_POSITION;

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        half3 detail                : TEXCOORD1;
    #endif

    #ifdef EFFECT_BUMP
        half4 normalWS              : TEXCOORD2;    // xyz: normal, w: viewDir.x
        half4 tangentWS             : TEXCOORD3;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS           : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normalWS              : TEXCOORD2;
        half3 viewDirWS             : TEXCOORD3;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(SpeedTreeVertexOutput input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS.xyz;
    inputData.positionCS = input.clipPos;

    #ifdef EFFECT_BUMP
        inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
        inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
        inputData.viewDirectionWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    #else
        inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
        inputData.viewDirectionWS = input.viewDirWS;
    #endif

    inputData.viewDirectionWS = SafeNormalize(inputData.viewDirectionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    // Billboards cannot use lightmaps.
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        inputData.positionCS.xy);
#else
    inputData.bakedGI = SAMPLE_GI(NOT_USED, input.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    inputData.shadowMask = half4(1, 1, 1, 1); // No GI currently.

    #if defined(DEBUG_DISPLAY) && !defined(LIGHTMAP_ON)
    inputData.vertexSH = input.vertexSH;
    #endif

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
    #endif
}

#ifdef GBUFFER
FragmentOutput SpeedTree7Frag(SpeedTreeVertexOutput input)
#else
half4 SpeedTree7Frag(SpeedTreeVertexOutput input) : SV_Target
#endif
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uvHueVariation.xy;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

    #ifdef SPEEDTREE_ALPHATEST
        diffuse.a = AlphaDiscard(diffuse.a, _Cutoff);
    #endif

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.clipPos);
    #endif

    half3 diffuseColor = diffuse.rgb;

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        half4 detailColor = tex2D(_DetailTex, input.detail.xy);
        diffuseColor.rgb = lerp(diffuseColor.rgb, detailColor.rgb, input.detail.z < 2.0f ? saturate(input.detail.z) : detailColor.a);
    #endif

    #ifdef EFFECT_HUE_VARIATION
        half3 shiftedColor = lerp(diffuseColor.rgb, _HueVariation.rgb, input.uvHueVariation.z);
        half maxBase = max(diffuseColor.r, max(diffuseColor.g, diffuseColor.b));
        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
        maxBase /= newMaxBase;
        maxBase = maxBase * 0.5f + 0.5f;
        // preserve vibrance
        shiftedColor.rgb *= maxBase;
        diffuseColor.rgb = saturate(shiftedColor);
    #endif

    #ifdef EFFECT_BUMP
        half3 normalTs = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
        #ifdef GEOM_TYPE_BRANCH_DETAIL
            half3 detailNormal = SampleNormal(input.detail.xy, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
            normalTs = lerp(normalTs, detailNormal, input.detail.z < 2.0f ? saturate(input.detail.z) : detailColor.a);
        #endif
    #else
        half3 normalTs = half3(0, 0, 1);
    #endif

    InputData inputData;
    InitializeInputData(input, normalTs, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uvHueVariation.xy);

    #ifdef VERTEX_COLOR
        diffuseColor.rgb *= input.color.rgb;
    #else
        diffuseColor.rgb *= _Color.rgb;
    #endif

    SurfaceData surfaceData;

    surfaceData.albedo = diffuseColor.rgb;
    surfaceData.alpha = diffuse.a;
    surfaceData.emission = half3(0, 0, 0);
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = 0;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = normalTs;

    #ifdef GBUFFER
        half4 color = half4(inputData.bakedGI * diffuseColor.rgb, diffuse.a);
        surfaceData.occlusion = 1.0;
        return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingSimpleLit);
    #else
        half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
        color.rgb = MixFog(color.rgb, inputData.fogCoord);
        color.a = OutputAlpha(color.a, _Surface);
        return color;
    #endif
}

half4 SpeedTree7FragDepth(SpeedTreeVertexDepthOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uvHueVariation.xy;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

    #ifdef SPEEDTREE_ALPHATEST
        AlphaDiscard(diffuse.a, _Cutoff);
    #endif

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.clipPos);
    #endif

    #if defined(SCENESELECTIONPASS)
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        return half4(_ObjectId, _PassValue, 1.0, 1.0);
    #else
        return half4(input.clipPos.z, 0, 0, 0);
    #endif
}

half4 SpeedTree7FragDepthNormal(SpeedTreeVertexDepthNormalOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.uvHueVariation.xy;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

    #ifdef SPEEDTREE_ALPHATEST
        AlphaDiscard(diffuse.a, _Cutoff);
    #endif

    #ifdef LOD_FADE_CROSSFADE
        LODFadeCrossFade(input.clipPos);
    #endif

    #if defined(EFFECT_BUMP)
        half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
        #ifdef GEOM_TYPE_BRANCH_DETAIL
            half4 detailColor = tex2D(_DetailTex, input.detail.xy);
            half3 detailNormal = SampleNormal(input.detail.xy, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
            normalTS = lerp(normalTS, detailNormal, input.detail.z < 2.0f ? saturate(input.detail.z) : detailColor.a);
        #endif

        half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz)).xyz;
    #else
        half3 normalWS = input.normalWS.xyz;
    #endif

    return half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
