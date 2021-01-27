#if defined(DOTS_INSTANCING_ON)
struct DeformedVertexData
{
    float3 Position;
    float3 Normal;
    float3 Tangent;
};

uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData;
uniform StructuredBuffer<DeformedVertexData> _PreviousFrameDeformedMeshData;

void DOTS_Deformation(inout AttributesMesh input)
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
#ifdef ATTRIBUTES_NEED_NORMAL
        input.normalOS = vertexData.Normal;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
        input.tangentOS = float4(vertexData.Tangent, 0);
#endif
    }
}

// position only for motion vec vs
void DOTS_Deformation_MotionVecPass(inout float3 currPos, inout float3 prevPos, uint vertexID)
{
    // x,y = current and previous frame indices
    // z = deformation check (0 = no deformation, 1 = has deformation)
    // w = skinned motion vectors
    const int4 deformProperty = asint(unity_ComputeMeshIndex);
    const int computeSkin = deformProperty.z;
    if (computeSkin == 1)
    {
        const int currStreamIndex = _HybridDeformedVertexStreamIndex;
        const int currMeshStart = deformProperty[currStreamIndex];
        currPos = _DeformedMeshData[currMeshStart + vertexID].Position;
    }

    const int skinMotionVec = deformProperty.w;
    if (skinMotionVec == 1)
    {
        const int prevStreamIndex = (_HybridDeformedVertexStreamIndex + 1) % 2;
        const int prevMeshStart = deformProperty[prevStreamIndex];
        prevPos = _PreviousFrameDeformedMeshData[prevMeshStart + vertexID].Position;
    }
}
#endif
