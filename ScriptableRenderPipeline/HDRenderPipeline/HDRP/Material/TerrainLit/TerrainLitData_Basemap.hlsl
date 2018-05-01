//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"
#include "../Lit/LitBuiltinData.hlsl"
#include "../Decal/DecalUtilities.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D(_MetallicTex);
SAMPLER(sampler_MainTex);

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    surfaceData.baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0).rgb;

    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0).a;
    surfaceData.ambientOcclusion = 1;
    surfaceData.metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MainTex, input.texCoord0).r;
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = 0;
    surfaceData.thickness = 1;
    surfaceData.diffusionProfile = 0;

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

#ifdef SURFACE_GRADIENT
    float3 normalTS = float3(0.0, 0.0, 0.0); // No gradient
#else
    float3 normalTS = float3(0.0, 0.0, 1.0);
#endif

    GetNormalWS(input, V, normalTS, surfaceData.normalWS);
    float3 bentNormalWS = surfaceData.normalWS;

    surfaceData.specularOcclusion = 1.0;

#ifndef _DISABLE_DBUFFER
    float alpha = 1;
    AddDecalContribution(posInput, surfaceData, alpha);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base0.uv, _BaseColorMap0, _BaseColorMap0_TexelSize, _BaseColorMap0_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }
#endif

    GetBuiltinData(input, surfaceData, alpha, bentNormalWS, 0, builtinData);
}

#include "TerrainLitDataMeshModification.hlsl"
