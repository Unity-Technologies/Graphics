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

float4 GetInScatteredRadianceAndTransmittance(float2 positionSS, float depthVS,
                                              float globalFogExtinction,
                                              TEXTURE3D(vBuffer), SAMPLER3D(bilinearSampler),
                                              float4 vBufferResolution,
                                              float4 vBufferDepthEncodingParams,
                                              int numSlices = 64)
{
    int   n = numSlices;
    float d = EncodeLogarithmicDepth(depthVS, vBufferDepthEncodingParams);

    float slice0 = clamp(floor(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...
    float slice1 = clamp( ceil(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...

    // We cannot simply perform trilinear interpolation since the distance between slices is Z-encoded.
    float d0 = slice0 * rcp(n) + (0.5 * rcp(n));
    float d1 = slice1 * rcp(n) + (0.5 * rcp(n));
    float z0 = DecodeLogarithmicDepth(d0, vBufferDepthEncodingParams);
    float z1 = DecodeLogarithmicDepth(d1, vBufferDepthEncodingParams);
    float z  = depthVS;

    // Scale and bias the UVs s.t.
    // {0} in the screen space corresponds to {0.5/n} and
    // {1} in the screen space corresponds to {1 - 0.5/n}.
    // TODO: precompute.
    float u = positionSS.x * (vBufferResolution.x - 1) / vBufferResolution.x + 0.5 / vBufferResolution.x;
    float v = positionSS.y * (vBufferResolution.y - 1) / vBufferResolution.y + 0.5 / vBufferResolution.y;

    // Perform 2 bilinear taps.
    float4 v0 = SAMPLE_TEXTURE3D_LOD(vBuffer, bilinearSampler, float3(u, v, d0), 0);
    float4 v1 = SAMPLE_TEXTURE3D_LOD(vBuffer, bilinearSampler, float3(u, v, d1), 0);
    float4 vt = lerp(v0, v1, saturate((z - z0) / (z1 - z0)));

    [flatten] if (z - z1 > 0)
    {
        // Our sample is beyond the far plane of the V-buffer.
        // Apply additional global fog attenuation.
        vt.a += OpticalDepthHomogeneous(globalFogExtinction, z - z1);

        // TODO: extra in-scattering from directional and ambient lights.
    }

    return float4(vt.rgb, Transmittance(vt.a));
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + 0.00001) / max(atDistance, 0.000001);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
