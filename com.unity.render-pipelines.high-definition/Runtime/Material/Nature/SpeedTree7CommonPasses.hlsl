#ifndef HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED
#define HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED

///////////////////////////////////////////////////////////////////////
//  struct SpeedTreeVertexInput

// texcoord setup
//
//      BRANCHES                        FRONDS                      LEAVES
// 0    diffuse uv, branch wind xy      "                           "
// 1    lod xyz, 0                      lod xyz, 0                  anchor xyz, lod scalar
// 2    detail/seam uv, seam amount, 0  frond wind xyz, 0           leaf wind xyz, leaf group

struct SpeedTreeVertexInput
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    float3 normal       : NORMAL;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    float4 color         : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SpeedTreeVertexOutput
{
    float3 uvHueVariation            : TEXCOORD0;

#ifdef VERTEX_COLOR
    float4 color                 : TEXCOORD1;
#endif

#ifdef GEOM_TYPE_BRANCH_DETAIL
    float3 detail                : TEXCOORD2;
#endif
    //float4 fogFactorAndVertexLight   : TEXCOORD3;    // x: fogFactor, yzw: vertex light

#ifdef EFFECT_BUMP
    float4 normalWS              : TEXCOORD4;    // xyz: normal, w: viewDir.x
    float4 tangentWS             : TEXCOORD5;    // xyz: tangent, w: viewDir.y
    float4 bitangentWS           : TEXCOORD6;    // xyz: bitangent, w: viewDir.z
#else
    float3 normalWS              : TEXCOORD4;
    float3 viewDirWS             : TEXCOORD5;
#endif

#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord          : TEXCOORD7;
#endif

    float3 positionWS               : TEXCOORD8;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SpeedTreeVertexDepthOutput
{
    float3 uvHueVariation            : TEXCOORD0;
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
/*
void InitializeInputData(SpeedTreeVertexOutput input, float3 normalTS, out InputData inputData)
{
    inputData.positionWS = input.positionWS.xyz;

#ifdef EFFECT_BUMP
    inputData.normalWS = TransformTangentToWorld(normalTS, float3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = float3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
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
    inputData.bakedGI = float3(0, 0, 0); // No GI currently.
}

float4 SpeedTree7Frag(SpeedTreeVertexOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_POST_VERTEX(input);

#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(input.clipPos.xyz, unity_LODFade.x);
#endif

    float2 uv = input.uvHueVariation.xy;
    float4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

#ifdef SPEEDTREE_ALPHATEST
    clip(diffuse.a - _Cutoff);
#endif

    float3 diffuseColor = diffuse.rgb;

#ifdef GEOM_TYPE_BRANCH_DETAIL
    float4 detailColor = tex2D(_DetailTex, input.detail.xy);
    diffuseColor.rgb = lerp(diffuseColor.rgb, detailColor.rgb, input.detail.z < 2.0f ? saturate(input.detail.z) : detailColor.a);
#endif

#ifdef EFFECT_HUE_VARIATION
    float3 shiftedColor = lerp(diffuseColor.rgb, _HueVariation.rgb, input.uvHueVariation.z);
    float maxBase = max(diffuseColor.r, max(diffuseColor.g, diffuseColor.b));
    float newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase /= newMaxBase;
    maxBase = maxBase * 0.5f + 0.5f;
    // preserve vibrance
    shiftedColor.rgb *= maxBase;
    diffuseColor.rgb = saturate(shiftedColor);
#endif

#ifdef EFFECT_BUMP
    float3 normalTs = SampleNormal(uv, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
#ifdef GEOM_TYPE_BRANCH_DETAIL
    float3 detailNormal = SampleNormal(input.detail.xy, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
    normalTs = lerp(normalTs, detailNormal, input.detail.z < 2.0f ? saturate(input.detail.z) : detailColor.a);
#endif
#else
    float3 normalTs = float3(0, 0, 1);
#endif

    InputData inputData;
    InitializeInputData(input, normalTs, inputData);

#ifdef VERTEX_COLOR
    diffuseColor.rgb *= input.color.rgb;
#else
    diffuseColor.rgb *= _Color.rgb;
#endif

    float4 color = LightweightFragmentBlinnPhong(inputData, diffuseColor.rgb, float4(0, 0, 0, 0), 0, 0, diffuse.a);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    return color;
}

float4 SpeedTree7FragDepth(SpeedTreeVertexDepthOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_POST_VERTEX(input);

#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(input.clipPos.xyz, unity_LODFade.x);
#endif

    float2 uv = input.uvHueVariation.xy;
    float4 diffuse = SampleAlbedoAlpha(uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
    diffuse.a *= _Color.a;

#ifdef SPEEDTREE_ALPHATEST
    clip(diffuse.a - _Cutoff);
#endif

#if defined(SCENESELECTIONPASS)
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    return float4(_ObjectId, _PassValue, 1.0, 1.0);
#else    
    return float4(0, 0, 0, 0);
#endif
}
*/
#endif
