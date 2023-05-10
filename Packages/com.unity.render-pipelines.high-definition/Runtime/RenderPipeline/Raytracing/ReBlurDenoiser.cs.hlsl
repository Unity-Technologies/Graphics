//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef REBLURDENOISER_CS_HLSL
#define REBLURDENOISER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesReBlur
// PackingRules = Exact
CBUFFER_START(ShaderVariablesReBlur)
    float4 _ReBlurPreBlurRotator;
    float4 _ReBlurBlurRotator;
    float4 _ReBlurPostBlurRotator;
    float4 _HistorySizeAndScale;
    float2 _ReBlurPadding;
    float _ReBlurDenoiserRadius;
    float _ReBlurAntiFlickeringStrength;
    float _ReBlurHistoryValidity;
    float _PaddingRBD0;
    float _PaddingRBD1;
    float _PaddingRBD2;
CBUFFER_END


#endif
