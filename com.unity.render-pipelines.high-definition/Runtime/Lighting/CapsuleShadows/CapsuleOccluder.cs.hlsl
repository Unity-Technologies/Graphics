//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CAPSULEOCCLUDER_CS_HLSL
#define CAPSULEOCCLUDER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCapsuleOccluders
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCapsuleOccluders)
    int _CapsuleOccluderCount;
    int _CapsuleOccluderUseEllipsoid;
    int _CapsuleOccluderPad0;
    int _CapsuleOccluderPad1;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleOccluderData
// PackingRules = Exact
struct CapsuleOccluderData
{
    float3 centerRWS;
    float radius;
    float3 axisDirWS;
    float offset;
    uint lightLayers;
    float range;
    float pad0;
    float pad1;
};


#endif
