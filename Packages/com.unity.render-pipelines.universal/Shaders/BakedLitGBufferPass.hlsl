#ifndef UNIVERSAL_BAKEDLIT_GBUFFER_PASS_INCLUDED
#define UNIVERSAL_BAKEDLIT_GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
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
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
    half3 normalWS : TEXCOORD2;

#if defined(_NORMALMAP)
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

#if defined(_NORMALMAP)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.shadowCoord = float4(0, 0, 0, 0);
    inputData.fogCoord = input.uv0AndFogCoord.z;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);

#if defined(DEBUG_DISPLAY)
#if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
#else
    inputData.vertexSH = input.vertexSH;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
#endif
#endif
}

void InitializeBakedGIData(Varyings input, inout InputData inputData)
{
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(input.positionWS),
        inputData.normalWS,
        input.viewDirWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif
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
#if defined(_FOG_FRAGMENT)
    output.uv0AndFogCoord.z = vertexInput.positionVS.z;
#else
    output.uv0AndFogCoord.z = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInput.normalWS;
#if defined(_NORMALMAP)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

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
    
#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif
    
    half2 uv = input.uv0AndFogCoord.xy;
#if defined(_NORMALMAP)
    half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap)).xyz;
#else
    half3 normalTS = half3(0, 0, 1);
#endif
    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half alpha = AlphaDiscard(texColor.a * _BaseColor.a, _Cutoff);
    half3 color = AlphaModulate(texColor.rgb * _BaseColor.rgb, alpha);
    
    InputData inputData;
    InitializeInputData(input, normalTS, inputData);
    
#if defined(_DBUFFER)
    ApplyDecalToBaseColorAndNormal(input.positionCS, color, inputData.normalWS);
#endif
    
    InitializeBakedGIData(input, inputData);
    
    SurfaceData surfaceData = (SurfaceData) 0;
    surfaceData.albedo = color;
    surfaceData.alpha = alpha;

#if defined(_SCREEN_SPACE_OCCLUSION) // GBuffer never has transparents
    float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
    surfaceData.occlusion = aoFactor.directAmbientOcclusion;
#else
    surfaceData.occlusion = 1;
#endif

    return PackGBuffersSurfaceData(surfaceData, inputData, float3(0, 0, 0));
}

#endif