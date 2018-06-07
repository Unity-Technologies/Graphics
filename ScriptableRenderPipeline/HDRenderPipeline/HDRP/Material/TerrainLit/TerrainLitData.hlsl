//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"
#include "../Lit/LitBuiltinData.hlsl"
#include "../Decal/DecalUtilities.hlsl"

#include "TerrainLitSplatCommon.hlsl"
#include "TerrainLitDataMeshModification.hlsl"

void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    {
        float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_Control0, (input.texCoord0 + 0.5f) * _TerrainHeightmapRecipSize.xy).rgb * 2 - 1;
        float3 normalWS = ((float3x3)GetObjectToWorldMatrix(), normalOS);
        float3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
        float renormFactor = 1.0 / length(normalWS);

        // bitangent on the fly option in xnormal to reduce vertex shader outputs.
        // this is the mikktspace transformation (must use unnormalized attributes)
        float3x3 worldToTangent = CreateWorldToTangent(normalWS, tangentWS.xyz, 1);

        // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
        // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
        input.worldToTangent[0] = worldToTangent[0] * renormFactor;
        input.worldToTangent[1] = worldToTangent[1] * renormFactor;
        input.worldToTangent[2] = worldToTangent[2] * renormFactor;		// normalizes the interpolated vertex normal

        input.texCoord0 *= _TerrainHeightmapRecipSize.zw;
    }
#endif

    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    float3 normalTS;
    TerrainSplatBlend(input.texCoord0, input.worldToTangent[0], input.worldToTangent[1],
        surfaceData.baseColor, normalTS, surfaceData.perceptualSmoothness, surfaceData.metallic);

    surfaceData.ambientOcclusion = 1;
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

    GetNormalWS(input, V, normalTS, surfaceData.normalWS);
    float3 bentNormalWS = surfaceData.normalWS;

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
//#if defined(_MASKMAP0) || defined(_MASKMAP1) || defined(_MASKMAP2) || defined(_MASKMAP3)
//    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(dot(surfaceData.normalWS, V), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
//#else
    surfaceData.specularOcclusion = 1.0;
//#endif

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

    GetBuiltinData(input, surfaceData, 1, bentNormalWS, 0, builtinData);
}
