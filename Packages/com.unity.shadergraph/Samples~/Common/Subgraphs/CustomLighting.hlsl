#ifndef CUSTOM_LIGHTING
#define CUSTOM_LIGHTING

void MainLight_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
    direction = normalize(float3(-0.5,0.5,-0.5));
    color = float3(1,1,1);
    shadowAtten = 1;
#else
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
        float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
        Light mainLight = GetMainLight(shadowCoord);
        direction = mainLight.direction;
        color = mainLight.color;
        shadowAtten = mainLight.shadowAttenuation;
    #else
        direction = normalize(float3(-0.5, 0.5, -0.5));
        color = float3(1, 1, 1);
        shadowAtten = 1;
    #endif
#endif
}

void MainLight_half(half3 worldPos, out half3 direction, out half3 color, out half shadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
    direction = normalize(half3(-0.5,0.5,0.5));
    color = half3(1,1,1);
    shadowAtten = 1;
#else
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
        half4 shadowCoord = TransformWorldToShadowCoord(worldPos);
        Light mainLight = GetMainLight(shadowCoord);
        direction = mainLight.direction;
        color = mainLight.color;
        shadowAtten = mainLight.shadowAttenuation;
    #else
        direction = normalize(float3(-0.5, 0.5, -0.5));
        color = float3(1, 1, 1);
        shadowAtten = 1;
    #endif
#endif
}

#ifndef SHADERGRAPH_PREVIEW
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)

    // This function gets additional light data and calculates realtime shadows
    Light GetAdditionalLightCustom(int pixelLightIndex, float3 worldPosition) {
        // Convert the pixel light index to the light data index
        #if USE_FORWARD_PLUS
            int lightIndex = pixelLightIndex;
        #else
            int lightIndex = GetPerObjectLightIndex(pixelLightIndex);
        #endif
        // Call the URP additional light algorithm. This will not calculate shadows, since we don't pass a shadow mask value
        Light light = GetAdditionalPerObjectLight(lightIndex, worldPosition);
        // Manually set the shadow attenuation by calculating realtime shadows
        light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, worldPosition, light.direction);
        return light;
    }
    #endif
#endif

void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView,
    float MainDiffuse, float3 MainSpecular, float3 MainColor,
    out float Diffuse, out float3 Specular, out float3 Color) {

    Diffuse = MainDiffuse;
    Specular = MainSpecular;
    Color = MainColor * (MainDiffuse + MainSpecular);

#ifndef SHADERGRAPH_PREVIEW
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
    
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            // for Foward+ LIGHT_LOOP_BEGIN macro uses inputData.normalizedScreenSpaceUV and inputData.positionWS
            InputData inputData = (InputData)0;
            float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
            inputData.positionWS = WorldPosition;
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLightCustom(lightIndex, WorldPosition);
            float NdotL = saturate(dot(WorldNormal, light.direction));
            float atten = light.distanceAttenuation * light.shadowAttenuation;
            float thisDiffuse = atten * NdotL;
            float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
            Diffuse += thisDiffuse;
            Specular += thisSpecular;
            #if defined(_LIGHT_COOKIES)
                float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
                light.color *= cookieColor;
            #endif
            Color += light.color * (thisDiffuse + thisSpecular);
        LIGHT_LOOP_END
        float total = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        Color = total <= 0 ? MainColor : Color / total;
    #endif
#endif
}

void AddAdditionalLights_half(half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView,
    half MainDiffuse, half3 MainSpecular, half3 MainColor,
    out half Diffuse, out half3 Specular, out half3 Color) {

    Diffuse = MainDiffuse;
    Specular = MainSpecular;
    Color = MainColor * (MainDiffuse + MainSpecular);

#ifndef SHADERGRAPH_PREVIEW
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            // for Foward+ LIGHT_LOOP_BEGIN macro uses inputData.normalizedScreenSpaceUV and inputData.positionWS
            InputData inputData = (InputData)0;
            float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
            inputData.positionWS = WorldPosition;
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLightCustom(lightIndex, WorldPosition);
            half NdotL = saturate(dot(WorldNormal, light.direction));
            half atten = light.distanceAttenuation * light.shadowAttenuation;
            half thisDiffuse = atten * NdotL;
            half3 thisSpecular = LightingSpecular(thisDiffuse * light.color, light.direction, WorldNormal, WorldView, 1, Smoothness);
            Diffuse += thisDiffuse;
            Specular += thisSpecular;
            #if defined(_LIGHT_COOKIES)
                half3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
                light.color *= cookieColor;
            #endif
            Color += light.color * (thisDiffuse + thisSpecular);
        LIGHT_LOOP_END
        //needs to be float to avoid precision issues
        float total = Diffuse + dot(Specular, half3(0.333, 0.333, 0.333));
        Color = total <= 0 ? MainColor : Color / total;
    #endif
#endif
}

void AddAdditionalLightsSimple_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView,
    float MainDiffuse, float3 MainSpecular, float3 MainColor,
    out float Diffuse, out float3 Specular, out float3 Color) {

    Diffuse = MainDiffuse;
    Specular = MainSpecular;
    Color = MainColor * (MainDiffuse + MainSpecular);

#ifndef SHADERGRAPH_PREVIEW
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            // for Foward+ LIGHT_LOOP_BEGIN macro uses inputData.normalizedScreenSpaceUV and inputData.positionWS
            InputData inputData = (InputData)0;
            float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
            inputData.positionWS = WorldPosition;
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLightCustom(lightIndex, WorldPosition);
            float NdotL = saturate(dot(WorldNormal, light.direction));
            float atten = light.distanceAttenuation * light.shadowAttenuation;
            float thisDiffuse = atten * NdotL;
            Diffuse += thisDiffuse;
            Color += light.color * thisDiffuse;
        LIGHT_LOOP_END
        float total = Diffuse;
        Color = total <= 0 ? MainColor : Color / total;
    #endif
#endif
}

void AddAdditionalLightsSimple_half(half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView,
    half MainDiffuse, half3 MainSpecular, half3 MainColor,
    out half Diffuse, out half3 Specular, out half3 Color) {

    Diffuse = MainDiffuse;
    Specular = MainSpecular;
    Color = MainColor * (MainDiffuse + MainSpecular);

#ifndef SHADERGRAPH_PREVIEW
    #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)

        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            // for Foward+ LIGHT_LOOP_BEGIN macro uses inputData.normalizedScreenSpaceUV and inputData.positionWS
            InputData inputData = (InputData)0;
            float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
            inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
            inputData.positionWS = WorldPosition;
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLightCustom(lightIndex, WorldPosition);
            half NdotL = saturate(dot(WorldNormal, light.direction));
            half atten = light.distanceAttenuation * light.shadowAttenuation;
            half thisDiffuse = atten * NdotL;
            Diffuse += thisDiffuse;
            Color += light.color * thisDiffuse;
        LIGHT_LOOP_END
        //needs to be float to avoid precision issues
        float total = Diffuse;
        Color = total <= 0 ? MainColor : Color / total;
    #endif
#endif
}

#endif