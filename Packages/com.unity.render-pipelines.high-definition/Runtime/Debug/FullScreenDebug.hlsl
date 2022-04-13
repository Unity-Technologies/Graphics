#ifndef UNITY_FULLSCREEN_DEBUG_INCLUDED
#define UNITY_FULLSCREEN_DEBUG_INCLUDED

// For quad overdraw, we need two halfscreen UAV (because a quad is 2x2 pixels). For vertex density, we need one fullscreen UAV.
// These modes are exclusive so we make only one fullscreen allocation for both.
// For vertex density, it stores the number of vertex projected in each pixel.
// For quad overdraw, each 2x2 quad of the UAV contains the overdraw count in top-left pixel and the locked quad id in the top-right pixel. The two other pixels of the quad are unused.
// Because metal doesn't support atomics on textures, this is actually a buffer
RWStructuredBuffer<uint> _FullScreenDebugBuffer : register(u1);

void IncrementVertexDensityCounter(float4 positionCS)
{
    positionCS.xyz /= positionCS.w;
    float3 ndc = float3(positionCS.xy * float2(0.5, (_ProjectionParams.x > 0) ? 0.5 : -0.5) + 0.5, positionCS.z);
    // If vertex is in viewport
    if (all(ndc == saturate(ndc)))
    {
        uint2 pixel = (uint2)(ndc.xy * _ScreenSize.xy);
        InterlockedAdd(_FullScreenDebugBuffer[_ScreenSize.x * (_ScreenSize.y * SLICE_ARRAY_INDEX + pixel.y) + pixel.x], 1);
    }
}

// https://blog.selfshadow.com/2012/11/12/counting-quads/
void IncrementQuadOverdrawCounter(uint2 positionSS, uint primitiveID)
{
#if defined(PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER)
    uint  prevID, thisID = primitiveID + 1;

    uint2 quad = positionSS & ~1;
    uint quad0_idx = _ScreenSize.x * (_ScreenSize.y * SLICE_ARRAY_INDEX + quad.y) + quad.x;
    uint quad1_idx = _ScreenSize.x * (_ScreenSize.y * SLICE_ARRAY_INDEX + quad.y) + quad.x + 1;

    bool processed  = false;
    int  lockCount  = 0;

    for (int i = 0; i < 16; i++)
    {
        if (!processed)
            InterlockedCompareExchange(_FullScreenDebugBuffer[quad1_idx], 0, thisID, prevID);

        [branch]
        if (prevID == 0)
        {
            // Wait a bit, then unlock for other quads
            if (++lockCount == 2)
                InterlockedExchange(_FullScreenDebugBuffer[quad1_idx], 0, prevID);
            processed = true;
        }

        if (prevID == thisID)
            processed = true;
    }

    if (lockCount)
        InterlockedAdd(_FullScreenDebugBuffer[quad0_idx], 1);
#endif
}

#endif // UNITY_FULLSCREEN_DEBUG_INCLUDED
