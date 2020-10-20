#include "GraniteShaderLibBase.hlsl"

#define VtAddressMode_Wrap 0
#define VtAddressMode_Clamp 1
#define VtAddressMode_Udim 2

#define VtFilter_Anisotropic 0

#define VtLevel_Automatic 0
#define VtLevel_Lod 1
#define VtLevel_Bias 2
#define VtLevel_Derivatives 3

#define VtUvSpace_Regular 0
#define VtUvSpace_PreTransformed 1

#define VtSampleQuality_Low 0
#define VtSampleQuality_High 1

struct VtInputParameters
{
    float2 uv;
    float lodOrOffset;
    float2 dx;
    float2 dy;
    int addressMode;
    int filterMode;
    int levelMode;
    int uvMode;
    int sampleQuality;
};

int VirtualTexturingLookup(
    in GraniteConstantBuffers grCB,
	in GraniteTranslationTexture translationTable,
    in VtInputParameters input,
	out GraniteLookupData graniteLookupData,
    out float4 resolveResult
)
{
    GraniteStreamingTextureConstantBuffer grSTCB = grCB.streamingTextureBuffer;
    GraniteTilesetConstantBuffer tsCB = grCB.tilesetBuffer;

    float2 texCoord = input.uv;
    float2 dx;
    float2 dy;
    float mipLevel; //interger

    if (input.levelMode == VtLevel_Automatic)
    {
	    dx = ddx(texCoord);
	    dy = ddy(texCoord);
    }
    else if (input.levelMode == VtLevel_Bias)
    {
        // We can't simply add the bias after the mip-calculation since the derivatives
        // are also used when sampling the cache so make sure we apply bias by scaling derivatives
        if ( input.sampleQuality == VtSampleQuality_High )
        {
            float offsetPow2 = pow(2.0f, input.lodOrOffset);
            dx = ddx(texCoord) * offsetPow2;
            dy = ddy(texCoord) * offsetPow2;
        }
        // In low qauality we don't care about cache derivatives and will add the bias later
        else
        {
            dx = ddx(texCoord);
            dy = ddy(texCoord);           
        }
    }    
    else if (input.levelMode == VtLevel_Derivatives)
    {
	    dx = input.dx;
	    dy = input.dy;
    }
    else /*input.levelMode == VtLevel_Lod*/
    {
        //gra_TrilinearOffset ensures we do round-nearest for no-trilinear and 
        //round-floor for trilinear.
	    float clampedLevel = clamp(input.lodOrOffset + gra_TrilinearOffset, 0.0f, gra_NumLevels);
        mipLevel = floor(clampedLevel);
	    dx = float2(frac(clampedLevel), 0.0f); // trilinear blend ratio
	    dy = float2(0.0f,0.0f);
    }
    
    // Transform the derivatives to atlas space if needed
    if (input.uvMode == VtUvSpace_Regular && input.levelMode != VtLevel_Lod)
    {
	    dx = gra_Transform.zw * dx;
	    dy = gra_Transform.zw * dy;
    }

    if (input.levelMode != VtLevel_Lod)
    {
        mipLevel = GranitePrivate_CalcMiplevelAnisotropic(grCB.tilesetBuffer, grCB.streamingTextureBuffer, dx, dy);   

        // Simply add it here derivatives are wrong from this point onwards but not used anymore
        if ( input.sampleQuality == VtSampleQuality_Low && input.levelMode == VtLevel_Bias)
        {
            mipLevel += input.lodOrOffset;
            // GranitePrivate_CalcMiplevelAnisotropic will already clamp between 0 gra_NumLevels
            // But we need to do it again here. The alternative is modifying dx,dy before passing to
            // GranitePrivate_CalcMiplevelAnisotropic adding a pow2 + 4 fmuls so probably
            // the exra clamp is more appropriate here.
            mipLevel = clamp(mipLevel, 0.0f, gra_NumLevels);
        }

        mipLevel = floor(mipLevel + 0.5f); //round nearest
    }

    // Apply clamp/wrap mode if needed and transform into atlas space
    // If the user passes in pre-transformed texture coords clamping and wrapping should be handled by the user
    if (input.uvMode == VtUvSpace_Regular)
    {
        if (input.addressMode == VtAddressMode_Wrap)
        {
            texCoord = frac(input.uv);
        }
        else if (input.addressMode == VtAddressMode_Clamp)
        {
            float2 epsilon2 = float2(gra_AssetWidthRcp, gra_AssetHeightRcp);
            texCoord = clamp(input.uv, epsilon2, float2(1,1) - epsilon2);
        }
        else if (input.addressMode == VtAddressMode_Udim)
        {
            // not modified (i.e outside of the 0-1 range, atlas transform below will take care of it)
            texCoord = input.uv;
        }

	    texCoord = Granite_Transform(gra_StreamingTextureCB, texCoord);        
    }

	// calculate resolver data
	float2 level0NumTiles = float2(gra_Level0NumTilesX, gra_Level0NumTilesX*gra_NumTilesYScale);
	float2 virtualTilesUv = floor(texCoord * level0NumTiles * pow(0.5, mipLevel));
	resolveResult = GranitePrivate_MakeResolveOutput(tsCB, virtualTilesUv, mipLevel);

    float4 translationTableData;
    if (input.levelMode != VtLevel_Lod)
    {
        // Look up the physical page indexes and the number of pages on the mipmap
        // level of the page in the translation texture
        // Note: this is equal for both anisotropic and linear sampling
        // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
        translationTableData = GranitePrivate_SampleLevel_Translation(translationTable, texCoord, mipLevel);
#else
        translationTableData = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, mipLevel));
#endif
    }
    else
    {
        // Look up the physical page indexes and the number of pages on the mipmap
        // level of the page in the translation texture
        // Note: this is equal for both anisotropic and linear sampling
        // We could use a sample bias here for 'auto' mip level detection
#if (GRA_LOAD_INSTR==0)
	    translationTableData = GranitePrivate_SampleLevel_Translation(translationTable, texCoord, mipLevel);
#else
	    translationTableData = GranitePrivate_Load(translationTable, gra_Int3(virtualTilesUv, mipLevel));
#endif   
    }

	graniteLookupData.translationTableData = translationTableData;
	graniteLookupData.textureCoordinates = texCoord;
	graniteLookupData.dX = dx;
	graniteLookupData.dY = dy;

	return 1;
}

int VirtualTexturingSample(
    in GraniteTilesetConstantBuffer tsCB,
    in GraniteLookupData graniteLookupData,
    in GraniteCacheTexture cacheTexture,
    in int layer,
    in int levelMode,
    in int quality,
    out float4 result)
{
	// Convert from pixels to [0-1] and look up in the physical page texture
	float2 deltaScale;
	float3 cacheCoord = GranitePrivate_TranslateCoord(tsCB, graniteLookupData.textureCoordinates, graniteLookupData.translationTableData, layer, deltaScale);

    if ( levelMode != VtLevel_Lod )
    {
        if ( quality == VtSampleQuality_Low )
        {
            // This leads to small artefacts at tile borders but is generally not noticable unless the texture
            // is greatly magnified
            result = GranitePrivate_SampleArray(cacheTexture, cacheCoord);
        }
        else /* quality == VtSampleQuality_High */
        {
            deltaScale *= gra_LodBiasPow2;

            // Calculate the delta scale this works by first converting the [0-1] texcoord deltas to
            // pixel deltas on the current mip level, then dividing by the cache size to convert to [0-1] cache deltas
            float2 sampDeltaX = graniteLookupData.dX*deltaScale;
            float2 sampDeltaY = graniteLookupData.dY*deltaScale;

            result = GranitePrivate_SampleGradArray(cacheTexture, cacheCoord, sampDeltaX, sampDeltaY);      
        }
    }
    else
    {
	    result = GranitePrivate_SampleLevelArray(cacheTexture, cacheCoord, graniteLookupData.dX.x);      
    }

	return 1;
}
