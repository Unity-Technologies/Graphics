using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class Texture3DAtlas
    {
        public static readonly int _ZBias = Shader.PropertyToID("_ZBias");
        public static readonly int _DstTex = Shader.PropertyToID("_DstTex");
        public static readonly int _SrcTex = Shader.PropertyToID("_SrcTex");
        public int NumTexturesInAtlas = 0;

        private List<DensityVolume> m_volumes = new List<DensityVolume>();

        private RTHandle m_atlas;
        private TextureFormat m_format;

        private bool m_updateAtlas = false;

        public delegate void AtlasUpdated();
        public AtlasUpdated OnAtlasUpdated = null;


        void NotifyAtlasUpdated()
        {
            if (OnAtlasUpdated != null)
            {
                OnAtlasUpdated();
            }
        }

        public Texture3DAtlas(TextureFormat format)
        {
            m_format = format;
        }

        public void AddVolume(DensityVolume volume)
        {
            bool addTexture = true;
            if (volume.parameters.volumeMask == null && volume.parameters.volumeShader == null)
            {
                volume.parameters.atlasIndex = -1;
                addTexture = false;
            }
            else if (volume.parameters.volumeMask != null)
            {
                var tex = volume.parameters.volumeMask;

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
                        volume.parameters.atlasIndex = v.parameters.atlasIndex;
                        addTexture = false;
                        break;
                    }
                }
            }

            if (addTexture)
            {
                volume.parameters.atlasIndex = NumTexturesInAtlas;
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

            if (m_updateAtlas)
            {
                m_updateAtlas = false;

                if (m_volumes.Count > 0)
                {
                    var clampedNumTextures = Math.Max(1, NumTexturesInAtlas);

                    // Calculate the size of the atlas
                    var depths = new int[clampedNumTextures];
                    Vector3Int atlasDim = new Vector3Int();
                    foreach (DensityVolume v in m_volumes)
                    {
                        Vector3Int curDim = new Vector3Int();
                        if (v.parameters.volumeShader != null)
                        {
                            curDim = v.parameters.volumeShaderResolution;
                        }
                        else if (v.parameters.volumeMask != null)
                        {
                            curDim = new Vector3Int(
                                v.parameters.volumeMask.width,
                                v.parameters.volumeMask.height,
                                v.parameters.volumeMask.depth
                            );
                        }
                        atlasDim.x = Mathf.Max(atlasDim.x, curDim.x);
                        atlasDim.y = Mathf.Max(atlasDim.y, curDim.y);
                        atlasDim.z += curDim.z;
                        depths[v.parameters.atlasIndex] = curDim.z;
                    }

                    // Calculate the atlas offset for each volume
                    foreach (DensityVolume v in m_volumes)
                    {
                        if (v.parameters.atlasIndex == -1)
                        {
                            v.parameters.atlasBias = -1.0f;
                            v.parameters.atlasScale = Vector3.one;
                        }
                        else
                        {
                            int offset = 0;
                            // Sum up the depths of the previous textures in the atlas
                            // to get the current texture's offset
                            for (int i = 0; i < v.parameters.atlasIndex; i++)
                            {
                                offset += depths[i];
                            }
                            // Divide by the total atlas depth to get a bias
                            // in the range [0, 1]
                            v.parameters.atlasBias = (float)offset / atlasDim.z;

                            Vector3Int curDim = new Vector3Int();
                            if (v.parameters.volumeShader != null)
                            {
                                curDim = v.parameters.volumeShaderResolution;
                            }
                            else if (v.parameters.volumeMask != null)
                            {
                                curDim = new Vector3Int(
                                    v.parameters.volumeMask.width,
                                    v.parameters.volumeMask.height,
                                    v.parameters.volumeMask.depth
                                );
                            }
                            v.parameters.atlasScale = new Vector3(
                                (float)curDim.x / atlasDim.x,
                                (float)curDim.y / atlasDim.y,
                                (float)curDim.z / atlasDim.z
                            );
                        }
                    }

                    // Allocate the atlas
                    m_atlas = RTHandles.Alloc(
                        atlasDim.x,
                        atlasDim.y,
                        atlasDim.z,
                        dimension: TextureDimension.Tex3D,
                        colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                        enableRandomWrite: true,
                        useMipMap: true,
                        autoGenerateMips: false,
                        name: "DensityVolumeAtlas"
                    );


                    var isCopied = new bool[clampedNumTextures];
                    var oldRt = RenderTexture.active;

                    // Copy all the volumes backed by 3D textures into the atlas
                    foreach (DensityVolume v in m_volumes)
                    {
                        if (v.parameters.volumeShader != null ||
                            v.parameters.atlasIndex == -1 ||
                            isCopied[v.parameters.atlasIndex])
                            continue;

                        isCopied[v.parameters.atlasIndex] = true;

                        var cs = blit3dShader;
                        cmd.SetComputeTextureParam(cs, 0, _DstTex, m_atlas);
                        cmd.SetComputeTextureParam(cs, 0, _SrcTex, v.parameters.volumeMask);
                        cmd.SetComputeIntParam(cs, _ZBias, Mathf.RoundToInt(v.parameters.atlasBias * m_atlas.rt.volumeDepth));
                        cmd.DispatchCompute(
                            cs,
                            0,
                            v.parameters.volumeMask.width / NUM_THREADS,
                            v.parameters.volumeMask.height / NUM_THREADS,
                            v.parameters.volumeMask.depth / NUM_THREADS
                        );
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
                    if (v.parameters.volumeShader != null &&
                        v.parameters.atlasIndex != -1)
                    {
                        var cs = v.parameters.volumeShader;
                        DensityVolumeManager.ComputeShaderParamsDelegate callback;
                        if (DensityVolumeManager.ComputeShaderParams.TryGetValue(v, out callback))
                        {
                            callback?.Invoke(v, cs, cmd);
                        }
                        cmd.SetComputeTextureParam(cs, 0, HDShaderIDs._VolumeMaskAtlas, m_atlas);
                        int zBias = Mathf.RoundToInt(v.parameters.atlasBias * m_atlas.rt.volumeDepth);
                        cmd.SetComputeIntParam(cs, _ZBias, zBias);
                        cmd.DispatchCompute(
                            cs,
                            0,
                            v.parameters.volumeShaderResolution.x / NUM_THREADS,
                            v.parameters.volumeShaderResolution.y / NUM_THREADS,
                            v.parameters.volumeShaderResolution.z / NUM_THREADS
                        );
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
