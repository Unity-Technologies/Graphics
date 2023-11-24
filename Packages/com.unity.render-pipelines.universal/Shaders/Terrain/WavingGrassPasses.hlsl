#ifndef UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED
#define UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

struct GrassVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexOutput
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;

    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif
    half4 color                     : TEXCOORD7;

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput input, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.posWSShininess.xyz;

    half3 viewDirWS = input.viewDir;
    viewDirWS = SafeNormalize(viewDirWS);

    inputData.normalWS = NormalizeNormalPerPixel(input.normal);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#if defined(_FOG_FRAGMENT)
    float clipZ = input.clipPos.z;
    #if !UNITY_REVERSED_Z
    clipZ = lerp(UNITY_NEAR_CLIP_VALUE, 1, clipZ);    // OpenGL NDC, -1 < z < 1
    #endif
    clipZ *= input.clipPos.w;
    inputData.fogCoord = ComputeFogFactor(clipZ);
#else
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
#endif
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, NOT_USED, input.vertexSH, inputData.normalWS);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.clipPos.xy);
#else
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.staticLightmapUV = input.lightmapUV;
    #elif defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.lightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
}

void InitializeVertData(GrassVertexInput input, inout GrassVertexOutput vertData)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    vertData.uv = input.texcoord;
    vertData.posWSShininess.xyz = vertexInput.positionWS;
    vertData.posWSShininess.w = 32;
    vertData.clipPos = vertexInput.positionCS;

    vertData.viewDir = GetCameraPositionWS() - vertexInput.positionWS;

    vertData.viewDir = SafeNormalize(vertData.viewDir);

    vertData.normal = TransformObjectToWorldNormal(input.normal);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vertData.lightmapUV);
    OUTPUT_SH4(vertexInput.positionWS, vertData.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), vertData.vertexSH);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normal.xyz);
#if defined(_FOG_FRAGMENT)
    half fogFactor = 0;
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
    vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    vertData.shadowCoord = GetShadowCoord(vertexInput);
#endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

GrassVertexOutput WavingGrassVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

GrassVertexOutput WavingGrassBillboardVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    TerrainBillboardGrass (v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

inline void InitializeSimpleLitSurfaceData(GrassVertexOutput input, out SurfaceData outSurfaceData)
{
    half4 diffuseAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    half3 diffuse = diffuseAlpha.rgb * input.color.rgb;

    half alpha = diffuseAlpha.a * input.color.a;
    alpha = AlphaDiscard(alpha, _Cutoff);

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.alpha = alpha;
    outSurfaceData.albedo = diffuse;
    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = 0.1;// SampleSpecularSmoothness(uv, diffuseAlpha.a, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    outSurfaceData.smoothness = input.posWSShininess.w;
    outSurfaceData.normalTS = 0.0; // unused
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0.0;
}

// Used for StandardSimpleLighting shader
#ifdef TERRAIN_GBUFFER
FragmentOutput LitPassFragmentGrass(GrassVertexOutput input)
#else
half4 LitPassFragmentGrass(GrassVertexOutput input) : SV_Target
#endif
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeSimpleLitSurfaceData(input, surfaceData);

    InputData inputData;
    InitializeInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, input.uv, _MainTex);

#ifdef TERRAIN_GBUFFER
    half4 color = half4(inputData.bakedGI * surfaceData.albedo + surfaceData.emission, surfaceData.alpha);
    return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingSimpleLit);
#else
    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return half4(color.rgb, OutputAlpha(surfaceData.alpha, IsSurfaceTypeTransparent(_Surface)));
#endif
};

struct GrassVertexDepthOnlyInput
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexDepthOnlyOutput
{
    float2 uv           : TEXCOORD0;
    half4 color         : TEXCOORD1;
    float4 clipPos      : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeVertData(GrassVertexDepthOnlyInput input, inout GrassVertexDepthOnlyOutput vertData)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    vertData.uv = input.texcoord;
    vertData.clipPos = vertexInput.positionCS;
}

GrassVertexDepthOnlyOutput DepthOnlyVertex(GrassVertexDepthOnlyInput v)
{
    GrassVertexDepthOnlyOutput o = (GrassVertexDepthOnlyOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass(v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

GrassVertexDepthOnlyOutput DepthOnlyBillboardVertex(GrassVertexDepthOnlyInput v)
{
    GrassVertexDepthOnlyOutput o = (GrassVertexDepthOnlyOutput) 0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    TerrainBillboardGrass (v.vertex, v.tangent.xy);

    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

half4 DepthOnlyFragment(GrassVertexDepthOnlyOutput input) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)).a, input.color, _Cutoff);
    return input.clipPos.z;
}
#endif
