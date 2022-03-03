#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityOITResources.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

    //Texcoord holds the coordinates of the original rendering before post processing.
    output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
    return output;
}


float4 Frag(Varyings input) : SV_Target
{
    uint2 texelCoord = (uint2)input.positionCS.xy;
    uint pixelOffset = texelCoord.y * (uint)_ScreenSize.x + texelCoord.x;

    uint listCount, listOffset;
    VisibilityOIT::GetPixelList(pixelOffset, listCount, listOffset);

    if (listCount == 0)
        return float4(0,0,0,0);

    float3 sumColor = float3(0,0,0);
    for (uint i = 0; i < listCount; ++i)
    {
        uint2 unusedTexelCoords;
        float unusedDepth;
        Visibility::VisibilityData visData;
        VisibilityOIT::GetVisibilitySample(i, listOffset, visData, unusedTexelCoords, unusedDepth);
        sumColor += Visibility::DebugVisIndexToRGB(visData.primitiveID);
    }

    return float4(sumColor,1);
}
