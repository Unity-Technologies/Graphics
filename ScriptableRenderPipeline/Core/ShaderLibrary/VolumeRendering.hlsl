#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, x) dx} = Transmittance(0, a) * Integral{0, b - a}{Transmittance(a, a + x) dx}

float OpticalDepthHomogeneous(float extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 OpticalDepthHomogeneous(float3 extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float Transmittance(float opticalDepth)
{
    return exp(-opticalDepth);
}

float3 Transmittance(float3 opticalDepth)
{
    return exp(-opticalDepth);
}

// Integral{0, b - a}{Transmittance(a, a + x) dx}.
float TransmittanceIntegralHomogeneous(float extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{0, b - a}{Transmittance(a, a + x) dx}.
float3 TransmittanceIntegralHomogeneous(float3 extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

float IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

float HenyeyGreensteinPhasePartConstant(float asymmetry)
{
    float g = asymmetry;

    return INV_FOUR_PI * (1 - g * g);
}

float HenyeyGreensteinPhasePartVarying(float asymmetry, float LdotD)
{
    float g = asymmetry;

    return pow(abs(1 + g * g - 2 * g * LdotD), -1.5);
}

float HenyeyGreensteinPhaseFunction(float asymmetry, float LdotD)
{
    return HenyeyGreensteinPhasePartConstant(asymmetry) *
           HenyeyGreensteinPhasePartVarying(asymmetry, LdotD);
}

// TODO: share this...
#define PRESET_ULTRA 0

#if PRESET_ULTRA
    // E.g. for 1080p: (1920/4)x(1080/4)x(256) = 33,177,600 voxels
    #define VBUFFER_TILE_SIZE   4
    #define VBUFFER_SLICE_COUNT 256
#else
    // E.g. for 1080p: (1920/8)x(1080/8)x(128) =  4,147,200 voxels
    #define VBUFFER_TILE_SIZE   8
    #define VBUFFER_SLICE_COUNT 128
#endif

float4 GetInScatteredRadianceAndTransmittance(float2 positionSS, float depthVS,
                                              TEXTURE3D(VBufferLighting), SAMPLER3D(sampler_LinearXY_PointZ_Clamp),
                                              float4 VBufferDepthEncodingParams, float2 VBufferScale)
{
    int   n = VBUFFER_SLICE_COUNT;
    float z = depthVS;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    float slice0 = clamp(floor(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...
    float slice1 = clamp( ceil(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...

    // We cannot use hardware trilinear interpolation since the distance between slices is log-encoded.
    // TODO: test the visual difference in practice.
    float d0 = slice0 * rcp(n) + (0.5 * rcp(n));
    float d1 = slice1 * rcp(n) + (0.5 * rcp(n));
    float z0 = DecodeLogarithmicDepth(d0, VBufferDepthEncodingParams);
    float z1 = DecodeLogarithmicDepth(d1, VBufferDepthEncodingParams);

    // Account for the visible area of the VBuffer.
    float2 uv = positionSS * VBufferScale;

    // Perform 2 bilinear taps. The sampler should clamp the values at the boundaries of the 3D texture.
    float4 v0 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, sampler_LinearXY_PointZ_Clamp, float3(uv, d0), 0);
    float4 v1 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, sampler_LinearXY_PointZ_Clamp, float3(uv, d1), 0);
    float4 vt = lerp(v0, v1, saturate((z - z0) / (z1 - z0)));

    return float4(vt.rgb, Transmittance(vt.a));
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + 0.00001) / max(atDistance, 0.000001);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
