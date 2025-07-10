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
#define INVALID_NODE 0xFFFFFFFFu
#define INVALID_IDX 0xffffffff

#define BVH_NODE_SIZE 64
#define BVH_NODE_STRIDE_SHIFT 6
#define BVH_NODE_BYTE_OFFSET(i) ((i) << BVH_NODE_STRIDE_SHIFT)

#define LEAF_NODE_INDEX(i) (i | (1 << 31))
#define IS_LEAF_NODE(i) (i & (1 << 31))
#define IS_INTERNAL_NODE(i) (!IS_LEAF_NODE(i))
#define GET_LEAF_NODE_FIRST_PRIM(i) (i & ~0xE0000000)
#define GET_LEAF_NODE_PRIM_COUNT(i) (((i & 0x60000000) >> 29) + 1)

struct BvhNode
{
    uint left_child;
    uint right_child;
    uint parent;
    uint update;

    uint data[12];

    void SetLeftAabb(float3 min, float3 max)
    {
        data[0] = min.x; data[1] = min.y; data[2] = min.z;
        data[3] = max.x; data[4] = max.y; data[5] = max.z;
    }

    /*void SetLeftAabb(Aabb aabb)
    {
        SetLeftAabb(aabb.pmin, aabb.pmax);
    }*/

    void SetRightAabb(float3 min, float3 max)
    {
        data[6] = min.x; data[7] = min.y; data[8] = min.z;
        data[9] = max.x; data[10] = max.y; data[11] = max.z;
    }

    /*void SetRightAabb(Aabb aabb)
    {
        SetRightAabb(aabb.pmin, aabb.pmax);
    }*/

    float3 LeftAabbMin()
    {
        return float3(asfloat(data[0]), asfloat(data[1]), asfloat(data[2]));
    }

    float3 LeftAabbMax()
    {
        return float3(asfloat(data[3]), asfloat(data[4]), asfloat(data[5]));
    }

    float3 RightAabbMin()
    {
        return float3(asfloat(data[6]), asfloat(data[7]), asfloat(data[8]));
    }

    float3 RightAabbMax()
    {
        return float3(asfloat(data[9]), asfloat(data[10]), asfloat(data[11]));
    }

};

#define CULL_MODE_FRONTFACE  1
#define CULL_MODE_BACKFACE  -1
#define CULL_MODE_NONE  0

struct InstanceInfo
{
    int blas_offset;
    int instance_mask;
    int vertex_offset;
    int blas_leaves_offset;
    uint disable_triangle_culling;
    uint invert_triangle_culling;
    uint user_instance_id;
    int is_opaque;
    Transform world_to_local_transform;
    Transform local_to_world_transform;
};

bool fast_intersect_triangle(in uint cull_mode,
                             in float3 ray_origin,
                             in float3 ray_direction,
                             in float3 v1,
                             in float3 v2,
                             in float3 v3,
                             in float tmin,
                             inout float closest_t,
                             inout float2 barycentric,
                             inout bool front_face)
{
    float3 e1;
    float3 e2;

    // Determine edge vectors for clockwise triangle vertices
    e1 = v2 - v1;
    e2 = v3 - v1;

    const float3 s1 = cross(ray_direction, e2);
    const float determinant = dot(s1, e1);
    const float invd = rcp(determinant);

    const float3 d = ray_origin - v1;
    const float u = dot(d, s1) * invd;

    front_face = (determinant > 0);

    // Barycentric coordinate U is outside range
    bool hit = false;
    if (!((u < 0.f) || (u > 1.f) || determinant == 0.0f || (asuint(determinant) & 0x80000000) == cull_mode))
    {
        const float3 s2 = cross(d, e1);
        const float v = dot(ray_direction, s2) * invd;

        // Barycentric coordinate V is outside range
        if (!((v < 0.f) || (u + v > 1.f)))
        {
            // Check parametric distance
            const float t = dot(e2, s2) * invd;
            if (!(t < tmin || t > closest_t))
            {
                // Accept hit
                closest_t = t;
                barycentric = float2(u, v);

                hit = true;
            }
        }
    }
    return hit;
}

float min3(float3 val) { return min(min(val.x, val.y), val.z); }

float max3(float3 val) { return max(max(val.x, val.y), val.z); }

// slabs method for Ray-AABB intersection test (Ref: Raytracing Gems 2 Chapter 2)
// relies on IEEE 754 floating point rules for infinity and NaNs to handle case when one or more ray_dir coordinates are 0.
// rays that are exactly passing along a face are considered as non intersecting
float2 fast_intersect_bbox(in float3 ray_origin, in float3 ray_inv_dir, in float3 box_min, in float3 box_max, in float t_min, in float t_max)
{
    float3 f = (box_max - ray_origin) * ray_inv_dir;
    float3 n = (box_min - ray_origin) * ray_inv_dir;
    float3 tmax = max(f, n);
    float3 tmin = min(f, n);
    float max_t = min(min3(tmax), t_max);
    float min_t = max(max3(tmin), t_min);

    return float2(min_t, max_t);
}

uint2 IntersectInternalNode(in BvhNode node, in float3 invd, in float3 o, in float tmin, in float tmax)
{
    float2 s0 = fast_intersect_bbox(o, invd, node.LeftAabbMin(), node.LeftAabbMax(), tmin, tmax);
    float2 s1 = fast_intersect_bbox(o, invd, node.RightAabbMin(), node.RightAabbMax(), tmin, tmax);

    uint traverse0 = (s0.x <= s0.y) ? node.left_child : INVALID_NODE;
    uint traverse1 = (s1.x <= s1.y) ? node.right_child : INVALID_NODE;

    return (s0.x < s1.x && traverse0 != INVALID_NODE) ? uint2(traverse0, traverse1) : uint2(traverse1, traverse0);
}

float3 FetchVertex(StructuredBuffer<uint> vertex_buffer, int vertex_stride, int vertex_offset, uint idx)
{
    uint index_in_floats = vertex_offset + idx * vertex_stride;
    return float3(
        asfloat(vertex_buffer[index_in_floats]),
        asfloat(vertex_buffer[index_in_floats + 1]),
        asfloat(vertex_buffer[index_in_floats + 2]));
}

bool IntersectLeafTriangle(StructuredBuffer<uint> vertex_buffer, int vertex_stride, int vertex_offset, uint4 leaf_node, int cull_mode, in float3 d, in float3 o, in float tmin, inout float closest_t, inout float2 uv, inout bool front_face)
{
    uint3 triangle_indices = leaf_node.xyz;
    float3 v0 = FetchVertex(vertex_buffer, vertex_stride, vertex_offset, triangle_indices.x);
    float3 v1 = FetchVertex(vertex_buffer, vertex_stride, vertex_offset, triangle_indices.y);
    float3 v2 = FetchVertex(vertex_buffer, vertex_stride, vertex_offset, triangle_indices.z);

    return fast_intersect_triangle(cull_mode, o, d, v0, v1, v2, tmin, closest_t, uv, front_face);
}
