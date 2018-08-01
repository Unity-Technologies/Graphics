#ifndef LIGHTWEIGHT_PASS_LIT_TERRAIN_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_TERRAIN_INCLUDED

#include "LWRP/ShaderLibrary/Lighting.hlsl"

struct VertexInput
{
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;
};

struct VertexOutput
{
    float4 uvSplat01                : TEXCOORD0; // xy: splat0, zw: splat1
    float4 uvSplat23                : TEXCOORD1; // xy: splat2, zw: splat3
    float4 uvControlAndLM           : TEXCOORD2; // xy: control, zw: lightmap

#if _TERRAIN_NORMAL_MAP
    half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 binormal                  : TEXCOORD5;    // xyz: binormal, w: viewDir.z
#else
    half3 normal                    : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
    float3 positionWS               : TEXCOORD7;
    float4 shadowCoord              : TEXCOORD8;
    float4 clipPos                  : SV_POSITION;
};

void InitializeInputData(VertexOutput IN, half3 normalTS, out InputData input)
{
    input = (InputData)0;

    input.positionWS = IN.positionWS;

#ifdef _TERRAIN_NORMAL_MAP
    half3 viewDir = half3(IN.normal.w, IN.tangent.w, IN.binormal.w);
    input.normalWS = TangentToWorldNormal(normalTS, IN.tangent.xyz, IN.binormal.xyz, IN.normal.xyz);
#else
    half3 viewDir = IN.viewDir;
    input.normalWS = FragmentNormalWS(IN.normal);
#endif

    input.viewDirectionWS = FragmentViewDirWS(viewDir);
#ifdef _SHADOWS_ENABLED
    input.shadowCoord = IN.shadowCoord;
#else
    input.shadowCoord = float4(0, 0, 0, 0);
#endif
    input.fogCoord = IN.fogFactorAndVertexLight.x;
    input.vertexLighting = IN.fogFactorAndVertexLight.yzw;

#ifdef LIGHTMAP_ON
    input.bakedGI = SampleLightmap(IN.uvControlAndLM.zw, input.normalWS);
#endif
}

void SplatmapMix(VertexOutput IN, half4 defaultAlpha, out half4 splatControl, out half weight, out half4 mixedDiffuse, inout half3 mixedNormal)
{
    splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, IN.uvControlAndLM.xy);
    weight = dot(splatControl, 1.0h);

#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
    clip(weight == 0.0h ? -1.0h : 1.0h);
#endif

    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);

    half4 alpha = defaultAlpha * splatControl;

    mixedDiffuse = 0.0h;
    mixedDiffuse += SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, IN.uvSplat01.xy) * half4(splatControl.rrr, alpha.r);
    mixedDiffuse += SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, IN.uvSplat01.zw) * half4(splatControl.ggg, alpha.g);
    mixedDiffuse += SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, IN.uvSplat23.xy) * half4(splatControl.bbb, alpha.b);
    mixedDiffuse += SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, IN.uvSplat23.zw) * half4(splatControl.aaa, alpha.a);

#ifdef _TERRAIN_NORMAL_MAP
    half4 nrm = 0.0f;
    nrm += SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, IN.uvSplat01.xy) * splatControl.r;
    nrm += SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, IN.uvSplat01.zw) * splatControl.g;
    nrm += SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, IN.uvSplat23.xy) * splatControl.b;
    nrm += SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, IN.uvSplat23.zw) * splatControl.a;
    mixedNormal = UnpackNormal(nrm);
#else
    mixedNormal = half3(0.0h, 0.0h, 1.0h);
#endif
}

void SplatmapFinalColor(inout half4 color, half fogCoord)
{
    color.rgb *= color.a;
#ifdef TERRAIN_SPLAT_ADDPASS
    ApplyFogColor(color.rgb, half3(0.0h, 0.0h, 0.0h), fogCoord);
#else
    ApplyFog(color.rgb, fogCoord);
#endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard Terrain shader
VertexOutput SplatmapVert(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;

    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    float4 clipPos = TransformWorldToHClip(positionWS);

    o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Splat0);
    o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Splat1);
    o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Splat2);
    o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Splat3);
    o.uvControlAndLM.xy = TRANSFORM_TEX(v.texcoord, _Control);
    o.uvControlAndLM.zw = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;

    half3 viewDir = VertexViewDirWS(GetCameraPositionWS() - positionWS.xyz);

#ifdef _TERRAIN_NORMAL_MAP
    float4 vertexTangent = float4(cross(v.normal, float3(0, 0, 1)), -1.0);
    OutputTangentToWorld(vertexTangent, v.normal, o.tangent.xyz, o.binormal.xyz, o.normal.xyz);

    o.normal.w = viewDir.x;
    o.tangent.w = viewDir.y;
    o.binormal.w = viewDir.z;
#else
    o.normal = TransformObjectToWorldNormal(v.normal);
    o.viewDir = viewDir;
#endif
    o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
    o.fogFactorAndVertexLight.yzw = VertexLighting(positionWS, o.normal);
    o.positionWS = positionWS;
    o.clipPos = clipPos;

#ifdef _SHADOWS_ENABLED
    #if SHADOWS_SCREEN
        o.shadowCoord = ComputeShadowCoord(o.clipPos);
    #else
        o.shadowCoord = TransformWorldToShadowCoord(positionWS);
    #endif
#endif

    return o;
}

// Used in Standard Terrain shader
half4 SpatmapFragment(VertexOutput IN) : SV_TARGET
{
    half4 splatControl;
    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
    half3 normalTS;
    SplatmapMix(IN, defaultSmoothness, splatControl, weight, mixedDiffuse, normalTS);

    half3 albedo = mixedDiffuse.rgb;
    half smoothness = mixedDiffuse.a;
    half metallic = dot(splatControl, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
    half3 specular = half3(0.0h, 0.0h, 0.0h);
    half alpha = weight;

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
    half4 color = LightweightFragmentPBR(inputData, albedo, metallic, specular, smoothness, /* occlusion */ 1.0h, /* emission */ half3(0.0h, 0.0h, 0.0h), alpha);

    SplatmapFinalColor(color, inputData.fogCoord);

    return half4(color.rgb, 1.0h);
}

#endif // LIGHTWEIGHT_PASS_LIT_TERRAIN_INCLUDED
