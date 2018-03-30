#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

DecalData FetchDecal(uint start, uint i)
{
#ifdef LIGHTLOOP_TILE_PASS
    int j = FetchIndex(start, i);
#else
    int j = start + i;
#endif
    return _DecalDatas[j];
}

// Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
// we need to find a way to calculate mips. For now just fetch first mip of the decals
void ApplyBlendNormal(inout float4 dst, inout int matMask, float2 texCoords, int sliceIndex, int mapMask, float3x3 decalToWorld, float blend, float lod)
{
	float4 src;
	src.xyz =  mul(decalToWorld, UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_ARRAY_LOD(_DecalAtlas, sampler_DecalAtlas, texCoords, sliceIndex, lod))) * 0.5f + 0.5f;
	src.w = blend;
	dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
	dst.w = dst.w * (1.0f - src.w);
	matMask |= mapMask;
}

void ApplyBlendDiffuse(inout float4 dst, inout int matMask, float2 texCoords, int sliceIndex, int mapMask, inout float blend, float lod)
{
	float4 src = SAMPLE_TEXTURE2D_ARRAY_LOD(_DecalAtlas, sampler_DecalAtlas, texCoords, sliceIndex, lod);
	src.w *= blend;
	blend = src.w;	// diffuse texture alpha affects all other channels
	dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
	dst.w = dst.w * (1.0f - src.w);
	matMask |= mapMask;
}

void ApplyBlendMask(inout float4 dst, inout int matMask, float2 texCoords, int sliceIndex, int mapMask, float blend, float lod)
{
	float4 src = SAMPLE_TEXTURE2D_ARRAY_LOD(_DecalAtlas, sampler_DecalAtlas, texCoords, sliceIndex, lod);
	src.z = src.w;
	src.w = blend;
	dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
	dst.w = dst.w * (1.0f - src.w);
	matMask |= mapMask;
}

void AddDecalContribution(PositionInputs posInput, inout SurfaceData surfaceData, inout float alpha)
{
	if(_EnableDBuffer)
	{
		DecalSurfaceData decalSurfaceData;
		int mask = 0;
		// the code in the macros, gets moved inside the conditionals by the compiler
		FETCH_DBUFFER(DBuffer, _DBufferTexture, posInput.positionSS);

#ifdef _SURFACE_TYPE_TRANSPARENT	// forward transparent using clustered decals
        uint decalCount, decalStart;
		DBuffer0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
		DBuffer1 = float4(0.5f, 0.5f, 0.5f, 1.0f);
		DBuffer2 = float4(0.0f, 0.0f, 0.0f, 1.0f);

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_DECAL, decalStart, decalCount);
    #else
        decalCount = _DecalCount;
        decalStart = 0;
    #endif
		float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);
		uint i = 0;
		UNITY_LOOP
        for (i = 0; i < decalCount; i++)
        {
            DecalData decalData = FetchDecal(decalStart, i);
			float3 positionDS = mul(decalData.worldToDecal, float4(positionWS, 1.0)).xyz;
			positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.0f, 0.5);
			float decalBlend = decalData.normalToWorld[0][3];
			int diffuseIndex = decalData.normalToWorld[1][3];
			int normalIndex = decalData.normalToWorld[2][3];
			int maskIndex = decalData.normalToWorld[3][3];
			float lod = ComputeTextureLOD(positionDS.xz, _DecalAtlasResolution);								
			decalBlend = ((all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f))) ? decalBlend : 0;	// use blend of 0 instead of an 'if' because compiler moves the lod calculation inside the 'if' which causes incorrect values 
																												// if any of the pixels in the 2x2 quad gets rejected

			// Verified that lod calculation works with a test texture, looking at the shader code in Razor the lod calculation is outside the dynamic branches where the texture fetch happens,
			// however compiler was placing it inside the branch that was rejecting the pixel, which was causing incorrect lod to be calculated for any 2x2 quad where any of the pixels were rejected, 
			// so had to use alpha blend of 0 instead of branching to solve that issue."

			if(diffuseIndex != -1)
			{
				ApplyBlendDiffuse(DBuffer0, mask, positionDS.xz, diffuseIndex, DBUFFERHTILEBIT_DIFFUSE, decalBlend, lod);
				alpha = alpha < decalBlend ? decalBlend : alpha;	// use decal alpha if it higher than transparent alpha
			}

			if(normalIndex != -1)
			{
				ApplyBlendNormal(DBuffer1, mask, positionDS.xz, normalIndex, DBUFFERHTILEBIT_NORMAL, (float3x3)decalData.normalToWorld, decalBlend, lod);
			}

			if(maskIndex != -1)
			{
				ApplyBlendMask(DBuffer2, mask, positionDS.xz, maskIndex, DBUFFERHTILEBIT_MASK, decalBlend, lod);
			}
		}
#else
		mask = UnpackByte(LOAD_TEXTURE2D(_DecalHTileTexture, posInput.positionSS / 8).r);
#endif
		DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
		// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
		if(mask & DBUFFERHTILEBIT_DIFFUSE)
		{
			surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
		}

		if(mask & DBUFFERHTILEBIT_NORMAL)
		{
			surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
		}
		if(mask & DBUFFERHTILEBIT_MASK)
		{
			surfaceData.metallic = surfaceData.metallic * decalSurfaceData.mask.w + decalSurfaceData.mask.x;
			surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.mask.w + decalSurfaceData.mask.y;
			surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
		}
	}
}


