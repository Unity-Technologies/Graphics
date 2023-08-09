#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#if SRC_TEXTURE2D_X_ARRAY
TEXTURE2D_ARRAY(_SourceTex);
#else
TEXTURE2D(_SourceTex);
#endif

SamplerState sampler_LinearClamp;
uniform uint _SourceTexArraySlice;
uniform uint _SRGBRead;
uniform uint _SRGBWrite;

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
};

Varyings VertQuad(Attributes input)
{
    Varyings output;
    output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_ScaleBiasRt.x, _ScaleBiasRt.y, 1, 1) + float4(_ScaleBiasRt.z, _ScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1

    //Using temporary as writing to global _ScaleBias.w is prohibited when compiling with DXC
    float scaleBiasW = _ScaleBias.w;
#if UNITY_UV_STARTS_AT_TOP
    // Unity viewport convention is bottom left as origin. Adjust Scalebias to read the correct region.
    scaleBiasW = 1 - _ScaleBias.w - _ScaleBias.y;
#endif
    output.texcoord = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + float2(_ScaleBias.z, scaleBiasW);
    return output;
}

float4 FragBilinear(Varyings input) : SV_Target
{
    float4 outColor;
    float2 uv = input.texcoord.xy;

#if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    // We use stereo eye index to sample the correct slice when resolving foveated targets.
    // Since MirrorView is not a stereo shader we have to populate unity_StereoEyeIndex ourselves.
    unity_StereoEyeIndex = _SourceTexArraySlice;

    uv = RemapFoveatedRenderingResolve(input.texcoord.xy);
#endif

#if SRC_TEXTURE2D_X_ARRAY
    outColor = SAMPLE_TEXTURE2D_ARRAY(_SourceTex, sampler_LinearClamp, uv, _SourceTexArraySlice);
#else
    outColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, uv);
#endif

    if (_SRGBRead && _SRGBWrite)
        return outColor;

    if (_SRGBRead)
        outColor = SRGBToLinear(outColor);

    if (_SRGBWrite)
        outColor = LinearToSRGB(outColor);

    return outColor;
}
