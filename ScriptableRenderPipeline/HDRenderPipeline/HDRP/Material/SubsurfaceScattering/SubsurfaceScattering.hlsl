#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "../DiffusionProfile/DiffusionProfileSettings.cs.hlsl"
#include "../DiffusionProfile/DiffusionProfile.hlsl"

// Subsurface scattering constant
#define SSS_WRAP_ANGLE (PI/12)              // 15 degrees
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

CBUFFER_START(UnitySSSAndTransmissionParameters)
// Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
// Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
uint   _EnableSubsurfaceScattering; // Globally toggles subsurface and transmission scattering on/off
float  _TexturingModeFlags;       // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
float  _TransmissionFlags;        // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
// Old SSS Model >>>
float4 _HalfRcpVariancesAndWeights[DIFFUSION_PROFILE_COUNT][2]; // 2x Gaussians in RGB, A is interpolation weights
// <<< Old SSS Model
// Use float4 to avoid any packing issue between compute and pixel shaders
float4  _ThicknessRemaps[DIFFUSION_PROFILE_COUNT];   // R: start, G = end - start, BA unused
float4 _ShapeParams[DIFFUSION_PROFILE_COUNT];        // RGB = S = 1 / D, A = filter radius
float4 _TransmissionTintsAndFresnel0[DIFFUSION_PROFILE_COUNT];  // RGB = 1/4 * color, A = fresnel0
float4 _WorldScales[DIFFUSION_PROFILE_COUNT];        // X = meters per world unit; Y = world units per meter
CBUFFER_END

// ----------------------------------------------------------------------------
// helper functions
// ----------------------------------------------------------------------------

// Returns the modified albedo (diffuse color) for materials with subsurface scattering.
// Ref: Advanced Techniques for Realistic Real-Time Skin Rendering.
float3 ApplySubsurfaceScatteringTexturingMode(float3 color, int diffusionProfile)
{
#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_SUBSURFACE_SCATTERING)
    // If the SSS pass is executed, we know we have SSS enabled.
    bool enableSss = true;
#else
    bool enableSss = _EnableSubsurfaceScattering != 0;
#endif

    // We can enter in this function even if SSS is not enabled in case of material classification per tile
    // (If there is SSS inside the tile we need to enable the feature for the whole tile with neutral value)
    // thus why we test != DIFFUSION_PROFILE_NEUTRAL_ID here, to be sure neutral profile don't affect the scene
    if (enableSss && diffusionProfile != DIFFUSION_PROFILE_NEUTRAL_ID)
    {
        bool performPostScatterTexturing = IsBitSet(asuint(_TexturingModeFlags), diffusionProfile);

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
    float  subsurfaceMask;
    uint   diffusionProfile;
};

#define SSSBufferType0 float4

// SSSBuffer texture declaration
TEXTURE2D(_SSSBufferTexture0);

// Note: The SSS buffer used here is sRGB
void EncodeIntoSSSBuffer(SSSData sssData, uint2 positionSS, out SSSBufferType0 outSSSBuffer0)
{
    outSSSBuffer0 = float4(sssData.diffuseColor, PackFloatInt8bit(sssData.subsurfaceMask, sssData.diffusionProfile, 16));
}

// Note: The SSS buffer used here is sRGB
void DecodeFromSSSBuffer(float4 sssBuffer, uint2 positionSS, out SSSData sssData)
{
    sssData.diffuseColor = sssBuffer.rgb;
    UnpackFloatInt8bit(sssBuffer.a, 16, sssData.subsurfaceMask, sssData.diffusionProfile);
}

void DecodeFromSSSBuffer(uint2 positionSS, out SSSData sssData)
{
    float4 sssBuffer = LOAD_TEXTURE2D(_SSSBufferTexture0, positionSS);
    DecodeFromSSSBuffer(sssBuffer, positionSS, sssData);
}

// OUTPUT_SSSBUFFER start from SV_Target2 as SV_Target0 and SV_Target1 are used for lighting buffer
#define OUTPUT_SSSBUFFER(NAME) out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target2
#define ENCODE_INTO_SSSBUFFER(SURFACE_DATA, UNPOSITIONSS, NAME) EncodeIntoSSSBuffer(ConvertSurfaceDataToSSSData(SURFACE_DATA), UNPOSITIONSS, MERGE_NAME(NAME, 0))

#define DECODE_FROM_SSSBUFFER(UNPOSITIONSS, SSS_DATA) DecodeFromSSSBuffer(UNPOSITIONSS, SSS_DATA)

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

