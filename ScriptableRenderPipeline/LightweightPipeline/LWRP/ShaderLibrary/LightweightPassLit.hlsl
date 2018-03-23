#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LWRP/ShaderLibrary/InputSurface.hlsl"
#include "LWRP/ShaderLibrary/Lighting.hlsl"

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
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

#ifdef _NORMALMAP
    half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 binormal                  : TEXCOORD5;    // xyz: binormal, w: viewDir.z
#else
    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#ifdef _SHADOWS_ENABLED
    float4 shadowCoord              : TEXCOORD7;
#endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(LightweightVertexOutput IN, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = IN.posWSShininess.xyz;

#ifdef _NORMALMAP
    half3 viewDir = half3(IN.normal.w, IN.tangent.w, IN.binormal.w);
    inputData.normalWS = TangentToWorldNormal(normalTS, IN.tangent.xyz, IN.binormal.xyz, IN.normal.xyz);
#else
    half3 viewDir = IN.viewDir;
    inputData.normalWS = FragmentNormalWS(IN.normal);
#endif

    inputData.viewDirectionWS = FragmentViewDirWS(viewDir);
#ifdef _SHADOWS_ENABLED
    inputData.shadowCoord = IN.shadowCoord;
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TransformMainTextureCoord(v.texcoord);

    o.posWSShininess.xyz = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(o.posWSShininess.xyz);

    half3 viewDir = VertexViewDirWS(GetCameraPositionWS() - o.posWSShininess.xyz);

#ifdef _NORMALMAP
    o.normal.w = viewDir.x;
    o.tangent.w = viewDir.y;
    o.binormal.w = viewDir.z;
#else
    o.viewDir = viewDir;
#endif

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normal.xyz, o.vertexSH);

    half3 vertexLight = VertexLighting(o.posWSShininess.xyz, o.normal.xyz);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _SHADOWS_ENABLED
#if SHADOWS_SCREEN
    o.shadowCoord = ComputeShadowCoord(o.clipPos);
#else
    o.shadowCoord = TransformWorldToShadowCoord(o.posWSShininess.xyz);
#endif
#endif

    return o;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);

    InputData inputData;
    InitializeInputData(IN, surfaceData.normalTS, inputData);

    half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    ApplyFog(color.rgb, inputData.fogCoord);
    return color;
}

// Used in Standard (Simple Lighting) shader
// TODO: we only need to specialise this because of _Shininess prop
// Once we refactor SimpleLighting shader we will be able to share vertex
// between PBS and Simple
LightweightVertexOutput LitPassVertexSimple(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TransformMainTextureCoord(v.texcoord);

    o.posWSShininess.xyz = TransformObjectToWorld(v.vertex.xyz);
    o.posWSShininess.w = _Shininess * 128.0;
    o.clipPos = TransformWorldToHClip(o.posWSShininess.xyz);

    half3 viewDir = VertexViewDirWS(GetCameraPositionWS() - o.posWSShininess.xyz);

#ifdef _NORMALMAP
    o.normal.w = viewDir.x;
    o.tangent.w = viewDir.y;
    o.binormal.w = viewDir.z;
#else
    o.viewDir = viewDir;
#endif

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normal.xyz, o.vertexSH);

    half3 vertexLight = VertexLighting(o.posWSShininess.xyz, o.normal.xyz);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _SHADOWS_ENABLED
#if SHADOWS_SCREEN
    o.shadowCoord = ComputeShadowCoord(o.clipPos);
#else
    o.shadowCoord = TransformWorldToShadowCoord(o.posWSShininess.xyz);
#endif
#endif

    return o;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float2 uv = IN.uv;
    half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

    half alpha = diffuseAlpha.a * _Color.a;
    AlphaDiscard(alpha, _Cutoff);
#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

#ifdef _NORMALMAP
    half3 normalTS = Normal(uv);
#else
    half3 normalTS = half3(0, 0, 1);
#endif

    half3 emission = Emission(uv);
    half4 specularGloss = SpecularGloss(uv, diffuseAlpha.a);
    half shininess = IN.posWSShininess.w;

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);

    return LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);
};

#endif
