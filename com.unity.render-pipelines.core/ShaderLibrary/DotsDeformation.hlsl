#if defined(DOTS_INSTANCING_ON)
struct DeformedVertexData
{
    float3 Position;
    float3 Normal;
    float3 Tangent;
};

int _HybridDeformedVertexStreamIndex;
uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData;
uniform StructuredBuffer<DeformedVertexData> _PreviousFrameDeformedMeshData;

// Reads vertex data for compute skinned meshes in Hybdrid Renderer
void FetchComputeVertexData(inout Attributes input)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_ComputeMeshIndex);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];
        const DeformedVertexData vertexData = _DeformedMeshData[startIndex + input.vertexID];

        input.positionOS = vertexData.Position;
        input.normalOS = vertexData.Normal;
        input.tangentOS = float4(vertexData.Tangent, 0);
    }
}
#endif
