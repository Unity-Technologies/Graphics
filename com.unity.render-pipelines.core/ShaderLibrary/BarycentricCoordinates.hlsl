[maxvertexcount(3)]
void geom(triangle PackedVaryings input[3], inout TriangleStream<PackedVaryings> tristream) {
#ifdef VARYINGS_NEED_BARYCENTRIC
    input[0].barycentric = float3(1, 0, 0);
    input[1].barycentric = float3(0, 1, 0);
    input[2].barycentric = float3(0, 0, 1);
#endif
    tristream.Append(input[0]);
    tristream.Append(input[1]);
    tristream.Append(input[2]);
    tristream.RestartStrip();
}
