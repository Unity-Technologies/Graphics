#ifndef UNIVERSAL_BAKEDLIT_GBUFFER_PASS_INCLUDED
#define UNIVERSAL_BAKEDLIT_GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitFeatures.hlsl"

#if FEATURES_NORMALMAP
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR 1
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 uv0AndFogCoord : TEXCOORD0; // xy: uv0, z: fogCoord
#if USE_LIGHTMAP_UV_INTERPOLATOR
    float2 staticLightmapUV         : LIGHTMAPUV;
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    half3 vertexSH                  : VERTEXSH;
#endif
    half3 normalWS : TEXCOORD2;

#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS : TEXCOORD3;
#endif

#if defined(DEBUG_DISPLAY) || (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    float3 positionWS : TEXCOORD4;
    float3 viewDirWS : TEXCOORD5;
#endif

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD6;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData) 0;

    inputData.positionCS = input.positionCS;
    inputData.positionWS = float3(0, 0, 0);
    inputData.viewDirectionWS = half3(0, 0, 1);

    inputData.normalWS = input.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    if (UseNormalMap())
    {
        float sgn = input.tangentWS.w;      // should be either +1 or -1
        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
    }
#endif

    inputData.shadowCoord = float4(0, 0, 0, 0);
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);

#if defined(DEBUG_DISPLAY)
#if USE_LIGHTMAP_UV_INTERPOLATOR
    inputData.staticLightmapUV = input.staticLightmapUV;
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    inputData.vertexSH = input.vertexSH;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
#endif
#endif
}

void InitializeBakedGIData(Varyings input, inout InputData inputData)
{
    GIParams giParams = (GIParams)0;

    #if USE_LIGHTMAP_UV_INTERPOLATOR
    giParams.staticLightmapUV = input.staticLightmapUV;
    #endif
    #if USE_VERTEX_SH_INTERPOLATOR
    giParams.vertexSH = input.vertexSH;
    #endif
    #ifdef USE_APV_PROBE_OCCLUSION
    giParams.vertexProbeOcclusion = input.probeOcclusion;
    #endif

    giParams.viewDirWS = float3(0, 0, 1);
    #if defined(DEBUG_DISPLAY) || defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    giParams.positionWS = input.positionWS;
    giParams.viewDirWS = input.viewDirWS;
    #endif

    giParams.normalWS = inputData.normalWS;
    giParams.positionSS = input.positionCS.xy;
    giParams.isSurfaceTypeTransparent = IsSurfaceTypeTransparent();

    InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);
}

Varyings BakedLitGBufferPassVertex(Attributes input)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    output.uv0AndFogCoord.xy = TRANSFORM_TEX(input.uv, _BaseMap);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if USE_LIGHTMAP_UV_INTERPOLATOR
    output.staticLightmapUV = TransformLightmapUV(input.staticLightmapUV.xy, unity_LightmapST);
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    #ifdef USE_APV_PROBE_OCCLUSION
    output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.probeOcclusion);
    #else
    output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS));
    #endif
#endif

#if defined(DEBUG_DISPLAY) || (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    output.positionWS = vertexInput.positionWS;
    output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
#endif

    return output;
}

GBufferFragOutput BakedLitGBufferPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    LODFadeCrossFade(input.positionCS);

    half2 uv = input.uv0AndFogCoord.xy;
    half3 normalTS = SampleNormal(uv);
    half4 texColor = SampleBaseMap(uv);
    half alpha = AlphaDiscard(texColor.a * _BaseColor.a, _Cutoff);
    half3 color = texColor.rgb * _BaseColor.rgb;
    if (UseAlphaModulate())
        color = ApplyAlphaModulate(color, alpha);

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

#if defined(_DBUFFER)
    ApplyDecalToBaseColorAndNormal(input.positionCS, color, inputData.normalWS);
#endif

    InitializeBakedGIData(input, inputData);

    SurfaceData surfaceData = (SurfaceData) 0;
    surfaceData.albedo = color;
    surfaceData.alpha = alpha;
    surfaceData.occlusion = 1;

    if (ScreenSpaceOcclusionAvailable()) // No transparent-surface check needed: the GBuffer pass only renders opaque geometry
    {
        float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV, IsSurfaceTypeTransparent());
        surfaceData.occlusion = aoFactor.directAmbientOcclusion;
    }

    return PackGBuffersSurfaceData(surfaceData, inputData, float3(0, 0, 0), ReceiveShadows());
}

#endif
