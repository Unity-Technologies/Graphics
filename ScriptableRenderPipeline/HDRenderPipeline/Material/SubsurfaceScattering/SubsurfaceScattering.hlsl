#include "SubsurfaceScatteringSettings.cs.hlsl"

CBUFFER_START(UnitySSSParameters)
// Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
// Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
uint   _EnableSSSAndTransmission; // Globally toggles subsurface and transmission scattering on/off
float  _TexturingModeFlags;       // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
float  _TransmissionFlags;        // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
                                  // Old SSS Model >>>
uint   _UseDisneySSS;
float4 _HalfRcpVariancesAndWeights[SSS_N_PROFILES][2]; // 2x Gaussians in RGB, A is interpolation weights
                                                       // <<< Old SSS Model
                                                       // Use float4 to avoid any packing issue between compute and pixel shaders
float4  _ThicknessRemaps[SSS_N_PROFILES];   // R: start, G = end - start, BA unused
float4 _ShapeParams[SSS_N_PROFILES];        // RGB = S = 1 / D, A = filter radius
float4 _TransmissionTints[SSS_N_PROFILES];  // RGB = 1/4 * color, A = unused
CBUFFER_END

// Subsurface scattering constant
#define SSS_WRAP_ANGLE (PI/12)              // 15 degrees
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

// ----------------------------------------------------------------------------
// SSS/Transmittance helper
// ----------------------------------------------------------------------------

// Computes the fraction of light passing through the object.
// Evaluate Int{0, inf}{2 * Pi * r * R(sqrt(r^2 + d^2))}, where R is the diffusion profile.
// Note: 'volumeAlbedo' should be premultiplied by 0.25.
// Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar (BSSRDF only).
float3 ComputeTransmittanceDisney(float3 S, float3 volumeAlbedo, float thickness, float radiusScale)
{
    // Thickness and SSS radius are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the radius scale of the profile.
    // thickness /= radiusScale;

#if 0
    float3 expOneThird = exp(((-1.0 / 3.0) * thickness) * S);
#else
    // Help the compiler.
    float  k = (-1.0 / 3.0) * LOG2_E;
    float3 p = (k * thickness) * S;
    float3 expOneThird = exp2(p);
#endif

    // Premultiply & optimize: T = (1/4 * A) * (e^(-t * S) + 3 * e^(-1/3 * t * S))
    return volumeAlbedo * (expOneThird * expOneThird * expOneThird + 3 * expOneThird);
}

// Evaluates transmittance for a linear combination of two normalized 2D Gaussians.
// Ref: Real-Time Realistic Skin Translucency (2010), equation 9 (modified).
// Note: 'volumeAlbedo' should be premultiplied by 0.25, correspondingly 'lerpWeight' by 4,
// and 'halfRcpVariance1' should be prescaled by (0.1 * SssConstants.SSS_BASIC_DISTANCE_SCALE)^2.
float3 ComputeTransmittanceJimenez(float3 halfRcpVariance1, float lerpWeight1,
                                   float3 halfRcpVariance2, float lerpWeight2,
                                   float3 volumeAlbedo, float thickness, float radiusScale)
{
    // Thickness and SSS radius are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the radius scale of the profile.
    // thickness /= radiusScale;

    float t2 = thickness * thickness;

    // T = A * lerp(exp(-t2 * halfRcpVariance1), exp(-t2 * halfRcpVariance2), lerpWeight2)
    return volumeAlbedo * (exp(-t2 * halfRcpVariance1) * lerpWeight1 + exp(-t2 * halfRcpVariance2) * lerpWeight2);
}

// Ref: Steve McAuley - Energy-Conserving Wrapped Diffuse
float ComputeWrappedDiffuseLighting(float NdotL, float w)
{
    return saturate((NdotL + w) / ((1 + w) * (1 + w)));
}

// In order to support subsurface scattering, we need to know which pixels have an SSS material.
// It can be accomplished by reading the stencil buffer.
// A faster solution (which avoids an extra texture fetch) is to simply make sure that
// all pixels which belong to an SSS material are not black (those that don't always are).
// We choose the blue color channel since it's perceptually the least noticeable.
float3 TagLightingForSSS(float3 subsurfaceLighting)
{
    subsurfaceLighting.b = max(subsurfaceLighting.b, HALF_MIN);
    return subsurfaceLighting;
}

// See TagLightingForSSS() for details.
bool TestLightingForSSS(float3 subsurfaceLighting)
{
    return subsurfaceLighting.b > 0;
}

// Returns the modified albedo (diffuse color) for materials with subsurface scattering.
// Ref: Advanced Techniques for Realistic Real-Time Skin Rendering.
float3 ApplyDiffuseTexturingMode(float3 color, int subsurfaceProfile)
{
#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_SUBSURFACE_SCATTERING)
    // If the SSS pass is executed, we know we have SSS enabled.
    bool enableSssAndTransmission = true;
#else
    bool enableSssAndTransmission = _EnableSSSAndTransmission != 0;
#endif

    if (enableSssAndTransmission)
    {
        bool performPostScatterTexturing = IsBitSet(asuint(_TexturingModeFlags), subsurfaceProfile);

        if (performPostScatterTexturing)
        {
            // Post-scatter texturing mode: the albedo is only applied during the SSS pass.
#if !defined(SHADERPASS) || (SHADERPASS != SHADERPASS_SUBSURFACE_SCATTERING)
            color = float3(1, 1, 1);
#endif
        }
        else
        {
            // Pre- and post- scatter texturing mode.
            color = sqrt(color);
        }
    }

    return color;
}

// ----------------------------------------------------------------------------
// Encoding/decoding SSS buffer functions
// ----------------------------------------------------------------------------

struct SSSData
{
    float3 diffuseColor;
    float subsurfaceRadius;
    float subsurfaceProfile;
};

// SSSBuffer texture declaration
TEXTURE2D(_SSSBufferTexture0);

void EncodeIntoSSSBuffer(SSSData sssData, uint2 positionSS, out SSSBufferType0 outSSSBuffer0, out SSSBufferType1 outSSSBuffer1)
{
    outSSSBuffer0 = float4(sssData.diffuseColor, PackFloatInt8bit(sssData.subsurfaceRadius, sssData.subsurfaceProfile, 16.0));
}

void DecodeSSSProfileFromSSSBuffer(SSSBufferType0 inSSSBuffer0, uint2 positionSS, out int subsurfaceProfile)
{
    float unused;
    UnpackFloatInt8bit(inSSSBuffer0.b, 16.0, unused, subsurfaceProfile);
}

void DecodeFromSSSBuffer(uint2 positionSS, out SSSData sssData)
{
    float4 inBuffer = LOAD_TEXTURE2D(_SSSBufferTexture0, positionSS);
    sssData.diffuseColor = LOAD_TEXTURE2D(_SSSBufferTexture0, positionSS).rgb;
    UnpackFloatInt8bit(inBuffer.a, 16.0, sssData.subsurfaceRadius, sssData.subsurfaceProfile);
}

#define OUTPUT_SSSBUFFER(NAME) out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0
#define ENCODE_INTO_SSSBUFFER(SURFACE_DATA, UNPOSITIONSS, NAME) EncodeIntoSSSBuffer(ConvertSurfaceDataToSSSData(SURFACE_DATA), UNPOSITIONSS, MERGE_NAME(NAME, 0))

#define DECODE_FROM_SSSBUFFER(UNPOSITIONSS, SSS_DATA) DecodeFromSSSBuffer(UNPOSITIONSS, SSS_DATA)
