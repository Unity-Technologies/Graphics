using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class Texture3DAtlas
    {
        public static readonly int _ZOffset = Shader.PropertyToID("_ZOffset");
        public static readonly int _DstTex = Shader.PropertyToID("_DstTex");
        public static readonly int _SrcTex = Shader.PropertyToID("_SrcTex");
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
            else if (volume.parameters.volumeMask != null)
            {
                var tex = volume.parameters.volumeMask;
                // Check that the texture size and format is as expected
                if (tex.width != m_atlasSize ||
                    tex.height != m_atlasSize ||
                    tex.depth != m_atlasSize)
                {
                    // TODO (Apoorva): Re-enable this check after support has been added for variable-resolution sub-textures:
                    /*
                    Debug.LogError(String.Format("3D Texture Atlas: Added texture {4} size {0}x{1}x{2} does not match size of atlas {3}x{3}x{3}",
                        tex.width,
                        tex.height,
                        tex.depth,
                        m_atlasSize,
                        tex.name
                    ));
                    return;
                    */
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

        public void UpdateAtlas(CommandBuffer cmd, ComputeShader blit3dShader)
        {
            const int NUM_THREADS = 8; // Defined as [numthreads(8,8,8)] in the compute shader
            int dispatchSize = DensityVolumeManager.volumeTextureSize / NUM_THREADS;

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
                                                autoGenerateMips: false,
                                                name: "DensityVolumeAtlas");

                    var isCopied = new bool[clampedNumTextures];
                    var oldRt = RenderTexture.active;

                    // Copy all the volumes backed by 3D textures into the atlas
                    foreach (DensityVolume v in m_volumes)
                    {
                        if (v.parameters.volumeShader != null ||
                            v.parameters.textureIndex == -1 ||
                            isCopied[v.parameters.textureIndex])
                            continue;

                        isCopied[v.parameters.textureIndex] = true;

                        var cs = blit3dShader;
                        cmd.SetComputeTextureParam(cs, 0, _DstTex, m_atlas);
                        cmd.SetComputeTextureParam(cs, 0, _SrcTex, v.parameters.volumeMask);
                        cmd.SetComputeIntParam(cs, _ZOffset, m_atlasSize * v.parameters.textureIndex);
                        cmd.DispatchCompute(cs, 0, dispatchSize, dispatchSize, dispatchSize);
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
                var isRun = new bool[NumTexturesInAtlas];

                foreach (DensityVolume v in m_volumes)
                {
                    if (v.parameters.volumeShader != null &&
                        v.parameters.textureIndex != -1 &&
                        !isRun[v.parameters.textureIndex])
                    {
                        isRun[v.parameters.textureIndex] = true;
                        var cs = v.parameters.volumeShader;
                        DensityVolumeManager.ComputeShaderParamsDelegate callback;
                        if (DensityVolumeManager.ComputeShaderParams.TryGetValue(v, out callback))
                        {
                            callback?.Invoke(v, cs, cmd);
                        }
                        cmd.SetComputeTextureParam(cs, 0, HDShaderIDs._VolumeMaskAtlas, m_atlas);
                        cmd.SetComputeIntParam(cs, _ZOffset, m_atlasSize * v.parameters.textureIndex);
                        cmd.DispatchCompute(cs, 0, dispatchSize, dispatchSize, dispatchSize);
                    }
                }
                m_atlas.rt.GenerateMips();
            }

            NotifyAtlasUpdated();
        }

        public RTHandle GetAtlas()
        {
            return m_atlas;
        }
    }
}
