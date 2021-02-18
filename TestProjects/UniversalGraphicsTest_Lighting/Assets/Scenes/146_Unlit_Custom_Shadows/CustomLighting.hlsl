//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
   Direction = half3(0.5, 0.5, 0);
   Color = 1;
   DistanceAtten = 1;
   ShadowAtten = 1;
#else
   half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
   Light mainLight = GetMainLight(shadowCoord);
   Direction = mainLight.direction;
   Color = mainLight.color;
   DistanceAtten = mainLight.distanceAttenuation;
   ShadowAtten = mainLight.shadowAttenuation;
#endif
}

void MainLight_half(out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
   Direction = half3(0.5, 0.5, 0);
   Color = 1;
   DistanceAtten = 1;
   ShadowAtten = 1;
#else
   Light mainLight = GetMainLight();
   Direction = mainLight.direction;
   Color = mainLight.color;
   DistanceAtten = mainLight.distanceAttenuation;
   ShadowAtten = mainLight.shadowAttenuation;
#endif
}

void MainLightFull_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
   #ifdef SHADERGRAPH_PREVIEW
   Direction = half3(0.5, 0.5, 0);
   Color = 1;
   DistanceAtten = 1;
   ShadowAtten = 1;
   #else
   #ifdef SHADOWS_SCREEN
   half4 clipPos = TransformWorldToHClip(WorldPos);
   half4 shadowCoord = ComputeScreenPos(clipPos);
   #else
   half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
   #endif
   Light mainLight = GetMainLight(shadowCoord);
   Direction = mainLight.direction;
   Color = mainLight.color;
   DistanceAtten = mainLight.distanceAttenuation;
   ShadowAtten = mainLight.shadowAttenuation;
   #endif
}

void SampleSH_half(half3 normalWS, out half3 Ambient)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    Ambient = max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

void SampleSH_float(float3 normalWS, out float3 Ambient)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    Ambient = max(float3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}