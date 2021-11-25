//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, float angleFadeFactor, out DecalSurfaceData surfaceData)
{
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
    // With inspector version of decal we can use instancing to get normal to world access
    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    float fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
    float2 texCoords = input.texCoord0.xy * scale + offset;
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)

    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
    #endif

    float fadeFactor = _DecalBlend.x;
    float2 texCoords = input.texCoord0.xy;
#endif

    float albedoMapBlend = fadeFactor;
    float maskMapBlend = _DecalMaskMapBlueScale * fadeFactor;

    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);

#ifdef _MATERIAL_AFFECTS_EMISSION
    surfaceData.emissive = _EmissiveColor.rgb * fadeFactor;
#ifdef _EMISSIVEMAP
    #if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
    // Fogbugzz 1359282. With projector emissive we can have issue with mips evaluation at the silhouette
    // so perform the processing ourselve. But not all paltform support GetDimensions() so in case it is not
    // supported we just used lof 0
    #if defined(MIP_COUNT_SUPPORTED)
    float2 emissiveColorMapSize;
    float emissiveColorMapLODs;
    _EmissiveColorMap.GetDimensions(0, emissiveColorMapSize.x, emissiveColorMapSize.y, emissiveColorMapLODs);
    float2 uvdx = ddx(texCoords * emissiveColorMapSize), uvdy = ddy(texCoords * emissiveColorMapSize);
    // float lod = 0.5f * log2(dot(uvdx, uvdx) + dot(uvdy, uvdy)) - 1.0f;
    float lod = 0.5f * log2(max(dot(uvdx, uvdx), dot(uvdy, uvdy))) - 1.0f;
    float lddx = ddx(posInput.linearDepth), lddy  = ddy(posInput.linearDepth);
    float ldd = max(dot(lddx, lddx), dot(lddy, lddy));
    float maxlod = emissiveColorMapLODs * (1.0f - 4.0f * ldd);
    surfaceData.emissive *= SAMPLE_TEXTURE2D_LOD(_EmissiveColorMap, sampler_EmissiveColorMap, texCoords, min(lod, maxlod)).rgb;
    #else
    surfaceData.emissive *= SAMPLE_TEXTURE2D_LOD(_EmissiveColorMap, sampler_EmissiveColorMap, texCoords, 0.0).rgb;
    #endif // defined(MIP_COUNT_SUPPORTED)
    #else
    surfaceData.emissive *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, texCoords).rgb;
    #endif // (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
#endif // _EMISSIVEMAP

    // Inverse pre-expose using _EmissiveExposureWeight weight
    float3 emissiveRcpExposure = surfaceData.emissive * GetInverseCurrentExposureMultiplier();
    surfaceData.emissive = lerp(emissiveRcpExposure, surfaceData.emissive, _EmissiveExposureWeight);
#endif // _MATERIAL_AFFECTS_EMISSION

    // Following code match the code in DecalUtilities.hlsl used for cluster. It have the same kind of condition and similar code structure
    surfaceData.baseColor = _BaseColor;
#ifdef _COLORMAP
    surfaceData.baseColor *= SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoords);
 #endif
    surfaceData.baseColor.w *= fadeFactor;
    albedoMapBlend = surfaceData.baseColor.w;
    // outside _COLORMAP because we still have base color for albedoMapBlend
#ifndef _MATERIAL_AFFECTS_ALBEDO
    surfaceData.baseColor.w = 0.0;  // dont blend any albedo - Note: as we already do RT color masking this is not needed, albedo will not be affected anyway
#endif

    // In case of Smoothness / AO / Metal, all the three are always computed but color mask can change
    // Note: We always use a texture here as the decal atlas for transparent decal cluster only handle texture case
    // If no texture is assign it is the white texture
#ifdef _MATERIAL_AFFECTS_MASKMAP
    #ifdef _MASKMAP
    surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoords);
    maskMapBlend *= surfaceData.mask.z; // store before overwriting with smoothness
    #ifdef DECALS_4RT
    surfaceData.mask.x = lerp(_MetallicRemapMin, _MetallicRemapMax, surfaceData.mask.x);
    surfaceData.mask.y = lerp(_AORemapMin, _AORemapMax, surfaceData.mask.y);
    #endif
    surfaceData.mask.z = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.mask.w);
    #else
    #ifdef DECALS_4RT
    surfaceData.mask.x = _Metallic;
    surfaceData.mask.y = _AO;
    #endif
    surfaceData.mask.z = _Smoothness;
    #endif

    surfaceData.mask.w = _MaskBlendSrc ? maskMapBlend : albedoMapBlend;
#endif

    // needs to be after mask, because blend source could be in the mask map blue
    // Note: We always use a texture here as the decal atlas for transparent decal cluster only handle texture case
    // If no texture is assign it is the bump texture (0.0, 0.0, 1.0)
#ifdef _MATERIAL_AFFECTS_NORMAL

#ifdef DECAL_SURFACE_GRADIENT
    #ifdef _NORMALMAP
    float2 deriv = UnpackDerivativeNormalRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoords));
    #else
    float2 deriv = float2(0.0, 0.0);
    #endif

    #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
    float3x3 tangentToWorld = transpose((float3x3)normalToWorld);
    #else
    float3x3 tangentToWorld = input.tangentToWorld;
    #endif

    // Consider oriented decal a volume bump map and use equation 2. in "Bump Mapping Unparametrized Surfaces on the GPU"
    // since the volume gradient is a linear operator (eq. 2 is used in gbuffer pass).
    //
    // For decal projectors, the heightmap can conceptually be thought of being directly (trivially) embedded in the (ambient)
    // world space, with the orthogonal projection direction of the projector being the dimension in which the volume texture
    // doesn't change (is a constant - we only have a 2D map after all) and thus the volume gradient is zero.
    // For mesh projectors, the heightmap is warped along the mesh surface and the volume gradient is zero along each normal.
    // Note: Since we sum volume gradients each having different directions and more importantly, the resulting gradient will most
    // probably have a component colinear with the direction of the mesh (vertex) surface normal of the final decal receiver,
    // it is important to extract from the volume gradient a surface gradient with regard to that final receiver mesh (vertex) normal
    // by removing any component colinear to the later (this is done with SurfaceGradientFromVolumeGradient).
    //
    // This must be done regardless if the shader of the receiver supports surface gradients or not (see DecalUtilities.hlsl:
    // GetDecalSurfaceData will resolve the gradient immediately in that case to return a corresponding perturbed normal from
    // the receiver unperturbed (vertex) surface normal)
    surfaceData.normalWS.xyz = SurfaceGradientFromTBN(deriv, tangentToWorld[0], tangentToWorld[1]);

#else // DECAL_SURFACE_GRADIENT

    #ifdef _NORMALMAP
    float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoords));
    #else
    float3 normalTS = float3(0.0, 0.0, 1.0);
    #endif
    float3 normalWS = float3(0.0, 0.0, 0.0);

    #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
    normalWS = mul((float3x3)normalToWorld, normalTS);
    #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
    #endif

    surfaceData.normalWS.xyz = normalWS;
#endif

    surfaceData.normalWS.w = _NormalBlendSrc ? maskMapBlend : albedoMapBlend;

#endif

    surfaceData.MAOSBlend.xy = float2(surfaceData.mask.w, surfaceData.mask.w);
}
