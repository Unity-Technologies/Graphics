#ifndef _UNIFIEDRAYTRACING_RAYQUERYSOFTWARE_HLSL_
#define _UNIFIEDRAYTRACING_RAYQUERYSOFTWARE_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Bindings.hlsl"

#pragma warning(disable : 4008) // fast_intersect_bbox is designed to handle inf and nans, so we disable the `floating point division by zero` warning

#ifndef UNIFIED_RT_LDS_STACK_SIZE
#define UNIFIED_RT_LDS_STACK_SIZE 8
#endif

#ifndef UNIFIED_RT_STACK_SIZE
#define UNIFIED_RT_STACK_SIZE 64
#endif

RWStructuredBuffer<uint> _UnifiedRT_Stack;

namespace UnifiedRT {

groupshared uint lds_stack[UNIFIED_RT_LDS_STACK_SIZE * (UNIFIED_RT_GROUP_SIZE_X*UNIFIED_RT_GROUP_SIZE_Y*UNIFIED_RT_GROUP_SIZE_Z)];

static const uint kTopLevelSentinel = 0xFFFFFFFE;
static const uint kCommittedNothing = 0;
static const uint kCommittedTriangleHit = 1;

static const uint kNonOpaqueInstanceBit = (1 << 29);
static const uint kCandidateHitCommittedBit = (1 << 28);

struct RayTraversalStack
{
    void Init(uint globalThreadIndex, uint localThreadIndex)
    {
        sbegin = UNIFIED_RT_STACK_SIZE * globalThreadIndex;
        sptr = sbegin;
        lds_sbegin = localThreadIndex * UNIFIED_RT_LDS_STACK_SIZE;
        lds_sptr = lds_sbegin;
        lds_stack[lds_sptr++] = INVALID_NODE;
    }

    void Push(uint addr)
    {
        if (lds_sptr - lds_sbegin >= UNIFIED_RT_LDS_STACK_SIZE)
        {
            for (int i = 1; i < UNIFIED_RT_LDS_STACK_SIZE; ++i)
            {
                _UnifiedRT_Stack[sptr + i] = lds_stack[lds_sbegin + i];
            }

            sptr += UNIFIED_RT_LDS_STACK_SIZE;
            lds_sptr = lds_sbegin + 1;
        }
        lds_stack[lds_sptr++] = addr;
    }

    uint Pop()
    {
        uint addr = lds_stack[--lds_sptr];
        if (addr == INVALID_NODE && sptr > sbegin)
        {
            sptr -= UNIFIED_RT_LDS_STACK_SIZE;
            for (int i = 1; i < UNIFIED_RT_LDS_STACK_SIZE; ++i)
            {
                lds_stack[lds_sbegin + i] = _UnifiedRT_Stack[sptr + i];
            }
            lds_sptr = lds_sbegin + UNIFIED_RT_LDS_STACK_SIZE;
            addr = lds_stack[--lds_sptr];
        }
        return addr;
    }

    uint lds_sptr;
    uint lds_sbegin;
    uint sptr;
    uint sbegin;
};

uint GetCullMode(uint rayFlags)
{
    uint cullMode = 0;
    cullMode |= (rayFlags & kRayFlagCullBackFacingTriangles) << 27;

    if ((rayFlags & (kRayFlagCullFrontFacingTriangles | kRayFlagCullBackFacingTriangles)) == 0)
        cullMode |= (1 << 30);

    return cullMode;
}

struct ClosestHit
{
    uint instanceIndex;
    uint isFrontFace_primitiveIndex;
    float2 uv;
};

struct CandidateHit
{
    uint isFrontFace_primitiveIndex;
    float2 uv;
    float hitT;
};

struct CurrentInstance
{
    uint cullMode_isNonOpaque_candidateCommitted;
    uint bvhOffset;
    int bvhLeavesOffset;
    int vertexOffset;
};

CurrentInstance GetCurrentInstance(InstanceInfo instanceInfo, uint rayCullMode, bool transparentInstance)
{
    CurrentInstance currentInstance;
    currentInstance.bvhOffset = instanceInfo.blas_offset;
    currentInstance.vertexOffset = instanceInfo.vertex_offset;
    currentInstance.bvhLeavesOffset = instanceInfo.blas_leaves_offset;

    uint instanceCullMode = (rayCullMode ^ instanceInfo.invert_triangle_culling) | instanceInfo.disable_triangle_culling;

    currentInstance.cullMode_isNonOpaque_candidateCommitted = instanceCullMode;
    if (transparentInstance)
        currentInstance.cullMode_isNonOpaque_candidateCommitted |= kNonOpaqueInstanceBit;

    return currentInstance;
}

bool IsInstanceNonOpaque(RayTracingAccelStruct accelStruct, uint instanceIndex, uint rayFlags)
{
    bool isTransparent = !accelStruct.instance_infos[instanceIndex].is_opaque;
    if (rayFlags & kRayFlagForceNonOpaque)
        isTransparent = true;
    if (rayFlags & kRayFlagForceOpaque)
        isTransparent = false;

    return isTransparent;
}

float4x3 ConvertToFloat4x3(Transform t)
{
    float4x3 m;
    m[0] = float3(t.row0.x, t.row1.x, t.row2.x);
    m[1] = float3(t.row0.y, t.row1.y, t.row2.y);
    m[2] = float3(t.row0.z, t.row1.z, t.row2.z);
    m[3] = float3(t.row0.w, t.row1.w, t.row2.w);
    return m;
}

float3x4 ConvertToFloat3x4(Transform t)
{
    float3x4 m;
    m[0] = float4(t.row0.x, t.row0.y, t.row0.z, t.row0.w);
    m[1] = float4(t.row1.x, t.row1.y, t.row1.z, t.row1.w);
    m[2] = float4(t.row2.x, t.row2.y, t.row2.z, t.row2.w);
    return m;
}

bool IntersectLeafTriangle(
    StructuredBuffer<uint> vertexBuffer, int vertexOffset, uint4 leafNode, uint triangleCullMode,
    float3 rayDirection, float3 rayOrigin, float tmin, float tmax,
    out CandidateHit hitInfo)
{
    hitInfo = (CandidateHit)0;
    uint3 triangleIndices = leafNode.xyz;
    float3 v1 = FetchVertex(vertexBuffer, 3, vertexOffset, triangleIndices.x);
    float3 v2 = FetchVertex(vertexBuffer, 3, vertexOffset, triangleIndices.y);
    float3 v3 = FetchVertex(vertexBuffer, 3, vertexOffset, triangleIndices.z);

    // Determine edge vectors for clockwise triangle vertices
    const float3 e1 = v2 - v1;
    const float3 e2 = v3 - v1;

    const float3 s1 = cross(rayDirection, e2);
    const float determinant = dot(s1, e1);
    const float invd = rcp(determinant);

    const float3 d = rayOrigin - v1;
    const float u = dot(d, s1) * invd;

    const uint detSignBit = asuint(determinant) & 0x80000000;    
    // Barycentric coordinate U is outside range or triangle front/backface culled
    bool hit = false;
    if (!((u < 0.f) || (u > 1.f) || determinant == 0.0f || detSignBit == triangleCullMode))
    {
        const float3 s2 = cross(d, e1);
        const float v = dot(rayDirection, s2) * invd;
        // Barycentric coordinate V is outside range
        if (!((v < 0.f) || (u + v > 1.f)))
        {
            // Check parametric distance
            const float t = dot(e2, s2) * invd;
            if (!(t < tmin || t > tmax))
            {
                // Accept hit
                hitInfo.isFrontFace_primitiveIndex = (detSignBit ^ 0x80000000) | leafNode.w;
                hitInfo.uv = float2(u, v);
                hitInfo.hitT = t;
                hit = true;
            }
        }
    }
    return hit;
}

struct RayQuery
{
    void Init(uint globalThreadIndex, uint localThreadIndex, RayTracingAccelStruct accelStruct_, uint rayFlags_, uint instanceMask_, Ray ray_)
    {
        accelStruct = accelStruct_;
        rayCullMode_Mask = instanceMask_ & 0x000000FF;
        rayCullMode_Mask |= GetCullMode(rayFlags_);
        rayFlags = rayFlags_;
        rayOriginInWorld = ray_.origin;
        rayDirectionInWorld = ray_.direction;
        tMin = ray_.tMin;
        tMax = ray_.tMax;
        rayOrigin = ray_.origin;
        rayDirection = ray_.direction;
        rayInvDir = 1.0 / ray_.direction;

        stack.Init(globalThreadIndex, localThreadIndex);

        candidateHit = (CandidateHit)0;
        closestHit = (ClosestHit)0;
        closestHit.instanceIndex = INVALID_NODE;
        currentInstance = (CurrentInstance)0;

        currentNodeIndex = accelStruct.bvh[0].parent;  // get root node index from bvh header
        currentInstanceIndex = INVALID_NODE;
        currentLeafTriangleIndex = -1;
    }

    bool Proceed()
    {
        bool transparencyEnabled = !(rayFlags & (UnifiedRT::kRayFlagForceOpaque | UnifiedRT::kRayFlagCullNonOpaque));

        if ((currentInstance.cullMode_isNonOpaque_candidateCommitted & kCandidateHitCommittedBit) && transparencyEnabled)
        {
            currentInstance.cullMode_isNonOpaque_candidateCommitted &= ~kCandidateHitCommittedBit;

            _CommitCandidateHit();

            if (rayFlags & kRayFlagAcceptFirstHitAndEndSearch)
            {
                Abort();
                return false;
            }
        }

        currentLeafTriangleIndex++;

        while (currentNodeIndex != INVALID_NODE)
        {
            bool isLeaf = IS_LEAF_NODE(currentNodeIndex);
            bool skipPopStack = false;

            // internal node (Bounding boxes)
            if (!isLeaf)
            {
                BvhNode node;
                if (currentInstanceIndex == INVALID_NODE)
                    node = accelStruct.bvh[1 + currentNodeIndex];
                else
                    node = accelStruct.bottom_bvhs[currentInstance.bvhOffset + 1 + currentNodeIndex];

                uint2 result = IntersectInternalNode(node, rayInvDir, rayOrigin, tMin, tMax);
                if (result.y != INVALID_NODE)
                {
                    stack.Push(result.y);
                }

                if (result.x != INVALID_NODE)
                {
                    currentNodeIndex = result.x;
                    skipPopStack = true;
                }
            }
            // top-level leaf: adjust ray respecively to transforms
            else if (currentInstanceIndex == INVALID_NODE)
            {
                uint currentInstanceIndex_ = GET_LEAF_NODE_FIRST_PRIM(currentNodeIndex);
                uint instanceMask = accelStruct.instance_infos[currentInstanceIndex_].instance_mask;
                bool instanceIsTransparent = IsInstanceNonOpaque(accelStruct, currentInstanceIndex_, rayFlags);

                bool instanceCulled = (instanceMask & rayCullMode_Mask & 0x000000FF) == 0 ||
                    (instanceIsTransparent && (rayFlags & kRayFlagCullNonOpaque)) ||
                    (!instanceIsTransparent && (rayFlags & kRayFlagCullOpaque));

                if (!instanceCulled)
                {
                    // push sentinel
                    stack.Push(kTopLevelSentinel);

                    currentInstanceIndex = currentInstanceIndex_;
                    currentInstance = GetCurrentInstance(accelStruct.instance_infos[currentInstanceIndex_], rayCullMode_Mask & 0xC0000000, instanceIsTransparent);
                    currentNodeIndex = accelStruct.bottom_bvhs[currentInstance.bvhOffset + 0].parent;

                    // transform ray into Bottom level space
                    Transform transform = accelStruct.instance_infos[currentInstanceIndex].world_to_local_transform;
                    rayOrigin = TransformPointT(rayOriginInWorld, transform);
                    rayDirection = TransformDirection(rayDirectionInWorld, transform);
                    rayInvDir = 1.0 / rayDirection;

                    skipPopStack = true;
                }
            }
            // bottom-level leaf (triangles)
            else
            {
                int firstTriangle = GET_LEAF_NODE_FIRST_PRIM(currentNodeIndex);
                int nodeTriangleCount = GET_LEAF_NODE_PRIM_COUNT(currentNodeIndex);

                while (currentLeafTriangleIndex < nodeTriangleCount)
                {
                    uint4 leafNode = accelStruct.bottom_bvh_leaves[currentInstance.bvhLeavesOffset + (firstTriangle + currentLeafTriangleIndex)];
                    uint triangleCullMode = (currentInstance.cullMode_isNonOpaque_candidateCommitted & 0xC0000000);
                    bool nonOpaqueInstance = (currentInstance.cullMode_isNonOpaque_candidateCommitted & kNonOpaqueInstanceBit);

                    if (IntersectLeafTriangle(
                        accelStruct.vertexBuffer, currentInstance.vertexOffset, leafNode, triangleCullMode,
                        rayDirection, rayOrigin, tMin, tMax,
                        candidateHit))
                    {
                        if (nonOpaqueInstance && transparencyEnabled)
                            return true;

                        _CommitCandidateHit();

                        if (rayFlags & kRayFlagAcceptFirstHitAndEndSearch)
                        {
                            Abort();
                            return false;
                        }
                    }

                    currentLeafTriangleIndex++;
                }

                currentLeafTriangleIndex = 0;
            }

            if (skipPopStack)
                continue;

            currentNodeIndex = stack.Pop();

            // check if need to go back to the top-level
            if (currentNodeIndex == kTopLevelSentinel)
            {
                currentNodeIndex = stack.Pop();
                currentInstanceIndex = INVALID_NODE;

                // restore ray
                rayOrigin = rayOriginInWorld;
                rayDirection= rayDirectionInWorld;
                rayInvDir = 1.0 / rayDirectionInWorld;
            }
        }

        return false;
    }

    void Abort()
    {
        currentNodeIndex = INVALID_NODE;
    }

    void CommitNonOpaqueTriangleHit()
    {
        currentInstance.cullMode_isNonOpaque_candidateCommitted |= kCandidateHitCommittedBit;
    }

    void _CommitCandidateHit()
    {
        closestHit.instanceIndex = currentInstanceIndex;
        closestHit.isFrontFace_primitiveIndex = candidateHit.isFrontFace_primitiveIndex;
        closestHit.uv = candidateHit.uv;
        tMax = candidateHit.hitT;
    }

    uint RayFlags() { return rayFlags; }
    float3 WorldRayOrigin() { return rayOriginInWorld; }
    float3 WorldRayDirection() { return rayDirectionInWorld; }
    float RayTMin() { return tMin; }

    float CandidateTriangleRayT() { return candidateHit.hitT; }
    uint CandidateInstanceID() { return accelStruct.instance_infos[currentInstanceIndex].user_instance_id; }
    uint CandidatePrimitiveIndex() { return candidateHit.isFrontFace_primitiveIndex & 0x7FFFFFFF; }
    float2 CandidateTriangleBarycentrics() { return candidateHit.uv; }
    bool CandidateTriangleFrontFace() { return candidateHit.isFrontFace_primitiveIndex & 0x80000000; }
    float3 CandidateLocalRayOrigin() { return rayOrigin; }
    float3 CandidateLocalRayDirection() { return rayDirection; }
    float3x4 CandidateWorldToLocal3x4() { return ConvertToFloat3x4(accelStruct.instance_infos[currentInstanceIndex].world_to_local_transform); }
    float4x3 CandidateWorldToLocal4x3() { return ConvertToFloat4x3(accelStruct.instance_infos[currentInstanceIndex].world_to_local_transform); }
    float3x4 CandidateLocalToWorld3x4() { return ConvertToFloat3x4(accelStruct.instance_infos[currentInstanceIndex].local_to_world_transform); }
    float4x3 CandidateLocalToWorld4x3() { return ConvertToFloat4x3(accelStruct.instance_infos[currentInstanceIndex].local_to_world_transform); }

    uint CommittedStatus() { return closestHit.instanceIndex == -1 ? kCommittedNothing : kCommittedTriangleHit; }
    float CommittedRayT() { return tMax; }
    uint CommittedInstanceID() { return accelStruct.instance_infos[closestHit.instanceIndex].user_instance_id;  }
    uint CommittedPrimitiveIndex() { return closestHit.isFrontFace_primitiveIndex & 0x7FFFFFFF; }
    float2 CommittedTriangleBarycentrics() { return closestHit.uv; }
    bool CommittedTriangleFrontFace() { return closestHit.isFrontFace_primitiveIndex & 0x80000000; }
    float3 CommittedLocalRayOrigin() { return TransformPointT(rayOriginInWorld, accelStruct.instance_infos[closestHit.instanceIndex].world_to_local_transform); }
    float3 CommittedLocalRayDirection() { return TransformDirection(rayDirectionInWorld, accelStruct.instance_infos[closestHit.instanceIndex].world_to_local_transform); }
    float3x4 CommittedWorldToLocal3x4() { return ConvertToFloat3x4(accelStruct.instance_infos[closestHit.instanceIndex].world_to_local_transform); }
    float4x3 CommittedWorldToLocal4x3() { return ConvertToFloat4x3(accelStruct.instance_infos[closestHit.instanceIndex].world_to_local_transform); }
    float3x4 CommittedLocalToWorld3x4() { return ConvertToFloat3x4(accelStruct.instance_infos[closestHit.instanceIndex].local_to_world_transform); }
    float4x3 CommittedLocalToWorld4x3() { return ConvertToFloat4x3(accelStruct.instance_infos[closestHit.instanceIndex].local_to_world_transform); }

    // read only data
    uint rayFlags;
    uint rayCullMode_Mask;
    float3 rayOriginInWorld;
    float3 rayDirectionInWorld;
    float tMin;
    RayTracingAccelStruct accelStruct;

    // traversal state
    float tMax;
    float3 rayOrigin;
    float3 rayDirection;
    float3 rayInvDir;
    ClosestHit closestHit;
    CandidateHit candidateHit;
    RayTraversalStack stack;
    CurrentInstance currentInstance;
    uint currentNodeIndex;
    uint currentInstanceIndex;
    int currentLeafTriangleIndex;
};


} // namespace UnifiedRT

#endif  // _UNIFIEDRAYTRACING_RAYQUERYSOFTWARE_HLSL_
