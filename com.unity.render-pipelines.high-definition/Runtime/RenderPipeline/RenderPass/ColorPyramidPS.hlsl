#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

TEXTURE2D_X_HALF(_Source);
SamplerState sampler_LinearClamp;
uniform half4 _SrcScaleBias;
uniform half4 _SrcUvLimits; // {xy: max uv, zw: offset of blur for 1 texel }
uniform half _SourceMip;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _SrcScaleBias.xy + _SrcScaleBias.zw;
    return output;
}

// half4 Frag(Varyings input) : SV_Target
// {
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

//     // Gaussian weights for 9 texel kernel from center textel to furthest texel. Keep in sync with ColorPyramid.compute
//     const half gaussWeights[] = { 0.27343750, 0.21875000, 0.10937500, 0.03125000, 0.00390625 };

//     half2 offset = _SrcUvLimits.zw;
//     half2 offset1 = offset * (1.0 + (gaussWeights[2] / (gaussWeights[1] + gaussWeights[2])));
//     half2 offset2 = offset * (3.0 + (gaussWeights[4] / (gaussWeights[3] + gaussWeights[4])));

//     half2 uv_m2 = input.texcoord.xy - offset2;
//     half2 uv_m1 = input.texcoord.xy - offset1;
//     half2 uv_p0 = input.texcoord.xy;
//     half2 uv_p1 = min(_SrcUvLimits.xy, input.texcoord.xy + offset1);
//     half2 uv_p2 = min(_SrcUvLimits.xy, input.texcoord.xy + offset2);

//     return
//         + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_m2, _SourceMip) * (gaussWeights[3] + gaussWeights[4])
//         + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_m1, _SourceMip) * (gaussWeights[1] + gaussWeights[2])
//         + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p0, _SourceMip) *  gaussWeights[0]
//         + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p1, _SourceMip) * (gaussWeights[1] + gaussWeights[2])
//         + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p2, _SourceMip) * (gaussWeights[3] + gaussWeights[4]);
// }

half4 Frag(Varyings input) : SV_Target
{
    const half gaussWeights[] = { 0.136498, 0.129188,0.109523,0.083173,0.056577,0.034474,0.018816 };

    half2 offset = _SrcUvLimits.zw / 1.666666666;
    half2 offset1 = offset * (1.0 + (gaussWeights[2] / (gaussWeights[1] + gaussWeights[2])));
    half2 offset2 = offset * (3.0 + (gaussWeights[4] / (gaussWeights[3] + gaussWeights[4])));
    half2 offset3 = offset * (5.0 + (gaussWeights[6] / (gaussWeights[5] + gaussWeights[6])));

    half2 uv_m3 = input.texcoord.xy - offset3;
    half2 uv_m2 = input.texcoord.xy - offset2;
    half2 uv_m1 = input.texcoord.xy - offset1;
    half2 uv_p0 = input.texcoord.xy;
    half2 uv_p1 = min(_SrcUvLimits.xy, input.texcoord.xy + offset1);
    half2 uv_p2 = min(_SrcUvLimits.xy, input.texcoord.xy + offset2);
    half2 uv_p3 = min(_SrcUvLimits.xy, input.texcoord.xy + offset3);

    return
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_m3, _SourceMip) * (gaussWeights[5] + gaussWeights[6])
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_m2, _SourceMip) * (gaussWeights[3] + gaussWeights[4])
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_m1, _SourceMip) * (gaussWeights[1] + gaussWeights[2])
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p0, _SourceMip) *  gaussWeights[0]
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p1, _SourceMip) * (gaussWeights[1] + gaussWeights[2])
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p2, _SourceMip) * (gaussWeights[3] + gaussWeights[4])
        + SAMPLE_TEXTURE2D_X_LOD(_Source, sampler_LinearClamp, uv_p3, _SourceMip) * (gaussWeights[5] + gaussWeights[6]);
}