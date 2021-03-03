// Output Type: Mesh

void ConfigureIndex(AttributesElementInputs input, inout AttributesElement element)
{
    uint index = input.instanceID;

    element.cull = ShouldCull(index);

    #if VFX_HAS_INDIRECT_DRAW
    index = indirectBuffer[index];
    #endif

    element.index = index;
}

void ConfigureMesh(AttributesElementInputs input, inout AttributesElement element)
{
    // Nothing needs to be done for mesh output.
}
