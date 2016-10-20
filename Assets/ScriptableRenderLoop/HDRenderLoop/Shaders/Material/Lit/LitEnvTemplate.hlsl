// No guard header!

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateLambertIBLRef(  EnvLightData lightData, BSDFData bsdfData,
                                UNITY_ARGS_ENV(_EnvTextures),
                                uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float3 acc      = float3(0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float3 L;
        float NdotL;
        float weightOverPdf;
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            acc += bsdfData.diffuseColor * Lambert() * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

float3 IntegrateDisneyDiffuseIBLRef(float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    UNITY_ARGS_ENV(_EnvTextures),
                                    uint sampleCount = 2048)
{
    float3 N = bsdfData.normalWS;
    float NdotV = dot(N, V);
    float3 acc  = float3(0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float3 L;
        float NdotL;
        float weightOverPdf;
        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {            
            float3 H = normalize(L + V);
            float LdotH = dot(L, H);
            // Note: we call DisneyDiffuse that require to multiply by Albedo / PI. Divide by PI is already taken into account
            // in weightOverPdf of ImportanceSampleLambert call.
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);
            acc += bsdfData.diffuseColor * disneyDiffuse * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateSpecularGGXIBLRef(  float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    UNITY_ARGS_ENV(_EnvTextures),
                                    uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float NdotV     = saturate(dot(N, V));
    float3 acc      = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;

        // GGX BRDF
        ImportanceSampleGGX(u, V, N, tangentX, tangentY, bsdfData.roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Fresnel component is apply here as describe in ImportanceSampleGGX function
            float3 FweightOverPdf = F_Schlick(bsdfData.fresnel0, VdotH) * weightOverPdf;

            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);

            acc += FweightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  float3 V, float3 positionWS, PreLightData prelightData, EnvLightData lightData, BSDFData bsdfData,
                        UNITY_ARGS_ENV(_EnvTextures),
                        out float4 diffuseLighting,
                        out float4 specularLighting)
{
#ifdef LIT_DISPLAY_REFERENCE

    specularLighting.rgb = IntegrateSpecularGGXIBLRef(V, lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    specularLighting.a = 1.0;

/*
    #ifdef DIFFUSE_LAMBERT_BRDF
    diffuseLighting.rgb = IntegrateLambertIBLRef(lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    #else
    diffuseLighting.rgb = IntegrateDisneyDiffuseIBLRef(V, lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    #endif
    diffuseLighting.a = 1.0;
*/
    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);

#else
    // TODO: factor this code in common, so other material authoring don't require to rewrite everything, 
    // also think about how such a loop can handle 2 cubemap at the same time as old unity. Macro can allow to do that
    // but we need to have UNITY_SAMPLE_ENV_LOD replace by a true function instead that is define by the lighting arcitecture.
    // Also not sure how to deal with 2 intersection....
    // Box and sphere are related to light property (but we have also distance based roughness etc...)

    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);
    
    // Note: As explain in GetPreLightData we use normalWS and not iblNormalWS here (in case of anisotropy)
    float3 rayWS = GetSpecularDominantDir(bsdfData.normalWS, prelightData.iblR, bsdfData.roughness);

    float3 R = rayWS;
    float weight = 1.0;

    // In this code we redefine a bit the behavior of the reflcetion proble. We separate the projection volume (the proxy of the scene) form the influence volume (what pixel on the screen is affected)

    // 1. First determine the projection volume
    
    // In Unity the cubemaps are capture with the localToWorld transform of the component. 
    // This mean that location and oritention matter. So after intersection of proxy volume we need to convert back to world.
    
    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.

    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    if (lightData.envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 rayLS = mul(rayWS, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, rayLS, -boxOuterDistance, boxOuterDistance);

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * rayWS) - lightData.positionWS;
        
        // TODO: add distance based roughness
    } 
    else if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float3 rayLS = mul(rayWS, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, rayLS, sphereOuterDistance);

        R = (positionWS + dist * rayWS) - lightData.positionWS;
    }

    // 2. Apply the influence volume (Box volume is used for culling whatever the influence shape)
    // TODO: In the future we could have an influence volume inside the projection volume (so with a different transform, in this case we will need another transform)
    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else // ENVSHAPETYPE_BOX or ENVSHAPETYPE_NONE 
    {
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }

    // Smooth weighting
    weight = smoothstep01(weight);

    // TODO: we must always perform a weight calculation as due to tiled rendering we need to smooth out cubemap at boundaries.
    // So goal is to split into two category and have an option to say if we parallax correct or not.

    // TODO: compare current Morten version: offline cubemap with a particular remap + the bias in perceptualRoughnessToMipmapLevel
    // to classic remap like unreal/Frobiste. The function GetSpecularDominantDir can result in a better matching in this case
    // We let GetSpecularDominantDir currently as it still an improvement but not as good as it could be
    float mip = perceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness);
    float4 preLD = UNITY_SAMPLE_ENV_LOD(_EnvTextures, R, lightData, mip);
    specularLighting.rgb = preLD.rgb * prelightData.specularFGD;

    // Apply specular occlusion on it
    specularLighting.rgb *= bsdfData.specularOcclusion;
    specularLighting.a = weight;

    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);

#endif    
}

