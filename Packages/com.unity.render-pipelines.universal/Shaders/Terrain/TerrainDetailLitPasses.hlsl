
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/ReceiveShadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"

struct Attributes
{
    float4  PositionOS  : POSITION;
    float2  UV0         : TEXCOORD0;
    float2  UV1         : TEXCOORD1;
    float3  NormalOS    : NORMAL;
    half4   Color       : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2  UV01            : TEXCOORD0; // UV0
    #if USE_LIGHTMAP_UV_INTERPOLATOR
    float2  staticLightmapUV : LIGHTMAPUV;
    #endif
    #if USE_VERTEX_SH_INTERPOLATOR
    half3   vertexSH        : VERTEXSH;
    #endif
    half4   Color           : TEXCOORD2; // Vertex Color
    URP_LIGHT_ACCUM3 VertexLighting : TEXCOORD3; // Vertex Lighting
    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    float4  ShadowCoords    : TEXCOORD4; // Shadow UVs
    #endif
    half4   NormalWS        : TEXCOORD5;
    float3  PositionWS      : TEXCOORD6;
    #ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion   : TEXCOORD7;
    #endif
    float4  PositionCS      : SV_POSITION; // Clip Position

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.preExposureMultiplier = GetPreExposureMultiplier();

    inputData.positionCS = input.PositionCS;
    inputData.normalWS = half3(0, 1, 0);
    inputData.viewDirectionWS = half3(0, 0, 1);

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        inputData.shadowCoord = ShadowCoordInterpolatorAvailable() ? input.ShadowCoords : TransformWorldToShadowCoord(input.PositionWS, IsSurfaceTypeTransparent());
    #else
        inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(input.PositionWS, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
    #endif

    inputData.vertexLighting = input.VertexLighting;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.PositionCS);
    inputData.positionWS = input.PositionWS;

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
        giParams.positionWS = inputData.positionWS;
        giParams.normalWS = input.NormalWS.xyz;
        giParams.viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
        giParams.positionSS = inputData.positionCS.xy;
        InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);
    }

    #if defined(DEBUG_DISPLAY)
    inputData.uv = input.UV01;
    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
    #endif
    #endif
}

void InitializeSurfaceData(half3 albedo, half alpha, out SurfaceData surfaceData)
{
    surfaceData = (SurfaceData)0;

    surfaceData.albedo = albedo;
    surfaceData.alpha = alpha;
    surfaceData.emission = half3(0, 0, 0);
    surfaceData.metallic = 0;
    surfaceData.occlusion = 0;
    surfaceData.smoothness = 1;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = half3(0, 0, 1);
}

URP_LIGHT_ACCUM4 UniversalTerrainLit(InputData inputData, SurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
    float4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return CompensateDebugColorForPreExposure(debugColor);
    }
    #endif

    URP_LIGHT_ACCUM3 lighting = ReceiveShadows() ? inputData.vertexLighting * SampleMainLightRealtimeShadow(inputData.shadowCoord, IsSurfaceTypeTransparent()) : inputData.vertexLighting;
    URP_LIGHT_ACCUM4 color = half4(surfaceData.albedo, surfaceData.alpha);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
    {
        lighting += inputData.bakedGI;
    }

    color.rgb *= lighting;

    return color;
}

URP_LIGHT_ACCUM4 UniversalTerrainLit(InputData inputData, half3 albedo, half alpha)
{
    SurfaceData surfaceData;
    InitializeSurfaceData(albedo, alpha, surfaceData);

    return UniversalTerrainLit(inputData, surfaceData);
}

Varyings TerrainLitVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // Vertex attributes
    output.UV01 = TRANSFORM_TEX(input.UV0, _MainTex);
    #if USE_LIGHTMAP_UV_INTERPOLATOR
    output.staticLightmapUV = LightmapAvailable() ? TransformLightmapUV(input.UV1.xy, unity_LightmapST) : float2(0, 0);
    #endif
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
    output.Color = input.Color;
    output.PositionCS = vertexInput.positionCS;

    // Shadow Coords
    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        output.ShadowCoords = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
    #endif

    // Vertex Lighting
    half3 NormalWS = input.NormalOS;
    #if USE_VERTEX_SH_INTERPOLATOR
    if (!LightmapAvailable())
    {
        #ifdef USE_APV_PROBE_OCCLUSION
        output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, NormalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.probeOcclusion);
        #else
        output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, NormalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS));
        #endif
    }
    else
    {
        output.vertexSH = half3(0, 0, 0);
    }
    #endif
    Light mainLight = GetMainLight();
    half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
    URP_LIGHT_ACCUM3 diffuseColor = half3(0, 0, 0);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
    {
        diffuseColor += LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);
    }

    // Adding !defined(USE_CLUSTER_LIGHT_LOOP): in Forward+ we can't possibly get the light list in a vertex shader.
    #if (defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)) && !defined(USE_CLUSTER_LIGHT_LOOP)
    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS))
    {
        int pixelLightCount = GetAdditionalLightsCount();
        for (int i = 0; i < pixelLightCount; ++i)
        {
            Light light = GetAdditionalLight(i, vertexInput.positionWS);
            half3 attenuatedLightColor = light.color * light.distanceAttenuation;
            diffuseColor += LightingLambert(attenuatedLightColor, light.direction, NormalWS);
        }
    }
    #endif

    output.VertexLighting = diffuseColor;

    output.NormalWS.xyz = NormalWS;
    output.PositionWS = vertexInput.positionWS;

    return output;
}

half4 TerrainLitForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData;
    InitializeInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TERRAIN(inputData);
    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
    URP_LIGHT_ACCUM4 color = UniversalTerrainLit(inputData, tex.rgb, tex.a);

    color.rgb = ClampExposed(inputData.preExposureMultiplier * BlendDistanceFog(color.rgb, input.PositionCS));
    return color;
}

#ifdef TERRAIN_GBUFFER

GBufferFragOutput TerrainLitGBufferFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
    InputData inputData;
    InitializeInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TERRAIN(inputData);
    SurfaceData surfaceData;
    InitializeSurfaceData(tex.rgb, tex.a, surfaceData);
    URP_LIGHT_ACCUM4 color = UniversalTerrainLit(inputData, tex.rgb, tex.a);

    return PackGBuffersSurfaceData(surfaceData, inputData, ClampExposed(inputData.preExposureMultiplier * color.rgb), ReceiveShadows());
}

#endif
