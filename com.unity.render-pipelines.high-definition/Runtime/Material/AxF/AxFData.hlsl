//-------------------------------------------------------------------------------------
// Defines
//-------------------------------------------------------------------------------------
// Gradients are now required:
#define SURFACE_GRADIENT // Note: this affects Material/MaterialUtilities.hlsl's GetNormalWS() and makes it expect a surface gradient.

//to test #define FLAKES_TILE_BEFORE_SCALE
#define AXF_REUSE_SCREEN_DDXDDY
// ...ie use _GRAD sampling for everything and calculate those only one time:
// offset doesn't change derivatives, and scales just scales them, so we can cache them.

// The compiler can't unroll the lightloop if flakes are sampled inside it, so we need to cache either LOD
// or derivatives. We prefer the later, as the CalculateLevelOfDetail will not work when anisotropic filtering
// is used, and AxF materials textures often have trilinear filtering set.
#define FLAKES_USE_DDXDDY

#define AXF_USES_RG_NORMAL_MAPS // else, RGB

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
#ifdef AXF_USES_RG_NORMAL_MAPS
#define AXF_DERIVATIVE_NORMAL UnpackDerivativeNormalRGorAG
#else
#define AXF_DERIVATIVE_NORMAL UnpackDerivativeNormalRGB
#endif

// Note: the scaling _Material_SO.xy should already be in texuv, but NOT the bias.
#define AXF_TRANSFORM_TEXUV_BYNAME(texuv, name) ((texuv.xy) * name##_SO.xy + name##_SO.zw + _Material_SO.zw)
#define AXF_GET_SINGE_SCALE_OFFSET(name) (name##_SO)
#define AXF_TRANSFORM_TEXUV(texuv, scaleOffset) ((texuv.xy) * scaleOffset.xy + scaleOffset.zw + _Material_SO.zw)

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

// Make sure lodBiasOrGrad is used statically!
//
#define AXF_SAMPLE_USE_LOD 1
#define AXF_SAMPLE_USE_BIAS 2
#define AXF_SAMPLE_USE_GRAD 3

// Note that scaleOffset are the texture specific ones, not the main material ones!
float4 AxfSampleTexture2D(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, TextureUVMapping uvMapping,
                          int lodBiasOrGrad = 0, float3 lodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    bool useLod = lodBiasOrGrad == 1;
    bool useBias = lodBiasOrGrad == 2;
    bool useGrad = lodBiasOrGrad == 3;
    bool useCachedDdxDdy = false;    
#ifdef AXF_REUSE_SCREEN_DDXDDY
    useCachedDdxDdy = false;
#endif

#ifdef _MAPPING_TRIPLANAR
    float4 val = 0;

    val += uvMapping.triplanarWeights.x 
           * ( useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
           : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
           : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), triDdx[0], triDdy[0])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset),  scaleOffset.xy * uvMapping.ddxZY, scaleOffset.xy * uvMapping.ddyZY)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset)) );
    val += uvMapping.triplanarWeights.y 
           * ( useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
           : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
           : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), triDdx[1], triDdy[1])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset),  scaleOffset.xy * uvMapping.ddxXZ, scaleOffset.xy * uvMapping.ddyXZ)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset)) );
    val += uvMapping.triplanarWeights.z 
           * ( useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
           : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
           : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), triDdx[2], triDdy[2])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset),  scaleOffset.xy * uvMapping.ddxXY, scaleOffset.xy * uvMapping.ddyXY)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset)) );

    return val;
#else
    return useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
           : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
           : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), triDdx[0], triDdy[0])
           : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset),  scaleOffset.xy * uvMapping.ddxBase, scaleOffset.xy * uvMapping.ddyBase)
           : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset));
#endif
}

// Normal map sampling requires special care especially for triplanar, we will use gradients for that.
// Also, AxF normal maps are encoded on 3 channels (xyz) but are still tangent space.
// Make sure useLod is used statically!
// Note that scaleOffset are the texture specific ones, not the main material ones!
float3 AxFSampleTexture2DNormalAsSurfaceGrad(TEXTURE2D_PARAM(textureName, samplerName), float4 scaleOffset, TextureUVMapping uvMapping,
                                             int lodBiasOrGrad = 0, float3 lodOrBias = 0, float3x2 triDdx = (float3x2)0, float3x2 triDdy = (float3x2)0)
{
    float scale = 1.0;
    bool useLod = lodBiasOrGrad == 1;
    bool useBias = lodBiasOrGrad == 2;
    bool useGrad = lodBiasOrGrad == 3;
    bool useCachedDdxDdy = false;    
#ifdef AXF_REUSE_SCREEN_DDXDDY
    useCachedDdxDdy = true;
#endif

#ifdef _MAPPING_TRIPLANAR

    float2 derivXplane;
    float2 derivYPlane;
    float2 derivZPlane;
    float4 packedNormal;
    derivXplane = derivYPlane = derivZPlane = float2(0.0, 0.0);

    // UnpackDerivativeNormalRGB will unpack an RGB tangent space normal map and output a corresponding height map gradient
    // (We will sum those to get a volume gradient and from it a surface gradient (and/or a final normal). Both have 3 coordinates)

    packedNormal = useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
                   : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), lodOrBias.x)
                   : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), triDdx[0], triDdy[0])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset), scaleOffset.xy * uvMapping.ddxZY, scaleOffset.xy * uvMapping.ddyZY)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvZY, scaleOffset));
    derivXplane = uvMapping.triplanarWeights.x * AXF_DERIVATIVE_NORMAL(packedNormal, scale);

    packedNormal = useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
                   : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), lodOrBias.y)
                   : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), triDdx[1], triDdy[1])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset), scaleOffset.xy * uvMapping.ddxXZ, scaleOffset.xy * uvMapping.ddyXZ)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXZ, scaleOffset));
    derivYPlane = uvMapping.triplanarWeights.y * AXF_DERIVATIVE_NORMAL(packedNormal, scale);

    packedNormal = useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
                   : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), lodOrBias.z)
                   : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), triDdx[2], triDdy[2])
                   : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset), scaleOffset.xy * uvMapping.ddxXY, scaleOffset.xy * uvMapping.ddyXY)
                   : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvXY, scaleOffset));
    derivZPlane = uvMapping.triplanarWeights.z * AXF_DERIVATIVE_NORMAL(packedNormal, scale);

    // Important note! See SurfaceGradientFromTriplanarProjection:
    // Tiling scales should NOT be negative!

    // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
    float3 volumeGrad = float3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
    float3 surfaceGrad = SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad);

    // We don't need to process further operation on the gradient, but we dont resolve it to a normal immediately:
    // ie by doing return SurfaceGradientResolveNormal(uvMapping.vertexNormalWS, surfaceGrad);
    // This is because we use GetNormalWS() later which with #define SURFACE_GRADIENT, expects a surface gradient.
    return surfaceGrad;

#else
    // No triplanar: in that case, just sample the texture, but also unpacks it as a surface gradient! See comment above

    float4 packedNormal = useLod ? SAMPLE_TEXTURE2D_LOD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
                          : useBias ? SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), lodOrBias.x)
                          : useGrad ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), triDdx[0], triDdy[0])
                          : useCachedDdxDdy ? SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset), scaleOffset.xy * uvMapping.ddxBase, scaleOffset.xy * uvMapping.ddyBase)
                          : SAMPLE_TEXTURE2D(textureName, samplerName, AXF_TRANSFORM_TEXUV(uvMapping.uvBase, scaleOffset));
    float2 deriv = AXF_DERIVATIVE_NORMAL(packedNormal, scale);

#ifndef _MAPPING_PLANAR
    // No planar mapping, in that case, just use the generated (or simply cached if using uv0) TBN:
    return SurfaceGradientFromTBN(deriv, uvMapping.vertexTangentWS, uvMapping.vertexBitangentWS);
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

    return SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad);
#endif // if not _MAPPING_PLANAR
#endif // if triplanar.
}

#define AXF_SAMPLE_TEXTURE2D(name, uvMapping) AxfSampleTexture2D(name, sampler##name, name##_SO, uvMapping)
#define AXF_SAMPLE_SMP_TEXTURE2D(name, samplername, uvMapping) AxfSampleTexture2D(name, samplername, name##_SO, uvMapping)
#define AXF_SAMPLE_SMP_TEXTURE2D_LOD(name, samplername, lod, uvMapping) AxfSampleTexture2D(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_LOD, lod)
#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS(name, samplername, bias, uvMapping) AxfSampleTexture2D(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_BIAS, bias)

#ifdef _MAPPING_TRIPLANAR
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD(name, samplername, triddx, triddy, uvMapping) AxfSampleTexture2D(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, triddx, triddy)
#else
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD(name, samplername, vddx, vddy, uvMapping) AxfSampleTexture2D(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, float3x2(vddx, (float2)0, (float2)0), float3x2(vddy, (float2)0, (float2)0))
#endif

#define AXF_SAMPLE_TEXTURE2D_NORMAL_AS_GRAD(name, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, sampler##name, name##_SO, uvMapping)
#define AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AS_GRAD(name, samplername, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, uvMapping)
#define AXF_SAMPLE_SMP_TEXTURE2D_LOD_NORMAL_AS_GRAD(name, samplername, lod, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_LOD, lod)
#define AXF_SAMPLE_SMP_TEXTURE2D_BIAS_NORMAL_AS_GRAD(name, samplername, bias, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_BIAS, bias)

#ifdef _MAPPING_TRIPLANAR
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AS_GRAD(name, samplername, triddx, triddy, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, triddx, triddy)
#else
#define AXF_SAMPLE_SMP_TEXTURE2D_GRAD_NORMAL_AS_GRAD(name, samplername, vddx, vddy, uvMapping) AxFSampleTexture2DNormalAsSurfaceGrad(name, samplername, name##_SO, uvMapping, /*lodBiasOrGrad*/ AXF_SAMPLE_USE_GRAD, /*unused*/(float3)0, float3x2(vddx, (float2)0, (float2)0), float3x2(vddy, (float2)0, (float2)0))
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

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)
{
#if defined(_AXF_BRDF_TYPE_SVBRDF) || defined(_AXF_BRDF_TYPE_CAR_PAINT) // Not implemented for BTF
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    surfaceData.diffuseColor.xyz = surfaceData.diffuseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
#ifdef _AXF_BRDF_TYPE_SVBRDF
    surfaceData.clearcoatColor.xyz = surfaceData.clearcoatColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
#endif

    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
    {
        // Affect both normal and clearcoat normal
        surfaceData.normalWS.xyz = SafeNormalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
        surfaceData.clearcoatNormalWS = SafeNormalize(surfaceData.clearcoatNormalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

#ifdef DECALS_4RT // only smoothness in 3RT mode
#ifdef _AXF_BRDF_TYPE_SVBRDF
    if (decalSurfaceData.MAOSBlend.x < 1.0)
    {
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.baseColor.w < 1.0) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor * (1.0f - decalSurfaceData.MAOSBlend.x);
    }
#endif

    surfaceData.clearcoatIOR = lerp(1.0, surfaceData.clearcoatIOR, decalSurfaceData.MAOSBlend.x); // Transition to IOR 1.0 with increase decal coverage (i.e decrease of decalSurfaceData.MAOSBlend.x value)

    // Note:There is no ambient occlusion with AxF material
#endif

    if (decalSurfaceData.mask.w < 1.0)
    {
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

    float alpha = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_AlphaMap, sampler_SVBRDF_AlphaMap, uvMapping).x;

#ifdef _ALPHATEST_ON
    // TODOTODO: Move alpha test earlier and test.
    float alphaCutoff = _AlphaCutoff;

    #if (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
        alphaCutoff = _UseShadowThreshold ? _AlphaCutoffShadow : alphaCutoff;
    #endif

    GENERIC_ALPHA_TEST(alpha, alphaCutoff);
#endif

    surfaceData.ambientOcclusion = 1.0;
    surfaceData.specularOcclusion = 1.0;
    surfaceData.specularLobe = 0;

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------

#ifdef _AXF_BRDF_TYPE_SVBRDF

    surfaceData.diffuseColor = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_DiffuseColorMap, sampler_SVBRDF_DiffuseColorMap, uvMapping).xyz;
    surfaceData.specularColor = AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_SpecularColorMap, sampler_SVBRDF_SpecularColorMap, uvMapping).xyz;
    surfaceData.specularLobe.xy = _SVBRDF_SpecularLobeMapScale * AxFGetRoughnessFromSpecularLobeTexture(
        AXF_SAMPLE_SMP_TEXTURE2D(_SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, uvMapping).xy);

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

    //Normal sampling:
    GetNormalWS(input, AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AS_GRAD(_SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, uvMapping).xyz, surfaceData.normalWS, doubleSidedConstants);
    GetNormalWS(input, AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AS_GRAD(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, uvMapping).xyz, surfaceData.clearcoatNormalWS, doubleSidedConstants);

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
    GetNormalWS(input, AXF_SAMPLE_SMP_TEXTURE2D_NORMAL_AS_GRAD(_ClearcoatNormalMap, sampler_ClearcoatNormalMap, uvMapping).xyz, surfaceData.clearcoatNormalWS, doubleSidedConstants);

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
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, input.tangentToWorld[2], surfaceData);
        }
    #endif

    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    // Instead of
    // surfaceData.biTangentWS = Orthonormalize(input.tangentToWorld[1], surfaceData.normalWS),
    // make AxF follow what we do in other HDRP shaders for consistency: use the
    // cross product to finish building the TBN frame and thus get a frame matching
    // the handedness of the world space (tangentToWorld can be passed right handed while
    // Unity's WS is left handed, so this makes a difference here).

#if defined(_ENABLE_GEOMETRIC_SPECULAR_AA)
    // Specular AA for geometric curvature

    surfaceData.specularLobe.x = PerceptualSmoothnessToRoughness(GeometricNormalFiltering(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.x), input.tangentToWorld[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold));
    surfaceData.specularLobe.y = PerceptualSmoothnessToRoughness(GeometricNormalFiltering(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.y), input.tangentToWorld[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold));
#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
    surfaceData.specularLobe.z = PerceptualSmoothnessToRoughness(GeometricNormalFiltering(RoughnessToPerceptualSmoothness(surfaceData.specularLobe.z), input.tangentToWorld[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold));
#endif
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
    
#ifdef _ALPHATEST_ON
    // Used for sharpening by alpha to mask
    builtinData.alphaClipTreshold = _AlphaCutoff;
#endif

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
