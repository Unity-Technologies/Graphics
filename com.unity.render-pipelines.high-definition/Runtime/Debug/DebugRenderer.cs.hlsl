//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DEBUGRENDERER_CS_HLSL
#define DEBUGRENDERER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.DebugRenderer+LineData
// PackingRules = Exact
struct LineData
{
    float4 p0;
    float4 p1;
    float4 color; // x: r y: g z: b w: a 
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.DebugRenderer+LineData
//
float4 GetP0(LineData value)
{
    return value.p0;
}
float4 GetP1(LineData value)
{
    return value.p1;
}
float4 GetColor(LineData value)
{
    return value.color;
}

#endif
