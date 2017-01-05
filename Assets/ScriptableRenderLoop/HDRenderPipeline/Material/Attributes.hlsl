//-------------------------------------------------------------------------------------
// FragInputs
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

#include "Assets/ScriptableRenderLoop/HDRenderPipeline/Debug/DebugViewMaterial.cs.hlsl"

struct FragInputs
{
    // Contain value return by SV_POSITION (That is name positionCS in PackedVarying).
    // xy: unormalized screen position (offset by 0.5), z: device depth, w: depth in view space
    // Note: SV_POSITION is the result of the clip space position provide to the vertex shaders that is transform by the viewport
    float4 unPositionSS; // In case depth offset is use, positionWS.w is equal to depth offset
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float3 tangentToWorld[3];
    float4 vertexColor;

    // CAUTION: Only use with velocity currently, null else
    // Note: Z component is not use currently
    // This is the clip space position. Warning, do not confuse with the value of positionCS in PackedVarying which is SV_POSITION and store in unPositionSS
    float4 positionCS;
    float4 previousPositionCS;
    // end velocity specific

    // For two sided lighting
    bool isFrontFace;
};

void ApplyDepthOffsetAttribute(float depthOffsetVS, inout FragInputs fragInput)
{
    fragInput.positionCS.w += depthOffsetVS;
    fragInput.previousPositionCS.w += depthOffsetVS;
}

void GetVaryingsDataDebug(uint paramId, FragInputs input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEWVARYING_TEXCOORD0:
        result = float3(input.texCoord0, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD1:
        result = float3(input.texCoord1, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD2:
        result = float3(input.texCoord2, 0.0);
        break;
    case DEBUGVIEWVARYING_TEXCOORD3:
        result = float3(input.texCoord3, 0.0);
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
