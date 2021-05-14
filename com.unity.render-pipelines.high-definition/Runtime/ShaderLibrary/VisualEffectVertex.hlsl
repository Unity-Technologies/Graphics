// Wrapper vertex invocations for VFX. Necesarry to work around various null input geometry issues for vertex input layout on DX12 and Vulkan.
#if NULL_GEOMETRY_INPUT
    #ifdef MOTION_VEC_VERTEX_COMMON_INCLUDED
        PackedVaryingsType VertVFX(uint vertexID : VERTEXID_SEMANTIC, uint instanceID : INSTANCEID_SEMANTIC)
        {
            AttributesMesh inputMesh;
            ZERO_INITIALIZE(AttributesMesh, inputMesh);

            AttributesPass inputPass;
            ZERO_INITIALIZE(AttributesPass, inputPass);

            inputMesh.vertexID = vertexID;
            inputMesh.instanceID = instanceID;

            return Vert(inputMesh, inputPass);
        }
    #else
        PackedVaryingsType VertVFX(uint vertexID : VERTEXID_SEMANTIC, uint instanceID : INSTANCEID_SEMANTIC)
        {
            AttributesMesh inputMesh;
            ZERO_INITIALIZE(AttributesMesh, inputMesh);

            inputMesh.vertexID = vertexID;
            inputMesh.instanceID = instanceID;

            return Vert(inputMesh);
        }
    #endif
#else
    #ifdef MOTION_VEC_VERTEX_COMMON_INCLUDED
        PackedVaryingsType VertVFX(AttributesMesh inputMesh, AttributesPass inputPass)
        {
            return Vert(inputMesh, inputPass);
        }
    #else
        PackedVaryingsType VertVFX(AttributesMesh inputMesh)
        {
            return Vert(inputMesh);
        }
    #endif
#endif
