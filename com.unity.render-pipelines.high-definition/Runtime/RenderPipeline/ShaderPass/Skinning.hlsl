#ifndef SKINNING_INCLUDED
#define SKINNING_INCLUDED

//TODO: defines around nrm and tan?
//LBS Node not supported (skinning in vertex shader)

#if defined(DOTS_SKINNING)

struct DeformedVertexData
{
    float3 Position;
    float3 Normal;
    float3 Tangent;
};

uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData;
uniform StructuredBuffer<DeformedVertexData> _PreviousFrameDeformedMeshData;

void DOTS_Deformation(inout float3 position, inout float3 normal, inout float4 tangent, uint vertexID)
{
    // x = curr frame index
    // y = prev frame index
    // z = deformation check (0 = no deformation, 1 = has deformation)
    const int4 deformProperty = asint(unity_ComputeMeshIndex);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
		const int streamIndex = _HybridDeformedVertexStreamIndex;
		const int startIndex = deformProperty[streamIndex];
		const DeformedVertexData vertexData = _DeformedMeshData[startIndex + vertexID];

        position = vertexData.Position;
        normal   = vertexData.Normal;
        tangent  = float4(vertexData.Tangent, 0);
    }
}

// position only for motion vec vs
void DOTS_Deformation_MotionVecPass(inout float3 currPos, inout float3 prevPos, uint vertexID)
{
    // x = curr frame index
    // y = prev frame index
    // z = deformation check (0 = no deformation, 1 = has deformation)
    const int4 deformProperty = asint(unity_ComputeMeshIndex);
    const int doSkinning = deformProperty.z;
    if (doSkinning == 1)
    {
        const int currStreamIndex = _HybridDeformedVertexStreamIndex;
		const int prevStreamIndex = (currStreamIndex + 1) % 2;

        const int currMeshStart = deformProperty[currStreamIndex];
        const int prevMeshStart = deformProperty[prevStreamIndex];

        currPos = _DeformedMeshData[currMeshStart + vertexID].Position;
        prevPos = _PreviousFrameDeformedMeshData[prevMeshStart + vertexID].Position;
    }
}
#endif

#endif
