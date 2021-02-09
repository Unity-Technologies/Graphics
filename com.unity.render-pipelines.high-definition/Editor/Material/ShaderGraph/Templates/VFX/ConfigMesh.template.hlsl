void ApplyVFXModification(AttributesMesh input, inout VaryingsMeshType output)
{
    Attributes attributes = (Attributes)0;

    // Index Setup
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
}
