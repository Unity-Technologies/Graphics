//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESCAPSULESHADOWS_CS_HLSL
#define SHADERVARIABLESCAPSULESHADOWS_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCapsuleShadows
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCapsuleShadows)
    float4 _CapsuleRenderSize;
    float3 _CapsuleLightDir;
    float _CapsuleLightCosTheta;
    float _CapsuleLightTanTheta;
    float _CapsuleShadowRange;
    uint _CapsulePad0;
    uint _CapsulePad1;
    float4 _CapsuleUpscaledSize;
    float4 _DepthPyramidSize;
    uint _FirstDepthMipOffsetX;
    uint _FirstDepthMipOffsetY;
    uint _CapsuleTileDebugMode;
    uint _CapsulePad2;
CBUFFER_END


#endif
