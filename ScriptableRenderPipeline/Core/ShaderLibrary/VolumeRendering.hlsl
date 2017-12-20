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
    float dRcp = rsqrt(dSq);
    float d    = dSq * dRcp;

    // TODO: optimize me. :-(
    float theta0 = FastATan(a * dRcp);
    float theta1 = FastATan(b * dRcp);
    float gamma  = theta1 - theta0;
    float theta  = lerp(theta0, theta1, rndVal);
    float t      = d * tan(theta);

    dist   = originToLightProj + t;
    rSq    = dSq + t * t;
    rcpPdf = gamma * rSq * dRcp;
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}

#define VOLUMETRIC_LIGHTING_ENABLED

#ifdef PRESET_ULTRA
    // E.g. for 1080p: (1920/4)x(1080/4)x(256) = 33,177,600 voxels
    #define VBUFFER_TILE_SIZE   4
    #define VBUFFER_SLICE_COUNT 256
#else
    // E.g. for 1080p: (1920/8)x(1080/8)x(128) =  4,147,200 voxels
    #define VBUFFER_TILE_SIZE   8
    #define VBUFFER_SLICE_COUNT 128
#endif // PRESET_ULTRA

// Point samples {volumetric radiance, optical depth}. Out-of-bounds loads return 0.
float4 LoadInScatteredRadianceAndTransmittance(float2 positionNDC, float linearDepth,
                                               TEXTURE3D(VBufferLighting),
                                               float4 VBufferResolutionAndScale,
                                               float4 VBufferDepthEncodingParams)
{
    int   k = VBUFFER_SLICE_COUNT;
    float z = linearDepth;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    // Account for the visible area of the VBuffer.
    float2 positionSS = positionNDC * (VBufferResolutionAndScale.xy * VBufferResolutionAndScale.zw);
    float  slice      = d * k;

    // Out-of-bounds loads return 0.
    return LOAD_TEXTURE3D(VBufferLighting, float3(positionSS, slice));
}

// Returns linearly interpolated {volumetric radiance, opacity}. The sampler clamps to edge.
float4 SampleInScatteredRadianceAndTransmittance(float2 positionNDC, float linearDepth,
                                                 TEXTURE3D_ARGS(VBufferLighting, linearClampSampler),
                                                 float2 VBufferScale, float4 VBufferDepthEncodingParams)
{
    int   k = VBUFFER_SLICE_COUNT;
    float z = linearDepth;
    float d = EncodeLogarithmicDepth(z, VBufferDepthEncodingParams);

    // Account for the visible area of the VBuffer.
    float2 uv = positionNDC * VBufferScale;

    float4 L;

    [branch] if (d != saturate(d))
    {
        // We are in front of the near or behind the far plane of the V-buffer.
        // The sampler will clamp to edge.
        L = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d), 0);
    }
    else
    {
        // We cannot use hardware trilinear interpolation since the distance between slices is log-encoded.
        // Therefore, we perform 2 bilinear taps.
        // TODO: test the visual difference in practice.
        float s0 = floor(d * k - 0.5);
        float s1 = ceil(d * k - 0.5);
        float d0 = saturate(s0 * rcp(k) + (0.5 * rcp(k)));
        float d1 = saturate(s1 * rcp(k) + (0.5 * rcp(k)));
        float z0 = DecodeLogarithmicDepth(d0, VBufferDepthEncodingParams);
        float z1 = DecodeLogarithmicDepth(d1, VBufferDepthEncodingParams);

        // The sampler will clamp to edge.
        float4 L0 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d0), 0);
        float4 L1 = SAMPLE_TEXTURE3D_LOD(VBufferLighting, linearClampSampler, float3(uv, d1), 0);

        L = lerp(L0, L1, saturate((z - z0) / (z1 - z0)));
    }

    return float4(L.rgb, 1 - Transmittance(L.a));
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
