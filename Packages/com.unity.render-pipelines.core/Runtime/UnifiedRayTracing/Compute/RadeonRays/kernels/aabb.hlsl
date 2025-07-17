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
#ifndef AABB_HLSL
#define AABB_HLSL

//< Bounding box defined by two corners.
struct Aabb
{
    float3 pmin;
    float3 pmax;
};

#ifndef HOST
#ifndef FLT_MAX
#define FLT_MAX 3.402823e+38
#endif
//< Create an empty bounding box.
Aabb CreateEmptyAabb()
{
    Aabb aabb;
    aabb.pmin = float3(FLT_MAX, FLT_MAX, FLT_MAX);
    aabb.pmax = float3(-FLT_MAX, -FLT_MAX, -FLT_MAX);
    return aabb;
}

//< Extend AABB to encompass a point.
void GrowAabb(in float3 p, inout Aabb aabb)
{
    aabb.pmin = min(aabb.pmin, p);
    aabb.pmax = max(aabb.pmax, p);
}

//< Extend AABB to encompass an AABB point.
void GrowAabb(in Aabb a, inout Aabb aabb)
{
    GrowAabb(a.pmin, aabb);
    GrowAabb(a.pmax, aabb);
}

//< Calculate surface area of an AABB.
float GetAabbSurfaceArea(in Aabb aabb)
{
    float3 e = aabb.pmax - aabb.pmin;
    return 2.f * dot(e, e.zxy);
}
#endif

#endif  // AABB_HLSL