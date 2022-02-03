// Wrapper vertex invocations for VFX. Necesarry to work around various null input geometry issues for vertex input layout on DX12 and Vulkan.
#if NULL_GEOMETRY_INPUT
PackedVaryings VertVFX(uint vertexID : VERTEXID_SEMANTIC, uint instanceID : INSTANCEID_SEMANTIC)
{
    Attributes input;
    ZERO_INITIALIZE(Attributes, input);

    input.vertexID = vertexID;
    input.instanceID = instanceID;

    return vert(input);
}
#else
PackedVaryings VertVFX(Attributes input)
{
    return vert(input);
}
#endif
