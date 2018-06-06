using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumeTextureAtlas
    {
        private List<Texture3D> textures = null;

        //Assuming every volume texture is square and number of depth slices equal to 2D dimensions
        private int requiredTextureSize;
        private TextureFormat requiredTextureFormat;

        public bool needTextureUpdate
        {
            get;
            private set;
        }


        public Texture3D volumeAtlas
        {
            get;
            private set;
        }

        public event Action OnAtlasUpdated;

        private void NotifyAtlasUpdated()
        {
            if (OnAtlasUpdated != null)
            {
                OnAtlasUpdated();
            }
        }

        public VolumeTextureAtlas(TextureFormat atlasFormat, int atlasSize)
        {
            requiredTextureSize = atlasSize;

            requiredTextureFormat = atlasFormat;

            textures = new List<Texture3D>();

            needTextureUpdate = false;
        }

        public void ClearTextures()
        {
            textures.Clear();
            needTextureUpdate = true;
        }

        public void AddTexture(Texture3D volumeTexture)
        {
            //TODO:  we should really just convert the texture to the right size and format...HOWEVER, we dont' support 3D textures at the moment in the ConvertTexture call
            if (volumeTexture.width != requiredTextureSize || volumeTexture.height != requiredTextureSize || volumeTexture.depth != requiredTextureSize)
            {
                Debug.LogError(string.Format("VolumeTextureAtlas: Dimensions of texture {0} does not match expected dimensions of {1}", volumeTexture, requiredTextureSize));
                return;
            }

            if (volumeTexture.format != requiredTextureFormat)
            {
                Debug.LogError(string.Format("VolumeTextureAtlas: Texture format of texture {0} : {1} does not match expected format of {2}", volumeTexture, volumeTexture.format, requiredTextureFormat));
                return;
            }

            if (!textures.Contains(volumeTexture))
            {
                textures.Add(volumeTexture);

                needTextureUpdate = true;
            }
        }

        public void RemoveTexture(Texture3D volumeTexture)
        {
            if (textures.Contains(volumeTexture))
            {
                textures.Add(volumeTexture);

                needTextureUpdate = true;
            }
        }

        //Generates the volume atlas by converting (if needed) and then copying the textures into one big volume texture.
        public void GenerateVolumeAtlas(CommandBuffer cmd)
        {
            if (needTextureUpdate)
            {
                if (textures.Count > 0)
                {
                    Color[] colorArray = new Color[0];
                    volumeAtlas = new Texture3D(requiredTextureSize, requiredTextureSize, requiredTextureSize * textures.Count, requiredTextureFormat, true);

                    foreach (Texture3D tex in textures)
                    {
                        //TODO: Need to have copy texture and convert texture working for Tex3D in order for this to be
                        //more robust
                        Color[] texColor = tex.GetPixels();
                        Array.Resize(ref colorArray, texColor.Length + colorArray.Length);
                        Array.Copy(texColor, 0, colorArray, colorArray.Length - texColor.Length, texColor.Length);
                    }

                    volumeAtlas.SetPixels(colorArray);
                    volumeAtlas.Apply();
                }
                else
                {
                    volumeAtlas = null;
                }

                needTextureUpdate = false;

                NotifyAtlasUpdated();
            }
        }

        public int GetTextureIndex(Texture3D tex)
        {
            return textures.IndexOf(tex);
        }
    }
}
