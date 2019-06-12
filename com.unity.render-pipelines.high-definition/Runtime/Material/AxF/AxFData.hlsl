//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
#if defined(_AXF_BRDF_TYPE_SVBRDF) && defined(_AXF_BRDF_TYPE_CAR_PAINT) // Not implemented for BTF
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.diffuseColor.xyz = surfaceData.diffuseColor.xyz * decalSurfaceData.diffuseColor.w + decalSurfaceData.diffuseColor.xyz;
#ifdef _AXF_BRDF_TYPE_SVBRDF
        surfaceData.clearcoatColor.xyz = surfaceData.clearcoatColor.xyz * decalSurfaceData.diffuseColor.w + decalSurfaceData.diffuseColor.xyz;
#endif
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
        // Affect both normal and clearcoat normal
        surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
        surfaceData.clearcoatNormalWS = normalize(surfaceData.clearcoatNormalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
#ifdef DECALS_4RT // only smoothness in 3RT mode
#ifdef _AXF_BRDF_TYPE_SVBRDF
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor;
#endif

        surfaceData.clearcoatIOR = 1.0; // Neutral
        // Note:There is no ambient occlusion with AxF material
#endif

        surfaceData.specularLobe = PerceptualSmoothnessToRoughness(RoughnessToPerceptualSmoothness(surfaceData.specularLobe) * decalSurfaceData.mask.w + decalSurfaceData.mask.z);
    }
#endif
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _DOUBLESIDED_ON
    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal

    float2 UV0 = input.texCoord0.xy * float2(_MaterialTilingU, _MaterialTilingV);

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------

    float alpha = 1.0;

#ifdef _AXF_BRDF_TYPE_SVBRDF

    surfaceData.diffuseColor = SAMPLE_TEXTURE2D(_SVBRDF_DiffuseColorMap, sampler_SVBRDF_DiffuseColorMap, UV0).xyz;
    surfaceData.specularColor = SAMPLE_TEXTURE2D(_SVBRDF_SpecularColorMap, sampler_SVBRDF_SpecularColorMap, UV0).xyz;
    surfaceData.specularLobe = _SVBRDF_SpecularLobeMapScale * SAMPLE_TEXTURE2D(_SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, UV0).xy;

    // The AxF models include both a general coloring term that they call "specular color" while the f0 is actually another term,
    // seemingly always scalar:
    surfaceData.fresnelF0 = SAMPLE_TEXTURE2D(_SVBRDF_FresnelMap, sampler_SVBRDF_FresnelMap, UV0).x;
    surfaceData.height_mm = SAMPLE_TEXTURE2D(_SVBRDF_HeightMap, sampler_SVBRDF_HeightMap, UV0).x * _SVBRDF_HeightMapMaxMM;
    // Our importer range remaps the [-HALF_PI, HALF_PI) range to [0,1). We map back here:
    surfaceData.anisotropyAngle = HALF_PI * (2.0 * SAMPLE_TEXTURE2D(_SVBRDF_AnisoRotationMap, sampler_SVBRDF_AnisoRotationMap, UV0).x - 1.0);
    surfaceData.clearcoatColor = SAMPLE_TEXTURE2D(_SVBRDF_ClearcoatColorMap, sampler_SVBRDF_ClearcoatColorMap, UV0).xyz;
    // The importer transforms the IOR to an f0, we map it back here as an IOR clamped under at 1.0
    // TODO: if we're reusing float textures anyway, we shouldn't need the normalization that transforming to an f0 provides.
    float clearcoatF0 = SAMPLE_TEXTURE2D(_SVBRDF_ClearcoatIORMap, sampler_SVBRDF_ClearcoatIORMap, UV0).x;
    float sqrtF0 = sqrt(clearcoatF0);
    surfaceData.clearcoatIOR = max(1.0, (1.0 + sqrtF0) / (1.00001 - sqrtF0));    // We make sure it's working for F0=1

    // TBN
    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, UV0).xyz - 1.0, surfaceData.normalWS, doubleSidedConstants);
    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, UV0).xyz - 1.0, surfaceData.clearcoatNormalWS, doubleSidedConstants);

    alpha = SAMPLE_TEXTURE2D(_SVBRDF_AlphaMap, sampler_SVBRDF_AlphaMap, UV0).x;

    // Useless for SVBRDF
    surfaceData.flakesUV = input.texCoord0.xy;
    surfaceData.flakesMipLevel = 0.0;

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_CAR_PAINT
    //-----------------------------------------------------------------------------

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    surfaceData.diffuseColor = _CarPaint2_CTDiffuse;
    surfaceData.clearcoatIOR = max(1.001, _CarPaint2_ClearcoatIOR); // Can't be exactly 1 otherwise the precise fresnel divides by 0!

    surfaceData.normalWS = input.tangentToWorld[2].xyz;
    GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, UV0).xyz - 1.0, surfaceData.clearcoatNormalWS, doubleSidedConstants);

    // Create mirrored UVs to hide flakes tiling
    surfaceData.flakesUV = _CarPaint2_FlakeTiling * UV0;

    surfaceData.flakesMipLevel = _CarPaint2_BTFFlakeMap.CalculateLevelOfDetail(sampler_CarPaint2_BTFFlakeMap, surfaceData.flakesUV);

    // TODO_FLAKES: this isn't really tiling
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
#endif

    // Finalize tangent space
    surfaceData.tangentWS = input.tangentToWorld[0];
    if (_Flags & 1) // IsAnisotropic
    {
        float3 tangentTS = float3(1, 0, 0);
        // We will keep anisotropyAngle in surfaceData for now for debug info, register will be freed
        // anyway by the compiler (never used again after this)
        sincos(surfaceData.anisotropyAngle, tangentTS.y, tangentTS.x);
        surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.tangentToWorld);
    }
    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    // Instead of
    // surfaceData.biTangentWS = Orthonormalize(input.tangentToWorld[1], surfaceData.normalWS),
    // make AxF follow what we do in other HDRP shaders for consistency: use the
    // cross product to finish building the TBN frame and thus get a frame matching
    // the handedness of the world space (tangentToWorld can be passed right handed while
    // Unity's WS is left handed, so this makes a difference here).

    // Propagate the geometry normal
    surfaceData.geomNormalWS = input.tangentToWorld[2];

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

#if HAVE_DECALS
    if (_EnableDecals)
    {
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
    }
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        // Not debug streaming information with AxF (this should never be stream)
        surfaceData.diffuseColor = float3(0.0, 0.0, 0.0);
    }

    // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
    // as it can modify attribute use for static lighting
    ApplyDebugToSurfaceData(input.tangentToWorld, surfaceData);
#endif

    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    // No back lighting with AxF
    InitBuiltinData(posInput, alpha, surfaceData.normalWS, surfaceData.normalWS, input.texCoord1, input.texCoord2, builtinData);
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
