using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Texture 3D atlas. It can only stores power of two cubic textures.
    /// In this atlas, texture are guaranteed to be aligned with a power of two grid.
    /// </summary>
    class Texture3DAtlas
    {
        class AtlasElement
        {
            public Vector3Int       position;
            public int              size;
            public Texture          texture;
            public int              hash;

            public AtlasElement[]   children = null;

            // If the texture is null, then it means this space is free 
            public bool IsFree() => texture == null && children == null;

            public AtlasElement(Vector3Int position, int size, Texture texture = null)
            {
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.hash = 0;
            }
        }

        List<AtlasElement>  m_Elements = new List<AtlasElement>();
        // We keep track of cached texture in a hashSet because it's faster to traverse than the element tree
        HashSet<Texture>    m_CachedTextures = new HashSet<Texture>();

        RenderTexture m_atlas;
        GraphicsFormat m_format;
        ComputeShader m_VolumeCopyCompute;
        int m_CopyKernel;
        Vector3Int m_KernelGroupSize;

        bool m_updateAtlas = false;
        int m_MaxElementSize = 0;
        int m_MaxElementCount = 0;

        public Texture3DAtlas(GraphicsFormat format, int maxElementSize, int maxElementCount)
        {
            m_format = format;
            m_MaxElementSize = maxElementSize;
            m_MaxElementCount = maxElementCount;

            m_atlas = new RenderTexture(maxElementSize, maxElementSize, 0, format)
            {
                volumeDepth = maxElementSize * maxElementCount,
                dimension = TextureDimension.Tex3D,
                hideFlags = HideFlags.HideAndDontSave,
                name = $"Density Volume Atlas - {maxElementSize}x{maxElementSize}x{maxElementSize * maxElementCount}",
            };

            // FIll the atlas with empty elements:
            for (int i = 0; i < maxElementCount; i++)
                m_Elements.Add(new AtlasElement(new Vector3Int(0, 0, i * maxElementSize), maxElementSize));
            
            m_VolumeCopyCompute = HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.densityVolumeAtlasCopy;
            m_CopyKernel = m_VolumeCopyCompute.FindKernel("Copy");
            m_VolumeCopyCompute.GetKernelThreadGroupSizes(m_CopyKernel, out var groupThreadX, out var groupThreadY, out var groupThreadZ);
            m_KernelGroupSize = new Vector3Int((int)groupThreadX, (int)groupThreadY, (int)groupThreadZ);
        }

        int GetTextureDepth(Texture t)
        {
            if (t is Texture3D volume)
                return volume.depth;
            else if (t is RenderTexture rt)
                return rt.volumeDepth;
            return 0;
        }

        protected int GetTextureHash(Texture texture)
        {
            int hash = texture.GetHashCode();

            unchecked
            {
#if UNITY_EDITOR
                hash = 23 * hash + texture.imageContentsHash.GetHashCode();
#endif
                hash = 23 * hash + texture.GetInstanceID().GetHashCode();
                hash = 23 * hash + texture.graphicsFormat.GetHashCode();
                hash = 23 * hash + texture.width.GetHashCode();
                hash = 23 * hash + texture.height.GetHashCode();
                hash = 23 * hash + texture.updateCount.GetHashCode();
            }

            return hash;
        }

        public bool AddTexture(Texture tex)
        {
            if (m_CachedTextures.Contains(tex))
                return true;

            if (tex.width != tex.height || tex.height != GetTextureDepth(tex))
            {
                Debug.LogError($"3D Texture Atlas: Added texture {tex} is not doesn't have a cubic size {tex.width}x{tex.height}x{GetTextureDepth(tex)}.");
                return false;
            }

            if (tex.width > m_MaxElementSize)
            {
                Debug.LogError($"3D Texture Atlas: Added texture {tex} size {tex.width} is bigger than the max element atlas size {m_MaxElementSize}.");
                return false;
            }

            if (!Mathf.IsPowerOfTwo(tex.width))
            {
                Debug.LogError($"3D Texture Atlas: Added texture {tex} size {tex.width} is not power of two.");
                return false;
            }

            // TODO: handle case where there is no more place
            if (TryFindEmptyPosition(tex, out var position))
                m_Elements.Add(new AtlasElement(position, tex.width, tex));

            return true;
        }

        bool TryFindEmptyPosition(Texture tex, out Vector3Int position)
        {
            position = Vector3Int.zero;

            foreach (var element in m_Elements)
            {
                // TODO: handle smaller elements correctly
                if (element.IsFree() && element.size >= tex.width)
                {
                    element.texture = tex;
                    position = element.position;
                    return true;
                }
            }

            return false;
        }

        public void RemoveTexture(Texture tex)
        {
            if (m_CachedTextures.Contains(tex))
            {
                m_CachedTextures.Remove(tex);

                Debug.Log("TODO: remove element");
            }
        }

        public void ClearTextures()
        {
            Debug.Log("TODO: clear elements");
            foreach (var elem in m_Elements)
            {
                elem.texture = null;
                elem.children = null;
            }
            m_CachedTextures.Clear();
        }

        // TODO: remove?
        public int GetTextureIndex(Texture tex)
        {
            // return m_textures.IndexOf(tex);
            return 0;
        }

        public void Update(CommandBuffer cmd)
        {
            if (m_CachedTextures.Count == 0)
                return;

            // TODO: check if all the texture are up to date & update them if needed

            // TODO: GC.Alloc
            Stack<AtlasElement> elements = new Stack<AtlasElement>();
            foreach (var element in m_Elements)
                elements.Push(element);
            
            while (elements.Count > 0)
            {
                var elem = elements.Pop();

                if (elem.children != null)
                {
                    foreach (var child in elem.children)
                        elements.Push(child);
                }
                else if (elem.texture != null)
                {
                    int newHash = GetTextureHash(elem.texture);

                    // Check if the texture on GPU is up to date:
                    if (elem.hash != newHash)
                    {
                        elem.hash = newHash;

                        // TODO: GPU copy with compute shader
                        cmd.SetComputeTextureParam(m_VolumeCopyCompute, m_CopyKernel, HDShaderIDs._VolumeMaskAtlas, m_atlas);
                        cmd.SetComputeTextureParam(m_VolumeCopyCompute, m_CopyKernel, HDShaderIDs._Source, elem.texture);
                        cmd.DispatchCompute(
                            m_VolumeCopyCompute,
                            m_CopyKernel,
                            elem.texture.width / m_KernelGroupSize.x,
                            elem.texture.height / m_KernelGroupSize.y,
                            GetTextureDepth(elem.texture) / m_KernelGroupSize.z
                        );
                    }
                }
            }

            // if (m_textures.Count > 0)
            // {
            //     int textureSliceSize = m_MaxElementSize * m_MaxElementSize * m_MaxElementSize;
            //     int totalTextureSize = textureSliceSize * m_textures.Count;

            //     Color [] colorData = new Color[totalTextureSize];
            //     m_atlas = new Texture3D(m_MaxElementSize, m_MaxElementSize, m_MaxElementSize * m_textures.Count, m_format, true);

            //     //Iterate through all the textures and append their texture data to the texture array
            //     //Once CopyTexture works for 3D textures we can replace this with a series of copy texture calls
            //     for (int i = 0; i < m_textures.Count; i++)
            //     {
            //         Texture3D tex = m_textures[i];
            //         Color [] texData = tex.GetPixels();
            //         Array.Copy(texData, 0, colorData, textureSliceSize * i, texData.Length);
            //     }

            //     m_atlas.SetPixels(colorData);
            //     m_atlas.Apply();
            // }
            // else
            // {
            //     m_atlas = null;
            // }
        }

        public RenderTexture GetAtlas() => m_atlas;

        public static long GetApproxCacheSizeInByte(int elementSize, int elementCount, GraphicsFormat format)
        {
            int formatInBytes = HDUtils.GetFormatSizeInBytes(format);
            long elementSizeInBytes = elementSize * elementSize * elementSize * formatInBytes;

            return elementSizeInBytes * elementCount;
        }
    }
}
