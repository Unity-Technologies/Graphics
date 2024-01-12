RWStructuredBuffer<float4> Repro_SampleGradient_Branch_Instancing_Buffer;

void Repro_SampleGradient_Branch_Instancing(inout VFXAttributes attributes, in int index)
{
    float4 data = (float4)1.0f;
    data.xyz = attributes.color;
    Repro_SampleGradient_Branch_Instancing_Buffer[index] = data;
}
