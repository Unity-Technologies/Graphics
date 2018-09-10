//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "HDRP/Material/BuiltinUtilities.hlsl"
#include "HDRP/Material/MaterialUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    // NEWLITTODO:
    // For now, just use the interpolated vertex normal. This has been normalized in the initial fragment interpolators unpacking.
    // Eventually, we want to share all the LitData LayerTexCoord (and surface gradient frame + uv, planar, triplanar, etc.) logic, also
    // spread in LitDataIndividualLayer and LitDataMeshModification.
    surfaceData.normalWS = input.worldToTangent[2].xyz;

    float2 UV0 = TRANSFORM_TEX(input.texCoord0, _BaseColorMap);

    UV0 *= float2(_MaterialTilingU, _MaterialTilingV);

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------

#ifdef _AXF_BRDF_TYPE_SVBRDF

    surfaceData.diffuseColor = SAMPLE_TEXTURE2D(_SVBRDF_DiffuseColorMap, sampler_SVBRDF_DiffuseColorMap, UV0).xyz * _BaseColor.xyz;
    surfaceData.specularColor = SAMPLE_TEXTURE2D(_SVBRDF_SpecularColorMap, sampler_SVBRDF_SpecularColorMap, UV0).xyz;
    surfaceData.specularLobe = _SVBRDF_SpecularLobeMapScale * SAMPLE_TEXTURE2D(_SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, UV0).xy;

    // Check influence of anisotropy
    //surfaceData.specularLobe.y = lerp(surfaceData.specularLobe.x, surfaceData.specularLobe.y, saturate(_DEBUG_anisotropicRoughessX));

    surfaceData.fresnelF0 = SAMPLE_TEXTURE2D(_SVBRDF_FresnelMap, sampler_SVBRDF_FresnelMap, UV0).x;
    surfaceData.height_mm = SAMPLE_TEXTURE2D(_SVBRDF_HeightMap, sampler_SVBRDF_HeightMap, UV0).x * _SVBRDF_HeightMapMaxMM;
    surfaceData.anisotropyAngle = PI * (2.0 * SAMPLE_TEXTURE2D(_SVBRDF_AnisoRotationMap, sampler_SVBRDF_AnisoRotationMap, UV0).x - 1.0);
    surfaceData.clearcoatColor = SAMPLE_TEXTURE2D(_SVBRDF_ClearcoatColorMap, sampler_SVBRDF_ClearcoatColorMap, UV0).xyz;

    float clearcoatF0 = SAMPLE_TEXTURE2D(_SVBRDF_ClearcoatIORMap, sampler_SVBRDF_ClearcoatIORMap, UV0).x;
    float sqrtF0 = sqrt(clearcoatF0);
    surfaceData.clearcoatIOR = max(1.0, (1.0 + sqrtF0) / (1.00001 - sqrtF0));    // We make sure it's working for F0=1

    // TBN
    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, UV0).xyz - 1.0, surfaceData.normalWS);
    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, UV0).xyz - 1.0, surfaceData.clearcoatNormalWS);

    float alpha = SAMPLE_TEXTURE2D(_SVBRDF_AlphaMap, sampler_SVBRDF_AlphaMap, UV0).x * _BaseColor.w;

    // Useless for SVBRDF
    surfaceData.flakesUV = input.texCoord0;
    surfaceData.flakesMipLevel = 0.0;

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_CAR_PAINT
    //-----------------------------------------------------------------------------

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    surfaceData.diffuseColor = _CarPaint2_CTDiffuse * _BaseColor.xyz;
    surfaceData.clearcoatIOR = max(1.001, _CarPaint2_ClearcoatIOR);    // Can't be exactly 1 otherwise the precise fresnel divides by 0!

    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, UV0).xyz - 1.0, surfaceData.clearcoatNormalWS);
    // surfaceData.normalWS = surfaceData.clearcoatNormalWS; // Use clearcoat normal map as global surface normal map


    // Create mirrored UVs to hide flakes tiling
    surfaceData.flakesUV = _CarPaint2_FlakeTiling * UV0;

    surfaceData.flakesMipLevel = _CarPaint2_BTFFlakeMap.CalculateLevelOfDetail(sampler_CarPaint2_BTFFlakeMap, surfaceData.flakesUV);
    //surfaceData.flakesMipLevel = _DEBUG_clearcoatIOR;      // DEBUG!!!

    if ((int(surfaceData.flakesUV.y) & 1) == 0)
        surfaceData.flakesUV.x += 0.5;
    else if ((uint(1000.0 + surfaceData.flakesUV.x) % 3) == 0)
        surfaceData.flakesUV.y = 1.0 - surfaceData.flakesUV.y;
    else
        surfaceData.flakesUV.x = 1.0 - surfaceData.flakesUV.x;

    // Useless for car paint BSDF
    surfaceData.specularColor = 0;
    surfaceData.specularLobe = 0;
    surfaceData.fresnelF0 = 0;
    surfaceData.height_mm = 0;
    surfaceData.anisotropyAngle = 0;
    surfaceData.clearcoatColor = 0;

    float alpha = 1.0;

#endif

    // Finalize tangent space
    surfaceData.tangentWS = Orthonormalize(input.worldToTangent[0], surfaceData.normalWS);
    surfaceData.biTangentWS = Orthonormalize(input.worldToTangent[1], surfaceData.normalWS);

#ifdef _ALPHATEST_ON
    //NEWLITTODO: Once we include those passes in the main AxF.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
    // and the related properties (in the .shader) and uniforms (in the AxFProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.diffuseColor = GetTextureDataDebug(_DebugMipMapMode, UV0, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.diffuseColor);
    }
#endif

    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    // For back lighting we use the oposite vertex normal
    InitBuiltinData(alpha, surfaceData.normalWS, surfaceData.normalWS, input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
