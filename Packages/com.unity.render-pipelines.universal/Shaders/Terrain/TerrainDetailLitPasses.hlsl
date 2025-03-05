
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

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
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
    half4   Color           : TEXCOORD2; // Vertex Color
    half4   LightingFog     : TEXCOORD3; // Vertex Lighting, Fog Factor
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
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

    inputData.positionCS = input.PositionCS;
    inputData.normalWS = half3(0, 1, 0);
    inputData.viewDirectionWS = half3(0, 0, 1);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.ShadowCoords;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(input.PositionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = input.LightingFog.a;
    inputData.vertexLighting = input.LightingFog.rgb;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.PositionCS);
    inputData.positionWS = input.PositionWS;

#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        input.NormalWS.xyz,
        GetWorldSpaceNormalizeViewDir(inputData.positionWS),
        inputData.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.NormalWS.xyz);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

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

half4 UniversalTerrainLit(InputData inputData, SurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    half3 lighting = inputData.vertexLighting * MainLightRealtimeShadow(inputData.shadowCoord);
    #else
    half3 lighting = inputData.vertexLighting;
    #endif
    half4 color = half4(surfaceData.albedo, surfaceData.alpha);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
    {
        lighting += inputData.bakedGI;
    }

    color.rgb *= lighting;

    return color;
}

half4 UniversalTerrainLit(InputData inputData, half3 albedo, half alpha)
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
    OUTPUT_LIGHTMAP_UV(input.UV1, unity_LightmapST, output.staticLightmapUV);
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
    output.Color = input.Color;
    output.PositionCS = vertexInput.positionCS;

    // Shadow Coords
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.ShadowCoords = GetShadowCoord(vertexInput);
    #endif

    // Vertex Lighting
    half3 NormalWS = input.NormalOS;
    OUTPUT_SH4(vertexInput.positionWS, NormalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);
    Light mainLight = GetMainLight();
    half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
    half3 diffuseColor = half3(0, 0, 0);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
    {
        diffuseColor += LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);
    }

    // Adding !defined(USE_CLUSTER_LIGHT_LOOP): in Forward+ we can't possibly get the light list in a vertex shader.
    #if (defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)) && !USE_FORWARD_PLUS
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

    output.LightingFog.xyz = diffuseColor;

    // Fog factor
    output.LightingFog.w = ComputeFogFactor(output.PositionCS.z);

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
    half4 color = UniversalTerrainLit(inputData, tex.rgb, tex.a);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

FragmentOutput TerrainLitGBufferFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
    InputData inputData;
    InitializeInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TERRAIN(inputData);
    SurfaceData surfaceData;
    InitializeSurfaceData(tex.rgb, tex.a, surfaceData);
    half4 color = UniversalTerrainLit(inputData, tex.rgb, tex.a);

    return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingInvalid);
}
