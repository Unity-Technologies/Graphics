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
            m_atlas = RTHandles.Alloc(
                1,
                1,
                1,
                dimension: TextureDimension.Tex3D,
                colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                name: "DensityVolumeAtlas"
            );
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

        public void UpdateAtlas(CommandBuffer cmd, ComputeShader blit3dShader, Vector3Int atlasResolution)
        {
            if (atlasResolution.x != m_atlas.rt.width ||
                atlasResolution.y != m_atlas.rt.height ||
                atlasResolution.z != m_atlas.rt.volumeDepth)
            {
                m_atlas = RTHandles.Alloc(
                    atlasResolution.x,
                    atlasResolution.y,
                    atlasResolution.z,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: Experimental.Rendering.GraphicsFormat.R8_UNorm,
                    enableRandomWrite: true,
                    useMipMap: true,
                    autoGenerateMips: false,
                    name: "DensityVolumeAtlas"
                );
                m_updateAtlas = true;
            }

            if (m_updateAtlas)
            {
                m_updateAtlas = false;

                var clampedNumTextures = Math.Max(1, NumTexturesInAtlas);

                // Calculate the texture depths of the atlas
                var depths = new int[clampedNumTextures];
                foreach (DensityVolume v in m_volumes)
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
                        v.parameters.volumeMask.width / DensityVolume.RESOLUTION_QUANTUM,
                        v.parameters.volumeMask.height / DensityVolume.RESOLUTION_QUANTUM,
                        v.parameters.volumeMask.depth / DensityVolume.RESOLUTION_QUANTUM
                    );
                }
                RenderTexture.active = oldRt;
            }

            if (m_atlas != null)
            {
                DensityVolumeManager.DynamicDensityVolumeCallback?.Invoke(m_volumes, cmd, m_atlas);
                // Currently, the mipchain of the whole atlas is regenerated even if a single
                // sub-texture is touched. This is an optimization opportunity. Details here:
                // https://docs.google.com/document/d/12glZGvntX2pQ0Reh9pqlRmfi_5wmIQG1zAqI1myqN5g/edit#heading=h.3cvzkltd785j
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
