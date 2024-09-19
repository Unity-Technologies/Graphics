// Output Type: Mesh

bool GetMeshAndElementIndex(inout VFX_SRP_ATTRIBUTES input, inout AttributesElement element)
{
    uint index = VFX_GET_INSTANCE_ID(input);

    $splice(VFXInitInstancing)
    #ifdef UNITY_INSTANCING_ENABLED
    input.instanceID = unity_InstanceID;
    #endif
    $splice(VFXLoadContextData)
    uint systemSeed = contextData.systemSeed;
    uint nbMax = contextData.maxParticleCount;
    if (ShouldCullElement(index, instanceIndex, nbMax))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[VFXGetIndirectBufferIndex(index, instanceActiveIndex)];
    #endif

    #if HAS_STRIPS_DATA
        StripData stripData = GetStripDataFromParticleIndex(index, instanceIndex);
        element.relativeIndexInStrip = GetRelativeIndex(index, stripData);
        element.stripData = stripData;
    #endif

    element.index = index;
    element.instanceIndex = instanceIndex;
    element.instanceActiveIndex = instanceActiveIndex;

    // Mesh requires no preliminary configuration.
    return true;
}
