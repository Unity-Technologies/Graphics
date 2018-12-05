using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    public class TextureCache2D : TextureCache
    {
        private Texture2DArray  m_Cache;

        public TextureCache2D(HDRenderPipelineAsset hdAsset, string cacheName = "")
            : base(cacheName)
        {
            m_MaterialFilterAreaLights = CoreUtils.CreateEngineMaterial(hdAsset.renderPipelineResources.shaders.filterAreaLightCookiesPS);
        }

        bool TextureHasMipmaps(Texture texture)
        {
            // Either the texture 
            if (texture is Texture2D)
                return ((Texture2D)texture).mipmapCount > 1;
            else
                return ((RenderTexture)texture).useMipMap;
        }

        protected override bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            // Make sure the array is not null or empty and that the first texture is a render-texture or a texture2D
            if(textureArray == null || textureArray.Length == 0  && (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
            {
                return false;
            }

            // First check here is to check if all the sub-texture have the same size
            for(int texIDx = 1; texIDx < textureArray.Length; ++texIDx)
            {
                // We cannot update if the textures if they don't have the same size or not the right type
                if (textureArray[texIDx].width != textureArray[0].width || textureArray[texIDx].height != textureArray[0].height || (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
                {
                    Debug.LogWarning("All the sub-textures should have the same dimensions to be handled by the texture cache.");
                    return false;
                }
            }

            // Do we have a mismatch ?
            var mismatch = (m_Cache.width != textureArray[0].width) || (m_Cache.height != textureArray[0].height);

            if (textureArray[0] is Texture2D)
            {
                mismatch |= (m_Cache.format != (textureArray[0] as Texture2D).format);
            }

            for (int texIDx = 0; texIDx < textureArray.Length; ++texIDx)
            {
                if (mismatch)
                {
                    cmd.ConvertTexture(textureArray[texIDx], 0, m_Cache, m_SliceSize * sliceIndex + texIDx);
                }
                else
                {
                    if (TextureHasMipmaps(textureArray[texIDx]))
                        cmd.CopyTexture(textureArray[texIDx], 0, m_Cache, m_SliceSize * sliceIndex + texIDx);
                    else
                        Debug.LogWarning("The texture '" + textureArray[texIDx] + "' should have mipmaps to be handled by the cookie texture array");
                }
            }
            return true;
        }

        protected override bool TransferToSliceAreaLight(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            // Make sure the array is not null or empty and that the first texture is a render-texture or a texture2D
            if(textureArray == null || textureArray.Length == 0  && (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
            {
                return false;
            }

            // First check here is to check if all the sub-texture have the same size
            for(int texIDx = 1; texIDx < textureArray.Length; ++texIDx)
            {
                // We cannot update if the textures if they don't have the same size or not the right type
                if (textureArray[texIDx].width != textureArray[0].width || textureArray[texIDx].height != textureArray[0].height || (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
                {
                    Debug.LogWarning("All the sub-textures should have the same dimensions to be handled by the texture cache.");
                    return false;
                }
            }

            // Use gaussian filtering to create clean mips
            for (int texIDx = 0; texIDx < textureArray.Length; ++texIDx)
            {
                Texture texture = FilterAreaLightTexture( cmd, textureArray[texIDx], m_SliceSize*sliceIndex + texIDx );
                cmd.CopyTexture(texture, 0, m_Cache, m_SliceSize * sliceIndex + texIDx);
            }

            return true;
        }

        public override Texture GetTexCache()
        {
            return m_Cache;
        }

        public bool AllocTextureArray(int numTextures, int width, int height, TextureFormat format, bool isMipMapped)
        {
            var res = AllocTextureArray(numTextures);
            m_NumMipLevels = GetNumMips(width, height);

            m_Cache = new Texture2DArray(width, height, numTextures, format, isMipMapped)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetTextureAutoName(width, height, format, TextureDimension.Tex2DArray, depth: numTextures, name: m_CacheName)
            };

            return res;
        }

        public void Release()
        {
            CoreUtils.Destroy(m_Cache);
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return (long)((long)nbElement * resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(int weight, int resolution, int sliceSize)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }

        #region Gaussian Filtering for Area Light Cookies

        internal static readonly int s_texSource = Shader.PropertyToID("_texSource");
        internal static readonly int s_sourceMipLevel = Shader.PropertyToID("_sourceMipLevel");
        internal static readonly int s_sourceSize = Shader.PropertyToID("_sourceSize");
        internal static readonly int s_targetSize = Shader.PropertyToID("_targetSize");

        Material                m_MaterialFilterAreaLights;
        MaterialPropertyBlock   m_MPBFilterAreaLights;

        RenderTexture           m_TempRenderTexture0;
        RenderTexture           m_TempRenderTexture1;

        /// <summary>
        /// Filters the source texture into the target array slice using a gaussian filter to build the mip maps
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetIndex"></param>
        Texture    FilterAreaLightTexture( CommandBuffer cmd, Texture source, int targetIndex )
        {
            if ( m_MaterialFilterAreaLights == null )
            {
                Debug.LogError( "FilterAreaLightTexture has an invalid shader. Can't filter area light cookie." );
                return null;
            }

            if ( m_TempRenderTexture0 == null )
            {
                m_TempRenderTexture0 = new RenderTexture( m_Cache.width, m_Cache.height, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB )
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = m_CacheName + "TempAreaLightRT0"
                };

                // We start by a horizontal gaussian into mip 1 that reduces the width by a factor 2 but keeps the same height
                m_TempRenderTexture1 = new RenderTexture( m_Cache.width >> 1, m_Cache.height, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB )
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = m_CacheName + "TempAreaLightRT1"
                };
            }

            int sourceWidth = m_Cache.width;
            int sourceHeight = m_Cache.height;
            int targetWidth = sourceWidth;
            int targetHeight = sourceHeight;
            Vector4 targetSize = new Vector4( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

            cmd.SetGlobalInt( "_sliceIndex", targetIndex );

            // Start by copying the source texture to the array slice's mip 0
            {
                cmd.SetGlobalTexture( s_texSource, source );
                cmd.SetGlobalVector( s_targetSize, targetSize );
                cmd.SetRenderTarget( m_TempRenderTexture0, 0, CubemapFace.Unknown, targetIndex );
//                CoreUtils.DrawFullScreen(cmd, m_MaterialFilterAreaLights);
                cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 0, MeshTopology.Quads, 4);
            }

            Vector4 sourceSize = Vector4.zero;
            for ( int mipIndex=1; mipIndex < m_NumMipLevels; mipIndex++ )
            {
                {   // Perform horizontal blur
                    targetWidth = Mathf.Max(1, targetWidth  >> 1);

                    sourceSize.Set( sourceWidth, sourceHeight, 1.0f / sourceWidth, 1.0f / sourceHeight );
                    targetSize.Set( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

                    cmd.SetGlobalTexture( s_texSource, m_TempRenderTexture0 );
                    cmd.SetGlobalInt( s_sourceMipLevel, mipIndex-1 );          // Use previous mip as source
                    cmd.SetGlobalVector( s_sourceSize, sourceSize );
                    cmd.SetGlobalVector( s_targetSize, targetSize );
                    cmd.SetRenderTarget( m_TempRenderTexture1, mipIndex-1 );    // Temp texture is already 1 mip lower than source
//                    CoreUtils.DrawFullScreen(cmd, m_MaterialFilterAreaLights);
                    cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 1, MeshTopology.Quads, 4);
                }

                sourceWidth = targetWidth;

                {   // Perform vertical blur
                    targetHeight = Mathf.Max(1, targetHeight >> 1);

                    sourceSize.Set( sourceWidth, sourceHeight, 1.0f / sourceWidth, 1.0f / sourceHeight );
                    targetSize.Set( targetWidth, targetHeight, 1.0f / targetWidth, 1.0f / targetHeight );

                    cmd.SetGlobalTexture( s_texSource, m_TempRenderTexture1 );
                    cmd.SetGlobalInt( s_sourceMipLevel, mipIndex-1 );
                    cmd.SetGlobalVector( s_sourceSize, sourceSize );
                    cmd.SetGlobalVector( s_targetSize, targetSize );
                    cmd.SetRenderTarget( m_TempRenderTexture0, mipIndex, CubemapFace.Unknown, targetIndex );
//                    CoreUtils.DrawFullScreen(cmd, m_MaterialFilterAreaLights);
                    cmd.DrawProcedural(Matrix4x4.identity, m_MaterialFilterAreaLights, 2, MeshTopology.Quads, 4);
                }

                sourceHeight = targetHeight;
            }

            return m_TempRenderTexture0;
        }

        #endregion
    }
}
