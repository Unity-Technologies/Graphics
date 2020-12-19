//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Water.cs which generates Water.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Water/Water.cs.hlsl"
// Those define allow to include desired SSS/Transmission functions
//#define MATERIAL_INCLUDE_SUBSURFACESCATTERING

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"


//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float GetPhaseTerm(float3 lightDirOS, float3 V, BSDFData bsdfData)
{
    float3 biasedOceanLightDirection = lightDirOS;
    float invIOR = 1.f - bsdfData.anisotropyIOR;
    biasedOceanLightDirection.y -= (invIOR * 2.f);
    biasedOceanLightDirection = normalize(biasedOceanLightDirection);
    float3 singleScatteringRay = refract(-V, bsdfData.phaseNormalWS, bsdfData.anisotropyIOR);

    float cos0RL = dot(singleScatteringRay, biasedOceanLightDirection);
    float phaseTerm = CornetteShanksPhasePartVarying(bsdfData.anisotropy, cos0RL);

    return (phaseTerm * bsdfData.anisotropyWeight);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    float3 phaseNormalWS = float3(0, 1, 0);
    return phaseNormalWS;
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor, 0.0);
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return 1.0f;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.materialFeatures = surfaceData.materialFeatures;

    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.lowFrequencyNormalWS = surfaceData.lowFrequencyNormalWS;
    bsdfData.diffuseColor = surfaceData.baseColor;
    bsdfData.foamColor = surfaceData.foamColor;
    bsdfData.diffuseWrapAmount = surfaceData.diffuseWrapAmount;
    bsdfData.phaseNormalWS = surfaceData.phaseNormalWS;

    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness); 

    // TODO: Could hardcode
    const float fresnel0 = 0.02037318784f;
    bsdfData.fresnel0 = fresnel0.xxx;// IorToFresnel0(1.333f).xxx;
    bsdfData.roughness = ClampRoughnessForAnalyticalLights(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness));

    bsdfData.specularSelfOcclusion = surfaceData.specularSelfOcclusion;
    bsdfData.anisotropy = surfaceData.anisotropy;
    bsdfData.anisotropyWeight = surfaceData.anisotropyWeight;
    bsdfData.anisotropyIOR = surfaceData.anisotropyIOR;
    bsdfData.scatteringLambertLighting = surfaceData.scatteringLambertLighting;
    bsdfData.customRefractionColor = surfaceData.customRefractionColor;

    return bsdfData;
}

//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()
    float partLambdaV;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewDiffuseNormal;
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)

#if HAS_REFRACTION
    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
#endif
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughness = max(minRoughness, bsdfData.roughness);
}

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;
    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    float NdotVLowFrequency = dot(bsdfData.lowFrequencyNormalWS, V);
    NdotVLowFrequency = ClampNdotV(NdotVLowFrequency);
    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
    preLightData.diffuseFGD = 1.0;

    float3 iblN;
    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, bsdfData.roughness);
    iblN = N;

    preLightData.iblR = reflect(-V, iblN);

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACosPos(clampedNdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = Remap01ToHalfTexelCoord(float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI), LTC_LUT_SIZE);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformDiffuse = k_identity3x3;

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal

    // If we use the cinematic mode we wrap the normal away from the light and this cannot
    // be precomputed here (it's done directly in EvaluateBSDF_Rect)
    if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_WATER_CINEMATIC))
    {
        preLightData.orthoBasisViewDiffuseNormal = GetOrthoBasisViewNormal(V, bsdfData.lowFrequencyNormalWS, NdotVLowFrequency);
    }
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);
    return preLightData;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // DiffuseColor for lightmapping
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; // In order to get cqustic effect and concavity
}

// This function apply BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 N = bsdfData.normalWS;
    float3 lowFrequencyNormal = bsdfData.lowFrequencyNormalWS;
    float NdotLLowFrequency = dot(lowFrequencyNormal, L);
    float NdotLWrappedDiffuseLowFrequency = ComputeWrappedDiffuseLighting(NdotLLowFrequency, bsdfData.diffuseWrapAmount);
    float clampedNdotLLowFrequency = saturate(NdotLLowFrequency);
    float NdotV = preLightData.NdotV;
    float clampedNdotV = ClampNdotV(NdotV);

    float NdotL = dot(N, L);
    float clampedNdotL = saturate(NdotL);

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // We use abs(NdotL) to handle the none case of double sided
    float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.roughness, preLightData.partLambdaV);

    float specularSelfOcclusion = saturate(clampedNdotLLowFrequency * 5.f + bsdfData.specularSelfOcclusion);
    cbsdf.specR = F * DV * clampedNdotL * specularSelfOcclusion;

    cbsdf.diffR = lerp(1.f, NdotLWrappedDiffuseLowFrequency, bsdfData.scatteringLambertLighting) * (1.0 - F);
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

// for Rotate function only
# include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"

// This is a test coming from Heretic demo. It is way more expensive
// and is sometimes better, sometime not than the "else" code and don't support caustic.
void LightWaterTransform(PositionInputs posInput, BSDFData bsdfData, inout float3 positionRWS, inout float3 forward, inout float3 right, inout float3 up)
{
    float3 L = normalize(positionRWS - posInput.positionWS);
    float3 geomNormalWS = float3(0, 1, 0);
    float3 refractL = -refract(-L, geomNormalWS, 1.0 / 1.333f);

    float3 axis = normalize(cross(L, refractL));
    float bsdfData_mask_x = 1.0f;
    float angle = lerp(0.0, acos(dot(L, refractL)), bsdfData_mask_x);

    positionRWS = Rotate(posInput.positionWS, positionRWS, axis, angle);
    forward = Rotate(float3(0, 0, 0), forward, axis, angle);
    right = Rotate(float3(0, 0, 0), right, axis, angle);
    up = Rotate(float3(0, 0, 0), up, axis, angle);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    DirectLighting dl = ShadeSurface_Directional( lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, V);
    float3 diffuseWater = dl.diffuse * bsdfData.diffuseColor;
    float3 diffuseFoam = dl.diffuse * bsdfData.foamColor.rgb;

    float3 lightDirOS = TransformWorldToObjectDir(-lightData.forward);

    float phaseTerm = GetPhaseTerm(lightDirOS, V, bsdfData);
    diffuseWater += (diffuseWater * phaseTerm);

    dl.diffuse = diffuseWater + diffuseFoam;

    return dl; 
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{

    DirectLighting dl = ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                                preLightData, lightData, bsdfData, V);

    float3 L;
    float4 distances; // {d, d^2, 1/d, d_proj}
    GetPunctualLightVectors(posInput.positionWS, lightData, L, distances);

    float3 diffuseWater = dl.diffuse * bsdfData.diffuseColor;
    float3 diffuseFoam = dl.diffuse * bsdfData.foamColor.rgb;

    float phaseTerm = GetPhaseTerm(L, V, bsdfData);
    diffuseWater += (diffuseWater * phaseTerm);

    dl.diffuse = diffuseWater + diffuseFoam;

    return dl;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect
//-----------------------------------------------------------------------------

// TODO: Put in CommonLighting.hlsl before in HDRP master. Until then let's keep it here
// for merge convenience.
// Ref: Stephen McAuley - Advances in Rendering: Graphics Research and Video Game Production
real3 ComputeWrappedNormal(real3 N, real3 L, real w)
{
    real NdotL = dot(N, L);
    real wrappedNdotL = saturate((NdotL + w) / (1 + w));
    real sinPhi = lerp(w, 0.f, wrappedNdotL);
    real cosPhi = sqrt(1.0f - sinPhi * sinPhi);
    return normalize(cosPhi * N + sinPhi * cross(cross(N, L), N));
}

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

    float3 unL = lightData.positionRWS - positionWS;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_WATER_CINEMATIC))
    {
        bsdfData.lowFrequencyNormalWS = ComputeWrappedNormal(bsdfData.lowFrequencyNormalWS, normalize(unL), bsdfData.diffuseWrapAmount);
        float NdotVLowFrequency = dot(bsdfData.lowFrequencyNormalWS, V);
        NdotVLowFrequency = ClampNdotV(NdotVLowFrequency);
        preLightData.orthoBasisViewDiffuseNormal = GetOrthoBasisViewNormal(V, bsdfData.lowFrequencyNormalWS, NdotVLowFrequency);
    }

    if (dot(lightData.forward, unL) < 0.0001)
    {

         // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

         // TODO: This could be precomputed.
        float halfWidth = lightData.size.x * 0.5;
        float halfHeight = lightData.size.y * 0.5;

         // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float range = lightData.range;
        float3 invHalfDim = rcp(float3(range + halfWidth,
                                    range + halfHeight,
                                    range));

         // Compute the light attenuation.
# ifdef ELLIPSOIDAL_ATTENUATION
        // The attenuation volume is an axis-aligned ellipsoid s.t.
        // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
        float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                        lightData.rangeAttenuationScale,
                                                        lightData.rangeAttenuationBias);
#else
        // The attenuation volume is an axis-aligned box s.t.
        // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
        float intensity = BoxDistanceAttenuation(unL, invHalfDim,
                                                    lightData.rangeAttenuationScale,
                                                    lightData.rangeAttenuationBias);
#endif

        // Terminate if the shaded point is too far away.
        if (intensity != 0.0)
        {
            lightData.diffuseDimmer *= intensity;
            lightData.specularDimmer *= intensity;

             // Translate the light s.t. the shaded point is at the origin of the coordinate system.
            lightData.positionRWS -= positionWS;

             float4x3 lightVerts;

             // TODO: some of this could be precomputed.
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

             // Note: We don't have the same normal for diffuse and specular
            // Rotate the endpoints into the local coordinate system.
            float4x3 lightVertsDiff  = mul(lightVerts, transpose(preLightData.orthoBasisViewDiffuseNormal));

             float3 ltcValue;

             // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(lightVertsDiff, preLightData.ltcTransformDiffuse);
            ltcValue = PolygonIrradiance(LD);
            ltcValue *= lightData.diffuseDimmer;

            // TODO: re-enable this when HDRP version supports it
            // Only apply cookie if there is one
            //if (lightData.cookieMode != COOKIEMODE_NONE)
            //{
            //    // Compute the cookie data for the diffuse term
            //    float3 formFactorD = PolygonFormFactor(LD);
            //    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            //}

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            // See comment for specular magnitude, it apply to diffuse as well
            lighting.diffuse = preLightData.diffuseFGD * ltcValue;

            // Evaluate the specular part
            float4x3 lightVertsSpec = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

            // Polygon irradiance in the transformed configuration.
            float4x3 LS = mul(lightVertsSpec, preLightData.ltcTransformSpecular);
            ltcValue = PolygonIrradiance(LS);
            ltcValue *= lightData.specularDimmer;

            // TODO: re-enable this when HDRP version supports it
            // Only apply cookie if there is one
            //if (lightData.cookieMode != COOKIEMODE_NONE)
            //{
            //    // Compute the cookie data for the specular term
            //    float3 formFactorS = PolygonFormFactor(LS);
            //    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LS, formFactorS);
            //}

            // We need to multiply by the magnitude of the integral of the BRDF
            // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
            // This value is what we store in specularFGD, so reuse it
            lighting.specular += preLightData.specularFGD * ltcValue;

             // Save ALU by applying 'lightData.color' only once.
            lighting.diffuse *= lightData.color;
            lighting.specular *= lightData.color;

 #ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
                lighting.diffuse *= PI * lightData.diffuseDimmer;
            }
#endif
        }

    }

    float3 diffuseWater = lighting.diffuse * bsdfData.diffuseColor;
    float3 diffuseFoam = lighting.diffuse * bsdfData.foamColor.rgb;

    lighting.diffuse = diffuseWater + diffuseFoam;

     return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_WATER_CINEMATIC))
    {
        LightWaterTransform(posInput, bsdfData, lightData.positionRWS, lightData.forward, lightData.right, lightData.up);
    }

    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
    float4 ssrLighting = LOAD_TEXTURE2D_X(_SsrLightingTexture, posInput.positionSS);
    InversePreExposeSsrLighting(ssrLighting);

    // Apply the weight on the ssr contribution (if required)
    // ApplyScreenSpaceReflectionWeight(ssrLighting);

    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.
    lighting.specularReflected = ssrLighting.rgb * preLightData.specularFGD;
    reflectionHierarchyWeight = ssrLighting.a;

    return lighting;
}

IndirectLighting EvaluateBSDF_ScreenspaceRefraction(LightLoopContext lightLoopContext,
                                                    float3 V, PositionInputs posInput,
                                                    PreLightData preLightData, BSDFData bsdfData,
                                                    EnvLightData envLightData,
                                                    inout float hierarchyWeight)
{
    // TODO: To refraction sampling here
    // How do we enable the LIGHTFEATUREFLAGS_SSREFRACTION featureFlag?

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    lighting.specularTransmitted = float3(1000000, 0, 0);
    lighting.specularReflected = float3(1000000, 0, 0);
    hierarchyWeight = 1;

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF

IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

    float3 R = preLightData.iblR;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float3 F = preLightData.specularFGD;

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness), lightData.rangeCompressionFactorCompensation, posInput.positionNDC);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F * preLD.rgb;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    lighting.specularReflected = envLighting; 

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{ 
    AmbientOcclusionFactor aoFactor;
    float ao = 1.f;
    float speculatOcclusion = 1.f;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, ao, speculatOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // TODO: Temp hack. How to we enable the LIGHTFEATUREFLAGS_SSREFRACTION featureFlag?
    float3 refraction = bsdfData.customRefractionColor * (1.f - preLightData.specularFGD);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    // TODO: I wonder if there's a better way to slit the lighting between the foam surface and the volume. Must ask the HDRP folks
    lightLoopOutput.diffuseLighting = /*bsdfData.diffuseColor * */ lighting.direct.diffuse + builtinData.bakeDiffuseLighting + refraction;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
