#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

TEXTURE2D_X(_SourceTex);
SamplerState sampler_LinearClamp;
uniform float4 _ScaleBias;
uniform float4 _ScaleBiasRt;
uniform float _MaxNits;
uniform uint _SourceTexArraySlice;
uniform float _SourceMaxNits;
uniform int _SourceHDREncoding;
uniform float4x4 _ColorTransform;

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
    output.texcoord = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
    return output;
}

float4 FragBilinear(Varyings input) : SV_Target
{
    float4 outColor;
#if defined(USE_TEXTURE2D_X_AS_ARRAY)
    outColor = SAMPLE_TEXTURE2D_ARRAY(_SourceTex, sampler_LinearClamp, input.texcoord.xy, _SourceTexArraySlice);
#else
    outColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, input.texcoord.xy);
#endif


#if HDR_COLORSPACE_CONVERSION_AND_ENCODING
    // Currently this will cause any values in the source color space that go outside of the destination to most likley get clamped
    // the same will be true for any luminance values in the source range outside the destination range. This will lead to hue shifts
    // and over saturated display, e.g. a red value of (1000, 100, 100) if converted to SDR would get clamped at (80,80,80) generating white.
    // TODO: The solution here is to add a hue preserving tonemap operator in as part of the conversion process but this will add quite a bit of extra expense.

    // Convert the encoded output image into linear
    outColor.rgb = InverseOETF(outColor.rgb, _SourceMaxNits, _SourceHDREncoding);
    // Now we need to convert the color space from source to destination;
    outColor.rgb = mul((float3x3)_ColorTransform, outColor.rgb);
    // Convert the linear image into the correct encoded output for the display
    outColor.rgb = OETF(outColor.rgb, _MaxNits);
#endif

    return outColor;
}
