#ifndef UNIVERSAL_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_PASS_INCLUDED

#include "SimpleLitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
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

struct Attributes
{
    float4 positionOS    : POSITION;
    float3 normalOS      : NORMAL;
    float4 tangentOS     : TANGENT;
    float2 texcoord      : TEXCOORD0;
    float2 staticLightmapUV    : TEXCOORD1;
    float2 dynamicLightmapUV    : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

    float3 positionWS                  : TEXCOORD1;    // xyz: posWS

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        half4 normalWS                 : TEXCOORD2;    // xyz: normal, w: viewDir.x
        half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS              : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
    #else
        half3  normalWS                : TEXCOORD2;
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        URP_LIGHT_ACCUM4 fogFactorAndVertexLight  : TEXCOORD5; // x: fogFactor, yzw: vertex light
    #else
        half  fogFactor                 : TEXCOORD5;
    #endif

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        float4 shadowCoord             : TEXCOORD6;
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

    float4 positionCS                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;
#if defined(DEBUG_DISPLAY)
    inputData.positionCS = input.positionCS;
#endif

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
        half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
        if (UseNormalMap())
        {
            inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
            inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
        }
        else
        {
            inputData.normalWS = input.normalWS.xyz;
        }
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
        inputData.normalWS = input.normalWS;
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
        inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    #else
        inputData.vertexLighting = half3(0, 0, 0);
    #endif

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
    giParams.positionSS = input.positionCS.xy;
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

        half fogFactor = 0;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS.xyz = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
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
#if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    output.dynamicLightmapUV = TransformLightmapUV(input.dynamicLightmapUV.xy, unity_DynamicLightmapST);
#endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        URP_LIGHT_ACCUM3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
        output.fogFactorAndVertexLight = URP_LIGHT_ACCUM4(fogFactor, vertexLight);
    #else
        output.fogFactor = fogFactor;
    #endif

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        output.shadowCoord = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
    #endif

    return output;
}

// Used for StandardSimpleLighting shader
void LitPassFragmentSimple(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
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
    if (!IsSurfaceTypeTransparent())
        ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData, IsSpecularSetup());
#endif

    InitializeBakedGIData(input, inputData);

    URP_LIGHT_ACCUM4 color = UniversalFragmentBlinnPhong(inputData, surfaceData,
        UseSpecGlossMap() || UseSpecularColor(), UseAlphaPremultiply(),
        ReceiveShadows(), IsSurfaceTypeTransparent());
    color.rgb = ClampExposed(inputData.preExposureMultiplier * BlendDistanceFog(color.rgb, input.positionCS));
#if defined(_TRANSPARENT_RECEIVE_FOG)
    if (IsSurfaceTypeTransparent())
        color.rgb = MixVolumetricFog(color.rgb, color.a, _Blend, UseAlphaPremultiply(), input.positionCS);
#endif
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent());

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
