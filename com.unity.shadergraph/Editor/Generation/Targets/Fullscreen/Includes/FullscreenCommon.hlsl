// Provies missing variables for the FullScreen pass
#ifndef FULLSCREEN_COMMON_INCLUDED
#define FULLSCREEN_COMMON_INCLUDED

struct FragOutput
{
    float4 color : SV_TARGET;
#ifdef DEPTH_WRITE
    float depth : SV_DEPTH;
#endif
};

void BuildVaryings(Attributes input, inout Varyings output)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = output.positionCS * 0.5 + 0.5;
    output.texCoord0.y = 1 - output.texCoord0.y;
#endif

#ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = output.texCoord1;
#endif
}

float4 GetDrawProceduralVertexPosition(uint vertexID)
{
    return GetFullScreenTriangleVertexPosition(vertexID, UNITY_RAW_FAR_CLIP_VALUE);
}

float4 GetBlitVertexPosition(uint vertexID)
{
    float4 positionCS = GetQuadVertexPosition(vertexID);
    positionCS.xy = positionCS.xy * 2 - 1;
    return positionCS;
}

FragOutput DefaultFullscreenFragmentShader(PackedVaryings packedInput)
{
    FragOutput output = (FragOutput)0;
    Varyings unpacked = UnpackVaryings(packedInput);

    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    output.color.rgb = surfaceDescription.Color;
    output.color.a = surfaceDescription.Alpha;
#ifdef DEPTH_WRITE
    output.depth = surfaceDescription.Depth;
#endif

    return output;
}

#endif // FULLSCREEN_COMMON_INCLUDED
