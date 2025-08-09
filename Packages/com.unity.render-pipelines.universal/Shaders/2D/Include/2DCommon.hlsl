#ifndef _2D_COMMON
#define _2D_COMMON

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

Varyings CommonUnlitVertex(Attributes input)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    o.positionCS = TransformObjectToHClip(input.positionOS);
#if defined(DEBUG_DISPLAY)
    o.positionWS = TransformObjectToWorld(input.positionOS);
#endif
    o.uv = input.uv;
    return o;
}

half4 CommonUnlitFragment(Varyings input, half4 color)
{
    float4 mainTex = color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

#if defined(DEBUG_DISPLAY)
    SurfaceData2D surfaceData;
    InputData2D inputData;
    half4 debugColor = 0;

    InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
    InitializeInputData(input.uv, inputData);
    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);

    if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
    {
        return debugColor;
    }
#endif

    return mainTex;
}

#endif // _2D_COMMON
