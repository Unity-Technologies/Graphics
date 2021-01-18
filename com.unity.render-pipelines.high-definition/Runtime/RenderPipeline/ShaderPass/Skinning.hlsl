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


void DOTS_Skinning(inout float3 position, inout float3 normal, inout float4 tangent, uint vertexID)
{
    const int doSkinning = (int)unity_SkinSetting.x;
    if (doSkinning > 0)
    {
        const int meshIndex = (int)unity_ComputeMeshIndex.x;
        const DeformedVertexData data = _DeformedMeshData[meshIndex + vertexID];

        position = data.Position;
        normal   = data.Normal;
        tangent  = float4(data.Tangent, 0);
    }
}

void DOTS_GetPreviousDeformedPosition(inout float3 prevPos, uint vertexID)
{
    const int doSkinning = (int)unity_SkinSetting.x;
    if (doSkinning > 0)
    {
        const int meshIndex = (int)unity_ComputeMeshIndex.x;
        prevPos = float3(0, 0, 0);// _DeformedMeshData[meshIndex + vertexID].Position; //Read from other buffer
    }
}
#endif

#endif
