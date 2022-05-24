// Output Type: Mesh

bool GetMeshAndElementIndex(inout VFX_SRP_ATTRIBUTES input, inout AttributesElement element)
{
    uint index = VFX_GET_INSTANCE_ID(input);

    $splice(VFXInitInstancingCompute)

    ContextData contextData = instancingContextData[instanceActiveIndex];
    uint systemSeed = contextData.systemSeed;
    uint nbMax = contextData.maxParticleCount;
    if (ShouldCullElement(index, instanceIndex, nbMax))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    element.index = index;
    element.instanceIndex = instanceIndex;

    // Mesh requires no preliminary configuration.
    return true;
}
