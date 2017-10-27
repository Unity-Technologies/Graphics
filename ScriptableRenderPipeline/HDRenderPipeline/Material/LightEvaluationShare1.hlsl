

// Sampler use by area light, gaussian pyramid, ambient occlusion etc...
SamplerState s_linear_clamp_sampler;
SamplerState s_trilinear_clamp_sampler;

// Rough refraction texture
// Color pyramid (width, height, lodcount, Unused)
TEXTURE2D(_GaussianPyramidColorTexture);
// Depth pyramid (width, height, lodcount, Unused)
TEXTURE2D(_PyramidDepthTexture);

CBUFFER_START(UnityGaussianPyramidParameters)
float4 _GaussianPyramidColorMipSize;
float4 _PyramidDepthMipSize;
CBUFFER_END

// Ambient occlusion texture
TEXTURE2D(_AmbientOcclusionTexture);

CBUFFER_START(UnityAmbientOcclusionParameters)
float4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght
CBUFFER_END

// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
TEXTURE2D(_PreIntegratedFGD);
TEXTURE2D_ARRAY(_LtcData); // We pack the 3 Ltc data inside a texture array
#define LTC_GGX_MATRIX_INDEX 0 // RGBA
#define LTC_DISNEY_DIFFUSE_MATRIX_INDEX 1 // RGBA
#define LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX 2 // RGB, A unused
#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))

#define MIN_N_DOT_V    0.0001               // The minimum value of 'NdotV'

#ifdef WANT_SSS_CODE
// Subsurface scattering specific constant
#define SSS_WRAP_ANGLE (PI/12)              // Used for wrap lighting
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

CBUFFER_START(UnitySSSParameters)
uint   _EnableSSSAndTransmission;           // Globally toggles subsurface and transmission scattering on/off
uint   _TexturingModeFlags;                 // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
uint   _TransmissionFlags;                  // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
// Old SSS Model >>>
uint   _UseDisneySSS;
float4 _HalfRcpVariancesAndWeights[SSS_N_PROFILES][2]; // 2x Gaussians in RGB, A is interpolation weights
// <<< Old SSS Model
// Use float4 to avoid any packing issue between compute and pixel shaders
float4  _ThicknessRemaps[SSS_N_PROFILES];   // R: start, G = end - start, BA unused
float4 _ShapeParams[SSS_N_PROFILES];        // RGB = S = 1 / D, A = filter radius
float4 _TransmissionTints[SSS_N_PROFILES];  // RGB = 1/4 * color, A = unused
CBUFFER_END
#endif

void ApplyDebugToSurfaceData(inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;

        if (overrideSmoothness)
        {
            surfaceData.perceptualSmoothness = overrideSmoothnessValue;
        }
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        surfaceData.baseColor = _DebugLightingAlbedo.xyz;
    }
#endif
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

#ifdef WANT_SSS_CODE
void FillMaterialIdSSSData(float3 baseColor, int subsurfaceProfile, float subsurfaceRadius, float thickness, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = baseColor;

    bsdfData.fresnel0 = 0.028; // TODO take from subsurfaceProfile instead
    bsdfData.subsurfaceProfile = subsurfaceProfile;
    bsdfData.subsurfaceRadius  = subsurfaceRadius;
    bsdfData.thickness = _ThicknessRemaps[subsurfaceProfile].x + _ThicknessRemaps[subsurfaceProfile].y * thickness;

    uint transmissionMode = BitFieldExtract(_TransmissionFlags, 2u, 2u * subsurfaceProfile);

    bsdfData.enableTransmission = transmissionMode != SSS_TRSM_MODE_NONE && (_EnableSSSAndTransmission > 0);
    bsdfData.useThinObjectMode  = transmissionMode == SSS_TRSM_MODE_THIN;

    bool performPostScatterTexturing = IsBitSet(_TexturingModeFlags, subsurfaceProfile);

#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) // In case of GI pass don't modify the diffuseColor
    bool enableSssAndTransmission = false;
#elif defined(SHADERPASS) && (SHADERPASS == SHADERPASS_SUBSURFACE_SCATTERING)
    bool enableSssAndTransmission = true;
#else
    bool enableSssAndTransmission = _EnableSSSAndTransmission != 0;
#endif

    if (enableSssAndTransmission) // If we globally disable SSS effect, don't modify diffuseColor
    {
        // We modify the albedo here as this code is used by all lighting (including light maps and GI).
        if (performPostScatterTexturing)
        {
        #if !defined(SHADERPASS) || (SHADERPASS != SHADERPASS_SUBSURFACE_SCATTERING)
            bsdfData.diffuseColor = float3(1.0, 1.0, 1.0);
        #endif
        }
        else
        {
            bsdfData.diffuseColor = sqrt(bsdfData.diffuseColor);
        }
    }

    if (bsdfData.enableTransmission)
    {
        if (_UseDisneySSS)
        {
            bsdfData.transmittance = ComputeTransmittance(_ShapeParams[subsurfaceProfile].rgb,
                                                          _TransmissionTints[subsurfaceProfile].rgb,
                                                          bsdfData.thickness, bsdfData.subsurfaceRadius);
        }
        else
        {
            bsdfData.transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[subsurfaceProfile][0].rgb,
                                                                 _HalfRcpVariancesAndWeights[subsurfaceProfile][0].a,
                                                                 _HalfRcpVariancesAndWeights[subsurfaceProfile][1].rgb,
                                                                 _HalfRcpVariancesAndWeights[subsurfaceProfile][1].a,
                                                                 _TransmissionTints[subsurfaceProfile].rgb,
                                                                 bsdfData.thickness, bsdfData.subsurfaceRadius);
        }

        bsdfData.transmittance *= bsdfData.diffuseColor; // Premultiply
    }
}
#endif

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular and diffuse (in case of DisneyDiffuse)
void GetPreIntegratedFGD(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 specularFGD, out float diffuseFGD, out float reflectivity)
{
    // Pre-integrate GGX FGD
    // Integral{BSDF * <N,L> dw} =
    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
    // F0 * Integral{(BSDF / F) * <N,L> dw} +
    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} =
    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
    // Pre integrate DisneyDiffuse FGD:
    // z = DisneyDiffuse
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    specularFGD = lerp(preFGD.xxx, preFGD.yyy, fresnel0);

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseFGD = 1.0;
#else
    // Remap from the [0, 1] to the [0.5, 1.5] range.
    diffuseFGD = preFGD.z + 0.5;
#endif

    reflectivity = preFGD.y;
}

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    // General
    float NdotV;                         // Geometric version (could be negative)

    // GGX
    float partLambdaV;
    float energyCompensation;
    float TdotV;
    float BdotV;

    // IBL
    float3 iblDirWS;                     // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblMipLevel;

    float3 specularFGD;                  // Store preconvoled BRDF for both specular and diffuse
    float diffuseFGD;

    // Area lights (17 VGPRs)
    float3x3 orthoBasisViewNormal; // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;  // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular; // Inverse transformation for GGX                                 (4x VGPRs)
    float    ltcMagnitudeDiffuse;
    float3   ltcMagnitudeFresnel;
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, BSDFData bsdfData)
{
    PreLightData preLightData;

    preLightData.NdotV = dot(bsdfData.normalWS, V); // Store the unaltered (geometric) version
    float NdotV = preLightData.NdotV;

    // In the case of IBL we want  shift a bit the normal that are not toward the viewver to reduce artifact
    float3 iblNormalWS = GetViewShiftedNormal(bsdfData.normalWS, V, NdotV, MIN_N_DOT_V); // Use non clamped NdotV
    float3 iblR = reflect(-V, iblNormalWS);

    NdotV = max(NdotV, MIN_N_DOT_V); // Use the modified (clamped) version

    // GGX aniso
    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        preLightData.TdotV       = dot(bsdfData.tangentWS, V);
        preLightData.BdotV       = dot(bsdfData.bitangentWS, V);
        preLightData.partLambdaV = GetSmithJointGGXAnisoPartLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV, bsdfData.roughnessT, bsdfData.roughnessB);

        // For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
        // We use a single fetch, and we stretch the normal to use based on various criteria.
        // result are far away from the reference but better than nothing
        // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
        float3 grainDirWS = (bsdfData.anisotropy >= 0) ? bsdfData.bitangentWS : bsdfData.tangentWS;
        // Reduce stretching for (perceptualRoughness < 0.2).
        float  stretch = abs(bsdfData.anisotropy) * saturate(5 * bsdfData.perceptualRoughness);
        // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NdotV) and use iblNormalWS
        // into function like GetSpecularDominantDir(). However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here.
        // With this in mind and for performance reasons we chose to only use modified normal to calculate R.
        float3 anisoIblNormalWS = GetAnisotropicModifiedNormal(grainDirWS, iblNormalWS, V, stretch);
        iblR = reflect(-V, anisoIblNormalWS);
    }
    else // GGX iso
    {
        preLightData.TdotV       = 0;
        preLightData.BdotV       = 0;
        preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, bsdfData.roughness);
        iblR                     = reflect(-V, iblNormalWS);
    }

    float reflectivity;

    // IBL
    GetPreIntegratedFGD(NdotV, bsdfData.perceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, reflectivity);

    // Note: this is a ad-hoc tweak.
    float iblRoughness, iblPerceptualRoughness;

    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        // Use the min roughness, and bias it for higher values of anisotropy and roughness.
        float roughnessBias = 0.075 * bsdfData.anisotropy * bsdfData.roughness;
        iblRoughness = saturate(min(bsdfData.roughnessT, bsdfData.roughnessB) + roughnessBias);
        iblPerceptualRoughness = RoughnessToPerceptualRoughness(iblRoughness);
    }
    else
    {
        iblRoughness = bsdfData.roughness;
        iblPerceptualRoughness = bsdfData.perceptualRoughness;
    }

    preLightData.iblDirWS = GetSpecularDominantDir(iblNormalWS, iblR, iblRoughness, NdotV);
    preLightData.iblMipLevel = PerceptualRoughnessToMipmapLevel(iblPerceptualRoughness);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    // Ref: Practical multiple scattering compensation for microfacet models.
    // We only apply the formulation for metals.
    // For dielectrics, the change of reflectance is negligible.
    // We deem the intensity difference of a couple of percent for high values of roughness
    // to not be worth the cost of another precomputed table.
    // Note: this formulation bakes the BSDF non-symmetric!
    preLightData.energyCompensation = 1.0 / reflectivity - 1.0;
#else
    preLightData.energyCompensation = 0.0;
#endif // LIT_USE_GGX_ENERGY_COMPENSATION

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACos(NdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;
#else
    // Get the inverse LTC matrix for Disney Diffuse
    preLightData.ltcTransformDiffuse      = 0.0;
    preLightData.ltcTransformDiffuse._m22 = 1.0;
    preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_DISNEY_DIFFUSE_MATRIX_INDEX, 0);
#endif

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal[0] = normalize(V - bsdfData.normalWS * preLightData.NdotV);
    preLightData.orthoBasisViewNormal[2] = bsdfData.normalWS;
    preLightData.orthoBasisViewNormal[1] = normalize(cross(preLightData.orthoBasisViewNormal[2], preLightData.orthoBasisViewNormal[0]));

    float3 ltcMagnitude = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX, 0).rgb;
    float  ltcGGXFresnelMagnitudeDiff = ltcMagnitude.r; // The difference of magnitudes of GGX and Fresnel
    float  ltcGGXFresnelMagnitude     = ltcMagnitude.g;
    float  ltcDisneyDiffuseMagnitude  = ltcMagnitude.b;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcMagnitudeDiffuse = 1;
#else
    preLightData.ltcMagnitudeDiffuse = ltcDisneyDiffuseMagnitude;
#endif

    // TODO: the fit seems rather poor. The scaling factor of 0.5 allows us
    // to match the reference for rough metals, but further darkens dielectrics.
    preLightData.ltcMagnitudeFresnel = bsdfData.fresnel0 * ltcGGXFresnelMagnitudeDiff + (float3)ltcGGXFresnelMagnitude;

    return preLightData;
}
