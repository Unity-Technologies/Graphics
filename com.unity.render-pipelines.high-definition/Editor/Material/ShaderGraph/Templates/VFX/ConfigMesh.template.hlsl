// Output Type: Mesh

AttributesMesh GetMeshVFX(AttributesMesh input, inout uint index)
{
    index = input.instanceID;

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    // Mesh output requires no preliminary configuration.
    return input;
}
