[maxvertexcount(3)]
void geom(triangle PackedVaryings input[3], inout TriangleStream<PackedVaryings> tristream) {
    tristream.Append(input[0]);
    tristream.Append(input[1]);
    tristream.Append(input[2]);
    tristream.RestartStrip();
}
