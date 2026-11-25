
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Core
{
    internal readonly struct AtlasUsage
    {
        public readonly int UsedSliceCount;
        public readonly int CapacitySliceCount;
        public readonly int MaxSliceCount;
        public readonly long AllocatedBytes;

        public AtlasUsage(int usedSliceCount, int capacitySliceCount, int maxSliceCount, long allocatedBytes)
        {
            UsedSliceCount = usedSliceCount;
            CapacitySliceCount = capacitySliceCount;
            MaxSliceCount = maxSliceCount;
            AllocatedBytes = allocatedBytes;
        }
    }

    // Container type that stores a set of arbitrarily sized textures in a 2D texture array,
    // where each slice is a quadtree. Supports add/update/remove operations.
    internal class TextureSlotAllocator : IDisposable
    {
        public readonly struct TextureLocation
        {
            public readonly int AtlasIndex; // Which atlas is the texture in
            public readonly TextureQuadTree.TextureNode TextureNode; // Which position in the atlas is the texture

            public TextureLocation(int atlasIndex, TextureQuadTree.TextureNode textureNode)
            {
                AtlasIndex = atlasIndex;
                TextureNode = textureNode;
            }

            public static readonly TextureLocation Invalid = new(-1, null);
            public bool IsValid => AtlasIndex >= 0 && TextureNode != null;
        }

        private readonly int _size;
        private readonly GraphicsFormat _format;
        private readonly FilterMode _filterMode;
        private readonly int _maxSliceCount;
        private readonly long _bytesPerPixel;
        private readonly CommandBuffer _cmd;
        private RenderTexture _atlases;

        private readonly HashSet<int> _freeAtlases = new();
        private TextureQuadTree[] _textureQuadTrees = Array.Empty<TextureQuadTree>();

        // Texture sizes stored in each quadtree node don't necessarily match the size of the quadtree node.
        // We keep track of the real texture size here, so we can write to the atlas without stretching.
        private readonly Dictionary<TextureLocation, Vector2Int> _textureSizes = new();

        private readonly int _tempTextureID = Shader.PropertyToID("_TempSlotRT");

        public RenderTexture Texture => _atlases;

        public TextureSlotAllocator(int size, GraphicsFormat format, FilterMode filterMode, int maxSliceCount)
        {
            Debug.Assert(maxSliceCount > 0, "The atlas must allow at least one slice.");
            _size = size;
            _format = format;
            _filterMode = filterMode;
            _maxSliceCount = maxSliceCount;
            _bytesPerPixel = GraphicsFormatUtility.GetBlockSize(format);
            _cmd = new CommandBuffer();
            _cmd.name = $"Texture Slot Allocator ({size}, {format})";

            // Initial allocation, in order to have a texture atlas in place even if we don't add anything to it.
            // Note: This is required when binding the textures to ray tracing shaders (compute shaders seem to be OK with null textures)
            bool created = TryResizeAtlas(1);
            Debug.Assert(created, "Failed to create the initial texture atlas slice.");
        }

        public TextureLocation AddTexture(Texture texture, Vector2 scale, Vector2 offset)
        {
            // Find the max dimension
            int size = Mathf.Max(texture.width, texture.height);

            // Try to find an appropriately sized atlas in the free list
            int atlasIndex = -1;
            TextureQuadTree atlas = null;
            foreach (int freeIndex in _freeAtlases)
            {
                if (_textureQuadTrees[freeIndex].HasSpaceForTexture(GetNodeSize(size)))
                {
                    atlasIndex = freeIndex;
                    atlas = _textureQuadTrees[atlasIndex];
                    break;
                }
            }

            // If we couldn't find one, create a new atlas
            if (atlasIndex == -1 || atlas == null)
            {
                int used = _textureQuadTrees.Length;
                if (used >= _maxSliceCount)
                    return TextureLocation.Invalid;

                if (used == _atlases.volumeDepth && !TryGrowAtlas(used))
                    return TextureLocation.Invalid;

                // Create a new atlas
                atlasIndex = used;
                Array.Resize(ref _textureQuadTrees, atlasIndex + 1);
                atlas = new TextureQuadTree(_size);
                _textureQuadTrees[atlasIndex] = atlas;

                // Add it to the free list
                _freeAtlases.Add(atlasIndex);
            }

            // Place in atlas
            if (!atlas.AddTexture(size, out var textureNode))
            {
                return TextureLocation.Invalid;
            }

            // We might have filled up the atlas. If so, we remove it from the free list, if it is there.
            if (atlas.IsFull)
            {
                _freeAtlases.Remove(atlasIndex);
            }

            // Blit the texture into the atlas
            TextureLocation location = new TextureLocation(atlasIndex, textureNode);
            UpdateTexture(in location, texture, scale, offset);
            return location;
        }

        public void UpdateTexture(in TextureLocation location, Texture texture, Vector2 scale, Vector2 offset)
        {
            TextureQuadTree.TextureNode node = location.TextureNode;
            int atlasIndex = location.AtlasIndex;
            Debug.Assert(atlasIndex < _atlases.volumeDepth, "The texture atlas is too small.");

            Vector2 textureScale = new Vector2(
                node.Size / (float)Mathf.Min(texture.width, node.Size),
                node.Size / (float)Mathf.Min(texture.height, node.Size));

            _cmd.GetTemporaryRT(_tempTextureID, new RenderTextureDescriptor(node.Size, node.Size, _format, 0));
            _cmd.Blit(texture, _tempTextureID, textureScale * scale, offset);
            _cmd.CopyTexture(_tempTextureID, 0, 0, 0, 0, node.Size, node.Size, _atlases, atlasIndex, 0, node.PosX, node.PosY);
            _cmd.ReleaseTemporaryRT(_tempTextureID);
            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();

            // Keep track of texture size
            _textureSizes[location] = new Vector2Int(texture.width, texture.height);
        }

        public void RemoveTexture(in TextureLocation location)
        {
            Debug.Assert(location.AtlasIndex < _textureQuadTrees.Length);
            var atlas = _textureQuadTrees[location.AtlasIndex];
            atlas.RemoveTexture(location.TextureNode);

            // After removal, the atlas can't be full, so make sure it's in the free list
            _freeAtlases.Add(location.AtlasIndex);

            // No need to hold on to texture size anymore
            _textureSizes.Remove(location);

            // We could choose to shrink the atlas here to save memory,
            // but don't do so currently due to the overhead of resizing the atlas.
        }

        public Vector2Int GetTextureSize(in TextureLocation location)
        {
            return new Vector2Int(
                Mathf.Min(_textureSizes[location].x, location.TextureNode.Size),
                Mathf.Min(_textureSizes[location].y, location.TextureNode.Size));
        }

        public void GetScaleAndOffset(in TextureLocation location, out Vector2 scale, out Vector2 offset)
        {
            Debug.Assert(location.AtlasIndex < _textureQuadTrees.Length);

            Vector2Int textureSize = GetTextureSize(location);
            scale = new Vector2(textureSize.x / (float)_size, textureSize.y / (float)_size);
            offset = new Vector2(location.TextureNode.PosX / (float)_size, location.TextureNode.PosY / (float)_size);
        }

        internal int GetNodeSize(int textureSize) => Mathf.Min(Mathf.NextPowerOfTwo(textureSize), _size);

        internal AtlasUsage GetUsage()
        {
            int capacity = _atlases != null ? _atlases.volumeDepth : 0;
            int used = 0;
            foreach (var quadTree in _textureQuadTrees)
            {
                if (!quadTree.IsEmpty)
                    used++;
            }
            return new AtlasUsage(used, capacity, _maxSliceCount, capacity * (long)_size * _size * _bytesPerPixel);
        }

        private bool TryGrowAtlas(int usedSliceCount)
        {
            int doubled = Mathf.Min(usedSliceCount * 2, _maxSliceCount);
            return TryResizeAtlas(doubled) || (doubled > usedSliceCount + 1 && TryResizeAtlas(usedSliceCount + 1));
        }

        private bool TryResizeAtlas(int sliceCount)
        {
            // Create new atlas array
            var texture = new RenderTexture(new RenderTextureDescriptor(_size, _size)
            {
                dimension = TextureDimension.Tex2DArray,
                depthBufferBits = 0,
                volumeDepth = sliceCount,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye,
                graphicsFormat = _format,
                enableRandomWrite = true,
            })
            {
                name = $"Texture Atlas (Format = {_format})",
                hideFlags = HideFlags.DontSaveInEditor,
                wrapMode = TextureWrapMode.Clamp,
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = _filterMode,
            };
            if (!texture.Create())
            {
                CoreUtils.Destroy(texture);
                return false;
            }

            if (_atlases != null)
            {
                int copyCount = Mathf.Min(sliceCount, _textureQuadTrees.Length);
                for (int i = 0; i < copyCount; i++)
                    _cmd.CopyTexture(_atlases, i, texture, i);
                Graphics.ExecuteCommandBuffer(_cmd);
                _cmd.Clear();

                _atlases.Release();
                CoreUtils.Destroy(_atlases);
            }

            _atlases = texture;
            return true;
        }

        public void Dispose()
        {
            _cmd?.Dispose();

            if (_atlases != null)
            {
                _atlases.Release();
                CoreUtils.Destroy(_atlases);
            }
        }
    }
}
