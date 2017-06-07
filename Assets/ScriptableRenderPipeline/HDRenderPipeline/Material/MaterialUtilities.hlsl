// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

// TODO: Confirm with Ionut but it seems that UNITY_LIGHT_PROBE_PROXY_VOLUME is always define for high end and
// unity_ProbeVolumeParams always bind.
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        float4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

        return SampleSH9(SHCoefficients, normalWS);
    }
    else
    {
        // TODO: Move all this to C++!
        float4x4 identity = 0;
        identity._m00_m11_m22_m33 = 1.0;
        float4x4 WorldToTexture = (unity_ProbeVolumeParams.y == 1.0) ? unity_ProbeVolumeWorldToObject : identity;

        float4x4 translation = identity;
        translation._m30_m31_m32 = -unity_ProbeVolumeMin.xyz;

        float4x4 scale = 0;
        scale._m00_m11_m22_m33 = float4(unity_ProbeVolumeSizeInv.xyz, 1.0);

        WorldToTexture = mul(mul(scale, translation), WorldToTexture);

        return SampleProbeVolumeSH4(TEXTURE3D_PARAM(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionWS, normalWS, WorldToTexture, unity_ProbeVolumeParams.z);
    }

#else

    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    #ifdef LIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap),
                                                        TEXTURE2D_PARAM(unity_LightmapInd, samplerunity_Lightmap),
                                                        uvStaticLightmap, unity_LightmapST, normalWS, true);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST, true);
        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                                                        TEXTURE2D_PARAM(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
                                                        uvDynamicLightmap, unity_DynamicLightmapST, normalWS, false);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false);
        #endif
    #endif

    return bakeDiffuseLighting;

#endif
}

float2 CalculateVelocity(float4 positionCS, float4 previousPositionCS)
{
    // This test on define is required to remove warning of divide by 0 when initializing empty struct
    // TODO: Add forward opaque MRT case...
#if (SHADERPASS == SHADERPASS_VELOCITY) || (SHADERPASS == SHADERPASS_GBUFFER && SHADEROPTIONS_VELOCITY_IN_GBUFFER)
    // Encode velocity
    positionCS.xy = positionCS.xy / positionCS.w;
    previousPositionCS.xy = previousPositionCS.xy / previousPositionCS.w;

    return (positionCS.xy - previousPositionCS.xy) * _ForceNoMotion;
#else
    return float2(0.0, 0.0);
#endif
}

// Flipping or mirroring a normal can be done directly on the tangent space. This has the benefit to apply to the whole process either in surface gradient or not.
// This function will modify FragInputs and this is not propagate outside of GetSurfaceAndBuiltinData(). This is ok as tangent space is not use outside of GetSurfaceAndBuiltinData().
void ApplyDoubleSidedFlipOrMirror(inout FragInputs input)
{
#ifdef _DOUBLESIDED_ON
    // _DoubleSidedConstants is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode
    // To get a flipped normal with the tangent space, we must flip bitangent (because it is construct from the normal) and normal
    // To get a mirror normal with the tangent space, we only need to flip the normal and not the tangent
    float2 flipSign = input.isFrontFace ? float2(1.0, 1.0) : _DoubleSidedConstants.yz; // TOCHECK :  GetOddNegativeScale() is not necessary here as it is apply for tangent space creation.
    input.worldToTangent[1] = flipSign.x * input.worldToTangent[1]; // bitangent
    input.worldToTangent[2] = flipSign.y * input.worldToTangent[2]; // normal

    #ifdef SURFACE_GRADIENT
    // TOCHECK: seems that we don't need to invert any genBasisTB(), sign cancel. Which is expected as we deal with surface gradient.

    // TODO: For surface gradient we must invert or mirror the normal just after the interpolation. It will allow to work with layered with all basis. Currently it is not the case
    #endif
#endif
}

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalAndTangentWS(FragInputs input, float3 V, float3 normalTS, inout float3 normalWS, inout float3 tangentWS)
{
    #ifdef SURFACE_GRADIENT
    normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], normalTS);
    #else
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
    #endif

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    // We assume that the length of the surface normal is sufficiently close to 1.
    // This is use with anisotropic material
    tangentWS = normalize(tangentWS - dot(tangentWS, normalWS) * normalWS);
}
