#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"

#if SRC_TEXTURE2D_X_ARRAY
TEXTURE2D_ARRAY(_SourceTex);
#else
TEXTURE2D(_SourceTex);
#endif

SamplerState sampler_LinearClamp;
uniform uint _SourceTexArraySlice;
uniform uint _SRGBRead;
uniform uint _SRGBWrite;
uniform float _MaxNits;
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

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        // We use stereo eye index to sample the correct slice when resolving foveated targets.
        // Since MirrorView is not a stereo shader we have to populate unity_StereoEyeIndex ourselves.
        unity_StereoEyeIndex = _SourceTexArraySlice;

        uv = RemapFoveatedRenderingLinearToNonUniform(input.texcoord.xy);
    }
#endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

#if SRC_TEXTURE2D_X_ARRAY
    outColor = SAMPLE_TEXTURE2D_ARRAY(_SourceTex, sampler_LinearClamp, uv, _SourceTexArraySlice);
#else
    outColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, uv);
#endif

#if HDR_COLORSPACE_CONVERSION_AND_ENCODING
    // Currently this will case any values in the source color space that go outside of the destination to most likley get clamped
    // the same will be true for any luminance values in the source range outside the destination range. This will lead to hue shifts
    // and over saturated display, e.g. a red value of (1000, 100, 100) if converted to SDR would get clamped at (80,80,80) generating white.
    // TODO: The solution here is to add a hue preserving tonemap operator in as part of the conversion process but this will add quite a bit of extra expense.

    // Convert the encoded output image into linear
    outColor.rgb = InverseOETF(outColor.rgb, _SourceMaxNits, _SourceHDREncoding);
    // Now we need to convert the color space from source to destination;
    outColor.rgb = mul((float3x3)_ColorTransform, outColor.rgb);
    // Convert the linear image into the correct encoded output for the display
    outColor.rgb = OETF(outColor.rgb, _MaxNits);
#else
    if (_SRGBRead && _SRGBWrite)
        return outColor;

    if (_SRGBRead)
        outColor = SRGBToLinear(outColor);

    if (_SRGBWrite)
        outColor = LinearToSRGB(outColor);
#endif


    return outColor;
}
