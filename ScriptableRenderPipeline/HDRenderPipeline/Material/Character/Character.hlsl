#include "Character.cs.hlsl"

SamplerState ltc_linear_clamp_sampler;
// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
TEXTURE2D(_PreIntegratedFGD);

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular and diffuse (in case of DisneyDiffuse)
void GetPreIntegratedFGD(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 specularFGD, out float diffuseFGD)
{
    // Pre-integrate GGX FGD
    //  _PreIntegratedFGD.x = Gv * (1 - Fc)  with Fc = (1 - H.L)^5
    //  _PreIntegratedFGD.y = Gv * Fc
    // Pre integrate DisneyDiffuse FGD:
    // _PreIntegratedFGD.z = DisneyDiffuse
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, ltc_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // f0 * Gv * (1 - Fc) + Gv * Fc
    specularFGD = fresnel0 * preFGD.x + preFGD.y;
    diffuseFGD = 1.0;
}

void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;

        if (overrideSmoothness)
        {
            bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(overrideSmoothnessValue);
            bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
        }
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        bsdfData.diffuseColor = _DebugLightingAlbedo.xyz;
    }
#endif
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);

    bsdfData.fresnel0 = 0.04;

    bsdfData.tangentWS   = surfaceData.tangentWS;
    bsdfData.bitangentWS = cross(surfaceData.normalWS, surfaceData.tangentWS);
    ConvertAnisotropyToRoughness(bsdfData.roughness, surfaceData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
    bsdfData.anisotropy = surfaceData.anisotropy;

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// PreLightData
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;

    // Aniso
    float TdotV;
    float BdotV;

    float3 iblDirWS;    // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblMipLevel;

    float3 specularFGD; // Store preconvole BRDF for both specular and diffuse
    float diffuseFGD;
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, BSDFData bsdfData)
{
    PreLightData preLightData;

    // We have handle the case of NdotV being negative in GetData() function with GetShiftedNdotV.
    // So we don't need to saturate or take the abs here.
    // In case a material use negative normal for double sided lighting like speedtree this will be handle in the GetData() code too.
    preLightData.NdotV = dot(bsdfData.normalWS, V);

    float  iblNdotV    = preLightData.NdotV;
    float3 iblNormalWS = bsdfData.normalWS;

    // Precompute anisotropy
    preLightData.TdotV = dot(bsdfData.tangentWS, V);
    preLightData.BdotV = dot(bsdfData.bitangentWS, V);
    // Tangent = highlight stretch (anisotropy) direction. Bitangent = grain (brush) direction.
    iblNormalWS = GetAnisotropicModifiedNormal(bsdfData.bitangentWS, bsdfData.normalWS, V, bsdfData.anisotropy);

    // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NDotV) and use iblNormalWS
    // into function like GetSpecularDominantDir(). However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here.
    // With this in mind and for performance reasons we chose to only use modified normal to calculate R.

    GetPreIntegratedFGD(iblNdotV, bsdfData.perceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD);

    // We need to take into account the modified normal for faking anisotropic here.
    float3 iblR = reflect(-V, iblNormalWS);
    preLightData.iblDirWS = GetSpecularDominantDir(bsdfData.normalWS, iblR, bsdfData.roughness, preLightData.NdotV);

    preLightData.iblMipLevel = PerceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness);

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// GetBakedDiffuseLigthing function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLigthing(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
    return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

void BSDF(  float3 V, float3 L, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    float NdotL    = saturate(dot(bsdfData.normalWS, L));
    float NdotV    = preLightData.NdotV;
    float LdotV    = dot(L, V);
    float invLenLV = rsqrt(abs(2 + 2 * LdotV));    // invLenLV = rcp(length(L + V))
    float NdotH    = saturate((NdotL + NdotV) * invLenLV);
    float LdotH    = saturate(invLenLV + invLenLV * LdotV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float Vis;
    float D;
    // TODO: this way of handling aniso may not be efficient, or maybe with material classification, need to check perf here
    // Maybe always using aniso maybe a win ?

    float3 H = (L + V) * invLenLV;
    // For anisotropy we must not saturate these values
    float TdotH = dot(bsdfData.tangentWS, H);
    float TdotL = dot(bsdfData.tangentWS, L);
    float BdotH = dot(bsdfData.bitangentWS, H);
    float BdotL = dot(bsdfData.bitangentWS, L);

    bsdfData.roughnessT = ClampRoughnessForAnalyticalLights(bsdfData.roughnessT);
    bsdfData.roughnessB = ClampRoughnessForAnalyticalLights(bsdfData.roughnessB);

    // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
    Vis = V_SmithJointGGXAniso( preLightData.TdotV, preLightData.BdotV, NdotV, TdotL, BdotL, NdotL,
                                bsdfData.roughnessT, bsdfData.roughnessB);

    D = D_GGXAniso(TdotH, BdotH, NdotH, bsdfData.roughnessT, bsdfData.roughnessB);

    specularLighting = F * (Vis * D);

    float  diffuseTerm = Lambert();

    diffuseLighting = bsdfData.diffuseColor * diffuseTerm;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

void EvaluateBSDF_Directional(  LightLoopContext lightLoopContext,
                                float3 V, PositionInputs posInput, PreLightData preLightData, DirectionalLightData lightData, BSDFData bsdfData,
                                out float3 diffuseLighting,
                                out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

    float3 L = -lightData.forward; // Lights are pointing backward in Unity
    float illuminance = saturate(dot(bsdfData.normalWS, L));

    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);
    float4 cookie    = float4(1.0, 1.0, 1.0, 1.0);
	float  shadow = 1;

	[branch] if (lightData.shadowIndex >= 0)
	{
		shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
		illuminance *= shadow;
	}

	[branch] if (lightData.cookieIndex >= 0)
	{
		float3 lightToSurface = positionWS - lightData.positionWS;

		// Project 'lightToSurface' onto the light's axes.
		float2 coord = float2(dot(lightToSurface, lightData.right), dot(lightToSurface, lightData.up));

		// Compute the NDC coordinates (in [-1, 1]^2).
		coord.x *= lightData.invScaleX;
		coord.y *= lightData.invScaleY;

		if (lightData.tileCookie || (abs(coord.x) <= 1 && abs(coord.y) <= 1))
		{
			// Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
			coord = coord * 0.5 + 0.5;

			// Tile the texture if the 'repeat' wrap mode is enabled.
			if (lightData.tileCookie) { coord = frac(coord); }

			cookie = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);
		}
		else
		{
			cookie = float4(0, 0, 0, 0);
		}

		illuminance *= cookie.a;
	}

	[branch] if (illuminance > 0.0)
	{
		BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

		diffuseLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.diffuseScale);
		specularLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.specularScale);
	}
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual( LightLoopContext lightLoopContext,
                            float3 V, PositionInputs posInput, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

    // All punctual light type in the same formula, attenuation is neutral depends on light type.
    // light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
    // mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
    // For point light and directional GetAngleAttenuation() return 1

    float3 unL = lightData.positionWS - positionWS;
    float3 L = normalize(unL);

    float attenuation = GetDistanceAttenuation(unL, lightData.invSqrAttenuationRadius);
    // Reminder: lights are ortiented backward (-Z)
    attenuation *= GetAngleAttenuation(L, -lightData.forward, lightData.angleScale, lightData.angleOffset);
    float illuminance = saturate(dot(bsdfData.normalWS, L)) * attenuation;

    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);
    float4 cookie    = float4(1.0, 1.0, 1.0, 1.0);
	float  shadow = 1;

    // TODO: measure impact of having all these dynamic branch here and the gain (or not) of testing illuminace > 0

    //[branch] if (lightData.IESIndex >= 0 && illuminance > 0.0)
    //{
    //    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
    //    float2 sphericalCoord = GetIESTextureCoordinate(lightToWorld, L);
    //    illuminance *= SampleIES(lightLoopContext, lightData.IESIndex, sphericalCoord, 0).r;
    //}

	[branch] if (lightData.shadowIndex >= 0)
	{
		float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
		shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS + offset, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
		shadow = lerp(1.0, shadow, lightData.shadowDimmer);

		illuminance *= shadow;
	}

	[branch] if (lightData.cookieIndex >= 0)
	{
		float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);

		// Rotate 'L' into the light space.
		// We perform the negation because lights are oriented backwards (-Z).
		float3 coord = mul(-L, transpose(lightToWorld));

		[branch] if (lightData.lightType == GPULIGHTTYPE_SPOT)
		{
			// Perform the perspective projection of the hemisphere onto the disk.
			coord.xy /= coord.z;

			// Rescale the projective coordinates to fit into the [-1, 1]^2 range.
			float cotOuterHalfAngle = lightData.size.x;
			coord.xy *= cotOuterHalfAngle;

			// Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
			coord.xy = coord.xy * 0.5 + 0.5;

			cookie = SampleCookie2D(lightLoopContext, coord.xy, lightData.cookieIndex);
		}
		else // GPULIGHTTYPE_POINT
		{
			cookie = SampleCookieCube(lightLoopContext, coord, lightData.cookieIndex);
		}

		illuminance *= cookie.a;
	}

	[branch] if (illuminance > 0.0)
	{
		BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

		diffuseLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.diffuseScale);
		specularLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.specularScale);
	}
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Projector
//-----------------------------------------------------------------------------

void EvaluateBSDF_Projector(LightLoopContext lightLoopContext,
                            float3 V, PositionInputs posInput, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

    // Translate and rotate 'positionWS' into the light space.
    float3 positionLS = mul(positionWS - lightData.positionWS,
                            transpose(float3x3(lightData.right, lightData.up, lightData.forward)));

    if (lightData.lightType == GPULIGHTTYPE_PROJECTOR_PYRAMID)
    {
        // Perform perspective division.
        positionLS *= rcp(positionLS.z);
    }
    else
    {
        // For orthographic projection, the Z coordinate plays no role.
        positionLS.z = 0;
    }

    // Compute the NDC position (in [-1, 1]^2). TODO: precompute the inverse?
    float2 positionNDC = positionLS.xy * rcp(0.5 * lightData.size);

    // Perform clipping.
    float clipFactor = ((positionLS.z >= 0) && (abs(positionNDC.x) <= 1 && abs(positionNDC.y) <= 1)) ? 1 : 0;

    float3 L = -lightData.forward; // Lights are pointing backward in Unity
    float illuminance = saturate(dot(bsdfData.normalWS, L) * clipFactor);

    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);
    float4 cookie    = float4(1.0, 1.0, 1.0, 1.0);
	float shadow = 1;

	[branch] if (lightData.shadowIndex >= 0)
	{
		shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
		illuminance *= shadow;
	}

	[branch] if (lightData.cookieIndex >= 0)
	{
		// Compute the texture coordinates in [0, 1]^2.
		float2 coord = positionNDC * 0.5 + 0.5;

		cookie = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);

		illuminance *= cookie.a;
	}

	[branch] if (illuminance > 0.0)
	{
		BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

		diffuseLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.diffuseScale);
		specularLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.specularScale);
	}
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

void EvaluateBSDF_Line(LightLoopContext lightLoopContext,
                       float3 V, PositionInputs posInput,
                       PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                       out float3 diffuseLighting, out float3 specularLighting)
{
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

void EvaluateBSDF_Area(LightLoopContext lightLoopContext,
                       float3 V, PositionInputs posInput,
                       PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                       out float3 diffuseLighting, out float3 specularLighting)
{
    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting, out float2 weight)
{
    float3 positionWS = posInput.positionWS;

    // TODO: factor this code in common, so other material authoring don't require to rewrite everything,
    // also think about how such a loop can handle 2 cubemap at the same time as old unity. Macro can allow to do that
    // but we need to have UNITY_SAMPLE_ENV_LOD replace by a true function instead that is define by the lighting arcitecture.
    // Also not sure how to deal with 2 intersection....
    // Box and sphere are related to light property (but we have also distance based roughness etc...)

    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);

    // In this code we redefine a bit the behavior of the reflcetion proble. We separate the projection volume (the proxy of the scene) form the influence volume (what pixel on the screen is affected)

    // 1. First determine the projection volume

    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and oritention matter. So after intersection of proxy volume we need to convert back to world.

    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.

    float3 R = preLightData.iblDirWS;
    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    if (lightData.envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 dirLS = mul(R, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * R) - lightData.positionWS;

        // TODO: add distance based roughness
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float3 dirLS = mul(R, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);

        R = (positionWS + dist * R) - lightData.positionWS;
    }

    // 2. Apply the influence volume (Box volume is used for culling whatever the influence shape)
    // TODO: In the future we could have an influence volume inside the projection volume (so with a different transform, in this case we will need another transform)
    weight.y = 1.0;

    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_BOX ||
             lightData.envShapeType == ENVSHAPETYPE_NONE)
    {
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }

    // Smooth weighting
    weight.x = 0.0;
    weight.y = smoothstep01(weight.y);

    // TODO: we must always perform a weight calculation as due to tiled rendering we need to smooth out cubemap at boundaries.
    // So goal is to split into two category and have an option to say if we parallax correct or not.

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, preLightData.iblMipLevel);
    specularLighting = preLD.rgb * preLightData.specularFGD;

    // Apply specular occlusion on it
    specularLighting *= bsdfData.specularOcclusion;
    diffuseLighting = float3(0.0, 0.0, 0.0);
}

#endif // #ifdef HAS_LIGHTLOOP