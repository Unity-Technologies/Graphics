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
            public Vector3Int position;
            public int size;
            public Texture texture;
            public int hash;

            public AtlasElement[] children = null;
            public AtlasElement parent = null;

            // If the texture is null, then it means this space is free
            public bool IsFree() => texture == null && children == null;

            public AtlasElement(Vector3Int position, int size, Texture texture = null)
            {
                this.position = position;
                this.size = size;
                this.texture = texture;
                this.hash = 0;
            }

            // Subdivide the current cell in 8 cubes of equal size
            public void PopulateChildren()
            {
                children = new AtlasElement[8];

                int halfSize = size / 2;
                // Down Front left corner
                children[0] = new AtlasElement(position + new Vector3Int(0, 0, 0), halfSize);
                // Down Front right corner
                children[1] = new AtlasElement(position + new Vector3Int(halfSize, 0, 0), halfSize);
                // Down Back left corner
                children[2] = new AtlasElement(position + new Vector3Int(0, 0, halfSize), halfSize);
                // Down Back right corner
                children[3] = new AtlasElement(position + new Vector3Int(halfSize, 0, halfSize), halfSize);
                // Up Front left corner
                children[4] = new AtlasElement(position + new Vector3Int(0, halfSize, 0), halfSize);
                // Up Front right corner
                children[5] = new AtlasElement(position + new Vector3Int(halfSize, halfSize, 0), halfSize);
                // Up Back left corner
                children[6] = new AtlasElement(position + new Vector3Int(0, halfSize, halfSize), halfSize);
                // Up Back right corner
                children[7] = new AtlasElement(position + new Vector3Int(halfSize, halfSize, halfSize), halfSize);

                foreach (var child in children)
                    child.parent = this;
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

            public override string ToString() => $"3D Atlas Element, pos: {position}, size: {size}, texture:{texture}, children: {children != null}";
        }

        List<AtlasElement> m_Elements = new List<AtlasElement>();
        // We keep track of cached texture in a map because it's faster to traverse than the element tree when looking for a texture
        Dictionary<Texture, AtlasElement> m_TextureElementsMap = new Dictionary<Texture, AtlasElement>();

        RenderTexture m_Atlas;
        RenderTexture m_MipMapGenerationTemp;
        GraphicsFormat m_format;
        ComputeShader m_Texture3DAtlasCompute;
        int m_CopyKernel;
        int m_GenerateMipKernel;
        Vector3Int m_KernelGroupSize;

        int m_MaxElementSize = 0;
        int m_MaxElementCount = 0;
        bool m_HasMipMaps = false;

        const float k_MipmapFactorApprox = 1.33f;

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
                Vector3Int pos = new Vector3Int((i % xElementCount) * maxElementSize, (int)(Mathf.FloorToInt(i / (float)xElementCount) * maxElementSize), 0);
                var elem = new AtlasElement(pos, maxElementSize);
                m_Elements.Add(elem);
            }

            m_Texture3DAtlasCompute = HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.texture3DAtlasCS;
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

            if (tex.width < 1)
            {
                Debug.LogError($"3D Texture Atlas: Added texture {tex} size {tex.width} is smaller than 1.");
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
            // For texture that have the max size in the atlas, we just have to find the first empty element.
            if (tex.width == m_MaxElementSize)
            {
                var freeElem = m_Elements.FirstOrDefault(e => e.IsFree());
                if (freeElem != null)
                {
                    SetTextureToElem(freeElem, tex);
                    return true;
                }
            }
            else // Otherwise, we traverse the tree in depth to find a suitable position
            {
                // Find free element by looking at children
                var freeElem = FindFreeElementWithSize(tex.width);

                if (freeElem != null)
                {
                    SetTextureToElem(freeElem, tex);
                    return true;
                }
                else
                {
                    // If we didn't found any empty element of the same size as the texture, then we have to create a new one
                    freeElem = m_Elements.FirstOrDefault(e => e.IsFree());

                    // No more space in the atlas
                    if (freeElem == null)
                        return true;

                    while (freeElem.size > tex.width)
                    {
                        freeElem.PopulateChildren();
                        freeElem = freeElem.children[0];
                    }

                    SetTextureToElem(freeElem, tex);
                    return true;
                }
            }

            void SetTextureToElem(AtlasElement element, Texture texture)
            {
                element.texture = texture;
                m_TextureElementsMap.Add(texture, element);
            }

            return false;
        }

        AtlasElement FindFreeElementWithSize(int size)
        {
            AtlasElement FindFreeElement(int size, AtlasElement elem)
            {
                if (elem.size == size)
                {
                    if (elem.IsFree())
                        return elem;
                    else
                        return null;
                }

                if (elem.children == null)
                    return null;

                foreach (var child in elem.children)
                {
                    if (child.children != null && child.size >= size)
                    {
                        var cell = FindFreeElement(size, child);
                        if (cell != null)
                            return cell;
                    }
                    else if (child.IsFree())
                        return child;
                }
                return null;
            }

            foreach (var elem in m_Elements)
            {
                var result = FindFreeElement(size, elem);
                if (result != null)
                    return result;
            }

            return null;
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

        public Vector3 GetTextureOffset(Texture tex)
        {
            if (tex != null && m_TextureElementsMap.TryGetValue(tex, out var element))
                return (Vector3)element.position;
            else
                return -Vector3.one;
        }

        public void Update(CommandBuffer cmd)
        {
            if (m_TextureElementsMap.Count == 0)
                return;

            // First pass to remove / add textures that changed resolution, it can happens if a 3D render texture is resized
            foreach (var element in m_Elements)
            {
                var texture = element.texture;

                if (texture == null)
                    continue;

                if (texture.width != element.size)
                {
                    RemoveTexture(texture);
                    AddTexture(texture);
                    continue;
                }
            }

            // Second pass to update elements where the texture content have changed
            foreach (var element in m_TextureElementsMap.Values)
            {
                if (element.texture == null)
                    continue;

                int newHash = GetTextureHash(element.texture);

                if (element.hash != newHash)
                {
                    element.hash = newHash;

                    CopyTexture(cmd, element);
                }
            }
        }

        struct MipGenerationSwapData
        {
            public RenderTexture target;
            public Vector3Int offset;
            public int mipOffset;
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

                    MipGenerationSwapData source = new MipGenerationSwapData { target = m_Atlas, offset = element.position, mipOffset = 0 };
                    // m_MipMapGenerationTemp is allocated in quater res to save memory so we need to apply a mip offset when writing to it.
                    int tempMipOffset = (int)Mathf.Log((m_MipMapGenerationTemp.width / (element.size >> 2)), 2);
                    MipGenerationSwapData destination = new MipGenerationSwapData { target = m_MipMapGenerationTemp, offset = Vector3Int.zero, mipOffset = tempMipOffset - 2 };

                    for (int i = 2; i < mipMapCount; i++)
                    {
                        GenerateMip(cmd, source.target, source.offset, i + source.mipOffset - 1, destination.target, destination.offset, i + destination.mipOffset);

                        // Swap rt settings
                        var temp = source;
                        source = destination;
                        destination = temp;
                    }

                    // Copy back the mips from the temp target to the atlas
                    for (int i = 2; i < mipMapCount; i += 2)
                    {
                        var mipPos = new Vector3Int((int)element.position.x >> i, (int)element.position.y >> i, (int)element.position.z >> i);
                        CopyMip(cmd, m_MipMapGenerationTemp, i - 2 + tempMipOffset, m_Atlas, mipPos, i);
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
            cmd.SetComputeIntParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcSize, mipMapSize);

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
            Vector3Int dstOffset = new Vector3Int(destinationOffset.x >> destinationMip, destinationOffset.y >> destinationMip, destinationOffset.z >> destinationMip);

            Vector3Int minSourceSize = new Vector3Int(Mathf.Min(source.width, destination.width), Mathf.Min(source.height, destination.height), Mathf.Min(GetTextureDepth(source), GetTextureDepth(destination)));
            // Vector3Int sourceTextureMipSize = new Vector3Int(minSourceSize.x >> sourceMip, minSourceSize.y >> sourceMip, minSourceSize.z >> sourceMip);
            Vector3Int destinationTextureMipSize = new Vector3Int(destination.width >> destinationMip, destination.height >> destinationMip, GetTextureDepth(destination) >> destinationMip);

            Vector3 scale = Vector3.one;

            Vector3Int sourceMipSize = new Vector3Int(source.width >> (sourceMip + 1), source.height >> (sourceMip + 1), GetTextureDepth(source) >> (sourceMip + 1));
            Vector3Int destinationMipSize = new Vector3Int(destination.width >> destinationMip, destination.height >> destinationMip, GetTextureDepth(destination) >> destinationMip);
            // if (source.width > destination.width)
            // {
            scale = new Vector3(
                Mathf.Min((float)destinationMipSize.x / sourceMipSize.x, 1),
                Mathf.Min((float)destinationMipSize.y / sourceMipSize.y, 1),
                Mathf.Min((float)destinationMipSize.z / sourceMipSize.z, 1)
            );
            // }

            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_GenerateMipKernel, HDShaderIDs._Src3DTexture, source);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcScale, scale);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcOffset, offset);
            cmd.SetComputeFloatParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcMip, sourceMip);

            cmd.SetComputeTextureParam(m_Texture3DAtlasCompute, m_GenerateMipKernel, HDShaderIDs._Dst3DTexture, destination, destinationMip);
            cmd.SetComputeVectorParam(m_Texture3DAtlasCompute, HDShaderIDs._DstOffset, (Vector3)dstOffset);

            // This is not correct when the atlas is the destination, we can compute it using the min of mip source size and dest mip side.
            int mipMapSize = Mathf.Min(GetTextureDepth(source) >> (sourceMip + 1), GetTextureDepth(destination) >> (destinationMip));
            cmd.SetComputeIntParam(m_Texture3DAtlasCompute, HDShaderIDs._SrcSize, mipMapSize);

            bool alphaOnly = (source is Texture3D t) && t.format == TextureFormat.Alpha8;
            cmd.SetComputeFloatParam(m_Texture3DAtlasCompute, HDShaderIDs._AlphaOnlyTexture, alphaOnly ? 1 : 0);

            cmd.DispatchCompute(
                m_Texture3DAtlasCompute,
                m_GenerateMipKernel,
                Mathf.Max(mipMapSize / m_KernelGroupSize.x, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.y, 1),
                Mathf.Max(mipMapSize / m_KernelGroupSize.z, 1)
            );
        }

        public RenderTexture GetAtlas() => m_Atlas;

        public void Release()
        {
            ClearTextures();
            CoreUtils.Destroy(m_Atlas);
            CoreUtils.Destroy(m_MipMapGenerationTemp);
        }

        public static long GetApproxCacheSizeInByte(int elementSize, int elementCount, GraphicsFormat format, bool hasMipMaps)
        {
            int formatInBytes = HDUtils.GetFormatSizeInBytes(format);
            long elementSizeInBytes = (long)(elementSize * elementSize * elementSize * formatInBytes * (hasMipMaps ? k_MipmapFactorApprox : 1.0f));

            return elementSizeInBytes * elementCount;
        }

        public static int GetMaxElementCountForWeightInByte(long weight, int elementSize, int elementCount, GraphicsFormat format, bool hasMipMaps)
        {
            long elementSizeInByte = (long)((long)elementSize * elementSize * elementSize * HDUtils.GetFormatSizeInBytes(format) * (hasMipMaps ? k_MipmapFactorApprox : 1.0f));
            return (int)Mathf.Clamp(weight / elementSizeInByte, 1, elementCount);
        }
    }
}
