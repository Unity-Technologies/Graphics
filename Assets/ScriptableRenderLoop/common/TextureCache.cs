using UnityEngine;
using UnityEditor;
//using System;
using System.Collections.Generic;



public class TextureCache2D : TextureCache
{
    private Texture2DArray cache;

    public override void TransferToSlice(int sliceIndex, Texture texture)
    {
		for (int m = 0; m < m_numMipLevels; m++)
			Graphics.CopyTexture(texture, 0, m, cache, sliceIndex, m);
    }

    public override Texture GetTexCache()
    {
        return cache;
    }

    public bool AllocTextureArray(int numTextures, int width, int height, TextureFormat format, bool isMipMapped)
    {
        bool res = AllocTextureArray(numTextures);
        m_numMipLevels = GetNumMips(width, height);

        cache = new Texture2DArray(width, height, numTextures, format, isMipMapped);
		cache.hideFlags = HideFlags.HideAndDontSave;
		cache.wrapMode = TextureWrapMode.Clamp;

        return res;
    }

    public void Release()
    {
        Texture.DestroyImmediate(cache);      // do I need this?
    }
}

public class TextureCacheCubemap : TextureCache
{
    private CubemapArray cache;

    public override void TransferToSlice(int sliceIndex, Texture texture)
    {
        for(int f=0; f<6; f++)
            for(int m=0; m<m_numMipLevels; m++)
                Graphics.CopyTexture(texture, f, m, cache, 6*sliceIndex + f, m);
    }

    public override Texture GetTexCache()
    {
        return cache;
    }

    public bool AllocTextureArray(int numCubeMaps, int width, TextureFormat format, bool isMipMapped)
    {
        bool res = AllocTextureArray(6*numCubeMaps);
        m_numMipLevels = GetNumMips(width, width);

        cache = new CubemapArray(width, numCubeMaps, format, isMipMapped);
		cache.hideFlags = HideFlags.HideAndDontSave;
		cache.wrapMode = TextureWrapMode.Clamp;

        return res;
    }

    public void Release()
    {
        Texture.DestroyImmediate(cache);      // do I need this?
    }
}


abstract public class TextureCache : Object
{
    protected int m_numMipLevels;

	public static int ms_GlobalTextureCacheVersion = 0;
	public int m_TextureCacheVersion = 0;

	internal class AssetReloader : AssetPostprocessor
	{
		void OnPostprocessTexture(Texture texture)
		{
			ms_GlobalTextureCacheVersion++;
		}
	}

    private struct SSliceEntry
    {
        public uint TexID;
		public uint CountLRU;
    };

    private int m_numTextures;
    private int [] m_SortedIdxArray;
    private SSliceEntry [] m_SliceArray;
	private AssetReloader m_assetReloader;

	Dictionary<uint, int> m_locatorInSliceArray;

    private static uint g_MaxFrameCount = unchecked( (uint) (-1) );
    private static uint g_InvalidTexID = (uint) 0;

    public int FetchSlice(Texture texture)
    {
        uint TexID = (uint)texture.GetInstanceID();

        //assert(TexID!=g_InvalidTexID);
        if(TexID==g_InvalidTexID) return 0;

        bool bSwapSlice = false;
        bool bFoundAvailOrExistingSlice = false;
        int sliceIndex = -1;

        // search for existing copy
		if(m_locatorInSliceArray.ContainsKey(TexID))
        {
			if (m_TextureCacheVersion != ms_GlobalTextureCacheVersion)
			{
				m_locatorInSliceArray.Remove(TexID);
				m_TextureCacheVersion++;
				Debug.Assert(m_TextureCacheVersion <= ms_GlobalTextureCacheVersion);
			}
			else
			{
				sliceIndex = m_locatorInSliceArray[TexID];
				bFoundAvailOrExistingSlice = true;
			}
            //assert(m_SliceArray[sliceIndex].TexID==TexID);
        }

        // If no existing copy found in the array
        if(!bFoundAvailOrExistingSlice)
        {
	        // look for first non zero entry. Will by the least recently used entry
	        // since the array was pre-sorted (in linear time) in NewFrame()
	        bool bFound = false;
	        int j=0, idx=0;
	        while((!bFound) && j<m_numTextures)
	        {
		        idx = m_SortedIdxArray[j];
		        if(m_SliceArray[idx].CountLRU==0) ++j;		// if entry already snagged by a new texture in this frame then ++j
		        else bFound=true;
	        }

	        if(bFound)
	        {
		        // if we are replacing an existing entry delete it from m_locatorInSliceArray.
		        if(m_SliceArray[idx].TexID!=g_InvalidTexID) 
		        { 
			        m_locatorInSliceArray.Remove( m_SliceArray[idx].TexID );
		        }

		        m_locatorInSliceArray.Add(TexID,idx);
		        m_SliceArray[idx].TexID=TexID;

		        sliceIndex=idx; 
		        bFoundAvailOrExistingSlice=true;
		        bSwapSlice = true;
	        }
        }


        // wrap up
        //assert(bFoundAvailOrExistingSlice);
        if(bFoundAvailOrExistingSlice)
        { 
	        m_SliceArray[sliceIndex].CountLRU=0;		// mark slice as in use this frame

	        if(bSwapSlice)	// if this was a miss
	        {
		        // transfer new slice to sliceIndex from source texture
                TransferToSlice(sliceIndex, texture);
	        }
        }

        return sliceIndex;
    }

    public void NewFrame()
    {
	    int numNonZeros = 0;
	    int [] tmpBuffer = new int[m_numTextures];
	    for(int i=0; i<m_numTextures; i++)
	    {
		    tmpBuffer[i]=m_SortedIdxArray[i];		// copy buffer
		    if(m_SliceArray[m_SortedIdxArray[i]].CountLRU!=0) ++numNonZeros;
	    }
	    int nonZerosBase = 0, zerosBase = 0;
	    for(int i=0; i<m_numTextures; i++)
	    {
		    if( m_SliceArray[tmpBuffer[i]].CountLRU==0 )
		    {
			    m_SortedIdxArray[zerosBase+numNonZeros]=tmpBuffer[i];
			    ++zerosBase;
		    }
		    else
		    {
			    m_SortedIdxArray[nonZerosBase]=tmpBuffer[i];
			    ++nonZerosBase;
		    }
	    }
	    
	    for(int i=0; i<m_numTextures; i++)
	    {
		    if(m_SliceArray[i].CountLRU<g_MaxFrameCount) ++m_SliceArray[i].CountLRU;		// next frame
	    }
	
	    //for(int q=1; q<m_numTextures; q++)
		//    assert(m_SliceArray[m_SortedIdxArray[q-1]].CountLRU>=m_SliceArray[m_SortedIdxArray[q]].CountLRU);
    }

    public TextureCache()
    {
	    m_numTextures=0;
        m_numMipLevels=0;
	}

    public virtual void TransferToSlice(int sliceIndex, Texture texture)
    {
    }

    public virtual Texture GetTexCache()
    {
        return null;
    }

    protected bool AllocTextureArray(int numTextures)
    {
        if (numTextures > 0)
        {
            m_SliceArray = new SSliceEntry[numTextures];
            m_SortedIdxArray = new int[numTextures];
            m_locatorInSliceArray = new Dictionary<uint, int>();

            m_numTextures = numTextures;
            for (int i = 0; i < m_numTextures; i++)
            {
                m_SliceArray[i].CountLRU = g_MaxFrameCount;			// never used before
                m_SliceArray[i].TexID = g_InvalidTexID;
                m_SortedIdxArray[i] = i;
            }
        }

        //return m_SliceArray != NULL && m_SortedIdxArray != NULL && numTextures > 0;
        return numTextures > 0;
    }

    // should not really be used in general. Assuming lights are culled properly entries will automatically be replaced efficiently.
    public void RemoveEntryFromSlice(Texture texture)
    {
        uint TexID = (uint)texture.GetInstanceID();

	    //assert(TexID!=g_InvalidTexID);
	    if(TexID==g_InvalidTexID) return;

        // search for existing copy
        if(m_locatorInSliceArray.ContainsKey(TexID))
        {
            int sliceIndex = m_locatorInSliceArray[TexID];
	    
		    //assert(m_SliceArray[sliceIndex].TexID==TexID);
		
		    // locate entry sorted by uCountLRU in m_pSortedIdxArray
		    bool bFoundIdxSortLRU = false;
		    int i=0;
		    while((!bFoundIdxSortLRU) && i<m_numTextures)
		    {
			    if(m_SortedIdxArray[i]==sliceIndex) bFoundIdxSortLRU=true;
			    else ++i;
		    }

		    if(bFoundIdxSortLRU)
		    {
			    // relocate sliceIndex to front of m_pSortedIdxArray since uCountLRU will be set to maximum.
			    for(int j=0; j<i; j++) { m_SortedIdxArray[j+1]=m_SortedIdxArray[j]; }
			    m_SortedIdxArray[0]=sliceIndex;

			    // delete from m_locatorInSliceArray and m_pSliceArray.
                m_locatorInSliceArray.Remove( TexID );
			    m_SliceArray[sliceIndex].CountLRU=g_MaxFrameCount;			// never used before
			    m_SliceArray[sliceIndex].TexID=g_InvalidTexID;
		    }
	    }

    }

    protected int GetNumMips(int width, int height)
    {
        return GetNumMips(width>height ? width : height);
    }

    protected int GetNumMips(int dim)
    {
	    uint uDim = (uint) dim;
	    int iNumMips = 0;
	    while(uDim>0)
	    { ++iNumMips; uDim>>=1; }
	    return iNumMips;
    }
}
