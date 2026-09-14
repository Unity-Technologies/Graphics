#ifndef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/DetailMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ParallaxMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitFeatures.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PackNormalsTexture.hlsl"

#if FEATURES_PARALLAXMAP
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR 1
#endif

#if (FEATURES_NORMALMAP || FEATURES_DETAILMAP)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR 1
#endif

#if defined(_ALPHATEST_ON) || FEATURES_PARALLAXMAP || FEATURES_NORMALMAP || FEATURES_DETAILMAP || defined(_WRITE_SMOOTHNESS)
#define REQUIRES_UV_INTERPOLATOR
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;

    #if defined(REQUIRES_UV_INTERPOLATOR)
    float2 uv          : TEXCOORD1;
    #endif

    half3 normalWS     : TEXCOORD2;

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS    : TEXCOORD4;    // xyz: tangent, w: sign
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS    : TEXCOORD8;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(REQUIRES_UV_INTERPOLATOR)
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    output.normalWS = half3(normalInput.normalWS);
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
        float sign = input.tangentOS.w * float(GetOddNegativeScale());
        half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    #endif

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        output.tangentWS = tangentWS;
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
        half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
        output.viewDirTS = viewDirTS;
    #endif

    return output;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
        half3 viewDirTS = input.viewDirTS;
        ApplyPerPixelDisplacement(viewDirTS, input.uv);
    #endif

    #if defined(_ALPHATEST_ON) || defined(_WRITE_SMOOTHNESS)
        float alpha = SampleBaseMap(input.uv).a;
    #endif

    #if defined(_ALPHATEST_ON)
        half discardAlpha = half(alpha) * GetBaseColor().a;
        if (UseSmoothnessTextureAlbedoChannelA() || UseGlossinessFromBaseAlpha())
            discardAlpha = GetBaseColor().a;
        AlphaDiscard(discardAlpha, GetCutoff());
    #endif

    LODFadeCrossFade(input.positionCS);

    half3 normalWS = input.normalWS;

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        if (UseNormalMap() || UseDetailMap())
        {
            float sgn = input.tangentWS.w;      // should be either +1 or -1
            float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
            half3 normalTS = SampleNormal(input.uv);

            if (UseDetailMap())
            {
                half detailMask = SampleDetailMask(input.uv).a;
                float2 detailUv = input.uv * GetDetailAlbedoMapST().xy + GetDetailAlbedoMapST().zw;
                normalTS = ApplyDetailNormal(detailUv, normalTS, detailMask);
            }

            normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
        }
    #endif

    outNormalWS = half4(PackNormalWSToTexture(NormalizeNormalPerPixel(normalWS, UseNormalMap())), 0.0);

    #if defined(_WRITE_SMOOTHNESS) && !defined(_SCREENSPACEREFLECTIONS_OFF)
        half4 specGloss;
        if (UseMetallicSpecGlossMap())
        {
            specGloss = IsSpecularSetup() ? SampleSpecGlossMap(input.uv) : SampleMetallicGlossMap(input.uv);
            specGloss.a = UseSmoothnessTextureAlbedoChannelA() ? alpha * _Smoothness : specGloss.a * _Smoothness;
        }
        else
        {
            specGloss.rgb = IsSpecularSetup() ? _SpecColor.rgb : _Metallic.rrr;
            specGloss.a = UseSmoothnessTextureAlbedoChannelA() ? alpha * _Smoothness : _Smoothness;
        }
        outNormalWS.a = specGloss.a;
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        outRenderingLayers = EncodeMeshRenderingLayer();
    #endif
}

#endif
