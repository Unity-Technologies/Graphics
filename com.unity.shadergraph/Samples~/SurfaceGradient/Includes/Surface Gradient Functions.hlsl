void TriplanarSwizzle_float(float3 In, out float2 Out_X, out float2 Out_Y, out float2 Out_Z)
{
    Out_X = In.zy;
    Out_Y = In.zx;
    Out_Z = In.xy;
}

void TriplanarSwizzle_half(half3 In, out half2 Out_X, out half2 Out_Y, out half2 Out_Z)
{
    Out_X = In.zy;
    Out_Y = In.zx;
    Out_Z = In.xy;
}