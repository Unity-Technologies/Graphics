#ifndef UNITY_SDF2D_INCLUDED
#define UNITY_SDF2D_INCLUDED

// Ref: https://www.iquilezles.org/www/articles/distfunctions2d/distfunctions2d.htm

float CircleSDF(float2 position, float radius)
{
    return length(position) - radius;
}

float RectangleSDF(float2 position, float2 bound)
{
    float2 d = abs(position) - bound;
    return length(max(d, float2(0, 0))) + min(max(d.x, d.y), 0.0);
}

float EllipseSDF(float2 position, float2 r)
{
    float2 p  = position;
    float2 r2 = r*r;

    float  k0 = length(p/r);
    float  k1 = length(p/r2);

    return k0*(k0 - 1.0)/k1;
}

#endif // UNITY_SDF2D_INCLUDED
