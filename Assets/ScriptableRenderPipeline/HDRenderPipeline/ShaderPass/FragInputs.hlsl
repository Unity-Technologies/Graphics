//-------------------------------------------------------------------------------------
// FragInputs
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

#include "../Debug/DebugDisplay.cs.hlsl"

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
    float4 color; // vertex color

    // TODO: confirm with Morten following statement
    // Our TBN is orthogonal but is maybe not orthonormal in order to be compliant with external bakers (Like xnormal that use mikktspace).
    // (xnormal for example take into account the interpolation when baking the normal and normalizing the tangent basis could cause distortion).
    // When using worldToTangent with surface gradient, it doesn't normalize the tangent/bitangent vector (We instead use exact same scale as applied to interpolated vertex normal to avoid breaking compliance).
    // this mean that any usage of worldToTangent[1] or worldToTangent[2] outside of the context of normal map (like for POM) must normalize the TBN (TCHECK if this make any difference ?)
    // When not using surface gradient, each vector of worldToTangent are normalize (TODO: Maybe they should not even in case of no surface gradient ? Ask Morten)
    float3x3 worldToTangent;

    // For two sided lighting
    bool isFrontFace;
};

// FragInputs use dir vector that are normalized in the code even if not used
// so we initialize them to a valid != 0 to shutdown compiler warning
FragInputs InitializeFragInputs()
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    // Init to some default value to make the computer quiet (else it output "divide by zero" warning even if value is not used).
    output.worldToTangent[0] = float3(1, 0, 0);
    output.worldToTangent[1] = float3(0, 1, 0);
    output.worldToTangent[2] = float3(0, 0, 1);

    return output;
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
        result = input.worldToTangent[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_BITANGENT_WS:
        result = input.worldToTangent[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_NORMAL_WS:
        result = input.worldToTangent[2].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR:
        result = input.color.rgb; needLinearToSRGB = true;
        break;
    case DEBUGVIEWVARYING_VERTEX_COLOR_ALPHA:
        result = input.color.aaa;
        break;
    }
}
