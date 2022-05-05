
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"



#define kShaderChannelMaskVertex        (1 << 0)
#define kShaderChannelMaskNormal        (1 << 1)
#define kShaderChannelMaskTangent       (1 << 2)
#define kShaderChannelMaskColor         (1 << 3)
#define kShaderChannelMaskTexCoordStart (1 << 4)

#define kShaderChannelMaskVertexStride        12
#define kShaderChannelMaskNormalStride        12
#define kShaderChannelMaskTangentStride       16
#define kShaderChannelMaskColorStride         16
#define kShaderChannelMaskTexCoordStride     8

#define MATERIAL_CHUNK_SIZE (4 * 64)
#define CLUSTER_BUFFER_SIZE (16)

#define kBufferSize_VisibleCluster                      (0)
#define kBufferSize_VisibleInstance                     (1)
#define kBufferSize_CachedVisibleCluster                (2)
#define kBufferSize_CachedVisibleInstance               (3)
#define kBufferSize_ClusterCW                           (4)
#define kBufferSize_ClusterCCW                          (5)

#define MAX_MATERIAL_COUNT (12)
#define LOD_COUNT_MAX (8)
struct LODProxy
{
    float screenRelativeHeight[LOD_COUNT_MAX];
    uint instanceID[LOD_COUNT_MAX];
};

struct InstanceVisible
{
    uint meshHash;
    uint rendererHash;
    uint instanceID;
    //uint status;
    uint lodProxyIndex;
};

struct StreamingInfo
{
    uint meshHash;
    uint rendererHash;
    
    uint clusterID;
    uint status;
};

struct InstanceBuffer
{
    float4x4 worldMatrix;
    float4x4 world2LocalMatrix;
    uint pageOffset;
    uint pageSize;
    
    uint copyUV0;
    uint vertexStride;
    uint vertexChannelMask;
    uint vertexChannelStride[14];

    uint clusterBegin;
    uint clusterCount;
    
    uint lppvHandle;
    int materialID[MAX_MATERIAL_COUNT];
    int materialPropertyOffset[MAX_MATERIAL_COUNT];
};

struct InstanceHeader
{
    float4x4 worldMatrix;
    float4x4 world2LocalMatrix;
    
    float3 boundsCenter;
    uint lodProxyIndex;
    float3 boundsExtents;
    uint layer;
    
    float4x4 previousWorldMatrix;
    float3 previousCenter;
    uint pad0;
    
    uint mask;
    uint lightmapIndex;
    uint pageOffset;
    uint pageSize;
    
    uint meshHash;
    uint rendererHash;
    uint geometryIndex;
    uint lppvHandle;
    
//#if CHECK_CULLING_SCENE_MASK
    uint2 cullingSceneMask;
    uint2 pad1;
//#endif
    
    uint materialID[MAX_MATERIAL_COUNT];
    uint materialPropertyOffset[MAX_MATERIAL_COUNT];
};

struct GeometryInfo
{
    uint vertexStride;
    uint vertexChannelMask;
    uint vertexChannelStride[14];

    uint clusterBegin; // the index in the cluster buffer
    uint clusterCount;

    uint copyUV0;
    uint meshHash;
};


struct ClusterBuffer
{
    uint indexCount;
    uint vertexOffset;
    uint submesh;
    uint pad0;
};

struct ClusterIDs
{
    uint clusterID;
    uint instanceID;
    uint meshHash;
    uint rendererHash;
    uint geometryID;
    uint3 pad;
};

struct MaterialRange
{
    uint min;
    uint max;
};

struct ClusterPageHeader
{
    uint offset; // the page's offset
    uint size; // the page's size
    uint pad0;
    uint pad1;
};

StructuredBuffer<InstanceBuffer> _InstanceBuffer;
StructuredBuffer<GeometryInfo> _GeometryBuffer;
StructuredBuffer<InstanceHeader> _InstanceHeaderBuffer;
StructuredBuffer<ClusterIDs> _ClusterIDBuffer;
StructuredBuffer<ClusterPageHeader> _ClusterPageHeaderBuffer;

ByteAddressBuffer _ClusterPageDataBuffer;

float4x4 _MatrixVP;


struct VertexData
{
    float4 clipPos;
    uint clusterID;
    uint indexID;
    
    uint4 debug;
};


struct VertexAttribute
{
    float3 localPosition;
    float3 normal;
    float4 tangent;
    half4 vertexColor;
    float2 texcoords[8];
    float2 texcoordsDDX[8];
    float2 texcoordsDDY[8];
};



uint DecodeIndexBuffer(ClusterPageHeader header, ClusterBuffer cluster, uint vertexID)
{
    if (vertexID < cluster.indexCount)
    {
        uint i = vertexID / 2;
        uint index = _ClusterPageDataBuffer.Load(i * 4 + header.offset + CLUSTER_BUFFER_SIZE);
        uint result = ~0u;
        if (vertexID % 2 == 0)
        {
            result = (index & 0xffff);
        }
        else
        {
            result = (index >> 16) & 0xffff;
        }
        return result;
    }
    return ~0u;
}


ClusterBuffer GetClusterBuffer(ClusterPageHeader header)
{
    ClusterBuffer cluster = (ClusterBuffer)0;
    uint pageOffset = header.offset;
    uint4 data = _ClusterPageDataBuffer.Load4(pageOffset);
    cluster.indexCount = data.x;
    cluster.vertexOffset = data.y;
    cluster.submesh = data.z;
    return cluster;
}


VertexAttribute GetVertexAttribute(uint clusterID, uint vertexID)
{
    VertexAttribute result = (VertexAttribute)0;
    
    ClusterIDs id = _ClusterIDBuffer[clusterID];
    ClusterPageHeader header = _ClusterPageHeaderBuffer[id.clusterID];
    if (header.offset == ~0u)
        return result;
    
    ClusterBuffer cluster = GetClusterBuffer(header);
    uint index = DecodeIndexBuffer(header, cluster, vertexID);
    if (index == ~0u)
        return result;
    
    GeometryInfo geometry = _GeometryBuffer[id.geometryID];
    index = header.offset + cluster.vertexOffset + index * geometry.vertexStride;
    uint channelIndex = 0;
    if (geometry.vertexChannelMask & kShaderChannelMaskVertex)
    {
        result.localPosition = asfloat(_ClusterPageDataBuffer.Load3(index));
        index += geometry.vertexChannelStride[channelIndex];
    }
    ++channelIndex;
    if (geometry.vertexChannelMask & kShaderChannelMaskNormal)
    {
        result.normal = asfloat(_ClusterPageDataBuffer.Load3(index));
        index += geometry.vertexChannelStride[channelIndex];
    }
    ++channelIndex;
    if (geometry.vertexChannelMask & kShaderChannelMaskTangent)
    {
        result.tangent = asfloat(_ClusterPageDataBuffer.Load4(index));
        index += geometry.vertexChannelStride[channelIndex];
    }
    ++channelIndex;
    if (geometry.vertexChannelMask & kShaderChannelMaskColor)
    {
        if (geometry.vertexChannelStride[channelIndex] == 4)
        {
            uint color = _ClusterPageDataBuffer.Load(index);
            result.vertexColor.r = (color & 0xFF000000) / 255.0f;
            result.vertexColor.g = (color & 0x00FF0000) / 255.0f;
            result.vertexColor.b = (color & 0x0000FF00) / 255.0f;
            result.vertexColor.a = (color & 0x000000FF) / 255.0f;

        }
        else if (geometry.vertexChannelStride[channelIndex] == 16)
        {
            result.vertexColor = asfloat(_ClusterPageDataBuffer.Load4(index));
        }
            
        index += geometry.vertexChannelStride[channelIndex];
    }
    ++channelIndex;
    
    [unroll]
    for (int i = 0; i != 8; ++i)
    {
        if (geometry.vertexChannelMask & (kShaderChannelMaskTexCoordStart << i))
        {
            result.texcoords[i] = asfloat(_ClusterPageDataBuffer.Load2(index));
            index += geometry.vertexChannelStride[channelIndex];
        }
        ++channelIndex;
    }
    
    return result;
}


float3 GetVertexPosition(ClusterPageHeader header, ClusterBuffer cluster, uint vertexStride, uint index)
{
    uint address = header.offset + cluster.vertexOffset + index * vertexStride;
    float3 pos = asfloat(_ClusterPageDataBuffer.Load3(address));
    return pos;
}


VertexData GetVertexData(uint vertexID, uint instanceID)
{
    VertexData vertexData = (VertexData)0;
    vertexData.clipPos = (float4) 0xFFFFFFFF;
    vertexData.clusterID = instanceID;
    
    ClusterIDs id = _ClusterIDBuffer[instanceID];
    ClusterPageHeader header = _ClusterPageHeaderBuffer[id.clusterID];
    if (header.offset == ~0u || header.size == 0)
        return vertexData;
    
    ClusterBuffer cluster = GetClusterBuffer(header);
    uint index = DecodeIndexBuffer(header, cluster, vertexID);
    if (index == -1)
        return vertexData;
    
    //InstanceBuffer instance = _InstanceBuffer[id.instanceID];
    GeometryInfo geometry = _GeometryBuffer[id.geometryID];
    InstanceHeader instance = _InstanceHeaderBuffer[id.instanceID];
    float3 vertex = GetVertexPosition(header, cluster, geometry.vertexStride, index);
    float4x4 mvp = mul(_MatrixVP, instance.worldMatrix);
    vertexData.clipPos = mul(mvp, float4(vertex, 1.0));
    vertexData.clusterID = instanceID;
    vertexData.indexID = vertexID / 3;
    
    vertexData.debug.x = index;
    vertexData.debug.y = header.offset;
    vertexData.debug.z = cluster.vertexOffset;
    return vertexData;
}


int GetMaterialID(uint clusterID)
{
    if (clusterID == -1)
        return 0;
    
    ClusterIDs id = _ClusterIDBuffer[clusterID];
    ClusterPageHeader header = _ClusterPageHeaderBuffer[id.clusterID];
    ClusterBuffer cluster = GetClusterBuffer(header);
    InstanceHeader instance = _InstanceHeaderBuffer[id.instanceID];
    return instance.materialID[cluster.submesh];
}


uint GetHandleLPPV(uint clusterID)
{
    if (clusterID == -1)
        return 0;
    
    ClusterIDs id = _ClusterIDBuffer[clusterID];
    InstanceHeader instance = _InstanceHeaderBuffer[id.instanceID];
    return instance.lppvHandle;
}


uint GetLightmapIndex(uint clusterID)
{
    if (clusterID == -1)
        return ~0u;
    
    ClusterIDs id = _ClusterIDBuffer[clusterID];
    InstanceHeader instance = _InstanceHeaderBuffer[id.instanceID];
    return instance.lightmapIndex;
}


uint GetClusterID(uint r)
{
    return r - 1;
}

uint GetTriangleID(uint g)
{
    return g - 1;
}


struct Barycentrics
{
    float3 UVW;
    float3 UVW_dx;
    float3 UVW_dy;
};

/** Calculates perspective correct barycentric coordinates and partial derivatives using screen derivatives. */
Barycentrics CalculateTriangleBarycentrics(float2 PixelClip, float4 PointClip0, float4 PointClip1, float4 PointClip2)
{
    Barycentrics Result;

    float3 Pos0 = PointClip0.xyz / PointClip0.w;
    float3 Pos1 = PointClip1.xyz / PointClip1.w;
    float3 Pos2 = PointClip2.xyz / PointClip2.w;

    float3 RcpW = rcp(float3(PointClip0.w, PointClip1.w, PointClip2.w));

    float3 Pos120X = float3(Pos1.x, Pos2.x, Pos0.x);
    float3 Pos120Y = float3(Pos1.y, Pos2.y, Pos0.y);
    float3 Pos201X = float3(Pos2.x, Pos0.x, Pos1.x);
    float3 Pos201Y = float3(Pos2.y, Pos0.y, Pos1.y);

    float3 C_dx = Pos201Y - Pos120Y;
    float3 C_dy = Pos120X - Pos201X;

    float3 C = C_dx * (PixelClip.x - Pos120X) + C_dy * (PixelClip.y - Pos120Y); // Evaluate the 3 edge functions
    float3 G = C * RcpW;

    float H = dot(C, RcpW);
    float RcpH = rcp(H);

	// UVW = C * RcpW / dot(C, RcpW)
    Result.UVW = G * RcpH;

	// Texture coordinate derivatives:
	// UVW = G / H where G = C * RcpW and H = dot(C, RcpW)
	// UVW' = (G' * H - G * H') / H^2
	// float2 TexCoordDX = UVW_dx.y * TexCoord10 + UVW_dx.z * TexCoord20;
	// float2 TexCoordDY = UVW_dy.y * TexCoord10 + UVW_dy.z * TexCoord20;
    float3 G_dx = C_dx * RcpW;
    float3 G_dy = C_dy * RcpW;

    float H_dx = dot(C_dx, RcpW);
    float H_dy = dot(C_dy, RcpW);

    Result.UVW_dx = (G_dx * H - G * H_dx) * (RcpH * RcpH) * (2.0f * 1.0f / _ScreenSize.x);
    Result.UVW_dy = (G_dy * H - G * H_dy) * (RcpH * RcpH) * (-2.0f * 1.0f / _ScreenSize.y);

    return Result;
}
