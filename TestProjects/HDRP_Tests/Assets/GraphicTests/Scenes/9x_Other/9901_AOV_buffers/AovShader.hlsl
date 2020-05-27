#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

TEXTURE2D(_ColorTexture);
TEXTURE2D(_NormalTexture);

// Vertex shader (procedural fullscreen triangle)
void Vertex(
    uint vertexID : SV_VertexID,
    out float4 positionCS : SV_POSITION,
    out float2 texcoord : TEXCOORD0
)
{
    positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    texcoord = GetFullScreenTriangleTexCoord(vertexID);
}

// Fragment shader
float4 Fragment(
    float4 positionCS : SV_POSITION,
    float2 texcoord : TEXCOORD0
) : SV_Target
{
    uint2 p0 = texcoord * _ScreenSize.xy;

    float4 c0 = LOAD_TEXTURE2D(_ColorTexture, p0);

    return float4(c0);
}
