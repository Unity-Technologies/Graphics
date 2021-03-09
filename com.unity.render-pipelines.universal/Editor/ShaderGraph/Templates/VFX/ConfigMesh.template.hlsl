// Output Type: Mesh

bool GetMeshAndElementIndex(inout Attributes input, inout AttributesElement element)
{
    uint index = input.instanceID;

    if (ShouldCull(index))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    element.index = index;

    // Mesh requires no preliminary configuration.
    return true;
}
