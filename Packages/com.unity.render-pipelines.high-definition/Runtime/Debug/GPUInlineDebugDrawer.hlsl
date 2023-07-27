#ifndef UNITY_GPU_INLINE_DEBUG_DRAWER_INCLUDED
#define UNITY_GPU_INLINE_DEBUG_DRAWER_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawer.cs.hlsl"

AppendStructuredBuffer<GPUInlineDebugDrawerLine>    _GPUInlineDebugDrawerLinesWSProduce;
StructuredBuffer<GPUInlineDebugDrawerLine>          _GPUInlineDebugDrawerLinesWSConsume;

AppendStructuredBuffer<GPUInlineDebugDrawerLine>    _GPUInlineDebugDrawerLinesCSProduce;
StructuredBuffer<GPUInlineDebugDrawerLine>          _GPUInlineDebugDrawerLinesCSConsume;

RWStructuredBuffer<float>                           _GPUInlineDebugDrawer_PlotRingBuffer;
RWStructuredBuffer<uint>                            _GPUInlineDebugDrawer_PlotRingBufferStart;
RWStructuredBuffer<uint>                            _GPUInlineDebugDrawer_PlotRingBufferEnd;

StructuredBuffer<float>                             _GPUInlineDebugDrawer_PlotRingBufferRead;
StructuredBuffer<uint>                              _GPUInlineDebugDrawer_PlotRingBufferStartRead;
StructuredBuffer<uint>                              _GPUInlineDebugDrawer_PlotRingBufferEndRead;

float2 _GPUInlineDebugDrawerMousePos;

////////////////////////////////////////////////////////////////////
// Helper to constrains the GPUInlineDebugDrawer:
// Example:
// 
// if (GPUInlineDebugDrawer_MouseOnly(positionSS))
// {
//     GPUInlineDebugDrawer_AddLineWS(positionWS, positionWS + float3(1, 0, 0), float3(1, 0, 0));
//     GPUInlineDebugDrawer_AddLineWS(positionWS, positionWS + float3(0, 1, 0), float3(0, 1, 0));
//     GPUInlineDebugDrawer_AddLineWS(positionWS, positionWS + float3(0, 0, 1), float3(0, 0, 1));
// }
// 
// if (GPUInlineDebugDrawer_AroundMouseOnlyDisc(positionSS, 5))
// {
//     GPUInlineDebugDrawer_AddLineWS(positionWS, positionWS + R, float3(1, 0, 0));
// }

bool GPUInlineDebugDrawer_MouseOnly(uint2 id)
{
    return all(id == (uint2)_GPUInlineDebugDrawerMousePos);
}

bool GPUInlineDebugDrawer_AroundMouseOnlyDisc(uint2 id, uint radius)
{
    uint2 delta = (uint2)_GPUInlineDebugDrawerMousePos - id;
    uint normSqr = dot(delta, delta);
    return normSqr < radius*radius;
}

bool GPUInlineDebugDrawer_AroundMouseOnlyBox(uint2 id, uint half_radius)
{
    uint2 minBB = (uint2)_GPUInlineDebugDrawerMousePos - half_radius.xx;
    uint2 maxBB = (uint2)_GPUInlineDebugDrawerMousePos + half_radius.xx;

    return all(minBB < id && id < maxBB);
}

////////////////////////////////////////////////////////////////////
// World Space Lines
void GPUInlineDebugDrawer_AddLineWS(float4 start, float4 end, float3 startColor, float3 endColor)
{
    GPUInlineDebugDrawerLine lineWS = { start, end, float4(startColor, 1.0f), float4(endColor, 1.0f) };
    _GPUInlineDebugDrawerLinesWSProduce.Append(lineWS);
}
void GPUInlineDebugDrawer_AddLineWS(float4 start, float4 end, float3 color = float3(1, 0, 0))
{
    GPUInlineDebugDrawer_AddLineWS(start, end, color, color);
}

void GPUInlineDebugDrawer_AddLineWS(float3 start, float3 end, float3 color = float3(1, 0, 0))
{
    GPUInlineDebugDrawer_AddLineWS(float4(start, 1.0f), float4(end, 1.0f), color, color);
}
void GPUInlineDebugDrawer_AddLineWS(float3 start, float3 end, float3 startColor, float3 endColor)
{
    GPUInlineDebugDrawer_AddLineWS(float4(start, 1.0f), float4(end, 1.0f), startColor, endColor);
}

////////////////////////////////////////////////////////////////////
// Clip Space Lines
void GPUInlineDebugDrawer_AddLineCS(float4 start, float4 end, float3 startColor, float3 endColor)
{
    GPUInlineDebugDrawerLine lineCS = { start, end, float4(startColor, 1.0f), float4(endColor, 1.0f) };
    _GPUInlineDebugDrawerLinesCSProduce.Append(lineCS);
}
void GPUInlineDebugDrawer_AddLineCS(float4 start, float4 end, float3 color = float3(1, 0, 0))
{
    GPUInlineDebugDrawer_AddLineCS(start, end, color, color);
}

void GPUInlineDebugDrawer_AddLineCS(float3 start, float3 end, float3 color = float3(1, 0, 0))
{
    GPUInlineDebugDrawer_AddLineCS(float4(start, 1.0f), float4(end, 1.0f), color, color);
}
void GPUInlineDebugDrawer_AddLineCS(float3 start, float3 end, float3 startColor, float3 endColor)
{
    GPUInlineDebugDrawer_AddLineCS(float4(start, 1.0f), float4(end, 1.0f), startColor, endColor);
}

////////////////////////////////////////////////////////////////////
// Plot Ring Buffer

// x: in [0.0f; 1.0f]
void GPUInlineDebugDrawer_PlotRingBufferAddFloat(float x)
{
    uint id = _GPUInlineDebugDrawer_PlotRingBufferEnd[0];
    _GPUInlineDebugDrawer_PlotRingBuffer[id] = x;

    uint newEndId = (id + 1) % GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER;
    _GPUInlineDebugDrawer_PlotRingBufferEnd[0] = newEndId;

    if (newEndId == _GPUInlineDebugDrawer_PlotRingBufferStart[0])
    {
        _GPUInlineDebugDrawer_PlotRingBufferStart[0] = (newEndId + 1) % GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER;
    }
}

// Useful to clear the PlotRingBuffer
// If the PlotRingBuffer is Clear then the "Window"
// On bottom-left of the screen will not show up.
void GPUInlineDebugDrawer_PlotRingBufferClear()
{
    _GPUInlineDebugDrawer_PlotRingBufferStart[0] = 0;
    _GPUInlineDebugDrawer_PlotRingBufferEnd[0] = 0;
}

#endif // UNITY_GPU_INLINE_DEBUG_DRAWER_INCLUDED
