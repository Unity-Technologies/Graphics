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
        // For the packing in 3D, we use an algorithm that packs the 3D textures in an octree
        // where the top level elements are divided into piece of maxElementSize size.
        // Due to the hardware limitation of max 2048 pixels in one dimension of a Texture3D, 
        // the atlas uses first the x axis and then y to place the volumes.
        // The z dimension of the atlas will always have the size of maxElementSize.
        // Here's a 2D representation of a possible atlas layout with 5 volumes:
        // +-----+-----+-----+-----+
        // |     |B |C |     |     |
        // |  A  +-----+  E  |     |
        // |     |D |  |     |     |
        // +-----+--+--+-----+-----+
        // As you can see the second cell is divided to place smaller POT elements in the atlas.
        // When an element is removed, the cell is marked as free. When the last leaf of a cell
        // is removed, all the leaves are removed to form a cell of maxElementSize again.
        class AtlasElement
        {
            public Vector3Int       position;
            public int              size;
            public Texture          texture;
            public int              hash;

            public AtlasElement[]   children = null;
            public AtlasElement     parent = null;

            // If the texture is null, then it means this space is free 
            public bool IsFree() => texture == null && children == null;

            public AtlasElement(Vector3Int position, int size, Texture texture = null)
            {
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.hash = 0;
            }

            public void PopulateChildren()
            {
                children = new AtlasElement[8];
                
                // TODO: split the cell in 8 cubes of POT size
                for (int i = 0; i < 8; i++)
                {
                    children[i].position = new Vector3Int();
                    children[i].parent = this;
                }
            }

            public void RemoveChildrenIfEmpty()
            {
                bool remove = true;
                foreach (var child in children)
                    if (child.texture != null)
                        remove = false;

                if (remove)
                    children = null;
            }
        }

        List<AtlasElement>  m_Elements = new List<AtlasElement>();
        // We keep track of cached texture in a map because it's faster to traverse than the element tree when looking for a texture
        Dictionary<Texture, AtlasElement> m_TextureElementsMap = new Dictionary<Texture, AtlasElement>();

        RenderTexture m_Atlas;
        RenderTexture m_MipMapGenerationTemp;
        GraphicsFormat m_format;
        ComputeShader m_Texture3DAtlasCompute;
        int m_CopyKernel;
        int m_GenerateMipKernel;
        Vector3Int m_KernelGroupSize;

        bool m_updateAtlas = false;
        int m_MaxElementSize = 0;
        int m_MaxElementCount = 0;
        bool m_HasMipMaps = false;

        public Texture3DAtlas(GraphicsFormat format, int maxElementSize, int maxElementCount, bool hasMipMaps = true)
        {
            m_format = format;
            m_MaxElementSize = maxElementSize;
            m_MaxElementCount = maxElementCount;
            m_HasMipMaps = hasMipMaps;

            // Texture 3D are limited to 2048 resolution in every axis, so wen need to create the atlas in x and y axis:
            const int maxTexture3DSize = 2048; // TODO replace this by SystemInfo.maxTexture3DSize when it will be available.

            int maxElementCountPerDimension = maxTexture3DSize / maxElementSize;
            int xElementCount = Mathf.Min(maxElementCount, maxElementCountPerDimension);
            int yElementCount = maxElementCount < maxElementCountPerDimension ? 1 : Mathf.CeilToInt(maxElementCount / maxElementCountPerDimension);

            m_Atlas = new RenderTexture(xElementCount * maxElementSize, yElementCount * maxElementSize, 0, format)
            {
                volumeDepth = maxElementSize,
                dimension = TextureDimension.Tex3D,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true,
                useMipMap = hasMipMaps,
                autoGenerateMips = false,
                name = $"Texture 3D Atlas - {xElementCount * maxElementSize}x{yElementCount * maxElementSize}x{maxElementSize}",
            };
            m_Atlas.Create();

            // Quarter res temp texture used for the mip generation
            m_MipMapGenerationTemp = new RenderTexture(maxElementSize / 4, maxElementSize / 4, 0, format)
            {
                volumeDepth = maxElementSize / 4,
                dimension = TextureDimension.Tex3D,
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true,
                useMipMap = hasMipMaps,
                autoGenerateMips = false,
                name = $"Texture 3D MipMap Temp - {maxElementSize / 4}x{maxElementSize / 4}x{maxElementSize / 4}",
            };
            m_MipMapGenerationTemp.Create();

            // Fill the atlas with empty elements:
            for (int i = 0; i < maxElementCount; i++)
            {
                Vector3Int pos = new Vector3Int((i % xElementCount) * maxElementSize, (int)((i / (float)xElementCount) * maxElementSize), 0);
                m_Elements.Add(new AtlasElement(pos, maxElementSize));
            }

            m_Texture3DAtlasCompute = HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.texture3DAtlasCS;
            m_CopyKernel = m_Texture3DAtlasCompute.FindKernel("Copy");
            m_GenerateMipKernel = m_Texture3DAtlasCompute.FindKernel("GenerateMipMap");
            m_Texture3DAtlasCompute.GetKernelThreadGroupSizes(m_CopyKernel, out var groupThreadX, out var groupThreadY, out var groupThreadZ);
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

        public bool IsTextureValid(Texture tex)
        {
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

            return true;
        }

        public bool AddTexture(Texture tex)
        {
            if (m_TextureElementsMap.ContainsKey(tex))
                return true;

            if (!IsTextureValid(tex))
                return false;

            if (!TryAddTextureToTree(tex))
                return false;

            return true;
        }

        bool TryAddTextureToTree(Texture tex)
        {
            foreach (var element in m_Elements)
            {
                // TODO: handle smaller elements correctly
                if (element.IsFree() && element.size >= tex.width)
                {
                    element.texture = tex;
                    m_TextureElementsMap.Add(tex, element);
                    return true;
                }
            }

            return false;
        }

        public void RemoveTexture(Texture tex)
        {
            if (m_TextureElementsMap.TryGetValue(tex, out var element))
            {
                element.texture = null;
                if (element.parent != null)
                    element.parent.RemoveChildrenIfEmpty();

                m_TextureElementsMap.Remove(tex);
            }
        }

        public void ClearTextures()
        {
            foreach (var elem in m_Elements)
            {
                elem.texture = null;
                elem.children = null;
            }
            m_TextureElementsMap.Clear();
        }

        public Vector2 GetTextureOffset(Texture tex)
        {
            if (tex != null && m_TextureElementsMap.TryGetValue(tex, out var element))
                return new Vector2(element.position.x, element.position.y);
            else
                return -Vector2.one;
        }

        public void Update(CommandBuffer cmd)
        {
            if (m_TextureElementsMap.Count == 0)
                return;

            foreach (var element in m_TextureElementsMap.Values)
            {
                int newHash = GetTextureHash(element.texture);
                // TODO: check if texture resolution haven't changed (it's possible with render textures)

                // if (elem.hash != newHash)
                {
                    element.hash = newHash;

                    CopyTexture(cmd, element);
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

        void CopyTexture(CommandBuffer cmd, AtlasElement element)
        {
            // Copy mip 0 of the texture
            CopyMip(cmd, element.texture, 0, m_Atlas, element.position, 0);

            // If we need mip maps, we either copy them from the source if it has mip maps or we generate them.
            if (m_HasMipMaps)
            {
                int mipMapCount = m_HasMipMaps ? Mathf.FloorToInt(Mathf.Log(element.texture.width, 2)) + 1 : 1;
                bool sourceHasMipMaps = element.texture.mipmapCount > 1;

                // If the source 3D texture has mipmaps, we can just copy them
                if (sourceHasMipMaps)
                    CopyMips(cmd, element.texture, m_Atlas, element.position);
                else // Otherwise, we need to generate them
                {
                    // TODO: handle texture that are smaller than m_MipMapGenerationTemp!

                    // Generating the first mip from the source texture into the atlas to save a copy.
                    GenerateMip(cmd, element.texture, Vector3Int.zero, 0, m_Atlas, element.position, 1);

                    // TODO: struct for this
                    var source = m_Atlas;
                    var destination = m_MipMapGenerationTemp;
                    var sourceOffset = element.position;
                    var destinationOffset = Vector3Int.zero;
                    var sourceMipOffset = 0;
                    var destinationMipOffset = -2; // m_MipMapGenerationTemp is allocated in quater res to save memory so we need to apply a mip offset when writing to it  .
                    for (int i = 2; i < mipMapCount; i++)
                    {
                        GenerateMip(cmd, source, sourceOffset, i + sourceMipOffset - 1, destination, destinationOffset, i + destinationMipOffset);

                        // Swap RTs and offsets 
                        var t = source;
                        source = destination;
                        destination = t;
                        var to = sourceOffset;
                        sourceOffset = destinationOffset;
                        destinationOffset = to;
                        var tm = sourceMipOffset;
                        sourceMipOffset = destinationMipOffset;
                        destinationMipOffset = tm;
                    }

                    // Copy back the mips from the temp target to the atlas
                    for (int i = 2; i < mipMapCount; i += 2)
                    {
                        var mipPos = new Vector3Int((int)element.position.x >> i, (int)element.position.y >> i, (int)element.position.z >> i);
                        CopyMip(cmd, m_MipMapGenerationTemp, i - 2, m_Atlas, mipPos, i);
                    }
                }
            }
        }

        void CopyMips(CommandBuffer cmd, Texture source, Texture destination, Vector3Int destinationOffset)
        {
            int mipMapCount = Mathf.FloorToInt(Mathf.Log(source.width, 2)) + 1;

            for (int i = 1; i < mipMapCount; i++)
            {
                var mipPos = new Vector3Int((int)destinationOffset.x >> i, (int)destinationOffset.y >> i, (int)destinationOffset.z >> i);
                CopyMip(cmd, source, i, destination, mipPos, i);
            }
        }

        void CopyMip(CommandBuffer cmd, Texture source, int sourceMip, Texture destination, Vector3Int destinationOffset, int destinationMip)
        {
            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_CopyKernel, HDShaderIDs._Src3DTexture, source);
            cmd.SetComputeFloatParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcMip, sourceMip);

            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_CopyKernel, HDShaderIDs._Dst3DTexture, destination, destinationMip);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._DstOffset, (Vector3)destinationOffset);

            // Previous volume texture only used the alpha channel so when we copy them, we put a white color to avoid having a black texture
            bool alphaOnly = (source is Texture3D t) && t.format == TextureFormat.Alpha8;
            cmd.SetComputeFloatParam(m_Texture3DAtlasCompute, HDShaderIDs._AlphaOnlyTexture, alphaOnly ? 1 : 0);

            int mipMapSize = (int)source.width >> sourceMip; // We assume that the texture is POT
            cmd.DispatchCompute(
                m_Texture3DAtlasCompute,
                m_CopyKernel,
                Mathf.Max(mipMapSize / m_KernelGroupSize.x, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.y, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.z, 1)
            );
        }

        void GenerateMip(CommandBuffer cmd, Texture source, Vector3Int sourceOffset, int sourceMip, Texture destination, Vector3Int destinationOffset, int destinationMip)
        {
            // Compute the source scale and offset in UV space:
            Vector3 offset = new Vector3(sourceOffset.x / (float)source.width, sourceOffset.y / (float)source.height, sourceOffset.z / (float)GetTextureDepth(source));

            Vector3Int sourceTextureMipSize = new Vector3Int(source.width >> (sourceMip + 1), source.height >> (sourceMip + 1), GetTextureDepth(source) >> (sourceMip + 1));
            Vector3Int destinationTextureMipSize = new Vector3Int(destination.width >> destinationMip, destination.height >> destinationMip, GetTextureDepth(destination) >> destinationMip);

            Vector3 scale = new Vector3(
                Mathf.Min(destinationTextureMipSize.x / (float)sourceTextureMipSize.x, 1),
                Mathf.Min(destinationTextureMipSize.y / (float)sourceTextureMipSize.y, 1),
                Mathf.Min(destinationTextureMipSize.z / (float)sourceTextureMipSize.y, 1)
            );

            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_GenerateMipKernel, HDShaderIDs._Src3DTexture, source);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcScale, scale);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcOffset, offset);
            cmd.SetComputeFloatParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcMip, sourceMip);

            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_GenerateMipKernel, HDShaderIDs._Dst3DTexture, destination, destinationMip);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._DstOffset, (Vector3)destinationOffset);

            int mipMapSize = GetTextureDepth(destination) >> destinationMip; // We assume that the texture is POT
            cmd.SetComputeIntParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcSize, mipMapSize);

            cmd.DispatchCompute(
                m_Texture3DAtlasCompute,
                m_GenerateMipKernel,
                Mathf.Max(mipMapSize / m_KernelGroupSize.x, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.y, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.z, 1)
            );
        }

        public RenderTexture GetAtlas() => m_Atlas;

        public static long GetApproxCacheSizeInByte(int elementSize, int elementCount, GraphicsFormat format)
        {
            int formatInBytes = HDUtils.GetFormatSizeInBytes(format);
            long elementSizeInBytes = elementSize * elementSize * elementSize * formatInBytes;

            return elementSizeInBytes * elementCount;
        }
    }
}
