#if SHADERPASS != SHADERPASS_VISIBILITY_OCCLUSION_CULLING
#error SHADERPASS_is_not_correctly_define
#endif

ByteAddressBuffer inputVisibleInstanceData;
RWByteAddressBuffer instanceVisibilityBitfield : register(u2);

#if defined(PROCEDURAL_CUBE)

#ifndef ATTRIBUTES_NEED_VERTEX_ID
    #error Attributes_requires_vertex_id
#endif

#if defined(PROBE_TRIANGLE)
    #if !defined(PROCEDURAL_CUBE) || !defined(VS_TRIANGLE_CULLING)
        #error Must have procedural VS culling to use probe
    #endif
#endif

#define CreateProceduralPositionOS BoundingBoxPositionOS

static const uint kFacesPerInstance = 3;
static const uint kTrianglesPerFace = 2;
static const uint kVerticesPerTriangle = 3;
static const uint kVerticesPerInstance = kVerticesPerTriangle * kTrianglesPerFace * kFacesPerInstance;

struct BoxVert
{
    uint face;
    float2 coords;
};

static const BoxVert BoxVerts[kVerticesPerInstance] =
{
    { 0, -1, -1, },
    { 0, 1, -1, },
    { 0, -1, 1, },
    { 0, 1, 1, },
    { 0, 1, -1, },
    { 0, -1, 1, },
    { 1, -1, -1, },
    { 1, 1, -1, },
    { 1, -1, 1, },
    { 1, 1, 1, },
    { 1, 1, -1, },
    { 1, -1, 1, },
    { 2, -1, -1, },
    { 2, 1, -1, },
    { 2, -1, 1, },
    { 2, 1, 1, },
    { 2, 1, -1, },
    { 2, -1, 1, },
};

static uint occlusion_ProceduralInstanceIndex;
static uint occlusion_ProceduralVertexIndex;

float4 ProbeVertexCS(uint vertexIndex)
{
    float3 cameraPosWS = _WorldSpaceCameraPos;
    float3 boxCenterWS = TransformObjectToWorld(occlusion_meshBounds.center);
    float3 cameraDirWS = cameraPosWS - boxCenterWS;

    float3 extents = occlusion_meshBounds.extents;
    float3 axisX = TransformObjectToWorldDir(float3(extents.x, 0,         0        ), false);
    float3 axisY = TransformObjectToWorldDir(float3(0,         extents.y, 0        ), false);
    float3 axisZ = TransformObjectToWorldDir(float3(0,         0,         extents.z), false);

    bool isXFront = dot(axisX, cameraDirWS) > 0;
    bool isYFront = dot(axisY, cameraDirWS) > 0;
    bool isZFront = dot(axisZ, cameraDirWS) > 0;

    if (!isXFront) axisX = -axisX;
    if (!isYFront) axisY = -axisY;
    if (!isZFront) axisZ = -axisZ;

    float3 closestVertexWS = boxCenterWS + axisX + axisY + axisZ;
    float4 closestVertexCS = TransformWorldToHClip(closestVertexWS);
    float2 pixelSize = _ScreenSize.zw * closestVertexCS.w;

    if (vertexIndex == 0)
        closestVertexCS.xy -= pixelSize;
    else if (vertexIndex == 1)
        closestVertexCS.x += pixelSize.x;
    else
        closestVertexCS.y += pixelSize.y;

    return closestVertexCS;
}

float3 BoundingBoxPositionOS(AttributesMesh input)
{
    BoxVert V = BoxVerts[occlusion_ProceduralVertexIndex];

    float3 cameraPosWS = _WorldSpaceCameraPos;
    float3 boxCenterWS = TransformObjectToWorld(occlusion_meshBounds.center);
    float3 cameraDirWS = cameraPosWS - boxCenterWS;

    float3 axisWS = 0;
    if (V.face == 0)
    {
        float3 axisOS = float3(1, 0, 0);
        axisWS = TransformObjectToWorldDir(axisOS, false);
    }
    else if (V.face == 1)
    {
        float3 axisOS = float3(0, 1, 0);
        axisWS = TransformObjectToWorldDir(axisOS, false);
    }
    else
    {
        float3 axisOS = float3(0, 0, 1);
        axisWS = TransformObjectToWorldDir(axisOS, false);
    }
    bool isFrontFacing = dot(axisWS, cameraDirWS) > 0;

    float w = isFrontFacing ? 1 : -1;
    float3 vertexPosOS = 0;
    if (V.face == 0)
        vertexPosOS = float3(w, V.coords.x, V.coords.y);
    else if (V.face == 1)
        vertexPosOS = float3(V.coords.x, w, V.coords.y);
    else
        vertexPosOS = float3(V.coords.x, V.coords.y, w);

    return TransformBoundsVertex(vertexPosOS);
}
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

struct OcclusionVaryings
{
    uint4 instanceID : COLOR0;
    SV_POSITION_QUALIFIERS float4 positionCS : SV_Position;
};

MeshBounds GetMeshBounds(uint instanceID, uint batchID)
{
    GeoPoolMetadataEntry metadata = GeometryPool::GetMetadataEntry(instanceID, batchID);
    MeshBounds bounds;
    bounds.center = metadata.boundsCenter;
    bounds.extents = metadata.boundsExtents;
    return bounds;
}

OcclusionVaryings VertOcclusion(AttributesMesh input, uint svInstanceId : SV_InstanceID)
{
#if defined(PROCEDURAL_CUBE)
    uint vid = input.vertexIndex;

#if defined(VS_TRIANGLE_CULLING)
    occlusion_ProceduralInstanceIndex = vid / kVerticesPerTriangle;
    occlusion_ProceduralVertexIndex =
        (svInstanceId * kVerticesPerTriangle) +
        (vid % kVerticesPerTriangle);
    {
        uint instanceIndex = occlusion_ProceduralInstanceIndex;
        uint instanceDword = instanceIndex >> 5;
        uint bitIndex =  instanceIndex & 0x1f;
        uint mask = 1 << bitIndex;
        uint dwordAddress = instanceDword << 2;
        uint value = instanceVisibilityBitfield.Load(dwordAddress);
        if (value & mask)
        {
            // Instance is already marked as visible, no need to rasterize
            // any more triangles for it.
            OcclusionVaryings o;
            o.instanceID = 0;
            o.positionCS = qnan;
            return o;
        }
    }
#else
    occlusion_ProceduralInstanceIndex = vid / kVerticesPerInstance;
    occlusion_ProceduralVertexIndex = vid % kVerticesPerInstance;
#endif

    uint instanceIndex = occlusion_ProceduralInstanceIndex;

#else
    uint instanceIndex = svInstanceId;
    input.positionOS *= 2; // Unity Cube goes from 0 to 0.5, scale it from 0 to 1

#endif
    occlusion_instanceID = inputVisibleInstanceData.Load(instanceIndex << 2);
    occlusion_meshBounds = GetMeshBounds(occlusion_instanceID, instanceBatchID);
    InitObjectToWorld();

#if defined(PROBE_TRIANGLE) && defined(PROCEDURAL_CUBE)
    if (svInstanceId == 0)
    {
        OcclusionVaryings o;
        o.instanceID = uint4(instanceIndex, occlusion_instanceID, 0, 0);
        o.positionCS = ProbeVertexCS(occlusion_ProceduralVertexIndex);
        return o;
    }
    else
        occlusion_ProceduralVertexIndex -= kVerticesPerTriangle;
#endif

    VaryingsMeshToPS vmesh = VertMesh(input);

    OcclusionVaryings o;
    o.instanceID = uint4(instanceIndex, occlusion_instanceID, 0, 0);
    o.positionCS = vmesh.positionCS;

    return o;
}

struct PSVaryings
{
    uint4 instanceIDVarying : COLOR0;
#if defined(DEBUG_OUTPUT)
    float4 svPosition : SV_Position;
#endif
};

[earlydepthstencil]
#if defined(DEBUG_OUTPUT)
float4 FragOcclusion(PSVaryings v) : SV_Target
#else
void FragOcclusion(PSVaryings v)
#endif
{
    uint instanceIndex = v.instanceIDVarying.x;
    uint instanceID = v.instanceIDVarying.y;

    uint instanceDword =  instanceIndex >> 5;
    uint bitIndex =  instanceIndex & 0x1f;
    uint mask = 1 << bitIndex;
    uint dwordAddress = instanceDword << 2;

    uint value = instanceVisibilityBitfield.Load(dwordAddress);
    if (!(value & mask))
    {
        instanceVisibilityBitfield.InterlockedOr(dwordAddress, mask);
    }

#if defined(DEBUG_OUTPUT)
    return v.svPosition.z;
#endif
}
