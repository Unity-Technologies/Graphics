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

void DOTS_Skinning(inout float3 position, inout float3 normal, inout float4 tangent, uint vertexID)
{
    const int doSkinning = asint(unity_ComputeMeshIndex.z);
    if (doSkinning > 0)
    {
		const int streamIndex = _HybridDeformedVertexStreamIndex;
		const int startIndex = asint(unity_ComputeMeshIndex)[streamIndex];
		const DeformedVertexData vertexData = _DeformedMeshData[startIndex + vertexID];
	
        //const int meshIndex = (int)unity_ComputeMeshIndex.x;
        //const DeformedVertexData data = _DeformedMeshData[meshIndex + vertexID];

        position = vertexData.Position;
        normal   = vertexData.Normal;
        tangent  = float4(vertexData.Tangent, 0);
    }
}

void DOTS_GetPreviousDeformedPosition(inout float3 prevPos, uint vertexID)
{
    const int doSkinning = asint(unity_ComputeMeshIndex.z);
    if (doSkinning > 0)
    {
		const int streamIndex = (_HybridDeformedVertexStreamIndex + 1) % 2;
		const int startIndex = asint(unity_ComputeMeshIndex)[streamIndex];
		const DeformedVertexData vertexData = _PreviousFrameDeformedMeshData[startIndex + vertexID];
        prevPos = vertexData.Position;
    }
}
#endif

#endif
