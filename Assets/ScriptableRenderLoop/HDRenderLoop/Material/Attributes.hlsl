//-------------------------------------------------------------------------------------
// FragInput
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Debug/DebugViewMaterial.cs.hlsl"

struct FragInput
{
    float4 unPositionSS; // This is the position return by VPOS (That is name positionCS in PackedVarying), only xy is use
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float3 tangentToWorld[3];
    float4 vertexColor;

    // For velocity
    // Note: Z component is not use
    float4 positionCS; // This is the clip spae position. Warning, do not confuse with the value of positionCS in PackedVarying which is VPOS and store in unPositionSS
    float4 previousPositionCS;

    // For two sided lighting
    bool isFrontFace;
};

void GetVaryingsDataDebug(uint paramId, FragInput input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWVARYING_TEXCOORD0:
        result = float3(input.texCoord0 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD1:
        result = float3(input.texCoord1 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD2:
        result = float3(input.texCoord2 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD3:
        result = float3(input.texCoord3 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEWVARYING_VERTEX_TANGENT_WS:
        result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_BITANGENT_WS:
        result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_NORMAL_WS:
        result = input.tangentToWorld[2].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR:
        result = input.vertexColor.rgb; needLinearToSRGB = true;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR_ALPHA:
        result = input.vertexColor.aaa;
        break;
    }
}
