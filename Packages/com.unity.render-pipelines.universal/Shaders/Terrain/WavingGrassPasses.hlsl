#ifndef UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED
#define UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/WavingGrassFeatures.hlsl"

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

#if USE_LIGHTMAP_UV_INTERPOLATOR
    float2 lightmapUV               : LIGHTMAPUV;
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    half3 vertexSH                  : VERTEXSH;
#endif

    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;

    URP_LIGHT_ACCUM4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    float4 shadowCoord              : TEXCOORD6;
#endif
    half4 color                     : TEXCOORD7;

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion           : TEXCOORD8;
#endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput input, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.preExposureMultiplier = GetPreExposureMultiplier();

    inputData.positionWS = input.posWSShininess.xyz;

    inputData.positionCS = input.clipPos;

    half3 viewDirWS = input.viewDir;
    viewDirWS = SafeNormalize(viewDirWS);

    inputData.normalWS = NormalizeNormalPerPixel(input.normal, UseNormalMap());
    inputData.viewDirectionWS = viewDirWS;

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    inputData.shadowCoord = ShadowCoordInterpolatorAvailable() ? input.shadowCoord : TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent());
#else
    inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
#endif

    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

    {
        GIParams giParams = (GIParams)0;
        #if USE_LIGHTMAP_UV_INTERPOLATOR
        giParams.staticLightmapUV = input.lightmapUV;
        #endif
        #if USE_VERTEX_SH_INTERPOLATOR
        giParams.vertexSH = input.vertexSH;
        #endif
        #ifdef USE_APV_PROBE_OCCLUSION
        giParams.vertexProbeOcclusion = input.probeOcclusion;
        #endif
        giParams.positionWS = inputData.positionWS;
        giParams.normalWS = inputData.normalWS;
        giParams.viewDirWS = inputData.viewDirectionWS;
        giParams.positionSS = input.clipPos.xy;
        InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);
    }

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);

    #if defined(DEBUG_DISPLAY)
    #if USE_LIGHTMAP_UV_INTERPOLATOR
    inputData.staticLightmapUV = input.lightmapUV;
    #endif
    #if USE_VERTEX_SH_INTERPOLATOR
    inputData.vertexSH = input.vertexSH;
    #endif
    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
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

#if USE_LIGHTMAP_UV_INTERPOLATOR
    vertData.lightmapUV = LightmapAvailable() ? TransformLightmapUV(input.lightmapUV, unity_LightmapST) : float2(0, 0);
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    if (!LightmapAvailable())
    {
        #ifdef USE_APV_PROBE_OCCLUSION
        vertData.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, vertData.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), vertData.probeOcclusion);
        #else
        vertData.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, vertData.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS));
        #endif
    }
    else
    {
        vertData.vertexSH = half3(0, 0, 0);
    }
#endif

    URP_LIGHT_ACCUM3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normal.xyz);
    half fogFactor = 0;
    vertData.fogFactorAndVertexLight = URP_LIGHT_ACCUM4(fogFactor, vertexLight);

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    vertData.shadowCoord = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
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
    half4 diffuseAlpha = SampleBaseMap(input.uv);
    half3 diffuse = diffuseAlpha.rgb * input.color.rgb;

    half alpha = diffuseAlpha.a * input.color.a;
    alpha = AlphaDiscard(alpha, _Cutoff);

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.alpha = alpha;
    outSurfaceData.albedo = diffuse;
    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = 0.0; // To match forward pass (UUM-113119)
    outSurfaceData.smoothness = input.posWSShininess.w;
    outSurfaceData.normalTS = 0.0; // unused
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0.0;
}

// Used for StandardSimpleLighting shader
#ifdef TERRAIN_GBUFFER
GBufferFragOutput LitPassFragmentGrass(GrassVertexOutput input)
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
    URP_LIGHT_ACCUM4 color = URP_LIGHT_ACCUM4(inputData.bakedGI * surfaceData.albedo + surfaceData.emission, surfaceData.alpha);
    return PackGBuffersSurfaceData(surfaceData, inputData, ClampExposed(inputData.preExposureMultiplier * color.rgb), ReceiveShadows());
#else
    URP_LIGHT_ACCUM4 color = UniversalFragmentBlinnPhong(inputData, surfaceData,
        UseSpecGlossMap() || UseSpecularColor(), UseAlphaPremultiply(),
        ReceiveShadows(), IsSurfaceTypeTransparent());
    color.rgb = ClampExposed(inputData.preExposureMultiplier * BlendDistanceFog(color.rgb, input.clipPos));
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
    half albedoAlpha = SampleBaseMap(input.uv).a;
    AlphaDiscard((UseSmoothnessTextureAlbedoChannelA() || UseGlossinessFromBaseAlpha()) ? input.color.a : albedoAlpha * input.color.a, _Cutoff);
    return input.clipPos.z;
}
#endif
