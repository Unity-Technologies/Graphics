#include "SubsurfaceScatteringSettings.cs.hlsl"
#include "ShaderLibrary\Packing.hlsl"
#include "CommonSubsurfaceScattering.hlsl"

// Subsurface scattering constant
#define SSS_WRAP_ANGLE (PI/12)              // 15 degrees
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

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
float4 _WorldScales[SSS_N_PROFILES];        // X = meters per world unit; Y = world units per meter
CBUFFER_END

// ----------------------------------------------------------------------------
// helper functions
// ----------------------------------------------------------------------------

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
    float  subsurfaceRadius;
    int    subsurfaceProfile;
};

#define SSSBufferType0 float4

// SSSBuffer texture declaration
TEXTURE2D(_SSSBufferTexture0);

void EncodeIntoSSSBuffer(SSSData sssData, uint2 positionSS, out SSSBufferType0 outSSSBuffer0)
{
    outSSSBuffer0 = float4(sssData.diffuseColor, PackFloatInt8bit(sssData.subsurfaceRadius, sssData.subsurfaceProfile, 16.0));
}

void DecodeFromSSSBuffer(float4 sssBuffer, uint2 positionSS, out SSSData sssData)
{
    sssData.diffuseColor = sssBuffer.rgb;
    UnpackFloatInt8bit(sssBuffer.a, 16.0, sssData.subsurfaceRadius, sssData.subsurfaceProfile);
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
