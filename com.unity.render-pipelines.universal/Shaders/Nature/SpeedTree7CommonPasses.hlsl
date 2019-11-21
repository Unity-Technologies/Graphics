#ifndef UNIVERSAL_SPEEDTREE7COMMON_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE7COMMON_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

    #ifdef _MAIN_LIGHT_SHADOWS
        float4 shadowCoord          : TEXCOORD6;
    #endif

    float3 positionWS               : TEXCOORD7;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthOutput
{
    half3 uvHueVariation            : TEXCOORD0;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(SpeedTreeVertexOutput input, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = input.positionWS.xyz;

    #ifdef EFFECT_BUMP
        inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
        inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
        inputData.viewDirectionWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    #else
        inputData.normalWS = input.normalWS;
        inputData.viewDirectionWS = input.viewDirWS;
    #endif

    #if SHADER_HINT_NICE_QUALITY
        inputData.viewDirectionWS = SafeNormalize(inputData.viewDirectionWS);
    #endif

    #ifdef _MAIN_LIGHT_SHADOWS
        inputData.shadowCoord = input.shadowCoord;
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = half3(0, 0, 0); // No GI currently.
}

half4 SpeedTree7Frag(SpeedTreeVertexOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        LODDitheringTransition(input.clipPos.xy, unity_LODFade.x);
    #endif
#endif

    half2 uv = input.uvHueVariation.xy;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

    #ifdef SPEEDTREE_ALPHATEST
        clip(diffuse.a - _Cutoff);
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

    #ifdef VERTEX_COLOR
        diffuseColor.rgb *= input.color.rgb;
    #else
        diffuseColor.rgb *= _Color.rgb;
    #endif

    half4 color = UniversalFragmentBlinnPhong(inputData, diffuseColor.rgb, half4(0, 0, 0, 0), 0, 0, diffuse.a);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    return color;
}

half4 SpeedTree7FragDepth(SpeedTreeVertexDepthOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        LODDitheringTransition(input.clipPos.xy, unity_LODFade.x);
    #endif
#endif

    half2 uv = input.uvHueVariation.xy;
    half4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

    #ifdef SPEEDTREE_ALPHATEST
        clip(diffuse.a - _Cutoff);
    #endif

    #if defined(SCENESELECTIONPASS)
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        return half4(_ObjectId, _PassValue, 1.0, 1.0);
    #else
        return half4(0, 0, 0, 0);
    #endif
}

#endif
