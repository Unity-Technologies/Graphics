
#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

uniform float4 _Control0_ST;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 texcoord : TEXCOORD0;
};

float2 ComputeControlUV(float2 uv)
{
    // adjust splatUVs so the edges of the terrain tile lie on pixel centers
    return (uv * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
}

Varyings Vert(uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    output.texcoord.xy = TRANSFORM_TEX(GetFullScreenTriangleTexCoord(vertexID), _Control0);
    output.texcoord.zw = ComputeControlUV(output.texcoord.xy);
    return output;
}

#if SHADERPASS == SHADERPASS_MAINTEX
float4 Frag(Varyings input) : SV_Target
#elif SHADERPASS == SHADERPASS_METALLICTEX
float2 Frag(Varyings input) : SV_Target
#endif
{
    SurfaceDescriptionInputs surfaceDescriptionInputs;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, surfaceDescriptionInputs);

    surfaceDescriptionInputs.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
    surfaceDescriptionInputs.uv0 = input.texcoord;
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

#if SHADERPASS == SHADERPASS_MAINTEX
    return float4(surfaceDescription.BaseColor, surfaceDescription.Smoothness);
#elif SHADERPASS == SHADERPASS_METALLICTEX
    return float2(surfaceDescription.Metallic, surfaceDescription.Occlusion);
#endif
}
