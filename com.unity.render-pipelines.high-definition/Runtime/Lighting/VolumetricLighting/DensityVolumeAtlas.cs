using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DensityVolumeAtlas
    {
        public static readonly int _ZBias = Shader.PropertyToID("_ZBias");
        public static readonly int _DstTex = Shader.PropertyToID("_DstTex");
        public static readonly int _SrcTex = Shader.PropertyToID("_SrcTex");
        public int NumTexturesInAtlas = 0;

        private List<DensityVolume> m_Volumes = new List<DensityVolume>();
        private List<DensityVolume> m_DynamicDensityVolumes = new List<DensityVolume>();

        private RTHandle m_Atlas = null;

        private bool m_UpdateAtlas = false;

        private bool[] m_IsCopiedCache = new bool[16];
        private void EnsureIsCopiedCache(int countRequested)
        {
            if ((countRequested > 0) && ((m_IsCopiedCache == null) || (m_IsCopiedCache.Length < countRequested)))
            {
                m_IsCopiedCache = new bool[countRequested];
            }
        }
        private void ClearIsCopiedCache(int countRequested)
        {
            for (int i = 0; i < countRequested; ++i)
            {
                m_IsCopiedCache[i] = false;
            }
        }

        private int[] m_DepthsCache = new int[16];
        private void EnsureDepthsCache(int countRequested)
        {
            if ((countRequested > 0) && ((m_DepthsCache == null) || (m_DepthsCache.Length < countRequested)))
            {
                m_DepthsCache = new int[countRequested];
            }
        }
        private void ClearDepthsCache(int countRequested)
        {
            for (int i = 0; i < countRequested; ++i)
            {
                m_DepthsCache[i] = -1;
            }
        }

        private static readonly TextureFormat s_SourceTextureFormat = TextureFormat.Alpha8;

        ~DensityVolumeAtlas()
        {
            if (m_Atlas != null) { RTHandles.Release(m_Atlas); }
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

                if (volume.parameters.volumeMask.format != s_SourceTextureFormat)
                {
                    Debug.LogError(String.Format(
                        "3D Texture Atlas: Added texture {2} format {0} does not match format of atlas {1}",
                        tex.format,
                        s_SourceTextureFormat,
                        tex.name
                    ));
                    return;
                }

                // Check if a volume with the same mask texture already exists
                foreach (DensityVolume v in m_Volumes)
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
                m_UpdateAtlas = true;
            }
            m_Volumes.Add(volume);

            if ((volume.parameters.volumeShader != null) && (volume.parameters.atlasIndex != -1))
            {
                m_DynamicDensityVolumes.Add(volume);
            }
        }

        public void RemoveVolume(DensityVolume volume)
        {
            int volumesIndex = m_Volumes.IndexOf(volume);
            if (volumesIndex != -1)
            {
                m_Volumes.RemoveAt(volumesIndex);
                m_UpdateAtlas = true;

                int dynamicDensityVolumesIndex = m_DynamicDensityVolumes.IndexOf(volume);
                if (dynamicDensityVolumesIndex != -1)
                {
                    m_DynamicDensityVolumes.RemoveAt(dynamicDensityVolumesIndex);
                }
            }
        }

        public void ClearVolumes()
        {
            m_Volumes.Clear();
            m_DynamicDensityVolumes.Clear();
            m_UpdateAtlas = true;
            NumTexturesInAtlas = 0;
        }

        public void UpdateAtlas(CommandBuffer cmd, ComputeShader blit3dShader, Vector3Int atlasResolution)
        {
            EnsureAtlas(atlasResolution);

            bool updateMipmapsRequested = false;

            if (m_UpdateAtlas)
            {
                m_UpdateAtlas = false;

                var clampedNumTextures = Math.Max(1, NumTexturesInAtlas);

                ComputeDepthsCache(clampedNumTextures);
                ComputeDensityVolumeAtlasOffsets(atlasResolution);
                EnsureIsCopiedCache(clampedNumTextures);
                ClearIsCopiedCache(clampedNumTextures);
                updateMipmapsRequested |= TryBlitStaticDensityDensityVolumeMasksIntoAtlas(cmd, blit3dShader);
            }

            updateMipmapsRequested |= TryDispatchDynamicDensityVolumes(cmd);

            if (updateMipmapsRequested)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.GenerateDensityVolumeAtlasMipmaps)))
                {
                    // Optimization opportunity: Only generate mipmap data for pixels that were updated
                    // i.e: for the regions of each static or dynamic density volume updated in the atlas this frame.
                    m_Atlas.rt.GenerateMips();
                }
            }
        }

        public RTHandle GetAtlas()
        {
            return m_Atlas;
        }

        private static readonly string s_DensityVolumeAtlasName = "DensityVolumeAtlas";

        private void EnsureAtlas(Vector3Int atlasResolution)
        {
            if (m_Atlas == null ||
                atlasResolution.x != m_Atlas.rt.width ||
                atlasResolution.y != m_Atlas.rt.height ||
                atlasResolution.z != m_Atlas.rt.volumeDepth)
            {
                if (m_Atlas != null) { RTHandles.Release(m_Atlas); }

                m_Atlas = RTHandles.Alloc(
                    atlasResolution.x,
                    atlasResolution.y,
                    atlasResolution.z,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                    enableRandomWrite: true,
                    useMipMap: true,
                    autoGenerateMips: false,
                    name: s_DensityVolumeAtlasName
                );

                m_UpdateAtlas = true;
            }
        }

        private void ComputeDepthsCache(int clampedNumTextures)
        {
            // Calculate the texture depths of the atlas
            EnsureDepthsCache(clampedNumTextures);
            ClearDepthsCache(clampedNumTextures);
            foreach (DensityVolume v in m_Volumes)
            {
                if (v.parameters.atlasIndex == -1)
                {
                    continue;
                }

                Vector3Int curDim = new Vector3Int();
                if (v.parameters.volumeShader != null)
                {
                    curDim = DensityVolume.FixupDynamicVolumeResolution(v.parameters.volumeShaderResolution);
                }
                else if (v.parameters.volumeMask != null)
                {
                    curDim = new Vector3Int(
                        v.parameters.volumeMask.width,
                        v.parameters.volumeMask.height,
                        v.parameters.volumeMask.depth
                    );
                }
                m_DepthsCache[v.parameters.atlasIndex] = curDim.z;
            }
        }

        private void ComputeDensityVolumeAtlasOffsets(Vector3Int atlasResolution)
        {
            foreach (DensityVolume v in m_Volumes)
            {
                if (v.parameters.atlasIndex == -1)
                {
                    v.parameters.atlasBias = -1.0f;
                    v.parameters.atlasScale = Vector3.one;
                }
                else
                {
                    int offset = 0;
                    // Sum up the m_DepthsCache of the previous textures in the atlas
                    // to get the current texture's offset
                    for (int i = 0; i < v.parameters.atlasIndex; i++)
                    {
                        offset += m_DepthsCache[i];
                    }
                    // Divide by the total atlas depth to get a bias
                    // in the range [0, 1]
                    v.parameters.atlasBias = (float)offset / atlasResolution.z;

                    Vector3Int curDim = new Vector3Int();
                    if (v.parameters.volumeShader != null)
                    {
                        curDim = DensityVolume.FixupDynamicVolumeResolution(v.parameters.volumeShaderResolution);
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
                        (float)curDim.x / atlasResolution.x,
                        (float)curDim.y / atlasResolution.y,
                        (float)curDim.z / atlasResolution.z
                    );

                    if (curDim.x > atlasResolution.x
                        || curDim.y > atlasResolution.y
                        || curDim.z + offset > atlasResolution.z)
                    {
                        Debug.LogError(
                            String.Format(
                                "The density volume atlas ({0}x{1}x{2}) is smaller than the requested size ({3}x{4}x{5}). Reduce the size/number of density volumes or increase the atlas size in the HDRP asset.",
                                atlasResolution.x, atlasResolution.y, atlasResolution.z,
                                curDim.x, curDim.y, curDim.z + offset
                            )
                        );
                    }
                }
            }
        }

        private bool TryBlitStaticDensityDensityVolumeMasksIntoAtlas(CommandBuffer cmd, ComputeShader blit3dShader)
        {
            bool updated = false;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BlitStaticDensityVolumeMasks)))
            {
                var oldRt = RenderTexture.active;

                // Copy all the volumes backed by 3D textures into the atlas
                foreach (DensityVolume v in m_Volumes)
                {
                    if (v.parameters.volumeShader != null ||
                        v.parameters.atlasIndex == -1 ||
                        m_IsCopiedCache[v.parameters.atlasIndex])
                    {
                        continue;
                    }

                    m_IsCopiedCache[v.parameters.atlasIndex] = true;

                    cmd.SetComputeTextureParam(blit3dShader, 0, _DstTex, m_Atlas);
                    cmd.SetComputeTextureParam(blit3dShader, 0, _SrcTex, v.parameters.volumeMask);
                    cmd.SetComputeIntParam(blit3dShader, _ZBias, Mathf.RoundToInt(v.parameters.atlasBias * m_Atlas.rt.volumeDepth));
                    cmd.DispatchCompute(
                        blit3dShader,
                        0,
                        v.parameters.volumeMask.width / DensityVolume.RESOLUTION_QUANTUM,
                        v.parameters.volumeMask.height / DensityVolume.RESOLUTION_QUANTUM,
                        v.parameters.volumeMask.depth / DensityVolume.RESOLUTION_QUANTUM
                    );

                    updated = true;
                }

                RenderTexture.active = oldRt;
            }

            return updated;
        }

        private bool TryDispatchDynamicDensityVolumes(CommandBuffer cmd)
        {
            bool updated = false;

            if ((m_Atlas != null) && (m_DynamicDensityVolumes.Count > 0) && (DensityVolumeManager.DynamicDensityVolumeCallback != null))
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DispatchDynamicDensityVolumeShaders)))
                {
                    DensityVolumeManager.DynamicDensityVolumeCallback.Invoke(m_DynamicDensityVolumes, cmd, m_Atlas);

                    updated = true;
                }
            }

            return updated;
        }
    }
}
