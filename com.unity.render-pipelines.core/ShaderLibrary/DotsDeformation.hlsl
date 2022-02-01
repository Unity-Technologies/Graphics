#if defined(DOTS_INSTANCING_ON)
struct DeformedVertexData
{
    float3 Position;
    float3 Normal;
    float3 Tangent;
};

uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData;
uniform StructuredBuffer<DeformedVertexData> _PreviousFrameDeformedMeshData;

// Reads vertex data for compute skinned meshes in Hybdrid Renderer
void FetchComputeVertexData(inout float3 pos, inout float3 nrm, inout float4 tan, in uint vertexID)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];
        const DeformedVertexData vertexData = _DeformedMeshData[startIndex + vertexID];

        pos = vertexData.Position;
        nrm = vertexData.Normal;
        tan = float4(vertexData.Tangent, 0);
    }
}

void FetchComputeVertexPosNrm(inout float3 pos, inout float3 nrm, in uint vertexID)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];
        const DeformedVertexData vertexData = _DeformedMeshData[startIndex + vertexID];

        pos = vertexData.Position;
        nrm = vertexData.Normal;
    }
}

void FetchComputeVertexNormal(inout float3 normal, in int4 deformProperty, in uint vertexID)
{
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];

        normal = _DeformedMeshData[startIndex + vertexID].Normal;
    }
}

void FetchComputeVertexPosition(inout float3 position, in int4 deformProperty, in uint vertexID)
{
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];

        position = _DeformedMeshData[startIndex + vertexID].Position;
    }
}

void FetchComputeVertexTangent(inout float3 tangent, in int4 deformProperty, in uint vertexID)
{
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int streamIndex = _HybridDeformedVertexStreamIndex;
        const int startIndex = deformProperty[streamIndex];

        tangent = _DeformedMeshData[startIndex + vertexID].Tangent;
    }
}

void FetchComputeVertexNormal(inout float3 normal, in uint vertexID)
{
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    FetchComputeVertexNormal(normal, deformProperty, vertexID);
}

void FetchComputeVertexPosition(inout float3 position, in uint vertexID)
{
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    FetchComputeVertexPosition(position, deformProperty, vertexID);
}

void FetchComputeVertexPosition(inout float4 position, in uint vertexID)
{
    FetchComputeVertexPosition(position.xyz, vertexID);
}
#endif
