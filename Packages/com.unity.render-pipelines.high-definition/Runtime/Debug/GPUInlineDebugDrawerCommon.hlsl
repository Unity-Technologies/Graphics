#ifndef UNITY_GPU_INLINE_DEBUG_DRAWER_COMMON_INCLUDED
#define UNITY_GPU_INLINE_DEBUG_DRAWER_COMMON_INCLUDED

struct AttributesLine
{
    uint vertexID : SV_VertexID;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsLine
{
    float4 positionCS : SV_POSITION;
    float4 color : COLOR0;

    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsLine vert(AttributesLine input, uint instanceID : SV_InstanceID)
{
    VaryingsLine output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#ifdef GPU_INLINE_DEBUG_DRAWER_WS
    GPUInlineDebugDrawerLine newLine = _GPUInlineDebugDrawerLinesWSConsume[instanceID];
#else
    GPUInlineDebugDrawerLine newLine = _GPUInlineDebugDrawerLinesCSConsume[instanceID];
#endif

    if (input.vertexID & 1)
    {
#ifdef GPU_INLINE_DEBUG_DRAWER_WS
        output.positionCS = TransformWorldToHClip(newLine.start);
#else
        output.positionCS = newLine.start;
#endif
        output.color = newLine.startColor;
    }
    else
    {
#ifdef GPU_INLINE_DEBUG_DRAWER_WS
        output.positionCS = TransformWorldToHClip(newLine.end);
#else
        output.positionCS = newLine.end;
#endif
        output.color = newLine.endColor;
    }

    return output;
}

// From x in [_min; _max] to [0.0f, 1.0f]
float Rescale01(float x, float _min, float _max)
{
    return (x - _min) / (_max - _min);
}

// From x in [_min; _max] to [newMin; newMax]
float Rescale(float x, float _min, float _max, float newMin, float newMax)
{
    return Rescale01(x, _min, _max) * (newMax - newMin) + newMin;
}

// From x in [0.0f; 1.0f] to [newMin; newMax]
float RescaleNormalized(float x, float newMin, float newMax)
{
    return x * (newMax - newMin) + newMin;
}

VaryingsLine vertPlotRingBuffer(AttributesLine input, uint instanceID : SV_InstanceID)
{
    VaryingsLine output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    uint startId = _GPUInlineDebugDrawer_PlotRingBufferStartRead[0];
    uint endId = _GPUInlineDebugDrawer_PlotRingBufferEndRead[0];

    float boundMinX = -0.95f;
    float boundMaxX = -0.25f;
    float boundMinY = 0.95f;
    float boundMaxY = 0.25f;

    if (startId == endId)
    {
        // If nothing on the ringBuffer draw outside the screen
        output.positionCS = float4(-2.0f, -2.0f, 0.0f, 1.0f);
    }
    else if (input.vertexID < GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER)
    {
        // Draw the RingBuffer values
        uint curId = (startId + input.vertexID) % GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER;
        float y0 = _GPUInlineDebugDrawer_PlotRingBufferRead[curId];

        float x = Rescale((float)input.vertexID, 0.0f, (float)(GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER - 1), boundMinX, boundMaxX);
        float y = RescaleNormalized(y0, boundMinY, boundMaxY);

        output.positionCS = float4(x, y, 0.0f, 1.0f);
    }
    else
    {
        // Draw the box of the Plot
        float4 box[] = {
                float4(boundMaxX, boundMinY, 0.0f, 1.0f),
                float4(boundMinX, boundMinY, 0.0f, 1.0f),
                float4(boundMinX, boundMaxY, 0.0f, 1.0f),
                float4(boundMaxX, boundMaxY, 0.0f, 1.0f)
            };

        output.positionCS = box[input.vertexID % 4];
    }

    if (input.vertexID < GPUINLINEDEBUGDRAWERPARAMS_MAX_PLOT_RING_BUFFER)
        output.color = float4(1, 0, 0, 1);
    else
        output.color = float4(1, 1, 1, 1);

    return output;
}

float4 frag(VaryingsLine input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return input.color;
}

#endif
