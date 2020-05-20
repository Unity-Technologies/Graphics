//-------------------------------------------------------------------------------------
// Defines
//-------------------------------------------------------------------------------------
//to test, dont enable: #define FLAKES_TILE_BEFORE_SCALE
#define AXF_REUSE_SCREEN_DDXDDY
// ...ie use _GRAD sampling for everything and calculate those only one time:
// offset doesn't change derivatives, and scales just scales them, so we can cache them.

// The compiler can't unroll the lightloop if flakes are sampled inside it, so we need to cache either LOD
// or derivatives. We prefer the later, as the CalculateLevelOfDetail will not work when anisotropic filtering
// is used, and AxF materials textures often have trilinear filtering set.
#define FLAKES_USE_DDXDDY

#if defined(_TEX_ANTI_MOIRE_NOTCH_NORMALMAPS) || defined(_TEX_ANTI_MOIRE_NOTCH_ALLMAPS)
#define NOTCH_CURVES_ENABLED //anti moire algo
// Options:
//#define NOTCH_CURVE_SMOOTH
// If either NOTCH_USES_CUSTOM_LOD_MIPCNT or NOTCH_USES_CUSTOM_LOD_TEXSIZES is defined,
// this prevents use of CalculateLevelOfDetail() for the notch curve domain input, and
// makes us calculate the LOD ourselves. The main reason to do this is to have consistent
// results whether anisotropic filtering is on or off (when anisotropic filtering is on,
// CalculateLevelOfDetail() returns plateaux of fixed values and is thus less useful)
//#define NOTCH_USES_CUSTOM_LOD_MIPCNT
#define NOTCH_USES_CUSTOM_LOD_TEXSIZES // better than using MIPCNT
#endif


#define AXF_USES_RG_NORMAL_MAPS // else, RGB

//-------------------------------------------------------------------------------------
// Defines (auto) from above
//-------------------------------------------------------------------------------------

// Gradients are now required:
#define SURFACE_GRADIENT // Note: this affects Material/MaterialUtilities.hlsl's GetNormalWS() and makes it expect a surface gradient.

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"

//-----------------------------------------------------------------------------
// Texture Mapping
//-----------------------------------------------------------------------------

//#if defined(NOTCH_USES_CUSTOM_LOD_TEXSIZES)
#define AXF_TEXSIZE_FROM_NAME(name) (name##_TexelSize.zw)
//#else
//#define AXF_TEXSIZE_FROM_NAME(name) (float2(1,1))
//#endif

#ifdef AXF_USES_RG_NORMAL_MAPS
#define AXF_DERIVATIVE_NORMAL UnpackDerivativeNormalRGorAG
#define AXF_UNPACK_NORMAL_VARIANCE(packedNormal) (packedNormal.z)
#else
#define AXF_DERIVATIVE_NORMAL UnpackDerivativeNormalRGB
#define AXF_UNPACK_NORMAL_VARIANCE(packedNormal) (packedNormal.w)
#endif

// Note: the scaling _Material_SO.xy should already be in texuv, but NOT the bias.
#define AXF_TRANSFORM_TEXUV_BYNAME(texuv, name) ((texuv.xy) * name##_SO.xy + name##_SO.zw + _Material_SO.zw)
#define AXF_GET_SINGE_SCALE_OFFSET(name) (name##_SO)
#define AXF_TRANSFORM_TEXUV(texuv, scaleOffset) ((texuv.xy) * scaleOffset.xy + scaleOffset.zw + _Material_SO.zw)

#define AXF_GET_TEX_FADE_NOTCH_SRC(name) (name##_LodIntoFade)
#define AXF_GET_TEX_BIAS_NOTCH_SRC(name) (name##_LodIntoBias)

// Used for _LodIntoBias and _LodIntoFade properties:
#define NOTCH_CURVE_NONE 0.0
#define NOTCH_CURVE_A 1.0
#define NOTCH_CURVE_B 2.0

// Note: the scaling _Material_SO.xy should already be in ddx and ddy:
#define AXF_SCALE_DDXDDY_BYNAME(vddx, name) ((vddx) * (name##_SO.xy))

#if 0
#define DDX(param) ddx_fine(param)
#define DDY(param) ddy_fine(param)
#else
#define DDX(param) ddx(param)
#define DDY(param) ddy(param)
#endif

struct TextureUVMapping
{
#ifdef _MAPPING_TRIPLANAR
    float2 uvZY;
    float2 uvXZ;
    float2 uvXY;
    float3 triplanarWeights;
    float2 ddxZY;
    float2 ddyZY;
    float2 ddxXZ;
    float2 ddyXZ;
    float2 ddxXY;
    float2 ddyXY;
#else
    float2 uvBase; // uv0..uv3 or a planar set (ZY, XZ or XY)
    float2 ddxBase;
    float2 ddyBase;
#endif

    float3 vertexNormalWS;
    float3 vertexTangentWS;
    float3 vertexBitangentWS;
};

void InitTextureUVMapping(FragInputs input, out TextureUVMapping uvMapping)
{
    float2 uvZY;
    float2 uvXZ;
    float2 uvXY;
    float2 uv3 = 0;

    // Set uv* variables above: they will contain a set of uv0...3 or a planar set:
#if (defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR))
    // planar/triplanar
    uv3 = 0;

#ifdef _PLANAR_LOCAL
    // If we use local planar mapping, convert to local space
    GetTriplanarCoordinate(TransformWorldToObject(input.positionRWS), uvXZ, uvXY, uvZY);
#else
    GetTriplanarCoordinate(GetAbsolutePositionWS(input.positionRWS), uvXZ, uvXY, uvZY);
#endif

    // Note: if only planar mapping is selected, we don't apply AxF main material tiling scale here,
    // we select one set with _MappingMask into the uvBase and scale that.

#ifdef _MAPPING_TRIPLANAR
    // In that case, we will need to store the 3 sets of planar coordinates:
    // (Apply AxF's main material tiling scale also)
    uvMapping.uvZY = uvZY * _Material_SO.xy;
    uvMapping.uvXZ = uvXZ * _Material_SO.xy;
    uvMapping.uvXY = uvXY * _Material_SO.xy;

    uvMapping.ddxZY = DDX(uvMapping.uvZY);
    uvMapping.ddyZY = DDY(uvMapping.uvZY);
    uvMapping.ddxXZ = DDX(uvMapping.uvXZ);
    uvMapping.ddyXZ = DDY(uvMapping.uvXZ);
    uvMapping.ddxXY = DDX(uvMapping.uvXY);
    uvMapping.ddyXY = DDY(uvMapping.uvXY);

#endif

#else // #if (defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR))

    // No planar and no triplanar: uvZY will alias uv0, uvXZ uv1 and uvXY uv2 and _MappingMask will select one:
    uv3 = input.texCoord3.xy;
    uvZY = input.texCoord0.xy;
    uvXZ = input.texCoord1.xy;
    uvXY = input.texCoord2.xy;
#endif // #if (defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR))

    // Set uvBase if not triplanar from the uv* variables above
#ifndef _MAPPING_TRIPLANAR
    // No triplanar: uvBase will store the selected single uv or planar coordinate set using _MappingMask:
    uvMapping.uvBase = _MappingMask.x * uvZY + // texCoord0 if no planar
                       _MappingMask.y * uvXZ + // texCoord1 if no planar
                       _MappingMask.z * uvXY + // texCoord2 if no planar
                       _MappingMask.w * uv3;   // _MappingMask.w should be 0 anyway if planar, but we force uv3 to 0

    // Apply AxF's main material tiling scale:
    uvMapping.uvBase *= _Material_SO.xy;

    uvMapping.ddxBase = DDX(uvMapping.uvBase);
    uvMapping.ddyBase = DDY(uvMapping.uvBase);

#endif

    // Calculate triplanar weights, interpreting "local planar space" for coordinates
    // as applying to the normal (used for weighting the samples fetched from those planar coords) also.
#ifdef _MAPPING_TRIPLANAR
    float3 vertexNormal = input.tangentToWorld[2].xyz;
#ifdef _PLANAR_LOCAL
    // If we use local planar mapping, convert to local space
    vertexNormal = TransformWorldToObjectDir(vertexNormal);
#endif
    uvMapping.triplanarWeights = ComputeTriplanarWeights(vertexNormal);
#endif

    // Use surface gradients to build an extra TBN is using anything other than UV0
    // Otherwise, use the vertex stage provided TBN as default:

    float3 vertexNormalWS = input.tangentToWorld[2];
    uvMapping.vertexNormalWS = vertexNormalWS;
    uvMapping.vertexTangentWS = input.tangentToWorld[0];
    uvMapping.vertexBitangentWS = input.tangentToWorld[1];

#if (defined(_REQUIRE_UV1)||defined(_REQUIRE_UV2)||defined(_REQUIRE_UV3))
    float3 dPdx = ddx_fine(input.positionRWS);
    float3 dPdy = ddy_fine(input.positionRWS);

    float3 sigmaX = dPdx - dot(dPdx, vertexNormalWS) * vertexNormalWS;
    float3 sigmaY = dPdy - dot(dPdy, vertexNormalWS) * vertexNormalWS;
    //float flipSign = dot(sigmaY, cross(vertexNormalWS, sigmaX) ) ? -1.0 : 1.0;
    float flipSign = dot(dPdy, cross(vertexNormalWS, dPdx)) < 0.0 ? -1.0 : 1.0; // gives same as the commented out line above

#if defined(_REQUIRE_UV1)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord1.xy, uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS);
#elif defined(_REQUIRE_UV2)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord2.xy, uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS);
#elif defined(_REQUIRE_UV3)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord3.xy, uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS);
#endif
#endif //#if (defined(_REQUIRE_UV1)||defined(_REQUIRE_UV2)||defined(_REQUIRE_UV3))
}


//                                                                             __ <--- notchLevel
// Notch fade in a region: shape of the curve is a reversed notch:       .__./|  |\.____.
//                                                                 LOD = 0  A B  C D    mipCnt
struct NotchCurve
{
    float notchA;
    float notchB;
    float notchC;
    float notchD;
    float notchLevel;
};

NotchCurve NotchGetCurve(float mipCnt, bool useCurveA = true)
{
    NotchCurve notchCurve;
    
    float notchCenter = mipCnt * (useCurveA ? _MipNotchCurveACenter : _MipNotchCurveBCenter);// _MipNotchCurve?Center is between 0 and 1
    notchCurve.notchLevel = (useCurveA ? _MipNotchCurveAParams.w : _MipNotchCurveBParams.w); // full effect region level 0 = no effect, 1 = full removal of normal map
    float notchDelayW = (useCurveA ? _MipNotchCurveAParams.x : _MipNotchCurveBParams.x);     // width to full effect
    float notchSustainW = (useCurveA ? _MipNotchCurveAParams.y : _MipNotchCurveBParams.y);   // full effect region width: C - B
    float notchReleaseW = (useCurveA ? _MipNotchCurveAParams.z : _MipNotchCurveBParams.z);   // width to no effect
    // x,y,z should all be >= 0

    //notchCurve.notchC = min( mipCnt, notchCenter + 0.5 * notchSustainW );
    //notchCurve.notchD = min( mipCnt, notchCurve.notchC + notchReleaseW );
    //notchCurve.notchB = max( 0, notchCenter - 0.5 * notchSustainW );
    //notchCurve.notchA = max( 0, notchCurve.notchB - notchDelayW );
    notchCurve.notchC = notchCenter + 0.5 * notchSustainW;
    notchCurve.notchD = notchCurve.notchC + notchReleaseW;
    notchCurve.notchB = notchCenter - 0.5 * notchSustainW;
    notchCurve.notchA = notchCurve.notchB - notchDelayW;

    return notchCurve;
}

// For triplanar, we support 3 lods and output 3 values
float3 NotchGetCurveValues(float3 lods, NotchCurve notchCurve, bool smooth = false)
{
#ifdef NOTCH_CURVE_SMOOTH
    smooth = true;
#endif
    float3 values = 0.0;
    float notchLevel = notchCurve.notchLevel;
    float notchC = notchCurve.notchC;
    float notchD = notchCurve.notchD;
    float notchB = notchCurve.notchB;
    float notchA = notchCurve.notchA;

    //lods += 0.0001; // So when center is 0, can start the curve immediately

    if (smooth)
    {
        values = notchLevel*( smoothstep(notchA,notchB,lods)*(1 - smoothstep(notchC,notchD,lods)) );
    }
    else
    {
        float3 lerpFactors = saturate((lods - notchA) * rcp(max(notchB - notchA, 0.0001))) - saturate((lods - notchC) * rcp(max(notchD - notchC, 0.0001)));
        values = notchLevel*lerpFactors;
    }

    return values;
}

// Notch curves can be used for two anti-moire algo: one only used by normal map sampling, which allows
// normal map fading, and the other for any maps, which allows more fined / adaptive control of a dynamic
// bias value. 

float3 NormalFadeNotchGetSurfaceGradientFadeScales(float3 notchCurveValues)
{
    float3 scales = 1 - saturate(notchCurveValues);
    return scales;
}

float3 LodIntoBiasNotchGetScreenDerivScales(float3 notchCurveValues)
{
    // To produce an effective lod sampling bias while still using derivatives,
    // we need to scale those by the exp2 of the wanted bias:
    float3 scales = exp2(notchCurveValues);
    return scales;
}

float3 NotchGetLods(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, float2 texSize, TextureUVMapping uvMapping,
                    float mipCnt,
                    int lodBiasOrGrad = 0, float3 inputLodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    // By default, try to use CalculateLevelOfDetail (this requires aniso level = 0 for the sampler! otherwise result of that call isnt good)
    bool useCustomLodCalculation = false;
#if defined(NOTCH_USES_CUSTOM_LOD_MIPCNT) || defined(NOTCH_USES_CUSTOM_LOD_TEXSIZES)
    useCustomLodCalculation = true;
#ifndef NOTCH_USES_CUSTOM_LOD_TEXSIZES
    texSize = (float2)1.0;
#else
    mipCnt = 0.0;
#endif // NOTCH_USES_CUSTOM_LOD_TEXSIZES
#endif // useCustomLodCalculation = true;

    bool useLod = lodBiasOrGrad == 1;
    bool useBias = lodBiasOrGrad == 2;
    bool useGrad = lodBiasOrGrad == 3;
    bool useCachedDdxDdy = false;    
    // If we have to use custom macro-submitted screen space derivatives (useGrad), we don't need UV,
    // and are forced to use a custom LOD calculation (otherwise, we can use CalculateLevelOfDetail on UVs)
    // Note that otherwise for custom LOD calculation, we use the cached derivatives which we always calculate.
    // We assume if given, the custom derivatives are in UV space.
    // The AXF_REUSE_SCREEN_DDXDDY is to always sample using _GRAD instead of normal sampling so derivatives are
    // calculated one time in the uvmapping structure.

    useCustomLodCalculation = useCustomLodCalculation || useGrad;

    float2 dpdx;
    float2 dpdy;
    float2 uv;
    float3 lods = 0;

    // Do main uvset, then 2 others if needed, if not triplanar, others won't be used later anyway.
#ifdef _MAPPING_TRIPLANAR
    uv = uvMapping.uvZY;
    dpdx = useGrad ? triDdx[0] : uvMapping.ddxZY * scaleOffset.xy;
    dpdy = useGrad ? triDdy[0] : uvMapping.ddyZY * scaleOffset.xy;
#else
    uv = uvMapping.uvBase;
    dpdx = useGrad ? triDdx[0] : uvMapping.ddxBase * scaleOffset.xy;
    dpdy = useGrad ? triDdy[0] : uvMapping.ddyBase * scaleOffset.xy;
#endif
    uv = AXF_TRANSFORM_TEXUV(uv, scaleOffset);
    // Note for lod calculation, take max deriv, eg dpdx: 
    //
    // sqrt(max_directional_deriv^2) =
    //
    // sqrt((dudx*texture_w)^2 + (dvdx*texture_h)^2), take max res of texture, we have
    // <
    //   sqrt((dudx*texture_res)^2 + (dvdx*texture_res)^2) 
    // = sqrt(texture_res^2 * [(dudx)^2 + (dvdx)^2]) 
    // = texture_res * ([(dudx)^2 + (dvdx)^2])^0.5 
    // = texture_res * dot(duvdx, duvdx)^0.5
    //
    // lod = log2(max_directional_deriv) 
    //     = log2(texture_res * dot(dpdx,dpdx)^0.5)
    //     = log2(texture_res) + log2(dot(dpdx,dpdx)^0.5)
    //     = mipCntFrac + 0.5 * log2(dot(dpdx,dpdx))
    //
    // Note that when the texture is perfectly aligned and adjusted with a 1 pixel / screen pixel derivative,
    // it means texture_res * uv = 1, so eg duvdx ~= 1/texture_res = -mipCntFrac.
    //
    // Note that mipCntFrac is fractionary here, while the number returned by GetMipCount is the total from
    // the texture. For LOD calculation, it is better to use texture_res since this is what ultimately is used
    // when sampling.

    // Note: texSize will be (1,1) if we don't use texture sizes and use mipCnt instead,
    // and mipCnt will be 0.0 if we use texture sizes!
    float dSq = max(dot(dpdx * texSize, dpdx * texSize), dot(dpdy * texSize, dpdy * texSize));
    lods.x = useCustomLodCalculation ? (0.5 * log2(dSq) + mipCnt) : CALCULATE_TEXTURE2D_LOD(textureName, samplerName, uv);

#ifdef _MAPPING_TRIPLANAR
    uv = uvMapping.uvXZ;
    dpdx = useGrad ? triDdx[1] : uvMapping.ddxXZ * scaleOffset.xy;
    dpdy = useGrad ? triDdy[1] : uvMapping.ddyXZ * scaleOffset.xy;
    uv = AXF_TRANSFORM_TEXUV(uv, scaleOffset);
    // Note: texSize will be (1,1) if we don't use texture sizes and use mipCnt instead,
    // and mipCnt will be 0.0 if we use texture sizes!
    dSq = max(dot(dpdx * texSize, dpdx * texSize), dot(dpdy * texSize, dpdy * texSize));
    lods.y = useCustomLodCalculation ? (0.5 * log2(dSq) + mipCnt) : CALCULATE_TEXTURE2D_LOD(textureName, samplerName, uv);

    uv = uvMapping.uvXY;
    dpdx = useGrad ? triDdx[2] : uvMapping.ddxXY * scaleOffset.xy;
    dpdy = useGrad ? triDdy[2] : uvMapping.ddyXY * scaleOffset.xy;
    uv = AXF_TRANSFORM_TEXUV(uv, scaleOffset);
    // Note: texSize will be (1,1) if we don't use texture sizes and use mipCnt instead,
    // and mipCnt will be 0.0 if we use texture sizes!
    dSq = max(dot(dpdx * texSize, dpdx * texSize), dot(dpdy * texSize, dpdy * texSize));
    lods.z = useCustomLodCalculation ? (0.5 * log2(dSq) + mipCnt) : CALCULATE_TEXTURE2D_LOD(textureName, samplerName, uv);
#endif

    // Add custom code given bias if needed (checking lodBiasOrGrad)
    lods += useBias ? inputLodOrBias : (float3)0;
    // Finally completely bypass this function's lod calculations if custom lods are given by the caller
    // (indicated again by the lodBiasOrGrad code):
    lods = useLod ? inputLodOrBias : lods;

    return lods;
}


void NotchGetScales(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, float2 texSize, TextureUVMapping uvMapping,
                    bool calledForNomalMap,
                    float lodIntoFadeProp, out float3 normalFadeNotchScales,
                    float lodIntoBiasProp, out float3 lodToBiasNotchScales,
                    out bool useLodToBiasNotch,
                    int lodBiasOrGrad = 0, float3 inputLodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    normalFadeNotchScales = 1;
    lodToBiasNotchScales = 1;
    useLodToBiasNotch = false;

    float3 curveAValues = 1;
    float3 curveBValues = 1;

    bool skipEval = false;
#ifndef _TEX_ANTI_MOIRE_NOTCH_ALLMAPS
    // We only allow the algo for the normal map
    skipEval = (calledForNomalMap == false);
#endif

#ifdef NOTCH_CURVES_ENABLED
    if (skipEval == false)
    {
        // We will calculate curve A and B values if required, then assign these to normalFade or lodToBias values
        // depending on the specific texture property config (_LodIntoFade and _LodIntoBias)
        // (compiler should not need calledForNomalMap help, but just to be clearer)
        bool calculateCurveA = (calledForNomalMap && lodIntoFadeProp == NOTCH_CURVE_A) || (lodIntoBiasProp == NOTCH_CURVE_A);
        bool calculateCurveB = (calledForNomalMap && lodIntoFadeProp == NOTCH_CURVE_B) || (lodIntoBiasProp == NOTCH_CURVE_B);

        if (calculateCurveA)
        {
            float mipCnt = GetMipCount(textureName);
            NotchCurve notchCurve = NotchGetCurve(mipCnt, /*useCurveA*/ true);
            float3 lods = NotchGetLods(textureName, samplerName, scaleOffset, texSize, uvMapping, mipCnt, lodBiasOrGrad, inputLodOrBias, triDdx, triDdy);
            curveAValues = NotchGetCurveValues(lods, notchCurve);
        }
        if (calculateCurveB)
        {
            float mipCnt = GetMipCount(textureName);
            NotchCurve notchCurve = NotchGetCurve(mipCnt, /*useCurveA*/ false);
            float3 lods = NotchGetLods(textureName, samplerName, scaleOffset, texSize, uvMapping, mipCnt, lodBiasOrGrad, inputLodOrBias, triDdx, triDdy);
            curveBValues = NotchGetCurveValues(lods, notchCurve);
        }

        if (calledForNomalMap && lodIntoFadeProp > 0)
        {
            float3 values = (lodIntoFadeProp == NOTCH_CURVE_A) ? curveAValues : curveBValues;
            normalFadeNotchScales = NormalFadeNotchGetSurfaceGradientFadeScales(values);
        }
        if (lodIntoBiasProp > 0)
        {
            useLodToBiasNotch = true;
            float3 values = (lodIntoBiasProp == NOTCH_CURVE_A) ? curveAValues : curveBValues;
            lodToBiasNotchScales = LodIntoBiasNotchGetScreenDerivScales(values);
        }
    }
#endif // #ifdef NOTCH_CURVES_ENABLED
}

// Make sure lodBiasOrGrad is used statically!
//
#define AXF_SAMPLE_USE_LOD 1
#define AXF_SAMPLE_USE_BIAS 2
#define AXF_SAMPLE_USE_GRAD 3

// Note that scaleOffset are the texture specific ones, not the main material ones!
float4 AxfSampleTexture2D(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, float2 texSize /*only valid if NOTCH_USES_CUSTOM_LOD_TEXSIZES*/, TextureUVMapping uvMapping,
                          float lodIntoBiasProp = NOTCH_CURVE_NONE, bool allowLodIntoBias = true /*static*/,
                          int lodBiasOrGrad = 0 /*static*/, float3 lodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    // See comments below for those: they should all be statically known from lodBiasOrGrad and allowLodIntoBias!
    bool useLodSampling = (allowLodIntoBias == false) && lodBiasOrGrad == AXF_SAMPLE_USE_LOD;
    bool useBiasSampling = (allowLodIntoBias == false) && lodBiasOrGrad == AXF_SAMPLE_USE_BIAS;
    bool useCustomGradSampling = lodBiasOrGrad == AXF_SAMPLE_USE_GRAD;
    bool useCachedDdxDdy = allowLodIntoBias;
    // ...see below: we need to use _GRAD sampling with allowLodIntoBias, so if no custom derivatives are given, we need to use
    // our precomputed ones.
#ifdef AXF_REUSE_SCREEN_DDXDDY
    useCachedDdxDdy = true;
#endif
    float3 lodToBiasNotchScales = 1;
    float3 normalFadeNotchScales_unused;
    bool useLodSamplingToBiasNotch = false;

    if (allowLodIntoBias)
    {
        NotchGetScales(textureName, samplerName, scaleOffset, texSize, uvMapping, /*calledForNomalMap*/false,
                       /*lodIntoFadeProp:*/NOTCH_CURVE_NONE, /*out*/normalFadeNotchScales_unused,
                       lodIntoBiasProp, /*out:*/lodToBiasNotchScales, /*out:*/useLodSamplingToBiasNotch,
                       lodBiasOrGrad, lodOrBias, triDdx, triDdy);
    }

    // When useLodSamplingToBiasNotch, we should ignore AXF_SAMPLE_USE_LOD or AXF_SAMPLE_USE_BIAS
    // and force the use of derivatives sampling macro, ie do
    // useLodSampling = useBiasSampling = false, and assume their effect has already by taken into account
    // when calculating lodToBiasNotchScales.
    //
    // The problem is that this makes useLodSampling and useBiasSampling no longer fully statically known, 
    // in the case the sampling macros are called with AXF_SAMPLE_USE_LOD or AXF_SAMPLE_USE_BIAS
    // and the NOTCH_CURVES_ENABLED algo is enabled.
    //
    // For that reason, we actually just don't allow useLodSampling or useBiasSampling if we allowLodIntoBias
    // and thus reading the per texture uniform config setting (use none, use curve A or use curve B).
    // 
    // IE allowLodIntoBias param forces _GRAD sampling statically, even if the texture property _LodIntoBias is
    // set to NOTCH_CURVE_NONE.
    //
    // Also, if we're not called with custom given screen derivatives, use our cached ones, since we need to
    //  use _GRAD sampling with our lodToBiasNotchScales.
    //
    // NOTE! This *still allows* calling macros with a LOD or BIAS specified (and thus with use LOD or use BIAS)
    // but these will only be considered in the notch curves if enabled.
    //
    if (useCustomGradSampling && useLodSamplingToBiasNotch)
    {
        triDdx[0] *= lodToBiasNotchScales.x;
        triDdy[0] *= lodToBiasNotchScales.x;
        triDdx[1] *= lodToBiasNotchScales.y;
        triDdy[1] *= lodToBiasNotchScales.y;
        triDdx[2] *= lodToBiasNotchScales.z;
        triDdy[2] *= lodToBiasNotchScales.z;
    }

#ifdef _MAPPING_TRIPLANAR
    float4 val = 0;

    val += uvMapping.triplanarWeights.x 
           * ( useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
           : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
           : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), triDdx[0], triDdy[0])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset),
                                                     lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddxZY, lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddyZY)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset)) );
    val += uvMapping.triplanarWeights.y 
           * ( useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
           : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
           : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), triDdx[1], triDdy[1])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset),
                                                     lodToBiasNotchScales.y * scaleOffset.xy * uvMapping.ddxXZ, lodToBiasNotchScales.y * scaleOffset.xy * uvMapping.ddyXZ)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset)) );
    val += uvMapping.triplanarWeights.z 
           * ( useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
           : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
           : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), triDdx[2], triDdy[2])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset),
                                                     lodToBiasNotchScales.z * scaleOffset.xy * uvMapping.ddxXY, lodToBiasNotchScales.z * scaleOffset.xy * uvMapping.ddyXY)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset)) );

    return val;
#else
    return useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
           : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
           : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), triDdx[0], triDdy[0])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset),
                                                     lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddxBase, lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddyBase)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset));
#endif
}


// Normal map sampling requires special care especially for triplanar, we will use gradients for that.
// Also, AxF normal maps are encoded on 3 channels (xyz) but are still tangent space.
// Make sure useLod is used statically!
// Note that scaleOffset are the texture specific ones, not the main material ones!
// Last coordinate is the normal variance if any.

float4 AxFSampleTexture2DNormalAsSurfaceGrad(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, float2 texSize/*only valid if NOTCH_USES_CUSTOM_LOD_TEXSIZES*/, TextureUVMapping uvMapping,
                                             float lodIntoFadeProp = NOTCH_CURVE_NONE,
                                             float lodIntoBiasProp = NOTCH_CURVE_NONE,
                                             bool allowLodIntoBias = true /*static*/,
                                             int lodBiasOrGrad = 0 /*static*/, float3 lodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    float scale = 1.0;
    // See comments in AxfSampleTexture2D for those: they should all be statically known from lodBiasOrGrad and allowLodIntoBias!
    bool useLodSampling = (allowLodIntoBias == false) && lodBiasOrGrad == AXF_SAMPLE_USE_LOD;
    bool useBiasSampling = (allowLodIntoBias == false) && lodBiasOrGrad == AXF_SAMPLE_USE_BIAS;
    bool useCustomGradSampling = lodBiasOrGrad == AXF_SAMPLE_USE_GRAD;
    bool useCachedDdxDdy = allowLodIntoBias;
    // ...see AxfSampleTexture2D: we need to use _GRAD sampling with allowLodIntoBias, so if no custom derivatives are given, we need to use
    // our precomputed ones.
#ifdef AXF_REUSE_SCREEN_DDXDDY
    useCachedDdxDdy = true;
#endif
    float3 normalFadeNotchScales = 1;
    float3 lodToBiasNotchScales = 1;
    bool useLodSamplingToBiasNotch = false;

    if (allowLodIntoBias)
    {
        NotchGetScales(textureName, samplerName, scaleOffset, texSize, uvMapping, /*calledForNomalMap*/ true,
                       lodIntoFadeProp, /*out:*/normalFadeNotchScales,
                       lodIntoBiasProp, /*out:*/lodToBiasNotchScales, /*out:*/useLodSamplingToBiasNotch,
                       lodBiasOrGrad, lodOrBias, triDdx, triDdy);
    }
    if (useCustomGradSampling && useLodSamplingToBiasNotch)
    {
        triDdx[0] *= lodToBiasNotchScales.x;
        triDdy[0] *= lodToBiasNotchScales.x;
        triDdx[1] *= lodToBiasNotchScales.y;
        triDdy[1] *= lodToBiasNotchScales.y;
        triDdx[2] *= lodToBiasNotchScales.z;
        triDdy[2] *= lodToBiasNotchScales.z;
    }


    float normalVariance = 0.0;

#ifdef _MAPPING_TRIPLANAR

    float2 derivXplane;
    float2 derivYPlane;
    float2 derivZPlane;
    float4 packedNormal;
    derivXplane = derivYPlane = derivZPlane = float2(0.0, 0.0);

    // UnpackDerivativeNormalRGB will unpack an RGB tangent space normal map and output a corresponding height map gradient
    // (We will sum those to get a volume gradient and from it a surface gradient (and/or a final normal). Both have 3 coordinates)

    packedNormal = useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
                   : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
                   : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), triDdx[0], triDdy[0])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset),
                                                             lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddxZY, lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddyZY)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset));
    normalVariance += uvMapping.triplanarWeights.x * AXF_UNPACK_NORMAL_VARIANCE(packedNormal);
    derivXplane = uvMapping.triplanarWeights.x * AXF_DERIVATIVE_NORMAL(packedNormal, scale);

    derivXplane *= normalFadeNotchScales.x;

    packedNormal = useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
                   : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
                   : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), triDdx[1], triDdy[1])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset),
                                                             lodToBiasNotchScales.y * scaleOffset.xy * uvMapping.ddxXZ, lodToBiasNotchScales.y * scaleOffset.xy * uvMapping.ddyXZ)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset));
    normalVariance += uvMapping.triplanarWeights.y * AXF_UNPACK_NORMAL_VARIANCE(packedNormal);
    derivYPlane = uvMapping.triplanarWeights.y * AXF_DERIVATIVE_NORMAL(packedNormal, scale);


    derivYPlane *= normalFadeNotchScales.y;

    packedNormal = useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
                   : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
                   : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), triDdx[2], triDdy[2])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset),
                                                             lodToBiasNotchScales.z * scaleOffset.xy * uvMapping.ddxXY, lodToBiasNotchScales.z * scaleOffset.xy * uvMapping.ddyXY)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset));
    normalVariance += uvMapping.triplanarWeights.z * AXF_UNPACK_NORMAL_VARIANCE(packedNormal);
    derivZPlane = uvMapping.triplanarWeights.z * AXF_DERIVATIVE_NORMAL(packedNormal, scale);

    derivZPlane *= normalFadeNotchScales.z;

    // Important note! See SurfaceGradientFromTriplanarProjection:
    // Tiling scales should NOT be negative!

    // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
    float3 volumeGrad = float3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
    float3 surfaceGrad = SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad);

    // We don't need to process further operation on the gradient, but we dont resolve it to a normal immediately:
    // ie by doing return SurfaceGradientResolveNormal(uvMapping.vertexNormalWS, surfaceGrad);
    // This is because we use GetNormalWS() later which with #define SURFACE_GRADIENT, expects a surface gradient.
    return float4(surfaceGrad, normalVariance);

#else
    // No triplanar: in that case, just sample the texture, but also unpacks it as a surface gradient! See comment above

    float4 packedNormal = useLodSampling ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
                          : useBiasSampling ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
                          : useCustomGradSampling ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), triDdx[0], triDdy[0])
                          : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset),
                                                                    lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddxBase, lodToBiasNotchScales.x * scaleOffset.xy * uvMapping.ddyBase)
                          : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset));
    normalVariance = AXF_UNPACK_NORMAL_VARIANCE(packedNormal);
    float2 deriv = AXF_DERIVATIVE_NORMAL(packedNormal, scale);

#ifndef _MAPPING_PLANAR
    // No planar mapping, in that case, just use the generated (or simply cached if using uv0) TBN:
    return float4(normalFadeNotchScales.x * SurfaceGradientFromTBN(deriv, uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS), normalVariance);
#else
    float3 volumeGrad;

    // We will use the mapping selector mask to know which plane we used.
    // This allows us to properly build the volume gradient:
    if (_MappingMask.x == 1.0) // uvZY
        volumeGrad = float3(0.0, deriv.y, deriv.x);
    else if (_MappingMask.y == 1.0) // uvXZ
        volumeGrad = float3(deriv.y, 0.0, deriv.x);
    else if (_MappingMask.z == 1.0) // uvXY
        volumeGrad = float3(deriv.x, deriv.y, 0.0);

    return float4(normalFadeNotchScales.x * SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad), normalVariance);
#endif // if not _MAPPING_PLANAR
#endif // if triplanar.
}

#define AXF_SAMPLE_TEXTURE2D_AA(name, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, sampler##name, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias)
#define AXF_SAMPLE_TEXTURE2D(name, uvMapping) AXF_SAMPLE_TEXTURE2D_AA(name, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_AA(name, samplername, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias)
#define AXF_SAMPLE_SMP_TEXTURE2D(name, samplername, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_AA(name, samplername, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_LOD_AA(name, samplername, lod, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_LOD, lod)
#define AXF_SAMPLE_SMP_TEXTURE2D_LOD(name, samplername, lod, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_LOD_AA(name, samplername, lod, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS_AA(name, samplername, bias, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_BIAS, bias)
#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS(name, samplername, bias, uvMapping, allowLodIntoBias) AXF_SAMPLE_SMP_TEXTURE2D_BIAS_AA(name, samplername, bias, uvMapping, true)

#ifdef _MAPPING_TRIPLANAR
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_AA(name, samplername, triddx, triddy, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, triddx, triddy)
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD(name, samplername, triddx, triddy, uvMapping, allowLodIntoBias) AXF_SAMPLE_SMP_TEXTURE2D_GRAD_AA(name, samplername, triddx, triddy, uvMapping, true)

#else

#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_AA(name, samplername, vddx, vddy, uvMapping, allowLodIntoBias) AxfSampleTexture2D(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, float3x2(vddx, (float2)0, (float2)0), float3x2(vddy, (float2)0, (float2)0))
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD(name, samplername, vddx, vddy, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_GRAD_AA(name, samplername, vddx, vddy, uvMapping, true)
#endif

#define AXF_SAMPLE_TEXTURE2D_NORMAL_AA(name, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, sampler##name, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias)
#define AXF_SAMPLE_TEXTURE2D_NORMAL(name, uvMapping) AXF_SAMPLE_TEXTURE2D_NORMAL_AA(name, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AA(name, samplername, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias)
#define AXF_SAMPLE_SMP_TEXTURE2D_NORMAL(name, samplername, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AA(name, samplername, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_LOD_NORMAL_AA(name, samplername, lod, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_LOD, lod)
#define AXF_SAMPLE_SMP_TEXTURE2D_LOD_NORMAL(name, samplername, lod, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_LOD_NORMAL_AA(name, samplername, lod, uvMapping, true)

#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS_NORMAL_AA(name, samplername, bias, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_BIAS, bias)
#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS_NORMAL(name, samplername, bias, uvMapping, allowLodIntoBias) AXF_SAMPLE_SMP_TEXTURE2D_BIAS_NORMAL_AA(name, samplername, bias, uvMapping, true)

#ifdef _MAPPING_TRIPLANAR
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AA(name, samplername, triddx, triddy, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, triddx, triddy)
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL(name, samplername, triddx, triddy, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AA(name, samplername, triddx, triddy, uvMapping, true)

#else

#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AA(name, samplername, vddx, vddy, uvMapping, allowLodIntoBias) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, AXF_TEXSIZE_FROM_NAME(name), uvMapping, name##_LodIntoFade, name##_LodIntoBias, allowLodIntoBias, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, float3x2(vddx, (float2)0, (float2)0), float3x2(vddy, (float2)0, (float2)0))
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL(name, samplername, vddx, vddy, uvMapping) AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AA(name, samplername, vddx, vddy, uvMapping, true)
#endif


float2 TileFlakesUV(float2 flakesUV)
{
    // Create mirrored UVs to hide flakes tiling
    // TODO_FLAKES: this isn't tiling!
    if ((int(flakesUV.y) & 1) == 0)
        flakesUV.x += 0.5;
    else if ((uint(1000.0 + flakesUV.x) % 3) == 0)
        flakesUV.y = 1.0 - flakesUV.y;
    else
        flakesUV.x = 1.0 - flakesUV.x;

    return flakesUV;
}


void SetFlakesSurfaceData(TextureUVMapping uvMapping, inout SurfaceData surfaceData)
{
    surfaceData.flakesDdxZY = surfaceData.flakesDdyZY = surfaceData.flakesDdxXZ = surfaceData.flakesDdyXZ =
    surfaceData.flakesDdxXY = surfaceData.flakesDdyXY = 0;

#ifdef _MAPPING_TRIPLANAR
    float2 uv;

    uv = AXF_TRANSFORM_TEXUV_BYNAME(uvMapping.uvZY, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesMipLevelZY = CALCULATE_TEXTURE2D_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, uv);
#ifndef FLAKES_TILE_BEFORE_SCALE
    surfaceData.flakesUVZY = TileFlakesUV(uv);
#else
    surfaceData.flakesUVZY = AXF_TRANSFORM_TEXUV_BYNAME(TileFlakesUV(uvMapping.uvZY), _CarPaint2_BTFFlakeMap);
#endif

    uv = AXF_TRANSFORM_TEXUV_BYNAME(uvMapping.uvXZ, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesMipLevelXZ = CALCULATE_TEXTURE2D_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, uv);
#ifndef FLAKES_TILE_BEFORE_SCALE
    surfaceData.flakesUVXZ = TileFlakesUV(uv);
#else
    surfaceData.flakesUVXZ = AXF_TRANSFORM_TEXUV_BYNAME(TileFlakesUV(uvMapping.uvXZ), _CarPaint2_BTFFlakeMap);
#endif

    uv = AXF_TRANSFORM_TEXUV_BYNAME(uvMapping.uvXY, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesMipLevelXY = CALCULATE_TEXTURE2D_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, uv);
#ifndef FLAKES_TILE_BEFORE_SCALE
    surfaceData.flakesUVXY = TileFlakesUV(uv);
#else
    surfaceData.flakesUVXY = AXF_TRANSFORM_TEXUV_BYNAME(TileFlakesUV(uvMapping.uvXY), _CarPaint2_BTFFlakeMap);
#endif

    surfaceData.flakesTriplanarWeights = uvMapping.triplanarWeights;

#ifdef FLAKES_USE_DDXDDY
    // Filling surfaceData.flakesDdx* to nonzero values will automatically ignore surfaceData.flakesMipLevel*
    // and the compiler will optimize them out (see SampleFlakes in AxF.hlsl)
    surfaceData.flakesDdxZY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddxZY, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdyZY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddyZY, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdxXZ = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddxXZ, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdyXZ = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddyXZ, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdxXY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddxXY, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdyXY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddyXY, _CarPaint2_BTFFlakeMap);
#endif

#else // TRIPLANAR

    float2 uv;
    // NOTE: When not triplanar UVZY has one uv set or one planar coordinate set,
    // and this planar coordinate set isn't necessarily ZY, we just reuse this field
    // as a common one.
    uv = AXF_TRANSFORM_TEXUV_BYNAME(uvMapping.uvBase, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesMipLevelZY = CALCULATE_TEXTURE2D_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, uv);
#ifndef FLAKES_TILE_BEFORE_SCALE
    surfaceData.flakesUVZY = TileFlakesUV(uv);
#else
    surfaceData.flakesUVZY = AXF_TRANSFORM_TEXUV_BYNAME(TileFlakesUV(uvMapping.uvBase), _CarPaint2_BTFFlakeMap);
#endif

#ifdef FLAKES_USE_DDXDDY
    // Filling surfaceData.flakesDdx* to nonzero values will automatically ignore surfaceData.flakesMipLevel*
    // and the compiler will optimize them out (see SampleFlakes in AxF.hlsl)
    surfaceData.flakesDdxZY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddxBase, _CarPaint2_BTFFlakeMap);
    surfaceData.flakesDdyZY = AXF_SCALE_DDXDDY_BYNAME(uvMapping.ddyBase, _CarPaint2_BTFFlakeMap);
#endif

    surfaceData.flakesUVXZ = surfaceData.flakesUVXY = 0;
    surfaceData.flakesMipLevelXZ = surfaceData.flakesMipLevelXY = 0;
    surfaceData.flakesTriplanarWeights = 0;
#endif
}

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
#if defined(_AXF_BRDF_TYPE_SVBRDF) || defined(_AXF_BRDF_TYPE_CAR_PAINT) // Not implemented for BTF
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.diffuseColor.xyz = surfaceData.diffuseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
#ifdef _AXF_BRDF_TYPE_SVBRDF
        surfaceData.clearcoatColor.xyz = surfaceData.clearcoatColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
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

        surfaceData.specularLobe.x = PerceptualSmoothnessToRoughness(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.x) * decalSurfaceData.mask.w + decalSurfaceData.mask.z);
        surfaceData.specularLobe.y = PerceptualSmoothnessToRoughness(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.y) * decalSurfaceData.mask.w + decalSurfaceData.mask.z);
#ifdef _AXF_BRDF_TYPE_CAR_PAINT
        surfaceData.specularLobe.z = PerceptualSmoothnessToRoughness(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.z) * decalSurfaceData.mask.w + decalSurfaceData.mask.z);
#endif
    }
#endif
}

bool HasPhongTypeBRDF()
{
    uint type = ((_SVBRDF_BRDFType >> 1) & 7);
    return type == 1 || type == 4;
}

float2 AxFGetRoughnessFromSpecularLobeTexture(float2 specularLobe)
{
    // For Blinn-Phong, AxF encodes specularLobe.xy as log2(shiniExp_xy) so
    //     shiniExp = exp2(abs(specularLobe.xy))
    // A good fit for a corresponding Beckmann roughness is
    //     roughnessBeckmann^2 = 2 /(shiniExp + 2)
    // See eg
    // http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html
    // http://simonstechblog.blogspot.com/2011/12/microfacet-brdf.html

    // We thus have 
    //     roughnessBeckmann = sqrt(2) * rsqrt(exp2(abs(specularLobe.xy)) + 2);
    //     shiniExp = 2 * rcp(max(0.0001,(roughnessBeckmann*roughnessBeckmann))) - 2;

    return (HasPhongTypeBRDF() ? (sqrt(2) * rsqrt(exp2(abs(specularLobe)) + 2)) : specularLobe);
}


float AxFGetBeckmannPerceptualSmoothnessFromRoughness(float roughness)
{
    float perceptualSmoothness = RoughnessToPerceptualSmoothness(roughness);

    // Consider everything else from GGX as Beckmann based
    // (Not true for Blinn-Phong but we convert approximatively elsewhere)
    return ( ((_SVBRDF_BRDFType >> 1) & 7) == 3 /* GGX */) ? RoughnessToPerceptualSmoothness(GGXRoughnessToBeckmannRoughness(roughness)) : perceptualSmoothness;
}

float AxFGetRoughnessFromBeckmannPerceptualSmoothness(float perceptualSmoothness)
{
    float roughness = PerceptualSmoothnessToRoughness(perceptualSmoothness);

    // Consider everything else from GGX as Beckmann based
    // (Not true for Blinn-Phong but we convert approximatively elsewhere)
    return ( ((_SVBRDF_BRDFType >> 1) & 7) == 3 /* GGX */) ? (BeckmannRoughnessToGGXRoughness(roughness)) : roughness;
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _DOUBLESIDED_ON
    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal

    // Note that in uvMapping, the main scaling _Material_SO.xy has been applied:
    TextureUVMapping uvMapping;
    InitTextureUVMapping(input, uvMapping);
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    float alpha = AXF_SAMPLE_SMP_TEXTURE2D_AA(_SVBRDF_AlphaMap, sampler_SVBRDF_AlphaMap, uvMapping, /*AxfSampleTexture2D*/false).x;

#ifdef _ALPHATEST_ON
    float alphaCutoff = _AlphaCutoff;

    #if SHADERPASS == SHADERPASS_SHADOWS 
        GENERIC_ALPHA_TEST(alpha, _UseShadowThreshold ? _AlphaCutoffShadow : alphaCutoff);
    #else
        GENERIC_ALPHA_TEST(alpha, alphaCutoff);
    #endif
#endif

    float4 gradient = 0;
    float4 coatGradient = 0;

    surfaceData.ambientOcclusion = 1.0;
    surfaceData.specularOcclusion = 1.0;
    surfaceData.specularLobe = 0;

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------

#ifdef _AXF_BRDF_TYPE_SVBRDF

    surfaceData.diffuseColor = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_DiffuseColorMap, sampler_SVBRDF_DiffuseColorMap, uvMapping).xyz;
    surfaceData.specularColor = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_SpecularColorMap, sampler_SVBRDF_SpecularColorMap, uvMapping).xyz;
    surfaceData.specularLobe.xy = _SVBRDF_SpecularLobeMapScale * AxFGetRoughnessFromSpecularLobeTexture( AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, uvMapping).xy);

    // The AxF models include both a general coloring term that they call "specular color" while the f0 is actually another term,
    // seemingly always scalar:
    surfaceData.fresnelF0 = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_FresnelMap, sampler_SVBRDF_FresnelMap, uvMapping).x;
    surfaceData.height_mm = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_HeightMap, sampler_SVBRDF_HeightMap, uvMapping).x * _SVBRDF_HeightMapMaxMM;
    // Our importer range remaps the [-HALF_PI, HALF_PI) range to [0,1). We map back here:
    surfaceData.anisotropyAngle =
        HALF_PI * (2.0 * AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_AnisoRotationMap, sampler_SVBRDF_AnisoRotationMap, uvMapping).x - 1.0);
    surfaceData.clearcoatColor = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_ClearcoatColorMap, sampler_SVBRDF_ClearcoatColorMap, uvMapping).xyz;

    // The importer transforms the IOR to an f0, we map it back here as an IOR clamped under at 1.0
    // TODO: if we're reusing float textures anyway, we shouldn't need the normalization that transforming to an f0 provides.
    float clearcoatF0 = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_ClearcoatIORMap, sampler_SVBRDF_ClearcoatIORMap, uvMapping).x;
    float sqrtF0 = sqrt(clearcoatF0);
    surfaceData.clearcoatIOR = max(1.0, (1.0 + sqrtF0) / (1.00001 - sqrtF0));    // We make sure it's working for F0=1

    //
    // TBN
    //
    // Note: since SURFACE_GRADIENT is enabled, resolve is done with input.tangentToWorld[2] in GetNormalWS(),
    // and uvMapping uses that as vertexNormalWS.

    //To help adjust the normal anti-moire lod-dependent notch, display it in debug:
#if defined(DEBUG_DISPLAY)
    float3 normalFadeNotchScales = 1.0;
    float3 lodToBiasNotchScales = 1.0;
    bool useLodSamplingToBiasNotch;
    float4 scaleOffset = AXF_GET_SINGE_SCALE_OFFSET(_SVBRDF_NormalMap);

    NotchGetScales(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, scaleOffset, AXF_TEXSIZE_FROM_NAME(_SVBRDF_NormalMap), uvMapping, /*calledForNomalMap*/true,
                   _SVBRDF_NormalMap_LodIntoFade, /*out*/normalFadeNotchScales,
                   _SVBRDF_NormalMap_LodIntoBias, /*out*/lodToBiasNotchScales, /*out:*/useLodSamplingToBiasNotch);

    surfaceData.normalFadeNotchScaleDebug = normalFadeNotchScales.x;

    //Test vis of custom lod vs CalculateLevelOfDetail:
    //float dSq = max(dot(uvMapping.ddxBase * _SVBRDF_NormalMap_TexelSize.zw * scaleOffset.xy, uvMapping.ddxBase * _SVBRDF_NormalMap_TexelSize.zw * scaleOffset.xy),
    //                dot(uvMapping.ddyBase * _SVBRDF_NormalMap_TexelSize.zw * scaleOffset.xy, uvMapping.ddyBase * _SVBRDF_NormalMap_TexelSize.zw * scaleOffset.xy));
    //float lodCustom = (0.5 * log2(dSq));
    //float lodMacro = CALCULATE_TEXTURE2D_LOD(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, uvMapping.uvBase * scaleOffset.xy);
    //surfaceData.normalFadeNotchScaleDebug = lodCustom;
    //surfaceData.normalFadeNotchScaleDebug = lodMacro;
#endif

    gradient = AXF_SAMPLE_SMP_TEXTURE2D_NORMAL(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, uvMapping);
    coatGradient = AXF_SAMPLE_SMP_TEXTURE2D_NORMAL(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, uvMapping);

    GetNormalWS(input, gradient.xyz, surfaceData.normalWS, doubleSidedConstants);
    GetNormalWS(input, coatGradient.xyz, surfaceData.clearcoatNormalWS, doubleSidedConstants);

    // Useless for SVBRDF, will be optimized out
    //SetFlakesSurfaceData(uvMapping, surfaceData);

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_CAR_PAINT
    //-----------------------------------------------------------------------------

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    surfaceData.diffuseColor = _CarPaint2_CTDiffuse;
    surfaceData.clearcoatIOR = max(1.001, _CarPaint2_ClearcoatIOR); // Can't be exactly 1 otherwise the precise fresnel divides by 0!

    surfaceData.specularLobe = _CarPaint2_CTSpreads.xyz; // We may want to modify these (eg for Specular AA)

    surfaceData.normalWS = input.tangentToWorld[2].xyz;
    coatGradient = AXF_SAMPLE_SMP_TEXTURE2D_NORMAL(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, uvMapping);
    GetNormalWS(input, coatGradient.xyz, surfaceData.clearcoatNormalWS, doubleSidedConstants);

    SetFlakesSurfaceData(uvMapping, surfaceData);

    // Useless for car paint BSDF
    surfaceData.specularColor = 0;
    surfaceData.fresnelF0 = 0;
    surfaceData.height_mm = 0;
    surfaceData.anisotropyAngle = 0;
    surfaceData.clearcoatColor = 0;
#endif

    // TODO
    // Assume same xyz encoding for AxF bent normal as other normal maps.
    //float3 bentNormalWS;
    //GetNormalWS(input, 2.0 * SAMPLE_TEXTURE2D(_BentNormalMap, sampler_BentNormalMap, UV0).xyz - 1.0, bentNormalWS, doubleSidedConstants);

    float perceptualRoughness = RoughnessToPerceptualRoughness(GetScalarRoughness(surfaceData.specularLobe));

    //TODO 
//#if defined(_SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP)
    // Note: we use normalWS as it will always exist and be equal to clearcoatNormalWS if there's no coat
    // (otherwise we do SO with the base lobe, might be wrong depending on way AO is computed, will be wrong either way with a single non-lobe specific value)
    //surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, perceptualRoughness);
//#endif
#if !defined(_SPECULAR_OCCLUSION_NONE)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, perceptualRoughness);
#endif

    // Propagate the geometry normal
    surfaceData.geomNormalWS = input.tangentToWorld[2];

    // Finalize tangent space
    surfaceData.tangentWS = uvMapping.vertexTangentWS;
    // TODOTODO:
    // This is crappy: anisotropy rotation don't mix triplanar style like scalar values because of what it represents. That's why in HDRP we use 
    // tangent space tangent vector maps and triplanar sample those as we do normals in the surface gradients framework! 
    // Better to rebuild a gradient in the proper space from each rotation, combine those gradients as normals and resolve here.
    if (HasAnisotropy())
    {
        float3 tangentTS = float3(1, 0, 0);
        // We will keep anisotropyAngle in surfaceData for now for debug info, register will be freed
        // anyway by the compiler (never used again after this)
        sincos(surfaceData.anisotropyAngle, tangentTS.y, tangentTS.x);
        float3x3 tbn = float3x3(uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS, uvMapping.vertexNormalWS);
        surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.tangentToWorld);
    }

    #if HAVE_DECALS
        if (_EnableDecals)
        {
            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
        }
    #endif

    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    // Instead of
    // surfaceData.biTangentWS = Orthonormalize(input.tangentToWorld[1], surfaceData.normalWS),
    // make AxF follow what we do in other HDRP shaders for consistency: use the
    // cross product to finish building the TBN frame and thus get a frame matching
    // the handedness of the world space (tangentToWorld can be passed right handed while
    // Unity's WS is left handed, so this makes a difference here).

    bool geometricSpecularAAEnabled = false;
    bool normalMapFilteringEnabled = false;
#if defined(_ENABLE_GEOMETRIC_SPECULAR_AA)
    geometricSpecularAAEnabled = true;
    // Specular AA for geometric curvature
#endif
#if defined(_ENABLE_NORMAL_MAP_FILTERING)
    normalMapFilteringEnabled = true;
#endif

    if (geometricSpecularAAEnabled || normalMapFilteringEnabled)
    {
        float geometricVariance = geometricSpecularAAEnabled ? GeometricNormalVariance(input.tangentToWorld[2], _SpecularAAScreenSpaceVariance) : 0.0;
        float normalMapFilteringVariance = _NormalMapFilteringWeight * (normalMapFilteringEnabled ? DecodeVariance(gradient.w) : 0.0);

        float val;

        val = NormalFiltering(AxFGetBeckmannPerceptualSmoothnessFromRoughness(surfaceData.specularLobe.x), geometricVariance + normalMapFilteringVariance, _SpecularAAThreshold);
        surfaceData.specularLobe.x = AxFGetRoughnessFromBeckmannPerceptualSmoothness(val);

        val = NormalFiltering(AxFGetBeckmannPerceptualSmoothnessFromRoughness(surfaceData.specularLobe.y), geometricVariance + normalMapFilteringVariance, _SpecularAAThreshold);
        surfaceData.specularLobe.y = AxFGetRoughnessFromBeckmannPerceptualSmoothness(val);

#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
        // Useless for car paint, no base normal map anyway.
        //val = NormalFiltering(AxFGetBeckmannPerceptualSmoothnessFromRoughness(surfaceData.specularLobe.z), geometricVariance + normalMapFilteringVariance, _SpecularAAThreshold);
        //surfaceData.specularLobe.z = val;
#endif
        // TODO: coat doesn't display delta / dirac lights for now (like the AxF X-Rite viewer)
        // so there's no coatroughness in surfaceData
    }

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
    
#ifdef _ALPHATEST_ON
    // Used for sharpening by alpha to mask
    builtinData.alphaClipTreshold = _AlphaCutoff;
#endif

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
