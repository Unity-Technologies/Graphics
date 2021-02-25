
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

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
    float2  LightmapUV      : TEXCOORD1; // Lightmap UVs
    half4   Color           : TEXCOORD2; // Vertex Color
    half4   LightingFog     : TEXCOORD3; // Vetex Lighting, Fog Factor
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    float4  ShadowCoords    : TEXCOORD4; // Shadow UVs
    #endif
    #if defined(_DEBUG_SHADER)
    float3 positionWS : TEXCOORD5;
    #endif
    float4  PositionCS      : SV_POSITION; // Clip Position

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

InputData CreateInputData(Varyings input)
{
    InputData inputData = (InputData)0;

    inputData.normalWS = half3(0, 1, 0);
    inputData.viewDirectionWS = half3(0, 0, 1);
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = input.ShadowCoords;
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif
    inputData.fogCoord = input.LightingFog.a;
    inputData.vertexLighting = input.LightingFog.rgb;
    inputData.bakedGI = SampleLightmap(input.LightmapUV, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.PositionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
    inputData.normalTS = half3(0, 0, 1);

    #if defined(LIGHTMAP_ON)
    inputData.lightmapUV = input.LightmapUV;
    #else
    inputData.vertexSH = float3(1, 1, 1);
    #endif

    #if defined(_NORMALMAP)
    inputData.tangentMatrixWS;
    #endif

    #if defined(_DEBUG_SHADER)
    inputData.positionWS = input.positionWS;
    inputData.uv = input.UV01;
    #else
    inputData.positionWS = float3(0, 0, 0);
    #endif

    return inputData;
}

SurfaceData CreateSurfaceData(half3 albedo, half alpha)
{
    SurfaceData surfaceData;

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

    return surfaceData;
}

half4 UniversalTerrainLit(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_DEBUG_SHADER)
    half4 debugColor;

    if(CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
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

    if(IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
    {
        lighting += inputData.bakedGI;
    }

    color.rgb *= lighting;

    return color;
}

half4 UniversalTerrainLit(InputData inputData, half3 albedo, half alpha)
{
    SurfaceData surfaceData = CreateSurfaceData(albedo, alpha);

    return UniversalTerrainLit(inputData, surfaceData);
}

Varyings TerrainLitVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // Vertex attributes
    output.UV01 = TRANSFORM_TEX(input.UV0, _MainTex);
    output.LightmapUV = input.UV1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
    output.Color = input.Color;
    output.PositionCS = vertexInput.positionCS;

    // Shadow Coords
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    output.ShadowCoords = GetShadowCoord(vertexInput);
    #endif

    // Vertex Lighting
    half3 NormalWS = input.NormalOS;
    Light mainLight = GetMainLight();
    half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
    half3 diffuseColor = half3(0, 0, 0);

    if(IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
    {
        diffuseColor += LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);
    }

    #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
    if(IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS))
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

    #if defined(_DEBUG_SHADER)
    output.positionWS = vertexInput.positionWS;
    #endif

    return output;
}

half4 TerrainLitForwardFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData = CreateInputData(input);
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
    InputData inputData = CreateInputData(input);
    SurfaceData surfaceData = CreateSurfaceData(tex.rgb, tex.a);
    half4 color = UniversalTerrainLit(inputData, tex.rgb, tex.a);

    return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingInvalid);
}
