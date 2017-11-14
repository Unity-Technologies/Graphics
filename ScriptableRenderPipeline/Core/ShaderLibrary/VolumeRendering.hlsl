#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) * Li(t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) * Li(t) dt}.

float OpticalDepthHomogeneousMedia(float extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 OpticalDepthHomogeneousMedia(float3 extinction, float intervalLength)
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

float TransmittanceHomogeneousMedia(float extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedia(extinction, intervalLength));
}

float3 TransmittanceHomogeneousMedia(float3 extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedia(extinction, intervalLength));
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float TransmittanceIntegralHomogeneousMedia(float extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float3 TransmittanceIntegralHomogeneousMedia(float3 extinction, float intervalLength)
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

// Samples the interval of homogeneous participating media using the closed-form tracking approach
// (proportionally to the transmittance times the extinction coefficient).
// Returns the offset from the start of the interval and the weight = (extinction * transmittance / pdf).
// Ref: Production Volume Rendering, 3.6.1.
void ImportanceSampleHomogeneousMedia(float extinction, float intervalLength, float rndVal,
                                      out float offset, out float weight)
{
    // pdf    = extinction * exp(-extinction * t) / (1 - exp(-intervalLength * extinction))
    // weight = extinction * exp(-extinction * t) / pdf
    // weight = 1 - exp(-intervalLength * extinction)

    weight = 1 - exp(-extinction * intervalLength);
    offset = log(1 - rndVal * weight) / extinction;
}

// Implements equiangular light sampling.
// Returns the distance from 0 and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Media.
void ImportanceSamplePunctualLight(float3 lightPosition, float3 rayOrigin, float3 rayDirection,
                                   float tMin, float tMax, float rndVal,
                                   out float dist, out float rcpPdf)
{
    float3 originToLight       = lightPosition - rayOrigin;
    float  originToLightProj   = dot(originToLight, rayDirection);
    float  originToLightDistSq = dot(originToLight, originToLight);
    float  rayToLightDistSq    = max(originToLightDistSq - originToLightProj * originToLightProj, FLT_SMALL);
    float  rayToLightDist      = sqrt(rayToLightDistSq);

    float a = tMin - originToLightProj;
    float b = tMax - originToLightProj;
    float d = rayToLightDist;

    // TODO: optimize me. :-(
    float theta0 = atan(a / d);
    float theta1 = atan(b / d);
    float theta  = lerp(theta0, theta1, rndVal);
    float t      = d * tan(theta);

    dist   = originToLightProj + t;
    rcpPdf = (theta1 - theta0) * (d + t * tan(theta));
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + FLT_SMALL) / max(atDistance, FLT_SMALL);
}

#ifndef USE_LEGACY_UNITY_SHADER_VARIABLES
    #define VOLUMETRIC_LIGHTING_ENABLED
#endif

#ifdef PRESET_ULTRA
    // E.g. for 1080p: (1920/4)x(1080/4)x(256) = 33,177,600 voxels
    #define VBUFFER_TILE_SIZE   4
    #define VBUFFER_SLICE_COUNT 256
#else
    // E.g. for 1080p: (1920/8)x(1080/8)x(128) =  4,147,200 voxels
    #define VBUFFER_TILE_SIZE   8
    #define VBUFFER_SLICE_COUNT 128
#endif // PRESET_ULTRA

float4 GetInScatteredRadianceAndTransmittance(float2 positionSS, float depthVS,
                                              TEXTURE3D(VBufferLighting), SAMPLER3D(linearClampSampler),
                                              float2 VBufferScale, float4 VBufferDepthEncodingParams)
{
    int   n = VBUFFER_SLICE_COUNT;
    float z = depthVS;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    // We cannot use hardware trilinear interpolation since the distance between slices is log-encoded.
    // Therefore, we perform 2 bilinear taps.
    // TODO: test the visual difference in practice.
    float s0 = clamp(floor(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...
    float s1 = clamp( ceil(d * n - 0.5), 0, n - 1); // TODO: somehow avoid the clamp...
    float d0 = s0 * rcp(n) + (0.5 * rcp(n));
    float d1 = s1 * rcp(n) + (0.5 * rcp(n));
    float z0 = DecodeLogarithmicDepth(d0, VBufferDepthEncodingParams);
    float z1 = DecodeLogarithmicDepth(d1, VBufferDepthEncodingParams);

    // Account for the visible area of the VBuffer.
    float2 uv = positionSS * VBufferScale;

    // The sampler should clamp to edge.
    float4 L0 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d0), 0);
    float4 L1 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d1), 0);
    float4 L  = lerp(L0, L1, saturate((z - z0) / (z1 - z0)));

    return float4(L.rgb, Transmittance(L.a));
}

// A version without depth - returns the value for the far plane.
float4 GetInScatteredRadianceAndTransmittance(float2 positionSS,
                                              TEXTURE3D(VBufferLighting), SAMPLER3D(linearClampSampler),
                                              float2 VBufferScale)
{
    // Account for the visible area of the VBuffer.
    float2 uv = positionSS * VBufferScale;

    // The sampler should clamp to edge.
    float4 L = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, 1), 0);

    return float4(L.rgb, Transmittance(L.a));
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
