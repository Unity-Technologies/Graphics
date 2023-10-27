#ifndef UNIVERSAL_STENCIL_DEFERRED
#define UNIVERSAL_STENCIL_DEFERRED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScaling.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

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

#if defined(_SPOT)
float4 _SpotLightScale;
float4 _SpotLightBias;
float4 _SpotLightGuard;
#endif

Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;

    #if defined(_SPOT)
    // Spot lights have an outer angle than can be up to 180 degrees, in which case the shape
    // becomes a capped hemisphere. There is no affine transforms to handle the particular cone shape,
    // so instead we will adjust the vertices positions in the vertex shader to get the tighest fit.
    [flatten] if (any(positionOS.xyz))
    {
        // The hemisphere becomes the rounded cap of the cone.
        positionOS.xyz = _SpotLightBias.xyz + _SpotLightScale.xyz * positionOS.xyz;
        positionOS.xyz = normalize(positionOS.xyz) * _SpotLightScale.w;
        // Slightly inflate the geometry to fit the analytic cone shape.
        // We want the outer rim to be expanded along xy axis only, while the rounded cap is extended along all axis.
        positionOS.xyz = (positionOS.xyz - float3(0, 0, _SpotLightGuard.w)) * _SpotLightGuard.xyz + float3(0, 0, _SpotLightGuard.w);
    }
    #endif

    #if defined(_DIRECTIONAL) || defined(_FOG) || defined(_CLEAR_STENCIL_PARTIAL) || (defined(_SSAO_ONLY) && defined(_SCREEN_SPACE_OCCLUSION))
        // Full screen render using a large triangle.
        output.positionCS = float4(positionOS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar
    #elif defined(_SSAO_ONLY) && !defined(_SCREEN_SPACE_OCCLUSION)
        // Deferred renderer does not know whether there is a SSAO feature or not at the C# scripting level.
        // However, this is known at the shader level because of the shader keyword SSAO feature enables.
        // If the keyword was not enabled, discard the SSAO_only pass by rendering the geometry outside the screen.
        output.positionCS = float4(positionOS.xy, -2, 1.0); // Force triangle to be discarded
    #else
        // Light shape geometry is projected as normal.
        VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
        output.positionCS = vertexInput.positionCS;
    #endif

    output.screenUV = output.positionCS.xyw;
    #if UNITY_UV_STARTS_AT_TOP
    output.screenUV.xy = output.screenUV.xy * float2(0.5, -0.5) + 0.5 * output.screenUV.z;
    #else
    output.screenUV.xy = output.screenUV.xy * 0.5 + 0.5 * output.screenUV.z;
    #endif

    output.screenUV.xy = DynamicScalingApplyScaleBias(output.screenUV.xy, float4(_RTHandleScale.xy, 0.0f, 0.0f));

    return output;
}

TEXTURE2D_X(_CameraDepthTexture);
TEXTURE2D_X_HALF(_GBuffer0);
TEXTURE2D_X_HALF(_GBuffer1);
TEXTURE2D_X_HALF(_GBuffer2);

#if _RENDER_PASS_ENABLED

#define GBUFFER0 0
#define GBUFFER1 1
#define GBUFFER2 2
#define GBUFFER3 3

FRAMEBUFFER_INPUT_HALF(GBUFFER0);
FRAMEBUFFER_INPUT_HALF(GBUFFER1);
FRAMEBUFFER_INPUT_HALF(GBUFFER2);
FRAMEBUFFER_INPUT_FLOAT(GBUFFER3);
#if OUTPUT_SHADOWMASK
#define GBUFFER4 4
FRAMEBUFFER_INPUT_HALF(GBUFFER4);
#endif
#else
#ifdef GBUFFER_OPTIONAL_SLOT_1
TEXTURE2D_X_HALF(_GBuffer4);
#endif
#endif

#if defined(GBUFFER_OPTIONAL_SLOT_2) && _RENDER_PASS_ENABLED
TEXTURE2D_X_HALF(_GBuffer5);
#elif defined(GBUFFER_OPTIONAL_SLOT_2)
TEXTURE2D_X(_GBuffer5);
#endif
#ifdef GBUFFER_OPTIONAL_SLOT_3
TEXTURE2D_X(_GBuffer6);
#endif

float4x4 _ScreenToWorld[2];
SamplerState my_point_clamp_sampler;

float3 _LightPosWS;
half3 _LightColor;
half4 _LightAttenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation *for SpotLights)
half3 _LightDirection;   // directional/spotLights support
half4 _LightOcclusionProbInfo;
int _LightFlags;
int _ShadowLightIndex;
uint _LightLayerMask;
int _CookieLightIndex;

half4 FragWhite(Varyings input) : SV_Target
{
    return half4(1.0, 1.0, 1.0, 1.0);
}

half4 SampleAdditionalLightCookieDeferred(int perObjectLightIndex, float3 samplePositionWS)
{
    float4 cookieUvRect = GetLightCookieAtlasUVRect(perObjectLightIndex);
    float4x4 worldToLight = GetLightCookieWorldToLightMatrix(perObjectLightIndex);
    float2 cookieUv = float2(0,0);

    #if defined(_SPOT)
        cookieUv = ComputeLightCookieUVSpot(worldToLight, samplePositionWS, cookieUvRect);
    #endif
    #if defined(_POINT)
        cookieUv = ComputeLightCookieUVPoint(worldToLight, samplePositionWS, cookieUvRect);
    #endif
    #if defined(_DIRECTIONAL)
        cookieUv = ComputeLightCookieUVDirectional(worldToLight, samplePositionWS, cookieUvRect, URP_TEXTURE_WRAP_MODE_REPEAT);
    #endif
    half4 cookieColor = SampleAdditionalLightsCookieAtlasTexture(cookieUv);
    cookieColor = half4(IsAdditionalLightsCookieAtlasTextureRGBFormat() ? cookieColor.rgb
                        : IsAdditionalLightsCookieAtlasTextureAlphaFormat() ? cookieColor.aaa
                        : cookieColor.rrr, 1);
    return cookieColor;

}

Light GetStencilLight(float3 posWS, float2 screen_uv, half4 shadowMask, uint materialFlags)
{
    Light unityLight;

    bool materialReceiveShadowsOff = (materialFlags & kMaterialFlagReceiveShadowsOff) != 0;

    uint lightLayerMask =_LightLayerMask;

    #if defined(_DIRECTIONAL)
        #if defined(_DEFERRED_MAIN_LIGHT)
            unityLight = GetMainLight();
            // unity_LightData.z is set per mesh for forward renderer, we cannot cull lights in this fashion with deferred renderer.
            unityLight.distanceAttenuation = 1.0;

            if (!materialReceiveShadowsOff)
            {
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    float4 shadowCoord = float4(screen_uv, 0.0, 1.0);
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(posWS.xyz);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif
                unityLight.shadowAttenuation = MainLightShadow(shadowCoord, posWS.xyz, shadowMask, _MainLightOcclusionProbes);
            }

            #if defined(_LIGHT_COOKIES)
                real3 cookieColor = SampleMainLightCookie(posWS);
                unityLight.color *= half3(cookieColor);
            #endif
        #else
            unityLight.direction = _LightDirection;
            unityLight.distanceAttenuation = 1.0;
            unityLight.shadowAttenuation = 1.0;
            unityLight.color = _LightColor.rgb;
            unityLight.layerMask = lightLayerMask;

            if (!materialReceiveShadowsOff)
            {
                #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                    unityLight.shadowAttenuation = AdditionalLightShadow(_ShadowLightIndex, posWS.xyz, _LightDirection, shadowMask, _LightOcclusionProbInfo);
                #endif
            }

        	#ifdef _LIGHT_COOKIES
                // Enable/disable is done toggling the keyword _LIGHT_COOKIES, but we could do a "static if" instead if required.
                // if(_CookieLightIndex >= 0)
                {
                    half3 cookieColor = SampleAdditionalLightCookieDeferred(_CookieLightIndex, posWS).xyz;
                    unityLight.color *= cookieColor;
                }
            #endif
        #endif
    #else
        PunctualLightData light;
        light.posWS = _LightPosWS;
        light.radius2 = 0.0; //  only used by tile-lights.
        light.color = float4(_LightColor, 0.0);
        light.attenuation = _LightAttenuation;
        light.spotDirection = _LightDirection;
        light.occlusionProbeInfo = _LightOcclusionProbInfo;
        light.flags = _LightFlags;
        light.layerMask = lightLayerMask;
        unityLight = UnityLightFromPunctualLightDataAndWorldSpacePosition(light, posWS.xyz, shadowMask, _ShadowLightIndex, materialReceiveShadowsOff);

        #ifdef _LIGHT_COOKIES
            // Enable/disable is done toggling the keyword _LIGHT_COOKIES, but we could do a "static if" instead if required.
            // if(_CookieLightIndex >= 0)
            {
                half3 cookieColor = SampleAdditionalLightCookieDeferred(_CookieLightIndex, posWS).xyz;
                unityLight.color *= cookieColor;
            }
        #endif
    #endif
    return unityLight;
}

half4 DeferredShading(Varyings input) : SV_Target
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

    half4 shadowMask = 1.0;

    #if _RENDER_PASS_ENABLED
    float d        = LOAD_FRAMEBUFFER_INPUT(GBUFFER3, input.positionCS.xy).x;
    half4 gbuffer0 = LOAD_FRAMEBUFFER_INPUT(GBUFFER0, input.positionCS.xy);
    half4 gbuffer1 = LOAD_FRAMEBUFFER_INPUT(GBUFFER1, input.positionCS.xy);
    half4 gbuffer2 = LOAD_FRAMEBUFFER_INPUT(GBUFFER2, input.positionCS.xy);
    #if defined(_DEFERRED_MIXED_LIGHTING)
    shadowMask = LOAD_FRAMEBUFFER_INPUT(GBUFFER4, input.positionCS.xy);
    #endif
    #else
    // Using SAMPLE_TEXTURE2D is faster than using LOAD_TEXTURE2D on iOS platforms (5% faster shader).
    // Possible reason: HLSLcc upcasts Load() operation to float, which doesn't happen for Sample()?
    float d        = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, my_point_clamp_sampler, screen_uv, 0).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
    half4 gbuffer0 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer1 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, my_point_clamp_sampler, screen_uv, 0);
    half4 gbuffer2 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, screen_uv, 0);
    #if defined(_DEFERRED_MIXED_LIGHTING)
    shadowMask = SAMPLE_TEXTURE2D_X_LOD(MERGE_NAME(_, GBUFFER_SHADOWMASK), my_point_clamp_sampler, screen_uv, 0);
    #endif
    #endif

    half surfaceDataOcclusion = gbuffer1.a;
    uint materialFlags = UnpackMaterialFlags(gbuffer0.a);

    half3 color = 0.0.xxx;
    half alpha = 1.0;

    #if defined(_DEFERRED_MIXED_LIGHTING)
    // If both lights and geometry are static, then no realtime lighting to perform for this combination.
    [branch] if ((_LightFlags & materialFlags) == kMaterialFlagSubtractiveMixedLighting)
        return half4(color, alpha); // Cannot discard because stencil must be updated.
    #endif

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
     UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
     {
        input.positionCS.xy = undistorted_screen_uv * _ScreenSize.xy;
     }
    #endif

    #if defined(USING_STEREO_MATRICES)
    int eyeIndex = unity_StereoEyeIndex;
    #else
    int eyeIndex = 0;
    #endif
    float4 posWS = mul(_ScreenToWorld[eyeIndex], float4(input.positionCS.xy, d, 1.0));
    posWS.xyz *= rcp(posWS.w);

    Light unityLight = GetStencilLight(posWS.xyz, screen_uv, shadowMask, materialFlags);

    #ifdef _LIGHT_LAYERS
    float4 renderingLayers = SAMPLE_TEXTURE2D_X_LOD(MERGE_NAME(_, GBUFFER_LIGHT_LAYERS), my_point_clamp_sampler, screen_uv, 0);
    uint meshRenderingLayers = DecodeMeshRenderingLayer(renderingLayers.r);
    [branch] if (!IsMatchingLightLayer(unityLight.layerMask, meshRenderingLayers))
        return half4(color, alpha); // Cannot discard because stencil must be updated.
    #endif

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screen_uv);
        unityLight.color *= aoFactor.directAmbientOcclusion;
        #if defined(_DIRECTIONAL) && defined(_DEFERRED_FIRST_LIGHT)
        // What we want is really to apply the mininum occlusion value between the baked occlusion from surfaceDataOcclusion and real-time occlusion from SSAO.
        // But we already applied the baked occlusion during gbuffer pass, so we have to cancel it out here.
        // We must also avoid divide-by-0 that the reciprocal can generate.
        half occlusion = aoFactor.indirectAmbientOcclusion < surfaceDataOcclusion ? aoFactor.indirectAmbientOcclusion * rcp(surfaceDataOcclusion) : 1.0;
        alpha = occlusion;
        #endif
    #endif

    InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);

    #if defined(_LIT)
        #if SHADER_API_MOBILE || SHADER_API_SWITCH
        // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
        bool materialSpecularHighlightsOff = false;
        #else
        bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);
        #endif
        BRDFData brdfData = BRDFDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
        color = LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS, materialSpecularHighlightsOff);
    #elif defined(_SIMPLELIT)
        SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, kLightingSimpleLit);
        half3 attenuatedLightColor = unityLight.color * (unityLight.distanceAttenuation * unityLight.shadowAttenuation);
        half3 diffuseColor = LightingLambert(attenuatedLightColor, unityLight.direction, inputData.normalWS);
        half smoothness = exp2(10 * surfaceData.smoothness + 1);
        half3 specularColor = LightingSpecular(attenuatedLightColor, unityLight.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);

        // TODO: if !defined(_SPECGLOSSMAP) && !defined(_SPECULAR_COLOR), force specularColor to 0 in gbuffer code
        color = diffuseColor * surfaceData.albedo + specularColor;
    #endif

    return half4(color, alpha);
}

half4 FragFog(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if _RENDER_PASS_ENABLED
        float d = LOAD_FRAMEBUFFER_INPUT(GBUFFER3, input.positionCS.xy).x;
    #else
        float d = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionCS.xy).x;
    #endif
    float eye_z = LinearEyeDepth(d, _ZBufferParams);
    float clip_z = UNITY_MATRIX_P[2][2] * -eye_z + UNITY_MATRIX_P[2][3];
    half fogFactor = ComputeFogFactor(clip_z);
    half fogIntensity = ComputeFogIntensity(fogFactor);
    return half4(unity_FogColor.rgb, fogIntensity);
}

half4 FragSSAOOnly(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screen_uv = (input.screenUV.xy / input.screenUV.z);
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screen_uv);
    half surfaceDataOcclusion = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, my_point_clamp_sampler, screen_uv, 0).a;
    // What we want is really to apply the mininum occlusion value between the baked occlusion from surfaceDataOcclusion and real-time occlusion from SSAO.
    // But we already applied the baked occlusion during gbuffer pass, so we have to cancel it out here.
    // We must also avoid divide-by-0 that the reciprocal can generate.
    half occlusion = aoFactor.indirectAmbientOcclusion < surfaceDataOcclusion ? aoFactor.indirectAmbientOcclusion * rcp(surfaceDataOcclusion) : 1.0;
    return half4(0.0, 0.0, 0.0, occlusion);
}
#endif //UNIVERSAL_STENCIL_DEFERRED
