using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LTCAreaLightCookieManager
    {
        HDRenderPipelineAsset m_RenderPipelineAsset = null;

        // Structure for cookies used by area lights
        TextureCache2D m_AreaCookieTexArray;

        internal static readonly int s_texSource = Shader.PropertyToID("_SourceTexture");
        internal static readonly int s_sourceMipLevel = Shader.PropertyToID("_SourceMipLevel");
        internal static readonly int s_sourceSize = Shader.PropertyToID("_SourceSize");

        Material m_MaterialFilterAreaLights;
        MaterialPropertyBlock m_MPBFilterAreaLights;

        RenderTexture m_TempRenderTexture0 = null;
        RenderTexture m_TempRenderTexture1 = null;

        public LTCAreaLightCookieManager(HDRenderPipelineAsset hdAsset, int maxCacheSize)
        {
            // Keep track of the render pipeline asset
            m_RenderPipelineAsset = hdAsset;

            // Create the texture cookie cache that we shall be using for the area lights
            GlobalLightLoopSettings gLightLoopSettings = hdAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;
            m_AreaCookieTexArray = new TextureCache2D("AreaCookie");
            int cookieSize = gLightLoopSettings.cookieTexArraySize;
            int cookieResolution = (int)gLightLoopSettings.cookieSize;
            if (TextureCache2D.GetApproxCacheSizeInByte(cookieSize, cookieResolution, 1) > maxCacheSize)
                cookieSize = TextureCache2D.GetMaxCacheSizeForWeightInByte(maxCacheSize, cookieResolution, 1);
            m_AreaCookieTexArray.AllocTextureArray(cookieSize, cookieResolution, cookieResolution, TextureFormat.RGBA32, true);

            // Also make sure to create the engine material that is used for the filtering
            m_MaterialFilterAreaLights = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.filterAreaLightCookiesPS);
        }

        public void ReleaseResources()
        {
            if(m_AreaCookieTexArray != null)
            {
                m_AreaCookieTexArray.Release();
                m_AreaCookieTexArray = null;
            }

            CoreUtils.Destroy(m_MaterialFilterAreaLights);
            if(m_TempRenderTexture0 != null)
            {
                m_TempRenderTexture0.Release();
                m_TempRenderTexture0 = null;
            }
            if (m_TempRenderTexture1 != null)
            {
                m_TempRenderTexture1.Release();
                m_TempRenderTexture1 = null;
            }
        }

        public Texture GetTexCache()
        {
            return m_AreaCookieTexArray.GetTexCache();
        }

        public int FetchSlice(CommandBuffer cmd, Texture texture)
        {
            bool needUpdate;
            int sliceIndex = m_AreaCookieTexArray.ReserveSlice(texture, out needUpdate);
            if (sliceIndex != -1 && needUpdate)
            {
                Texture filteredAreaLight = FilterAreaLightTexture(cmd, texture);
                m_AreaCookieTexArray.UpdateSlice(cmd, sliceIndex, filteredAreaLight, m_AreaCookieTexArray.GetTextureHash(texture));

            }
            return sliceIndex;
        }

        Texture FilterAreaLightTexture( CommandBuffer cmd, Texture source)
        {
            if ( m_MaterialFilterAreaLights == null )
            {
                Debug.LogError( "FilterAreaLightTexture has an invalid shader. Can't filter area light cookie." );
                return null;
            }

            Texture texCache = m_AreaCookieTexArray.GetTexCache();
            int numMipLevels = m_AreaCookieTexArray.GetNumMipLevels();

            if (m_TempRenderTexture0 == null )
            {
                string cacheName = m_AreaCookieTexArray.GetCacheName();
                m_TempRenderTexture0 = new RenderTexture(texCache.width, texCache.height, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB )
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = cacheName + "TempAreaLightRT0"
                };

                // We start by a horizontal gaussian into mip 1 that reduces the width by a factor 2 but keeps the same height
                m_TempRenderTexture1 = new RenderTexture(texCache.width >> 1, texCache.height, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB )
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = cacheName + "TempAreaLightRT1"
                };
            }

            int sourceWidth = texCache.width;
            int sourceHeight = texCache.height;
            int targetWidth = sourceWidth;
            int targetHeight = sourceHeight;
            Vector4 targetSize = new Vector4( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

            // Start by copying the source texture to the array slice's mip 0
            {
                cmd.SetGlobalTexture( s_texSource, source );
                cmd.SetGlobalInt( s_sourceMipLevel, 0 );
                cmd.SetRenderTarget( m_TempRenderTexture0, 0);
                cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 0, MeshTopology.Triangles, 3, 1);
            }

            // Then operate on all the remaining mip levels
            Vector4 sourceSize = Vector4.zero;
            for ( int mipIndex=1; mipIndex < numMipLevels; mipIndex++ )
            {
                {   // Perform horizontal blur
                    targetWidth = Mathf.Max(1, targetWidth  >> 1);

                    sourceSize.Set( sourceWidth, sourceHeight, 1.0f / sourceWidth, 1.0f / sourceHeight );
                    targetSize.Set( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

                    cmd.SetGlobalTexture( s_texSource, m_TempRenderTexture0 );
                    cmd.SetGlobalInt( s_sourceMipLevel, mipIndex-1 );          // Use previous mip as source
                    cmd.SetGlobalVector( s_sourceSize, sourceSize );
                    cmd.SetRenderTarget( m_TempRenderTexture1, mipIndex-1 );    // Temp texture is already 1 mip lower than source
                    cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 1, MeshTopology.Triangles, 3, 1);
                }

                sourceWidth = targetWidth;

                {   // Perform vertical blur
                    targetHeight = Mathf.Max(1, targetHeight >> 1);

                    sourceSize.Set( sourceWidth, sourceHeight, 1.0f / sourceWidth, 1.0f / sourceHeight );
                    targetSize.Set( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

                    cmd.SetGlobalTexture( s_texSource, m_TempRenderTexture1 );
                    cmd.SetGlobalInt( s_sourceMipLevel, mipIndex-1 );
                    cmd.SetGlobalVector( s_sourceSize, sourceSize );
                    cmd.SetRenderTarget( m_TempRenderTexture0, mipIndex);
                    cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 2, MeshTopology.Triangles, 3, 1);
                }

                sourceHeight = targetHeight;
            }

            return m_TempRenderTexture0;
        }
    }
}
