// Provies missing variables for the FullScreen pass
#ifndef FULLSCREEN_COMMON_INCLUDED
#define FULLSCREEN_COMMON_INCLUDED

struct FragOutput
{
    float4 color : SV_TARGET;
#ifdef DEPTH_WRITE
    float depth : DEPTH_OFFSET_SEMANTIC;
#endif
};

float4x4 inverse(float4x4 m) {
  float
      a00 = m[0][0], a01 = m[0][1], a02 = m[0][2], a03 = m[0][3],
      a10 = m[1][0], a11 = m[1][1], a12 = m[1][2], a13 = m[1][3],
      a20 = m[2][0], a21 = m[2][1], a22 = m[2][2], a23 = m[2][3],
      a30 = m[3][0], a31 = m[3][1], a32 = m[3][2], a33 = m[3][3],

      b00 = a00 * a11 - a01 * a10,
      b01 = a00 * a12 - a02 * a10,
      b02 = a00 * a13 - a03 * a10,
      b03 = a01 * a12 - a02 * a11,
      b04 = a01 * a13 - a03 * a11,
      b05 = a02 * a13 - a03 * a12,
      b06 = a20 * a31 - a21 * a30,
      b07 = a20 * a32 - a22 * a30,
      b08 = a20 * a33 - a23 * a30,
      b09 = a21 * a32 - a22 * a31,
      b10 = a21 * a33 - a23 * a31,
      b11 = a22 * a33 - a23 * a32,

      det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;

  return float4x4(
      a11 * b11 - a12 * b10 + a13 * b09,
      a02 * b10 - a01 * b11 - a03 * b09,
      a31 * b05 - a32 * b04 + a33 * b03,
      a22 * b04 - a21 * b05 - a23 * b03,
      a12 * b08 - a10 * b11 - a13 * b07,
      a00 * b11 - a02 * b08 + a03 * b07,
      a32 * b02 - a30 * b05 - a33 * b01,
      a20 * b05 - a22 * b02 + a23 * b01,
      a10 * b10 - a11 * b08 + a13 * b06,
      a01 * b08 - a00 * b10 - a03 * b06,
      a30 * b04 - a31 * b02 + a33 * b00,
      a21 * b02 - a20 * b04 - a23 * b00,
      a11 * b07 - a10 * b09 - a12 * b06,
      a00 * b09 - a01 * b07 + a02 * b06,
      a31 * b01 - a30 * b03 - a32 * b00,
      a20 * b03 - a21 * b01 + a22 * b00) / det;
}

// Some render pipeline don't have access to the inverse view projection matrix
// It's okay to compute it in the vertex shader because we only have 3 to 4 vertices
void BuildVaryingsWithoutInverseProjection(Attributes input, inout Varyings output)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.texCoord0 = output.positionCS * 0.5 + 0.5;

#if UNITY_UV_STARTS_AT_TOP
    if (_FlipY < 0.5)
        output.texCoord0.y = 1 - output.texCoord0.y;
#endif

    float3x3 inverseView = (float3x3)inverse(UNITY_MATRIX_V);
    float4x4 inverseProj = inverse(UNITY_MATRIX_P);
    float4 viewDirectionEyeSpace = mul(inverseProj, float4(output.positionCS.xyz, 1));
    float3 viewDirectionWS = mul(inverseView, viewDirectionEyeSpace.xyz).xyz;

    // Encode view direction in texCoord1
    output.texCoord1.xyz = viewDirectionWS;
}

void BuildVaryings(Attributes input, inout Varyings output)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.texCoord0 = output.positionCS * 0.5 + 0.5;

#if UNITY_UV_STARTS_AT_TOP
    if (_FlipY < 0.5)
        output.texCoord0.y = 1 - output.texCoord0.y;
#endif

    float3 p = ComputeWorldSpacePosition(output.positionCS, UNITY_MATRIX_I_VP);

    // Encode view direction in texCoord1
    output.texCoord1.xyz = GetWorldSpaceViewDir(p);
}

float4 GetDrawProceduralVertexPosition(uint vertexID)
{
    return GetFullScreenTriangleVertexPosition(vertexID, UNITY_NEAR_CLIP_VALUE);
}

float4 GetBlitVertexPosition(uint vertexID)
{
    float4 positionCS = GetQuadVertexPosition(vertexID);
    positionCS.xy = positionCS.xy * 2 - 1;
    return positionCS;
}

float4 GetBlitVertexPositionFromPositionOS(float3 positionOS)
{
    return float4(positionOS.xy *  2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
}

FragOutput DefaultFullscreenFragmentShader(PackedVaryings packedInput)
{
    FragOutput output = (FragOutput)0;
    Varyings unpacked = UnpackVaryings(packedInput);

    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    output.color.rgb = surfaceDescription.BaseColor;
    output.color.a = surfaceDescription.Alpha;
#if defined(DEPTH_WRITE)

    float n = _ProjectionParams.y;
    float f = _ProjectionParams.z;

#if defined(DEPTH_WRITE_MODE_EYE)
    // Reverse of LinearEyeDepth
    float d = rcp(max(surfaceDescription.FullscreenEyeDepth, 0.000000001));
    output.depth = (d - _ZBufferParams.w) / _ZBufferParams.z;
#endif

#if defined(DEPTH_WRITE_MODE_LINEAR01)
    // Reverse of Linear01Depth
    float d = rcp(max(surfaceDescription.FullscreenLinear01Depth, 0.000000001));
    output.depth = (d - _ZBufferParams.y) / _ZBufferParams.x;
#endif

#if defined(DEPTH_WRITE_MODE_RAW)
    output.depth = surfaceDescription.FullscreenRawDepth;
#endif

#endif

    return output;
}

#endif // FULLSCREEN_COMMON_INCLUDED
