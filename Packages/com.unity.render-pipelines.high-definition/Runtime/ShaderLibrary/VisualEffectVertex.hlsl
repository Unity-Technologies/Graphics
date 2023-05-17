// Wrapper vertex invocations for VFX. Necessary to work around various null input geometry issues for vertex input layout on DX12 and Vulkan.
PackedVaryingsType VertVFX(
#if NULL_GEOMETRY_INPUT
      uint vertexID : VERTEXID_SEMANTIC
    , uint instanceID : INSTANCEID_SEMANTIC
#else
    AttributesMesh inputMesh
    #if defined(MOTION_VEC_VERTEX_COMMON_INCLUDED)
        , AttributesPass inputPass
    #endif
#endif
)
{
#if NULL_GEOMETRY_INPUT
    AttributesMesh inputMesh;
    ZERO_INITIALIZE(AttributesMesh, inputMesh);

    inputMesh.vertexID = vertexID;
    inputMesh.instanceID = instanceID;

    #if defined(MOTION_VEC_VERTEX_COMMON_INCLUDED)
        AttributesPass inputPass;
        ZERO_INITIALIZE(AttributesPass, inputPass);
    #endif
#endif

    return Vert(inputMesh
#if defined(MOTION_VEC_VERTEX_COMMON_INCLUDED)
        , inputPass
#endif
    );
}
