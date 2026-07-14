#ifndef SHADOW2D_GEOMETRY_VERTEX_INCLUDED
#define SHADOW2D_GEOMETRY_VERTEX_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
};

Varyings ShadowGeometryVert(Attributes v)
{
    Varyings o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    return o;
}

#endif // SHADOW2D_GEOMETRY_VERTEX_INCLUDED
