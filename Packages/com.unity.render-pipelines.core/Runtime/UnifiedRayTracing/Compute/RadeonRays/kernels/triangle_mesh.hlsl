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
#ifndef TRIANGLE_MESH_HLSL
#define TRIANGLE_MESH_HLSL

struct TriangleData
{
    float3 v0;
    float3 v1;
    float3 v2;
};

#ifdef MESH_INDICES_BINDINGS

uint LoadIndex16(uint offset)
{
    uint val = g_indices.Load(((g_indices_offset + offset) / 2u) << 2);
    val = (g_indices_offset + offset) % 2u == 0 ? (val & 0x0000ffff) : (val >> 16);
    return val;
}

#if !(TOP_LEVEL)
uint3 GetFaceIndices(uint tri_idx)
{
    #if UINT16_INDICES
    return g_base_index + uint3(LoadIndex16(3*tri_idx), LoadIndex16(3 * tri_idx + 1), LoadIndex16(3 * tri_idx + 2));
    #else
    return g_base_index + uint3(g_indices.Load((g_indices_offset + 3 * tri_idx)<<2), g_indices.Load((g_indices_offset + 3 * tri_idx + 1)<<2), g_indices.Load((g_indices_offset + 3 * tri_idx + 2)<<2));
    #endif
}
#endif

#endif // MESH_INDICES_BINDINGS

float3 FetchVertex(uint idx)
{
    uint stride_in_floats = g_constants_vertex_stride;
    uint index_in_floats  = g_vertices_offset + idx * stride_in_floats;
    return float3(g_vertices[index_in_floats], g_vertices[index_in_floats + 1], g_vertices[index_in_floats + 2]);
}

TriangleData FetchTriangle(uint3 idx)
{
    TriangleData tri;
    tri.v0 = FetchVertex(idx.x);
    tri.v1 = FetchVertex(idx.y);
    tri.v2 = FetchVertex(idx.z);
    return tri;
}


#endif // TRIANGLE_MESH_HLSL
