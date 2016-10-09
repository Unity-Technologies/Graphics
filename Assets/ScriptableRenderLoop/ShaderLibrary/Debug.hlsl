#ifndef UNITY_DEBUG_INCLUDED
#define UNITY_DEBUG_INCLUDED

// Given an enum (represented by an int here), return a color.
// Use for DebugView of enum
float3 GetIndexColor(int index)
{
    float3 outColor = float3(1.0, 0.0, 0.0);

    if (index == 0)
        outColor = float3(1.0, 0.5, 0.5);
    else if (index == 1)
        outColor = float3(0.5, 1.0, 0.5);
    else if (index == 2)
        outColor = float3(0.5, 0.5, 1.0);
    else if (index == 3)
        outColor = float3(1.0, 1.0, 0.5);
    else if (index == 4)
        outColor = float3(1.0, 0.5, 1.0);
    else if (index == 5)
        outColor = float3(0.5, 1.0, 1.0);
    else if (index == 6)
        outColor = float3(0.25, 0.75, 1.0);
    else if (index == 7)
        outColor = float3(1.0, 0.75, 0.25);
    else if (index == 8)
        outColor = float3(0.75, 1.0, 0.25);
    else if (index == 9)
        outColor = float3(0.75, 0.25, 1.0);

    return outColor;
}

#endif // UNITY_DEBUG_INCLUDED