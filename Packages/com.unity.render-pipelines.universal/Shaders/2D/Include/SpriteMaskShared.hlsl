
#if !defined(SPRITE_MASK_SHARED)
#define SPRITE_MASK_SHARED

#if defined(DEBUG_DISPLAY)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
#endif
// alpha below which a mask should discard a pixel, thereby preventing the stencil buffer from being marked with the Mask's presence
half  _Cutoff;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    float4 positionOS : POSITION;
    half2  texcoord : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half2  uv : TEXCOORD0;
};

Varyings MaskRenderingVertex(Attributes input)
{
    Varyings output;

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.texcoord;

    return output;
}

half4 MaskRenderingFragment(Varyings input) : SV_Target
{
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    // for masks: discard pixel if alpha falls below MaskingCutoff
    clip(c.a - _Cutoff);
    #if defined(DEBUG_DISPLAY)
    half4 debugColor = 0;
    SurfaceData2D surfaceData;
    InitializeSurfaceData(1.0f, 1.0f, surfaceData);
    InputData2D inputData;
    InitializeInputData(input.positionCS, input.uv, inputData);

    if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
       return debugColor;
    #endif
    return half4(1, 1, 1, 0.2);
}

#endif
