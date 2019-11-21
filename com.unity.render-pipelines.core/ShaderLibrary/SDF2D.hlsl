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

float EllipseSDF(float2 position, float2 radiuses)
{
    position = abs(position);
    if (position.x > position.y)
    {
        position = position.yx;
        radiuses = radiuses.yx;
    }
    float l = radiuses.y*radiuses.y - radiuses.x*radiuses.x;
    float m = radiuses.x*position.x/l;
    float m2 = m*m; 
    float n = radiuses.y*position.y/l;
    float n2 = n*n; 
    float c = (m2 + n2 - 1.0)/3.0;
    float c3 = c*c*c;
    float q = c3 + m2*n2*2.0;
    float d = c3 + m2*n2;
    float g = m + m*n2;
    float co;
    if (d < 0.0)
    {
        float h = acos(q/c3)/3.0;
        float s = cos(h);
        float t = sin(h)*sqrt(3.0);
        float rx = sqrt(-c*(s + t + 2.0) + m2);
        float ry = sqrt(-c*(s - t + 2.0) + m2);
        co = (ry + sign(l)*rx+abs(g)/(rx*ry) - m)/2.0;
    }
    else
    {
        float h = 2.0*m*n*sqrt(d);
        float s = sign(q + h)*pow(abs(q + h), 1.0/3.0);
        float u = sign(q - h)*pow(abs(q - h), 1.0/3.0);
        float rx = -s - u - c*4.0 + 2.0*m2;
        float ry = (s - u)*sqrt(3.0);
        float rm = sqrt(rx*rx + ry*ry);
        co = (ry/sqrt(rm - rx) + 2.0*g/rm - m)/2.0;
    }
    float2 r = radiuses*float2(co, sqrt(1.0 - co*co));
    return length(r - position)*sign(position.y - r.y);
}

#endif // UNITY_SDF2D_INCLUDED
