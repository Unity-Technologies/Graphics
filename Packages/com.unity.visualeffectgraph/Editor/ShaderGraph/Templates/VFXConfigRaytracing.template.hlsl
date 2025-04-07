#include "Packages/com.unity.visualeffectgraph/Shaders/VFXRayTracingCommon.hlsl"

void GetVFXInstancingIndices(out uint index, out uint instanceIndex, out uint instanceActiveIndex)
{
    #ifdef VFX_RT_DECIMATION_FACTOR
    int rayTracingDecimationFactor = VFX_RT_DECIMATION_FACTOR;
    #else
    int rayTracingDecimationFactor = 1;
    #endif
    index = PrimitiveIndex() * rayTracingDecimationFactor;
    instanceIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceIndex));
    instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    VFXGetInstanceCurrentIndex(index);
}

float GetVFXVertexDisplacement(int index, float3 currentWS, float3 inputVertexPosition, uint currentFrameIndex)
{
    float displacement = 0.0;
    #if VFX_FEATURE_MOTION_VECTORS
    uint elementToVFXBaseIndex = index * 13;
    uint previousFrameIndex = elementToVFXBufferPrevious.Load(elementToVFXBaseIndex++ << 2);
    if (currentFrameIndex - previousFrameIndex == 1u)    //if (dot(previousElementToVFX[0], 1) != 0)
        {
        float4x4 previousElementToVFX = (float4x4)0;
        previousElementToVFX[3] = float4(0,0,0,1);
        UNITY_UNROLL
        for (int itIndexMatrixRow = 0; itIndexMatrixRow < 3; ++itIndexMatrixRow)
        {
            uint4 read = elementToVFXBufferPrevious.Load4((elementToVFXBaseIndex + itIndexMatrixRow * 4) << 2);
            previousElementToVFX[itIndexMatrixRow] = asfloat(read);
        }
        float3 previousWS = TransformPreviousVFXPositionToWorld(mul(previousElementToVFX, float4(inputVertexPosition, 1.0f)).xyz);
        displacement = length(currentWS - previousWS);
        }
    #endif
    return displacement;
}

void BuildFragInputsFromVFXIntersection(AttributeData attributeData, out FragInputs output, out uint outCurrentFrameIndex)
{
    uint index, instanceIndex, instanceActiveIndex;
    GetVFXInstancingIndices(index, instanceIndex, instanceActiveIndex);
    #if VFX_USE_GRAPH_VALUES
    $splice(VFXLoadGraphValues)
    #endif

    InternalAttributesElement attributes;
    ZERO_INITIALIZE(InternalAttributesElement, attributes);
    $splice(VFXLoadAttribute)
    $splice(VFXProcessBlocks)

    float3 size3 = GetElementSizeRT(attributes
#if VFX_USE_GRAPH_VALUES
        , graphValues
#endif
    );

    float3 rayDirection = WorldRayDirection();
    output.positionSS = float4(0.0, 0.0, 0.0, 0.0);
    output.positionRWS = WorldRayOrigin() + rayDirection * RayTCurrent();
    output.texCoord0 = float4(attributeData.barycentrics,0,0);
    output.texCoord1 = float4(attributeData.barycentrics,0,0);
    output.texCoord2 = float4(attributeData.barycentrics,0,0);
    output.texCoord3 = float4(attributeData.barycentrics,0,0);

    #if VFX_USE_COLOR_CURRENT
    float3 color = attributes.color;
    #else
    float3 color = float3(1,1,1);
    #endif
    #if VFX_USE_ALPHA_CURRENT
    float alpha = attributes.alpha;
    #else
    float alpha = 1;
    #endif
    output.color = float4(color, alpha);

    // Compute the world space normal
    float3 normalWS = normalize(-WorldToPrimitive(attributes, size3)[2].xyz);
    float3 tangentWS = normalize(WorldToPrimitive(attributes, size3)[0].xyz);
    output.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, /*sign(currentVertex.tangentOS.w)*/1);

    output.isFrontFace = dot(rayDirection, output.tangentToWorld[2]) < 0.0f;

    $splice(VFXSetFragInputsRT)

#if VFX_FEATURE_MOTION_VECTORS
    $splice(VFXLoadCurrentFrameIndexParameter)
    outCurrentFrameIndex = currentFrameIndex;
#endif
}
