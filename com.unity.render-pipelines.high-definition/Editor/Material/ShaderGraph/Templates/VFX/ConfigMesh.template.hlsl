// Output Type: Mesh

void GetMeshAndElementIndex(inout AttributesMesh input, inout uint index)
{
    index = input.instanceID;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    // Mesh requires no preliminary configuration.
}
