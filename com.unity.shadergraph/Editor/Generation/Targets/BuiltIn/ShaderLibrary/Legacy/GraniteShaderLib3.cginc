//@IGNORE_BEGIN
/**
*   Graphine Granite Runtime Shader 3.0
*
*   Copyright (c) 2017 Graphine. All Rights Reserved
*
*   This shader library contains all shader functionality to sample
*   Granite tile sets. It should be included in the application-specific
*   shader code.
*
*   --------------
*   FUNCTION CALLS
*   --------------
*
*   To sample a layer from a tile set, first perform the lookup:
*
*       int Granite_Lookup[_UDIM](  in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord,
*                                   in GraniteTranslationTexture translationTable,
*                                   out GraniteLookupData graniteLookupData,
*                                   out gra_Float4 resolveResult);
*
*   It is now possible to sample from any layer in the tile set:
*
*       int Granite_Sample( in GraniteConstantBuffers grCB,
*                           in GraniteLookupData graniteLookupData,
*                           in uint layer,
*                           in GraniteCacheTexture cacheTexture,
*                           out gra_Float4 result);
*
*
*   Depending on you resolve strategy, you will need to do the following
*
*       - Separate Resolver:
*
*           Calculate only the resolve output in the separate resolver pass:
*
*               gra_Float4  Granite_ResolverPixel[_UDIM](   in GraniteConstantBuffers grCB,
*                                                           gra_Float2 inputTexCoord);
*
*           You can supply a dummy resolveResult parameter to the Granite_Lookup function when sampling.
*
*       - MRT Resolver:
*
*           Output the resolveResult parameter given by the Granite_Lookup function to the render target.
*
*       - RWTexture2D Resolver:
*
*           You can write the resolveResult parameter given by the Granite_Lookup function to a RWTexture as follows:
*
*           void Granite_DitherResolveOutput(   in gra_Float4 resolve,
*                                               in RWTexture2D<gra_Float4> resolveTexture,
*                                               in gra_Float2 screenPos,
*                                               in float alpha = 1.0f);
*
*           Don't forget to set GRA_RWTEXTURE2D_SCALE to the actual scale used!
*
*
*   To transform the texture coordinates when using atlassing use:
*
*   gra_Float4  Granite_Transform(  in GraniteStreamingTextureConstantBuffer grSTCB,
*                                   gra_Float2 inputTexCoord);
*
*   If you want to sample from a UDIM streaming texture use the Granite_Lookup_UDIM functions to perform the lookup.
*   If you want to sample with explicit derivatives, use the overloaded 'Lookup' and 'Resolve' functions that take additional ddX and ddY parameters.
*   If you want to sample with explicit level-of-detail, use the overloaded 'Lookup' and 'Resolve' functions that take an additional LOD parameter. Note that these take a special GraniteLodVTData parameter.
*   If you do not want to have texture wrapping of streaming textures when using atlassing, use the overloaded 'PreTransformed' Lookup functions. Call Granite_Transform (or transform the UVs at the CPU) yourself!
* If you want to sample a cube map, use the appropriate 'Lookup'' and 'Sample' functions.
* Pass in the complete texture coordinate ( NOT the fractional part of it) in order to avoid discontinuities!
*
*   ---------------------
*   INTEGRATION CHECKLIST
*   ---------------------
*
*   (1.) Define the following preprocessor directives to configure the shader:
*
*   define GRA_HLSL_3 1/0
*       Enable/disable HLSL 3 syntax
*       Default: disabled
*
*   define GRA_HLSL_4 1/0
*       Enable/disable HLSL 4 syntax
*       Default: disabled
*
*   define GRA_HLSL_5 1/0
*       Enable/disable HLSL 5 syntax
*       Default: disabled
*
*   define GRA_GLSL_120 1/0
*       Enable/disable GLSL version 1.2 syntax
*       Default: disabled
*
*   define GRA_GLSL_130 1/0
*       Enable/disable GLSL version 1.3 syntax
*       Default: disabled
*
*   define GRA_GLSL_330 1/0
*       Enable/disable GLSL version 3.2 syntax
*       Default: disabled
*
*   define GRA_VERTEX_SHADER 1/0
*       Define that we are compiling a vertex shader and limit the instruction set to valid instructions
*       Default: disabled
*
*   define GRA_PIXEL_SHADER 1/0
*       Define that we are compiling a pixel shader and limit the instruction set to valid instructions
*       Default: disabled
*
*   define GRA_HQ_CUBEMAPPING 1/0
*       Enable/disable high quality cubemapping
*       Default: disabled
*
*   define GRA_DEBUG 0
*       Enable/disable debug mode of shader. It recommended to set this to true when first integrating
*       Granite into your engine. It will catch some common mistakes with passing shader parameters etc.
*       Default: disabled
*
*   define GRA_DEBUG_TILES 1/0
*       Enable/disable visual debug output of tiles
*       Default: disabled
*
*   define GRA_BGRA 1/0
*       Enable shader output in BGRA format (else RGBA is used)
*       Default: disabled (rgba)
*
*   define GRA_ROW_MAJOR 1/0
*       Set row major or colum major order of arrays
*       Default: enabled (row major)
*
*   define GRA_64BIT_RESOLVER 1/0
*       Render the resolver pass to a 64bpp resolver instead of a 32 bit per pixel format.
*       Default: disabled
*
*   define GRA_RWTEXTURE2D_SCALE [1,16]
*       The scale we are resolving at in the RWTexture2D. Must match the resolveScale when creation the RWTexture2D resolver.
*       Default: 16
*
*   define GRA_DISABLE_TEX_LOAD 1/0
*       Prefer a texture sample over a texture load (Only has effect on shader models that support the texture load/fetch instruction)
*       Default: 0
*
*   define GRA_PACK_RESOLVE_OUTPUT 1/0
*       When enabled, pack the resolve output values. If disabled, you should pack the returned resolve value using Granite_PackTileId.
*       Use this when performing multiple VT samples and dithering the resolve output.
*       Default: 1
*
*   define GRA_TEXTURE_ARRAY_SUPPORT 1/0
*       Does the graphics API / shader model supports texture arrays ?
*       Default: 1 for shader models supporting texture arrays, else 0
*
*   (2.) Include the Shader library, "GraniteShaderLib.h"
*
*   (3.) Ensure a nearest-point sampler is passed for the translation texture,
*        including the mipmap filter (e.g., D3DTEXF_POINT for D3D9, or
*        NearestMipmapNearest for CG)
*
*/
//@IGNORE_END
// KEEP THESE IGNORE HERE (DO NOT COLLAPSE)
//@IGNORE_BEGIN
#ifndef GRA_HLSL_3
#define GRA_HLSL_3 0
#endif

#ifndef GRA_HLSL_4
#define GRA_HLSL_4 0
#endif

#ifndef GRA_HLSL_5
#define GRA_HLSL_5 0
#endif

#ifndef GRA_GLSL_120
#define GRA_GLSL_120 0
#endif

#ifndef GRA_GLSL_130
#define GRA_GLSL_130 0
#endif

#ifndef GRA_GLSL_330
#define GRA_GLSL_330 0
#endif

#ifndef GRA_VERTEX_SHADER
#define GRA_VERTEX_SHADER 0
#endif

#ifndef GRA_PIXEL_SHADER
#define GRA_PIXEL_SHADER 0
#endif

#ifndef GRA_HQ_CUBEMAPPING
#define GRA_HQ_CUBEMAPPING 0
#endif

#ifndef GRA_DEBUG_TILES
#define GRA_DEBUG_TILES 0
#endif

#ifndef GRA_BGRA
#define GRA_BGRA 0
#endif

#ifndef GRA_ROW_MAJOR
#define GRA_ROW_MAJOR 1
#endif

#ifndef GRA_DEBUG
#define GRA_DEBUG 1
#endif

#ifndef GRA_64BIT_RESOLVER
#define GRA_64BIT_RESOLVER 0
#endif

#ifndef GRA_RWTEXTURE2D_SCALE
#define GRA_RWTEXTURE2D_SCALE 16
#endif

#ifndef GRA_DISABLE_TEX_LOAD
#define GRA_DISABLE_TEX_LOAD 0
#endif

#ifndef GRA_PACK_RESOLVE_OUTPUT
#define GRA_PACK_RESOLVE_OUTPUT 1
#endif

#ifndef GRA_FORCE_SM3
#define GRA_FORCE_SM3 0
#endif

// Temp workaround for PSSL's lack of unorm. Ideally there would be a whole seperate GRA_PSSL backend.
#ifdef GRA_NO_UNORM
    #define GRA_UNORM
#else
    #define GRA_UNORM unorm
#endif

#ifndef GRA_TEXTURE_ARRAY_SUPPORT
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1)
        #define GRA_TEXTURE_ARRAY_SUPPORT 1
    #else
        #define GRA_TEXTURE_ARRAY_SUPPORT 0
    #endif
#endif

#define GRA_HLSL_FAMILY ((GRA_HLSL_3 == 1) || (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1))
#define GRA_GLSL_FAMILY ((GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1))

#if GRA_HLSL_FAMILY
    #define gra_Float2 float2
    #define gra_Float3 float3
    #define gra_Float4 float4
    #define gra_Int3 int3
    #define gra_Float4x4 float4x4
    #define gra_Unroll [unroll]
    #define gra_Branch [branch]
#elif GRA_GLSL_FAMILY
    #if (GRA_VERTEX_SHADER == 0) && (GRA_PIXEL_SHADER ==0)
        #error GLSL requires knowledge of the shader stage! Neither GRA_VERTEX_SHADER or GRA_PIXEL_SHADER are defined!
    #else
        #define gra_Float2 vec2
        #define gra_Float3 vec3
        #define gra_Float4 vec4
        #define gra_Int3 ivec3
        #define gra_Float4x4 mat4
        #define gra_Unroll
        #define gra_Branch
        #if (GRA_VERTEX_SHADER == 1)
            #define ddx
            #define ddy
        #elif (GRA_PIXEL_SHADER == 1)
            #define ddx dFdx
            #define ddy dFdy
        #endif
        #define frac fract
        #define lerp mix
        /** This is not correct (http://stackoverflow.com/questions/7610631/glsl-mod-vs-hlsl-fmod) but it is for our case */
        #define fmod mod
    #endif
#else
    #error unknown shader architecture
#endif

#if (GRA_DISABLE_TEX_LOAD!=1)
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        #define GRA_LOAD_INSTR 1
    #else
        #define GRA_LOAD_INSTR 0
    #endif
#else
    #define GRA_LOAD_INSTR 0
#endif

//@IGNORE_END

// These are special values that need to be replaced by the shader stripper
//! gra_TranslationTableBias
//! gra_MaxAnisotropyLog2
//! gra_CalcMiplevelDeltaScale
//! gra_CalcMiplevelDeltaScaleX
//! gra_CalcMiplevelDeltaScaleY
//! gra_LodBiasPow2
//! gra_TileToCacheScale
//! gra_TileToCacheBias
//! gra_CacheIdxToCacheScale
//! gra_CacheDeltaScale
//! gra_Level0NumTilesX
//! gra_TextureMagic
//! gra_TextureId
//! gra_TransSwiz
//! gra_NumTilesYScale
//! gra_CutoffLevel
//! gra_MetaTiles
//! gra_NumLevels
//! gra_TileContentInTiles
//! gra_RcpCacheInTiles
//! gra_BorderPixelsRcpCache
//! TranslateCoord
//! PackTileId
//! UnpackTileId
//! CalcMiplevelAnisotropic
//! DrawDebugTiles
//! TranslateCoord
//! numPagesOnLevel
//! cacheCoord
//! deltaScale
//! sampDeltaX
//! sampDeltaY
//! lenDxSqr
//! lenDySqr
//! dMaxSqr
//! dMinSqr
//! maxLevel
//! minLevel
//! anisoLog2
//! borderTemp
//! translationData
//! virtualTilesUv
//! cache
//! ddX
//! ddY
//! faceIdx
//! resultBits
//! swiz
//! level0NumTiles
//! sourceColor
//! borderColor
//! level
//! ddxTc
//! ddyTc
//! availableMips
//! maxAvailableMip
//! lod
//! smoothLevel
//! unused_resolve
//! transform
//! textureCoord
//! resolveResult
//! GranitePrivate
//! pixelLocation
//! screenPos
//! resolveTexture
//! pixelPos
//! writePos
//! dither
//! packedTile
//! cacheOffs
//! offset
//! tileY
//! tileX
//! border
//! tex
//! scale
//! output
//! temp
//! value
//! LOD
//! graniteLookupData
//! numTilesOnLevel
//! cacheX
//! cacheY
//! contTexCoord
//! derivX
//! derivY
//! dVx
//! dVy
//! majorAxis
//! GranitePrivate_CalcMiplevelAnisotropic
//! GranitePrivate_PackTileId
//! GranitePrivate_UnpackTileId
//! GranitePrivate_TranslateCoord
//! GranitePrivate_DrawDebugTiles
//! GranitePrivate_Sample
//! GranitePrivate_SampleLevel
//! GranitePrivate_SampleGrad
//! GranitePrivate_SampleArray
//! GranitePrivate_SampleLevelArray
//! GranitePrivate_SampleGradArray
//! GranitePrivate_SampleBias
//! GranitePrivate_Saturate
//! GranitePrivate_CalculateLevelOfDetail
//! GranitePrivate_Gather
//! GranitePrivate_Load
//! GranitePrivate_FloatAsUint
//! GranitePrivate_Pow2
//! GranitePrivate_CalcMiplevelLinear
//! GranitePrivate_ResolverPixel
//! GranitePrivate_DitherResolveOutput
//! GranitePrivate_CalculateCubemapCoordinates
//! GranitePrivate_MakeResolveOutput
//! exponent
//! gra_TilesetBuffer
//! gra_TilesetBufferInternal
//! gra_TilesetCacheBuffer
//! gra_StreamingTextureCB
//! gra_StreamingTextureCubeCB
//! gra_Transform
//! gra_CubeTransform
//! gra_StreamingTextureTransform
//! gra_StreamingTextureInfo
//! grCB
//! tsCB
//! grSTCB
//! tileTexCoord
//! transforms
//! GranitePrivate_RepeatUV
//! GranitePrivate_UdimUV
//! GranitePrivate_ClampUV
//! GranitePrivate_MirrorUV
//! gra_AssetWidthRcp
//! gra_AssetHeightRcp

// These are special values that need to be defined by the shader stripper
//! d floor
//! d frac
//! d fmod
//! d dot
//! d max
//! d min
//! d log2
//! d ddx
//! d ddy
//! d pow
//! d smoothstep
//! d sqrt
//! d saturate
//! d clamp
//! d float
//! d gra_Float2
//! d gra_Float3
//! d gra_Float4
//! d gra_Float4x4
//! d int
//! d int2
//! d uint2
//! d const
//! d bool
//! d if
//! d else
//! d for
//! d translationTable
//! d cacheTexture
//! d inputTexCoord
//! d tileXY
//! d resolveOutput
//! d result
//! d texCoord
//! d return
//! d paramBlock0
//! d paramBlock1
//! d class
//! d in
//! d out
//! d inout
//! d Texture
//! d TextureArray
//! d Sampler
//! d GraniteCacheTexture
//! d GraniteTranslationTexture
//! d GraniteLookupData
//! d GraniteLODLookupData
//! d RWTexture2D
//! d tex2Dlod
//! d tex2D
//! d tex2Dbias
//! d dX
//! d dY
//! d textureCoordinates
//! d translationTableData
//! d layer
//! d cacheLevel
//! d StreamingTextureConstantBuffer
//! d StreamingTextureCubeConstantBuffer
//! d TilesetConstantBuffer
//! d GraniteConstantBuffers
//! d GraniteCubeConstantBuffers

//@IGNORE_BEGIN

/**
    a cross API texture handle
*/
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    struct GraniteTranslationTexture
    {
        SamplerState Sampler;
        Texture2D Texture;
    };
    struct GraniteCacheTexture
    {
        SamplerState Sampler;

        #if GRA_TEXTURE_ARRAY_SUPPORT
            Texture2DArray TextureArray;
        #else
            Texture2D Texture;
        #endif
    };
#elif (GRA_HLSL_3 == 1) || (GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    #define GraniteTranslationTexture sampler2D

    #if GRA_TEXTURE_ARRAY_SUPPORT
        #define GraniteCacheTexture sampler2DArray
    #else
        #define GraniteCacheTexture sampler2D
    #endif

#else
    #error unknow shader archtecture
#endif

/**
        Struct defining the constant buffer for each streaming texture.
        Use IStreamingTexture::GetConstantBuffer to fill this struct.
*/
struct GraniteStreamingTextureConstantBuffer
{
    #define _grStreamingTextureCBSize 2
    gra_Float4 data[_grStreamingTextureCBSize];
};

/**
        Struct defining the constant buffer for each cube streaming texture.
        Use multiple calls to IStreamingTexture::GetConstantBuffer this struct (one call for each face).
    */
struct GraniteStreamingTextureCubeConstantBuffer
{
    #define _grStreamingTextureCubeCBSize 6
    GraniteStreamingTextureConstantBuffer data[_grStreamingTextureCubeCBSize];
};

/**
        Struct defining the constant buffer for each tileset.
        Use ITileSet::GetConstantBuffer to fill this struct.
*/
struct GraniteTilesetConstantBuffer
{
    #define _grTilesetCBSize 2
    gra_Float4x4 data[_grTilesetCBSize];
};

/**
        Utility struct used by the shaderlib to wrap up all required constant buffers needed to perform a VT lookup/sample.
    */
struct GraniteConstantBuffers
{
    GraniteTilesetConstantBuffer                        tilesetBuffer;
    GraniteStreamingTextureConstantBuffer   streamingTextureBuffer;
};

/**
        Utility struct used by the shaderlib to wrap up all required constant buffers needed to perform a Cube VT lookup/sample.
    */
struct GraniteCubeConstantBuffers
{
    GraniteTilesetConstantBuffer                                tilesetBuffer;
    GraniteStreamingTextureCubeConstantBuffer   streamingTextureCubeBuffer;
};

/**
    The Granite lookup data for the different sampling functions.
*/

// Granite lookup data for automatic mip level selecting sampling
struct GraniteLookupData
{
    gra_Float4 translationTableData;
    gra_Float2 textureCoordinates;
    gra_Float2 dX;
    gra_Float2 dY;
};

// Granite lookup data for explicit level-of-detail sampling
struct GraniteLODLookupData
{
    gra_Float4 translationTableData;
    gra_Float2 textureCoordinates;
    float cacheLevel;
};
//@IGNORE_END

// public interface
//@IGNORE_BEGIN
/**
    The public interface of all Granite related shader calls
*/

/**
*   Transform the texture coordinates from [0...1]x[0...1] texture space in [0...1]x[0...1] tile set space.
*
*   @param grSTCB The Granite Shader Runtime streaming texture parameter block
*   @param textureCoord The texture coord that will be transformed
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float2 Granite_Transform(in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 textureCoord);


/**
    Merge two resolve output values into one.
    This can be used when you need multiple tile sets on the same geometry ( != multiple layers from one tile set).
*/
gra_Float4 Granite_MergeResolveOutputs(in gra_Float4 resolve0, in gra_Float4 resolve1, in gra_Float2 pixelLocation);


/**
    Convert the 64bit created resolver to 32bit resolver data.
    This is useful when debugging 64bit resolver resources.
    Only use in debug mode !
*/
gra_Float4 Granite_DebugPackedTileId64(in gra_Float4 PackedTile);


/**
    Convert the packed normal data to a normalized three-dimensional normal.

    @param PackedNormal The normal data sampled by Granite_Sample

    @return The normalized three-dimensional normal.
*/
gra_Float3 Granite_UnpackNormal(in gra_Float4 packedNormal);

/**
    Convert the packed normal data to a normalized three-dimensional normal.

    @param PackedNormal The normal data sampled by Granite_Sample
    @param scale scale to apply to the normal during unpacking

    @return The normalized three-dimensional normal.
*/
gra_Float3 Granite_UnpackNormal(in gra_Float4 packedNormal, float scale);

#if GRA_HLSL_FAMILY
/**
    Applies an additional resolution offset to the parameter block.
    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The additional resolution offset calculated as follows: resolutionOffsetPow2 = 2^resolutionOffset.
    @return The Granite Shader Runtime tile set parameter block transformed with the additional resolution offset.
*/
GraniteTilesetConstantBuffer Granite_ApplyResolutionOffset(in GraniteTilesetConstantBuffer INtsCB, in float resolutionOffsetPow2);

/**
    Applies an user provided maximum anisotropy to the parameter block.
    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The new anisotropy calculated as follows: maxAnisotropyLog2 = log2(maxAnisotropy).
    @return The Granite Shader Runtime tile set parameter block transformed with the new maximum anisotropy.
*/
GraniteTilesetConstantBuffer Granite_SetMaxAnisotropy(in GraniteTilesetConstantBuffer INtsCB, in float maxAnisotropyLog2);
#else
/**
    Applies an additional resolution offset to the parameter block.

    @param INtsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The additional resolution offset calculated as follows: resolutionOffsetPow2 = 2^resolutionOffset.
*/
void Granite_ApplyResolutionOffset(inout GraniteTilesetConstantBuffer tsCB, in float resolutionOffsetPow2);

/**
    Applies an user provided maximum anisotropy to the parameter block.

    @param tsCB The Granite Shader Runtime tile set parameter block
    @param resolutionOffsetPow2 The new anisotropy calculated as follows: maxAnisotropyLog2 = log2(maxAnisotropy).
*/
void Granite_SetMaxAnisotropy(inout GraniteTilesetConstantBuffer tsCB, in float maxAnisotropyLog2);
#endif
/**
    Pack the (unpacked) returned resolve output.
    Should only be used when GRA_PACK_RESOLVE_OUTPUT equals 0.

    @param paramBlock The Granite Shader Runtime parameter block
    @param unpackedTileID The TileID you wish to pack

    @return The packed tile ID
*/
gra_Float4 Granite_PackTileId(in gra_Float4 unpackedTileID);

#if (GRA_HLSL_5 == 1)
/**
    Pack the (unpacked) returned resolve output.
    Should only be used when GRA_PACK_RESOLVE_OUTPUT equals 0.

    @param resolve The resolve output
    @param resolveTexture RWTexture2D resource where the resolve output is written to
    @param screenPos The pixel coordinates of the pixel on the screen (SV_Position)
*/
void Granite_DitherResolveOutput(in gra_Float4 resolve, in RWTexture2D<GRA_UNORM gra_Float4> resolveTexture, in gra_Float2 screenPos, in float alpha /*= 1.0f*/);
#endif

/**
    Get the dimensions (in pixels) of a streaming texture

    @param grSTCB The Granite Shader Runtime streaming texture parameter block

    @return The streaming texture dimensions
*/
gra_Float2 Granite_GetTextureDimensions(in GraniteStreamingTextureConstantBuffer grSTCB);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed UDIM tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed UDIM tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed Clamped tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed Clamped tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed UDIM tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed UDIM tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a pretransformed Clamped tile set
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a pretransformed Clamped tile set with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the pretransformed input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


/**
*   Virtual texture lookup of a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                                                        in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                                                        in gra_Float3 ddX, in gra_Float3 ddY,
                                                                        out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed(  in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube(    in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed UDIM tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_UDIM( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a UDIM tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_UDIM(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a UDIM tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_UDIM(   in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup of a preTransformed Clamped tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_PreTransformed_Clamp(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a Clamped tile set with explicit level-of-detail using wrapped texture addressing (tiling)
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Clamp(   in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup of a cubemap in a Clamped tile set with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param LOD Specifies the explicit level-of-detail
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Cube_Clamp(  in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param LOD The Level of Detail to sample.
*   @param graniteLODLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param graniteLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/
int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult);

/**
*   Virtual texture lookup using wrapped texture addressing (tiling) and dynamic udim selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param translationTable GraniteTranslationTexture object that contains the translation texture
*   @param inputTexCoord the input texture coordinates
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @param LOD The Level of Detail to sample.
*   @param graniteLODLookupData the Granite lookup data
*   @param resolveResult Resolve analysis output. Supply a dummy variable if you don't need it.
*
*   @return 1 in case sampling was successful.
*/

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Samples a single layer of a tile set.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteConstantBuffers grCB,
                                        in GraniteLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set.
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteCubeConstantBuffers grCB,
                                        in GraniteLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a tile set with high quality filtering
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample_HQ(  in GraniteConstantBuffers grCB,
                                                in GraniteLookupData graniteLookupData,
                                                in GraniteCacheTexture cacheTexture, in int layer,
                                                out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set with high quality filtering
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite Lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample_HQ(  in GraniteCubeConstantBuffers grCB,
                                                in GraniteLookupData graniteLookupData,
                                                in GraniteCacheTexture cacheTexture, in int layer,
                                                out gra_Float4 result);

/**
*   Samples a single layer of a tile set with explicit level-of-detail.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param graniteLookupData The Granite lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteConstantBuffers grCB,
                                        in GraniteLODLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

/**
*   Samples a single layer of a cube tile set with explicit level-of-detail.
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param graniteLookupData The Granite lookup data ((@ref Granite_Lookup)
*   @param cacheTexture The GraniteCacheTexture object of the cache texture of the tile set layer
*   @param layer The layer index in the tile set you want to sample from
*   @param result The sampled texel
*
*   @return 1 in case sampling was successful.
*/
int Granite_Sample( in GraniteCubeConstantBuffers grCB,
                                        in GraniteLODLookupData graniteLookupData,
                                        in GraniteCacheTexture cacheTexture, in int layer,
                                        out gra_Float4 result);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return The packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the pretransformed resolver output with explicit derivatives
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);

/**
*   Calculate the resolver output using wrapping texture addressing
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit derivatives
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY);


/**
*   Calculate the resolver output of a cubemap
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord);

/**
*   Calculate the resolver output of a cubemap with explicit derivatives
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param ddX Specifies the explicit partial derivative with respect to X
*   @param ddY Specifies the explicit partial derivative with respect to Y
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY);


//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_UDIM(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the pretransformed resolver output with explicit level-of-detail
* Caller needs to either already called Granite_Transform in the shader or used [TransformTextureCoordinates](@ref Graphine::Granite::IStreamingTexture::TransformTextureCoordinates) on the vertex data.
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output using wrapping texture addressing with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @param transform The transform to apply. This can be got with [GetTransform](@ref Graphine::Granite::IStreamingTexture::GetTransform).
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD);

/**
*   Calculate the resolver output of a cubemap with explicit level-of-detail
*
*   @param grCB the Granite Shader Runtime Cube parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param LOD Specifies the explicit level-of-detail
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Cube_Clamp(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the resolver output using wrapping texture addressing with dynamic UDIM selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Dynamic_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM);

//@IGNORE_END
//@IGNORE_BEGIN

/**
*   Calculate the resolver output using wrapping texture addressing with dynamic UDIM selection
*
*   @param grCB the Granite Shader Runtime parameter block
*   @param inputTexCoord The texture coordinate that will be used to sample the texture
*   @param asUDIM Should we sample using UDIM or regular (tiled) adressing
*   @return a gra_Float4 containing the packed tile id
*/
gra_Float4 Granite_ResolverPixel_Dynamic_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM);

//@IGNORE_END


/*
    END OF PUBLIC INTERFACE
    Everything below this point should be treated as private to GraniteShaderLib.h
*/

//@INSERT_DEFINES
#define gra_TilesetBuffer grCB.tilesetBuffer
#define gra_TilesetBufferInternal  tsCB.data[0]
#define gra_TilesetCacheBuffer  tsCB.data[1]

#define gra_StreamingTextureCB grCB.streamingTextureBuffer
#define gra_StreamingTextureCubeCB grCB.streamingTextureCubeBuffer

#define gra_Transform grCB.streamingTextureBuffer.data[0]
#define gra_CubeTransform grCB.streamingTextureCubeBuffer.data

#define gra_StreamingTextureTransform grSTCB.data[0]
#define gra_StreamingTextureInfo grSTCB.data[1]

#define gra_NumLevels gra_StreamingTextureInfo.x
#define gra_AssetWidthRcp gra_StreamingTextureInfo.y
#define gra_AssetHeightRcp gra_StreamingTextureInfo.z

#if GRA_ROW_MAJOR == 1

    #define gra_TranslationTableBias            gra_TilesetBufferInternal[0][0]
    #define gra_MaxAnisotropyLog2               gra_TilesetBufferInternal[1][0]
    #define gra_CalcMiplevelDeltaScale      gra_Float2(gra_TilesetBufferInternal[2][0], gra_TilesetBufferInternal[3][0])
    #define gra_CalcMiplevelDeltaScaleX     gra_TilesetBufferInternal[2][0]
    #define gra_CalcMiplevelDeltaScaleY     gra_TilesetBufferInternal[3][0]
    #define gra_LodBiasPow2                             gra_TilesetBufferInternal[0][1]
    #define gra_TileContentInTiles              gra_Float2(gra_TilesetBufferInternal[0][2], gra_TilesetBufferInternal[1][2])
    #define gra_Level0NumTilesX                     gra_TilesetBufferInternal[0][3]
    #define gra_NumTilesYScale                      gra_TilesetBufferInternal[1][3]
    #define gra_TextureMagic                            gra_TilesetBufferInternal[2][3]
    #define gra_TextureId                               gra_TilesetBufferInternal[3][3]

    #define gra_RcpCacheInTiles(l)              gra_Float2(gra_TilesetCacheBuffer[0][l], gra_TilesetCacheBuffer[1][l])
    #define gra_BorderPixelsRcpCache(l)     gra_Float2(gra_TilesetCacheBuffer[2][l], gra_TilesetCacheBuffer[3][l])

#else

    #define gra_TranslationTableBias            gra_TilesetBufferInternal[0][0]
    #define gra_MaxAnisotropyLog2               gra_TilesetBufferInternal[0][1]
    #define gra_CalcMiplevelDeltaScale      gra_Float2(gra_TilesetBufferInternal[0][2], gra_TilesetBufferInternal[0][3])
    #define gra_CalcMiplevelDeltaScaleX     gra_TilesetBufferInternal[0][2]
    #define gra_CalcMiplevelDeltaScaleY     gra_TilesetBufferInternal[0][3]
    #define gra_LodBiasPow2                             gra_TilesetBufferInternal[1][0]
    #define gra_TileContentInTiles              gra_Float2(gra_TilesetBufferInternal[2][0], gra_TilesetBufferInternal[2][1])
    #define gra_Level0NumTilesX                     gra_TilesetBufferInternal[3][0]
    #define gra_NumTilesYScale                      gra_TilesetBufferInternal[3][1]
    #define gra_TextureMagic                            gra_TilesetBufferInternal[3][2]
    #define gra_TextureId                               gra_TilesetBufferInternal[3][3]

    #define gra_RcpCacheInTiles(l)              gra_Float2(gra_TilesetCacheBuffer[l][0], gra_TilesetCacheBuffer[l][1])
    #define gra_BorderPixelsRcpCache(l)     gra_Float2(gra_TilesetCacheBuffer[l][2], gra_TilesetCacheBuffer[l][3])

#endif

#if (GRA_GLSL_120==1)
    // Extension needed for texture2DLod
    #extension GL_ARB_shader_texture_lod : enable
    // Extensions needed fot texture2DGrad
    #extension GL_EXT_gpu_shader4 : enable
    // Extensions needed for bit manipulation
    #extension GL_ARB_shader_bit_encoding : enable
#endif


#if (GRA_TEXTURE_ARRAY_SUPPORT==1)
    gra_Float4 GranitePrivate_SampleArray(in GraniteCacheTexture tex, in gra_Float3 texCoord)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.Sample(tex.Sampler, texCoord);
    #elif (GRA_GLSL_330 == 1)
        return texture(tex, texCoord);
    #else
        #error using unsupported function
    #endif
    }

    gra_Float4 GranitePrivate_SampleGradArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in gra_Float2 dX, in gra_Float2 dY)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.SampleGrad(tex.Sampler,texCoord,dX,dY);
    #elif (GRA_GLSL_330 == 1)
        return textureGrad(tex, texCoord, dX, dY);
    #else
        #error using unsupported function
    #endif
    }

    gra_Float4 GranitePrivate_SampleLevelArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in float level)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.TextureArray.SampleLevel(tex.Sampler, texCoord, level);
    #elif (GRA_GLSL_330 == 1)
        return textureLod(tex, texCoord, level);
    #else
        #error using unsupported function
    #endif
    }
#else
    gra_Float4 GranitePrivate_Sample(in GraniteCacheTexture tex, in gra_Float2 texCoord)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.Sample(tex.Sampler,texCoord);
    #elif (GRA_HLSL_3 == 1)
        return tex2D(tex,texCoord);
    #elif (GRA_GLSL_120 == 1) || (GRA_GLSL_130 == 1)
        return texture2D(tex, texCoord);
    #elif (GRA_GLSL_330 == 1)
        return texture(tex, texCoord);
    #endif
    }

    gra_Float4 GranitePrivate_SampleLevel(in GraniteCacheTexture tex, in gra_Float2 texCoord, in float level)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.SampleLevel(tex.Sampler, texCoord, level);
    #elif (GRA_HLSL_3 == 1)
        return tex2Dlod(tex,gra_Float4(texCoord,0.0,level));
    #elif (GRA_GLSL_120 == 1)
        return texture2DLod(tex, texCoord, level);
    #elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        return textureLod(tex, texCoord, level);
    #endif
    }

    gra_Float4 GranitePrivate_SampleGrad(in GraniteCacheTexture tex, in gra_Float2 texCoord, in gra_Float2 dX, in gra_Float2 dY)
    {
    #if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
        return tex.Texture.SampleGrad(tex.Sampler,texCoord,dX,dY);
    #elif (GRA_HLSL_3 == 1)
        return tex2D(tex,texCoord,dX,dY);
    #elif (GRA_GLSL_120 == 1)
        return texture2DGrad(tex, texCoord, dX, dY);
    #elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
        return textureGrad(tex, texCoord, dX, dY);
    #endif
    }
#endif //#if (GRA_TEXTURE_ARRAY_SUPPORT==1)

#if (GRA_LOAD_INSTR==1)
gra_Float4 GranitePrivate_Load(in GraniteTranslationTexture tex, in gra_Int3 location)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return tex.Texture.Load(location);
#elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    return texelFetch(tex, location.xy, location.z);
#elif (GRA_HLSL_3 == 1) || (GRA_GLSL_120 == 1)
    #error using unsupported function
#endif
}
#endif

//work-around shader compiler bug
//compiler gets confused with GranitePrivate_SampleLevel taking a GraniteCacheTexture as argument when array support is disabled
//Without array support, GraniteCacheTexture and GraniteTranslationTexture are the same (but still different types!)
//compiler is confused (ERR_AMBIGUOUS_FUNCTION_CALL). Looks like somebody is over enthusiastic optimizing...
gra_Float4 GranitePrivate_SampleLevel_Translation(in GraniteTranslationTexture tex, in gra_Float2 texCoord, in float level)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return tex.Texture.SampleLevel(tex.Sampler, texCoord, level);
#elif (GRA_HLSL_3 == 1)
    return tex2Dlod(tex,gra_Float4(texCoord,0.0,level));
#elif (GRA_GLSL_120 == 1)
    return texture2DLod(tex, texCoord, level);
#elif (GRA_GLSL_130 == 1) || (GRA_GLSL_330 == 1)
    return textureLod(tex, texCoord, level);
#endif
}

float GranitePrivate_Saturate(in float value)
{
#if GRA_HLSL_FAMILY
    return saturate(value);
#elif GRA_GLSL_FAMILY
    return clamp(value, 0.0f, 1.0f);
#endif
}

#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1)
uint GranitePrivate_FloatAsUint(float value)
{
#if (GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1)
    return asuint(value);
#elif (GRA_GLSL_330 == 1)
    return floatBitsToUint(value);
#endif
}
#endif

// IOS gl shader compiler doesn't like overloads with int and uint. It thinks they are the same giving the error
// redefinition of 'GranitePrivate_Pow2'.
// So we only declare the one we exactly need for the given platform.
#if (GRA_FORCE_SM3 == 0) && ((GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1))
float GranitePrivate_Pow2(uint exponent)
{
#if GRA_HLSL_FAMILY
    return pow(2.0, exponent);
#else
    return pow(2.0, float(exponent));
#endif
}
#else
float GranitePrivate_Pow2(int exponent)
{
#if GRA_HLSL_FAMILY
    return pow(2.0, exponent);
#else
    return pow(2.0, float(exponent));
#endif
}
#endif

gra_Float2 GranitePrivate_RepeatUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return frac(uv);
}

gra_Float2 GranitePrivate_UdimUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return uv;
}

gra_Float2 GranitePrivate_ClampUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
  gra_Float2 epsilon2 = gra_Float2(gra_AssetWidthRcp, gra_AssetHeightRcp);
  return clamp(uv, epsilon2, gra_Float2(1,1) - epsilon2);
}

gra_Float2 GranitePrivate_MirrorUV(in gra_Float2 uv, in GraniteStreamingTextureConstantBuffer grSTCB)
{
    gra_Float2 t = frac(uv*0.5)*2.0;
    gra_Float2 l = gra_Float2(1.0,1.0);
    return l-abs(t-l);
}

// function definitons for private functions
gra_Float4 GranitePrivate_PackTileId(in gra_Float2 tileXY, in float level, in float textureID);

gra_Float4 Granite_DebugPackedTileId64(in gra_Float4 PackedTile)
{
#if GRA_64BIT_RESOLVER
    gra_Float4 output;

    const float scale = 1.0f / 65535.0f;
    gra_Float4 temp = PackedTile / scale;

    output.x = fmod(temp.x, 256.0f);
    output.y = floor(temp.x / 256.0f) + fmod(temp.y, 16.0f) * 16.0f;
    output.z = floor(temp.y / 16.0f);
    output.w = temp.z + temp.a * 16.0f;

    return gra_Float4
    (
        (float)output.x / 255.0f,
        (float)output.y / 255.0f,
        (float)output.z / 255.0f,
        (float)output.w / 255.0f
    );
#else
    return PackedTile;
#endif
}

gra_Float3 Granite_UnpackNormal(in gra_Float4 PackedNormal, float scale)
{
    gra_Float2 reconstructed = gra_Float2(PackedNormal.x * PackedNormal.a, PackedNormal.y) * 2.0f - 1.0f;
    reconstructed *= scale;
    float z = sqrt(1.0f - GranitePrivate_Saturate(dot(reconstructed, reconstructed)));
    return gra_Float3(reconstructed, z);
}

gra_Float3 Granite_UnpackNormal(in gra_Float4 PackedNormal)
{
    return Granite_UnpackNormal(PackedNormal, 1.0);
}

#if GRA_HLSL_FAMILY
GraniteTilesetConstantBuffer Granite_ApplyResolutionOffset(in GraniteTilesetConstantBuffer INtsCB, in float resolutionOffsetPow2)
{
    GraniteTilesetConstantBuffer tsCB = INtsCB;
    gra_LodBiasPow2 *= resolutionOffsetPow2;
    //resolutionOffsetPow2 *= resolutionOffsetPow2; //Square it before multiplying it in below
    gra_CalcMiplevelDeltaScaleX *= resolutionOffsetPow2;
    gra_CalcMiplevelDeltaScaleY *= resolutionOffsetPow2;
    return tsCB;
}

GraniteTilesetConstantBuffer Granite_SetMaxAnisotropy(in GraniteTilesetConstantBuffer INtsCB, in float maxAnisotropyLog2)
{
    GraniteTilesetConstantBuffer tsCB = INtsCB;
    gra_MaxAnisotropyLog2 = min(gra_MaxAnisotropyLog2, maxAnisotropyLog2);
    return tsCB;
}
#else
void Granite_ApplyResolutionOffset(inout GraniteTilesetConstantBuffer tsCB, in float resolutionOffsetPow2)
{
    gra_LodBiasPow2 *= resolutionOffsetPow2;
    //resolutionOffsetPow2 *= resolutionOffsetPow2; //Square it before multiplying it in below
    gra_CalcMiplevelDeltaScaleX *= resolutionOffsetPow2;
    gra_CalcMiplevelDeltaScaleY *= resolutionOffsetPow2;
}

void Granite_SetMaxAnisotropy(inout GraniteTilesetConstantBuffer tsCB, in float maxAnisotropyLog2)
{
    gra_MaxAnisotropyLog2 = min(gra_MaxAnisotropyLog2, maxAnisotropyLog2);
}
#endif

gra_Float2 Granite_Transform(in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 textureCoord)
{
    return textureCoord * gra_StreamingTextureTransform.zw  + gra_StreamingTextureTransform.xy;
}

gra_Float4 Granite_MergeResolveOutputs(in gra_Float4 resolve0, in gra_Float4 resolve1, in gra_Float2 pixelLocation)
{
    gra_Float2 screenPos = frac(pixelLocation * 0.5f);
    bool dither = (screenPos.x != screenPos.y);
    return (dither) ? resolve0 : resolve1;
}

gra_Float4 Granite_PackTileId(in gra_Float4 unpackedTileID)
{
    return GranitePrivate_PackTileId(unpackedTileID.xy, unpackedTileID.z, unpackedTileID.w);
}

#if (GRA_HLSL_5 == 1)
void Granite_DitherResolveOutput(in gra_Float4 resolve, in RWTexture2D<GRA_UNORM gra_Float4> resolveTexture, in gra_Float2 screenPos, in float alpha)
{
    const uint2 pixelPos = int2(screenPos);
    const uint2 pixelLocation = pixelPos % GRA_RWTEXTURE2D_SCALE;
    bool dither = (pixelLocation.x  == 0) && (pixelLocation.y  == 0);
    uint2 writePos = pixelPos / GRA_RWTEXTURE2D_SCALE;

    if ( alpha == 0 )
    {
        dither = false;
    }
    else if (alpha != 1.0)
    {
        // Do a 4x4 dither patern so alternating pixels resolve to the first or the second texture
        gra_Float2 pixelLocationAlpha = frac(screenPos * 0.25f); // We don't scale after the frac so this will give coords 0, 0.25, 0.5, 0.75
        int pixelId = (int)(pixelLocationAlpha.y * 16 + pixelLocationAlpha.x * 4); //faster as a dot2 ?

        // Clamp
        // This ensures that for example alpha=0.95 still resolves some tiles of the surfaces behind it
        // and alpha=0.05 still resolves some tiles of this surface
        alpha = min(max(alpha, 0.0625), 0.9375);

        // Modern hardware supports array indexing with per pixel varying indexes
        // on old hardware this will be expanded to a conditional tree by the compiler
        const float thresholdMaxtrix[16] = {    1.0f / 17.0f, 9.0f / 17.0f, 3.0f / 17.0f, 11.0f / 17.0f,
                                                    13.0f / 17.0f,  5.0f / 17.0f, 15.0f / 17.0f, 7.0f / 17.0f,
                                                    4.0f / 17.0f, 12.0f / 17.0f, 2.0f / 17.0f, 10.0f / 17.0f,
                                                    16.0f / 17.0f, 8.0f / 17.0f, 14.0f / 17.0f, 6.0f / 17.0f};
        float threshold = thresholdMaxtrix[pixelId];

        if (alpha < threshold)
        {
            dither = false;
        }
    }

    gra_Branch if (dither)
    {
#if (GRA_PACK_RESOLVE_OUTPUT==0)
        resolveTexture[writePos] = Granite_PackTileId(resolve);
#else
        resolveTexture[writePos] = resolve;
#endif
    }
}
#endif

float GranitePrivate_CalcMiplevelAnisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 ddxTc, in gra_Float2 ddyTc)
{
    // Calculate the required mipmap level, this uses a similar
    // formula as the GL spec.
    // To reduce sqrt's and log2's we do some stuff in squared space here and further below in log space
    // i.e. we wait with the sqrt untill we can do it for 'free' later during the log2

  ddxTc *= gra_CalcMiplevelDeltaScale;
  ddyTc *= gra_CalcMiplevelDeltaScale;

  float lenDxSqr = dot(ddxTc, ddxTc);
    float lenDySqr = dot(ddyTc, ddyTc);
    float dMaxSqr = max(lenDxSqr, lenDySqr);
    float dMinSqr = min(lenDxSqr, lenDySqr);

    // Calculate mipmap levels directly from sqared distances. This uses log2(sqrt(x)) = 0.5 * log2(x) to save some sqrt's
    float maxLevel = 0.5 * log2( dMaxSqr );
    float minLevel = 0.5 * log2( dMinSqr );

    // Calculate the log2 of the anisotropy and clamp it by the max supported. This uses log2(a/b) = log2(a)-log2(b) and min(log(a),log(b)) = log(min(a,b))
    float anisoLog2 = maxLevel - minLevel;
    anisoLog2 = min( anisoLog2, gra_MaxAnisotropyLog2 );

    // Adjust for anisotropy & clamp to level 0
    float result = max(maxLevel - anisoLog2 - 0.5f, 0.0f); //Subtract 0.5 to compensate for trilinear mipmapping

    // Added clamping to avoid "hot pink" on small tilesets that try to sample past the 1x1 tile miplevel
    // This happens if you for example import a relatively small texture and zoom out
    return min(result, gra_NumLevels);
}

float GranitePrivate_CalcMiplevelLinear(in  GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB, in gra_Float2 ddxTc, in gra_Float2 ddyTc)
{
    // Calculate the required mipmap level, this uses a similar
    // formula as the GL spec.
    // To reduce sqrt's and log2's we do some stuff in squared space here and further below in log space
    // i.e. we wait with the sqrt untill we can do it for 'free' later during the log2

  ddxTc *= gra_CalcMiplevelDeltaScale;
  ddyTc *= gra_CalcMiplevelDeltaScale;

    float lenDxSqr = dot(ddxTc, ddxTc);
    float lenDySqr = dot(ddyTc, ddyTc);
    float dMaxSqr = max(lenDxSqr, lenDySqr);

    // Calculate mipmap levels directly from squared distances. This uses log2(sqrt(x)) = 0.5 * log2(x) to save some sqrt's
    float maxLevel = 0.5 * log2(dMaxSqr) - 0.5f;  //Subtract 0.5 to compensate for trilinear mipmapping

    return clamp(maxLevel, 0.0f, gra_NumLevels);
}

gra_Float4 GranitePrivate_PackTileId(in gra_Float2 tileXY, in float level, in float textureID)
{
#if GRA_64BIT_RESOLVER == 0
    gra_Float4 resultBits;

    resultBits.x = fmod(tileXY.x, 256.0f);
    resultBits.y = floor(tileXY.x / 256.0f) + fmod(tileXY.y, 32.0f) * 8.0f;
    resultBits.z = floor(tileXY.y / 32.0f) + fmod(level, 4.0f) * 64.0f;
    resultBits.w = floor(level / 4.0f) + textureID * 4.0f;

    const float scale = 1.0f / 255.0f;

#if GRA_BGRA == 0
    return scale * gra_Float4
    (
        float(resultBits.x),
        float(resultBits.y),
        float(resultBits.z),
        float(resultBits.w)
    );
#else
    return scale * gra_Float4
    (
        float(resultBits.z),
        float(resultBits.y),
        float(resultBits.x),
        float(resultBits.w)
    );
#endif
#else
    const float scale = 1.0f / 65535.0f;
    return gra_Float4(tileXY.x, tileXY.y, level, textureID) * scale;
#endif

}

gra_Float4 GranitePrivate_UnpackTileId(in gra_Float4 packedTile)
{
    gra_Float4 swiz;
#if GRA_BGRA == 0
    swiz = packedTile;
#else
    swiz = packedTile.zyxw;
#endif
    swiz *= 255.0f;

    float tileX = swiz.x + fmod(swiz.y, 16.0f) * 256.0f;
    float tileY = floor(swiz.y / 16.0f) + swiz.z * 16.0f;
    float level = fmod(swiz.w, 16.0f);
    float tex   = floor(swiz.w /  16.0f);

    return gra_Float4(tileX, tileY, level, tex);
}

gra_Float3 GranitePrivate_TranslateCoord(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 inputTexCoord, in gra_Float4 translationData, in int layer, out gra_Float2 numPagesOnLevel)
{
#if (GRA_FORCE_SM3 == 0) && ((GRA_HLSL_5 == 1) || (GRA_HLSL_4 == 1) || (GRA_GLSL_330 == 1))
    // The translation table contains uint32_t values so we have to get to the individual bits of the float data
    uint data = GranitePrivate_FloatAsUint(translationData[layer]);

    // Slice Index: 7 bits, Cache X: 10 bits, Cache Y: 10 bits, Tile Level: 4 bits
    uint slice  = (data >> 24u) & 0x7Fu;
    uint cacheX = (data >> 14u) & 0x3FFu;
    uint cacheY = (data >> 4u) & 0x3FFu;
    uint revLevel = data & 0xFu;
#else
    // The translation table contains integer float values so we have to cast the float value to an int (which works up to 24-bit integer values)
    int data = int(translationData[layer]);

    int slice  = 0;
    int cacheX = (data / 16384);
    int cacheY = (data % 16384) / 16;
    int revLevel = (data % 16);
#endif

    gra_Float2 numTilesOnLevel;
    numTilesOnLevel.x = GranitePrivate_Pow2(revLevel);
    numTilesOnLevel.y = numTilesOnLevel.x * gra_NumTilesYScale;

    gra_Float2 tileTexCoord = frac(inputTexCoord * numTilesOnLevel);

    gra_Float2 tileTexCoordCache = tileTexCoord * gra_TileContentInTiles + gra_Float2(cacheX, cacheY);
    gra_Float3 final = gra_Float3(tileTexCoordCache * gra_RcpCacheInTiles(layer) + gra_BorderPixelsRcpCache(layer), slice);

    numPagesOnLevel = numTilesOnLevel * gra_TileContentInTiles * gra_RcpCacheInTiles(layer);

    return final;
}

gra_Float4 GranitePrivate_DrawDebugTiles(in gra_Float4 sourceColor, in gra_Float2 textureCoord, in gra_Float2 numPagesOnLevel)
{
    // Calculate the border values
    gra_Float2 cacheOffs = frac(textureCoord * numPagesOnLevel);
    float borderTemp = max(cacheOffs.x, 1.0-cacheOffs.x);
    borderTemp = max(max(cacheOffs.y, 1.0-cacheOffs.y), borderTemp);
    float border = smoothstep(0.98, 0.99, borderTemp);

    // White
    gra_Float4 borderColor = gra_Float4(1,1,1,1);

    //Lerp it over the source color
    return lerp(sourceColor, borderColor, border);
}

gra_Float4 GranitePrivate_MakeResolveOutput(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 tileXY, in float level)
{
#if GRA_PACK_RESOLVE_OUTPUT
    return GranitePrivate_PackTileId(tileXY, level, gra_TextureId);
#else
    return gra_Float4(tileXY, level, gra_TextureId);
#endif
}

gra_Float4 GranitePrivate_ResolverPixel(in GraniteTilesetConstantBuffer tsCB, in gra_Float2 inputTexCoord, in float LOD)
{
    float level = floor(LOD + 0.5f);

    // Number of tiles on level zero
    gra_Float2 level0NumTiles;
    level0NumTiles.x = gra_Level0NumTilesX;
    level0NumTiles.y = gra_Level0NumTilesX * gra_NumTilesYScale;

    // Calculate xy of the tiles to load
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));

    return GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);
}

void GranitePrivate_CalculateCubemapCoordinates(in gra_Float3 inputTexCoord, in gra_Float3 dVx, in gra_Float3 dVy, in GraniteStreamingTextureCubeConstantBuffer transforms, out int faceIdx, out gra_Float2 texCoord, out gra_Float2 dX, out gra_Float2 dY)
{
    gra_Float2 contTexCoord;
    gra_Float3 derivX;
    gra_Float3 derivY;

    float majorAxis;
    if (abs(inputTexCoord.z) >= abs(inputTexCoord.x) && abs(inputTexCoord.z) >= abs(inputTexCoord.y))
    {
        // Z major axis
        if(inputTexCoord.z < 0.0)
        {
            faceIdx = 5;
            texCoord.x = -inputTexCoord.x;
        }
        else
        {
            faceIdx = 4;
            texCoord.x = inputTexCoord.x;
        }
        texCoord.y = -inputTexCoord.y;
        majorAxis = inputTexCoord.z;

        contTexCoord = gra_Float2(inputTexCoord.x, inputTexCoord.y);
        derivX = gra_Float3(dVx.x, dVx.y, dVx.z);
        derivY = gra_Float3(dVy.x, dVy.y, dVy.z);
    }
    else if (abs(inputTexCoord.y) >= abs(inputTexCoord.x))
    {
        // Y major axis
        if(inputTexCoord.y < 0.0)
        {
            faceIdx = 3;
            texCoord.y = -inputTexCoord.z;
        }
        else
        {
            faceIdx = 2;
            texCoord.y = inputTexCoord.z;
        }
        texCoord.x = inputTexCoord.x;
        majorAxis = inputTexCoord.y;

        contTexCoord = gra_Float2(inputTexCoord.x, inputTexCoord.z);
        derivX = gra_Float3(dVx.x, dVx.z, dVx.y);
        derivY = gra_Float3(dVy.x, dVy.z, dVy.y);
    }
    else
    {
        // X major axis
        if(inputTexCoord.x < 0.0)
        {
            faceIdx = 1;
            texCoord.x = inputTexCoord.z;
        }
        else
        {
            faceIdx = 0;
            texCoord.x = -inputTexCoord.z;
        }
        texCoord.y = -inputTexCoord.y;
        majorAxis = inputTexCoord.x;

        contTexCoord = gra_Float2(inputTexCoord.z, inputTexCoord.y);
        derivX = gra_Float3(dVx.z, dVx.y, dVx.x);
        derivY = gra_Float3(dVy.z, dVy.y, dVy.x);
    }
    texCoord = (texCoord + majorAxis) / (2.0 * abs(majorAxis));

#if GRA_HQ_CUBEMAPPING
    dX = /*contTexCoord **/ ((contTexCoord + derivX.xy) / ( 2.0 * (majorAxis + derivX.z)) - (contTexCoord / (2.0 * majorAxis)));
    dY = /*contTexCoord **/ ((contTexCoord + derivY.xy) / ( 2.0 * (majorAxis + derivY.z)) - (contTexCoord / (2.0 * majorAxis)));
#else
    dX = ((/*contTexCoord **/ derivX.xy) / (2.0 * abs(majorAxis)));
    dY = ((/*contTexCoord **/ derivY.xy) / (2.0 * abs(majorAxis)));
#endif

    // Now scale the derivatives with the texture transform scale
    dX *= transforms.data[faceIdx].data[0].zw;
    dY *= transforms.data[faceIdx].data[0].zw;
}

// Auto-level
void GranitePrivate_CalculateCubemapCoordinates(in gra_Float3 inputTexCoord, in GraniteStreamingTextureCubeConstantBuffer transforms, out int faceIdx, out gra_Float2 texCoord, out gra_Float2 dX, out gra_Float2 dY)
{
    gra_Float3 dVx = ddx(inputTexCoord);
    gra_Float3 dVy = ddy(inputTexCoord);

    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, dVx, dVy, transforms, faceIdx, texCoord, dX, dY);
}

gra_Float2 Granite_GetTextureDimensions(in GraniteStreamingTextureConstantBuffer grSTCB)
{
    return gra_Float2(1.0 / gra_AssetWidthRcp, 1.0 / gra_AssetHeightRcp); //TODO(ddebaets) use HLSL rcp here
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Linear
//! GranitePrivate_Lookup_Software_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_Linear

// General
int GranitePrivate_Lookup_Software_General_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Linear(   in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Linear(  in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Linear( in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM_Linear
//! GranitePrivate_Lookup_Software_UDIM_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear

// General
int GranitePrivate_Lookup_Software_General_UDIM_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM_Linear(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM_Linear( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_UDIM_Linear(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp_Linear
//! GranitePrivate_Lookup_Software_Clamp_Linear
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear

// General
int GranitePrivate_Lookup_Software_General_Clamp_Linear(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelLinear(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp_Linear( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Linear(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp_Linear(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Linear(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Clamp_Linear(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Linear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Anisotropic
//! GranitePrivate_Lookup_Software_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Anisotropic(  in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Anisotropic( in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Anisotropic(    in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM_Anisotropic
//! GranitePrivate_Lookup_Software_UDIM_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM_Anisotropic( in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM_Anisotropic(    in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_UDIM_Anisotropic(   in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp_Anisotropic
//! GranitePrivate_Lookup_Software_Clamp_Anisotropic
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic

// General
int GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                            in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable,
                                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult,
                                                                                                            in gra_Float2 dX, in gra_Float2 dY)
{
    float smoothLevel = GranitePrivate_CalcMiplevelAnisotropic(tsCB, grSTCB, dX, dY);
    float level = floor(smoothLevel + 0.5f);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, smoothLevel);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.dX = dX;
    graniteLookupData.dY = dY;

    return 1;
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}
//
//  Auto level
//

// pretransformed

// Tiled
int GranitePrivate_Lookup_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int GranitePrivate_Lookup_Software_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY, in GraniteTranslationTexture translationTable, out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    // Scale the derivatives with the texture transform scale
    ddX *= gra_Transform.zw;
    ddY *= gra_Transform.zw;

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, ddX, ddY);
}


// pretransformed
int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp_Anisotropic(    in GraniteConstantBuffers grCB,
                                                                                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                                                                            out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


// Tiled
int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Anisotropic(grCB, inputTexCoord, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp_Anisotropic(   in GraniteConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY,
                                out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp_Anisotropic(grCB, inputTexCoord, ddX, ddY, translationTable, graniteLookupData, resolveResult);
}


//**
//* GranitePrivate cubemap sampling
//**

int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Cube_Clamp_Anisotropic(  in GraniteCubeConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp_Anisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General
//! GranitePrivate_Lookup_Software
//! GranitePrivate_Lookup_PreTransformed_Software


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General( in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed(  in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube(    in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_UDIM
//! GranitePrivate_Lookup_Software_UDIM
//! GranitePrivate_Lookup_PreTransformed_Software_UDIM


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General_UDIM(    in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_UDIM( in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_UDIM(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_UDIM(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_UDIM(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube_UDIM(   in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_UDIM(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// These are special values that need to be replaced by the shader stripper
//! GranitePrivate_Lookup_Software_General_Clamp
//! GranitePrivate_Lookup_Software_Clamp
//! GranitePrivate_Lookup_PreTransformed_Software_Clamp


//**
//* GranitePrivate software sampling lod
//**

//
//  Explicit level
//

// General
int GranitePrivate_Lookup_Software_General_Clamp(   in GraniteTilesetConstantBuffer tsCB, in GraniteStreamingTextureConstantBuffer grSTCB,
                                                                                                    in gra_Float2 inputTexCoord, in float LOD, in GraniteTranslationTexture translationTable,
                                                                                                    out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Clamp to the highest mip available in the streaming texture
    LOD = clamp(LOD, 0.0f, gra_NumLevels);

    float level = floor(LOD);

    // calculate resolver data
    gra_Float2 level0NumTiles = gra_Float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
    gra_Float2 virtualTilesUv = floor(inputTexCoord * level0NumTiles * pow(0.5, level));
    resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, level);

    // Look up the physical page indexes and the number of pages on the mipmap
    // level of the page in the translation texture
    // Note: this is equal for both anisotropic and linear sampling
    // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
    gra_Float4 cache = GranitePrivate_SampleLevel_Translation(translationTable, inputTexCoord, LOD);
#else
    gra_Float4 cache = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, level));
#endif

    // Do a simple cache sample with trilinear filtering
    float cacheLevel = frac(LOD);

    graniteLookupData.translationTableData = cache;
    graniteLookupData.textureCoordinates = inputTexCoord;
    graniteLookupData.cacheLevel = cacheLevel;

    return 1;
}

// PreTransformed
int GranitePrivate_Lookup_PreTransformed_Software_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

// regular
int GranitePrivate_Lookup_Software_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, float LOD, in GraniteTranslationTexture translationTable, out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_PreTransformed_Clamp(    in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_PreTransformed_Software_Clamp(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Clamp(   in GraniteConstantBuffers grCB,
                            in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord, in float LOD,
                            out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    return GranitePrivate_Lookup_Software_Clamp(grCB, inputTexCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Cube_Clamp(  in GraniteCubeConstantBuffers grCB,
                                in GraniteTranslationTexture translationTable, in gra_Float3 inputTexCoord, in float LOD,
                                out GraniteLODLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_Lookup_Software_General_Clamp(gra_TilesetBuffer, gra_CubeTransform[faceIdx], texCoord, LOD, translationTable, graniteLookupData, resolveResult);
}

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General_Linear(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Dynamic_Linear(  in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLODLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLODLookupData, resolveResult);
}

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM,
                                    out GraniteLookupData graniteLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General_Anisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, translationTable, graniteLookupData, resolveResult, dX, dY);
}

int Granite_Lookup_Dynamic_Anisotropic( in GraniteConstantBuffers grCB,
                                    in GraniteTranslationTexture translationTable, in gra_Float2 inputTexCoord,
                                    in bool asUDIM, in float LOD,
                                    out GraniteLODLookupData graniteLODLookupData, out gra_Float4 resolveResult)
{
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    return GranitePrivate_Lookup_Software_General(gra_TilesetBuffer, gra_StreamingTextureCB, inputTexCoord, LOD, translationTable, graniteLODLookupData, resolveResult);
}


int Granite_Sample_Internal(in GraniteTilesetConstantBuffer tsCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    // Convert from pixels to [0-1] and look up in the physical page texture
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

    // This leads to small artefacts at tile borders but is generally not noticable unless the texture
    // is greatly magnified
#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleArray(cacheTexture, cacheCoord);
#else
    result = GranitePrivate_Sample(cacheTexture, cacheCoord.xy);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, graniteLookupData.textureCoordinates, numPagesOnLevel).xyz;
#endif

    return 1;
}

int Granite_Sample_HQ_Internal(in GraniteTilesetConstantBuffer tsCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    // Convert from pixels to [0-1] and look up in the physical page texture
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

    deltaScale *= gra_LodBiasPow2;

    // Calculate the delta scale this works by first converting the [0-1] texcoord deltas to
    // pixel deltas on the current mip level, then dividing by the cache size to convert to [0-1] cache deltas
    gra_Float2 sampDeltaX = graniteLookupData.dX*deltaScale;
    gra_Float2 sampDeltaY = graniteLookupData.dY*deltaScale;

#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleGradArray(cacheTexture, cacheCoord, sampDeltaX, sampDeltaY);
#else
    result = GranitePrivate_SampleGrad(cacheTexture, cacheCoord.xy, sampDeltaX, sampDeltaY);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, graniteLookupData.textureCoordinates, numPagesOnLevel).xyz;
#endif

    return 1;
}

int Granite_Sample_Interal(in GraniteTilesetConstantBuffer tsCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
    gra_Float2 deltaScale;
    gra_Float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

#if GRA_TEXTURE_ARRAY_SUPPORT
    result = GranitePrivate_SampleLevelArray(cacheTexture, cacheCoord, graniteLookupData.cacheLevel);
#else
    result = GranitePrivate_SampleLevel(cacheTexture, cacheCoord.xy, graniteLookupData.cacheLevel);
#endif

#if GRA_DEBUG == 1
    if ( gra_TextureMagic != 2202.0f )
    {
        result = gra_Float4(1,0,0,1);
    }
#endif

#if GRA_DEBUG_TILES == 1
    //result.xyz = GranitePrivate_DrawDebugTiles(result, inputTexCoord, numPagesOnLevel).xyz;
#endif

    return 1;
}

// LQ
int Granite_Sample(in GraniteConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample(in GraniteCubeConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}

// HQ
int Granite_Sample_HQ(in GraniteConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_HQ_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample_HQ(in GraniteCubeConstantBuffers grCB, in GraniteLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_HQ_Internal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}

// LOD
int Granite_Sample(in GraniteConstantBuffers grCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Interal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}
int Granite_Sample(in GraniteCubeConstantBuffers grCB, in GraniteLODLookupData graniteLookupData, in GraniteCacheTexture cacheTexture, in int layer, out gra_Float4 result)
{
        return Granite_Sample_Interal(gra_TilesetBuffer, graniteLookupData, cacheTexture, layer, result);
}



//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_UDIM_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Clamp_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Linear(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_UDIM_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_UDIM_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = ddx(inputTexCoord);
    gra_Float2 dY = ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//
// Tiled
//

// Auto level
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Clamp_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in gra_Float2 ddX, in gra_Float2 ddY)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, ddX, ddY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}

//**
//* Granite resolver for cubemaps
//**

// Auto level
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}

// Explicit derivatives
gra_Float4 Granite_ResolverPixel_Cube_Clamp_Anisotropic(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in gra_Float3 ddX, in gra_Float3 ddY)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, ddX, ddY, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_CubeTransform[faceIdx], dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, level);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_RepeatUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_RepeatUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_UDIM(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_UdimUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube_UDIM(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_UdimUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


//**
//* Granite resolver implementation
//**

//
// Non-tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_PreTransformed_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB);

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//
// Tiled
//

// Explicit level
gra_Float4 Granite_ResolverPixel_Clamp(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in float LOD)
{
    // Always in 0-1 range
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, GranitePrivate_ClampUV(inputTexCoord, gra_StreamingTextureCB));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, LOD);
}

//**
//* Granite resolver for cubemaps
//**

// Explicit level
gra_Float4 Granite_ResolverPixel_Cube_Clamp(in GraniteCubeConstantBuffers grCB, in gra_Float3 inputTexCoord, in float LOD)
{
    int faceIdx;
    gra_Float2 texCoord;
    gra_Float2 dX;
    gra_Float2 dY;
    GranitePrivate_CalculateCubemapCoordinates(inputTexCoord, gra_StreamingTextureCubeCB, faceIdx, texCoord, dX, dY);

    // Always in 0-1 range
    texCoord = Granite_Transform(gra_CubeTransform[faceIdx], GranitePrivate_ClampUV(texCoord, gra_CubeTransform[faceIdx]));

    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, texCoord, LOD);
}


gra_Float4 Granite_ResolverPixel_Dynamic_Linear(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    float level = GranitePrivate_CalcMiplevelLinear(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}


gra_Float4 Granite_ResolverPixel_Dynamic_Anisotropic(in GraniteConstantBuffers grCB, in gra_Float2 inputTexCoord, in bool asUDIM)
{
    // Calculate texcoord deltas (do this before the frac to avoid any discontinuities)
    gra_Float2 dX = gra_Transform.zw * ddx(inputTexCoord);
    gra_Float2 dY = gra_Transform.zw * ddy(inputTexCoord);

    // Always in 0-1 range
    gra_Float2 actualUV = asUDIM ? inputTexCoord : frac(inputTexCoord);
    inputTexCoord = Granite_Transform(gra_StreamingTextureCB, actualUV);

    float level = GranitePrivate_CalcMiplevelAnisotropic(gra_TilesetBuffer, gra_StreamingTextureCB, dX, dY);
    return GranitePrivate_ResolverPixel(gra_TilesetBuffer, inputTexCoord, level);
}
