using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class Texture3DAtlas
    {
        private List<DensityVolume> m_volumes = new List<DensityVolume>();

        private RTHandle m_atlas;
        private TextureFormat m_format;

        private bool m_updateAtlas = false;
        private int m_atlasSize = 0;
        private int m_numTexturesInAtlas = 0;

        public delegate void AtlasUpdated();
        public AtlasUpdated OnAtlasUpdated = null;


        void NotifyAtlasUpdated()
        {
            if (OnAtlasUpdated != null)
            {
                OnAtlasUpdated();
            }
        }

        public Texture3DAtlas(TextureFormat format, int textureSize)
        {
            m_format = format;
            m_atlasSize = textureSize;
        }

        public void AddVolume(DensityVolume volume)
        {
            bool addTexture = true;
            if (volume.parameters.volumeMask == null && volume.parameters.volumeShader == null)
            {
                volume.parameters.textureIndex = -1;
                addTexture = false;
            }
            else if (volume.parameters.volumeShader != null)
            {
                // Check if a volume with the same shader already exists
                foreach (DensityVolume v in m_volumes)
                {
                    if (v.parameters.volumeShader == volume.parameters.volumeShader)
                    {
                        volume.parameters.textureIndex = v.parameters.textureIndex;
                        addTexture = false;
                        break;
                    }
                }
            }
            else if (volume.parameters.volumeMask != null)
            {
                var tex = volume.parameters.volumeMask;
                // Check that the texture size and format is as expected
                if (tex.width != m_atlasSize ||
                    tex.height != m_atlasSize ||
                    tex.depth != m_atlasSize)
                {
                    Debug.LogError(String.Format("3D Texture Atlas: Added texture {4} size {0}x{1}x{2} does not match size of atlas {3}x{3}x{3}",
                        tex.width,
                        tex.height,
                        tex.depth,
                        m_atlasSize,
                        tex.name
                    ));
                    return;
                }

                if (volume.parameters.volumeMask.format != m_format)
                {
                    Debug.LogError(String.Format(
                        "3D Texture Atlas: Added texture {2} format {0} does not match format of atlas {1}",
                        tex.format,
                        m_format,
                        tex.name
                    ));
                    return;
                }

                // Check if a volume with the same mask texture already exists
                foreach (DensityVolume v in m_volumes)
                {
                    if (v.parameters.volumeShader != null)
                        continue;

                    if (v.parameters.volumeMask == volume.parameters.volumeMask)
                    {
                        volume.parameters.textureIndex = v.parameters.textureIndex;
                        addTexture = false;
                        break;
                    }
                }
            }

            if (addTexture)
            {
                volume.parameters.textureIndex = m_numTexturesInAtlas;
                m_numTexturesInAtlas++;
                m_updateAtlas = true;
            }
            m_volumes.Add(volume);
        }

        public void RemoveVolume(DensityVolume volume)
        {
            if (m_volumes.Contains(volume))
            {
                m_volumes.Remove(volume);
                m_updateAtlas = true;
            }
        }

        public void ClearVolumes()
        {
            m_volumes.Clear();
            m_updateAtlas = true;
            m_numTexturesInAtlas = 0;
        }

        public void UpdateAtlas(CommandBuffer cmd)
        {
            foreach (DensityVolume v in m_volumes)
            {
                if (v.parameters.volumeShader != null)
                {
                    cmd.SetComputeTextureParam(v.parameters.volumeShader, 0, HDShaderIDs._VolumeMaskAtlas, m_atlas);
                    cmd.DispatchCompute(v.parameters.volumeShader, 0, 4, 4, 4);
                }
            }

            if (!m_updateAtlas)
            {
                return;
            }

            if (m_volumes.Count > 0)
            {
                int textureSliceSize = m_atlasSize * m_atlasSize * m_atlasSize;
                int totalTextureSize = textureSliceSize * m_numTexturesInAtlas;

                Color [] colorData = new Color[totalTextureSize];
                m_atlas = RTHandles.Alloc(m_atlasSize,
                                          m_atlasSize,
                                          m_atlasSize * m_numTexturesInAtlas,
                                            dimension: TextureDimension.Tex3D,
                                            //colorFormat: Experimental.Rendering.GraphicsFormat.R8_UInt,
                                            colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                                            enableRandomWrite: true,
                                            useMipMap:         true,
                                            name: "DensityVolumeAtlas");
                var isCopied = new bool[m_numTexturesInAtlas];

                //Iterate through all the textures and append their texture data to the texture array
                //Once CopyTexture works for 3D textures we can replace this with a series of copy texture calls
                foreach (DensityVolume v in m_volumes)
                {
                    if (v.parameters.volumeShader != null ||
                        v.parameters.textureIndex == -1 ||
                        isCopied[v.parameters.textureIndex])
                        continue;

                    isCopied[v.parameters.textureIndex] = true;
                    Texture3D tex = v.parameters.volumeMask;
                    Color [] texData = tex.GetPixels();
                    Array.Copy(texData, 0, colorData, textureSliceSize * v.parameters.textureIndex, texData.Length);
                }

                // TODO (Apoorva): Re-enable texture blitting
                // m_atlas.SetPixels(colorData);
                // m_atlas.Apply();
            }
            else
            {
                m_atlas = null;
            }

            NotifyAtlasUpdated();

            m_updateAtlas = false;
        }

        public RTHandle GetAtlas()
        {
            return m_atlas;
        }
    }
}
