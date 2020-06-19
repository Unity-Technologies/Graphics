
#ifndef GRA_HQ_CUBEMAPPING
#define GRA_HQ_CUBEMAPPING 0
#endif

#ifndef GRA_BGRA
#define GRA_BGRA 0
#endif

#ifndef GRA_64BIT_RESOLVER
#define GRA_64BIT_RESOLVER 0
#endif

#ifndef GRA_RWTEXTURE2D_SCALE
#define GRA_RWTEXTURE2D_SCALE 16
#endif

#ifndef GRA_PACK_RESOLVE_OUTPUT
#define GRA_PACK_RESOLVE_OUTPUT 1
#endif

// Temp workaround for some platforms's lack of unorm.
#ifdef GRA_NO_UNORM
	#define GRA_UNORM
#else	
	#define GRA_UNORM unorm
#endif

#define gra_Float2 float2
#define gra_Float3 float3
#define gra_Float4 float4
#define gra_Int3 int3
#define gra_Float4x4 float4x4
#define gra_Unroll [unroll]
#define gra_Branch [branch]

/**
	a cross API texture handle
*/
struct GraniteTranslationTexture
{
	SamplerState Sampler;
	Texture2D Texture;
};

struct GraniteCacheTexture
{
	SamplerState Sampler;
    Texture2DArray TextureArray;
};

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
	GraniteTilesetConstantBuffer 						tilesetBuffer;
	GraniteStreamingTextureConstantBuffer 	streamingTextureBuffer;
};

/**
		Utility struct used by the shaderlib to wrap up all required constant buffers needed to perform a Cube VT lookup/sample.
	*/
struct GraniteCubeConstantBuffers
{
	GraniteTilesetConstantBuffer 								tilesetBuffer;
	GraniteStreamingTextureCubeConstantBuffer 	streamingTextureCubeBuffer;
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

/*
	END OF PUBLIC INTERFACE
	Everything below this point should be treated as private to GraniteShaderLib.h
*/

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

#define gra_TranslationTableBias 			gra_TilesetBufferInternal[0][0]
#define gra_MaxAnisotropyLog2 				gra_TilesetBufferInternal[1][0]
#define gra_CalcMiplevelDeltaScale 		gra_Float2(gra_TilesetBufferInternal[2][0], gra_TilesetBufferInternal[3][0])
#define gra_CalcMiplevelDeltaScaleX 	gra_TilesetBufferInternal[2][0]
#define gra_CalcMiplevelDeltaScaleY 	gra_TilesetBufferInternal[3][0]
#define gra_LodBiasPow2								gra_TilesetBufferInternal[0][1]
#define gra_TrilinearOffset							gra_TilesetBufferInternal[0][2]	
#define gra_TileContentInTiles 				gra_Float2(gra_TilesetBufferInternal[0][2], gra_TilesetBufferInternal[1][2])
#define gra_Level0NumTilesX						gra_TilesetBufferInternal[0][3]
#define gra_NumTilesYScale						gra_TilesetBufferInternal[1][3]
#define gra_TextureMagic							gra_TilesetBufferInternal[2][3]
#define gra_TextureId 								gra_TilesetBufferInternal[3][3]

#define gra_RcpCacheInTiles(l)				gra_Float2(gra_TilesetCacheBuffer[0][l], gra_TilesetCacheBuffer[1][l])
#define gra_BorderPixelsRcpCache(l)		gra_Float2(gra_TilesetCacheBuffer[2][l], gra_TilesetCacheBuffer[3][l])

gra_Float4 GranitePrivate_SampleArray(in GraniteCacheTexture tex, in gra_Float3 texCoord)
{
	return tex.TextureArray.Sample(tex.Sampler, texCoord);
}

gra_Float4 GranitePrivate_SampleGradArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in gra_Float2 dX, in gra_Float2 dY)
{
	return tex.TextureArray.SampleGrad(tex.Sampler,texCoord,dX,dY);
}

gra_Float4 GranitePrivate_SampleLevelArray(in GraniteCacheTexture tex, in gra_Float3 texCoord, in float level)
{
	return tex.TextureArray.SampleLevel(tex.Sampler, texCoord, level);
}

gra_Float4 GranitePrivate_Load(in GraniteTranslationTexture tex, in gra_Int3 location)
{
	return tex.Texture.Load(location);
}

//work-around shader compiler bug
//compiler gets confused with GranitePrivate_SampleLevel taking a GraniteCacheTexture as argument when array support is disabled
//Without array support, GraniteCacheTexture and GraniteTranslationTexture are the same (but still different types!)
//compiler is confused (ERR_AMBIGUOUS_FUNCTION_CALL). Looks like somebody is over enthusiastic optimizing...
gra_Float4 GranitePrivate_SampleLevel_Translation(in GraniteTranslationTexture tex, in gra_Float2 texCoord, in float level)
{
	return tex.Texture.SampleLevel(tex.Sampler, texCoord, level);
}

float GranitePrivate_Saturate(in float value)
{
	return saturate(value);
}

uint GranitePrivate_FloatAsUint(float value)
{
	return asuint(value);
}

float GranitePrivate_Pow2(uint exponent)
{
	return pow(2.0, exponent);
}

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
		const float thresholdMaxtrix[16] = {	1.0f / 17.0f, 9.0f / 17.0f,	3.0f / 17.0f, 11.0f / 17.0f,
													13.0f / 17.0f,	5.0f / 17.0f, 15.0f / 17.0f, 7.0f / 17.0f,
													4.0f / 17.0f, 12.0f / 17.0f, 2.0f / 17.0f, 10.0f / 17.0f,
													16.0f / 17.0f, 8.0f / 17.0f, 14.0f / 17.0f,	6.0f / 17.0f};
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
	// The translation table contains uint32_t values so we have to get to the individual bits of the float data
	uint data = GranitePrivate_FloatAsUint(translationData[layer]);

	// Slice Index: 7 bits, Cache X: 10 bits, Cache Y: 10 bits, Tile Level: 4 bits
	uint slice  = (data >> 24u) & 0x7Fu;
	uint cacheX = (data >> 14u) & 0x3FFu;
	uint cacheY = (data >> 4u) & 0x3FFu;
	uint revLevel = data & 0xFu;

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
