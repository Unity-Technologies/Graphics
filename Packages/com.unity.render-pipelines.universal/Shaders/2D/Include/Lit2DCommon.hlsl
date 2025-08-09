#ifndef _LIT_2D_COMMON
#define _LIT_2D_COMMON

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

TEXTURE2D(_MaskTex);
SAMPLER(sampler_MaskTex);

Varyings CommonLitVertex(Attributes input)
{
    Varyings o = (Varyings) 0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(input.positionOS);
    #if defined(DEBUG_DISPLAY)
        o.positionWS = TransformObjectToWorld(input.positionOS);
    #endif
    o.uv = input.uv;
    o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
    return o;
}

half4 CommonLitFragment(Varyings input, half4 color)
{
    const half4 main = color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);

    SurfaceData2D surfaceData;
    InputData2D inputData;

    InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
    InitializeInputData(input.uv, input.lightingUV, inputData);

#if defined(DEBUG_DISPLAY)
    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
#endif

    return CombinedShapeLightShared(surfaceData, inputData);
}

#endif // _LIT_2D_COMMON
