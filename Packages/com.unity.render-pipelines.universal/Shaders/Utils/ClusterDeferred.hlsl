#ifndef UNIVERSAL_CLUSTER_DEFERRED
#define UNIVERSAL_CLUSTER_DEFERRED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScaling.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 screenUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings VertexFullScreen(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    output.positionCS = float4(positionOS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar

    output.screenUV = output.positionCS.xyw;
    #if UNITY_UV_STARTS_AT_TOP
    output.screenUV.xy = output.screenUV.xy * float2(0.5, -0.5) + 0.5 * output.screenUV.z;
    #else
    output.screenUV.xy = output.screenUV.xy * 0.5 + 0.5 * output.screenUV.z;
    #endif

    output.screenUV.xy = DynamicScalingApplyScaleBias(output.screenUV.xy, float4(_RTHandleScale.xy, 0.0f, 0.0f));

    return output;
}

float4x4 _ScreenToWorld[2];

half3 DeferredLightContribution(Light light, InputData inputData, GBufferData gBufferData)
{
    #if defined(_LIGHT_LAYERS)
    UNITY_BRANCH if (!IsMatchingLightLayer(light.layerMask, gBufferData.meshRenderingLayers))
        return half3(0.0, 0.0, 0.0);
    #endif

    #if defined(_SIMPLELIT)
    {
        SurfaceData surfaceData = GBufferDataToSurfaceData(gBufferData);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        half3 diffuseColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);
        half smoothness = exp2(10 * surfaceData.smoothness + 1);
        half3 specularColor = LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);

        // TODO: if !defined(_SPECGLOSSMAP) && !defined(_SPECULAR_COLOR), force specularColor to 0 in gbuffer code
        return half3(diffuseColor * surfaceData.albedo + specularColor);
    }
    #elif defined(_LIT)
    {
        #if SHADER_API_MOBILE || SHADER_API_SWITCH
            // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
            bool materialSpecularHighlightsOff = false;
        #else
            bool materialSpecularHighlightsOff = (gBufferData.materialFlags & kMaterialFlagSpecularHighlightsOff);
        #endif

        BRDFData brdfData = GBufferDataToBRDFData(gBufferData);
        return half3(LightingPhysicallyBased(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, materialSpecularHighlightsOff));
    }
    #endif

    return half3(0.0, 0.0, 0.0);
}

half4 DeferredShadingClustered(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screen_uv = (input.screenUV.xy / input.screenUV.z);

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    float2 undistorted_screen_uv = screen_uv;
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        screen_uv = input.positionCS.xy * _ScreenSize.zw;
    }
    #endif

    GBufferData gBufferData = UnpackGBuffers(input.positionCS.xy);

    half3 color = 0.0;
    half alpha = 1.0;

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        input.positionCS.xy = undistorted_screen_uv * _ScreenSize.xy;
    }
    #endif

    float4 posWS = mul(_ScreenToWorld[SLICE_ARRAY_INDEX], float4(input.positionCS.xy, gBufferData.depth, 1.0));
    posWS.xyz *= rcp(posWS.w);

    InputData inputData = (InputData)0;

    inputData.positionWS = posWS.xyz;
    inputData.normalWS = gBufferData.normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(posWS.xyz);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screen_uv);

    #if defined(_SCREEN_SPACE_OCCLUSION)
        // What we want is really to apply the minimum occlusion value between the baked occlusion from surfaceDataOcclusion and real-time occlusion from SSAO.
        // But we already applied the baked occlusion during gbuffer pass, so we have to cancel it out here.
        // We must also avoid divide-by-0 that the reciprocal can generate.
        half surfaceDataOcclusion = gBufferData.occlusion;
        half occlusion = aoFactor.indirectAmbientOcclusion < surfaceDataOcclusion ? aoFactor.indirectAmbientOcclusion * rcp(surfaceDataOcclusion) : 1.0;
        alpha = occlusion;
    #endif

    // Main light
    Light mainLight = GetMainLight();
    mainLight.distanceAttenuation = 1.0;
    bool materialReceiveShadowsOff = (gBufferData.materialFlags & kMaterialFlagReceiveShadowsOff) != 0;
    UNITY_BRANCH if (!materialReceiveShadowsOff)
    {
        #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
            float4 shadowCoord = float4(screen_uv, 0.0, 1.0);
        #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
            float4 shadowCoord = TransformWorldToShadowCoord(posWS.xyz);
        #else
            float4 shadowCoord = float4(0, 0, 0, 0);
        #endif
        mainLight.shadowAttenuation = MainLightShadow(shadowCoord, posWS.xyz, gBufferData.shadowMask, _MainLightOcclusionProbes);
    }

    #if defined(_LIGHT_COOKIES)
        half3 cookieColor = SampleMainLightCookie(posWS.xyz);
        mainLight.color *= half3(cookieColor);
    #endif

    #if defined(_SCREEN_SPACE_OCCLUSION)
        mainLight.shadowAttenuation *= aoFactor.directAmbientOcclusion;
    #endif

    color += DeferredLightContribution(mainLight, inputData, gBufferData);

    // Additional light loop
    // We do additional directional lights last because otherwise FXC complains...
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, gBufferData.shadowMask, aoFactor);

        UNITY_BRANCH if (materialReceiveShadowsOff)
        {
            light.shadowAttenuation = 1.0;
        }

        color += DeferredLightContribution(light, inputData, gBufferData);
    LIGHT_LOOP_END

    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData, gBufferData.shadowMask, aoFactor);

        UNITY_BRANCH if (materialReceiveShadowsOff)
        {
            light.shadowAttenuation = 1.0;
        }

        color += DeferredLightContribution(light, inputData, gBufferData);
    }

    return half4(color, alpha);
}
#endif //UNIVERSAL_CLUSTER_DEFERRED
