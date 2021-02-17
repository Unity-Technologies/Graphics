// Output Type: Mesh

bool GetMeshAndElementIndex(inout AttributesMesh input, inout uint index)
{
    index = input.instanceID;

    if (ShouldCull(index))
        return false;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    // Mesh requires no preliminary configuration.
    return true;
}
