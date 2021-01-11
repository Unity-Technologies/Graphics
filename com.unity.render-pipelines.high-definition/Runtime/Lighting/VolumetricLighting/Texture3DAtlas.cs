using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class Texture3DAtlas
    {
        public static readonly int _ZOffset = Shader.PropertyToID("_ZOffset");
        public int NumTexturesInAtlas = 0;

        private List<DensityVolume> m_volumes = new List<DensityVolume>();

        private RTHandle m_atlas;
        private TextureFormat m_format;

        private bool m_updateAtlas = false;
        private int m_atlasSize = 0;

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
                volume.parameters.textureIndex = NumTexturesInAtlas;
                NumTexturesInAtlas++;
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
            NumTexturesInAtlas = 0;
        }

        public void UpdateAtlas(CommandBuffer cmd)
        {
            if (m_updateAtlas)
            {
                m_updateAtlas = false;

                if (m_volumes.Count > 0)
                {
                    // Ensure that number of textures is at least one to avoid a zero-sized atlas
                    var clampedNumTextures = Math.Max(1, NumTexturesInAtlas);
                    m_atlas = RTHandles.Alloc(m_atlasSize,
                                              m_atlasSize,
                                              m_atlasSize * clampedNumTextures,
                                                dimension: TextureDimension.Tex3D,
                                                colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                                                enableRandomWrite: true,
                                                useMipMap: true,
                                                name: "DensityVolumeAtlas");
                    var isCopied = new bool[clampedNumTextures];
                    var oldRt = RenderTexture.active;

                    //Iterate through all the textures and append their texture data to the texture array
                    //Once CopyTexture works for 3D textures we can replace this with a series of copy texture calls
                    foreach (DensityVolume v in m_volumes)
                    {
                        if (v.parameters.volumeShader != null ||
                            v.parameters.textureIndex == -1 ||
                            isCopied[v.parameters.textureIndex])
                            continue;

                        isCopied[v.parameters.textureIndex] = true;
                        for (int i = 0; i < m_atlasSize; i++)
                        {
                            Graphics.Blit(v.parameters.volumeMask, m_atlas.rt, Vector2.one, Vector2.zero, i, m_atlasSize * v.parameters.textureIndex + i);
                        }
                    }

                    RenderTexture.active = oldRt;
                }
                else
                {
                    m_atlas = null;
                }
            }

            if (m_atlas != null)
            {
                foreach (DensityVolume v in m_volumes)
                {
                    if (v.parameters.volumeShader != null)
                    {
                        var cs = v.parameters.volumeShader;
                        cmd.SetComputeTextureParam(cs, 0, HDShaderIDs._VolumeMaskAtlas, m_atlas);
                        var mtx =
                            Matrix4x4.Rotate(Quaternion.Euler(45f, 0, 0))
                            * Matrix4x4.Rotate(Quaternion.Euler(0, 100f * Time.time, 0))
                            * Matrix4x4.Translate(new Vector3(-16f, -16f, -16f));
                        cmd.SetComputeMatrixParam(cs, HDShaderIDs._Params, mtx);
                        cmd.SetComputeIntParam(cs, _ZOffset, m_atlasSize * v.parameters.textureIndex);
                        cmd.DispatchCompute(v.parameters.volumeShader, 0, 4, 4, 4);
                    }
                }
            }


            NotifyAtlasUpdated();

        }

        public RTHandle GetAtlas()
        {
            return m_atlas;
        }
    }
}
