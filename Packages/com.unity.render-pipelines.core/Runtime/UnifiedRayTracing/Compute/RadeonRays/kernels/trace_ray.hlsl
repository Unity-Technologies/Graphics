/**********************************************************************
Copyright (c) 2019 Advanced Micro Devices, Inc. All rights reserved.
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
********************************************************************/
#ifndef TRACE_RAY_HLSL
#define TRACE_RAY_HLSL

#include "intersect_structures.hlsl"
#include "transform.hlsl"
#include "intersector_common.hlsl"

#pragma warning(disable : 4008) // // fast_intersect_bbox is designed to handle inf and nans, so we disable the `floating point division by zero` warning
#ifndef GROUP_SIZE
#define GROUP_SIZE 128
#endif

#define LDS_STACK_SIZE 8
#define STACK_SIZE 64
#define TOP_LEVEL_SENTINEL 0xFFFFFFFE
groupshared uint lds_stack[LDS_STACK_SIZE * GROUP_SIZE];

void PushStack(RWStructuredBuffer<uint> stack, in uint addr, inout uint lds_sptr, inout uint lds_sbegin, inout uint sptr, inout uint sbegin)
{
    if (lds_sptr - lds_sbegin >= LDS_STACK_SIZE)
    {
        for (int i = 1; i < LDS_STACK_SIZE; ++i)
        {
            stack[sptr + i] = lds_stack[lds_sbegin + i];
        }

        sptr += LDS_STACK_SIZE;
        lds_sptr = lds_sbegin + 1;
    }
    lds_stack[lds_sptr++] = addr;
}

uint PopStack(RWStructuredBuffer<uint> stack, inout uint lds_sptr, inout uint lds_sbegin, inout uint sptr, inout uint sbegin)
{
    uint addr = lds_stack[--lds_sptr];
    if (addr == INVALID_NODE && sptr > sbegin)
    {
        sptr -= LDS_STACK_SIZE;
        for (int i = 1; i < LDS_STACK_SIZE; ++i)
        {
            lds_stack[lds_sbegin + i] = stack[sptr + i];
        }
        lds_sptr = lds_sbegin + LDS_STACK_SIZE;
        addr = lds_stack[--lds_sptr];
    }
    return addr;
}

/**
 * @brief Trace rays againsts given acceleration structure
 *
 **/

struct TraceHitResult
{
    float2 uv;
    uint   inst_id;
    uint   prim_id;
    float  hit_distance;
    bool   front_face;
};

struct TraceParams
{
    StructuredBuffer<BvhNode> bvh;
    StructuredBuffer<BvhNode> bottom_bvhs;
    StructuredBuffer<uint4> bottom_bvh_leaves;
    StructuredBuffer<uint> bottom_bvhs_vertices;
    int bottom_bvhs_vertex_stride;

    RWStructuredBuffer<uint> stack;
    StructuredBuffer<InstanceInfo> instance_infos;

    uint globalThreadIndex;
    uint localThreadIndex;
};

TraceHitResult TraceRaySoftware(
    TraceParams params,
    float3 rayOrigin, float tmin, float3 rayDirection, float tmax,
    uint rayMask,
    int ray_cull_mode,
    bool closestHit)
{
    // define stack start
    uint sbegin = STACK_SIZE * params.globalThreadIndex;
    uint sptr = sbegin;
    uint lds_sbegin = params.localThreadIndex * LDS_STACK_SIZE;
    uint lds_sptr = lds_sbegin;
    lds_stack[lds_sptr++] = INVALID_NODE;
    // prepare ray info for trace
    float3 ray_o = rayOrigin;
    float3 ray_d = rayDirection;
    float  ray_mint = tmin;
    float  ray_maxt = tmax;
    float3 ray_invd = 1.0 / ray_d; // fast_intersect_bbox is designed to handle inf and nans

    float ray_length = 1.0f;
    bool intersection_found_in_bottom_level = false;
    // instance index for scene
    uint current_instance = INVALID_NODE;
    uint closest_instance = INVALID_NODE;
    uint closest_prim = INVALID_NODE;
    float2 closest_uv = float2(0.f, 0.f);
    bool closest_front_face = true;

    int vertex_offset = 0;
    int bottom_bvh_leaves_offset = 0;
    int cull_mode = ray_cull_mode;

    // get root node index from bvh header
    uint node_index = params.bvh[0].parent;
    uint bottom_bvh_offset;

    while (node_index != INVALID_NODE )
    {
        bool is_leaf = IS_LEAF_NODE(node_index);
        bool skip_popstack = false;

        if (!is_leaf)
        {
            BvhNode node;
            if (current_instance == INVALID_NODE)
            {
                node = params.bvh[1 + node_index];
            }
            else
            {
                node = params.bottom_bvhs[bottom_bvh_offset + 1 + node_index];
            }

            uint2 result = IntersectInternalNode(node, ray_invd, ray_o, ray_mint, ray_maxt);
            if (result.y != INVALID_NODE)
            {
                PushStack(params.stack, result.y, lds_sptr, lds_sbegin, sptr, sbegin);
            }

            if (result.x != INVALID_NODE)
            {
                node_index = result.x;
                skip_popstack = true;
            }
        }
        // top-level leaf: adjust ray respecively to transforms
        else if (current_instance == INVALID_NODE)
        {
            uint instance_in_leaf_node = GET_LEAF_NODE_FIRST_PRIM(node_index);
            uint instance_mask = params.instance_infos[instance_in_leaf_node].instance_mask;

            if ((instance_mask & rayMask) != 0)
            {
                // push sentinel
                PushStack(params.stack, TOP_LEVEL_SENTINEL, lds_sptr, lds_sbegin, sptr, sbegin);

                current_instance = instance_in_leaf_node;
                bottom_bvh_offset   = params.instance_infos[current_instance].blas_offset;
                Transform transform = params.instance_infos[current_instance].world_to_local_transform;
                vertex_offset       = params.instance_infos[current_instance].vertex_offset;
                bottom_bvh_leaves_offset = params.instance_infos[current_instance].blas_leaves_offset;

                cull_mode = ray_cull_mode;
                if (params.instance_infos[current_instance].invert_triangle_culling)
                    cull_mode = -cull_mode;

                if (!params.instance_infos[current_instance].triangle_culling_enabled)
                    cull_mode = 0;

                node_index = params.bottom_bvhs[bottom_bvh_offset + 0].parent;

                // transform ray into Bottom level space
                intersection_found_in_bottom_level = false;
                ray_o = TransformPointT(ray_o, transform);
                ray_d = TransformDirection(ray_d, transform);
                tmax = ray_maxt;
                ray_length = max3(abs(ray_d));
                ray_d /= ray_length; // rescale ray to avoid floating point precision issues
                ray_maxt *= ray_length;
                ray_mint *= ray_length;
                ray_invd = 1.0 / ray_d;

                skip_popstack = true;
            }
        }
        // bottom-level leaf
        else
        {
            int first_triangle = GET_LEAF_NODE_FIRST_PRIM(node_index);
            int node_triangle_count = GET_LEAF_NODE_PRIM_COUNT(node_index);

            for (int i = 0; i < node_triangle_count; ++i)
            {
                uint4 leafNode = params.bottom_bvh_leaves[bottom_bvh_leaves_offset + (first_triangle + i)];
                uint prim_id = leafNode.w;
                float2 uv = 0.0f;
                bool is_front_face = false;
                if (IntersectLeafTriangle(params.bottom_bvhs_vertices, params.bottom_bvhs_vertex_stride, vertex_offset, leafNode, cull_mode, ray_d, ray_o, ray_mint, ray_maxt, uv, is_front_face))
                {
                    intersection_found_in_bottom_level = true;
                    if (closestHit)
                    {
                        closest_instance = current_instance;
                        closest_prim = prim_id;
                        closest_uv = uv;
                        closest_front_face = is_front_face;
                    }
                    else
                    {
                        TraceHitResult res;
                        res.inst_id = current_instance;
                        res.uv = uv;
                        res.prim_id = prim_id;
                        res.hit_distance = ray_maxt;
                        res.front_face = is_front_face;
                        return res;
                    }
                }
            }
        }

        if (skip_popstack)
            continue;

        node_index = PopStack(params.stack, lds_sptr, lds_sbegin, sptr, sbegin);
        // check if need to go back to the top-level
        if (node_index == TOP_LEVEL_SENTINEL)
        {
            node_index = PopStack(params.stack, lds_sptr, lds_sbegin, sptr, sbegin);
            current_instance = INVALID_NODE;
            // restore ray
            ray_o = rayOrigin;
            ray_d = rayDirection;
            ray_maxt = intersection_found_in_bottom_level ? ray_maxt / ray_length : tmax;
            ray_mint = tmin;
            ray_invd = 1.0 / ray_d; // fast_intersect_bbox is designed to handle inf and nans
        }
    }

    TraceHitResult res;
    if (closestHit && closest_instance != INVALID_NODE)
    {
        res.uv = closest_uv;
        res.prim_id = closest_prim;
        res.inst_id = closest_instance;
        res.hit_distance = ray_maxt;
        res.front_face = closest_front_face;
    }
    else
    {
        res.uv = 0;
        res.prim_id = INVALID_NODE;
        res.inst_id = INVALID_NODE;
        res.hit_distance = FLT_MAX;
        res.front_face = false;
    }

    return res;
}

uint GetUserInstanceID(TraceParams params, int instance_id)
{
    return params.instance_infos[instance_id].user_instance_id;
}
#endif  // TRACE_RAY_HLSL
