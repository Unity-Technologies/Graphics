#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) * Li(t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) * Li(t) dt}.

float OpticalDepthHomogeneousMedium(float extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 OpticalDepthHomogeneousMedium(float3 extinction, float intervalLength)
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

float TransmittanceHomogeneousMedium(float extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

float3 TransmittanceHomogeneousMedium(float3 extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float TransmittanceIntegralHomogeneousMedium(float extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float3 TransmittanceIntegralHomogeneousMedium(float3 extinction, float intervalLength)
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

// Samples the interval of homogeneous participating medium using the closed-form tracking approach
// (proportionally to the transmittance).
// Returns the offset from the start of the interval and the weight = (transmittance / pdf).
// Ref: Production Volume Rendering, 3.6.1.
void ImportanceSampleHomogeneousMedium(float rndVal, float extinction, float intervalLength,
                                      out float offset, out float weight)
{
    // pdf    = extinction * exp(-extinction * t) / (1 - exp(-intervalLength * extinction))
    // weight = exp(-extinction * t) / pdf
    // weight = (1 - exp(-extinction * intervalLength)) / extinction;

    float x = 1 - exp(-extinction * intervalLength);

    weight = x * rcp(extinction);
    offset = -log(1 - rndVal * x) * rcp(extinction);
}

// Implements equiangular light sampling.
// Returns the distance from the origin of the ray, the squared (radial) distance from the light,
// and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Medium.
void ImportanceSamplePunctualLight(float rndVal, float3 lightPosition,
                                   float3 rayOrigin, float3 rayDirection,
                                   float tMin, float tMax,
                                   out float dist, out float rSq, out float rcpPdf)
{
    float3 originToLight       = lightPosition - rayOrigin;
    float  originToLightProj   = dot(originToLight, rayDirection);
    float  originToLightDistSq = dot(originToLight, originToLight);
    float  rayToLightDistSq    = max(originToLightDistSq - originToLightProj * originToLightProj, FLT_EPS);

    float a    = tMin - originToLightProj;
    float b    = tMax - originToLightProj;
    float dSq  = rayToLightDistSq;
    float d    = sqrt(dSq);
    float dInv = rsqrt(dSq);

    // TODO: optimize me. :-(
    float theta0 = FastATan(a * dInv);
    float theta1 = FastATan(b * dInv);
    float gamma  = theta1 - theta0;
    float theta  = lerp(theta0, theta1, rndVal);
    float t      = d * tan(theta);

    dist   = originToLightProj + t;
    rSq    = dSq + t * t;
    rcpPdf = gamma * rSq * dInv;
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
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

float4 GetInScatteredRadianceAndTransmittance(float2 positionNDC, float linearDepth,
                                              TEXTURE3D(VBufferLighting), SAMPLER3D(linearClampSampler),
                                              float2 VBufferScale, float4 VBufferDepthEncodingParams)
{
    int   n = VBUFFER_SLICE_COUNT;
    float z = linearDepth;
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
    float2 uv = positionNDC * VBufferScale;

    // The sampler should clamp to edge.
    float4 L0 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d0), 0);
    float4 L1 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d1), 0);
    float4 L  = lerp(L0, L1, saturate((z - z0) / (z1 - z0)));

    return float4(L.rgb, Transmittance(L.a));
}

// A version without depth - returns the value for the far plane.
float4 GetInScatteredRadianceAndTransmittance(float2 positionNDC,
                                              TEXTURE3D(VBufferLighting), SAMPLER3D(linearClampSampler),
                                              float2 VBufferScale)
{
    // Account for the visible area of the VBuffer.
    float2 uv = positionNDC * VBufferScale;

    // The sampler should clamp to edge.
    float4 L = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, 1), 0);

    return float4(L.rgb, Transmittance(L.a));
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
