//-------------------------------------------------------------------------------------
// FragInputs
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

#include "HDRenderPipeline/Debug/DebugViewMaterial.cs.hlsl"

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

    #ifdef SURFACE_GRADIENT
    // Various tangent space for all UVSet
    // used for vertex level tangent space only (support on UV set 0 only)
    float3 vtxNormalWS;  
    float3 mikktsTang;
    float3 mikktsBino;
    // Use for the 3 other UVSet;
    float3 vT1, vB1;
    float3 vT2, vB2;
    float3 vT3, vB3;

    #else
    // TODO: confirm with Morten following statement
    // Our TBN is orthogonal but is maybe not orthonormal in order to be compliant with external bakers (Like xnormal that use mikktspace).
    // (xnormal for example take into account the interpolation when baking the normal and normalizing the tangent basis could cause distortion).
    float3 worldToTangent[3]; // These 3 vectors are normalized (no need for the material to normalize) and these are only for UVSet 0
    #endif

    // For two sided lighting
    bool isFrontFace;
};

// FragInputs use dir vector that are normalized in the code even if not used
// so we initialize them to a valid != 0 to shutdown compiler warning
FragInputs InitializeFragInputs()
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    output.worldToTangent[0] = float3(0.0, 0.0, 1.0);
    output.worldToTangent[2] = float3(0.0, 0.0, 1.0);

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
