#ifndef UNIVERSAL_SIMPLELIT_GBUFFER_PASS_INCLUDED
#define UNIVERSAL_SIMPLELIT_GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitFeatures.hlsl"

// Realtime shadows are never sampled when Receive Shadows is off at compile time;
// drop the vertex shadow-coord interpolator.
#if _RECEIVE_SHADOWS_OFF_STATICALLY_ENABLED
    #undef USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    #define USE_VERTEX_SHADOW_COORD_INTERPOLATOR 0
#endif

#if FEATURES_NORMALMAP
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR 1
#endif

// keep this file in sync with LitForwardPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

    float3 posWS                    : TEXCOORD1;    // xyz: posWS

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        half4 normal                   : TEXCOORD2;    // xyz: normal, w: viewDir.x
        half4 tangent                  : TEXCOORD3;    // xyz: tangent, w: viewDir.y
        half4 bitangent                : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
    #else
        half3  normal                  : TEXCOORD2;
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        URP_LIGHT_ACCUM3 vertexLighting            : TEXCOORD5; // xyz: vertex light
    #endif

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        float4 shadowCoord              : TEXCOORD6;
    #endif

#if USE_LIGHTMAP_UV_INTERPOLATOR
    float2 staticLightmapUV         : LIGHTMAPUV;
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    half3 vertexSH                  : VERTEXSH;
#endif
#if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    float2 dynamicLightmapUV        : DYNLIGHTMAPUV;
#endif

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion           : PROBEOCCLUSION;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.posWS;
    inputData.positionCS = input.positionCS;

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        half3 viewDirWS = half3(input.normal.w, input.tangent.w, input.bitangent.w);
        if (UseNormalMap())
        {
            inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangent.xyz, input.bitangent.xyz, input.normal.xyz));
        }
        else
        {
            inputData.normalWS = input.normal.xyz;
        }
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
        inputData.normalWS = input.normal;
    #endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS, UseNormalMap());
    viewDirWS = SafeNormalize(viewDirWS);

    inputData.viewDirectionWS = viewDirWS;

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        inputData.shadowCoord = ShadowCoordInterpolatorAvailable() ? input.shadowCoord : TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent());
    #else
        inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        inputData.vertexLighting = input.vertexLighting.xyz;
    #else
        inputData.vertexLighting = half3(0, 0, 0);
    #endif

    inputData.fogCoord = 0; // we don't apply fog in the gbuffer pass
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined(DEBUG_DISPLAY)
    #if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
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

    inputData.preExposureMultiplier = GetPreExposureMultiplier();
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
    #if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    giParams.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #ifdef USE_APV_PROBE_OCCLUSION
    giParams.vertexProbeOcclusion = input.probeOcclusion;
    #endif

    giParams.positionWS = inputData.positionWS;
    giParams.normalWS = inputData.normalWS;
    giParams.viewDirWS = inputData.viewDirectionWS;
    giParams.positionSS = inputData.positionCS.xy;
    giParams.isSurfaceTypeTransparent = IsSurfaceTypeTransparent();

    InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Simple Lighting) shader
Varyings LitPassVertexSimple(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.posWS.xyz = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
        output.normal = half4(normalInput.normalWS, viewDirWS.x);
        output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
        output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
        output.normal = NormalizeNormalPerVertex(normalInput.normalWS);
    #endif

#if USE_LIGHTMAP_UV_INTERPOLATOR
    output.staticLightmapUV = TransformLightmapUV(input.staticLightmapUV.xy, unity_LightmapST);
#endif
#if USE_VERTEX_SH_INTERPOLATOR
    #ifdef USE_APV_PROBE_OCCLUSION
    output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, output.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.probeOcclusion);
    #else
    output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, output.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS));
    #endif
#endif
#if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    output.dynamicLightmapUV = TransformLightmapUV(input.dynamicLightmapUV.xy, unity_DynamicLightmapST);
#endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        URP_LIGHT_ACCUM3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
        output.vertexLighting = vertexLight;
    #endif

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        output.shadowCoord = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
    #endif

    return output;
}

// Used for StandardSimpleLighting shader
GBufferFragOutput LitPassFragmentSimple(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeSimpleLitSurfaceData(input.uv, surfaceData,
        UseSpecGlossMap(), UseSpecularColor(), UseGlossinessFromBaseAlpha(),
        UseAlphaModulate(), UseNormalMap(), UseEmission());

    LODFadeCrossFade(input.positionCS);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData, IsSpecularSetup());
#endif

    InitializeBakedGIData(input, inputData);

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask, ReceiveShadows(), IsSurfaceTypeTransparent());
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    URP_LIGHT_ACCUM4 color = URP_LIGHT_ACCUM4(inputData.bakedGI * surfaceData.albedo + surfaceData.emission, surfaceData.alpha);

    return PackGBuffersSurfaceData(surfaceData, inputData, ClampExposed(inputData.preExposureMultiplier * color.rgb), ReceiveShadows());
};

#endif
