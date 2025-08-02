#ifndef SG_TERRAIN_PBRFORWARDPASS_INC
#define SG_TERRAIN_PBRFORWARDPASS_INC

#include "TerrainVert.hlsl"

void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    half3 SH = 0.0h;
    CalculateTerrainNormalWS(input, surfaceDescription, inputData);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, SH, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.sh;
    #endif
    #endif
}

void frag(PackedVaryings packedInput,
    out half4 color : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
    )
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    float2 sampleCoords = (unpacked.texCoord0.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb;
    normalOS = normalize(normalOS * 2.0 - 1.0);

    unpacked.normalWS = TransformObjectToWorldNormal(normalOS);

    #ifdef VARYINGS_NEED_TANGENT_WS
        float4 tangentOS = ConstructTerrainTangent(normalOS, float3(0.0, 0.0, 1.0));
        unpacked.tangentWS = float4(TransformObjectToWorldNormal(normalize(tangentOS.xyz)), tangentOS.w);
    #endif
#endif

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);
    #ifdef _TERRAIN_SG_ALPHA_CLIP
        AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
    #endif

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord0.xy);

    float3 specular = 0;
    float metallic = surfaceDescription.Metallic;

    half3 normalTS = half3(0, 0, 0);
    #if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
        normalTS = surfaceDescription.NormalTS;
    #endif

    SurfaceData surface;
    surface.albedo              = surfaceDescription.BaseColor;
    surface.metallic            = saturate(metallic);
    surface.specular            = specular;
    surface.smoothness          = saturate(surfaceDescription.Smoothness);
    surface.occlusion           = surfaceDescription.Occlusion;
    surface.emission            = surfaceDescription.Emission;
    surface.alpha               = 1.0;
    surface.normalTS            = normalTS;
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;

    surface.albedo = AlphaModulate(surface.albedo, surface.alpha);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(unpacked.positionCS, surface, inputData);
#endif

    color = UniversalFragmentPBR(inputData, surface);
    SplatmapFinalColor(color, inputData.fogCoord);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
    color = half4(color.rgb, 1.0h);
}

#endif
