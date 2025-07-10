/**********************************************************************
Copyright (c) 2020 Advanced Micro Devices, Inc. All rights reserved.
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
#ifndef TRANSFORM_HLSL
#define TRANSFORM_HLSL

#ifndef HOST
#include "math.hlsl"
#include "aabb.hlsl"
#endif

//< Transform matrix
struct Transform
{
    float4 row0;
    float4 row1;
    float4 row2;
};

#ifndef HOST
float3 TransformPointT(in float3 p, in Transform transform)
{
    float4 transform_vec[3] = {transform.row0, transform.row1, transform.row2};
    return TransformPoint(p, transform_vec);
}

float3 TransformDirection(in float3 d, in Transform transform)
{
    return  float3(dot(d, transform.row0.xyz),
                   dot(d, transform.row1.xyz),
                   dot(d, transform.row2.xyz));
}

Transform Inverse(in Transform t)
{
    float4x4 m;
    m[0] = float4(t.row0.x, t.row1.x, t.row2.x, 0);
    m[1] = float4(t.row0.y, t.row1.y, t.row2.y, 0);
    m[2] = float4(t.row0.z, t.row1.z, t.row2.z, 0);
    m[3] = float4(t.row0.w, t.row1.w, t.row2.w, 1);

    m = inverse(m);

    Transform res;
    res.row0 = float4(m[0].x, m[1].x, m[2].x, m[3].x);
    res.row1 = float4(m[0].y, m[1].y, m[2].y, m[3].y);
    res.row2 = float4(m[0].z, m[1].z, m[2].z, m[3].z);

    return res;
}

Aabb TransformAabb(in Aabb aabb, in Transform t)
{
    float3 p0 = aabb.pmin;
    float3 p1 = float3(aabb.pmin.x, aabb.pmin.y, aabb.pmax.z);
    float3 p2 = float3(aabb.pmin.x, aabb.pmax.y, aabb.pmin.z);
    float3 p3 = float3(aabb.pmin.x, aabb.pmax.y, aabb.pmax.z);
    float3 p4 = float3(aabb.pmax.x, aabb.pmin.y, aabb.pmax.z);
    float3 p5 = float3(aabb.pmax.x, aabb.pmax.y, aabb.pmin.z);
    float3 p6 = float3(aabb.pmax.x, aabb.pmax.y, aabb.pmax.z);
    float3 p7 = aabb.pmax;

    p0 = TransformPointT(p0, t);
    p1 = TransformPointT(p1, t);
    p2 = TransformPointT(p2, t);
    p3 = TransformPointT(p3, t);
    p4 = TransformPointT(p4, t);
    p5 = TransformPointT(p5, t);
    p6 = TransformPointT(p6, t);
    p7 = TransformPointT(p7, t);

    aabb = CreateEmptyAabb();
    GrowAabb(p0, aabb);
    GrowAabb(p1, aabb);
    GrowAabb(p2, aabb);
    GrowAabb(p3, aabb);
    GrowAabb(p4, aabb);
    GrowAabb(p5, aabb);
    GrowAabb(p6, aabb);
    GrowAabb(p7, aabb);

    return aabb;
}
#endif

#endif  // TRANSFORM_HLSL