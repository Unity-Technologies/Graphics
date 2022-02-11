//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESCAPSULESHADOWS_CS_HLSL
#define SHADERVARIABLESCAPSULESHADOWS_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCapsuleShadows
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCapsuleShadows)
    float4 _OutputSize;
    float3 _CapsuleLightDir;
    float _CapsuleLightCosTheta;
    float _CapsuleLightTanTheta;
    float _CapsuleShadowRange;
    uint _CapsulePad0;
    uint _CapsulePad1;
    uint _FirstDepthMipOffsetX;
    uint _FirstDepthMipOffsetY;
    uint _CapsulesFullResolution;
    uint _CapsuleTileDebugMode;
CBUFFER_END


#endif
