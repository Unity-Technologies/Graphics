RWStructuredBuffer<float4> global_debug_buffer;

void CustomHLSL(inout VFXAttributes attributes)
{
    float4 data = (float4)0.0f;
    data.xyz = attributes.color;
    data.w = attributes.size;
    global_debug_buffer[0] = data;
}
