// Output Type: Mesh

bool GetMeshAndElementIndex(inout VFX_SRP_ATTRIBUTES input, inout AttributesElement element)
{
    uint index = VFX_GET_INSTANCE_ID(input);

    $splice(VFXInitInstancing)

    ContextData contextData = instancingContextData[instanceActiveIndex];
    uint systemSeed = contextData.systemSeed;
    uint nbMax = contextData.maxParticleCount;
    if (ShouldCullElement(index, instanceIndex, nbMax))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[VFXGetIndirectBufferIndex(index, instanceActiveIndex)];
    #endif

    element.index = index;
    element.instanceIndex = instanceIndex;
    element.instanceActiveIndex = instanceActiveIndex;

    // Mesh requires no preliminary configuration.
    return true;
}
