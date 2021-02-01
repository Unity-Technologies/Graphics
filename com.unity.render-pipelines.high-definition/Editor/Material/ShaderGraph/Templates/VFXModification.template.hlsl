$splice(VFXDefineSpace)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders/VFXCommon.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl"

$splice(VFXParameterBuffer)

#define VFX_NEEDS_COLOR_INTERPOLATOR (VFX_USE_COLOR_CURRENT || VFX_USE_ALPHA_CURRENT)
#if HAS_STRIPS
#define VFX_OPTIONAL_INTERPOLATION
#else//
#define VFX_OPTIONAL_INTERPOLATION nointerpolation
#endif

ByteAddressBuffer attributeBuffer;

#define VFX_HAS_INDIRECT_DRAW 1

#if VFX_HAS_INDIRECT_DRAW
StructuredBuffer<uint> indirectBuffer;
#endif

#if USE_DEAD_LIST_COUNT
ByteAddressBuffer deadListCount;
#endif

#if HAS_STRIPS
Buffer<uint> stripDataBuffer;
#endif

#if WRITE_MOTION_VECTOR_IN_FORWARD || USE_MOTION_VECTORS_PASS
ByteAddressBuffer elementToVFXBufferPrevious;
#endif

CBUFFER_START(outputParams)
    float nbMax;
float systemSeed;
CBUFFER_END

// VFX Graph Block Functions
$splice(VFXGeneratedBlockFunction)

#define VaryingsMeshType VaryingsMeshToPS

VaryingsMeshType ApplyVFXModification(AttributesMesh input, inout VaryingsMeshType output)
{
    Attributes attributes = (Attributes)0;

    uint index = input.instanceID;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    // Load Attributes
    $splice(VFXLoadAttribute)

    // Process Blocks
    $splice(VFXProcessBlocks)

    // Instance to Particle
    float3 size3 = float3(attributes.size,attributes.size,attributes.size);

    float3 inputVertexPosition = input.positionOS;

    float4x4 elementToVFX = GetElementToVFXMatrix(
        attributes.axisX,
        attributes.axisY,
        attributes.axisZ,
        float3(attributes.angleX,attributes.angleY,attributes.angleZ),
        float3(attributes.pivotX,attributes.pivotY,attributes.pivotZ),
        size3,
        attributes.position);

    float3 vPos = mul(elementToVFX,float4(inputVertexPosition,1.0f)).xyz;
    float4 csPos = TransformPositionVFXToClip(vPos);

    output.positionCS = csPos;

    #ifdef VARYINGS_NEED_POSITION_WS
    // Need to overwrite the position with the result from VFX.
    // Warning: Need to be explicit about relative space.
    output.positionRWS = TransformPositionVFXToWorld(vPos);
    #endif

    // Interpolants Generation
    $splice(VFXInterpolantsGeneration)

    return output;
}
