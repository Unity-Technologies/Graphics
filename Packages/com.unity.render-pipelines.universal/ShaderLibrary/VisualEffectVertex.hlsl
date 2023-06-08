// Wrapper vertex invocations for VFX. Necessary to work around various null input geometry issues for vertex input layout on DX12 and Vulkan.
void VertVFX(
#if NULL_GEOMETRY_INPUT
    uint vertexID : VERTEXID_SEMANTIC
    , uint instanceID : INSTANCEID_SEMANTIC
#else
    Attributes input
#endif

#if (SHADERPASS == SHADERPASS_MOTION_VECTORS)
    , out PackedMotionVectorPassVaryings packedMvOutput
#endif
    , out PackedVaryings packedOutput
)
{
#if NULL_GEOMETRY_INPUT
    Attributes input;
    ZERO_INITIALIZE(Attributes, input);
    input.vertexID = vertexID;
    input.instanceID = instanceID;
#endif

#if (SHADERPASS != SHADERPASS_MOTION_VECTORS)
    packedOutput = vert(input);
#else
    MotionVectorPassAttributes dummy = (MotionVectorPassAttributes)0;
    vert(input, dummy, packedMvOutput, packedOutput);
#endif
}
