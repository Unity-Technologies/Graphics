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
void FetchComputeVertexData(inout AttributesMesh input)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const uint streamIndex = _HybridDeformedVertexStreamIndex;
        const uint startIndex = deformProperty[streamIndex];
        const DeformedVertexData vertexData = _DeformedMeshData[startIndex + input.vertexID];

        input.positionOS = vertexData.Position;
#ifdef ATTRIBUTES_NEED_NORMAL
        input.normalOS = vertexData.Normal;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
        input.tangentOS = float4(vertexData.Tangent, input.tangentOS.w);
#endif
    }
}

// Reads vertex position for compute skinned meshes in Hybdrid Renderer
// and also previous frame position if skinned motion vectors are used
void FetchComputeVertexPosition(inout float3 currPos, inout float3 prevPos, uint vertexID)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_DOTSDeformationParams);
    const int computeSkin = deformProperty.z;
    const uint streamIndex = _HybridDeformedVertexStreamIndex;
    if (computeSkin == 1)
    {
        const uint currMeshStart = deformProperty[streamIndex]; 
        const uint currStreamIndex = _HybridDeformedVertexStreamIndex;
        currPos = _DeformedMeshData[currMeshStart + vertexID].Position;
    }

    const int skinMotionVec = deformProperty.w;
    if (skinMotionVec == 1)
    {
        const uint prevStreamIndex = streamIndex ^ 1;
        const int prevMeshStart = deformProperty[prevStreamIndex];

        if(prevMeshStart == -1)
            prevPos = _DeformedMeshData[prevMeshStart + vertexID].Position;
        else
            prevPos = _PreviousFrameDeformedMeshData[prevMeshStart + vertexID].Position;

    }
}
#endif
