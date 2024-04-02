float3 FloatFunction(in StructuredBuffer<float> buffer)
{
  return float3(buffer[0], 0, buffer[1]);
}